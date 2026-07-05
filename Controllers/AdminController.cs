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
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPatch("pharmacies/{id}/verify")]
        public async Task<IActionResult> VerifyPharmacy(int id)
        {
            var p = await _context.Pharmacies.FindAsync(id);
            if (p == null) return NotFound();
            p.IsVerified = true;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("finance")]
        public async Task<IActionResult> GetFinanceAggregation()
        {
            var totalCommission = await _context.Invoices
                .Where(i => i.Type == "PlatformCommissionInvoice")
                .SumAsync(i => i.TotalAmount);

            var totalInvoicesCount = await _context.Invoices
                .Where(i => i.Type == "PlatformCommissionInvoice")
                .CountAsync();

            return Ok(new {
                Revenue = totalCommission,
                CommissionInvoicesCount = totalInvoicesCount
            });
        }
    }
}
