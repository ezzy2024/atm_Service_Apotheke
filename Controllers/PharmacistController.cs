using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ServiceApotheke.API.Models;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Services;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using ServiceApotheke.API.Domain.Constants;


namespace ServiceApotheke.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PharmacistController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly EmailService _emailService;
        private readonly IGeocodingService _geocodingService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly IGoogleCloudStorageService _gcsService;

        public PharmacistController(DataContext context, EmailService emailService, IGeocodingService geocodingService, Microsoft.Extensions.Configuration.IConfiguration configuration, IGoogleCloudStorageService gcsService)
        {
            _context = context;
            _emailService = emailService;
            _geocodingService = geocodingService;
            _configuration = configuration;
            _gcsService = gcsService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        [EnableRateLimiting("AuthLimiter")]
        public async Task<IActionResult> Register([FromBody] PharmacistRegDto registration)
        {
            if (await _context.Pharmacists.AnyAsync(p => p.Email == registration.Email))
                return BadRequest(new { message = "Diese E-Mail-Adresse ist bereits registriert." });

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(registration.Password);
            string token = new Random().Next(100000, 999999).ToString();

            var coords = await _geocodingService.GetCoordinatesAsync($"{registration.Street} {registration.HouseNumber}, {registration.PostalCode} {registration.City}, Germany");

            var pharmacist = new Pharmacist
            {
                FullName = registration.FullName,
                Email = registration.Email,
                PasswordHash = passwordHash,
                PhoneNumber = registration.PhoneNumber,
                Street = registration.Street,
                HouseNumber = registration.HouseNumber,
                PostalCode = registration.PostalCode,
                City = registration.City,
                Latitude = coords?.Latitude,
                Longitude = coords?.Longitude,
                Qualification = registration.Qualification,
                WwsProficiency = registration.WwsProficiency,
                EmailConfirmationToken = token,
                EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(24),
                IsEmailConfirmed = false,
                
                // Explicit initialization mapped to exact model types
                IsVerified = false,
                HasApprobation = false,
                EmergencyServiceWillingness = false,
                WeekendWillingness = false,
                TravelWillingness = "",
                ShortNoticeAvailability = "",
                MaxDistanceKm = 50,
                RadiusKm = 50,
                ExperienceYears = "",
                AvailableDaysPerWeek = 0,
                ApprobationCountry = "",
                AvailabilityType = "",
                FeeModel = "",
                Mobility = "",
                PreferredContactMethod = "",
                PreferredStates = "",
                SoftwareExperience = "",
                Specialties = "",
                TravelExpenses = "",
                VatSubject = ""
            };

            _context.Pharmacists.Add(pharmacist);

            try 
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"[DB ERROR] {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, new { message = "Ein Datenbankfehler ist aufgetreten.", details = ex.InnerException?.Message });
            }

            try 
            {
                string subject = "Ihr Aktivierungscode für ServiceApotheke";
                string message = $"Willkommen bei ServiceApotheke!\n\nIhr 6-stelliger Aktivierungscode lautet: {token}\n\nBitte geben Sie diesen Code auf der Bestätigungsseite ein.";
                await _emailService.SendEmailAsync(pharmacist.Email, subject, message);
            } 
            catch (Exception ex) 
            {
                Console.WriteLine($"[EMAIL ERROR] Konnte E-Mail nicht senden an {pharmacist.Email}: {ex.Message}");
            }

            return Ok(new { message = "Registrierung erfolgreich." });
        }

        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] EmailConfirmDto model)
        {
            var user = await _context.Pharmacists.SingleOrDefaultAsync(u => u.Email == model.Email);
            if (user == null || user.EmailConfirmationToken != model.Token || user.EmailConfirmationTokenExpiry < DateTime.UtcNow) 
                return BadRequest("Code ungültig oder abgelaufen.");
            
            user.IsEmailConfirmed = true; 
            user.EmailConfirmationToken = null;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Konto bestätigt." });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [EnableRateLimiting("AuthLimiter")]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            var user = await _context.Pharmacists.SingleOrDefaultAsync(p => p.Email == login.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash))
                return Unauthorized(new { message = "Ungültige Anmeldedaten." });

            if (!user.IsEmailConfirmed)
                return Unauthorized(new { message = "Bitte bestätigen Sie zuerst Ihre E-Mail-Adresse." });

            var jwtKey = _configuration["JwtSettings:Secret"];
            if (string.IsNullOrEmpty(jwtKey)) throw new Exception("JWT Secret is missing from configuration!");
            var key = Encoding.UTF8.GetBytes(jwtKey);
            var tokenDescriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity(new[] { 
                    new Claim("id", user.Id.ToString()), 
                    new Claim(ClaimTypes.Email, user.Email), 
                    new Claim(ClaimTypes.Role, "Pharmacist"),
                    new Claim("SessionVersion", user.SessionVersion.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = "ServiceApotheke.API",
                Audience = "ServiceApotheke.Clients",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = new JwtSecurityTokenHandler().CreateToken(tokenDescriptor);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            
            var cookieOptions = new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Expires = DateTime.UtcNow.AddHours(8) };
            // cookieOptions.Domain = ".serviceapotheke.tech";
            Response.Cookies.Append("sa_auth_v2", tokenString, cookieOptions);
            return Ok(new { id = user.Id.ToString(), fullName = user.FullName, token = tokenString });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPharmacist(int id)
        {
            var user = await _context.Pharmacists.FindAsync(id);
            if (user == null) return NotFound();
            user.PasswordHash = string.Empty;
            return Ok(user);
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        [EnableRateLimiting("AuthLimiter")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _context.Pharmacists.SingleOrDefaultAsync(p => p.Email == dto.Email);
            if (user == null) return Ok(new { message = "Falls diese E-Mail existiert, wurde ein Link versendet." });

            string token = new Random().Next(100000, 999999).ToString();
            user.EmailConfirmationToken = token;
            user.EmailConfirmationTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            string subject = "Passwort zurücksetzen (ServiceApotheke)";
            string message = $"Ihr Code zum Zurücksetzen des Passworts lautet: {token}\nDieser Code ist 1 Stunde gültig.";
            await _emailService.SendEmailAsync(user.Email, subject, message);

            return Ok(new { message = "Falls diese E-Mail existiert, wurde ein Link versendet." });
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        [EnableRateLimiting("AuthLimiter")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _context.Pharmacists.SingleOrDefaultAsync(p => p.Email == dto.Email);
            if (user == null || user.EmailConfirmationToken != dto.Token || user.EmailConfirmationTokenExpiry < DateTime.UtcNow)
                return BadRequest("Code ungültig oder abgelaufen.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.SessionVersion++; // Invalidate active sessions
            user.EmailConfirmationToken = null;

            var mobileTokens = await _context.MobileRefreshTokens.Where(t => t.PharmacistId == user.Id).ToListAsync();
            _context.MobileRefreshTokens.RemoveRange(mobileTokens);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Passwort erfolgreich zurückgesetzt. Sie können sich nun anmelden." });
        }

        [HttpPut("{id}/profile")]
        public async Task<IActionResult> UpdatePharmacistProfile(int id, [FromBody] PharmacistProfileUpdateDto dto)
        {
            var user = await _context.Pharmacists.FindAsync(id);
            if (user == null) return NotFound();

            bool addressChanged = (dto.Street != null && dto.Street != user.Street) ||
                                  (dto.HouseNumber != null && dto.HouseNumber != user.HouseNumber) ||
                                  (dto.PostalCode != null && dto.PostalCode != user.PostalCode) ||
                                  (dto.City != null && dto.City != user.City);

            user.FullName = dto.FullName ?? user.FullName;
            user.PhoneNumber = dto.Phone ?? user.PhoneNumber;
            user.Street = dto.Street ?? user.Street;
            user.HouseNumber = dto.HouseNumber ?? user.HouseNumber;
            user.PostalCode = dto.PostalCode ?? user.PostalCode;
            user.City = dto.City ?? user.City;
            user.MaxDistanceKm = dto.MaxDistanceKm;
            user.AvailableDaysPerWeek = dto.AvailableDaysPerWeek;
            user.Iban = dto.Iban ?? user.Iban;
            user.Bic = dto.Bic ?? user.Bic;
            
            if (addressChanged || user.Latitude == null || user.Longitude == null)
            {
                var coords = await _geocodingService.GetCoordinatesAsync($"{user.Street} {user.HouseNumber}, {user.PostalCode} {user.City}, Germany");
                if (coords != null)
                {
                    user.Latitude = coords.Value.Latitude;
                    user.Longitude = coords.Value.Longitude;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(user);
        }

        [HttpGet("{id}/all-shifts")]
        public async Task<IActionResult> GetAllShifts(int id)
        {
            var apps = await _context.JobApplications
                .Include(a => a.JobPost!)
                    .ThenInclude(jp => jp.Pharmacy!)
                .Include(a => a.Timesheet)
                .Where(a => a.PharmacistId == id && (a.Status == JobApplicationStatus.Accepted || a.Status == JobApplicationStatus.Completed || a.Status == JobApplicationStatus.Invoiced) && a.JobPost != null)
                .Select(a => new {
                    Id = a.Id,
                    Status = a.Status,
                    TimesheetStatus = a.Timesheet != null ? a.Timesheet.Status : null,
                    JobPost = new {
                        Title = a.JobPost!.Title,
                        StartDate = a.JobPost.StartDate,
                        EndDate = a.JobPost.EndDate,
                        Salary = a.JobPost.Salary,
                        Pharmacy = new { PharmacyName = a.JobPost.Pharmacy!.PharmacyName }
                    }
                })
                .ToListAsync();

            return Ok(apps);
        }

        [HttpGet("{id}/upcoming-shifts")]
        public async Task<IActionResult> GetUpcomingShifts(int id)
        {
            var apps = await _context.JobApplications
                .Include(a => a.JobPost!)
                    .ThenInclude(jp => jp.Pharmacy!)
                .Include(a => a.Timesheet)
                .Where(a => a.PharmacistId == id && a.Status == JobApplicationStatus.Accepted && a.JobPost != null)
                .ToListAsync();

            var upcoming = apps.Where(a => a.Status == JobApplicationStatus.Accepted && a.JobPost!.StartDate > DateTime.UtcNow)
                .Select(a => new {
                    Id = a.Id,
                    Status = a.Status,
                    TimesheetStatus = a.Timesheet != null ? a.Timesheet.Status : null,
                    StartDate = a.JobPost!.StartDate,
                    EndDate = a.JobPost.EndDate,
                    Salary = a.JobPost.Salary,
                    Title = a.JobPost.Title,
                    PharmacyName = a.JobPost.Pharmacy!.PharmacyName
                }).ToList();
                
            return Ok(upcoming);
        }

        [HttpGet("{id}/completed-shifts")]
        public async Task<IActionResult> GetCompletedShifts(int id)
        {
            var apps = await _context.JobApplications
                .Include(a => a.JobPost!)
                    .ThenInclude(jp => jp.Pharmacy!)
                .Include(a => a.Timesheet)
                .Where(a => a.PharmacistId == id && a.Status == JobApplicationStatus.Accepted && a.JobPost != null)
                .ToListAsync();

            var completed = apps.Where(a => a.Status == JobApplicationStatus.Accepted && a.JobPost!.EndDate < DateTime.UtcNow)
                .Select(a => new {
                    Id = a.Id,
                    Status = a.Status,
                    TimesheetStatus = a.Timesheet != null ? a.Timesheet.Status : null,
                    JobPost = new {
                        Title = a.JobPost!.Title,
                        StartDate = a.JobPost.StartDate,
                        EndDate = a.JobPost.EndDate,
                        Salary = a.JobPost.Salary,
                        Pharmacy = new { PharmacyName = a.JobPost.Pharmacy!.PharmacyName }
                    }
                }).ToList();
            return Ok(completed);
        }

        [HttpPost("{id}/upload-cv")]
        public async Task<IActionResult> UploadCv(int id, IFormFile file)
        {
            var user = await _context.Pharmacists.FindAsync(id);
            if (user == null) return NotFound();
            user.CvDocumentPath = file.FileName;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("{id}/upload-documents")]
        public async Task<IActionResult> UploadDocuments(int id, IFormFile? approbation, IFormFile? cv, IFormFile? profilePicture)
        {
            var user = await _context.Pharmacists.FindAsync(id);
            if (user == null) return NotFound();

            if (approbation == null && cv == null && profilePicture == null)
                return BadRequest("No files uploaded.");

            var uploadsFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ServiceApothekeUploads");
            if (!System.IO.Directory.Exists(uploadsFolder))
                System.IO.Directory.CreateDirectory(uploadsFolder);

            async Task<string> SaveFileAsync(IFormFile file, string type)
            {
                var folder = System.IO.Path.Combine(uploadsFolder, type);
                if (!System.IO.Directory.Exists(folder))
                    System.IO.Directory.CreateDirectory(folder);

                var fileName = $"{id}_{System.Guid.NewGuid()}_{file.FileName}";
                var filePath = System.IO.Path.Combine(folder, fileName);

                using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                return filePath;
            }

            if (approbation != null)
            {
                user.ApprobationDocumentPath = await SaveFileAsync(approbation, "approbations");
                user.HasApprobation = true;
                user.IsVerified = false;
            }
            if (cv != null)
            {
                user.CvDocumentPath = await SaveFileAsync(cv, "cvs");
            }
            if (profilePicture != null)
            {
                user.ProfilePicturePath = await SaveFileAsync(profilePicture, "profiles");
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Dokumente erfolgreich hochgeladen." });
        }

        [Authorize]
        [HttpGet("{id}/document/{docType}")]
        public async Task<IActionResult> DownloadDocument(int id, string docType)
        {
            var userIdStr = User.FindFirstValue("id") ?? User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue("role") ?? User.FindFirstValue(System.Security.Claims.ClaimTypes.Role);
            
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized();
                
            var pharmacist = await _context.Pharmacists.FindAsync(id);
            if (pharmacist == null) return NotFound();
            
            // 1. Authorization Gate: Verify pharmacy actually has an application from this pharmacist
            if (userRole == "Pharmacy")
            {
                bool hasApplication = await _context.JobApplications
                    .Include(a => a.JobPost)
                    .AnyAsync(a => a.PharmacistId == id && a.JobPost != null && a.JobPost.PharmacyId == userId);
                    
                if (!hasApplication)
                {
                    return StatusCode(403, new { message = "Zugriff verweigert: Keine aktive Bewerbung für Ihre Apotheke." });
                }
            }
            else if (userRole == "Pharmacist" && userId != id)
            {
                return StatusCode(403, new { message = "Zugriff verweigert." });
            }
            
            string? path = docType.ToLower() switch 
            {
                "approbation" => pharmacist.ApprobationDocumentPath,
                "cv" => pharmacist.CvDocumentPath,
                "profile" => pharmacist.ProfilePicturePath,
                _ => null
            };
            
            if (string.IsNullOrEmpty(path)) return NotFound("Dokument nicht gefunden.");
            try {
                var memoryStream = await _gcsService.DownloadDocumentAsync(path);
                memoryStream.Position = 0;
                string ext = System.IO.Path.GetExtension(path).ToLower();
                string contentType = ext switch {
                    ".pdf" => "application/pdf",
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    _ => "application/octet-stream"
                };
                return File(memoryStream, contentType);
            } catch (Exception) {
                return NotFound("Dokument nicht gefunden in GCS.");
            }
        }

#if DEBUG
        [AllowAnonymous]
        [HttpGet("test-seed")]
        public async Task<IActionResult> TestSeed()
        {
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            var pharmacy = new Pharmacy
            {
                PharmacyName = $"Test Apotheke {suffix}",
                Email = $"testpharmacy_{suffix}@example.com",
                PasswordHash = "hashed",
                PhoneNumber = "0123456789",
                Street = "Apothekenstr.",
                HouseNumber = "42",
                PostalCode = "80331",
                City = "München",
                IsVerified = true
            };
            _context.Pharmacies.Add(pharmacy);

            var pharmacist = new Pharmacist
            {
                FullName = $"Test Apotheker {suffix}",
                Email = $"test.apotheker_{suffix}@example.com",
                PasswordHash = "hashed",
                Street = "Teststr.",
                HouseNumber = "1",
                PostalCode = "10115",
                City = "Berlin",
                Iban = "DE12345678901234567890",
                Bic = "TESTDEFFXXX",
                IsVerified = true,
                HasApprobation = true,
                Qualification = "Approbation",
                HourlyRate = 50.0m
            };
            _context.Pharmacists.Add(pharmacist);
            await _context.SaveChangesAsync();

            var jobPost = new JobPost
            {
                PharmacyId = pharmacy.Id,
                Title = "Tagesvertretung",
                StartDate = DateTime.UtcNow.AddDays(-2),
                EndDate = DateTime.UtcNow.AddDays(-1),
                Salary = 50.0m,
                Status = "Open",
                Description = "Test"
            };
            _context.JobPosts.Add(jobPost);
            await _context.SaveChangesAsync();

            var app = new JobApplication
            {
                JobPostId = jobPost.Id,
                PharmacistId = pharmacist.Id,
                Status = "Accepted",
                AppliedAt = DateTime.UtcNow.AddDays(-3)
            };
            _context.JobApplications.Add(app);
            await _context.SaveChangesAsync();

            var timesheet = new Timesheet
            {
                JobApplicationId = app.Id,
                ActualStartDate = jobPost.StartDate ?? DateTime.UtcNow.AddDays(-2),
                ActualStartTime = new TimeSpan(8, 0, 0),
                ActualEndTime = new TimeSpan(16, 0, 0),
                HourlyRate = 50.0m,
                TravelCosts = 20.0m,
                Status = "Submitted"
            };
            _context.Timesheets.Add(timesheet);
            await _context.SaveChangesAsync();

            return Ok($"Test data seeded successfully! Timesheet ID: {timesheet.Id}");
        }
#endif
    }

    public class EmailConfirmDto { public string Email { get; set; } = ""; public string Token { get; set; } = ""; }
    
    public class PharmacistProfileUpdateDto 
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Street { get; set; }
        public string? HouseNumber { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public int MaxDistanceKm { get; set; }
        public int AvailableDaysPerWeek { get; set; }
        public string? Iban { get; set; }
        public string? Bic { get; set; }
    }



    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
