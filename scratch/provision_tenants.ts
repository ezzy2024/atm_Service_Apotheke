import { createClient } from "@supabase/supabase-js";
import QRCode from "qrcode";
import fs from "fs";
import path from "path";

// Initialize Supabase Admin Client
const supabaseUrl = process.env.VITE_SUPABASE_URL || "http://127.0.0.1:54321";
const supabaseServiceKey = process.env.VITE_SUPABASE_SERVICE_ROLE_KEY || "dummy_service_key";
const supabaseAdmin = createClient(supabaseUrl, supabaseServiceKey);

const BASE_URL = process.env.VITE_APP_URL || "https://atm.cloud";

async function provisionTenants() {
  console.log("=== STARTING TENANT PROVISIONING ===");
  
  // 1. Fetch all active pilot pharmacies
  const { data: pharmacies, error: pErr } = await supabaseAdmin
    .from("pharmacies")
    .select("id, name")
    .eq("status", "active");

  if (pErr || !pharmacies) {
    console.error("Failed to fetch pharmacies:", pErr);
    return;
  }
  
  console.log(`Found ${pharmacies.length} active pilot pharmacies.`);

  const qrDir = path.join(process.cwd(), "marketing", "qr_codes");
  if (!fs.existsSync(qrDir)) {
    fs.mkdirSync(qrDir, { recursive: true });
  }

  // 2. Map Users and Generate QR Codes
  for (const pharmacy of pharmacies) {
    console.log(`\nProvisioning Pharmacy: ${pharmacy.name} (${pharmacy.id})`);
    
    // Generate QR Code URL
    const patientUrl = `${BASE_URL}/patient/start?pharmacy_id=${pharmacy.id}`;
    const qrPath = path.join(qrDir, `${pharmacy.name.replace(/\s+/g, '_')}_QR.png`);
    
    await QRCode.toFile(qrPath, patientUrl, {
      color: { dark: "#000000", light: "#ffffff" },
      width: 500,
      margin: 2
    });
    console.log(`  -> Generated Patient QR Code: ${qrPath}`);
    
    // In a real environment, we would lookup the owner user by email, but we log the instructions here
    console.log(`  -> TO DO: Ensure auth.users mapping exists in user_pharmacy_roles for pharmacy_id: ${pharmacy.id}`);
  }

  console.log("\n=== TENANT PROVISIONING COMPLETE ===");
}

provisionTenants().catch(console.error);
