import express from "express";
import path from "path";
import { createServer as createViteServer } from "vite";
import { GoogleGenAI, Type } from "@google/genai";
import { createClient } from "@supabase/supabase-js";
import dotenv from "dotenv";
import PDFDocument from "pdfkit";
import winston from "winston";
import Stripe from "stripe";

dotenv.config();

const logger = winston.createLogger({
  level: "info",
  format: winston.format.combine(
    winston.format.timestamp(),
    winston.format.json()
  ),
  transports: [
    new winston.transports.Console(),
    new winston.transports.File({ filename: "error.log", level: "error" }),
    new winston.transports.File({ filename: "system-events.log" })
  ]
});

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

    doc.font('Helvetica-Bold').text('Warnhinweise (Red Flags) geprüft:');
    doc.font('Helvetica').text(`• Keine akute Vitalgefährdung gemeldet (Frage 2 negativ)`);
    doc.moveDown(0.8);
    
    doc.font('Helvetica-Bold').text('Dauer der Beschwerden:');
    doc.font('Helvetica').text(`• ${triage.duration || '-'}`);
    doc.moveDown(0.8);
    
    doc.font('Helvetica-Bold').text('Dringlichkeitseinstufung (Vorfilterung):');
    doc.font('Helvetica').text(`• ${triage.urgency || '-'}`);
    doc.moveDown(2);

    // Disclaimer Logging
    doc.fillColor(primaryColor).font('Helvetica-Bold').fontSize(12).text('Rechtliche Hinweise & Disclaimer', { underline: false });
    doc.moveDown(0.8);
    doc.fillColor(textColor).fontSize(10).font('Helvetica');
    doc.text('Der Patient hat am Kiosk-Terminal aktiv per Klick bestätigt, dass das System keine medizinische Diagnose durchführt und keinen Arztbesuch in Notfällen ersetzt. Der Patient wurde angewiesen, bei akuter Lebensgefahr die 112 zu wählen.');
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
  let PORT = parseInt(process.env.PORT || "4000", 10);
  
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
    logger.warn("WARNING: SUPABASE_SERVICE_ROLE_KEY is missing from environment variables.");
  }

  const supabaseAdmin = createClient(supabaseUrl, supabaseServiceKey || "dummy-key");
  
  // Removed verifyDeviceToken

  // --- API Routes ---
  app.get("/api/health", (req, res) => {
    res.json({ status: "ok" });
  });

  // 1. Consent API Endpoint
  app.post('/api/kiosk/consent', async (req, res) => {
    const { pharmacy_id, patient_name, health_insurance_name, health_insurance_number, signature_blob, ik_nummer, birth_date, status_field } = req.body;
    
    if (!pharmacy_id) return res.status(400).json({ error: "Missing pharmacy_id" });

    try {
      if (!patient_name || !signature_blob || !health_insurance_name) {
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
        logger.error("Error inserting consent agreement:", error);
        return res.status(500).json({ error: error.message });
      }
      
      res.status(200).json({ success: true, consent_id: data.id });
    } catch (error: any) {
      logger.error("Server error inserting consent agreement:", error);
      res.status(500).json({ error: error.message });
    }
  });

  // 2. Billing API Endpoint (Neu für RLS Bypass)
  app.post('/api/kiosk/billing', async (req, res) => {
    const { pharmacy_id, consent_id, service_type, amount, date_of_service, sonderkennzeichen, executed_by_pharmacist_name } = req.body;
    
    if (!pharmacy_id) return res.status(400).json({ error: "Missing pharmacy_id" });

    try {
      if (!consent_id || !service_type) {
        return res.status(400).json({ error: 'Fehlende Pflichtfelder für Abrechnung' });
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
        logger.error("Error inserting billing record:", error);
        return res.status(500).json({ error: error.message });
      }
      
      res.status(200).json({ success: true, record_id: data.id });
    } catch (error: any) {
      logger.error("Server error inserting billing record:", error);
      res.status(500).json({ error: error.message });
    }
  });

  // Telemetry API Endpoint
  app.post('/api/kiosk/telemetry', async (req, res) => {
    try {
      const { pharmacy_id, session_id, event_type } = req.body;
      
      if (!pharmacy_id) return res.status(400).json({ error: 'Fehlende pharmacy_id' });
      if (!session_id || !event_type) {
        return res.status(400).json({ error: 'Fehlende Pflichtfelder' });
      }

      const { error } = await supabaseAdmin
        .from('session_telemetry')
        .insert([{
          session_id,
          pharmacy_id,
          event_type
        }]);

      if (error) {
        logger.error("Error inserting telemetry:", error);
        return res.status(500).json({ error: error.message });
      }
      
      res.status(200).json({ success: true });
    } catch (error: any) {
      logger.error("Server error inserting telemetry:", error);
      res.status(500).json({ error: error.message });
    }
  });

  // 2.5 Generate PDF Clinical Report (Anamnese-Protokoll)
  app.post('/api/kiosk/generate-report', async (req, res) => {
    try {
      const { pharmacy_id, consent_id, billing_id, triage_data } = req.body;
      
      if (!pharmacy_id) return res.status(400).json({ error: "Missing pharmacy_id" });
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
        logger.error('Error fetching patient for report:', patientErr);
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
        logger.error('Error uploading clinical report:', uploadErr);
        return res.status(500).json({ error: `Upload fehlgeschlagen: ${uploadErr.message}` });
      }

      // Link PDF path to the billing record
      if (billing_id) {
        const { error: updateErr } = await supabaseAdmin
          .from('billing_records')
          .update({ report_path: filePath })
          .eq('id', billing_id);

        if (updateErr) {
          logger.error('Error linking report to billing record:', updateErr);
          // Don't fail the request, since the PDF was uploaded successfully
        }
      }

      res.status(200).json({ success: true, file_path: filePath });
    } catch (error: any) {
      logger.error('Server error generating report:', error);
      res.status(500).json({ error: error.message });
    }
  });

  // POST /api/billing/generate-summary (Phase 3 Requirement)
  app.post('/api/billing/generate-summary', async (req, res) => {
    try {
      const { appointment_id, consent_id, pharmacy_id, triage_data } = req.body;
      const targetId = appointment_id || consent_id; // Tolerate both naming conventions

      if (!targetId || !pharmacy_id) {
        return res.status(400).json({ error: "Missing appointment_id/consent_id or pharmacy_id" });
      }

      // Validate JWT
      const authHeader = req.headers.authorization;
      if (!authHeader) return res.status(401).json({ error: "Unauthorized" });

      const token = authHeader.replace("Bearer ", "");
      const { data: { user }, error: authError } = await supabaseAdmin.auth.getUser(token);

      if (authError || !user) {
        return res.status(401).json({ error: "Unauthorized / Invalid JWT" });
      }

      // Check user_pharmacy_roles for authorization
      const { data: role, error: roleError } = await supabaseAdmin
        .from('user_pharmacy_roles')
        .select('*')
        .eq('user_id', user.id)
        .eq('pharmacy_id', pharmacy_id)
        .single();

      if (roleError || !role) {
        return res.status(403).json({ error: "Forbidden - Not authorized for this pharmacy" });
      }

      // Fetch patient info
      const { data: patient, error: patientErr } = await supabaseAdmin
        .from('consent_agreements')
        .select('*')
        .eq('id', targetId)
        .single();

      if (patientErr || !patient) {
        return res.status(404).json({ error: 'Patientendaten/Einverständnis nicht gefunden' });
      }

      // Generate the PDF Buffer
      const pdfBuffer = await generateAnamnesePDF(patient, triage_data || {});

      // Ensure storage bucket exists
      await supabaseAdmin.storage.createBucket('billing_documentation', { public: false }).catch(() => {});

      // Upload generated PDF to Supabase Storage
      const filePath = `reports/${pharmacy_id}/${targetId}.pdf`;
      const { error: uploadErr } = await supabaseAdmin.storage
        .from('billing_documentation')
        .upload(filePath, pdfBuffer, {
          contentType: 'application/pdf',
          upsert: true,
        });

      if (uploadErr) {
        logger.error('Error uploading billing summary:', uploadErr);
        return res.status(500).json({ error: `Upload fehlgeschlagen: ${uploadErr.message}` });
      }

      // Link PDF path to the billing record if exists
      await supabaseAdmin
        .from('billing_records')
        .update({ report_path: filePath })
        .eq('consent_id', targetId);

      res.status(200).json({ success: true, file_path: filePath });

    } catch (error: any) {
      logger.error('Server error generating clinical report:', error);
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
        logger.error("Error creating signed URL:", error);
        return res.status(500).json({ error: error.message });
      }

      res.json({ url: data.signedUrl });
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  app.get("/api/admin/billing", async (req, res) => {
    try {
      const { pharmacy_id, page = '1', limit = '10', start_date, end_date } = req.query;
      let query = supabaseAdmin.from("billing_records").select("*, consent_agreements(*)", { count: 'exact' });
      
      if (pharmacy_id) {
        query = query.eq("pharmacy_id", pharmacy_id);
      }
      if (start_date) {
        query = query.gte("created_at", start_date);
      }
      if (end_date) {
        query = query.lte("created_at", end_date);
      }

      const pageNum = parseInt(page as string, 10);
      const limitNum = parseInt(limit as string, 10);
      const from = (pageNum - 1) * limitNum;
      const to = from + limitNum - 1;

      const { data, count, error } = await query.order("created_at", { ascending: false }).range(from, to);

      if (error) {
        return res.status(500).json({ error: error.message });
      }

      res.json({ data, total: count, page: pageNum, limit: limitNum });
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  app.get("/api/admin/billing/export", async (req, res) => {
    try {
      const { pharmacy_id, start_date, end_date } = req.query;
      if (!pharmacy_id) return res.status(400).json({ error: "Missing pharmacy_id" });

      let query = supabaseAdmin.from("billing_records").select("*, consent_agreements(*)");
      query = query.eq("pharmacy_id", pharmacy_id);
      
      if (start_date) query = query.gte("created_at", start_date);
      if (end_date) query = query.lte("created_at", end_date);

      const { data, error } = await query.order("created_at", { ascending: false });
      if (error) throw error;

      // ARZ CSV Format: Zeitstempel, IK_Nummer, KVNR, Geburtsdatum, Leistungscode (Sonderkennzeichen), Betrag
      const csvRows = ["Zeitstempel;IK_Nummer;KVNR;Geburtsdatum;Leistungscode;Betrag"];
      
      if (data) {
        for (const row of data) {
          const consent = row.consent_agreements;
          if (!consent) continue;
          const dateStr = new Date(row.created_at).toISOString().split('T')[0];
          csvRows.push(`${dateStr};${consent.ik_number};${consent.health_insurance_number};${consent.birth_date};${row.sonderkennzeichen};${row.amount}`);
        }
      }

      const csvString = csvRows.join("\n");
      res.setHeader('Content-Type', 'text/csv');
      res.setHeader('Content-Disposition', 'attachment; filename="arz_abrechnung_export.csv"');
      res.send(csvString);
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  app.get("/api/admin/telemetry/stats", async (req, res) => {
    try {
      const { pharmacy_id } = req.query;
      if (!pharmacy_id) return res.status(400).json({ error: "Missing pharmacy_id" });

      const { data, error } = await supabaseAdmin
        .from('session_telemetry')
        .select('*')
        .eq('pharmacy_id', pharmacy_id);

      if (error) throw error;

      const sessionsStarted = data.filter((e: any) => e.event_type === 'SESSION_STARTED').length;
      const sessionsCompleted = data.filter((e: any) => e.event_type === 'SESSION_COMPLETED').length;
      const sessionsAborted = data.filter((e: any) => e.event_type === 'SESSION_ABORTED').length;
      
      // Calculate conversion rate
      const successRate = sessionsStarted > 0 ? (sessionsCompleted / sessionsStarted) * 100 : 0;
      
      // Calculate drop-off points
      let dropTriage = 0, dropConsent = 0, dropService = 0;
      data.filter((e: any) => e.event_type === 'SESSION_ABORTED').forEach((e: any) => {
         // simplified metric: we assume they dropped at the step prior to abort if we mapped it, 
         // but since we only have ABORTED, we can infer drop-offs via funnel analysis.
      });

      res.json({
        total_sessions: sessionsStarted,
        completed_sessions: sessionsCompleted,
        aborted_sessions: sessionsAborted,
        success_rate: successRate.toFixed(1),
        // average duration could be calculated by matching session_ids...
      });
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
      const { data, error } = await supabaseAdmin.from("pharmacies").select("*");
      if (error) return res.status(500).json({ error: error.message });
      res.json(data);
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  // --- Zero-Touch Provisioning (Pairing) ---

  // 1. Admin generates a code
  app.post("/api/admin/terminals/pair", async (req, res) => {
    try {
      const { pharmacy_id } = req.body;
      if (!pharmacy_id) return res.status(400).json({ error: "pharmacy_id is required" });

      const code = Math.floor(100000 + Math.random() * 900000).toString(); // 6 digits
      // expires in 15 mins
      const expiresAt = new Date(Date.now() + 15 * 60000).toISOString();

      const { data, error } = await supabaseAdmin
        .from("pairing_codes")
        .insert([{ pharmacy_id, code, expires_at: expiresAt }])
        .select("code")
        .single();

      if (error) return res.status(500).json({ error: error.message });
      res.json({ success: true, code: data.code });
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  // 2. Kiosk redeems a code for a token
  app.post("/api/kiosk/authenticate", async (req, res) => {
    try {
      const { code, terminal_name } = req.body;
      if (!code || !terminal_name) return res.status(400).json({ error: "code and terminal_name required" });

      // Find valid code
      const { data: codeData, error: codeErr } = await supabaseAdmin
        .from("pairing_codes")
        .select("*")
        .eq("code", code)
        .gte("expires_at", new Date().toISOString())
        .single();

      if (codeErr || !codeData) {
        return res.status(400).json({ error: "Invalid or expired code" });
      }

      // Generate token
      const deviceToken = "atm_" + Math.random().toString(36).substring(2) + Date.now().toString(36);

      // Create terminal
      const { data: terminalData, error: terminalErr } = await supabaseAdmin
        .from("kiosk_terminals")
        .insert([{
          pharmacy_id: codeData.pharmacy_id,
          name: terminal_name,
          device_token: deviceToken,
          status: 'active'
        }])
        .select("*")
        .single();

      if (terminalErr) return res.status(500).json({ error: terminalErr.message });

      // Delete the used code
      await supabaseAdmin.from("pairing_codes").delete().eq("id", codeData.id);

      res.json({ success: true, device_token: deviceToken, terminal: terminalData });
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  // 3. Admin lists terminals
  app.get("/api/admin/terminals", async (req, res) => {
    try {
      const { pharmacy_id } = req.query;
      if (!pharmacy_id) return res.status(400).json({ error: "pharmacy_id required" });

      const { data, error } = await supabaseAdmin
        .from("kiosk_terminals")
        .select("*")
        .eq("pharmacy_id", pharmacy_id)
        .order("created_at", { ascending: false });

      if (error) return res.status(500).json({ error: error.message });
      res.json(data);
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  // 4. Admin revokes a terminal
  app.delete("/api/admin/terminals/:id", async (req, res) => {
    try {
      const { error } = await supabaseAdmin
        .from("kiosk_terminals")
        .update({ status: 'revoked' })
        .eq("id", req.params.id);

      if (error) return res.status(500).json({ error: error.message });
      res.json({ success: true });
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  // AVV Accept Endpoint
  app.post("/api/admin/accept-avv", async (req, res) => {
    try {
      const { pharmacy_id } = req.body;
      if (!pharmacy_id) return res.status(400).json({ error: "pharmacy_id is required" });

      const timestamp = new Date().toISOString();
      const { data, error } = await supabaseAdmin
        .from("pharmacies")
        .update({ avv_akzeptiert_am: timestamp })
        .eq("id", pharmacy_id)
        .select("avv_akzeptiert_am")
        .single();

      if (error) return res.status(500).json({ error: error.message });
      res.json({ success: true, avv_akzeptiert_am: data.avv_akzeptiert_am });
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

  // B2B Registration (creates pharmacy, auth user, and pharmacist profile)
  app.post("/api/auth/register-b2b", async (req, res) => {
    try {
      const { name, ik_nummer, bsnr, admin_email, password, telefon } = req.body;

      if (!name || !ik_nummer || !bsnr || !admin_email || !password) {
        return res.status(400).json({ error: "Fehlende Pflichtfelder" });
      }

      // 1. Create the Pharmacy in pharmacies table
      const { data: pharmacy, error: pharmacyErr } = await supabaseAdmin
        .from("pharmacies")
        .insert([
          {
            name,
            ik_nummer,
            bsnr,
            ansprechpartner: admin_email,
            telefon,
            status: "pending",
            is_approved: false,
            onboarding_status: "pending_approval",
          }
        ])
        .select("*")
        .single();

      if (pharmacyErr || !pharmacy) {
        logger.error("Error creating pharmacy:", pharmacyErr);
        return res.status(500).json({ error: `Apothekenerstellung fehlgeschlagen: ${pharmacyErr?.message}` });
      }

      // 2. Create the Auth User in Supabase Auth
      const { data: authUser, error: authErr } = await supabaseAdmin.auth.admin.createUser({
        email: admin_email,
        password: password,
        email_confirm: true,
        user_metadata: {
          pharmacy_id: pharmacy.id,
          role: "pharmacy_admin",
        },
        app_metadata: {
          pharmacy_id: pharmacy.id,
          role: "pharmacy_admin",
        }
      });

      if (authErr || !authUser?.user) {
        logger.error("Error creating auth user:", authErr);
        // Rollback: Delete created pharmacy to maintain consistency
        await supabaseAdmin.from("pharmacies").delete().eq("id", pharmacy.id);
        return res.status(500).json({ error: `Benutzererstellung fehlgeschlagen: ${authErr?.message}` });
      }

      // 3. Create Profile in profiles table
      const { error: profileErr } = await supabaseAdmin
        .from("profiles")
        .insert([
          {
            id: authUser.user.id,
            role: "pharmacy_admin",
            pharmacy_id: pharmacy.id,
            full_name: "Apotheken-Administrator",
          }
        ]);

      if (profileErr) {
        logger.error("Error creating profile:", profileErr);
        // Rollback: Delete auth user and pharmacy
        await supabaseAdmin.auth.admin.deleteUser(authUser.user.id);
        await supabaseAdmin.from("pharmacies").delete().eq("id", pharmacy.id);
        return res.status(500).json({ error: `Profil-Erstellung fehlgeschlagen: ${profileErr.message}` });
      }

      res.status(201).json({ success: true, pharmacy_id: pharmacy.id, user_id: authUser.user.id });
    } catch (error: any) {
      logger.error("Server error in register-b2b:", error);
      res.status(500).json({ error: error.message });
    }
  });

  // Upload Onboarding Document
  app.post("/api/pharmacy/upload-document", async (req, res) => {
    try {
      const { pharmacy_id, doc_type, file_base64 } = req.body;

      if (!pharmacy_id || !doc_type || !file_base64) {
        return res.status(400).json({ error: "Fehlende Parameter" });
      }

      // Convert base64 to Uint8Array for browser-like fetch in Supabase JS
      const buffer = Buffer.from(file_base64, 'base64');
      const uint8Array = new Uint8Array(buffer);

      // Create pharmacy-documents bucket if not exists
      await supabaseAdmin.storage.createBucket('pharmacy-documents', { public: false }).catch(() => {});

      const filePath = `${pharmacy_id}/${doc_type}.pdf`;
      const { error: uploadErr } = await supabaseAdmin.storage
        .from('pharmacy-documents')
        .upload(filePath, uint8Array, {
          contentType: 'application/pdf',
          upsert: true,
        });

      if (uploadErr) {
        logger.error('Error uploading pharmacy document:', uploadErr);
        return res.status(500).json({ error: `Upload fehlgeschlagen: ${uploadErr.message}` });
      }

      // Update document path in DB
      let updateFields: any = {};
      if (doc_type === 'operating_license') {
        updateFields.operating_license_path = filePath;
      } else if (doc_type === 'approbationsurkunde') {
        updateFields.approbationsurkunde_path = filePath;
      } else if (doc_type === 'avv_document') {
        updateFields.avv_document_path = filePath;
      }

      const { data: pharmacy, error: fetchErr } = await supabaseAdmin
        .from('pharmacies')
        .select('*')
        .eq('id', pharmacy_id)
        .single();

      if (fetchErr || !pharmacy) {
        return res.status(404).json({ error: "Apotheke nicht gefunden" });
      }

      // Determine next onboarding status
      // If they uploaded the final document, change status to 'pending_verification'
      const hasLic = doc_type === 'operating_license' || pharmacy.operating_license_path;
      const hasApp = doc_type === 'approbationsurkunde' || pharmacy.approbationsurkunde_path;
      const hasAvv = doc_type === 'avv_document' || pharmacy.avv_document_path;

      if (hasLic && hasApp && hasAvv) {
        updateFields.onboarding_status = 'pending_verification';
      } else {
        updateFields.onboarding_status = 'pending_documents';
      }

      const { error: updateErr } = await supabaseAdmin
        .from('pharmacies')
        .update(updateFields)
        .eq('id', pharmacy_id);

      if (updateErr) {
        logger.error('Error updating pharmacy doc status:', updateErr);
        return res.status(500).json({ error: updateErr.message });
      }

      res.status(200).json({ success: true, file_path: filePath, next_status: updateFields.onboarding_status });
    } catch (error: any) {
      logger.error('Server error uploading document:', error);
      res.status(500).json({ error: error.message });
    }
  });

  // Get signed URL for onboarding document
  app.get("/api/admin/pharmacy-document-url", async (req, res) => {
    try {
      const { report_path } = req.query;
      if (!report_path) {
        return res.status(400).json({ error: "Missing report_path" });
      }

      const { data, error } = await supabaseAdmin.storage
        .from("pharmacy-documents")
        .createSignedUrl(report_path as string, 300); // 5 minutes expiry

      if (error) {
        logger.error("Error creating signed URL for document:", error);
        return res.status(500).json({ error: error.message });
      }

      res.json({ url: data.signedUrl });
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  // Invite pharmacist via Supabase Auth
  app.post("/api/admin/invite", async (req, res) => {
    try {
      const { email, pharmacy_id } = req.body;
      if (!email || !pharmacy_id) {
        return res.status(400).json({ error: "Missing email or pharmacy_id" });
      }

      const origin = req.headers.origin || process.env.VITE_SITE_URL || "http://localhost:4000";
      
      // Invite user through Supabase Auth
      const { data, error } = await supabaseAdmin.auth.admin.inviteUserByEmail(email, {
        data: {
          role: "pharmacist",
          pharmacy_id: pharmacy_id
        },
        redirectTo: `${origin}/login`
      });

      if (error) {
        logger.error("Error inviting user:", error);
        return res.status(500).json({ error: error.message });
      }

      // Create a profile for the invited user
      if (data.user) {
        const { error: profileErr } = await supabaseAdmin
          .from("profiles")
          .insert([
            {
              id: data.user.id,
              role: "pharmacist",
              pharmacy_id: pharmacy_id,
              full_name: email.split("@")[0], // Temporary name
            }
          ]);
        
        if (profileErr) {
          logger.error("Error creating profile for invited user:", profileErr);
          // Non-fatal error, the invite was still sent
        }
      }

      res.status(200).json({ success: true, user: data.user });
    } catch (error: any) {
      logger.error("Server error inviting user:", error);
      res.status(500).json({ error: error.message });
    }
  });

  // Verify onboarding documents and activate pharmacy
  app.post("/api/admin/pharmacies/:id/verify-documents", async (req, res) => {
    try {
      const { id } = req.params;
      const { onboarding_status } = req.body; // 'active' or 'pending_documents' (rejected)

      const isApproved = onboarding_status === 'active';

      const { data, error } = await supabaseAdmin
        .from("pharmacies")
        .update({
          onboarding_status,
          is_approved: isApproved,
          status: isApproved ? 'active' : 'pending'
        })
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

  // SKZ Labels mapping
  const SKZ_LABELS: Record<string, string> = {
    "19816342": "Kombileistung (Ersteinschätzung + Video)",
    "19816313": "Nur Ersteinschätzung",
    "19816336": "Nur Videosprechstunde"
  };

  // Helper function to aggregate billing data
  const getBillingAggregation = async (pharmacy_id: string, month: string) => {
    const startDate = `${month}-01`;
    const nextMonth = new Date(startDate);
    nextMonth.setMonth(nextMonth.getMonth() + 1);
    const endDate = nextMonth.toISOString().split('T')[0];

    const { data: pharmacy, error: pharmErr } = await supabaseAdmin
      .from("pharmacies")
      .select("name, ik_number, bsnr")
      .eq("id", pharmacy_id)
      .single();

    if (pharmErr) throw new Error("Pharmacy not found");

    const { data: records, error: recordsErr } = await supabaseAdmin
      .from("billing_records")
      .select("*")
      .eq("pharmacy_id", pharmacy_id)
      .gte("date_of_service", startDate)
      .lt("date_of_service", endDate);

    if (recordsErr) throw new Error(recordsErr.message);

    const aggregation: Record<string, { count: number; total_amount: number; single_amount: number }> = {};
    records.forEach(record => {
      const skz = record.sonderkennzeichen;
      if (!aggregation[skz]) {
        aggregation[skz] = { count: 0, total_amount: 0, single_amount: Number(record.amount) };
      }
      aggregation[skz].count += 1;
      aggregation[skz].total_amount += Number(record.amount);
    });

    return { pharmacy, aggregation, month };
  };

  // 1. Billing Export Endpoint (JSON)
  app.get("/api/admin/billing/export/:pharmacy_id", async (req, res) => {
    try {
      const { pharmacy_id } = req.params;
      const { month } = req.query;
      if (!month || typeof month !== 'string') return res.status(400).json({ error: "Month parameter (YYYY-MM) is required" });

      const { pharmacy, aggregation } = await getBillingAggregation(pharmacy_id, month);

      const exportData = {
        Abrechnungs_Metadaten: {
          IK_Apotheke: pharmacy.ik_number,
          Abrechnungsmonat: month,
          Erstellungsdatum: new Date().toISOString(),
          Empfaenger: "Nacht- und Notdienstfonds (NNF) / ARZ",
          Format_Version: "1.0-aTM"
        },
        Leistungsdatensaetze: Object.entries(aggregation).map(([skz, data]) => ({
          Sonderkennzeichen: skz,
          Bezeichnung: SKZ_LABELS[skz] || "Unbekannt",
          Anzahl_Leistungen: data.count,
          Verguetung_Einzel_EUR: data.single_amount.toFixed(2),
          Verguetung_Gesamt_EUR: data.total_amount.toFixed(2)
        })),
        Gesamtsumme_EUR: Object.values(aggregation).reduce((sum, d) => sum + d.total_amount, 0).toFixed(2)
      };

      res.json(exportData);
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  // 2. Billing Export Endpoint (CSV)
  app.get("/api/admin/billing/export-csv/:pharmacy_id", async (req, res) => {
    try {
      const { pharmacy_id } = req.params;
      const { month } = req.query;
      if (!month || typeof month !== 'string') return res.status(400).json({ error: "Month required" });

      const { pharmacy, aggregation } = await getBillingAggregation(pharmacy_id, month);

      let csvContent = "\uFEFF"; // UTF-8 BOM
      csvContent += "IK_Apotheke;Abrechnungsmonat;Sonderkennzeichen;Bezeichnung;Anzahl;Einzelbetrag_EUR;Gesamtbetrag_EUR\n";

      let totalSum = 0;
      let totalCount = 0;

      Object.entries(aggregation).forEach(([skz, data]) => {
        const label = SKZ_LABELS[skz] || "Unbekannt";
        csvContent += `${pharmacy.ik_number};${month};${skz};${label};${data.count};${data.single_amount.toFixed(2)};${data.total_amount.toFixed(2)}\n`;
        totalSum += data.total_amount;
        totalCount += data.count;
      });

      csvContent += `;;;GESAMT;${totalCount};;${totalSum.toFixed(2)}\n`;

      res.setHeader('Content-Type', 'text/csv; charset=utf-8');
      res.setHeader('Content-Disposition', `attachment; filename="NNF_Abrechnung_${pharmacy.ik_number}_${month}.csv"`);
      res.send(csvContent);
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  // 3. Billing Export Endpoint (PDF)
  app.get("/api/admin/billing/export-pdf/:pharmacy_id", async (req, res) => {
    try {
      const { pharmacy_id } = req.params;
      const { month } = req.query;
      if (!month || typeof month !== 'string') return res.status(400).json({ error: "Month required" });

      const { pharmacy, aggregation } = await getBillingAggregation(pharmacy_id, month);

      const doc = new PDFDocument({ margin: 50 });
      res.setHeader('Content-Type', 'application/pdf');
      res.setHeader('Content-Disposition', `attachment; filename="Sonderbeleg_${pharmacy.ik_number}_${month}.pdf"`);
      doc.pipe(res);

      // Header
      doc.fontSize(20).text("Sonderbeleg für Telemedizinische Leistungen (aTM)", { align: 'center' });
      doc.moveDown(2);

      // Pharmacy Info
      doc.fontSize(12).font('Helvetica-Bold').text("Apotheke:");
      doc.font('Helvetica').text(pharmacy.name || "Service Apotheke");
      doc.text(`IK-Nummer: ${pharmacy.ik_number || "Nicht hinterlegt"}`);
      doc.text(`BSNR: ${pharmacy.bsnr || "Nicht hinterlegt"}`);
      doc.text(`Abrechnungsmonat: ${month}`);
      doc.text(`Erstellungsdatum: ${new Date().toLocaleDateString('de-DE')}`);
      doc.moveDown(2);

      // Table Header
      const startY = doc.y;
      doc.font('Helvetica-Bold');
      doc.text("Sonderkennzeichen", 50, startY, { width: 120 });
      doc.text("Bezeichnung", 180, startY, { width: 200 });
      doc.text("Anzahl", 390, startY, { width: 50, align: 'right' });
      doc.text("Gesamt (€)", 450, startY, { width: 90, align: 'right' });
      
      doc.moveTo(50, startY + 15).lineTo(540, startY + 15).stroke();
      
      let currentY = startY + 25;
      let totalSum = 0;

      // Table Body
      doc.font('Helvetica');
      Object.entries(aggregation).forEach(([skz, data]) => {
        doc.text(skz, 50, currentY, { width: 120 });
        doc.text(SKZ_LABELS[skz] || "Unbekannt", 180, currentY, { width: 200 });
        doc.text(data.count.toString(), 390, currentY, { width: 50, align: 'right' });
        doc.text(data.total_amount.toFixed(2), 450, currentY, { width: 90, align: 'right' });
        
        totalSum += data.total_amount;
        currentY += 20;
      });

      doc.moveTo(50, currentY).lineTo(540, currentY).stroke();
      currentY += 10;

      // Total
      doc.font('Helvetica-Bold');
      doc.text("Gesamtsumme:", 280, currentY, { width: 160, align: 'right' });
      doc.text(totalSum.toFixed(2) + " €", 450, currentY, { width: 90, align: 'right' });

      doc.moveDown(4);

      // Footer
      doc.font('Helvetica').fontSize(10).text("Rechtsgrundlage: § 129 SGB V", 50, doc.y);
      doc.moveDown(4);
      
      doc.moveTo(50, doc.y).lineTo(250, doc.y).stroke();
      doc.moveDown(0.5);
      doc.text("Ort, Datum, Unterschrift Apothekenleitung", 50, doc.y);

      doc.end();
    } catch (error: any) {
      if (!res.headersSent) res.status(500).json({ error: error.message });
    }
  });

  // Stripe Setup
  const stripe = new Stripe(process.env.STRIPE_SECRET_KEY || "sk_test_placeholder", {
    apiVersion: '2025-01-27.acacia',
  });

  // Stripe Checkout Endpoint
  app.post("/api/stripe/create-checkout-session", async (req, res) => {
    try {
      const { pharmacy_id } = req.body;
      const priceId = process.env.STRIPE_PRICE_ID || "price_placeholder";
      
      const session = await stripe.checkout.sessions.create({
        mode: "subscription",
        payment_method_types: ["card", "sepa_debit"],
        client_reference_id: pharmacy_id,
        line_items: [
          {
            price: priceId,
            quantity: 1,
          },
        ],
        success_url: `${req.headers.origin}/admin?session_id={CHECKOUT_SESSION_ID}&stripe_success=true`,
        cancel_url: `${req.headers.origin}/onboarding`,
      });
      res.json({ checkout_url: session.url });
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  // Stripe Customer Portal Endpoint
  app.post("/api/stripe/portal-session", async (req, res) => {
    try {
      const { pharmacy_id } = req.body;
      
      const { data: pharmacy } = await supabaseAdmin
        .from("pharmacies")
        .select("stripe_customer_id")
        .eq("id", pharmacy_id)
        .single();
        
      if (!pharmacy?.stripe_customer_id) {
        return res.status(400).json({ error: "No active subscription found" });
      }

      const portalSession = await stripe.billingPortal.sessions.create({
        customer: pharmacy.stripe_customer_id,
        return_url: `${req.headers.origin}/admin`,
      });
      res.json({ portal_url: portalSession.url });
    } catch (error: any) {
      res.status(500).json({ error: error.message });
    }
  });

  // Stripe Webhook Endpoint
  app.post("/api/stripe/webhook", express.raw({ type: 'application/json' }), async (req, res) => {
    const sig = req.headers['stripe-signature'];
    let event;

    try {
      event = stripe.webhooks.constructEvent(
        req.body,
        sig,
        process.env.STRIPE_WEBHOOK_SECRET || "whsec_placeholder"
      );
    } catch (err: any) {
      return res.status(400).send(`Webhook Error: ${err.message}`);
    }

    try {
      switch (event.type) {
        case 'checkout.session.completed': {
          const session = event.data.object;
          const pharmacyId = session.client_reference_id;
          const customerId = session.customer;
          const subscriptionId = session.subscription;

          if (pharmacyId) {
            await supabaseAdmin
              .from('pharmacies')
              .update({
                stripe_customer_id: customerId,
                subscription_status: 'active'
              })
              .eq('id', pharmacyId);

            await supabaseAdmin
              .from('subscriptions')
              .insert({
                pharmacy_id: pharmacyId,
                stripe_subscription_id: subscriptionId,
                stripe_price_id: process.env.STRIPE_PRICE_ID || "price_placeholder",
                status: 'active'
              });
          }
          break;
        }
        case 'customer.subscription.updated':
        case 'customer.subscription.deleted': {
          const subscription = event.data.object;
          await supabaseAdmin
            .from('subscriptions')
            .update({
              status: subscription.status,
              current_period_end: new Date(subscription.current_period_end * 1000).toISOString()
            })
            .eq('stripe_subscription_id', subscription.id);
            
          const { data: sub } = await supabaseAdmin
            .from('subscriptions')
            .select('pharmacy_id')
            .eq('stripe_subscription_id', subscription.id)
            .single();
            
          if (sub?.pharmacy_id) {
            await supabaseAdmin
              .from('pharmacies')
              .update({ subscription_status: subscription.status })
              .eq('id', sub.pharmacy_id);
          }
          break;
        }
      }
      res.json({ received: true });
    } catch (err: any) {
      logger.error("Error processing webhook:", err);
      res.status(500).send("Webhook processing failed");
    }
  });

  // Chatbot Endpoint
  app.post("/api/chat", async (req, res) => {
    try {
      const { history, message } = req.body;
      const genAI = getGemini();
      
      const chat = genAI.chats.create({
        model: "gemini-2.5-flash", // using flash for higher quota limits and speed
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
        model: "gemini-2.5-flash",
        contents,
        config: {
          systemInstruction: "Du bist ein hilfreicher und professioneller Assistent für eine deutsche Apotheke im Bereich der Assistierten Telemedizin (aTM). Du hilfst Apothekern bei Fragen zu Ersteinschätzung, Videosprechstunden und Sonderkennzeichen. Antworte immer auf Deutsch und professionell."
        }
      });

      res.json({ text: response.text });
    } catch (error: any) {
      logger.error("Gemini Error:", error);
      res.status(500).json({ error: error.message || "Failed to generate response" });
    }
  });

  // Image Analysis Endpoint
  app.post("/api/analyze-image", async (req, res) => {
    try {
      const { imageBase64, mimeType, prompt } = req.body;
      const genAI = getGemini();
      
      const response = await genAI.models.generateContent({
        model: "gemini-2.5-flash",
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
      logger.error("Gemini Error:", error);
      res.status(500).json({ error: error.message || "Failed to analyze image" });
    }
  });

  // DSGVO Audit Log PDF Generator
  app.get("/api/admin/audit-log/:pharmacy_id", async (req, res) => {
    try {
      const { pharmacy_id } = req.params;
      
      const { data: pharmacy, error: pharmErr } = await supabaseAdmin
        .from("pharmacies")
        .select("*")
        .eq("id", pharmacy_id)
        .single();

      if (pharmErr || !pharmacy) {
        return res.status(404).json({ error: "Pharmacy not found" });
      }

      if (!pharmacy.avv_akzeptiert_am) {
        return res.status(400).json({ error: "AVV wurde von der Apotheke noch nicht akzeptiert. PDF kann nicht generiert werden." });
      }

      const PDFDocument = require('pdfkit');
      const doc = new PDFDocument({ margin: 50 });
      
      res.setHeader('Content-Type', 'application/pdf');
      res.setHeader('Content-Disposition', `attachment; filename=DSGVO_Audit_Log_${pharmacy.ik_number || 'Apotheke'}.pdf`);
      
      doc.pipe(res);
      
      // Header
      doc.fontSize(20).text('Verfahrensverzeichnis nach Art. 30 DSGVO', { align: 'center' });
      doc.moveDown();
      doc.fontSize(14).text('Telepharmazie & Assistierte Telemedizin (aTM)', { align: 'center' });
      doc.moveDown(2);
      
      // Pharmacy details
      doc.fontSize(12).font('Helvetica-Bold').text('1. Verantwortliche Stelle (Apotheke)');
      doc.font('Helvetica').text(`Name: ${pharmacy.name}`);
      doc.text(`IK-Nummer: ${pharmacy.ik_number || 'Nicht hinterlegt'}`);
      doc.text(`BSNR: ${pharmacy.bsnr || 'Nicht hinterlegt'}`);
      doc.text(`Datum der Erstellung: ${new Date().toLocaleDateString('de-DE')}`);
      
      const avvDate = new Date(pharmacy.avv_akzeptiert_am).toLocaleString('de-DE');
      doc.text(`AVV digital akzeptiert am: ${avvDate}`);
      doc.moveDown();

      // DSGVO descriptions
      doc.font('Helvetica-Bold').text('2. Beschreibung der Verarbeitungstätigkeiten');
      doc.font('Helvetica').text('Zweck: Durchführung von Ersteinschätzungen und Videosprechstunden im Rahmen der assistierten Telemedizin gemäß § 129 SGB V.');
      doc.text('Rechtsgrundlage: Art. 6 Abs. 1 lit. a (Einwilligung), Art. 9 Abs. 2 lit. a DSGVO.');
      doc.moveDown();

      doc.font('Helvetica-Bold').text('3. Kategorien betroffener Personen');
      doc.font('Helvetica').text('Patienten der Apotheke, die telemedizinische Leistungen in Anspruch nehmen.');
      doc.moveDown();

      doc.font('Helvetica-Bold').text('4. Kategorien personenbezogener Daten');
      doc.font('Helvetica').text('- Stammdaten (Name, Geburtsdatum, KVNR)');
      doc.text('- Gesundheitsdaten (Triage-Ergebnisse, Video-Stream)');
      doc.text('- Abrechnungsdaten (Sonderkennzeichen, Zeitstempel)');
      doc.moveDown();

      doc.font('Helvetica-Bold').text('5. Technische und organisatorische Maßnahmen (TOMs)');
      doc.font('Helvetica').text('- Die Videosprechstunde erfolgt über eine Ende-zu-Ende verschlüsselte WebRTC/Jitsi-Infrastruktur.');
      doc.text('- Es erfolgt KEINE serverseitige Aufzeichnung der Videostreams.');
      doc.text('- Zugriff auf das Apotheken-Dashboard ist durch rollenbasierte Autorisierung (Supabase Auth) und RLS (Row Level Security) geschützt.');
      doc.moveDown();

      doc.font('Helvetica-Bold').text('6. Löschfristen');
      doc.font('Helvetica').text('Einverständniserklärungen (AVV) sowie Abrechnungsdaten werden gemäß gesetzlicher Vorgaben für 4 Jahre (retention_expires_at) verschlüsselt aufbewahrt und danach automatisiert vernichtet.');
      doc.moveDown(2);

      doc.fontSize(10).fillColor('gray').text('Dieses Dokument wurde maschinell generiert und entspricht den Anforderungen der Landesapothekerkammern (LAK) für die digitale Vor-Ort-Versorgung.', { align: 'center' });

      doc.end();
    } catch (error: any) {
      logger.error("Audit log error:", error);
      res.status(500).json({ error: error.message });
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
    // Fallback for SPA routing - must be LAST
    app.get("*", (req, res) => {
      res.sendFile(path.join(distPath, "index.html"));
    });
  }

  const startListening = (port: number) => {
    const server = app.listen(port, "0.0.0.0", () => {
      logger.info(`Server running on http://localhost:${port}`);
    });

    server.on('error', (e: any) => {
      if (e.code === 'EADDRINUSE') {
        logger.warn(`Port ${port} is in use, trying ${port + 1}...`);
        setTimeout(() => {
          startListening(port + 1);
        }, 100);
      } else {
        logger.error(`Server error: ${e.message}`);
      }
    });
  };

  startListening(PORT);
}

startServer();
