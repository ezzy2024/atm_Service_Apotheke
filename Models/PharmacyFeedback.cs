using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ServiceApotheke.API.Models
{
    public class PharmacyFeedback
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

        [Range(1, 5)]
        public int? CompetenceRating { get; set; }
        [Range(1, 5)]
        public int? IndependenceRating { get; set; }
        [Range(1, 5)]
        public int? AccuracyRating { get; set; }
        [Range(1, 5)]
        public int? StressManagementRating { get; set; }
        [Range(1, 5)]
        public int? TeamworkRating { get; set; }
        [Range(1, 5)]
        public int? CommunicationRating { get; set; }

        public string? Punctuality { get; set; }
        public string? OnboardingRequired { get; set; }
        public string? OverallGrade { get; set; }

        public string? WouldHireAgain { get; set; }
        public string? Positives { get; set; }
        public string? Improvements { get; set; }
        public string? NextDemand { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}