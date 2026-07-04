using System;

namespace ServiceApotheke.API.Models
{
    public class JobPost
    {
        public int Id { get; set; }
        public int PharmacyId { get; set; }
        
        // --- Job Content ---
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? RequiredQualifications { get; set; }
        public string? RequiredWws { get; set; }
        public string? ReasonForVacancy { get; set; }
        public string? ShiftDetails { get; set; }
        
        // --- Temporal ---
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        
        // --- Business ---
        public decimal? Salary { get; set; }
        public string? Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- Relationships ---
        public virtual Pharmacy? Pharmacy { get; set; }
        public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}