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
using ServiceApotheke.API.Domain.Constants;

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
                .Where(t => t.Status == TimesheetStatus.Submitted && 
                            t.JobApplication != null && 
                            (t.JobApplication.Status == JobApplicationStatus.Accepted || t.JobApplication.Status == JobApplicationStatus.Completed) &&
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
                timesheet.Status = TimesheetStatus.Approved;
                app.Status = JobApplicationStatus.Completed;

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

                // Upload PDF to GCS with fallback to local storage
                var gcsService = scope.ServiceProvider.GetService<IGoogleCloudStorageService>();
                string fileName = $"invoices/INV-{DateTime.UtcNow:yyyy}-{invoice.Id:D6}.pdf";
                bool uploadedToGcs = false;

                if (gcsService != null)
                {
                    try
                    {
                        using var stream = new System.IO.MemoryStream(pdfBytes);
                        string gcsPath = await gcsService.UploadDocumentAsync(stream, fileName, "application/pdf");
                        invoice.PdfFilePath = gcsPath;
                        uploadedToGcs = true;
                        _logger.LogInformation($"Successfully uploaded PDF to GCS for Invoice {invoice.Id}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"GCS upload failed for Invoice {invoice.Id}, falling back to local storage.");
                    }
                }

                if (!uploadedToGcs)
                {
                    var localDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ServiceApothekeUploads", "invoices");
                    System.IO.Directory.CreateDirectory(localDir);
                    var localPath = System.IO.Path.Combine(localDir, $"INV-{DateTime.UtcNow:yyyy}-{invoice.Id:D6}.pdf");
                    await System.IO.File.WriteAllBytesAsync(localPath, pdfBytes);
                    invoice.PdfFilePath = $"invoices/INV-{DateTime.UtcNow:yyyy}-{invoice.Id:D6}.pdf";
                    _logger.LogInformation($"Successfully saved PDF to local fallback storage for Invoice {invoice.Id}");
                }
                
                await context.SaveChangesAsync();
            }
        }
    }
}
