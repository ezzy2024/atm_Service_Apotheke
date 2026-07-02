import fs from 'fs';
import path from 'path';
import dotenv from 'dotenv';

// Lade Umgebungsvariablen
dotenv.config();

const MANUS_API_KEY = process.env.MANUS_API_KEY;
const BASE_URL = 'https://api.manus.ai';

const INITIAL_PROMPT = `
Du bist ein Lead Enterprise Software Architect und Security Auditor. Deine Aufgabe ist es, die Codebase des Projekts 'Service Apotheke aTM' (Assistierte Telemedizin) tiefgehend zu analysieren, zu testen und eine Strategie für die Weiterentwicklung zu einer hochskalierbaren, mandantenfähigen (Multi-Tenant) SaaS-Applikation auszuarbeiten.

Kontext des Projekts:
Es handelt sich um ein Kiosk-System für deutsche Apotheken. Patienten können an einem Terminal in der Apotheke eine medizinische Ersteinschätzung (basierend auf SmED) durchführen und bei Bedarf direkt aus der Apotheke eine datenschutzkonforme Videosprechstunde (Jitsi) mit einem Apotheker/Arzt starten.

Der Quellcode liegt öffentlich auf GitHub: https://github.com/ezzy2024/atm_Service_Apotheke

Aktueller Status:
Das Kiosk-MVP, das B2B-Onboarding (inkl. DSGVO-AVV-Upload) und das Admin-Dashboard funktionieren.

Dein Ziel / Deine Aufgaben:
1. Code-Analyse: Bitte analysiere das GitHub Repository auf Schwachstellen (Security, Performance, Clean Code).
2. SaaS & Multi-Tenancy: Erstelle eine Strategie, wie die App in ein voll mandantenfähiges SaaS-Produkt umgebaut wird (Trennung von Apotheken-Daten, Subscriptions, verschiedene Berechtigungsstufen).
3. Compactness & Refactoring: Identifiziere redundanten Code und schlage Refactorings vor.

Bitte beginne mit der Analyse des Repositories und präsentiere dann deine strategischen Erkenntnisse.
`;

async function manusFetch(endpoint, options = {}) {
  const url = `${BASE_URL}${endpoint}`;
  const headers = {
    'x-manus-api-key': MANUS_API_KEY,
    'Content-Type': 'application/json',
    ...(options.headers || {})
  };

  const response = await fetch(url, { ...options, headers });
  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`Manus API Error ${response.status}: ${errorText}`);
  }
  return response.json();
}

async function startTask() {
  if (!MANUS_API_KEY) {
    console.error("❌ MANUS_API_KEY fehlt in der .env Datei!");
    console.error("Bitte fügen Sie MANUS_API_KEY=Ihr_Key in die .env Datei ein.");
    process.exit(1);
  }

  console.log("🚀 Starte neue Aufgabe bei Manus...");
  
  try {
    // Nutzt task.create Endpunkt (V2 Konvention RPC style)
    const taskData = await manusFetch('/v2/task.create', {
      method: 'POST',
      body: JSON.stringify({
        message: { content: INITIAL_PROMPT }
      })
    });

    console.log("✅ Manus Aufgabe erfolgreich erstellt!");
    console.log("Task Data:", JSON.stringify(taskData, null, 2));
    const taskId = taskData.id || taskData.taskId || (taskData.data && taskData.data.id) || (taskData.task && taskData.task.id);
    
    fs.writeFileSync('.manus-task', JSON.stringify(taskData, null, 2));
    console.log("Task-ID wurde in '.manus-task' gespeichert.");
    console.log("\nUm den Status abzufragen, starten Sie:");
    console.log("node scratch/manus-bridge.js status");
    
  } catch (error) {
    console.error("Fehler bei der Kommunikation mit Manus:", error.message);
  }
}

async function checkStatus() {
  if (!fs.existsSync('.manus-task')) {
    console.error("❌ Keine laufende Aufgabe gefunden. Bitte starten Sie erst eine Aufgabe.");
    return;
  }
  
  const taskMeta = JSON.parse(fs.readFileSync('.manus-task', 'utf8'));
  const taskId = taskMeta.task_id;
  
  console.log(`🔍 Prüfe Nachrichten für Task ${taskId}...`);
  
  try {
    const messages = await manusFetch(`/v2/task.listMessages?task_id=${taskId}`, {
      method: 'GET'
    });
    
    console.log("\n--- Letzte Nachrichten von Manus ---\n");
    const msgs = messages.messages || [];
    msgs.forEach(msg => {
      if (msg.type === 'assistant_message' && msg.assistant_message) {
        console.log(`[MANUS]:\n${msg.assistant_message.content}\n`);
      } else if (msg.type === 'user_message' && msg.user_message) {
        console.log(`[USER]:\n${msg.user_message.content}\n`);
      } else if (msg.type === 'status_update' && msg.status_update) {
        console.log(`[STATUS]: ${msg.status_update.content}\n`);
      }
    });
    
  } catch (error) {
    console.error("Fehler beim Abrufen des Status:", error.message);
  }
}

// Einfaches CLI-Routing
const command = process.argv[2];
if (command === 'status') {
  checkStatus();
} else {
  startTask();
}
