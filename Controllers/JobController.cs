using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Models;
using ServiceApotheke.API.Models.DTOs;
using ServiceApotheke.API.Data;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using ServiceApotheke.API.Domain.Constants;

namespace ServiceApotheke.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class JobController : ControllerBase
    {
        private readonly DataContext _context;

        public JobController(DataContext context)
        {
            _context = context;
        }

        private MaskedPharmacistDto MapToPharmacistDto(Pharmacist p, bool isAccepted)
        {
            var nameParts = p.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
            var lastName = nameParts.Length > 1 ? nameParts.Last() : "";
            var initial = string.IsNullOrEmpty(lastName) ? "" : lastName.Substring(0, 1);
            
            var dto = new MaskedPharmacistDto
            {
                Id = p.Id,
                FirstName = firstName,
                LastNameInitial = initial,
                HourlyRate = p.HourlyRate,
                ExperienceYears = p.ExperienceYears,
                Qualification = p.Qualification,
                WwsProficiency = p.WwsProficiency,
                ProfilePicturePath = p.ProfilePicturePath,
                ApprobationDocumentPath = p.ApprobationDocumentPath,
                CvDocumentPath = p.CvDocumentPath
            };
            
            if (isAccepted)
            {
                dto.LastName = lastName;
                dto.Email = p.Email;
                dto.PhoneNumber = p.PhoneNumber;
            }
            return dto;
        }

        private MaskedPharmacyDto MapToPharmacyDto(Pharmacy p, bool isAccepted)
        {
            var postalPrefix = !string.IsNullOrEmpty(p.PostalCode) && p.PostalCode.Length >= 2 
                ? p.PostalCode.Substring(0, 2) + "***" 
                : "";
                
            var dto = new MaskedPharmacyDto
            {
                Id = p.Id,
                City = p.City,
                PostalCodePrefix = postalPrefix,
                TargetHourlyRate = p.TargetHourlyRate
            };
            
            if (isAccepted)
            {
                dto.PharmacyName = p.PharmacyName;
                dto.Email = p.Email;
                dto.PhoneNumber = p.PhoneNumber;
                dto.Street = p.Street;
                dto.HouseNumber = p.HouseNumber;
                dto.PostalCode = p.PostalCode;
            }
            return dto;
        }

        private MaskedJobPostDto MapToJobPostDto(JobPost j, bool isAccepted)
        {
            return new MaskedJobPostDto
            {
                Id = j.Id,
                PharmacyId = j.PharmacyId,
                Title = j.Title,
                Description = j.Description,
                StartDate = j.StartDate,
                EndDate = j.EndDate,
                Salary = j.Salary,
                Status = j.Status,
                RequiredQualifications = j.RequiredQualifications,
                CreatedAt = j.CreatedAt,
                HasApplied = j.HasApplied,
                Pharmacy = j.Pharmacy != null ? MapToPharmacyDto(j.Pharmacy, isAccepted) : null
            };
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableJobs()
        {
            var jobs = await _context.JobPosts
                .Include(j => j.Pharmacy)
                .Include(j => j.JobApplications)
                .Where(j => j.Status == JobPostStatus.Active)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            var userIdClaim = User.FindFirst("id")?.Value;
            var dtos = new List<MaskedJobPostDto>();
            
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int pid))
            {
                foreach (var job in jobs)
                {
                    job.HasApplied = job.JobApplications.Any(a => a.PharmacistId == pid);
                    
                    // The job listing board NEVER shows full pharmacy details, even if applied.
                    // Full details are only shown in the Application view if Accepted.
                    dtos.Add(MapToJobPostDto(job, isAccepted: false)); 
                }
            }
            else
            {
                foreach (var job in jobs)
                {
                    dtos.Add(MapToJobPostDto(job, isAccepted: false));
                }
            }

            return Ok(dtos);
        }

        [HttpGet("pharmacy/{pharmacyId}")]
        public async Task<IActionResult> GetJobsByPharmacy(int pharmacyId)
        {
            var jobs = await _context.JobPosts
                .Include(j => j.JobApplications)
                    .ThenInclude(a => a.Pharmacist)
                .Where(j => j.PharmacyId == pharmacyId)
                .OrderByDescending(j => j.StartDate)
                .ToListAsync();

            var dtos = jobs.Select(j => new
            {
                id = j.Id,
                title = j.Title,
                description = j.Description,
                startDate = j.StartDate,
                endDate = j.EndDate,
                salary = j.Salary,
                status = j.Status,
                requiredQualifications = j.RequiredQualifications,
                createdAt = j.CreatedAt,
                jobApplications = j.JobApplications.Select(a => new ServiceApotheke.API.Models.DTOs.JobApplicationDto
                {
                    Id = a.Id,
                    JobPostId = a.JobPostId,
                    PharmacistId = a.PharmacistId,
                    Status = a.Status,
                    AppliedAt = a.AppliedAt,
                    JobPost = null,
                    Pharmacist = a.Pharmacist != null ? MapToPharmacistDto(a.Pharmacist, a.Status == JobApplicationStatus.Accepted || a.Status == JobApplicationStatus.Completed || a.Status == JobApplicationStatus.Invoiced) : null
                }).ToList()
            });

            return Ok(dtos);
        }

        [HttpGet("test-matching/{pharmacistId}")]
        [AllowAnonymous]
        public async Task<IActionResult> TestMatching(int pharmacistId)
        {
            var pharmacist = await _context.Pharmacists.FindAsync(pharmacistId);
            if (pharmacist == null) return NotFound("Pharmacist not found");
            var job = await _context.JobPosts.Include(j => j.Pharmacy).OrderByDescending(j => j.Id).FirstOrDefaultAsync();
            if (job == null) return NotFound("Job not found");

            var result = new {
                PharmacistInfo = new { pharmacist.Id, pharmacist.RadiusKm, pharmacist.MaxDistanceKm, pharmacist.HourlyRate, pharmacist.Qualification, pharmacist.Latitude, pharmacist.Longitude },
                JobInfo = new { job.Id, job.Salary, job.Pharmacy.TargetHourlyRate, job.RequiredQualifications, job.Pharmacy.Latitude, job.Pharmacy.Longitude },
                MatchingStats = new {
                    BudgetMatch = (job.Pharmacy.TargetHourlyRate != null && job.Pharmacy.TargetHourlyRate >= pharmacist.HourlyRate) || (job.Salary != null && job.Salary >= pharmacist.HourlyRate),
                    QualMatch = string.IsNullOrEmpty(job.RequiredQualifications) || job.RequiredQualifications == pharmacist.Qualification
                }
            };
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateJob([FromBody] JobPost job)
        {
            if (job.PharmacyId == 0)
            {
                var userIdClaim = User.FindFirst("id")?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int pid))
                {
                    job.PharmacyId = pid;
                }
            }
            job.CreatedAt = DateTime.UtcNow;
            job.Status = JobPostStatus.Active;
            
            // Auto-generate title
            string reason = string.IsNullOrEmpty(job.ReasonForVacancy) ? "Vertretung" : job.ReasonForVacancy;
            string startStr = job.StartDate?.ToString("dd.MM.yyyy") ?? "?";
            string endStr = job.EndDate?.ToString("dd.MM.yyyy") ?? "?";
            
            if (startStr == endStr) {
                job.Title = $"{reason} ({startStr})";
            } else {
                job.Title = $"{reason} ({startStr} - {endStr})";
            }

            _context.JobPosts.Add(job);
            await _context.SaveChangesAsync();
            return Ok(job);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateJobPost(int id, [FromBody] JobPost updatedJob)
        {
            var job = await _context.JobPosts.FindAsync(id);
            if (job == null)
                return NotFound(new { message = "Stellenangebot nicht gefunden." });

            job.Title = updatedJob.Title;
            job.Description = updatedJob.Description;
            job.StartDate = updatedJob.StartDate;
            job.EndDate = updatedJob.EndDate;
            job.Salary = updatedJob.Salary;
            job.Status = updatedJob.Status;
            job.RequiredQualifications = updatedJob.RequiredQualifications;

            await _context.SaveChangesAsync();
            return Ok(job);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJobPost(int id)
        {
            var job = await _context.JobPosts.FindAsync(id);
            if (job == null)
                return NotFound(new { message = "Stellenangebot nicht gefunden." });

            _context.JobPosts.Remove(job);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Stellenangebot erfolgreich gelöscht." });
        }

        [HttpPost("JobApplication/apply")]
        public async Task<IActionResult> ApplyForJob([FromBody] JobApplication application)
        {
            var pharmacist = await _context.Pharmacists.FindAsync(application.PharmacistId);
            if (pharmacist == null)
                return NotFound(new { message = "Apotheker-Profil nicht gefunden." });

            if (pharmacist.Status != VerificationStatus.Verified)
            {
                return StatusCode(403, new { 
                    message = "Aktion verweigert: Ihr Profil ist noch nicht verifiziert. Gemäß den Vorgaben müssen Approbationsurkunde und relevante Dokumente im Compliance Profil hochgeladen und vom Support bestätigt werden, bevor Sie sich bewerben können." 
                });
            }

            if (await _context.JobApplications.AnyAsync(a => a.JobPostId == application.JobPostId && a.PharmacistId == application.PharmacistId))
                return BadRequest(new { message = "Bereits beworben." });

            application.AppliedAt = DateTime.UtcNow;
            application.Status = JobApplicationStatus.Pending;
            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();
            return Ok(application);
        }

        [HttpGet("JobApplication/pharmacist/{pharmacistId}")]
        public async Task<IActionResult> GetApplicationsForPharmacist(int pharmacistId)
        {
            var apps = await _context.JobApplications
                .Include(a => a.JobPost!)
                    .ThenInclude(j => j.Pharmacy!)
                .Where(a => a.PharmacistId == pharmacistId && a.JobPost != null)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();
                
            var dtos = apps.Select(a => new ServiceApotheke.API.Models.DTOs.JobApplicationDto
            {
                Id = a.Id,
                JobPostId = a.JobPostId,
                PharmacistId = a.PharmacistId,
                Status = a.Status,
                AppliedAt = a.AppliedAt,
                JobPost = a.JobPost != null ? MapToJobPostDto(a.JobPost, a.Status == JobApplicationStatus.Accepted || a.Status == JobApplicationStatus.Completed || a.Status == JobApplicationStatus.Invoiced) : null
            }).ToList();
            
            return Ok(dtos);
        }

        [HttpGet("JobApplication/pharmacy/{pharmacyId}")]
        public async Task<IActionResult> GetApplicationsForPharmacy(int pharmacyId)
        {
            var apps = await _context.JobApplications
                .Include(a => a.Pharmacist)
                .Include(a => a.JobPost)
                .Where(a => a.JobPost != null && a.JobPost!.PharmacyId == pharmacyId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();
                
            var dtos = apps.Select(a => new ServiceApotheke.API.Models.DTOs.JobApplicationDto
            {
                Id = a.Id,
                JobPostId = a.JobPostId,
                PharmacistId = a.PharmacistId,
                Status = a.Status,
                AppliedAt = a.AppliedAt,
                JobPost = a.JobPost != null ? MapToJobPostDto(a.JobPost, true) : null, // Pharmacy can see their own full details
                Pharmacist = a.Pharmacist != null ? MapToPharmacistDto(a.Pharmacist, a.Status == JobApplicationStatus.Accepted || a.Status == JobApplicationStatus.Completed || a.Status == JobApplicationStatus.Invoiced) : null
            }).ToList();
            
            return Ok(dtos);
        }

        [HttpPut("JobApplication/{id}/status")]
        public async Task<IActionResult> UpdateApplicationStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var app = await _context.JobApplications.FindAsync(id);
            if (app == null) return NotFound();

            app.Status = dto.Status;
            await _context.SaveChangesAsync();
            return Ok(app);
        }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
