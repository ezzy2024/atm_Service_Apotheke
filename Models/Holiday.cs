using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceApotheke.API.Models
{
    public class Holiday
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        [MaxLength(2)]
        public string StateCode { get; set; } // e.g. "DE", "BY", "NW"
    }
}
