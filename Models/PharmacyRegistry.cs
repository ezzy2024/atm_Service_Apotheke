using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceApotheke.API.Models
{
    [Table("pharmacy_registry")]
    public class PharmacyRegistry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Street { get; set; } = string.Empty;

        [MaxLength(20)]
        public string PLZ { get; set; } = string.Empty;

        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
    }
}
