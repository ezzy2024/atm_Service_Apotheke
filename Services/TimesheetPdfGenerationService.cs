using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ServiceApotheke.API.Models;

namespace ServiceApotheke.API.Services
{
    public class TimesheetPdfGenerationService : IPdfGenerationService
    {
        public Task<(byte[] PdfBytes, string DocumentHash)> GenerateTimesheetAsync(Timesheet timesheet, InternalShift shift)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(x => ComposeContent(x, timesheet, shift));
                    page.Footer().Element(ComposeFooter);
                });
            });

            var pdfBytes = document.GeneratePdf();
            
            // Calculate SHA-256 hash
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(pdfBytes);
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            return Task.FromResult((pdfBytes, hashString));
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("ServiceApotheke Timesheet").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                });
            });
        }

        private void ComposeContent(IContainer container, Timesheet timesheet, InternalShift shift)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Spacing(20);

                column.Item().Text("Pharmacy Details").SemiBold();
                var pharmacyName = shift.Pharmacy?.PharmacyName ?? "Unknown Pharmacy";
                column.Item().Text(pharmacyName);

                column.Item().Text("Locum Details").SemiBold();
                var locumName = shift.Employee != null ? $"{shift.Employee.FirstName} {shift.Employee.LastName}" : "Unknown Locum";
                column.Item().Text(locumName);

                column.Item().Text("Shift Metrics").SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Metric").SemiBold();
                        header.Cell().Text("Value").SemiBold();
                    });

                    table.Cell().Text("Date");
                    table.Cell().Text(timesheet.ActualStartDate.ToString("yyyy-MM-dd"));
                    
                    table.Cell().Text("Start Time");
                    table.Cell().Text(timesheet.ActualStartTime.ToString(@"hh\:mm"));
                    
                    table.Cell().Text("End Time");
                    table.Cell().Text(timesheet.ActualEndTime.ToString(@"hh\:mm"));

                    table.Cell().Text("Hourly Rate");
                    table.Cell().Text($"€ {timesheet.HourlyRate:F2}");

                    var duration = timesheet.ActualEndTime - timesheet.ActualStartTime;
                    if (duration.TotalHours < 0) duration = duration.Add(TimeSpan.FromHours(24));
                    
                    table.Cell().Text("Hours Worked");
                    table.Cell().Text($"{duration.TotalHours:F2}");

                    table.Cell().Text("Total Payout");
                    var payout = (decimal)duration.TotalHours * timesheet.HourlyRate;
                    table.Cell().Text($"€ {payout:F2}");
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(x =>
            {
                x.Span("Page ");
                x.CurrentPageNumber();
                x.Span(" of ");
                x.TotalPages();
            });
        }
    }
}
