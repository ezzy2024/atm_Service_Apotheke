using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ServiceApotheke.API.Models.ATM;
using ServiceApotheke.API.Models.PDL;

namespace ServiceApotheke.API.Models
{
    public class Pharmacy
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Required, MaxLength(150)]
        public string PharmacyName { get; set; } = string.Empty;
        [Required] public string Email { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;
        public int SessionVersion { get; set; } = 1;
        [Required] public string PhoneNumber { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string LicenseNumber { get; set; } = string.Empty;

        public bool IsEmailConfirmed { get; set; }

        // Stripe Subscription
        public string? StripeCustomerId { get; set; }
        public string SubscriptionTier { get; set; } = "Free"; // Free, Pro, Enterprise
        public string? EmailConfirmationToken { get; set; }
        public DateTime? EmailConfirmationTokenExpiry { get; set; }
        
        public bool IsVerified { get; set; } = false;
        public string FreelanceContractStatus { get; set; } = "Pending"; // Pending, Active, Expired
        public string UstIdValidationStatus { get; set; } = "Pending"; // Pending, Valid, Invalid
        public string? UstIdNr { get; set; }
        public string? BetriebserlaubnisNumber { get; set; }
        public VerificationStatus Status { get; set; } = VerificationStatus.Unverified;
        
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
        public string? FreelanceContractDocumentPath { get; set; }
        public bool IsTelepharmacyConsentGranted { get; set; } = false;
        public string? TelepharmacyConsentDocumentPath { get; set; }

        // UTM Attribution (Marketing)
        [MaxLength(200)] public string? UtmSource { get; set; }
        [MaxLength(200)] public string? UtmMedium { get; set; }
        [MaxLength(200)] public string? UtmCampaign { get; set; }
        [MaxLength(200)] public string? UtmTerm { get; set; }

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
