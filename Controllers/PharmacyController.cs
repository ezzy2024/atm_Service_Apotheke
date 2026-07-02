using Microsoft.AspNetCore.Mvc;
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

        public PharmacyController(DataContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] PharmacyRegDto registration)
        {
            if (await _context.Pharmacies.AnyAsync(p => p.Email == registration.Email))
                return BadRequest(new { message = "Diese E-Mail-Adresse ist bereits registriert." });

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(registration.Password);
            string token = new Random().Next(100000, 999999).ToString();

            var pharmacy = new Pharmacy
            {
                PharmacyName = registration.PharmacyName,
                Email = registration.Email,
                PasswordHash = passwordHash,
                PhoneNumber = registration.PhoneNumber,
                Address = registration.Address,
                LicenseNumber = registration.LicenseNumber,
                EmailConfirmationToken = token,
                IsEmailConfirmed = false,
                
                // CRITICAL FIX: Explicitly set required default values to prevent constraint violations
                IsVerified = false,
                InvoiceBillingPossible = false, 
                ParkingAvailable = false,
                ContactPerson = "",
                SoftwareSystem = "",
                FocusAreas = "",
                StaffSupport = "",
                TargetHourlyRate = null,
                AccommodationProvided = ""
            };

            _context.Pharmacies.Add(pharmacy);
            
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
                await _emailService.SendEmailAsync(pharmacy.Email, subject, message);
            } 
            catch (Exception ex) 
            {
                Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
            }

            return Ok(new { message = "Registrierung erfolgreich." });
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
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            var pharmacy = await _context.Pharmacies.SingleOrDefaultAsync(p => p.Email == login.Email);
            
            if (pharmacy == null || !BCrypt.Net.BCrypt.Verify(login.Password, pharmacy.PasswordHash))
                return Unauthorized(new { message = "Ungültige Anmeldedaten." });

            if (!pharmacy.IsEmailConfirmed)
                return Unauthorized(new { message = "Bitte bestätigen Sie zuerst Ihre E-Mail-Adresse." });

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("vTccveQUGQTOL56EI0X/o3R1wHtjIjoed0NusZ9fKoY="); // Consider injecting this from configuration
            
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
            if (Request.Host.Host.Contains("serviceapotheke.tech")) cookieOptions.Domain = ".serviceapotheke.tech";
            Response.Cookies.Append("jwt", tokenString, cookieOptions);

            return Ok(new { 
                id = pharmacy.Id.ToString(), 
                pharmacyName = pharmacy.PharmacyName
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

            pharmacy.PharmacyName = dto.PharmacyName ?? pharmacy.PharmacyName;
            pharmacy.PhoneNumber = dto.PhoneNumber ?? pharmacy.PhoneNumber;
            pharmacy.Address = dto.Address ?? pharmacy.Address;
            pharmacy.LicenseNumber = dto.LicenseNumber ?? pharmacy.LicenseNumber;
            pharmacy.SoftwareSystem = dto.SoftwareSystem ?? pharmacy.SoftwareSystem;

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

            pharmacy.PharmacyName = dto.PharmacyName ?? pharmacy.PharmacyName;
            pharmacy.PhoneNumber = dto.PhoneNumber ?? pharmacy.PhoneNumber;
            pharmacy.Address = dto.Address ?? pharmacy.Address;
            pharmacy.LicenseNumber = dto.LicenseNumber ?? pharmacy.LicenseNumber;
            pharmacy.SoftwareSystem = dto.SoftwareSystem ?? pharmacy.SoftwareSystem;
            pharmacy.ContactPerson = dto.ContactPerson ?? pharmacy.ContactPerson;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profil erfolgreich aktualisiert." });
        }
    }

    public class UpdatePharmacyProfileDto
    {
        public string? PharmacyName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? LicenseNumber { get; set; }
        public string? SoftwareSystem { get; set; }
        public string? ContactPerson { get; set; }
    }

    public class PharmacyRegDto
    {
        public string PharmacyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
    }
}