using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceApotheke.API.Models.ATM
{
    public class ConsentAgreement
    {
        [Key]
        public int Id { get; set; }

        public int PharmacyId { get; set; }

        [ForeignKey("PharmacyId")]
        public Pharmacy Pharmacy { get; set; }

        [Required]
        [MaxLength(255)]
        public string PatientName { get; set; }

        [Required]
        [MaxLength(255)]
        public string HealthInsuranceName { get; set; }

        [MaxLength(100)]
        public string HealthInsuranceNumber { get; set; }

        [MaxLength(100)]
        public string IkNumber { get; set; }

        public byte[] SignatureBlob { get; set; }

        public bool IsTelepharmacyConsentGranted { get; set; }

        public bool IsWwsExportGranted { get; set; }

        public DateTime SignedDate { get; set; } = DateTime.UtcNow;
    }
}
