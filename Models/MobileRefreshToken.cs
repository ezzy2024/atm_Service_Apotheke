using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceApotheke.API.Models
{
    public class MobileRefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PharmacistId { get; set; }

        [ForeignKey("PharmacistId")]
        public Pharmacist Pharmacist { get; set; }

        [Required]
        [MaxLength(255)]
        public string TokenHash { get; set; }

        [Required]
        [MaxLength(100)]
        public string DeviceId { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? RevokedAt { get; set; }
        
        public bool IsActive => RevokedAt == null && DateTime.UtcNow <= ExpiresAt;
    }
}
