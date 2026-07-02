import dotenv from 'dotenv';
import { createClient } from '@supabase/supabase-js';

dotenv.config();

const supabaseUrl = process.env.VITE_SUPABASE_URL;
const supabaseKey = process.env.SUPABASE_SERVICE_ROLE_KEY;
const supabaseAdmin = createClient(supabaseUrl, supabaseKey);

async function checkPharmacies() {
  const { data, error } = await supabaseAdmin
    .from('pharmacies')
    .select('id, name, onboarding_status, is_approved');

  if (error) {
    console.error("Error fetching pharmacies:", error);
  } else {
    console.log("Pharmacies in DB:");
    console.log(data);
  }
}

checkPharmacies();
