using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceApotheke.API.Models
{
    public class SaturdayRotation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        [Required]
        public int TeamId { get; set; }

        public int PharmacyId { get; set; }
        
        [ForeignKey("PharmacyId")]
        public Pharmacy Pharmacy { get; set; }

        [ForeignKey("TeamId")]
        public SaturdayRotationTeam Team { get; set; }
    }
}
