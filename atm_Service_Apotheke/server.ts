import express from "express";
import path from "path";
import { createServer as createViteServer } from "vite";
import { GoogleGenAI, Type } from "@google/genai";
import { createClient } from "@supabase/supabase-js";
import dotenv from "dotenv";
import PDFDocument from "pdfkit";

dotenv.config();

function generateAnamnesePDF(patient: any, triage: any): Promise<Buffer> {
  return new Promise((resolve, reject) => {
    const doc = new PDFDocument({ margin: 50 });
    const buffers: Buffer[] = [];
    
    doc.on('data', chunk => buffers.push(chunk));
    doc.on('end', () => resolve(Buffer.concat(buffers)));
    doc.on('error', err => reject(err));

    // Colors
    const primaryColor = '#0082C8';
    const textColor = '#0f172a';
    const lightText = '#64748b';
    const borderColor = '#e2e8f0';

    // Title / Brand
    doc.fillColor(primaryColor).font('Helvetica-Bold').fontSize(22).text('Service Apotheke', { align: 'left' });
    doc.fillColor(lightText).font('Helvetica').fontSize(9).text('Assistierte Telemedizin (aTM) - Dokumentationssystem', { align: 'left' });
    doc.moveDown(1);
    
    // Horizontal separator
    doc.strokeColor(borderColor).lineWidth(1.5).moveTo(50, doc.y).lineTo(550, doc.y).stroke();
    doc.moveDown(1.5);

    // Document Title
    doc.fillColor(textColor).font('Helvetica-Bold').fontSize(16).text('Klinisches Anamnese-Protokoll', { align: 'center' });
    doc.moveDown(0.5);
    doc.fillColor(lightText).font('Helvetica').fontSize(9).text(`Erstellungsdatum: ${new Date().toLocaleString('de-DE')}`, { align: 'center' });
    doc.moveDown(1.5);

    // Patient Info container box
    const currentY = doc.y;
    doc.fillColor('#f8fafc');
    doc.rect(50, currentY, 500, 100).fill();
    
    doc.fillColor(textColor).fontSize(10);
    doc.font('Helvetica-Bold').text('Patientendaten', 65, currentY + 10, { underline: true });
    
    doc.font('Helvetica').text(`Name: ${patient.patient_name || '-'}`, 65, currentY + 30);
    doc.text(`Geburtsdatum: ${patient.birth_date ? new Date(patient.birth_date).toLocaleDateString('de-DE') : '-'}`, 300, currentY + 30);
    
    doc.text(`Krankenkasse: ${patient.health_insurance_name || '-'}`, 65, currentY + 50);
    doc.text(`Versichertennummer (KVNR): ${patient.health_insurance_number || '-'}`, 300, currentY + 50);
    
    doc.text(`Kostenträgerkennung (IK): ${patient.ik_number || '-'}`, 65, currentY + 70);
    doc.text(`Statusfeld: ${patient.status_field || '-'}`, 300, currentY + 70);
    
    doc.y = currentY + 115; // Move below the box
    doc.moveDown(1.5);

    // Triage Info
    doc.fillColor(primaryColor).font('Helvetica-Bold').fontSize(12).text('Ersteinschätzungs-Ergebnisse (SmED)', { underline: false });
    doc.moveDown(0.8);
    
    doc.fillColor(textColor).fontSize(10);
    
    doc.font('Helvetica-Bold').text('Hauptbeschwerde / Symptom-Kategorie:');
    doc.font('Helvetica').text(`• ${triage.symptoms || '-'}`);
    doc.moveDown(0.8);
    
    doc.font('Helvetica-Bold').text('Dauer der Beschwerden:');
    doc.font('Helvetica').text(`• ${triage.duration || '-'}`);
    doc.moveDown(0.8);
    
    doc.font('Helvetica-Bold').text('Dringlichkeitseinstufung:');
    doc.font('Helvetica').text(`• ${triage.urgency || '-'}`);
    doc.moveDown(2);

    // Footer
    doc.strokeColor(borderColor).lineWidth(1).moveTo(50, doc.y).lineTo(550, doc.y).stroke();
    doc.moveDown(1.5);
    doc.fillColor(lightText).font('Helvetica').fontSize(8).text('Dieses Dokument wurde im Kiosk-Modus der Apotheke automatisiert generiert und verschlüsselt in der elektronischen Dokumentenablage gespeichert. Es dient ausschließlich der Unterstützung der ärztlichen Befundung.', { align: 'center', lineGap: 2 });

    doc.end();
  });
}

async function startServer() {
  const app = express();
  const PORT = 3000;

  app.use(express.json({ limit: "50mb" }));

  // Initialize Gemini AI
  // Keep key server-side
  let ai: GoogleGenAI | null = null;
  function getGemini() {
    if (!ai) {
      const key = process.env.GEMINI_API_KEY;
      if (!key) {
        throw new Error("GEMINI_API_KEY environment variable is missing.");
      }
      ai = new GoogleGenAI({ 
        apiKey: key,
        httpOptions: {
          headers: {
            'User-Agent': 'aistudio-build',
          }
        }
      });
    }
    return ai;
  }

  // --- Supabase Admin Initialization ---
  const supabaseUrl = process.env.VITE_SUPABASE_URL || "https://tngpemwxwkwazijgivjn.supabase.co";
  const supabaseServiceKey = process.env.SUPABASE_SERVICE_ROLE_KEY;

  if (!supabaseServiceKey) {
    console.warn("WARNING: SUPABASE_SERVICE_ROLE_KEY is missing from environment variables.");
  }

  const supabaseAdmin = createClient(supabaseUrl, supabaseServiceKey || "dummy-key");

  // --- API Routes ---
  app.get("/api/health", (req, res) => {
    res.json({ status: "ok" });
  });

  // 1. Consent API Endpoint
  app.post('/api/kiosk/consent', async (req, res) => {
    try {
      const { pharmacy_id, patient_name, health_insurance_name, health_insurance_number, signature_blob, ik_nummer, birth_date, status_field } = req.body;

      if (!pharmacy_id || !patient_name || !signature_blob || !health_insurance_name) {
        return res.status(400).json({ error: 'Fehlende Pflichtfelder (inkl. health_insurance_name)' });
      }

      const { data, error } = await supabaseAdmin
        .from('consent_agreements')
        .insert([{
          pharmacy_id,
          patient_name,
          health_insurance_name, // Wichtig für den NOT NULL Constraint
          health_insurance_number,
          signature_blob,
          ik_number: ik_nummer,
          birth_date,
          status_field
        }])
        .select('id')
        .single();

      if (error) {
        console.error("Error inserting consent agreement:", error);
        return res.status(500).json({ error: error.message });
      }
      
      res.status(200).json({ success: true, consent_id: data.id });
    } catch (error: any) {
      console.error("Server error inserting consent agreement:", error);
      res.status(500).json({ error: error.message });
    }
  });

  // 2. Billing API Endpoint (Neu für RLS Bypass)
  app.post('/api/kiosk/billing', async (req, res) => {
    try {
      const { consent_id, service_type, amount, date_of_service, sonderkennzeichen, executed_by_pharmacist_name, pharmacy_id: body_pharmacy_id } = req.body;

      if (!consent_id || !service_type) {
        return res.status(400).json({ error: 'Fehlende Pflichtfelder für Abrechnung' });
      }

      // Resolve pharmacy_id from consent_agreements if not provided in body to keep database records complete
      let pharmacy_id = body_pharmacy_id;
      if (!pharmacy_id) {
        const { data: consentData, error: consentErr } = await supabaseAdmin
          .from('consent_agreements')
          .select('pharmacy_id')
          .eq('id', consent_id)
          .single();
        if (!consentErr && consentData) {
          pharmacy_id = consentData.pharmacy_id;
        }
      }

      const { data, error } = await supabaseAdmin
        .from('billing_records')
        .insert([{
          pharmacy_id,
          consent_id,
          service_type,
          amount,
          date_of_service,
          sonderkennzeichen,
          executed_by_pharmacist_name
        }])
        .select('id')
        .single();

      if (error) {
        console.error("Error inserting billing record:", error);
        return res.status(500).json({ error: error.message });
      }
      
      res.status(200).json({ success: true, billing_id: data.id });
    } catch (error: any) {
      console.error("Server error inserting billing record:", error);
      res.status(500).json({ error: error.message });
    }
  });

  // 2.5 Generate PDF Clinical Report (Anamnese-Protokoll)
  app.post('/api/kiosk/generate-report', async (req, res) => {
    try {
      const { consent_id, billing_id, triage_data } = req.body;

      if (!consent_id || !triage_data) {
        return res.status(400).json({ error: 'Fehlende Pflichtfelder (consent_id oder triage_data)' });
      }

      // Fetch patient info securely using the admin client
      const { data: patient, error: patientErr } = await supabaseAdmin
        .from('consent_agreements')
        .select('*')
        .eq('id', consent_id)
        .single();

      if (patientErr || !patient) {
        console.error('Error fetching patient for report:', patientErr);
        return res.status(404).json({ error: 'Einverständniserklärung nicht gefunden' });
      }

      // Generate the PDF Buffer
      const pdfBuffer = await generateAnamnesePDF(patient, triage_data);

      // Ensure storage bucket exists
      await supabaseAdmin.storage.createBucket('clinical-reports', { public: false }).catch(() => {
        // Ignore duplicate bucket error
      });

      // Upload generated PDF to Supabase Storage
      const filePath = `reports/${patient.pharmacy_id}/${consent_id}.pdf`;
      const { error: uploadErr } = await supabaseAdmin.storage
        .from('clinical-reports')
        .upload(filePath, pdfBuffer, {
          contentType: 'application/pdf',
          upsert: true,
        });

      if (uploadErr) {
        console.error('Error uploading clinical report:', uploadErr);
        return res.status(500).json({ error: `Upload fehlgeschlagen: ${uploadErr.message}` });
      }

      // Link PDF path to the billing record
      if (billing_id) {
        const { error: updateErr } = await supabaseAdmin
          .from('billing_records')
          .update({ report_path: filePath })
          .eq('id', billing_id);

        if (updateErr) {
          console.error('Error linking report to billing record:', updateErr);
          // Don't fail the request, since the PDF was uploaded successfully
        }
      }

      res.status(200).json({ success: true, file_path: filePath });
    } catch (error: any) {
      console.error('Server error generating clinical report:', error);
      res.status(500).json({ error: error.message });
    }
  });

  app.get("/api/admin/consents", async (req, res) => {
    try {
      const { pharmacy_id } = req.query;
      let query = supabaseAdmin.from("consent_agreements").select("*");
      
      if (pharmacy_id) {
        query = query.eq("pharmacy_id", pharmacy_id);
      }
      
      const { data, error } = await query.order("signed_date", { ascending: false });

      if (error) {
        return res.status(500).json({ error: error.message });
      }

      res.json(data);
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  // Get signed URL for clinical report
  app.get("/api/admin/report-url", async (req, res) => {
    try {
      const { report_path } = req.query;
      if (!report_path) {
        return res.status(400).json({ error: "Missing report_path" });
      }

      const { data, error } = await supabaseAdmin.storage
        .from("clinical-reports")
        .createSignedUrl(report_path as string, 300); // 5 minutes expiry

      if (error) {
        console.error("Error creating signed URL:", error);
        return res.status(500).json({ error: error.message });
      }

      res.json({ url: data.signedUrl });
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  app.get("/api/admin/billing", async (req, res) => {
    try {
      const { pharmacy_id } = req.query;
      let query = supabaseAdmin.from("billing_records").select("*, consent_agreements(*)");
      
      if (pharmacy_id) {
        query = query.eq("pharmacy_id", pharmacy_id);
      }

      const { data, error } = await query.order("created_at", { ascending: false });

      if (error) {
        return res.status(500).json({ error: error.message });
      }

      res.json(data);
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  // 3. Appointments
  app.get("/api/admin/appointments", async (req, res) => {
    try {
      const { pharmacy_id } = req.query;
      let query = supabaseAdmin.from("appointments").select("*");
      
      if (pharmacy_id) {
        query = query.eq("pharmacy_id", pharmacy_id);
      }

      const { data, error } = await query.order("start_time", { ascending: true });

      if (error) {
        return res.status(500).json({ error: error.message });
      }

      res.json(data);
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  app.post("/api/admin/appointments", async (req, res) => {
    try {
      const { pharmacy_id, patient_name, start_time, end_time, status } = req.body;

      const { data, error } = await supabaseAdmin
        .from("appointments")
        .insert([
          {
            pharmacy_id,
            patient_name,
            start_time,
            end_time,
            status,
          }
        ])
        .select("*")
        .single();

      if (error) {
        return res.status(500).json({ error: error.message });
      }

      res.status(201).json(data);
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  // 4. Pharmacies
  app.post("/api/admin/pharmacies", async (req, res) => {
    try {
      const { name, ik_nummer, bsnr, ansprechpartner, telefon, status, is_approved } = req.body;

      const { data, error } = await supabaseAdmin
        .from("pharmacies")
        .insert([
          {
            name,
            ik_nummer,
            bsnr,
            ansprechpartner,
            telefon,
            status: status || 'pending',
            is_approved: is_approved || false,
          }
        ])
        .select("*")
        .single();

      if (error) {
        return res.status(500).json({ error: error.message });
      }

      res.status(201).json(data);
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  app.get("/api/admin/pharmacies", async (req, res) => {
    try {
      const { data, error } = await supabaseAdmin
        .from("pharmacies")
        .select("*")
        .order("created_at", { ascending: false });

      if (error) {
        return res.status(500).json({ error: error.message });
      }

      res.json(data);
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  app.put("/api/admin/pharmacies/:id", async (req, res) => {
    try {
      const { id } = req.params;
      const updates = req.body;

      const { data, error } = await supabaseAdmin
        .from("pharmacies")
        .update(updates)
        .eq("id", id)
        .select("*")
        .single();

      if (error) {
        return res.status(500).json({ error: error.message });
      }

      res.json(data);
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  // Chatbot Endpoint
  app.post("/api/chat", async (req, res) => {
    try {
      const { history, message } = req.body;
      const genAI = getGemini();
      
      const chat = genAI.chats.create({
        model: "gemini-3.1-pro-preview", // using pro for complex tasks
        config: {
          systemInstruction: "Du bist ein hilfreicher und professioneller Assistent für eine deutsche Apotheke im Bereich der Assistierten Telemedizin (aTM). Du hilfst Apothekern bei Fragen zu Ersteinschätzung, Videosprechstunden und Sonderkennzeichen. Antworte immer auf Deutsch und professionell.",
        }
      });

      // Restore history manually if needed, or just send the latest message
      // Actually, since we need to send the whole history to the model:
      // The @google/genai SDK chats.create can take an initial history or we can just send everything as contents.
      // We will construct the contents array from the history
      const contents = history.map((msg: any) => ({
        role: msg.role === 'user' ? 'user' : 'model',
        parts: [{ text: msg.parts[0].text }]
      }));
      contents.push({ role: 'user', parts: [{ text: message }] });

      const response = await genAI.models.generateContent({
        model: "gemini-3.1-pro-preview",
        contents,
        config: {
          systemInstruction: "Du bist ein hilfreicher und professioneller Assistent für eine deutsche Apotheke im Bereich der Assistierten Telemedizin (aTM). Du hilfst Apothekern bei Fragen zu Ersteinschätzung, Videosprechstunden und Sonderkennzeichen. Antworte immer auf Deutsch und professionell."
        }
      });

      res.json({ text: response.text });
    } catch (error: any) {
      console.error("Gemini Error:", error);
      res.status(500).json({ error: error.message || "Failed to generate response" });
    }
  });

  // Image Analysis Endpoint
  app.post("/api/analyze-image", async (req, res) => {
    try {
      const { imageBase64, mimeType, prompt } = req.body;
      const genAI = getGemini();
      
      const response = await genAI.models.generateContent({
        model: "gemini-3.1-pro-preview",
        contents: {
          parts: [
            {
              inlineData: {
                data: imageBase64,
                mimeType: mimeType || "image/jpeg",
              },
            },
            {
              text: prompt || "Bitte analysiere dieses Bild und fasse zusammen, was du siehst. Ist es für die Telemedizin relevant?",
            },
          ],
        },
        config: {
          systemInstruction: "Du bist ein hilfreicher und professioneller Assistent für eine deutsche Apotheke im Bereich der Assistierten Telemedizin (aTM). Antworte auf Deutsch.",
        }
      });

      res.json({ text: response.text });
    } catch (error: any) {
      console.error("Gemini Error:", error);
      res.status(500).json({ error: error.message || "Failed to analyze image" });
    }
  });

  // --- Vite Middleware ---
  if (process.env.NODE_ENV !== "production") {
    const vite = await createViteServer({
      server: { middlewareMode: true },
      appType: "spa",
    });
    app.use(vite.middlewares);
  } else {
    const distPath = path.join(process.cwd(), "dist");
    app.use(express.static(distPath));
    // For React Router fallback
    app.get("*", (req, res) => {
      res.sendFile(path.join(distPath, "index.html"));
    });
  }

  app.listen(PORT, "0.0.0.0", () => {
    console.log(`Server running on http://localhost:${PORT}`);
  });
}

startServer();
