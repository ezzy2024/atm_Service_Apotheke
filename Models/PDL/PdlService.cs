using System;

namespace ServiceApotheke.API.Models.PDL
{
    public class PdlService
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; }

        // PLANNED, PERFORMED, BILLED
        public string Status { get; set; } = "PLANNED";
        
        // e.g. "POLYMEDIKATION", "BLUTDRUCK"
        public string ServiceType { get; set; }

        // JSON string of AI Analysis results
        public string AiAnalysisResultJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PerformedAt { get; set; }
        public DateTime? BilledAt { get; set; }
    }
}
