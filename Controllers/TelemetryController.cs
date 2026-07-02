using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Models;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelemetryController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public TelemetryController(DataContext context, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public class TemperaturePayload
        {
            public double Temperature { get; set; }
        }

        [HttpPost("temperature")]
        public async Task<IActionResult> LogTemperature([FromBody] TemperaturePayload payload)
        {
            if (!Request.Headers.TryGetValue("x-api-key", out var extractedApiKey))
            {
                return Unauthorized(new { message = "API Key was not provided." });
            }

            var pharmacy = await _context.Pharmacies
                .FirstOrDefaultAsync(p => p.ApiKey == extractedApiKey.ToString());

            if (pharmacy == null)
            {
                return Unauthorized(new { message = "Invalid API Key." });
            }

            bool isAnomaly = payload.Temperature < 2.0 || payload.Temperature > 8.0;

            var log = new TemperatureLog
            {
                PharmacyId = pharmacy.Id,
                Temperature = payload.Temperature,
                RecordedAt = DateTime.UtcNow,
                IsAnomaly = isAnomaly
            };

            _context.TemperatureLogs.Add(log);
            await _context.SaveChangesAsync();

            if (isAnomaly)
            {
                Console.WriteLine($"[ALERT] Temperature Anomaly Detected: {payload.Temperature}°C at Pharmacy {pharmacy.Id}");
            }

            var smtpHost = _configuration["SmtpSettings:Server"];
            var smtpUser = _configuration["SmtpSettings:Username"];
            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
            {
                Console.WriteLine($"[WARNING] SMTP configuration is incomplete. Alerts cannot be dispatched.");
            }

            return Ok(new { message = "Telemetry logged successfully.", isAnomaly });
        }
    }
}
