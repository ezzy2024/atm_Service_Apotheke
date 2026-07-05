using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ServiceApotheke.API.Models.ATM;
using ServiceApotheke.API.Models.PDL;

namespace ServiceApotheke.API.Models
{
    public class Pharmacy
    {
        public int Id { get; set; }
        [Required, MaxLength(150)]
        public string PharmacyName { get; set; } = string.Empty;
        [Required] public string Email { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;
        [Required] public string PhoneNumber { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;

        public bool IsEmailConfirmed { get; set; }
        public string? EmailConfirmationToken { get; set; }
        public bool IsVerified { get; set; } = false;
        
        [MaxLength(100)]
        public string? ApiKey { get; set; }
        
        // Neue Profilfelder
        public string? ContactPerson { get; set; }
        public string? SoftwareSystem { get; set; }
        public string? FocusAreas { get; set; }
        public string? StaffSupport { get; set; }
        public bool InvoiceBillingPossible { get; set; }
        public decimal? TargetHourlyRate { get; set; }
        public bool ParkingAvailable { get; set; }
        public string? AccommodationProvided { get; set; }

        // GDPR & B2B Compliance
        public DateTime? DataProcessingAgreementSignedAt { get; set; }
        public DateTime? GdprAnonymizedAt { get; set; }

        [JsonIgnore]
        public ICollection<TemperatureLog> TemperatureLogs { get; set; } = new List<TemperatureLog>();

        [JsonIgnore]
        public ICollection<Patient> Patients { get; set; } = new List<Patient>();

        [JsonIgnore]
        public ICollection<PdlDocument> PdlDocuments { get; set; } = new List<PdlDocument>();

        [JsonIgnore]
        public virtual ICollection<JobPost> JobPosts { get; set; } = new List<JobPost>();
    }
}