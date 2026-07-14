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
        
        // Stripe Connect fields
        public string? StripePaymentIntentId { get; set; }
        public string? StripeTransferId { get; set; }
        public string EscrowStatus { get; set; } = "Pending"; // Pending, Held, PayoutCompleted
        
        public string? RateNegotiatedBy { get; set; } // e.g. "Pharmacy", "Pharmacist"
        public DateTime? AcceptedAt { get; set; }
        
        public virtual Pharmacy? Pharmacy { get; set; }
        public virtual PharmacyEmployee? Employee { get; set; }
    }
}
