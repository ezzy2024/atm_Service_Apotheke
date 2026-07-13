using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
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

namespace ServiceApotheke.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PharmacyController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly EmailService _emailService;
        private readonly IGeocodingService _geocodingService;
        private readonly IWebHostEnvironment _env;

        public PharmacyController(DataContext context, EmailService emailService, IGeocodingService geocodingService, IWebHostEnvironment env)
        {
            _context = context;
            _emailService = emailService;
            _geocodingService = geocodingService;
            _env = env;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        [EnableRateLimiting("AuthLimiter")]
        [ValidateAntiForgeryToken]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Register([FromForm] PharmacyRegDto registration, IFormFile? documentFile)
        {
            if (await _context.Pharmacies.AnyAsync(p => p.Email == registration.Email))
                return BadRequest(new { message = "Diese E-Mail-Adresse ist bereits registriert." });

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(registration.Password);
            string token = new Random().Next(100000, 999999).ToString();

            var coords = await _geocodingService.GetCoordinatesAsync($"{registration.Street} {registration.HouseNumber}, {registration.PostalCode} {registration.City}, Germany");

            var pharmacy = new Pharmacy
            {
                PharmacyName = registration.PharmacyName ?? "",
                Email = registration.Email ?? "",
                PasswordHash = passwordHash,
                PhoneNumber = registration.PhoneNumber ?? "",
                Street = registration.Street ?? "",
                HouseNumber = registration.HouseNumber ?? "",
                PostalCode = registration.PostalCode ?? "",
                City = registration.City ?? "",
                Latitude = coords?.Latitude,
                Longitude = coords?.Longitude,
                LicenseNumber = registration.LicenseNumber ?? "",
                EmailConfirmationToken = token,
                IsEmailConfirmed = true,
                
                // CRITICAL FIX: Explicitly set required default values to prevent constraint violations
                IsVerified = false,
                InvoiceBillingPossible = false, 
                ParkingAvailable = false,
                ContactPerson = "",
                SoftwareSystem = registration.SoftwareSystem ?? "",
                FocusAreas = registration.Description ?? "",
                StaffSupport = "",
                TargetHourlyRate = null,
                AccommodationProvided = "",
                UtmSource = registration.UtmSource ?? "",
                UtmMedium = registration.UtmMedium ?? "",
                UtmCampaign = registration.UtmCampaign ?? "",
                UtmTerm = registration.UtmTerm ?? ""
            };

            _context.Pharmacies.Add(pharmacy);
            
            try 
            {
                await _context.SaveChangesAsync();

                // Process Document File Upload after DB Save (to get ID)
                if (documentFile != null)
                {
                    var webRoot = Path.Combine(Path.GetTempPath(), "ServiceApothekeUploads", "pharmacy");
                    var uploadPath = Path.Combine(webRoot, "uploads", pharmacy.Id.ToString());

                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    var docPath = Path.Combine(uploadPath, "license_" + documentFile.FileName);
                    using (var stream = new FileStream(docPath, FileMode.Create))
                    {
                        await documentFile.CopyToAsync(stream);
                    }
                    // Optionally save path back to pharmacy if there is a field for it
                }
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
                await _emailService.SendEmailAsync(pharmacy.Email, subject, message);
            } 
            catch (Exception ex) 
            {
                Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("EIN_LANGER_GEHEIMER_SCHLUESSEL_MIT_MINDESTENS_32_ZEICHEN");
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", pharmacy.Id.ToString()),
                    new Claim(ClaimTypes.Email, pharmacy.Email),
                    new Claim(ClaimTypes.Role, "Pharmacy")
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = "ServiceApotheke.API",
                Audience = "ServiceApotheke.Clients",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var jwtToken = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(jwtToken);
            
            var cookieOptions = new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Expires = DateTime.UtcNow.AddHours(8) };
            Response.Cookies.Append("sa_auth_v2", tokenString, cookieOptions);

            return Ok(new { 
                message = "Registrierung erfolgreich.",
                id = pharmacy.Id.ToString(), 
                pharmacyName = pharmacy.PharmacyName,
                token = tokenString
            });
        }

        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] EmailConfirmDto model)
        {
            var pharmacy = await _context.Pharmacies.SingleOrDefaultAsync(u => u.Email == model.Email);
            if (pharmacy == null || pharmacy.EmailConfirmationToken != model.Token) 
                return BadRequest("Code ungültig oder abgelaufen.");
            
            pharmacy.IsEmailConfirmed = true;
            pharmacy.EmailConfirmationToken = null;
            await _context.SaveChangesAsync();
            return Ok("Konto bestätigt.");
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [EnableRateLimiting("AuthLimiter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            var pharmacy = await _context.Pharmacies.SingleOrDefaultAsync(p => p.Email == login.Email);
            
            if (pharmacy == null || !BCrypt.Net.BCrypt.Verify(login.Password, pharmacy.PasswordHash))
                return Unauthorized(new { message = "Ungültige Anmeldedaten." });

            if (!pharmacy.IsEmailConfirmed)
                return Unauthorized(new { message = "Bitte bestätigen Sie zuerst Ihre E-Mail-Adresse." });

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("EIN_LANGER_GEHEIMER_SCHLUESSEL_MIT_MINDESTENS_32_ZEICHEN");
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("id", pharmacy.Id.ToString()),
                    new Claim(ClaimTypes.Email, pharmacy.Email),
                    new Claim(ClaimTypes.Role, "Pharmacy")
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = "ServiceApotheke.API",
                Audience = "ServiceApotheke.Clients",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            
            var cookieOptions = new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Expires = DateTime.UtcNow.AddHours(8) };
            // cookieOptions.Domain = ".serviceapotheke.tech";
            Response.Cookies.Append("sa_auth_v2", tokenString, cookieOptions);

            return Ok(new { 
                id = pharmacy.Id.ToString(), 
                pharmacyName = pharmacy.PharmacyName,
                token = tokenString
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPharmacy(int id)
        {
            var pharmacy = await _context.Pharmacies.FindAsync(id);
            if (pharmacy == null) return NotFound(new { message = "Apotheke nicht gefunden." });
            
            pharmacy.PasswordHash = string.Empty; 
            return Ok(pharmacy);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdatePharmacyProfileDto dto)
        {
            var pharmacy = await _context.Pharmacies.FindAsync(id);
            if (pharmacy == null) return NotFound(new { message = "Apotheke nicht gefunden." });

            bool addressChanged = (dto.Street != null && dto.Street != pharmacy.Street) ||
                                  (dto.HouseNumber != null && dto.HouseNumber != pharmacy.HouseNumber) ||
                                  (dto.PostalCode != null && dto.PostalCode != pharmacy.PostalCode) ||
                                  (dto.City != null && dto.City != pharmacy.City);

            pharmacy.PharmacyName = dto.PharmacyName ?? pharmacy.PharmacyName;
            pharmacy.PhoneNumber = dto.PhoneNumber ?? pharmacy.PhoneNumber;
            pharmacy.Street = dto.Street ?? pharmacy.Street; pharmacy.HouseNumber = dto.HouseNumber ?? pharmacy.HouseNumber; pharmacy.PostalCode = dto.PostalCode ?? pharmacy.PostalCode; pharmacy.City = dto.City ?? pharmacy.City;
            pharmacy.LicenseNumber = dto.LicenseNumber ?? pharmacy.LicenseNumber;
            pharmacy.SoftwareSystem = dto.SoftwareSystem ?? pharmacy.SoftwareSystem;

            if (addressChanged || pharmacy.Latitude == null || pharmacy.Longitude == null)
            {
                var coords = await _geocodingService.GetCoordinatesAsync($"{pharmacy.Street} {pharmacy.HouseNumber}, {pharmacy.PostalCode} {pharmacy.City}, Germany");
                if (coords != null)
                {
                    pharmacy.Latitude = coords.Value.Latitude;
                    pharmacy.Longitude = coords.Value.Longitude;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profil erfolgreich aktualisiert." });
        }

        [HttpPut("/api/PharmacyProfile")]
        public async Task<IActionResult> UpdatePharmacyProfile([FromBody] UpdatePharmacyProfileDto dto)
        {
            var userIdString = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized(new { message = "Benutzer nicht authentifiziert." });
            }

            var pharmacy = await _context.Pharmacies.FindAsync(userId);
            if (pharmacy == null) return NotFound(new { message = "Apotheke nicht gefunden." });

            bool addressChanged = (dto.Street != null && dto.Street != pharmacy.Street) ||
                                  (dto.HouseNumber != null && dto.HouseNumber != pharmacy.HouseNumber) ||
                                  (dto.PostalCode != null && dto.PostalCode != pharmacy.PostalCode) ||
                                  (dto.City != null && dto.City != pharmacy.City);

            pharmacy.PharmacyName = dto.PharmacyName ?? pharmacy.PharmacyName;
            pharmacy.PhoneNumber = dto.PhoneNumber ?? pharmacy.PhoneNumber;
            pharmacy.Street = dto.Street ?? pharmacy.Street; pharmacy.HouseNumber = dto.HouseNumber ?? pharmacy.HouseNumber; pharmacy.PostalCode = dto.PostalCode ?? pharmacy.PostalCode; pharmacy.City = dto.City ?? pharmacy.City;
            pharmacy.LicenseNumber = dto.LicenseNumber ?? pharmacy.LicenseNumber;
            pharmacy.SoftwareSystem = dto.SoftwareSystem ?? pharmacy.SoftwareSystem;
            pharmacy.ContactPerson = dto.ContactPerson ?? pharmacy.ContactPerson;

            if (addressChanged || pharmacy.Latitude == null || pharmacy.Longitude == null)
            {
                var coords = await _geocodingService.GetCoordinatesAsync($"{pharmacy.Street} {pharmacy.HouseNumber}, {pharmacy.PostalCode} {pharmacy.City}, Germany");
                if (coords != null)
                {
                    pharmacy.Latitude = coords.Value.Latitude;
                    pharmacy.Longitude = coords.Value.Longitude;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profil erfolgreich aktualisiert." });
        }
    }

    public class UpdatePharmacyProfileDto
    {
        public string? PharmacyName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Street { get; set; }
        public string? HouseNumber { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? LicenseNumber { get; set; }
        public string? SoftwareSystem { get; set; }
        public string? ContactPerson { get; set; }
    }

    public class PharmacyRegDto
    {
        public string? PharmacyName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Street { get; set; }
        public string? HouseNumber { get; set; }
        public string? PostalCode { get; set; }
        public string? City { get; set; }
        public string? LicenseNumber { get; set; }
        public string? SoftwareSystem { get; set; }
        public string? Description { get; set; }
        
        // UTM Attribution (Marketing)
        public string? UtmSource { get; set; }
        public string? UtmMedium { get; set; }
        public string? UtmCampaign { get; set; }
        public string? UtmTerm { get; set; }
    }
}
