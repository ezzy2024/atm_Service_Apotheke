import express from "express";
import path from "path";
import { createServer as createViteServer } from "vite";
import { GoogleGenAI, Type } from "@google/genai";

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

  // --- API Routes ---
  app.get("/api/health", (req, res) => {
    res.json({ status: "ok" });
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
