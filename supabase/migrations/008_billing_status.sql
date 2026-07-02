-- Add status column to billing_records for Live Queue
ALTER TABLE public.billing_records 
ADD COLUMN IF NOT EXISTS status text DEFAULT 'waiting';

-- Update existing records to completed if they are old (optional cleanup)
UPDATE public.billing_records 
SET status = 'completed' 
WHERE status = 'waiting' AND created_at < NOW() - INTERVAL '1 day';
