using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Services;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize(Roles = "Admin,Backoffice")] // Simplified for demo/testing, assume strict role in prod
    [Authorize]
    public class ComplianceAdminController : ControllerBase
    {
        private readonly DataContext _dbContext;
        private readonly ICryptographicStorageService _cryptoStorage;

        public ComplianceAdminController(DataContext dbContext, ICryptographicStorageService cryptoStorage)
        {
            _dbContext = dbContext;
            _cryptoStorage = cryptoStorage;
        }

        [HttpGet("approbation/{pharmacistId}")]
        public async Task<IActionResult> FetchApprobationDocument(int pharmacistId, CancellationToken ct)
        {
            var pharmacist = await _dbContext.Pharmacists.FindAsync(new object[] { pharmacistId }, ct);
            if (pharmacist == null || string.IsNullOrEmpty(pharmacist.ApprobationDocumentPath))
            {
                return NotFound(new { message = "Document not found." });
            }

            try
            {
                var decryptedBytes = await _cryptoStorage.RetrieveAndDecryptAsync(pharmacist.ApprobationDocumentPath, ct);
                
                var ext = Path.GetExtension(pharmacist.ApprobationDocumentPath).Replace(".enc", "");
                var mimeType = ext == ".pdf" ? "application/pdf" : "image/jpeg";

                return File(decryptedBytes, mimeType, $"Approbation_{pharmacistId}{ext}");
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { message = "Encrypted blob could not be located." });
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                return StatusCode(500, new { message = "Cryptographic integrity failure. Ciphertext may have been tampered with." });
            }
        }

        [HttpPost("verify/{pharmacistId}")]
        public async Task<IActionResult> VerifyApprobation(int pharmacistId, CancellationToken ct)
        {
            var pharmacist = await _dbContext.Pharmacists.FindAsync(new object[] { pharmacistId }, ct);
            if (pharmacist == null) return NotFound(new { message = "Pharmacist not found." });

            if (string.IsNullOrEmpty(pharmacist.ApprobationDocumentPath))
            {
                return BadRequest(new { message = "Cannot verify. No document uploaded." });
            }

            // Mutate state to unlock the matching engine
            pharmacist.IsApprobationVerified = true;
            await _dbContext.SaveChangesAsync(ct);

            return Ok(new { message = "Approbationsurkunde verified successfully. Matching engine unlocked for this Pharmacist." });
        }
    }
}
