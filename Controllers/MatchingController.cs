using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Models;
using ServiceApotheke.API.Services;
using ServiceApotheke.API.Domain.Constants;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MatchingController : ControllerBase
    {
        private readonly DataContext _dbContext;
        private readonly IHaversineDistanceService _haversine;
        private readonly INotificationDispatcher _dispatcher;
        private readonly IMatchingService _matchingService;

        public MatchingController(
            DataContext dbContext, 
            IHaversineDistanceService haversine, 
            INotificationDispatcher dispatcher,
            IMatchingService matchingService)
        {
            _dbContext = dbContext;
            _haversine = haversine;
            _dispatcher = dispatcher;
            _matchingService = matchingService;
        }

        [HttpPost("job-posts/{jobPostId}/notify-matches")]
        public async Task<IActionResult> NotifyMatchedPharmacists(int jobPostId)
        {
            // Identity checks could go here depending on Pharmacy role
            var matches = await _matchingService.FindMatchesForJobPostAsync(jobPostId);
            
            // For example, notify top 5 matches
            var topMatches = matches.Take(5).ToList();
            
            int notifiedCount = 0;
            foreach (var match in topMatches)
            {
                var title = "Neue Schicht verfügbar!";
                var body = $"Eine neue Schicht bei {match.JobPost.Pharmacy.PharmacyName} passt zu Ihrem Profil (Match-Score: {match.Score * 100}%).";
                
                await _dispatcher.DispatchToPharmacistAsync(
                    match.Pharmacist.Id, 
                    title, 
                    body, 
                    new { jobPostId = jobPostId, type = "NEW_MATCH" }
                );
                notifiedCount++;
            }

            return Ok(new { message = $"Dispatched push notifications to {notifiedCount} freelancers." });
        }

        [HttpGet("available-shifts")]
        public async Task<IActionResult> GetAvailableShifts(CancellationToken ct)
        {
            // Extract Identity
            var pharmacistIdClaim = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(pharmacistIdClaim, out var pharmacistId))
            {
                return Unauthorized(new { message = "Invalid token claims." });
            }

            var pharmacist = await _dbContext.Pharmacists.FindAsync(new object[] { pharmacistId }, ct);
            if (pharmacist == null)
            {
                return NotFound(new { message = "Pharmacist not found." });
            }

            // Compliance Gating (Pre-Filtration)
            if (pharmacist.Status != VerificationStatus.Verified || !pharmacist.IsApprobationVerified || pharmacist.FreelanceContractStatus != "Active")
            {
                return StatusCode(403, new { 
                    code = "COMPLIANCE_LOCK", 
                    message = "Pharmacist must have verified Approbation, an Active Freelance contract, and Verified Status to view shifts." 
                });
            }

            if (!pharmacist.Latitude.HasValue || !pharmacist.Longitude.HasValue)
            {
                return BadRequest(new { 
                    message = "Pharmacist location (Latitude/Longitude) is required for proximity matching." 
                });
            }

            // Execute Matching
            var activeJobPosts = await _dbContext.JobPosts
                .Include(j => j.Pharmacy)
                .Where(j => j.Status == JobPostStatus.Open || j.Status == JobPostStatus.Active)
                .ToListAsync(ct);

            var matches = activeJobPosts
                .Where(j => j.Pharmacy.Latitude.HasValue && j.Pharmacy.Longitude.HasValue)
                .Select(j => new
                {
                    JobPost = j,
                    Distance = _haversine.CalculateDistance(
                        pharmacist.Latitude.Value, 
                        pharmacist.Longitude.Value, 
                        j.Pharmacy.Latitude.Value, 
                        j.Pharmacy.Longitude.Value)
                })
                .Where(x => x.Distance <= pharmacist.MaxDistanceKm)
                .OrderBy(x => x.Distance)
                .Select(x => new
                {
                    id = x.JobPost.Id,
                    title = x.JobPost.Title,
                    description = x.JobPost.Description,
                    date = x.JobPost.StartDate,
                    hourlyRate = x.JobPost.Salary,
                    startTime = x.JobPost.StartDate,
                    endTime = x.JobPost.EndDate,
                    distanceKm = Math.Round(x.Distance, 1),
                    pharmacy = new {
                        id = x.JobPost.Pharmacy.Id,
                        name = x.JobPost.Pharmacy.PharmacyName,
                        city = x.JobPost.Pharmacy.City,
                        postalCode = x.JobPost.Pharmacy.PostalCode
                    }
                })
                .ToList();

            return Ok(matches);
        }
    }
}
