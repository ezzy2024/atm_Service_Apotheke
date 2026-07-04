using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ServiceApotheke.API.Models
{
    public class PharmacistFeedback
    {
        public int Id { get; set; }

        public int JobPostId { get; set; }
        [JsonIgnore]
        public virtual JobPost JobPost { get; set; } = null!;

        public int PharmacistId { get; set; }
        [JsonIgnore]
        public virtual Pharmacist Pharmacist { get; set; } = null!;

        public DateTime? ActualStart { get; set; }
        public DateTime? ActualEnd { get; set; }
        public int? ActualPauseMinutes { get; set; }

        public string? WorkloadLevel { get; set; }
        public string? RelevantAreas { get; set; }
        public string? CriticalIncidents { get; set; }

        [Range(1, 5)]
        public int? OrganizationRating { get; set; }
        [Range(1, 5)]
        public int? SupportRating { get; set; }
        [Range(1, 5)]
        public int? WorkspacePrepRating { get; set; }
        [Range(1, 5)]
        public int? BtmComplianceRating { get; set; }
        [Range(1, 5)]
        public int? PrivacyRating { get; set; }

        [Range(1, 5)]
        public int? OverallRating { get; set; }

        public string? WouldWorkAgain { get; set; }
        public string? Positives { get; set; }
        public string? Improvements { get; set; }

        public bool TimesheetConfirmed { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}