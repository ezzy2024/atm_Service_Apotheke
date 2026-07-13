using System;
using System.Collections.Generic;

namespace ServiceApotheke.API.Models.PDL
{
    public class Patient
    {
        public int Id { get; set; }
        public int PharmacyId { get; set; }
        public Pharmacy Pharmacy { get; set; }
        
        // E2EE Opaque Storage
        public string CiphertextBase64 { get; set; } = string.Empty;
        public string IvBase64 { get; set; } = string.Empty;

        public ICollection<PdlService> PdlServices { get; set; }
        public ICollection<PdlDocument> PdlDocuments { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
