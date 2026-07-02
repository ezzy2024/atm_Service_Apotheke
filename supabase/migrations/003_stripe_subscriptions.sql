-- Erweiterung pharmacies
ALTER TABLE pharmacies ADD COLUMN IF NOT EXISTS stripe_customer_id TEXT;
ALTER TABLE pharmacies ADD COLUMN IF NOT EXISTS subscription_status TEXT DEFAULT 'inactive';

-- Neue Tabelle für Subscriptions
CREATE TABLE IF NOT EXISTS subscriptions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  pharmacy_id UUID REFERENCES pharmacies(id) ON DELETE CASCADE,
  stripe_subscription_id TEXT UNIQUE NOT NULL,
  stripe_price_id TEXT NOT NULL,
  status TEXT NOT NULL, -- 'active', 'past_due', 'canceled'
  current_period_end TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now()
);

-- RLS for subscriptions
ALTER TABLE subscriptions ENABLE ROW LEVEL SECURITY;

-- Allow read access for authenticated users to their own subscriptions
CREATE POLICY "Enable read access for authenticated users" 
ON subscriptions FOR SELECT 
TO authenticated 
USING (pharmacy_id = auth.uid());

-- Allow service role full access
CREATE POLICY "Enable full access for service role"
ON subscriptions FOR ALL
TO service_role
USING (true)
WITH CHECK (true);
