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
using ServiceApotheke.API.Domain.Constants;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public AdminController(DataContext context, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto login)
        {
            var adminUser = _configuration["AdminSettings:Email"] ?? "admin@serviceapotheke.tech";
            var adminPass = _configuration["AdminSettings:Password"];
            if (string.IsNullOrEmpty(adminPass)) throw new Exception("Admin password is missing from configuration!");

            if (!string.IsNullOrEmpty(adminPass) && login.Email == adminUser && login.Password == adminPass)
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtKey = _configuration["JwtSettings:Secret"];
                if (string.IsNullOrEmpty(jwtKey)) throw new Exception("JWT Secret is missing from configuration!");
                var key = Encoding.UTF8.GetBytes(jwtKey);
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
            p.Status = VerificationStatus.Verified;
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
            p.Status = VerificationStatus.Verified;
            await _context.SaveChangesAsync();
            return Ok();
        }

        public class PremiumAccessUpdateDto
        {
            public bool HasPremiumAccess { get; set; }
        }

        [HttpPatch("pharmacies/{id}/premium-access")]
        public async Task<IActionResult> UpdatePremiumAccess(int id, [FromBody] PremiumAccessUpdateDto dto)
        {
            var p = await _context.Pharmacies.FindAsync(id);
            if (p == null) return NotFound();
            p.HasPremiumAccess = dto.HasPremiumAccess;
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
                .Where(t => t.Status == TimesheetStatus.Disputed && t.DisputedAt != null && t.DisputedAt < threshold)
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
                .Where(k => k.Status != TerminalStatus.Active)
                .ToListAsync();

            foreach (var terminal in inactiveTerminals)
            {
                Console.WriteLine($"[ALERT] Kiosk Terminal {terminal.Id} ({terminal.Name}) at Pharmacy {terminal.PharmacyId} is in status '{terminal.Status}'.");
            }

            return Ok(new { message = $"Checked kiosk telemetry. Found {inactiveTerminals.Count} non-active terminals." });
        }

        [HttpGet("document/{role}/{id}/{documentType}")]
        public async Task<IActionResult> DownloadAdminDocument(
            string role, 
            int id, 
            string documentType,
            [FromServices] ServiceApotheke.API.Services.ICryptographicStorageService cryptoStorage,
            [FromServices] ServiceApotheke.API.Services.IGoogleCloudStorageService gcsStorage)
        {
            string? path = null;
            if (role.ToLower() == "pharmacist")
            {
                var p = await _context.Pharmacists.FindAsync(id);
                if (p == null) return NotFound();
                path = documentType.ToLower() switch 
                {
                    "approbation" => p.ApprobationDocumentPath,
                    "cv" => p.CvDocumentPath,
                    "contract" => p.FreelanceContractDocumentPath,
                    "idcard" => p.IdCardDocumentPath,
                    "insurance" => p.LiabilityInsuranceDocumentPath,
                    "profile" => p.ProfilePicturePath,
                    _ => null
                };
            }
            else if (role.ToLower() == "pharmacy")
            {
                var p = await _context.Pharmacies.FindAsync(id);
                if (p == null) return NotFound();
                path = documentType.ToLower() switch 
                {
                    "contract" => p.FreelanceContractDocumentPath,
                    "telepharmacy" => p.TelepharmacyConsentDocumentPath,
                    _ => null
                };
            }

            if (string.IsNullOrEmpty(path)) return NotFound("Dokument nicht gefunden.");

            string ext = System.IO.Path.GetExtension(path.Replace(".enc", "")).ToLower();
            string contentType = ext switch {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };

            try 
            {
                if (path.StartsWith("gs://"))
                {
                    var objectName = path.Substring(5);
                    var stream = await gcsStorage.DownloadDocumentAsync(objectName);
                    return File(stream, contentType);
                }
                else if (path.EndsWith(".enc"))
                {
                    var fileBytes = await cryptoStorage.RetrieveAndDecryptAsync(path);
                    return File(fileBytes, contentType);
                }
                else if (System.IO.File.Exists(path))
                {
                    var stream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    return File(stream, contentType);
                }
                else if (path.StartsWith("/uploads"))
                {
                    var webRoot = Path.Combine(Path.GetTempPath(), "ServiceApothekeUploads", role.ToLower());
                    var localPath = Path.Combine(webRoot, path.TrimStart('/'));
                    if (System.IO.File.Exists(localPath))
                    {
                        var stream = new System.IO.FileStream(localPath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                        return File(stream, contentType);
                    }
                }
                
                return NotFound($"Dokumentdatei nicht gefunden: {path}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Fehler beim Laden des Dokuments.", error = ex.Message });
            }
        }

        [HttpPost("migrate-dms-to-gcs")]
        public async Task<IActionResult> MigrateDmsToGcs(
            [FromServices] ServiceApotheke.API.Services.ICryptographicStorageService cryptoStorage,
            [FromServices] ServiceApotheke.API.Services.IGoogleCloudStorageService gcsStorage)
        {
            int migratedCount = 0;
            int errorCount = 0;
            var errors = new System.Collections.Generic.List<string>();

            async Task<string?> MigrateFileAsync(string? currentPath, string folderName, string defaultMime)
            {
                if (string.IsNullOrEmpty(currentPath) || currentPath.StartsWith("gs://"))
                    return currentPath;

                try
                {
                    byte[] fileBytes;
                    if (currentPath.EndsWith(".enc"))
                    {
                        // Encrypted in DmsVault
                        fileBytes = await cryptoStorage.RetrieveAndDecryptAsync(currentPath);
                    }
                    else if (System.IO.File.Exists(currentPath))
                    {
                        // Unencrypted in ServiceApothekeUploads or absolute path
                        fileBytes = await System.IO.File.ReadAllBytesAsync(currentPath);
                    }
                    else
                    {
                        errors.Add($"File not found for path: {currentPath}");
                        errorCount++;
                        return currentPath; // Keep old path if file missing
                    }

                    using var ms = new System.IO.MemoryStream(fileBytes);
                    var ext = System.IO.Path.GetExtension(currentPath.Replace(".enc", ""));
                    if (string.IsNullOrEmpty(ext)) ext = ".pdf";
                    
                    var newFileName = $"{folderName}/{Guid.NewGuid()}{ext}";
                    var mimeType = ext.ToLower() == ".pdf" ? "application/pdf" : 
                                   ext.ToLower() == ".jpg" || ext.ToLower() == ".jpeg" ? "image/jpeg" : defaultMime;

                    await gcsStorage.UploadDocumentAsync(ms, newFileName, mimeType);
                    migratedCount++;
                    return $"gs://{newFileName}";
                }
                catch (Exception ex)
                {
                    errors.Add($"Error migrating {currentPath}: {ex.Message}");
                    errorCount++;
                    return currentPath;
                }
            }

            var pharmacists = await _context.Pharmacists.ToListAsync();
            foreach (var p in pharmacists)
            {
                p.ApprobationDocumentPath = await MigrateFileAsync(p.ApprobationDocumentPath, "pharmacist-approbations", "application/pdf");
                p.FreelanceContractDocumentPath = await MigrateFileAsync(p.FreelanceContractDocumentPath, "pharmacist-contracts", "application/pdf");
                p.CvDocumentPath = await MigrateFileAsync(p.CvDocumentPath, "pharmacist-cvs", "application/pdf");
                p.ProfilePicturePath = await MigrateFileAsync(p.ProfilePicturePath, "pharmacist-profiles", "image/jpeg");
                p.IdCardDocumentPath = await MigrateFileAsync(p.IdCardDocumentPath, "pharmacist-idcards", "image/jpeg");
                p.LiabilityInsuranceDocumentPath = await MigrateFileAsync(p.LiabilityInsuranceDocumentPath, "pharmacist-insurance", "application/pdf");
            }

            var pharmacies = await _context.Pharmacies.ToListAsync();
            foreach (var p in pharmacies)
            {
                p.FreelanceContractDocumentPath = await MigrateFileAsync(p.FreelanceContractDocumentPath, "pharmacy-contracts", "application/pdf");
                p.TelepharmacyConsentDocumentPath = await MigrateFileAsync(p.TelepharmacyConsentDocumentPath, "pharmacy-telepharmacy", "application/pdf");
            }

            await _context.SaveChangesAsync();

            return Ok(new {
                message = "Migration complete.",
                migratedCount,
                errorCount,
                errors
            });
        }
    }
}
