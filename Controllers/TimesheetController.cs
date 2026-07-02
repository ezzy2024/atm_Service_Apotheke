using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Models;
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

        public TimesheetController(DataContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        [HttpGet("pending/pharmacy/{pharmacyId}")]
        public async Task<IActionResult> GetPendingTimesheetsForPharmacy(int pharmacyId)
        {
            var timesheets = await _context.Timesheets
                .Include(t => t.JobApplication!)
                    .ThenInclude(ja => ja.JobPost!)
                .Include(t => t.JobApplication!)
                    .ThenInclude(ja => ja.Pharmacist!)
                .Where(t => t.JobApplication!.JobPost!.PharmacyId == pharmacyId && t.Status == "Submitted")
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
                    totalExpected = ((decimal)(t.ActualEndTime - t.ActualStartTime).TotalHours * t.HourlyRate) + t.TravelCosts + t.AccommodationCosts
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
            _context.Timesheets.Add(timesheet);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Arbeitszeiten erfolgreich eingereicht." });
        }

        [HttpPost("{timesheetId}/approve")]
        public async Task<IActionResult> ApproveTimesheet(int timesheetId)
        {
            var timesheet = await _context.Timesheets.FindAsync(timesheetId);
            if (timesheet == null) return NotFound(new { message = "Timesheet nicht gefunden." });

            if (timesheet.Status == "Approved") return BadRequest(new { message = "Bereits freigegeben." });

            timesheet.Status = "Approved";

            // Calculate total cost
            decimal hours = (decimal)(timesheet.ActualEndTime - timesheet.ActualStartTime).TotalHours;
            decimal totalAmount = (hours * timesheet.HourlyRate) + timesheet.TravelCosts + timesheet.AccommodationCosts;

            // Generate Invoice
            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{timesheetId}",
                TimesheetId = timesheetId,
                Type = "PharmacyInvoice",
                TotalAmount = totalAmount,
                Status = "Unpaid",
                CreatedAt = DateTime.UtcNow
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Zeiten freigegeben und Rechnung erstellt." });
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