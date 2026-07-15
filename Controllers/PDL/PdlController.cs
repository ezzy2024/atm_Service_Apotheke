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
        public async Task<IActionResult> IngestExcel([FromBody] List<EncryptedPayloadDto> payloads)
        {
            if (payloads == null || !payloads.Any()) return BadRequest("No payloads provided.");
            
            var userId = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int pharmacyUserId)) return Unauthorized();

            var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.Id == pharmacyUserId);
            if (pharmacy == null) return Unauthorized();

            try
            {
                int processedCount = 0;

                foreach (var payload in payloads)
                {
                    var patient = new Patient
                    {
                        PharmacyId = pharmacy.Id,
                        CiphertextBase64 = payload.CiphertextBase64,
                        IvBase64 = payload.IvBase64
                    };
                    _context.Patients.Add(patient);
                    processedCount++;
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, processed = processedCount });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PdlController Ingest Error] {ex.Message}");
                return BadRequest(new { error = "Fehler bei der Speicherung der verschlüsselten Daten." });
            }
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
                    p.CiphertextBase64,
                    p.IvBase64,
                    HasAnalysis = p.PdlServices.Any(s => s.ServiceType == "POLYMEDIKATION" || s.ServiceType == "AMTS_POLYMEDIKATION"),
                    LatestPdfUrl = p.PdlServices.Where(s => s.ServiceType == "POLYMEDIKATION" || s.ServiceType == "AMTS_POLYMEDIKATION")
                                      .SelectMany(s => _context.PdlDocuments.Where(d => d.PdlServiceId == s.Id))
                                      .Select(d => d.PdfUrl)
                                      .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(patients);
        }

        [HttpPost("analyze/{patientId}")]
        public IActionResult AnalyzePatient(int patientId)
        {
            // The LLM dependencies have been severed for strict Zero-Knowledge E2EE.
            // All AI orchestration must now occur client-side within the React UI.
            return StatusCode(501, "Backend AI Analysis is disabled due to Zero-Knowledge architecture. Execute via client-side Orchestration.");
        }

        [HttpPost("services")]
        public async Task<IActionResult> CreateService([FromBody] PdlServicePayload payload)
        {
            var userId = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out int pharmacyUserId)) return Unauthorized();

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == payload.PatientId && p.PharmacyId == pharmacyUserId);
            if (patient == null) return NotFound("Patient not found.");

            var service = new PdlService 
            {
                PatientId = patient.Id,
                ServiceType = payload.ServiceType,
                Status = "PERFORMED",
                PerformedAt = DateTime.UtcNow,
                // Pack ciphertext into existing JSON column to avoid EF Migration churn
                AiAnalysisResultJson = JsonSerializer.Serialize(new { 
                    ciphertextBase64 = payload.CiphertextBase64, 
                    ivBase64 = payload.IvBase64 
                })
            };
            
            _context.PdlServices.Add(service);
            await _context.SaveChangesAsync();
            
            return Ok(new { success = true, serviceId = service.Id });
        }

        [AllowAnonymous]
        [HttpDelete("test-cleanup")]
        public async Task<IActionResult> TestCleanup()
        {
            var patients = await _context.Patients
                .Where(p => p.CiphertextBase64 == "dGVzdF9jaXBoZXI=")
                .ToListAsync();

            if (patients.Any())
            {
                _context.Patients.RemoveRange(patients);
                await _context.SaveChangesAsync();
                return Ok(new { message = $"Deleted {patients.Count} test rows." });
            }
            return Ok(new { message = "No test rows found." });
        }
    }

    public class PdlServicePayload 
    {
        public int PatientId { get; set; }
        public string ServiceType { get; set; }
        public string CiphertextBase64 { get; set; }
        public string IvBase64 { get; set; }
    }
}
