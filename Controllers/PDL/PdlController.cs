using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Models.PDL;
using ServiceApotheke.API.Services.PDL;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System;

namespace ServiceApotheke.API.Controllers.PDL
{
    [ApiController]
    [Route("api/pdl")]
    [Authorize(Roles = "Pharmacy")]
    public class PdlController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly AiAnalysisService _aiService;

        public PdlController(DataContext context, AiAnalysisService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        [HttpPost("ingest")]
        public async Task<IActionResult> IngestExcel(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");
            
            var userId = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int pharmacyUserId)) return Unauthorized();

            var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.Id == pharmacyUserId);
            if (pharmacy == null) return Unauthorized();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            // Parse excel dynamically
            var rows = stream.Query().ToList();
            
            // Expected columns: KdnNr, Geburtsjahr, Geschlecht, Medikament
            var groupedPatients = rows.GroupBy(r => {
                var dict = r as IDictionary<string, object>;
                if (dict != null && dict.ContainsKey("KdnNr")) return dict["KdnNr"]?.ToString() ?? "";
                return "";
            }).Where(g => !string.IsNullOrEmpty(g.Key));

            int processedCount = 0;
            int eligibleCount = 0;

            foreach (var group in groupedPatients)
            {
                var kdnNr = group.Key;
                var firstRow = group.First() as IDictionary<string, object>;
                if (firstRow == null) continue;

                int birthYear = 1960;
                if (firstRow.ContainsKey("Geburtsjahr") && firstRow["Geburtsjahr"] != null)
                {
                    if (int.TryParse(firstRow["Geburtsjahr"].ToString(), out int y)) birthYear = y;
                }
                
                string gender = "unbekannt";
                if (firstRow.ContainsKey("Geschlecht") && firstRow["Geschlecht"] != null)
                {
                    gender = firstRow["Geschlecht"].ToString() ?? "unbekannt";
                }
                
                var medications = new List<string>();
                foreach (var r in group)
                {
                    var d = r as IDictionary<string, object>;
                    if (d != null && d.ContainsKey("Medikament") && d["Medikament"] != null)
                    {
                        medications.Add(d["Medikament"].ToString());
                    }
                }

                // Save/Update Patient
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.PharmacyId == pharmacy.Id && p.KdnNr == kdnNr);
                if (patient == null)
                {
                    patient = new Patient
                    {
                        PharmacyId = pharmacy.Id,
                        KdnNr = kdnNr,
                        Geburt = birthYear.ToString(),
                        Gender = gender,
                        Name = "Anonymisiert",
                        Vorname = "Anonymisiert"
                    };
                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();
                }

                // Analyze AMTS if they have >= 5 meds
                if (medications.Count >= 5)
                {
                    // Check if we already did an analysis recently
                    var existingService = await _context.PdlServices.FirstOrDefaultAsync(s => s.PatientId == patient.Id && s.ServiceType == "POLYMEDIKATION");
                    if (existingService == null)
                    {
                        var aiResult = await _aiService.AnalyzePolymedicationAsync(birthYear, gender, medications);

                        bool isEligible = false;
                        try
                        {
                            using var doc = JsonDocument.Parse(aiResult);
                            isEligible = doc.RootElement.GetProperty("isEligibleForPdl").GetBoolean();
                        }
                        catch { }

                        if (isEligible)
                        {
                            eligibleCount++;
                            var service = new PdlService
                            {
                                PatientId = patient.Id,
                                ServiceType = "POLYMEDIKATION",
                                Status = "PLANNED",
                                AiAnalysisResultJson = aiResult
                            };
                            _context.PdlServices.Add(service);
                        }
                    }
                }
                
                processedCount++;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, processed = processedCount, newlyEligible = eligibleCount });
        }

        [HttpGet("services")]
        public async Task<IActionResult> GetServices()
        {
            var userId = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int pharmacyUserId)) return Unauthorized();

            var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.Id == pharmacyUserId);
            if (pharmacy == null) return Unauthorized();

            var services = await _context.PdlServices
                .Include(s => s.Patient)
                .Where(s => s.Patient.PharmacyId == pharmacy.Id)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new {
                    s.Id,
                    s.ServiceType,
                    s.Status,
                    s.CreatedAt,
                    PatientKdnNr = s.Patient.KdnNr,
                    AnalysisSummary = s.AiAnalysisResultJson
                })
                .ToListAsync();

            return Ok(services);
        }
    }
}
