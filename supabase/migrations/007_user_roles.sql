-- Migration: 007_user_roles.sql
-- Architecture Pivot: Software-Only BYOD

-- 1. Create Junction Table for Multi-Tenant Access Control
CREATE TABLE IF NOT EXISTS user_pharmacy_roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    pharmacy_id UUID NOT NULL REFERENCES pharmacies(id) ON DELETE CASCADE,
    role VARCHAR(50) NOT NULL CHECK (role IN ('admin', 'staff', 'external_pharmacist')),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE (user_id, pharmacy_id)
);

-- 2. Create B-Tree Indexes for Performance
CREATE INDEX IF NOT EXISTS idx_user_pharmacy_roles_user_id ON user_pharmacy_roles USING btree (user_id);
CREATE INDEX IF NOT EXISTS idx_user_pharmacy_roles_pharmacy_id ON user_pharmacy_roles USING btree (pharmacy_id);

-- 3. Security Definer Function for RLS
-- This function allows RLS policies to quickly check if the auth.uid() has access to a specific pharmacy_id.
CREATE OR REPLACE FUNCTION public.has_pharmacy_access(target_pharmacy_id UUID)
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1
        FROM user_pharmacy_roles
        WHERE user_id = auth.uid()
          AND pharmacy_id = target_pharmacy_id
    );
END;
$$;

-- 4. Enable RLS on user_pharmacy_roles
ALTER TABLE user_pharmacy_roles ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view their own roles"
ON user_pharmacy_roles FOR SELECT
USING (user_id = auth.uid());

CREATE POLICY "Admins can manage roles for their pharmacies"
ON user_pharmacy_roles FOR ALL
USING (
    EXISTS (
        SELECT 1 FROM user_pharmacy_roles upr
        WHERE upr.user_id = auth.uid()
          AND upr.pharmacy_id = user_pharmacy_roles.pharmacy_id
          AND upr.role = 'admin'
    )
);

-- 5. Update RLS Policies for Core Tables
-- Replace previous kiosk device_token dependencies with the new user roles system.

-- Consent Agreements
DROP POLICY IF EXISTS "Enable read access for authenticated users" ON consent_agreements;
CREATE POLICY "Users can read consents for their pharmacies"
ON consent_agreements FOR SELECT
USING (public.has_pharmacy_access(pharmacy_id));

-- Billing Records
DROP POLICY IF EXISTS "Enable read access for authenticated users" ON billing_records;
CREATE POLICY "Users can read billing records for their pharmacies"
ON billing_records FOR SELECT
USING (public.has_pharmacy_access(pharmacy_id));

-- Appointments
DROP POLICY IF EXISTS "Enable read access for authenticated users" ON appointments;
CREATE POLICY "Users can read appointments for their pharmacies"
ON appointments FOR SELECT
USING (public.has_pharmacy_access(pharmacy_id));

-- Pharmacies
DROP POLICY IF EXISTS "Enable read access for authenticated users" ON pharmacies;
CREATE POLICY "Users can read pharmacy details they have access to"
ON pharmacies FOR SELECT
USING (public.has_pharmacy_access(id));
