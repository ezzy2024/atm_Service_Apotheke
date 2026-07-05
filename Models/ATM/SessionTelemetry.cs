using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceApotheke.API.Models.ATM
{
    public class SessionTelemetry
    {
        [Key]
        public int Id { get; set; }

        public int PharmacyId { get; set; }

        [ForeignKey("PharmacyId")]
        public Pharmacy Pharmacy { get; set; }

        [Required]
        [MaxLength(100)]
        public string SessionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
