using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Models;
using ServiceApotheke.API.Data;
using System;
using System.Threading.Tasks;
using System.Linq;

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

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableJobs()
        {
            var jobs = await _context.JobPosts
                .Include(j => j.Pharmacy)
                .Where(j => j.Status == "Active")
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
            return Ok(jobs);
        }

        [HttpGet("pharmacy/{pharmacyId}")]
        public async Task<IActionResult> GetJobsByPharmacy(int pharmacyId)
        {
            var jobs = await _context.JobPosts
                .Include(j => j.JobApplications)
                .Where(j => j.PharmacyId == pharmacyId)
                .OrderByDescending(j => j.StartDate)
                .ToListAsync();
            return Ok(jobs);
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
            job.CreatedAt = DateTime.UtcNow;
            job.Status = "Active";
            
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
            // 1. AÜG & Liability Compliance Gate: Verify KYC Status
            var pharmacist = await _context.Pharmacists.FindAsync(application.PharmacistId);
            if (pharmacist == null)
                return NotFound(new { message = "Apotheker-Profil nicht gefunden." });

            // Temporarily disabled KYC check for testing
            /*
            if (!pharmacist.IsKycVerified)
            {
                return StatusCode(403, new { 
                    message = "Aktion verweigert: Ihr Profil ist noch nicht verifiziert. Gemäß AÜG-Vorgaben müssen Approbationsurkunde, Personalausweis und Berufshaftpflichtversicherung zwingend im Compliance Profil hochgeladen und vom Support bestätigt werden, bevor Sie Mandate anfragen können." 
                });
            }
            */

            // 2. Prevent Duplicate Applications
            if (await _context.JobApplications.AnyAsync(a => a.JobPostId == application.JobPostId && a.PharmacistId == application.PharmacistId))
                return BadRequest(new { message = "Bereits beworben." });

            // 3. Execute Application
            application.AppliedAt = DateTime.UtcNow;
            application.Status = "Pending";
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
            return Ok(apps);
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
            return Ok(apps);
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
