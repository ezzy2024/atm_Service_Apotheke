-- Database Schema (Supabase PostgreSQL) for Service Apotheke aTM

-- Drop existing tables if they exist
DROP TABLE IF EXISTS billing_records CASCADE;
DROP TABLE IF EXISTS consent_agreements CASCADE;
DROP TABLE IF EXISTS appointments CASCADE;
DROP TABLE IF EXISTS profiles CASCADE;
DROP TABLE IF EXISTS pharmacies CASCADE;

-- Enum for service type
DO $$ BEGIN
    CREATE TYPE service_type AS ENUM ('triage_only', 'video_only', 'triage_and_video');
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

DO $$ BEGIN
    CREATE TYPE app_role AS ENUM ('super_admin', 'pharmacy_admin', 'pharmacist');
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

DO $$ BEGIN
    CREATE TYPE pharmacy_status AS ENUM ('pending', 'active', 'suspended');
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

-- 1. Pharmacies Table
CREATE TABLE pharmacies (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    ik_nummer VARCHAR(9) NOT NULL,
    bsnr VARCHAR(9) NOT NULL DEFAULT '000000000',
    is_approved BOOLEAN DEFAULT false,
    oeffnungszeiten JSONB DEFAULT '{"mo": "08:00-18:00", "tu": "08:00-18:00", "we": "08:00-18:00", "th": "08:00-18:00", "fr": "08:00-18:00"}',
    telefon VARCHAR(50),
    ansprechpartner VARCHAR(255),
    status pharmacy_status DEFAULT 'pending',
    avv_signed_at TIMESTAMPTZ,
    avv_akzeptiert_am TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 1.5 Profiles Table
CREATE TABLE profiles (
    id UUID PRIMARY KEY, -- In Supabase this references auth.users(id)
    role app_role NOT NULL DEFAULT 'pharmacist',
    pharmacy_id UUID REFERENCES pharmacies(id) ON DELETE CASCADE,
    full_name VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 2. Appointments Table
CREATE TABLE appointments (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    pharmacy_id UUID REFERENCES pharmacies(id) ON DELETE CASCADE,
    patient_id UUID,
    patient_name VARCHAR(255) NOT NULL,
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'scheduled', -- scheduled, completed, cancelled
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 3. Consent Agreements Table
CREATE TABLE consent_agreements (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    pharmacy_id UUID REFERENCES pharmacies(id) ON DELETE CASCADE,
    patient_name VARCHAR(255) NOT NULL,
    health_insurance_name TEXT NOT NULL,
    health_insurance_number VARCHAR(10) NOT NULL, -- KVNR: Exact 10 chars
    ik_number VARCHAR(9) NOT NULL, -- Kostenträgerkennung
    birth_date DATE NOT NULL,
    status_field VARCHAR(7) NOT NULL, -- First 5 from user + '83'
    signed_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    signature_blob TEXT NOT NULL,
    retention_expires_at TIMESTAMPTZ NOT NULL DEFAULT NOW() + INTERVAL '4 years',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

COMMENT ON COLUMN consent_agreements.retention_expires_at IS 'Must be retained for 4 years';

-- 4. Billing Records Table
CREATE TABLE billing_records (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    pharmacy_id UUID REFERENCES pharmacies(id) ON DELETE CASCADE,
    consent_id UUID REFERENCES consent_agreements(id) ON DELETE CASCADE,
    service_type service_type NOT NULL,
    amount NUMERIC(10, 2) NOT NULL,
    date_of_service DATE NOT NULL,
    sonderkennzeichen VARCHAR(20) NOT NULL,
    executed_by_pharmacist_name VARCHAR(255),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Trigger to automatically assign amount and sonderkennzeichen based on service_type and date
CREATE OR REPLACE FUNCTION apply_billing_rules()
RETURNS TRIGGER AS $$
BEGIN
    -- Assign Sonderkennzeichen
    IF NEW.service_type = 'triage_only' THEN
        NEW.sonderkennzeichen := '19816313';
    ELSIF NEW.service_type = 'video_only' THEN
        NEW.sonderkennzeichen := '19816336';
    ELSIF NEW.service_type = 'triage_and_video' THEN
        NEW.sonderkennzeichen := '19816342';
    END IF;

    -- Assign amount based on date (before July 1, 2027)
    IF NEW.date_of_service < '2027-07-01' THEN
        NEW.amount := 30.00;
    -- Add future pricing logic here if needed
    ELSE
        NEW.amount := 25.50; -- Next tier
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER billing_records_before_insert
BEFORE INSERT ON billing_records
FOR EACH ROW
EXECUTE FUNCTION apply_billing_rules();

-- RLS POLICIES --

-- Enable RLS on all tables
ALTER TABLE pharmacies ENABLE ROW LEVEL SECURITY;
ALTER TABLE appointments ENABLE ROW LEVEL SECURITY;
ALTER TABLE consent_agreements ENABLE ROW LEVEL SECURITY;
ALTER TABLE billing_records ENABLE ROW LEVEL SECURITY;



-- Assuming auth.uid() maps to a user who belongs to a pharmacy, or the token has a claim
-- Let's define helper functions
CREATE OR REPLACE FUNCTION auth_user_role() RETURNS app_role AS $$
  SELECT role FROM profiles WHERE id = auth.uid();
$$ LANGUAGE SQL STABLE;

CREATE OR REPLACE FUNCTION auth_pharmacy_id() RETURNS UUID AS $$
  SELECT COALESCE(
    nullif(current_setting('request.jwt.claims', true)::json->'app_metadata'->>'pharmacy_id', '')::uuid,
    nullif(current_setting('request.jwt.claims', true)::json->'user_metadata'->>'pharmacy_id', '')::uuid
  );
$$ LANGUAGE SQL STABLE;

-- Profiles
ALTER TABLE profiles ENABLE ROW LEVEL SECURITY;
CREATE POLICY "Super admin can read all profiles" ON profiles FOR SELECT TO authenticated USING (auth_user_role() = 'super_admin');
CREATE POLICY "Pharmacy admins can read their pharmacy profiles" ON profiles FOR SELECT TO authenticated USING (pharmacy_id = auth_pharmacy_id() AND auth_user_role() = 'pharmacy_admin');
CREATE POLICY "Users can read own profile" ON profiles FOR SELECT TO authenticated USING (id = auth.uid());

-- Pharmacies policies
CREATE POLICY "Super admin can do anything with pharmacies" ON pharmacies FOR ALL TO authenticated USING (auth_user_role() = 'super_admin');
CREATE POLICY "Users can view their own pharmacy" ON pharmacies
    FOR SELECT TO authenticated
    USING (id = auth_pharmacy_id());
CREATE POLICY "Pharmacy admin can update own pharmacy" ON pharmacies
    FOR UPDATE TO authenticated
    USING (id = auth_pharmacy_id() AND auth_user_role() = 'pharmacy_admin');

-- Appointments
DROP POLICY IF EXISTS "Pharmacist_Appointments" ON appointments;
CREATE POLICY "Pharmacist_Appointments" ON appointments
  FOR ALL TO authenticated
  USING (pharmacy_id = auth_pharmacy_id())
  WITH CHECK (pharmacy_id = auth_pharmacy_id());

-- Consent Agreements
DROP POLICY IF EXISTS "Pharmacist_Consent" ON consent_agreements;
CREATE POLICY "Pharmacist_Consent" ON consent_agreements
  FOR ALL TO authenticated
  USING (pharmacy_id = auth_pharmacy_id())
  WITH CHECK (pharmacy_id = auth_pharmacy_id());

-- Billing Records
DROP POLICY IF EXISTS "Pharmacist_Billing" ON billing_records;
CREATE POLICY "Pharmacist_Billing" ON billing_records
  FOR ALL TO authenticated
  USING (consent_id IN (SELECT id FROM consent_agreements WHERE pharmacy_id = auth_pharmacy_id()))
  WITH CHECK (consent_id IN (SELECT id FROM consent_agreements WHERE pharmacy_id = auth_pharmacy_id()));

-- Notification Triggers Placeholder (Using logical functions since pg_net might not be enabled)
CREATE OR REPLACE FUNCTION notify_superadmin_new_pharmacy() RETURNS TRIGGER AS $$
BEGIN
  -- HTTP POST to internal API or Email service
  -- e.g., perform pg_net.http_post(...)
  RAISE LOG 'New pharmacy registered: %', NEW.name;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER on_pharmacy_registered
AFTER INSERT ON pharmacies
FOR EACH ROW EXECUTE FUNCTION notify_superadmin_new_pharmacy();

CREATE OR REPLACE FUNCTION notify_pharmacy_approval() RETURNS TRIGGER AS $$
BEGIN
  IF NEW.is_approved = true AND OLD.is_approved = false THEN
    RAISE LOG 'Pharmacy approved, AVV ready: %', NEW.name;
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER on_pharmacy_approved
AFTER UPDATE ON pharmacies
FOR EACH ROW EXECUTE FUNCTION notify_pharmacy_approval();

-- 5. Seed default demo pharmacy for local testing and demo logins
INSERT INTO pharmacies (id, name, ik_nummer, bsnr, is_approved, status)
VALUES ('d3b07384-d113-4956-a50e-a1c563e4410a', 'Demo-Apotheke', '123456789', '000000000', true, 'active')
ON CONFLICT (id) DO NOTHING;

-- 6. Add report_path column to billing_records table (for clinical reports)
ALTER TABLE billing_records ADD COLUMN IF NOT EXISTS report_path TEXT;

-- 7. Storage Bucket RLS Policies for Clinical Reports
DROP POLICY IF EXISTS "Apotheken-Mitarbeiter duerfen nur eigene Berichte lesen" ON storage.objects;
CREATE POLICY "Apotheken-Mitarbeiter duerfen nur eigene Berichte lesen"
ON storage.objects FOR SELECT TO authenticated
USING (
  bucket_id = 'clinical-reports' 
  AND split_part(name, '/', 2) = (auth_pharmacy_id())::text
);



