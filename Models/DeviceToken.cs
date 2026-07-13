using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceApotheke.API.Models
{
    public class DeviceToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PharmacistId { get; set; }

        [ForeignKey("PharmacistId")]
        public Pharmacist Pharmacist { get; set; }

        [Required]
        [MaxLength(255)]
        public string FcmToken { get; set; }

        [Required]
        [MaxLength(20)]
        public string DevicePlatform { get; set; } // e.g. "iOS", "Android"

        [Required]
        [MaxLength(100)]
        public string DeviceId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime LastActive { get; set; }
    }
}
