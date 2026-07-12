using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceApotheke.API.Models
{
    public class SaturdayRotationTeam
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int PharmacyId { get; set; }

        [ForeignKey("PharmacyId")]
        public Pharmacy Pharmacy { get; set; }
        
        // Storing comma-separated Employee IDs or Pharmacist IDs for simplicity in this domain service
        public string PharmacistIds { get; set; } 
    }
}
