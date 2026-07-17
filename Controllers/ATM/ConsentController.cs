using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Models.ATM;
using ServiceApotheke.API.Attributes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ServiceApotheke.API.Services;

namespace ServiceApotheke.API.Controllers.ATM
{
    [ApiController]
    [Route("api/atm/consent")]
    [KioskAuthorize] // Enforces x-device-token validation
    public class ConsentController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IGoogleCloudStorageService _storageService;

        public ConsentController(DataContext context, IWebHostEnvironment env, IGoogleCloudStorageService storageService)
        {
            _context = context;
            _env = env;
            _storageService = storageService;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateConsent([FromBody] ConsentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!request.IsTelepharmacyConsentGranted)
            {
                return BadRequest(new { error = "Die Telepharmazie-Einwilligung ist zwingend erforderlich." });
            }

            // Retrieve PharmacyId injected by the KioskAuthorizeFilter
            if (!HttpContext.Items.TryGetValue("KioskPharmacyId", out var pharmacyIdObj) || !(pharmacyIdObj is int pharmacyId))
            {
                return Unauthorized(new { error = "Terminal mapping failed." });
            }

            var consentAgreement = new ConsentAgreement
            {
                PharmacyId = pharmacyId,
                PatientName = request.PatientName,
                HealthInsuranceName = request.HealthInsuranceName,
                HealthInsuranceNumber = request.HealthInsuranceNumber,
                IkNumber = request.IkNumber,
                SignatureBlob = request.SignatureBlob,

                IsWwsExportGranted = request.IsWwsExportGranted,
                SignedDate = DateTime.UtcNow
            };

            // 1. Database Transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.ConsentAgreements.Add(consentAgreement);
                await _context.SaveChangesAsync(); // To get the ConsentAgreement.Id

                var fileName = $"consent_{consentAgreement.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                var pdfBytes = GeneratePdf(request);
                
                using var memoryStream = new MemoryStream(pdfBytes);
                var locator = await _storageService.UploadDocumentAsync(memoryStream, fileName, "application/pdf");

                var billingRecord = new AtmBillingRecord
                {
                    PharmacyId = pharmacyId,
                    ConsentId = consentAgreement.Id,
                    ServiceType = "AMTS",
                    Amount = 15.00m,
                    DateOfService = DateTime.UtcNow,
                    Sonderkennzeichen = "0256789",
                    ReportPath = $"/api/atm/kiosk/download/{locator}"
                };

                _context.AtmBillingRecords.Add(billingRecord);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new 
                { 
                    success = true, 
                    consentId = consentAgreement.Id, 
                    pdfUrl = billingRecord.ReportPath 
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Internal server error during consent processing.", details = ex.Message });
            }
        }

        private byte[] GeneratePdf(ConsentRequest request)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Text("Assistierte Telemedizin (aTM) - Dokumentationssystem")
                        .SemiBold().FontSize(16).FontColor(Colors.Blue.Darken2);

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Item().Text("DSGVO-Anamnese-Protokoll").Bold().FontSize(14).Underline();
                        col.Item().PaddingTop(10).Text($"Erstellungsdatum: {DateTime.Now:dd.MM.yyyy HH:mm}");
                        col.Item().PaddingTop(20).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Text("Patientendaten").Bold();
                        
                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Text($"Name: {request.PatientName}");
                            row.RelativeItem().Text($"Krankenkasse: {request.HealthInsuranceName}");
                        });

                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text($"KVNR: {request.HealthInsuranceNumber}");
                            row.RelativeItem().Text($"IK-Nummer: {request.IkNumber}");
                        });

                        col.Item().PaddingTop(30).Text("Rechtliche Hinweise & Disclaimer").Bold();
                        col.Item().PaddingTop(5).Text("Der Patient hat am Kiosk-Terminal aktiv bestätigt, dass das System keine medizinische Diagnose durchführt und keinen Arztbesuch in Notfällen ersetzt.");
                        col.Item().PaddingTop(5).Text($"Einwilligung zur Telepharmazie (aTM): {(request.IsTelepharmacyConsentGranted ? "Ja" : "Nein")}");
                        col.Item().PaddingTop(5).Text($"Einwilligung zum Apotheken-WWS Datenexport: {(request.IsWwsExportGranted ? "Ja" : "Nein")}");

                        col.Item().PaddingTop(30).Text("Signatur des Patienten").Bold();
                        
                        if (request.SignatureBlob != null && request.SignatureBlob.Length > 0)
                        {
                            col.Item().PaddingTop(10).Width(200).Image(request.SignatureBlob);
                        }
                        else
                        {
                            col.Item().PaddingTop(10).Text("[Keine Signatur hinterlegt]").Italic();
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Seite ");
                        x.CurrentPageNumber();
                        x.Span(" von ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf();
        }
    }

    public class ConsentRequest
    {
        public string PatientName { get; set; }
        public string HealthInsuranceName { get; set; }
        public string HealthInsuranceNumber { get; set; }
        public string IkNumber { get; set; }
        public byte[] SignatureBlob { get; set; }
        public bool IsTelepharmacyConsentGranted { get; set; }
        public bool IsWwsExportGranted { get; set; }
    }
}
