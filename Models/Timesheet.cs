using System;

namespace ServiceApotheke.API.Models
{
    public class Timesheet
    {
        public int Id { get; set; }
        public int JobApplicationId { get; set; }
        public JobApplication? JobApplication { get; set; }
        
        public DateTime ActualStartDate { get; set; }
        public TimeSpan ActualStartTime { get; set; }
        public TimeSpan ActualEndTime { get; set; }
        public int BreaksMinutes { get; set; } = 0;
        public decimal HourlyRate { get; set; }
        
        public decimal TravelCosts { get; set; }
        public decimal AccommodationCosts { get; set; }
        
        // Status: "Submitted" (Eingereicht), "Approved" (Freigegeben), "Disputed" (Konflikt)
        public string Status { get; set; } = "Submitted";
        
        public string? DisputeReason { get; set; }
        public DateTime? DisputedAt { get; set; }
        
        public string? TimesheetPath { get; set; }
        public string? DigitalSignatureHash { get; set; }
    }
}