using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceApotheke.API.Models.ATM
{
    public class KioskTerminal
    {
        [Key]
        public int Id { get; set; }

        public int PharmacyId { get; set; }
        
        [ForeignKey("PharmacyId")]
        public Pharmacy Pharmacy { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [Required]
        [MaxLength(255)]
        public string DeviceToken { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "active"; // e.g. active, revoked

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
