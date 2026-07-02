import { Client } from 'pg';
import fs from 'fs';
import path from 'path';
import dotenv from 'dotenv';
import { fileURLToPath } from 'url';

dotenv.config();

const DATABASE_URL = process.env.DATABASE_URL;

async function runMigration() {
  if (!DATABASE_URL) {
    console.error('DATABASE_URL not found in .env');
    process.exit(1);
  }

  const client = new Client({
    connectionString: DATABASE_URL,
    ssl: { rejectUnauthorized: false }
  });

  try {
    await client.connect();
    console.log('Connected to Supabase Postgres');

    const sqlPath = path.join(process.cwd(), 'supabase', 'migrations', '007_user_roles.sql');
    const sql = fs.readFileSync(sqlPath, 'utf8');

    await client.query(sql);
    console.log('Migration 007_user_roles.sql executed successfully.');
  } catch (err) {
    console.error('Migration failed:', err);
    process.exit(1);
  } finally {
    await client.end();
  }
}

runMigration();
