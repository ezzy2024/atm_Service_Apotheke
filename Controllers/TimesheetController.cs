using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Models;
using ServiceApotheke.API.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimesheetController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IPdfGenerationService _pdfGenerationService;
        private readonly ICryptographicStorageService _cryptoStorageService;
        private readonly IPaymentService _paymentService;

        public TimesheetController(DataContext context, IPdfGenerationService pdfGenerationService, ICryptographicStorageService cryptoStorageService, IPaymentService paymentService)
        {
            _context = context;
            _pdfGenerationService = pdfGenerationService;
            _cryptoStorageService = cryptoStorageService;
            _paymentService = paymentService;
        }

        [HttpGet("pending/pharmacy/{pharmacyId}")]
        public async Task<IActionResult> GetPendingTimesheetsForPharmacy(int pharmacyId)
        {
            var timesheets = await _context.Timesheets
                .Include(t => t.JobApplication!)
                    .ThenInclude(ja => ja.JobPost!)
                .Include(t => t.JobApplication!)
                    .ThenInclude(ja => ja.Pharmacist!)
                .Where(t => t.JobApplication!.JobPost!.PharmacyId == pharmacyId && (t.Status == "Submitted" || t.Status == "Disputed"))
                .Select(t => new
                {
                    id = t.Id,
                    pharmacistName = t.JobApplication!.Pharmacist!.FullName,
                    date = t.ActualStartDate,
                    startTime = t.ActualStartTime.ToString(@"hh\:mm"),
                    endTime = t.ActualEndTime.ToString(@"hh\:mm"),
                    totalHours = (t.ActualEndTime - t.ActualStartTime).TotalHours,
                    travelCosts = t.TravelCosts,
                    accommodationCosts = t.AccommodationCosts,
                    totalExpected = ((decimal)(t.ActualEndTime - t.ActualStartTime).TotalHours * t.HourlyRate) + t.TravelCosts + t.AccommodationCosts,
                    status = t.Status,
                    disputeReason = t.DisputeReason
                })
                .ToListAsync();

            return Ok(timesheets);
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitTimesheet([FromBody] Timesheet timesheet)
        {
            var application = await _context.JobApplications.FindAsync(timesheet.JobApplicationId);
            if (application == null) return NotFound(new { message = "Bewerbung nicht gefunden." });

            timesheet.Status = "Submitted";
            timesheet.DisputeReason = null;
            timesheet.DisputedAt = null;

            _context.Timesheets.Add(timesheet);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Arbeitszeiten erfolgreich eingereicht.", id = timesheet.Id });
        }

        [HttpPut("{timesheetId}/revise")]
        public async Task<IActionResult> ReviseTimesheet(int timesheetId, [FromBody] Timesheet revisedTimesheet)
        {
            var timesheet = await _context.Timesheets.FindAsync(timesheetId);
            if (timesheet == null) return NotFound(new { message = "Timesheet nicht gefunden." });
            if (timesheet.Status != "Disputed") return BadRequest(new { message = "Nur konfliktbehaftete Stundenzettel können korrigiert werden." });

            timesheet.ActualStartDate = revisedTimesheet.ActualStartDate;
            timesheet.ActualStartTime = revisedTimesheet.ActualStartTime;
            timesheet.ActualEndTime = revisedTimesheet.ActualEndTime;
            timesheet.TravelCosts = revisedTimesheet.TravelCosts;
            timesheet.AccommodationCosts = revisedTimesheet.AccommodationCosts;
            timesheet.Status = "Submitted";
            timesheet.DisputeReason = null;
            timesheet.DisputedAt = null;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Stundenzettel korrigiert und erneut eingereicht." });
        }

        public class DisputeRequest
        {
            public string Reason { get; set; } = string.Empty;
        }

        [HttpPost("{timesheetId}/dispute")]
        public async Task<IActionResult> DisputeTimesheet(int timesheetId, [FromBody] DisputeRequest request)
        {
            var timesheet = await _context.Timesheets.FindAsync(timesheetId);
            if (timesheet == null) return NotFound(new { message = "Timesheet nicht gefunden." });
            if (timesheet.Status == "Approved") return BadRequest(new { message = "Bereits freigegeben." });

            timesheet.Status = "Disputed";
            timesheet.DisputeReason = request.Reason;
            timesheet.DisputedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Stundenzettel abgelehnt. Der Apotheker wurde benachrichtigt." });
        }

        [HttpPost("{timesheetId}/approve")]
        public async Task<IActionResult> ApproveTimesheet(int timesheetId)
        {
            var timesheet = await _context.Timesheets
                .Include(t => t.JobApplication!)
                    .ThenInclude(ja => ja.JobPost!)
                        .ThenInclude(jp => jp.Pharmacy)
                .Include(t => t.JobApplication!)
                    .ThenInclude(ja => ja.Pharmacist)
                .FirstOrDefaultAsync(t => t.Id == timesheetId);

            if (timesheet == null) return NotFound(new { message = "Timesheet nicht gefunden." });
            if (timesheet.Status == "Approved") return BadRequest(new { message = "Bereits freigegeben." });

            timesheet.Status = "Approved";

            var pharmacy = timesheet.JobApplication!.JobPost!.Pharmacy!;
            var pharmacist = timesheet.JobApplication!.Pharmacist!;

            decimal hours = (decimal)(timesheet.ActualEndTime - timesheet.ActualStartTime).TotalHours;
            if (hours < 0) hours += 24m;
            decimal laborCost = hours * timesheet.HourlyRate;
            
            decimal serviceTotalAmount = laborCost + timesheet.TravelCosts + timesheet.AccommodationCosts;
            decimal commissionTotalAmount = laborCost * 0.15m;

            var invoiceService = new InvoiceService();
            var baseInvoicePath = Path.Combine(Path.GetTempPath(), "ServiceApothekeUploads", "invoices");
            if (!Directory.Exists(baseInvoicePath)) Directory.CreateDirectory(baseInvoicePath);

            var serviceInvoice = new Invoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{timesheetId}",
                TimesheetId = timesheetId,
                Type = "PharmacistServiceInvoice",
                TotalAmount = serviceTotalAmount,
                Status = "Unpaid",
                CreatedAt = DateTime.UtcNow
            };
            _context.Invoices.Add(serviceInvoice);
            await _context.SaveChangesAsync(); // Save to get ID

            var servicePdfBytes = invoiceService.GeneratePharmacistServiceInvoice(
                serviceInvoice.Id, timesheet, 
                pharmacy.PharmacyName, $"{pharmacy.Street} {pharmacy.HouseNumber}, {pharmacy.PostalCode} {pharmacy.City}", 
                pharmacy.ContactPerson, pharmacist);
            
            string servicePdfPath = Path.Combine(baseInvoicePath, $"{serviceInvoice.InvoiceNumber}.pdf");
            await System.IO.File.WriteAllBytesAsync(servicePdfPath, servicePdfBytes);
            serviceInvoice.PdfFilePath = $"/invoices/{serviceInvoice.InvoiceNumber}.pdf";

            var commissionInvoice = new Invoice
            {
                InvoiceNumber = $"COM-{DateTime.UtcNow:yyyyMMdd}-{timesheetId}",
                TimesheetId = timesheetId,
                Type = "PlatformCommissionInvoice",
                TotalAmount = commissionTotalAmount,
                Status = "Unpaid",
                CreatedAt = DateTime.UtcNow
            };
            _context.Invoices.Add(commissionInvoice);
            await _context.SaveChangesAsync(); // Save to get ID

            var commissionPdfBytes = invoiceService.GeneratePlatformCommissionInvoice(
                commissionInvoice.Id, timesheet, 
                pharmacy.PharmacyName, $"{pharmacy.Street} {pharmacy.HouseNumber}, {pharmacy.PostalCode} {pharmacy.City}", 
                pharmacy.ContactPerson);
            
            string commissionPdfPath = Path.Combine(baseInvoicePath, $"{commissionInvoice.InvoiceNumber}.pdf");
            await System.IO.File.WriteAllBytesAsync(commissionPdfPath, commissionPdfBytes);
            commissionInvoice.PdfFilePath = $"/invoices/{commissionInvoice.InvoiceNumber}.pdf";

            await _context.SaveChangesAsync();

            return Ok(new { message = "Zeiten freigegeben und AÜG-konforme Rechnungen erstellt." });
        }

        [HttpGet("{timesheetId}")]
        public async Task<IActionResult> GetTimesheet(int timesheetId)
        {
            var timesheet = await _context.Timesheets.FindAsync(timesheetId);
            if (timesheet == null) return NotFound();
            return Ok(timesheet);
        }

        [HttpPost("{id}/finalize")]
        public async Task<IActionResult> FinalizeTimesheet(int id)
        {
            var timesheet = await _context.Timesheets
                .Include(t => t.JobApplication!)
                    .ThenInclude(ja => ja.Pharmacist)
                .Include(t => t.JobApplication!)
                    .ThenInclude(ja => ja.JobPost!)
                        .ThenInclude(jp => jp.Pharmacy)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (timesheet == null) return NotFound(new { message = "Timesheet nicht gefunden." });
            if (timesheet.Status == "Disputed") return BadRequest(new { message = "Konfliktbehaftete Stundenzettel können nicht finalisiert werden." });

            var shift = await _context.InternalShifts
                .Include(s => s.Pharmacy)
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Date.Date == timesheet.ActualStartDate.Date && s.PharmacyId == timesheet.JobApplication!.JobPost!.PharmacyId);
            
            if (shift == null) 
            {
                // Fallback shift model for PDF generation if not linked to an internal shift
                shift = new InternalShift
                {
                    Pharmacy = timesheet.JobApplication!.JobPost!.Pharmacy,
                    Employee = new PharmacyEmployee { FirstName = timesheet.JobApplication!.Pharmacist!.FullName, LastName = "" }
                };
            }

            var (pdfBytes, documentHash) = await _pdfGenerationService.GenerateTimesheetAsync(timesheet, shift);
            
            using var stream = new MemoryStream(pdfBytes);
            var locatorFileName = await _cryptoStorageService.EncryptAndStoreAsync(stream, $"Timesheet_{id}.pdf");

            timesheet.TimesheetPath = locatorFileName;
            timesheet.DigitalSignatureHash = documentHash;
            timesheet.Status = "Approved";

            // Execute Escrow Release
            try
            {
                if (shift.EscrowStatus == "Held" || shift.EscrowStatus == "Pending")
                {
                    var transfer = await _paymentService.ReleaseEscrowAsync(shift, timesheet);
                    shift.StripeTransferId = transfer.Id;
                    shift.EscrowStatus = "Released";
                }
            }
            catch (Exception ex)
            {
                // In production, log this and potentially push to a retry queue or alert Admin
                shift.EscrowStatus = "Failed";
                Console.WriteLine($"[Escrow Release Failed] Shift {shift.Id}: {ex.Message}");
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Timesheet successfully finalized and securely stored.", 
                path = locatorFileName,
                hash = documentHash
            });
        }

        [HttpGet("{applicationId}/download")]
        public async Task<IActionResult> DownloadTimesheet(int applicationId)
        {
            var application = await _context.JobApplications
                .Include(a => a.Pharmacist)
                .Include(a => a.JobPost)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null) return NotFound();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Text("Arbeitsnachweis (Timesheet)")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Darken3);

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                    {
                        x.Spacing(15);
                        x.Item().Text($"Apotheken-ID: {application.JobPost?.PharmacyId.ToString() ?? "Unbekannt"}");
                        x.Item().Text($"Apotheker/in: {application.Pharmacist?.FullName ?? "Unbekannt"}");
                        x.Item().Text($"Bewerbungsdatum: {application.AppliedAt:dd.MM.yyyy}");
                        x.Item().Text($"Status: {application.Status}");
                        
                        x.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        
                        x.Item().PaddingTop(40).Text("Unterschrift Apothekenleitung: ___________________________________");
                        x.Item().PaddingTop(40).Text("Unterschrift Vertretung: ___________________________________");
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("ServiceApotheke | Nachweis generiert am " + DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm") + " | Seite ");
                        x.CurrentPageNumber();
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return File(stream.ToArray(), "application/pdf", $"Timesheet_APP{applicationId}.pdf");
        }
    }
}
