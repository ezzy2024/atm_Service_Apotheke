using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ServiceApotheke.API.Models
{
    public class TemperatureLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PharmacyId { get; set; }

        [Required]
        public double Temperature { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        public bool IsAnomaly { get; set; }

        [JsonIgnore]
        [ForeignKey("PharmacyId")]
        public virtual Pharmacy? Pharmacy { get; set; }
    }
}
