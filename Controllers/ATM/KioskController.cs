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

namespace ServiceApotheke.API.Controllers.ATM
{
    [ApiController]
    [Route("api/atm/kiosk")]
    public class KioskController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private readonly DataContext _context;

        public KioskController(IMemoryCache cache, DataContext context)
        {
            _cache = cache;
            _context = context;
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

            var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.UserId == pharmacyUserId);
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
