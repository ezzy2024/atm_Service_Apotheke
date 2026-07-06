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
using Microsoft.AspNetCore.Hosting;

namespace ServiceApotheke.API.Controllers.PDL
{
    [ApiController]
    [Route("api/pdl")]
    [Authorize(Roles = "Pharmacy")]
    public class PdlController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly AiAnalysisService _aiService;
        private readonly PdlReportEngine _reportEngine;
        private readonly IWebHostEnvironment _env;

        public PdlController(DataContext context, AiAnalysisService aiService, PdlReportEngine reportEngine, IWebHostEnvironment env)
        {
            _context = context;
            _aiService = aiService;
            _reportEngine = reportEngine;
            _env = env;
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

            var rows = stream.Query(useHeaderRow: true).ToList();
            
            var groupedPatients = rows.GroupBy(r => {
                var dict = r as IDictionary<string, object>;
                if (dict != null) {
                    if (dict.ContainsKey("KdnNr")) return dict["KdnNr"]?.ToString() ?? "";
                    if (dict.ContainsKey("PatientId_Hash")) return dict["PatientId_Hash"]?.ToString() ?? "";
                }
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
                    if (d != null)
                    {
                        var medName = "";
                        if (d.ContainsKey("Medikament") && d["Medikament"] != null) medName = d["Medikament"].ToString();
                        else if (d.ContainsKey("MedicationName") && d["MedicationName"] != null) medName = d["MedicationName"].ToString();

                        if (!string.IsNullOrEmpty(medName)) medications.Add(medName);
                    }
                }

                bool isEligible = medications.Count >= 5;
                if (isEligible) eligibleCount++;

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
                        Vorname = "Anonymisiert",
                        MedicationCount = medications.Count,
                        IsEligibleForAmts = isEligible,
                        MedicationsJson = JsonSerializer.Serialize(medications)
                    };
                    _context.Patients.Add(patient);
                }
                else
                {
                    patient.Geburt = birthYear.ToString();
                    patient.Gender = gender;
                    patient.MedicationCount = medications.Count;
                    patient.IsEligibleForAmts = isEligible;
                    patient.MedicationsJson = JsonSerializer.Serialize(medications);
                    _context.Patients.Update(patient);
                }
                
                processedCount++;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, processed = processedCount, newlyEligible = eligibleCount });
        }

        [HttpGet("patients")]
        public async Task<IActionResult> GetPatients()
        {
            var userId = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int pharmacyUserId)) return Unauthorized();

            var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.Id == pharmacyUserId);
            if (pharmacy == null) return Unauthorized();

            var patients = await _context.Patients
                .Include(p => p.PdlServices)
                .Where(p => p.PharmacyId == pharmacy.Id)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new {
                    p.Id,
                    p.KdnNr,
                    p.Geburt,
                    p.Gender,
                    p.MedicationCount,
                    p.IsEligibleForAmts,
                    HasAnalysis = p.PdlServices.Any(s => s.ServiceType == "POLYMEDIKATION"),
                    LatestPdfUrl = p.PdlServices.Where(s => s.ServiceType == "POLYMEDIKATION")
                                      .SelectMany(s => _context.PdlDocuments.Where(d => d.PdlServiceId == s.Id))
                                      .Select(d => d.PdfUrl)
                                      .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(patients);
        }

        [HttpPost("analyze/{patientId}")]
        public async Task<IActionResult> AnalyzePatient(int patientId)
        {
            var userId = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int pharmacyUserId)) return Unauthorized();

            var patient = await _context.Patients
                .Include(p => p.Pharmacy)
                .FirstOrDefaultAsync(p => p.Id == patientId && p.Pharmacy.Id == pharmacyUserId);
                
            if (patient == null) return NotFound("Patient not found.");
            if (!patient.IsEligibleForAmts) return BadRequest("Patient is not eligible for AMTS.");

            var existingService = await _context.PdlServices.FirstOrDefaultAsync(s => s.PatientId == patient.Id && s.ServiceType == "POLYMEDIKATION");
            
            var medications = JsonSerializer.Deserialize<List<string>>(patient.MedicationsJson) ?? new List<string>();
            int birthYear = int.TryParse(patient.Geburt, out int y) ? y : 1960;

            // Generate AI Analysis
            var aiResult = await _aiService.AnalyzePolymedicationAsync(birthYear, patient.Gender, medications);

            PdlService service;
            if (existingService == null)
            {
                service = new PdlService
                {
                    PatientId = patient.Id,
                    ServiceType = "POLYMEDIKATION",
                    Status = "PERFORMED",
                    AiAnalysisResultJson = aiResult,
                    PerformedAt = DateTime.UtcNow
                };
                _context.PdlServices.Add(service);
                await _context.SaveChangesAsync(); // save to get ID
            }
            else
            {
                service = existingService;
                service.AiAnalysisResultJson = aiResult;
                service.Status = "PERFORMED";
                service.PerformedAt = DateTime.UtcNow;
                _context.PdlServices.Update(service);
                await _context.SaveChangesAsync();
            }

            // Generate QuestPDF Report
            var pdfRelativePath = _reportEngine.GeneratePolymedicationReport(service, _env.WebRootPath);

            var doc = new PdlDocument
            {
                PatientId = patient.Id,
                PharmacyId = patient.PharmacyId,
                PdlServiceId = service.Id,
                PdfUrl = pdfRelativePath,
                BillingAmount = 90.00m // standard fee
            };
            _context.PdlDocuments.Add(doc);
            
            service.Status = "BILLED";
            service.BilledAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, pdfUrl = pdfRelativePath });
        }
    }
}
