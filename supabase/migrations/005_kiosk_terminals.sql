-- Kiosk Terminals and Pairing Flow

CREATE TABLE IF NOT EXISTS kiosk_terminals (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    pharmacy_id UUID NOT NULL REFERENCES pharmacies(id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    device_token TEXT NOT NULL UNIQUE,
    status TEXT NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'revoked')),
    last_ping TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE IF NOT EXISTS pairing_codes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    pharmacy_id UUID NOT NULL REFERENCES pharmacies(id) ON DELETE CASCADE,
    code TEXT NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ DEFAULT now()
);

-- RLS for kiosk_terminals
ALTER TABLE kiosk_terminals ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Pharmacists can view their own terminals"
    ON kiosk_terminals FOR SELECT
    USING (auth.uid() = pharmacy_id);

CREATE POLICY "Pharmacists can insert their own terminals"
    ON kiosk_terminals FOR INSERT
    WITH CHECK (auth.uid() = pharmacy_id);

CREATE POLICY "Pharmacists can update their own terminals"
    ON kiosk_terminals FOR UPDATE
    USING (auth.uid() = pharmacy_id);

CREATE POLICY "Pharmacists can delete their own terminals"
    ON kiosk_terminals FOR DELETE
    USING (auth.uid() = pharmacy_id);

-- RLS for pairing_codes
ALTER TABLE pairing_codes ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Pharmacists can manage their own pairing codes"
    ON pairing_codes FOR ALL
    USING (auth.uid() = pharmacy_id);
