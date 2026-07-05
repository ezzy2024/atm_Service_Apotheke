using System;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ServiceApotheke.API.Models.PDL;
using System.Text.Json;
using System.Linq;

namespace ServiceApotheke.API.Services.PDL
{
    public class PdlReportEngine
    {
        public PdlReportEngine()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public string GeneratePolymedicationReport(PdlService pdlService, string wwwrootPath)
        {
            var pdfPath = Path.Combine(wwwrootPath, "consents", $"PDL_AMTS_{pdlService.Patient.KdnNr}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
            Directory.CreateDirectory(Path.Combine(wwwrootPath, "consents"));

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(x => ComposeContent(x, pdlService));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            })
            .GeneratePdf(pdfPath);

            // return relative path
            return $"/consents/{Path.GetFileName(pdfPath)}";
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("pDL Dokumentationsprotokoll").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text("Erweiterte Medikationsberatung bei Polymedikation (AMTS)").FontSize(14).FontColor(Colors.Grey.Darken2);
                    column.Item().PaddingTop(5).Text($"Ausgestellt am: {DateTime.UtcNow:dd.MM.yyyy}");
                });
            });
        }

        private void ComposeContent(IContainer container, PdlService pdlService)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Spacing(20);

                column.Item().Text("Patienteninformationen").FontSize(14).SemiBold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(120);
                        columns.RelativeColumn();
                    });

                    table.Cell().Text("Kunden-Nr:");
                    table.Cell().Text(pdlService.Patient.KdnNr);

                    table.Cell().Text("Geburtsjahr:");
                    table.Cell().Text(pdlService.Patient.Geburt);

                    table.Cell().Text("Geschlecht:");
                    table.Cell().Text(pdlService.Patient.Gender);
                });

                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                column.Item().Text("KI-Analyse (Gemini-Engine)").FontSize(14).SemiBold();

                try
                {
                    using var doc = JsonDocument.Parse(pdlService.AiAnalysisResultJson);
                    var root = doc.RootElement;
                    
                    var summary = root.GetProperty("summary").GetString();
                    column.Item().Text("Zusammenfassung:").SemiBold();
                    column.Item().Text(summary);

                    column.Item().PaddingTop(10).Text("Gefundene Risiken & Empfehlungen:").SemiBold();
                    
                    var issues = root.GetProperty("issues").EnumerateArray().ToList();
                    if (issues.Any())
                    {
                        foreach (var issue in issues)
                        {
                            var severity = issue.GetProperty("severity").GetString();
                            var description = issue.GetProperty("description").GetString();
                            var recommendation = issue.GetProperty("recommendation").GetString();

                            var color = severity == "HIGH" ? Colors.Red.Darken2 : severity == "MEDIUM" ? Colors.Orange.Darken2 : Colors.Yellow.Darken2;

                            column.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                            {
                                c.Item().Text($"Schweregrad: {severity}").FontColor(color).SemiBold();
                                c.Item().Text($"Problem: {description}");
                                c.Item().Text($"Empfehlung: {recommendation}").Italic();
                            });
                        }
                    }
                    else
                    {
                        column.Item().Text("Keine spezifischen Risiken gefunden.");
                    }
                }
                catch
                {
                    column.Item().Text("Fehler beim Parsen der KI-Analyse.").FontColor(Colors.Red.Medium);
                    column.Item().Text(pdlService.AiAnalysisResultJson);
                }

                column.Item().PaddingTop(30).Text("Apotheker-Bestätigung").FontSize(14).SemiBold();
                column.Item().Text("Die oben genannten Erkenntnisse wurden evaluiert und fließen in die Medikationsberatung ein.");
            });
        }
    }
}
