-- 009_storage_policies.sql
-- Configure Storage Bucket and RLS Policies for billing_documentation

-- Ensure the bucket exists and is private
INSERT INTO storage.buckets (id, name, public) 
VALUES ('billing_documentation', 'billing_documentation', false)
ON CONFLICT (id) DO UPDATE SET public = false;

-- Enable RLS on the storage.objects table if not already enabled
ALTER TABLE storage.objects ENABLE ROW LEVEL SECURITY;

-- Policy: Only authenticated users (Pharmacy Staff/Admins) can READ documents in their pharmacy folder
CREATE POLICY "Authenticated users can read billing documentation"
ON storage.objects FOR SELECT
TO authenticated
USING (
  bucket_id = 'billing_documentation' AND 
  (storage.foldername(name))[1] IN (
    SELECT pharmacy_id::text 
    FROM public.user_pharmacy_roles 
    WHERE user_id = auth.uid()
  )
);

-- Policy: Only Service Role can INSERT/UPDATE documents (Server-side PDF generation)
CREATE POLICY "Service Role can insert billing documentation"
ON storage.objects FOR INSERT
TO service_role
WITH CHECK (bucket_id = 'billing_documentation');

CREATE POLICY "Service Role can update billing documentation"
ON storage.objects FOR UPDATE
TO service_role
USING (bucket_id = 'billing_documentation');
