-- Füge AVV-Tracking-Spalten zur pharmacies Tabelle hinzu
ALTER TABLE pharmacies ADD COLUMN IF NOT EXISTS avv_akzeptiert_am TIMESTAMPTZ;

-- Hinweis: Die RLS-Policies der Tabelle "pharmacies" greifen automatisch auch auf diese neue Spalte. 
-- Da die Tabelle in der initialen Migration mit "ENABLE ROW LEVEL SECURITY" angelegt wurde und 
-- Policies für "update" (durch den Eigentümer) existieren, kann kein anderer Tenant diese Spalte verändern.
