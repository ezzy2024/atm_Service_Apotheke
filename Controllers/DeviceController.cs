using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ServiceApotheke.API.Models;
using ServiceApotheke.API.Data;
using System;
using System.Threading.Tasks;

namespace ServiceApotheke.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        private readonly DataContext _context;

        public DeviceController(DataContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceDto dto)
        {
            var userIdStr = User.FindFirstValue("id");
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var existingDevice = await _context.DeviceTokens
                .SingleOrDefaultAsync(d => d.PharmacistId == userId && d.DeviceId == dto.DeviceId);

            if (existingDevice != null)
            {
                // Upsert: Overwrite the FCM token because they frequently rotate
                existingDevice.FcmToken = dto.FcmToken;
                existingDevice.LastActive = DateTime.UtcNow;
                
                // If platform changed somehow, update it
                if (!string.IsNullOrEmpty(dto.DevicePlatform))
                {
                    existingDevice.DevicePlatform = dto.DevicePlatform;
                }
            }
            else
            {
                // New device registration
                var newDevice = new DeviceToken
                {
                    PharmacistId = userId,
                    DeviceId = dto.DeviceId,
                    FcmToken = dto.FcmToken,
                    DevicePlatform = dto.DevicePlatform ?? "Unknown",
                    CreatedAt = DateTime.UtcNow,
                    LastActive = DateTime.UtcNow
                };
                _context.DeviceTokens.Add(newDevice);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Device registered successfully." });
        }
    }

    public class RegisterDeviceDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public string FcmToken { get; set; } = string.Empty;
        public string DevicePlatform { get; set; } = string.Empty;
    }
}
