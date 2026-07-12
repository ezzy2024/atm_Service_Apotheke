using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Services;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ComplianceUploadController : ControllerBase
    {
        private readonly DataContext _dbContext;
        private readonly IFileSanitizationService _sanitizer;
        private readonly ICryptographicStorageService _cryptoStorage;

        public ComplianceUploadController(
            DataContext dbContext, 
            IFileSanitizationService sanitizer, 
            ICryptographicStorageService cryptoStorage)
        {
            _dbContext = dbContext;
            _sanitizer = sanitizer;
            _cryptoStorage = cryptoStorage;
        }

        [HttpPost("approbation")]
        public async Task<IActionResult> UploadApprobation(IFormFile file, CancellationToken ct)
        {
            // 1. Identity Extraction
            var pharmacistIdClaim = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(pharmacistIdClaim, out var pharmacistId)) return Unauthorized(new { message = "Invalid token claims." });

            var pharmacist = await _dbContext.Pharmacists.FindAsync(new object[] { pharmacistId }, ct);
            if (pharmacist == null) return NotFound(new { message = "Pharmacist not found." });

            // 2. Payload Sanitization
            if (!_sanitizer.IsValidDocument(file))
            {
                return BadRequest(new { message = "Invalid file payload. Must be a valid PDF, JPG, or PNG under 5MB." });
            }

            // 3. Cryptographic Storage Pipeline
            using var stream = file.OpenReadStream();
            var locatorPath = await _cryptoStorage.EncryptAndStoreAsync(stream, file.FileName, ct);

            // 4. State Mutation
            pharmacist.ApprobationDocumentPath = locatorPath;
            pharmacist.IsApprobationVerified = false; // Reset verification state, requires new admin review
            
            await _dbContext.SaveChangesAsync(ct);

            return Ok(new { message = "Approbationsurkunde securely uploaded and encrypted. Pending administrative verification." });
        }
    }
}
