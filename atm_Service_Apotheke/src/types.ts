export type ServiceType = "triage_only" | "video_only" | "triage_and_video";

export type AppRole = "super_admin" | "pharmacy_admin" | "pharmacist";

export interface Profile {
  id: string;
  role: AppRole;
  pharmacy_id: string | null;
  full_name: string;
  created_at?: string;
}

export interface Pharmacy {
  id: string;
  name: string;
  ik_nummer: string;
  bsnr: string;
  is_approved?: boolean;
  oeffnungszeiten?: any;
  telefon?: string;
  ansprechpartner?: string;
  status?: "pending" | "active" | "suspended";
  avv_signed_at?: string | null;
  avv_akzeptiert_am?: string | null;
  created_at?: string;
}

export interface Appointment {
  id: string;
  pharmacy_id?: string;
  patient_id?: string;
  patient_name: string;
  start_time: string;
  end_time: string;
  status: "scheduled" | "completed" | "cancelled" | "in-progress";
  created_at?: string;
}

export interface ConsentAgreement {
  id: string;
  pharmacy_id?: string;
  patient_name: string;
  health_insurance_name: string;
  health_insurance_number: string;
  ik_number: string;
  birth_date: string;
  status_field: string;
  signed_date: string;
  signature_blob: string;
  retention_expires_at?: string;
  created_at?: string;
}

export interface BillingRecord {
  id: string;
  pharmacy_id?: string;
  consent_id: string;
  service_type: ServiceType;
  amount: number;
  date_of_service: string;
  sonderkennzeichen: string;
  executed_by_pharmacist_name?: string;
  created_at?: string;
  consent_agreements?: ConsentAgreement;
}
