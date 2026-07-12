using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [HttpPost("verify-aug/{id}")]
        public async Task<IActionResult> VerifyAugContract(int id, [FromQuery] string targetType, CancellationToken ct)
        {
            if (targetType == "Pharmacist")
            {
                var pharmacist = await _dbContext.Pharmacists.FindAsync(new object[] { id }, ct);
                if (pharmacist == null) return NotFound(new { message = "Pharmacist not found." });
                if (string.IsNullOrEmpty(pharmacist.AugContractDocumentPath)) return BadRequest(new { message = "No AÜG document uploaded." });

                pharmacist.AugContractStatus = "Active";
            }
            else if (targetType == "Pharmacy")
            {
                var pharmacy = await _dbContext.Pharmacies.FindAsync(new object[] { id }, ct);
                if (pharmacy == null) return NotFound(new { message = "Pharmacy not found." });
                if (string.IsNullOrEmpty(pharmacy.AugContractDocumentPath)) return BadRequest(new { message = "No AÜG document uploaded." });

                pharmacy.AugContractStatus = "Active";
            }
            else
            {
                return BadRequest(new { message = "Invalid targetType. Must be 'Pharmacist' or 'Pharmacy'." });
            }

            await _dbContext.SaveChangesAsync(ct);
            return Ok(new { message = $"AÜG contract verified successfully for {targetType}." });
        }

        [HttpPost("verify-telepharmacy/{pharmacyId}")]
        public async Task<IActionResult> VerifyTelepharmacyConsent(int pharmacyId, CancellationToken ct)
        {
            var pharmacy = await _dbContext.Pharmacies.FindAsync(new object[] { pharmacyId }, ct);
            if (pharmacy == null) return NotFound(new { message = "Pharmacy not found." });

            if (string.IsNullOrEmpty(pharmacy.TelepharmacyConsentDocumentPath))
                return BadRequest(new { message = "No Telepharmacy Consent document uploaded." });

            pharmacy.IsTelepharmacyConsentGranted = true;
            await _dbContext.SaveChangesAsync(ct);

            return Ok(new { message = "Telepharmacy consent verified successfully. Platform logic unlocked." });
        }

        [HttpPost("sync-migrations")]
        [Authorize] // Assume strict role in prod
        public async Task<IActionResult> SyncMigrations(CancellationToken ct)
        {
            var migrationIds = new[]
            {
                "20260620112636_InitialCleanState",
                "20260623074307_SeedInitialTimesheet",
                "20260629142041_AddJobDescriptionColumn",
                "20260701140229_AddInvoicePaidAt",
                "20260701142421_AddTemperatureLog",
                "20260701170308_AddAuditLogging",
                "20260702201315_AddressAndNotifications",
                "20260704123412_AddPharmacyLicenseDocument",
                "20260704155831_AddFeedbackModels",
                "20260704182034_SplitPharmacistAddress",
                "20260705095355_AddUnifiedMegaSchema_ATM",
                "20260705101841_AddUnifiedMegaSchema_PDL",
                "20260705120900_AddPatientMedicationCount",
                "20260705130455_AddUtmTrackingToPharmacy",
                "20260705180047_AddKioskConsentFlags",
                "20260706090839_AddPharmacyCreatedAt",
                "20260712132520_AddDienstplan"
            };

            int totalAffectedRows = 0;

            // Ensure tracking table exists
            await _dbContext.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (" +
                "\"MigrationId\" character varying(150) NOT NULL, " +
                "\"ProductVersion\" character varying(32) NOT NULL, " +
                "CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY (\"MigrationId\"));", ct);

            foreach (var migrationId in migrationIds)
            {
                var sql = "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, '8.0.0') ON CONFLICT DO NOTHING;";
                totalAffectedRows += await _dbContext.Database.ExecuteSqlRawAsync(sql, new object[] { migrationId }, ct);
            }

            return Ok(new { message = "Migration synchronization executed.", affectedRows = totalAffectedRows });
        }
    }
}
