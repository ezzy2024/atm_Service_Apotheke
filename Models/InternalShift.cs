using System;

namespace ServiceApotheke.API.Models
{
    public class InternalShift
    {
        public int Id { get; set; }
        public int PharmacyId { get; set; }
        public int PharmacyEmployeeId { get; set; }
        
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        
        public bool IsEmergencyDuty { get; set; } = false; // Notdienst flag
        
        public virtual Pharmacy? Pharmacy { get; set; }
        public virtual PharmacyEmployee? Employee { get; set; }
    }
}
