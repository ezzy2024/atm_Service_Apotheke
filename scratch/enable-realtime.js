import dotenv from 'dotenv';
import { createClient } from '@supabase/supabase-js';

dotenv.config();

const supabaseUrl = process.env.VITE_SUPABASE_URL;
const supabaseKey = process.env.SUPABASE_SERVICE_ROLE_KEY;
const supabaseAdmin = createClient(supabaseUrl, supabaseKey);

async function enableRealtime() {
  const { error } = await supabaseAdmin.rpc('exec_sql', {
    query: `
      alter publication supabase_realtime add table billing_records;
    `
  });
  if (error) {
    console.error("Error enabling realtime:", error);
  } else {
    console.log("Realtime successfully enabled for billing_records!");
  }
}

enableRealtime();
