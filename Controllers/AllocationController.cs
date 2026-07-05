using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Models;
using ServiceApotheke.API.Services;

namespace ServiceApotheke.API.Controllers
{
    public class ShiftStatusUpdateDto
    {
        public string NewStatus { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AllocationController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly EmailService _emailService;
        private readonly InvoiceService _invoiceService;

        public AllocationController(DataContext context, EmailService emailService, InvoiceService invoiceService)
        {
            _context = context;
            _emailService = emailService;
            _invoiceService = invoiceService;
        }

        [HttpPut("shift/{applicationId}/status")]
        public async Task<IActionResult> UpdateShiftStatus(int applicationId, [FromBody] ShiftStatusUpdateDto dto)
        {
            var application = await _context.JobApplications
                .Include(a => a.JobPost)
                .ThenInclude(jp => jp.Pharmacy)
                .Include(a => a.Pharmacist)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null)
            {
                return NotFound(new { message = "Job application not found." });
            }

            var currentStatus = application.Status;
            var newStatus = dto.NewStatus;

            // 1. State Transition Validation
            bool isValidTransition = (currentStatus == "Pending" && newStatus == "Accepted") ||
                                     (currentStatus == "Accepted" && newStatus == "Completed") ||
                                     (currentStatus == "Completed" && newStatus == "Invoiced");

            if (!isValidTransition)
            {
                return BadRequest(new { message = $"Invalid state transition from '{currentStatus}' to '{newStatus}'." });
            }

            // 2. Perform State Mutation
            application.Status = newStatus;

            if (newStatus == "Accepted")
            {
                // Modify the JobPost to trigger Optimistic Concurrency Control (xmin)
                if (application.JobPost.Status != "Active")
                {
                    return Conflict(new { message = "The shift is no longer active." });
                }
                
                application.JobPost.Status = "Filled";
            }

            Invoice? newInvoice = null;
            Timesheet? timesheet = null;

            if (newStatus == "Invoiced")
            {
                timesheet = await _context.Timesheets.FirstOrDefaultAsync(t => t.JobApplicationId == applicationId);
                if (timesheet == null)
                {
                    return BadRequest(new { message = "Timesheet not found for this completed shift." });
                }

                // Calculate total amount
                var totalHours = (decimal)(timesheet.ActualEndTime - timesheet.ActualStartTime).TotalHours;
                if (totalHours < 0) totalHours += 24m;
                var laborCost = totalHours * timesheet.HourlyRate;
                var platformFee = laborCost * 0.15m;
                var totalAmount = laborCost + platformFee + timesheet.TravelCosts + timesheet.AccommodationCosts;

                newInvoice = new Invoice
                {
                    TimesheetId = timesheet.Id,
                    Type = "PharmacyInvoice",
                    TotalAmount = totalAmount,
                    Status = "Unpaid",
                    CreatedAt = DateTime.UtcNow,
                    PdfFilePath = string.Empty // MVP: No physical persistence
                };
                
                // We generate a temp InvoiceNumber, which we might update after getting the ID.
                newInvoice.InvoiceNumber = $"INV-{DateTime.UtcNow:yyyy}-PENDING";
                _context.Invoices.Add(newInvoice);
            }

            try
            {
                // Synchronous SaveChanges - Commits DB state and generates AuditLogs FIRST
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "Double-booking detected. Another pharmacist has already accepted this shift." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during state transition.", details = ex.Message });
            }

            // 3. Event Notification (SMTP)
            if (newStatus == "Accepted" && application.JobPost?.Pharmacy != null && application.Pharmacist != null)
            {
                var pharmacy = application.JobPost.Pharmacy;
                var pharmacist = application.Pharmacist;

                string pharmacistSubject = $"Schichtbestätigung: {application.JobPost.Title} bei {pharmacy.PharmacyName}";
                string pharmacistMessage = $@"Hallo {pharmacist.FullName},

Ihre Schicht bei {pharmacy.PharmacyName} wurde erfolgreich zugewiesen.
Details: {application.JobPost.Description}

WICHTIGER HINWEIS:
Der Einsatz erfolgt als freier Mitarbeiter (Honorarvertretung). Der Auftragnehmer wird nicht in die Betriebsstruktur eingegliedert und unterliegt keinem fachlichen Weisungsrecht. Die Abführung von Steuern und Sozialabgaben obliegt vollumfänglich dem Auftragnehmer.

Mit freundlichen Grüßen,
ServiceApotheke";

                string pharmacySubject = $"Apotheker zugewiesen: {application.JobPost.Title}";
                string pharmacyMessage = $@"Die Schicht '{application.JobPost.Title}' wurde erfolgreich an {pharmacist.FullName} zugewiesen.

WICHTIGER HINWEIS:
Der Einsatz erfolgt als freier Mitarbeiter (Honorarvertretung). Der Auftragnehmer wird nicht in die Betriebsstruktur eingegliedert und unterliegt keinem fachlichen Weisungsrecht. Die Abführung von Steuern und Sozialabgaben obliegt vollumfänglich dem Auftragnehmer.";

                _ = _emailService.SendEmailAsync(pharmacist.Email, pharmacistSubject, pharmacistMessage);
                _ = _emailService.SendEmailAsync(pharmacy.Email, pharmacySubject, pharmacyMessage);
            }
            else if (newStatus == "Invoiced")
            {
                // The actual invoice generation and PDF storage is handled exclusively by TimesheetController.Approve
                // We just send a notification email here.
                if (application.JobPost?.Pharmacy != null && application.Pharmacist != null)
                {
                    var pharmacy = application.JobPost.Pharmacy;
                    string subject = $"Rechnungen für Einsatz {application.JobPost.Title} verfügbar";
                    string message = $@"Hallo {pharmacy.ContactPerson ?? "Apothekenleitung"},

Ihr Einsatz wurde abgerechnet. Die Service-Rechnung des Apothekers sowie die Provisionsrechnung der Plattform stehen in Ihrem Dashboard zum Download bereit.

Mit freundlichen Grüßen,
Ihr ServiceApotheke Team";

                    _ = _emailService.SendEmailAsync(pharmacy.Email, subject, message);
                }
            }

            return Ok(new { message = "Status updated successfully.", currentStatus = newStatus });
        }
    }
}
