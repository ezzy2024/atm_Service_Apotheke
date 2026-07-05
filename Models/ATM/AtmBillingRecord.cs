using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceApotheke.API.Models.ATM
{
    public class AtmBillingRecord
    {
        [Key]
        public int Id { get; set; }

        public int PharmacyId { get; set; }

        [ForeignKey("PharmacyId")]
        public Pharmacy Pharmacy { get; set; }

        public int ConsentId { get; set; }

        [ForeignKey("ConsentId")]
        public ConsentAgreement ConsentAgreement { get; set; }

        [Required]
        [MaxLength(100)]
        public string ServiceType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime DateOfService { get; set; }

        [MaxLength(50)]
        public string Sonderkennzeichen { get; set; }

        [MaxLength(500)]
        public string ReportPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
