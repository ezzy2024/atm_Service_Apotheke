using Microsoft.AspNetCore.Mvc;
using ServiceApotheke.API.Data;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _env;

        public DocumentController(DataContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("pharmacist/{id}")]
        public async Task<IActionResult> UploadDocuments(int id, IFormFile? approbation, IFormFile? cv)
        {
            var user = await _context.Pharmacists.FindAsync(id);
            if (user == null) return NotFound(new { message = "Benutzer nicht gefunden." });

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadPath = Path.Combine(webRoot, "uploads", id.ToString());

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            if (approbation != null)
            {
                var appPath = Path.Combine(uploadPath, "approb_" + approbation.FileName);
                using (var stream = new FileStream(appPath, FileMode.Create)) await approbation.CopyToAsync(stream);
                user.ApprobationDocumentPath = $"/uploads/{id}/approb_{approbation.FileName}";
            }

            if (cv != null)
            {
                var cvPath = Path.Combine(uploadPath, "cv_" + cv.FileName);
                using (var stream = new FileStream(cvPath, FileMode.Create)) await cv.CopyToAsync(stream);
                user.CvDocumentPath = $"/uploads/{id}/cv_{cv.FileName}";
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Dokumente hochgeladen." });
        }
        [Authorize]
        [HttpGet("contract/{jobId}")]
        public async Task<IActionResult> GenerateContract(int jobId)
        {
            var userIdStr = User.FindFirstValue("id") ?? User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue("role") ?? User.FindFirstValue(System.Security.Claims.ClaimTypes.Role);

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var jobPost = await _context.JobPosts
                .Include(j => j.Pharmacy)
                .Include(j => j.JobApplications)
                    .ThenInclude(a => a.Pharmacist)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (jobPost == null)
                return NotFound("Job nicht gefunden.");

            var acceptedApp = jobPost.JobApplications.FirstOrDefault(a => a.Status == "Accepted");
            if (acceptedApp == null)
                return BadRequest("Kein Apotheker für diesen Job akzeptiert.");

            if (userRole == "Pharmacy" && jobPost.PharmacyId != userId)
                return Forbid();
            
            if (userRole == "Pharmacist" && acceptedApp.PharmacistId != userId)
                return Forbid();

            var pharmacy = jobPost.Pharmacy;
            var pharmacist = acceptedApp.Pharmacist;

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.PageColor(QuestPDF.Helpers.Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(QuestPDF.Helpers.Fonts.Arial));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("HONORARVERTRAG").FontSize(20).SemiBold().FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);
                            column.Item().Text("für freiberufliche Apotheker").FontSize(14).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                            column.Item().PaddingTop(5).Text($"Datum: {DateTime.UtcNow:dd.MM.yyyy}");
                        });
                    });

                    page.Content().PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre).Column(column =>
                    {
                        column.Spacing(20);
                        column.Item().Text("Zwischen").Bold();
                        column.Item().Text($"{pharmacy.PharmacyName}\n{pharmacy.Street} {pharmacy.HouseNumber}\n{pharmacy.PostalCode} {pharmacy.City}");
                        column.Item().Text("- nachfolgend \"Apotheke\" genannt -").Italic();

                        column.Item().Text("und").Bold();
                        column.Item().Text($"{pharmacist.FullName}\n{pharmacist.Street} {pharmacist.HouseNumber}\n{pharmacist.PostalCode} {pharmacist.City}");
                        column.Item().Text("- nachfolgend \"Vertretungsapotheker\" genannt -").Italic();

                        column.Item().PaddingTop(10).Text("§ 1 Vertragsgegenstand").Bold().FontSize(12);
                        column.Item().Text($"Der Vertretungsapotheker übernimmt in der Zeit vom {jobPost.StartDate:dd.MM.yyyy HH:mm} bis {jobPost.EndDate:dd.MM.yyyy HH:mm} die selbstständige und weisungsfreie Vertretung in der o.g. Apotheke. Grund der Vertretung: {jobPost.ReasonForVacancy}.");

                        column.Item().PaddingTop(10).Text("§ 2 Vergütung").Bold().FontSize(12);
                        column.Item().Text($"Der Vertretungsapotheker erhält für seine Tätigkeit ein Honorar in Höhe von {jobPost.Salary} € pro Stunde. Die Zahlung erfolgt nach Rechnungsstellung ohne Abzug innerhalb von 14 Tagen.");

                        column.Item().PaddingTop(10).Text("§ 3 Status").Bold().FontSize(12);
                        column.Item().Text("Die Parteien sind sich darüber einig, dass der Vertretungsapotheker seine Tätigkeit als freier Mitarbeiter (Selbstständiger) ausübt. Er unterliegt keinem Direktionsrecht der Apotheke und teilt sich seine Arbeitszeit im Rahmen der gesetzlichen Bestimmungen eigenverantwortlich ein.");

                        column.Item().PaddingTop(30).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("______________________________________");
                                col.Item().Text("Unterschrift Apotheke");
                            });
                            
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("______________________________________");
                                col.Item().Text("Unterschrift Vertretungsapotheker");
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Seite ");
                        x.CurrentPageNumber();
                        x.Span(" von ");
                        x.TotalPages();
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Vertrag_Job_{jobId}.pdf");
        }
    }
}
