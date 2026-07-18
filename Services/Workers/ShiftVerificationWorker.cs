using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Models;

namespace ServiceApotheke.API.Services.Workers
{
    public class ShiftVerificationWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ShiftVerificationWorker> _logger;

        public ShiftVerificationWorker(IServiceProvider serviceProvider, ILogger<ShiftVerificationWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ShiftVerificationWorker is starting.");

            // Poll every 1 minute for demonstration purposes
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await VerifyShiftsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing ShiftVerificationWorker.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task VerifyShiftsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var invoiceService = scope.ServiceProvider.GetRequiredService<InvoiceService>();

            // Find timesheets that are "Submitted" for shifts that have ended
            var timesheetsToVerify = await context.Timesheets
                .Include(t => t.JobApplication)
                    .ThenInclude(a => a!.JobPost)
                        .ThenInclude(j => j!.Pharmacy)
                .Include(t => t.JobApplication)
                    .ThenInclude(a => a!.Pharmacist)
                .Where(t => t.Status == "Submitted" && 
                            t.JobApplication != null && 
                            t.JobApplication.Status == "Accepted" &&
                            t.JobApplication.JobPost != null &&
                            t.JobApplication.JobPost.EndDate != null &&
                            t.JobApplication.JobPost.EndDate < DateTime.UtcNow)
                .ToListAsync();

            if (!timesheetsToVerify.Any()) return;

            foreach (var timesheet in timesheetsToVerify)
            {
                var app = timesheet.JobApplication!;
                var pharmacist = app.Pharmacist!;
                var pharmacy = app.JobPost!.Pharmacy!;

                _logger.LogInformation($"Verifying shift for Timesheet ID {timesheet.Id}, JobApplication ID {app.Id}");

                // Update Statuses
                timesheet.Status = "Approved";
                app.Status = "Completed";

                // Generate Invoice ID
                var invoice = new Invoice
                {
                    TimesheetId = timesheet.Id,
                    TotalAmount = ((decimal)(timesheet.ActualEndTime - timesheet.ActualStartTime).TotalHours * timesheet.HourlyRate) + timesheet.TravelCosts + timesheet.AccommodationCosts,
                    Status = "PaidToPharmacist", // Based on existing logic or adjust as needed
                    CreatedAt = DateTime.UtcNow,
                    Type = "PharmacistServiceInvoice"
                };

                context.Invoices.Add(invoice);
                await context.SaveChangesAsync();

                // Generate PDF with Bank Details
                var pdfBytes = invoiceService.GeneratePharmacistServiceInvoice(
                    invoice.Id, 
                    timesheet, 
                    pharmacy.PharmacyName, 
                    $"{pharmacy.Street} {pharmacy.HouseNumber}, {pharmacy.PostalCode} {pharmacy.City}", 
                    pharmacy.ContactPerson ?? "", 
                    pharmacist
                );

                // Upload PDF to GCS
                var gcsService = scope.ServiceProvider.GetService<IGoogleCloudStorageService>();
                if (gcsService != null)
                {
                    string fileName = $"invoices/INV-{DateTime.UtcNow:yyyy}-{invoice.Id:D6}.pdf";
                    using var stream = new System.IO.MemoryStream(pdfBytes);
                    string gcsPath = await gcsService.UploadDocumentAsync(stream, fileName, "application/pdf");
                    invoice.PdfFilePath = gcsPath;
                    await context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogWarning("IGoogleCloudStorageService is not available. Invoice PDF not saved.");
                }
            }
        }
    }
}
