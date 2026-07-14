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
        private readonly IGoogleCloudStorageService _storageService;

        public ComplianceUploadController(
            DataContext dbContext, 
            IFileSanitizationService sanitizer, 
            IGoogleCloudStorageService storageService)
        {
            _dbContext = dbContext;
            _sanitizer = sanitizer;
            _storageService = storageService;
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
            var locatorPath = await _storageService.UploadDocumentAsync(stream, file.FileName, file.ContentType, ct);

            // 4. State Mutation
            pharmacist.ApprobationDocumentPath = locatorPath;
            pharmacist.IsApprobationVerified = false; // Reset verification state, requires new admin review
            
            await _dbContext.SaveChangesAsync(ct);

            return Ok(new { message = "Approbationsurkunde securely uploaded and encrypted. Pending administrative verification." });
        }

        [HttpPost("freelance-contract")]
        public async Task<IActionResult> UploadFreelanceContract(IFormFile file, CancellationToken ct)
        {
            var roleClaim = User.FindFirstValue(ClaimTypes.Role);
            var idClaim = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idClaim, out var entityId)) return Unauthorized(new { message = "Invalid token claims." });

            if (!_sanitizer.IsValidDocument(file))
                return BadRequest(new { message = "Invalid file payload. Must be a valid PDF, JPG, or PNG under 5MB." });

            using var stream = file.OpenReadStream();
            var locatorPath = await _storageService.UploadDocumentAsync(stream, file.FileName, file.ContentType, ct);

            if (roleClaim == "Pharmacist")
            {
                var pharmacist = await _dbContext.Pharmacists.FindAsync(new object[] { entityId }, ct);
                if (pharmacist == null) return NotFound(new { message = "Pharmacist not found." });
                pharmacist.FreelanceContractDocumentPath = locatorPath;
                pharmacist.FreelanceContractStatus = "Pending";
            }
            else if (roleClaim == "Pharmacy")
            {
                var pharmacy = await _dbContext.Pharmacies.FindAsync(new object[] { entityId }, ct);
                if (pharmacy == null) return NotFound(new { message = "Pharmacy not found." });
                pharmacy.FreelanceContractDocumentPath = locatorPath;
                pharmacy.FreelanceContractStatus = "Pending";
            }
            else
            {
                return Forbid();
            }

            await _dbContext.SaveChangesAsync(ct);
            return Ok(new { message = "Freelance contract securely uploaded and encrypted. Pending administrative verification." });
        }

        [HttpPost("telepharmacy-consent")]
        public async Task<IActionResult> UploadTelepharmacyConsent(IFormFile file, CancellationToken ct)
        {
            var roleClaim = User.FindFirstValue(ClaimTypes.Role);
            if (roleClaim != "Pharmacy") return Forbid();

            var idClaim = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idClaim, out var pharmacyId)) return Unauthorized(new { message = "Invalid token claims." });

            var pharmacy = await _dbContext.Pharmacies.FindAsync(new object[] { pharmacyId }, ct);
            if (pharmacy == null) return NotFound(new { message = "Pharmacy not found." });

            if (!_sanitizer.IsValidDocument(file))
                return BadRequest(new { message = "Invalid file payload. Must be a valid PDF, JPG, or PNG under 5MB." });

            using var stream = file.OpenReadStream();
            var locatorPath = await _storageService.UploadDocumentAsync(stream, file.FileName, file.ContentType, ct);

            pharmacy.TelepharmacyConsentDocumentPath = locatorPath;
            pharmacy.IsTelepharmacyConsentGranted = false; // Requires admin verification
            
            await _dbContext.SaveChangesAsync(ct);
            return Ok(new { message = "Telepharmacy consent securely uploaded and encrypted. Pending administrative verification." });
        }
    }
}
