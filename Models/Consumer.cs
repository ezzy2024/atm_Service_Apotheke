using System;
using System.ComponentModel.DataAnnotations;

namespace ServiceApotheke.API.Models
{
    public class Consumer
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [MaxLength(100)]
        public string FirstName { get; set; }

        [MaxLength(100)]
        public string LastName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool HasAcceptedBgbWaiver { get; set; }
        public DateTime? BgbWaiverAcceptedAt { get; set; }
    }
}
