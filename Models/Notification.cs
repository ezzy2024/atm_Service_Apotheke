using System.ComponentModel.DataAnnotations;

namespace ServiceApotheke.API.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty; // e.g., "Pharmacy_1" or "Pharmacist_5" to handle both types easily
        
        [Required]
        public string Role { get; set; } = string.Empty; // "Pharmacy" or "Pharmacist"
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        public string Type { get; set; } = "System"; // e.g. ShiftProposal, Invoice, System
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
