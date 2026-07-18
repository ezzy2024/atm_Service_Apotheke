using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ServiceApotheke.API.Models
{
    public class Pharmacist
    {
        public int MaxDistanceKm { get; set; } = 20;
        public int AvailableDaysPerWeek { get; set; } = 5;
        public int Id { get; set; }

        [Required, MaxLength(100)] 
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress] 
        public string Email { get; set; } = string.Empty;

        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;
        public int SessionVersion { get; set; } = 1;

        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        // Bank Details
        public string? Iban { get; set; }
        public string? Bic { get; set; }

        // Stripe Connect Account
        public string? StripeConnectAccountId { get; set; }

        public string Street { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Status-Felder
        public bool IsEmailConfirmed { get; set; } = false;
        public string? EmailConfirmationToken { get; set; }
        public DateTime? EmailConfirmationTokenExpiry { get; set; }
        public bool IsVerified { get; set; } = false;
        public bool IsApprobationVerified { get; set; } = false;
        public string FreelanceContractStatus { get; set; } = "Pending"; // Pending, Active, Expired
        public string UstIdValidationStatus { get; set; } = "Pending"; // Pending, Valid, Invalid
        
        public string? ApprobationNumber { get; set; }
        public bool IsFreelancerConfirmed { get; set; }
        public VerificationStatus Status { get; set; } = VerificationStatus.Unverified;

        // Beruflich / Profil
        public string? PreferredContactMethod { get; set; }
        public bool HasApprobation { get; set; }
        public string? ApprobationCountry { get; set; }
        public string? ExperienceYears { get; set; }
        public string? Specialties { get; set; }
        public string? SoftwareExperience { get; set; }
        public string Qualification { get; set; } = "Approbation";
        public string WwsProficiency { get; set; } = "";
        public int RadiusKm { get; set; } = 20; // Hier als int für korrekte Logik
        public string? PreferredStates { get; set; }
        public string? TravelWillingness { get; set; }
        public string? Mobility { get; set; }
        public string? AvailabilityType { get; set; }
        public string? ShortNoticeAvailability { get; set; }
        public bool EmergencyServiceWillingness { get; set; }
        public bool WeekendWillingness { get; set; }
        public string? FeeModel { get; set; }
        public string? BillingModel { get; set; } // Stundensatz, Tagessatz, Beides
        public decimal HourlyRate { get; set; }
        public bool IsVatRequired { get; set; }
        public string? VatSubject { get; set; }
        public string? TravelCostModel { get; set; } // Inklusive, nach km/Beleg, nach Absprache
        public string? TravelExpenses { get; set; }
        public string? CountryOfLicense { get; set; } // Deutschland, Other

        // Dateipfade
        public string? ApprobationDocumentPath { get; set; }
        public string? FreelanceContractDocumentPath { get; set; }
        public string? CvDocumentPath { get; set; }
        public string? ProfilePicturePath { get; set; }

        // KYC & Freelancer Compliance
        public bool IsKycVerified { get; set; } = false;
        public string? IdCardDocumentPath { get; set; }
        public string? LiabilityInsuranceDocumentPath { get; set; }
        public string? TaxId { get; set; }

        // GDPR Tracking
        public DateTime? GdprAnonymizedAt { get; set; }
        public DateTime? TermsAcceptedAt { get; set; }

        [JsonIgnore]
        public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}
