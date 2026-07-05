using System;

namespace ServiceApotheke.API.Models.PDL
{
    public class PdlDocument
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; }
        
        public int PharmacyId { get; set; }
        public Pharmacy Pharmacy { get; set; }
        
        public int PdlServiceId { get; set; }
        public PdlService PdlService { get; set; }

        public string PdfUrl { get; set; }
        public decimal BillingAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
