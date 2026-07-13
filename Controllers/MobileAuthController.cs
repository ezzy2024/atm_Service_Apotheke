using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ServiceApotheke.API.Models;
using ServiceApotheke.API.Data;
using System;
using System.Threading.Tasks;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/mobile/auth")]
    public class MobileAuthController : ControllerBase
    {
        private readonly DataContext _context;

        public MobileAuthController(DataContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] MobileLoginDto login)
        {
            var user = await _context.Pharmacists.SingleOrDefaultAsync(p => p.Email == login.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash))
                return Unauthorized(new { message = "Ungültige Anmeldedaten." });

            if (!user.IsEmailConfirmed)
                return Unauthorized(new { message = "Bitte bestätigen Sie zuerst Ihre E-Mail-Adresse." });

            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshTokenString();
            var refreshTokenHash = HashToken(refreshToken);

            var mobileToken = new MobileRefreshToken
            {
                PharmacistId = user.Id,
                TokenHash = refreshTokenHash,
                DeviceId = login.DeviceId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            // Revoke any existing active token for this device
            var existingTokens = await _context.MobileRefreshTokens
                .Where(t => t.PharmacistId == user.Id && t.DeviceId == login.DeviceId && t.RevokedAt == null)
                .ToListAsync();

            foreach(var t in existingTokens) {
                t.RevokedAt = DateTime.UtcNow;
            }

            _context.MobileRefreshTokens.Add(mobileToken);
            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                accessToken, 
                refreshToken, 
                expiresIn = 3600, // 1 hour for access token
                id = user.Id.ToString(), 
                fullName = user.FullName 
            });
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] MobileRefreshDto refreshDto)
        {
            var tokenHash = HashToken(refreshDto.RefreshToken);
            var mobileToken = await _context.MobileRefreshTokens
                .Include(t => t.Pharmacist)
                .SingleOrDefaultAsync(t => t.TokenHash == tokenHash && t.DeviceId == refreshDto.DeviceId);

            if (mobileToken == null || !mobileToken.IsActive)
                return Unauthorized(new { message = "Invalid refresh token." });

            // Issue new tokens (Refresh Token Rotation)
            var newAccessToken = GenerateAccessToken(mobileToken.Pharmacist);
            var newRefreshToken = GenerateRefreshTokenString();
            var newRefreshTokenHash = HashToken(newRefreshToken);

            mobileToken.RevokedAt = DateTime.UtcNow; // Revoke old one

            var newTokenEntry = new MobileRefreshToken
            {
                PharmacistId = mobileToken.PharmacistId,
                TokenHash = newRefreshTokenHash,
                DeviceId = refreshDto.DeviceId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            _context.MobileRefreshTokens.Add(newTokenEntry);
            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                accessToken = newAccessToken, 
                refreshToken = newRefreshToken,
                expiresIn = 3600
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] MobileLogoutDto logoutDto)
        {
            var userIdStr = User.FindFirstValue("id");
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var activeTokens = await _context.MobileRefreshTokens
                .Where(t => t.PharmacistId == userId && t.DeviceId == logoutDto.DeviceId && t.RevokedAt == null)
                .ToListAsync();

            foreach(var t in activeTokens) {
                t.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Logged out successfully" });
        }

        private string GenerateAccessToken(Pharmacist user)
        {
            var key = Encoding.UTF8.GetBytes("EIN_LANGER_GEHEIMER_SCHLUESSEL_MIT_MINDESTENS_32_ZEICHEN");
            var tokenDescriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity(new[] { 
                    new Claim("id", user.Id.ToString()), 
                    new Claim(ClaimTypes.Email, user.Email), 
                    new Claim(ClaimTypes.Role, "Pharmacist") 
                }),
                // Short expiration for mobile access token (e.g. 1 hour)
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = "ServiceApotheke.API",
                Audience = "ServiceApotheke.Clients",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = new JwtSecurityTokenHandler().CreateToken(tokenDescriptor);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshTokenString()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private string HashToken(string token)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(token);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }

    public class MobileLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
    }

    public class MobileRefreshDto
    {
        public string RefreshToken { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
    }

    public class MobileLogoutDto
    {
        public string DeviceId { get; set; } = string.Empty;
    }
}
