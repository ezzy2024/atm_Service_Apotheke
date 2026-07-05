using System;
using System.Collections.Generic;

namespace ServiceApotheke.API.Models.PDL
{
    public class Patient
    {
        public int Id { get; set; }
        public int PharmacyId { get; set; }
        public Pharmacy Pharmacy { get; set; }
        
        // Pseudo-Anonymized or real data based on the source Excel
        public string KdnNr { get; set; }
        public string Name { get; set; }
        public string Vorname { get; set; }
        public string Geburt { get; set; } // z.B. "1950" or "01.01.1950"
        public string Gender { get; set; }

        public int MedicationCount { get; set; }
        public bool IsEligibleForAmts { get; set; }
        public string MedicationsJson { get; set; } = "[]";

        public ICollection<PdlService> PdlServices { get; set; }
        public ICollection<PdlDocument> PdlDocuments { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
