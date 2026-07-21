using System;
using System.Collections.Generic;

namespace ServiceApotheke.API.Models.DTOs
{
    public class MaskedPharmacyDto
    {
        public int Id { get; set; }
        public string City { get; set; } = string.Empty;
        public string PostalCodePrefix { get; set; } = string.Empty;
        public decimal? TargetHourlyRate { get; set; }
        
        // Full fields for accepted state
        public string? PharmacyName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Street { get; set; }
        public string? HouseNumber { get; set; }
        public string? PostalCode { get; set; }
    }

    public class MaskedPharmacistDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastNameInitial { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public string? ExperienceYears { get; set; }
        public string Qualification { get; set; } = string.Empty;
        public string WwsProficiency { get; set; } = string.Empty;
        public string? ProfilePicturePath { get; set; }
        public string? ApprobationDocumentPath { get; set; }
        public string? CvDocumentPath { get; set; }
        
        // Full fields for accepted state
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        
        public string FullName => string.IsNullOrEmpty(LastName) 
            ? $"{FirstName} {LastNameInitial}." 
            : $"{FirstName} {LastName}";
    }

    public class MaskedJobPostDto
    {
        public int Id { get; set; }
        public int PharmacyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? Salary { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RequiredQualifications { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool HasApplied { get; set; }
        
        public MaskedPharmacyDto? Pharmacy { get; set; }
    }
    
    public class JobApplicationDto
    {
        public int Id { get; set; }
        public int JobPostId { get; set; }
        public int PharmacistId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        
        public MaskedJobPostDto? JobPost { get; set; }
        public MaskedPharmacistDto? Pharmacist { get; set; }
    }
}
