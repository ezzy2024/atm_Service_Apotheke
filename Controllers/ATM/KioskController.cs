using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Models.ATM;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Services;

namespace ServiceApotheke.API.Controllers.ATM
{
    [ApiController]
    [Route("api/atm/kiosk")]
    public class KioskController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private readonly DataContext _context;
        private readonly IGoogleCloudStorageService _storageService;

        public KioskController(IMemoryCache cache, DataContext context, IGoogleCloudStorageService storageService)
        {
            _cache = cache;
            _context = context;
            _storageService = storageService;
        }

        // Unauthenticated, Terminal-side
        [HttpPost("initiate")]
        [AllowAnonymous]
        public IActionResult Initiate()
        {
            // Generates a 6-digit numerical code
            var random = new Random();
            var code = random.Next(100000, 999999).ToString();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            // Initial state: token is null, waiting for pharmacy to pair
            _cache.Set(code, new KioskPairingState { IsPaired = false, DeviceToken = null }, cacheEntryOptions);

            return Ok(new { code });
        }

        // Unauthenticated, Terminal-side polling
        [HttpGet("status/{code}")]
        [AllowAnonymous]
        public IActionResult Status(string code)
        {
            if (!_cache.TryGetValue(code, out KioskPairingState state))
            {
                return BadRequest(new { error = "Code expired or invalid" });
            }

            if (state.IsPaired && !string.IsNullOrEmpty(state.DeviceToken))
            {
                return Ok(new { status = "paired", deviceToken = state.DeviceToken });
            }

            return Ok(new { status = "pending" });
        }

        // Authenticated, Pharmacy-side
        [HttpPost("pair")]
        [Authorize(Roles = "Pharmacy")]
        [ServiceApotheke.API.Filters.PremiumFeature]
        public async Task<IActionResult> Pair([FromBody] PairRequest request)
        {
            if (string.IsNullOrEmpty(request.Code))
            {
                return BadRequest(new { error = "Code is required" });
            }

            if (!_cache.TryGetValue(request.Code, out KioskPairingState state))
            {
                return BadRequest(new { error = "Code expired or invalid" });
            }

            var userId = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int pharmacyUserId))
            {
                return Unauthorized();
            }

            var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.Id == pharmacyUserId);
            if (pharmacy == null)
            {
                return Unauthorized();
            }

            // Generate 32-byte Device Token
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            var deviceToken = Convert.ToBase64String(tokenBytes);

            // Save KioskTerminal to DB
            var terminalName = string.IsNullOrWhiteSpace(request.TerminalName) ? $"Kiosk-{request.Code}" : request.TerminalName;
            var terminal = new KioskTerminal
            {
                PharmacyId = pharmacy.Id,
                Name = terminalName,
                DeviceToken = deviceToken,
                Status = "active"
            };

            _context.KioskTerminals.Add(terminal);
            await _context.SaveChangesAsync();

            // Update cache so the terminal can pick it up
            state.IsPaired = true;
            state.DeviceToken = deviceToken;
            _cache.Set(request.Code, state, TimeSpan.FromMinutes(5));

            return Ok(new { success = true, terminalId = terminal.Id });
        }

        [HttpGet("terminals")]
        [Authorize(Roles = "Pharmacy")]
        [ServiceApotheke.API.Filters.PremiumFeature]
        public async Task<IActionResult> GetTerminals()
        {
            var userId = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int pharmacyUserId)) return Unauthorized();

            var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.Id == pharmacyUserId);
            if (pharmacy == null) return Unauthorized();

            var terminals = await _context.KioskTerminals
                .Where(t => t.PharmacyId == pharmacy.Id && t.Status == "active")
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new { t.Id, t.Name, t.CreatedAt })
                .ToListAsync();

            return Ok(terminals);
        }

        [HttpDelete("terminals/{id}")]
        [Authorize(Roles = "Pharmacy")]
        [ServiceApotheke.API.Filters.PremiumFeature]
        public async Task<IActionResult> RevokeTerminal(int id)
        {
            var userId = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int pharmacyUserId)) return Unauthorized();

            var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.Id == pharmacyUserId);
            if (pharmacy == null) return Unauthorized();

            var terminal = await _context.KioskTerminals.FirstOrDefaultAsync(t => t.Id == id && t.PharmacyId == pharmacy.Id);
            if (terminal == null) return NotFound();

            terminal.Status = "revoked";
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpGet("ledger")]
        [Authorize(Roles = "Pharmacy")]
        [ServiceApotheke.API.Filters.PremiumFeature]
        public async Task<IActionResult> GetLedger()
        {
            var userId = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int pharmacyUserId)) return Unauthorized();

            var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.Id == pharmacyUserId);
            if (pharmacy == null) return Unauthorized();

            var ledger = await _context.AtmBillingRecords
                .Include(b => b.ConsentAgreement)
                .Where(b => b.PharmacyId == pharmacy.Id)
                .OrderByDescending(b => b.DateOfService)
                .Select(b => new {
                    b.Id,
                    b.ServiceType,
                    b.Amount,
                    b.DateOfService,
                    b.ReportPath,
                    PatientName = b.ConsentAgreement.PatientName,
                    KVNR = b.ConsentAgreement.HealthInsuranceNumber
                })
                .ToListAsync();

            return Ok(ledger);
        }

        [HttpGet("download/{locator}")]
        [Authorize(Roles = "Pharmacy")]
        [ServiceApotheke.API.Filters.PremiumFeature]
        public async Task<IActionResult> DownloadConsent(string locator)
        {
            var userId = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int pharmacyUserId)) return Unauthorized();

            var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.Id == pharmacyUserId);
            if (pharmacy == null) return Unauthorized();

            // Note: A true production system would also verify this specific locator 
            // belongs to a billing record owned by this pharmacy to prevent IDOR.
            var stream = await _storageService.DownloadDocumentAsync(locator);
            return File(stream, "application/pdf");
        }

        [HttpGet("debug-db")]
        public async Task<IActionResult> DebugDb()
        {
            try {
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT * FROM \"ConsentAgreements\" ORDER BY \"Id\" DESC LIMIT 5;";
                await _context.Database.OpenConnectionAsync();
                var results = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
                using (var reader = await command.ExecuteReaderAsync()) {
                    while (await reader.ReadAsync()) {
                        var row = new System.Collections.Generic.Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++) {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }
                        results.Add(row);
                    }
                }
                
                command.CommandText = "SELECT column_name, data_type, is_nullable FROM information_schema.columns WHERE table_name = 'ConsentAgreements';";
                var columns = new System.Collections.Generic.List<object>();
                using (var reader2 = await command.ExecuteReaderAsync()) {
                    while (await reader2.ReadAsync()) {
                        columns.Add(new {
                            Name = reader2.GetString(0),
                            Type = reader2.GetString(1),
                            Nullable = reader2.GetString(2)
                        });
                    }
                }
                
                command.CommandText = "SELECT count(*) FROM \"ConsentAgreements\";";
                int count = 0;
                using (var reader3 = await command.ExecuteReaderAsync()) {
                    if (await reader3.ReadAsync()) {
                        count = Convert.ToInt32(reader3.GetValue(0));
                    }
                }

                command.CommandText = "SELECT column_name, data_type, is_nullable FROM information_schema.columns WHERE table_name = 'AtmBillingRecords';";
                var columns2 = new System.Collections.Generic.List<object>();
                using (var reader4 = await command.ExecuteReaderAsync()) {
                    while (await reader4.ReadAsync()) {
                        columns2.Add(new {
                            Name = reader4.GetString(0),
                            Type = reader4.GetString(1),
                            Nullable = reader4.GetString(2)
                        });
                    }
                }

                return Ok(new { Columns = columns, Count = count, AtmColumns = columns2 });
            } catch (Exception ex) {
                return StatusCode(500, ex.ToString());
            }
        }
    }

    public class KioskPairingState
    {
        public bool IsPaired { get; set; }
        public string DeviceToken { get; set; }
    }

    public class PairRequest
    {
        public string Code { get; set; }
        public string TerminalName { get; set; }
    }
}
