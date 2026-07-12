using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using ServiceApotheke.API.Models;
using Microsoft.AspNetCore.Authorization;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;

        public AdminController(DataContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto login)
        {
            if (login.Email == "admin@serviceapotheke.tech" && login.Password == "RootAccess2026!")
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes("EIN_LANGER_GEHEIMER_SCHLUESSEL_MIT_MINDESTENS_32_ZEICHEN");
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim("id", "0"),
                        new Claim(ClaimTypes.Email, login.Email),
                        new Claim(ClaimTypes.Role, "admin")
                    }),
                    Expires = DateTime.UtcNow.AddHours(12),
                    Issuer = "ServiceApotheke.API",
                    Audience = "ServiceApotheke.Clients",
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                return Ok(new { token = tokenHandler.WriteToken(token), id = 0, type = "admin" });
            }
            return Unauthorized(new { message = "Root-Zugriff verweigert." });
        }

        [HttpGet("pharmacists/pending")]
        public async Task<IActionResult> GetPendingPharmacists()
        {
            var data = await _context.Pharmacists
                .Where(p => !p.IsVerified)
                .Select(p => new { 
                    p.Id, 
                    p.FullName, 
                    p.Email, 
                    p.PhoneNumber, 
                    p.ApprobationCountry,
                    p.ApprobationDocumentPath 
                })
                .ToListAsync();
            return Ok(data);
        }

        [HttpGet("pharmacies/pending")]
        public async Task<IActionResult> GetPendingPharmacies()
        {
            var data = await _context.Pharmacies
                .Where(p => !p.IsVerified)
                .Select(p => new { 
                    p.Id, 
                    p.PharmacyName, 
                    p.Email, 
                    p.LicenseNumber, 
                    Address = p.Street + " " + p.HouseNumber + ", " + p.PostalCode + " " + p.City 
                })
                .ToListAsync();
            return Ok(data);
        }

        [HttpPatch("pharmacists/{id}/verify")]
        public async Task<IActionResult> VerifyPharmacist(int id)
        {
            var p = await _context.Pharmacists.FindAsync(id);
            if (p == null) return NotFound();
            p.IsVerified = true;
            p.IsEmailConfirmed = true;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPatch("pharmacies/{id}/verify")]
        public async Task<IActionResult> VerifyPharmacy(int id)
        {
            var p = await _context.Pharmacies.FindAsync(id);
            if (p == null) return NotFound();
            p.IsVerified = true;
            p.IsEmailConfirmed = true;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("finance")]
        public async Task<IActionResult> GetFinanceAggregation()
        {
            var commissionInvoices = await _context.Invoices
                .Where(i => i.Type == "PlatformCommissionInvoice")
                .ToListAsync();

            var totalCommission = commissionInvoices.Sum(i => i.TotalAmount);
            var totalInvoicesCount = commissionInvoices.Count;

            return Ok(new {
                Revenue = totalCommission,
                CommissionInvoicesCount = totalInvoicesCount
            });
        }

        [HttpGet("metrics/pac")]
        public async Task<IActionResult> GetPacMetrics()
        {
            // PAC Calculation: aggregate commission revenue grouped by UtmTerm
            // Client-side evaluation due to complex nested includes in SQLite/EFCore
            var query = await _context.Invoices
                .Where(i => i.Type == "PlatformCommissionInvoice")
                .Include(i => i.Timesheet)
                    .ThenInclude(t => t.JobApplication)
                        .ThenInclude(ja => ja.JobPost)
                            .ThenInclude(jp => jp.Pharmacy)
                .ToListAsync();

            var pacData = query
                .Where(i => i.Timesheet?.JobApplication?.JobPost?.Pharmacy != null)
                .GroupBy(i => i.Timesheet.JobApplication.JobPost.Pharmacy.UtmTerm ?? "Organic")
                .Select(g => new {
                    UtmTerm = g.Key,
                    TotalCommissionRevenue = g.Sum(i => i.TotalAmount),
                    InvoicesCount = g.Count(),
                    PharmaciesAcquired = g.Select(i => i.Timesheet.JobApplication.JobPost.Pharmacy.Id).Distinct().Count()
                })
                .ToList();

            return Ok(pacData);
        }
        [HttpGet("metrics/tta")]
        public async Task<IActionResult> GetTimeToActivation()
        {
            var pharmacies = await _context.Pharmacies
                .Include(p => p.JobPosts)
                .Include(p => p.PdlDocuments)
                .ToListAsync();

            var results = pharmacies.Select(p => {
                var firstJobPost = p.JobPosts.OrderBy(jp => jp.CreatedAt).FirstOrDefault()?.CreatedAt;
                var firstPdl = p.PdlDocuments.OrderBy(pd => pd.CreatedAt).FirstOrDefault()?.CreatedAt;
                
                DateTime? firstActivity = null;
                if (firstJobPost.HasValue && firstPdl.HasValue)
                    firstActivity = firstJobPost.Value < firstPdl.Value ? firstJobPost.Value : firstPdl.Value;
                else if (firstJobPost.HasValue)
                    firstActivity = firstJobPost.Value;
                else if (firstPdl.HasValue)
                    firstActivity = firstPdl.Value;

                var timeDelta = firstActivity.HasValue ? firstActivity.Value - p.CreatedAt : DateTime.UtcNow - p.CreatedAt;
                
                return new {
                    PharmacyId = p.Id,
                    PharmacyName = p.PharmacyName,
                    UtmTerm = p.UtmTerm,
                    PostalCode = p.PostalCode,
                    CreatedAt = p.CreatedAt,
                    FirstActivityAt = firstActivity,
                    TimeDeltaHours = timeDelta.TotalHours,
                    RequiresOutbound = !firstActivity.HasValue && timeDelta.TotalHours > 48
                };
            }).ToList();

            return Ok(results);
        }

        [AllowAnonymous]
        [HttpPost("cron/stale-timesheets")]
        public async Task<IActionResult> CheckStaleTimesheets()
        {
            var threshold = DateTime.UtcNow.AddDays(-7);
            var staleTimesheets = await _context.Timesheets
                .Where(t => t.Status == "Disputed" && t.DisputedAt != null && t.DisputedAt < threshold)
                .ToListAsync();

            foreach (var ts in staleTimesheets)
            {
                // In production, this would dispatch an email or PagerDuty alert via an injected service
                Console.WriteLine($"[ALERT] Timesheet {ts.Id} has been Disputed since {ts.DisputedAt}. Escalation required.");
            }

            return Ok(new { message = $"Processed {staleTimesheets.Count} stale timesheets." });
        }

        [AllowAnonymous]
        [HttpPost("cron/kiosk-telemetry")]
        public async Task<IActionResult> CheckKioskTelemetry()
        {
            // Here we check for kiosks that are inactive or have revoked status
            var inactiveTerminals = await _context.KioskTerminals
                .Where(k => k.Status != "active")
                .ToListAsync();

            foreach (var terminal in inactiveTerminals)
            {
                Console.WriteLine($"[ALERT] Kiosk Terminal {terminal.Id} ({terminal.Name}) at Pharmacy {terminal.PharmacyId} is in status '{terminal.Status}'.");
            }

            return Ok(new { message = $"Checked kiosk telemetry. Found {inactiveTerminals.Count} non-active terminals." });
        }
    }
}
