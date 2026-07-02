import dotenv from 'dotenv';
import { createClient } from '@supabase/supabase-js';

dotenv.config();

const supabaseUrl = process.env.VITE_SUPABASE_URL;
const supabaseKey = process.env.SUPABASE_SERVICE_ROLE_KEY;
const supabaseAdmin = createClient(supabaseUrl, supabaseKey);

async function approvePharmacies() {
  const { data, error } = await supabaseAdmin
    .from('pharmacies')
    .update({ 
      onboarding_status: 'active',
      is_approved: true,
      status: 'active'
    })
    .neq('onboarding_status', 'active'); // Update all that are NOT active yet

  if (error) {
    console.error("Fehler bei der Freischaltung:", error);
  } else {
    console.log("Alle restlichen Apotheken wurden erfolgreich freigeschaltet!");
  }
}

approvePharmacies();
