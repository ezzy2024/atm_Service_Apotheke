using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComplianceController : ControllerBase
    {
        private readonly DataContext _context;

        public ComplianceController(DataContext context)
        {
            _context = context;
        }

        [HttpPost("pharmacy/{id}/accept-avv")]
        public async Task<IActionResult> AcceptDataProcessingAgreement(int id)
        {
            var pharmacy = await _context.Pharmacies.FindAsync(id);
            if (pharmacy == null) return NotFound();

            if (pharmacy.DataProcessingAgreementSignedAt != null)
                return BadRequest(new { message = "AV-Vertrag wurde bereits signiert." });

            pharmacy.DataProcessingAgreementSignedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "AV-Vertrag erfolgreich signiert.", 
                timestamp = pharmacy.DataProcessingAgreementSignedAt 
            });
        }

        [HttpDelete("pharmacist/{id}/forget")]
        public async Task<IActionResult> ExecuteRightToBeForgotten(int id)
        {
            var pharmacist = await _context.Pharmacists.FindAsync(id);
            if (pharmacist == null) return NotFound();

            if (pharmacist.GdprAnonymizedAt != null)
                return BadRequest(new { message = "Profil ist bereits anonymisiert." });

            // Hard PII Purge (Art. 17 DSGVO)
            pharmacist.FullName = "Anonymized User";
            pharmacist.Email = $"anonymized_{Guid.NewGuid()}@deleted.local";
            pharmacist.PhoneNumber = "DELETED";
            pharmacist.Address = "DELETED";
            pharmacist.IdCardDocumentPath = null;
            pharmacist.LiabilityInsuranceDocumentPath = null;
            pharmacist.TaxId = null;
            pharmacist.ApprobationDocumentPath = null;
            pharmacist.PasswordHash = "DELETED";
            pharmacist.IsKycVerified = false;
            pharmacist.GdprAnonymizedAt = DateTime.UtcNow;

            // Purge non-financial relational data (Pending/Rejected Applications)
            var purgeableApps = await _context.JobApplications
                .Where(a => a.PharmacistId == id && (a.Status == "Pending" || a.Status == "Rejected"))
                .ToListAsync();
            
            _context.JobApplications.RemoveRange(purgeableApps);

            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Recht auf Vergessenwerden ausgeführt. Personenbezogene Daten wurden irreversibel gelöscht oder anonymisiert." 
            });
        }
    }
}