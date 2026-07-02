using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;

namespace ServiceApotheke.API.Services
{
    public class InvoiceService
    {
        public byte[] GeneratePharmacyInvoice(int invoiceId, Models.Timesheet timesheet, string pharmacyName, string pharmacyAddress, string contactPerson)
        {
            var totalHours = (decimal)(timesheet.ActualEndTime - timesheet.ActualStartTime).TotalHours;
            if (totalHours < 0) totalHours += 24m;
            var laborCost = totalHours * timesheet.HourlyRate;
            var platformFee = laborCost * 0.15m; // 15% Provision
            var subTotal = laborCost + platformFee;
            var total = subTotal + timesheet.TravelCosts + timesheet.AccommodationCosts;

            var invoiceNumber = $"INV-{DateTime.UtcNow:yyyy}-{invoiceId:D6}";

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    // DIN 5008 Type B Window Margins:
                    page.MarginTop(2.0f, Unit.Centimetre);
                    page.MarginBottom(2.0f, Unit.Centimetre);
                    page.MarginLeft(2.5f, Unit.Centimetre);
                    page.MarginRight(2.0f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(ComposeHeader);
                    
                    page.Content().Element(content => ComposeContent(content, invoiceNumber, timesheet, pharmacyName, pharmacyAddress, contactPerson, totalHours, laborCost, platformFee, total));

                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf();
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("RECHNUNG").FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
                });
                
                row.ConstantItem(150).AlignRight().Column(col => 
                {
                    var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "Service Apotheke.png");
                    if (File.Exists(logoPath))
                    {
                        col.Item().Image(logoPath);
                    }
                    else
                    {
                        col.Item().Text("Service Apotheke").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                    }
                });
            });
        }

        private void ComposeContent(IContainer container, string invoiceNumber, Models.Timesheet timesheet, string pharmacyName, string pharmacyAddress, string contactPerson, decimal totalHours, decimal laborCost, decimal platformFee, decimal total)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(col =>
            {
                // DIN 5008 Address Window (approx 45mm from top)
                col.Item().PaddingTop(2.5f, Unit.Centimetre).Column(address =>
                {
                    address.Item().Text("Service Apotheke • Karlsplatz 2 • 47798 Krefeld").FontSize(8).FontColor(Colors.Grey.Darken2).Underline();
                    address.Item().PaddingTop(5).Text(pharmacyName).FontSize(11).SemiBold();
                    address.Item().Text($"z. Hd. {contactPerson}").FontSize(11);
                    address.Item().Text(pharmacyAddress).FontSize(11);
                });

                col.Item().PaddingTop(2, Unit.Centimetre).Row(row =>
                {
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text("Rechnungsnummer").SemiBold();
                        inner.Item().Text(invoiceNumber);
                    });
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text("Leistungsdatum").SemiBold();
                        inner.Item().Text($"{timesheet.ActualStartDate:dd.MM.yyyy}");
                    });
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text("Rechnungsdatum").SemiBold();
                        inner.Item().Text($"{DateTime.UtcNow:dd.MM.yyyy}");
                    });
                });

                col.Item().PaddingTop(30).EnsureSpace().Element(e => ComposeTable(e, timesheet, totalHours, laborCost, platformFee));

                // Totals & Fiscal Compliance
                col.Item().EnsureSpace().PaddingTop(20).Column(inner =>
                {
                    inner.Item().AlignRight().Text($"Gesamtbetrag: {total:F2} €").FontSize(14).SemiBold();
                    
                    inner.Item().PaddingTop(20).Text("Hinweis zur Umsatzsteuer:").SemiBold();
                    inner.Item().Text("Umsatzsteuerfrei nach § 4 Nr. 14 UStG (Heilkundliche Leistungen).").FontSize(10);
                });
            });
        }

        private void ComposeTable(IContainer container, Models.Timesheet timesheet, decimal totalHours, decimal laborCost, decimal platformFee)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30); // Pos.
                    columns.ConstantColumn(80); // Datum
                    columns.ConstantColumn(80); // Zeitraum
                    columns.RelativeColumn();   // Beschreibung
                    columns.ConstantColumn(60); // Menge
                    columns.ConstantColumn(70); // Einzelpreis
                    columns.ConstantColumn(70); // Betrag
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Pos.");
                    header.Cell().Element(CellStyle).Text("Datum");
                    header.Cell().Element(CellStyle).Text("Zeitraum");
                    header.Cell().Element(CellStyle).Text("Beschreibung");
                    header.Cell().Element(CellStyle).AlignRight().Text("Menge");
                    header.Cell().Element(CellStyle).AlignRight().Text("Einzelpreis");
                    header.Cell().Element(CellStyle).AlignRight().Text("Betrag");

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                    }
                });

                static IContainer BlockStyle(IContainer container)
                {
                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }

                // Row 1: Labor
                table.Cell().Element(BlockStyle).Text("1");
                table.Cell().Element(BlockStyle).Text($"{timesheet.ActualStartDate:dd.MM.yyyy}");
                table.Cell().Element(BlockStyle).Text($"{timesheet.ActualStartTime:hh\\:mm} - {timesheet.ActualEndTime:hh\\:mm}");
                table.Cell().Element(BlockStyle).Text("Vertretungseinsatz Apotheker/in");
                table.Cell().Element(BlockStyle).AlignRight().Text($"{totalHours:F1} Std.");
                table.Cell().Element(BlockStyle).AlignRight().Text($"{timesheet.HourlyRate:F2} €");
                table.Cell().Element(BlockStyle).AlignRight().Text($"{laborCost:F2} €");

                // Row 2: Travel Costs
                table.Cell().Element(BlockStyle).Text("2");
                table.Cell().Element(BlockStyle).Text("");
                table.Cell().Element(BlockStyle).Text("");
                table.Cell().Element(BlockStyle).Text("Fahrtkosten");
                table.Cell().Element(BlockStyle).AlignRight().Text("");
                table.Cell().Element(BlockStyle).AlignRight().Text("");
                table.Cell().Element(BlockStyle).AlignRight().Text($"{timesheet.TravelCosts:F2} €");

                // Row 3: Accommodation
                table.Cell().Element(BlockStyle).Text("3");
                table.Cell().Element(BlockStyle).Text("");
                table.Cell().Element(BlockStyle).Text("");
                table.Cell().Element(BlockStyle).Text("Unterkunftskosten");
                table.Cell().Element(BlockStyle).AlignRight().Text("");
                table.Cell().Element(BlockStyle).AlignRight().Text("");
                table.Cell().Element(BlockStyle).AlignRight().Text($"{timesheet.AccommodationCosts:F2} €");

                // Row 4: Service Fee
                table.Cell().Element(BlockStyle).Text("4");
                table.Cell().Element(BlockStyle).Text("");
                table.Cell().Element(BlockStyle).Text("");
                table.Cell().Element(BlockStyle).Text("Vermittlungs-/Servicegebühr");
                table.Cell().Element(BlockStyle).AlignRight().Text("");
                table.Cell().Element(BlockStyle).AlignRight().Text("");
                table.Cell().Element(BlockStyle).AlignRight().Text($"{platformFee:F2} €");
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Service Apotheke").SemiBold();
                        c.Item().Text("Ezzeldin Hassan");
                        c.Item().Text("Karlsplatz 2");
                        c.Item().Text("47798 Krefeld");
                    });
                    
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Bankverbindung").SemiBold();
                        c.Item().Text("Bank: N26");
                        c.Item().Text("IBAN: DE79 1001 1001 2692 4103 20");
                        c.Item().Text("BIC: NTSBDEB1XXX");
                    });

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Kontakt & Rechtliches").SemiBold();
                        c.Item().Text("Email: team@serviceapotheke.tech");
                        c.Item().Text("Steuernummer: 11750863892");
                        c.Item().Text("Zahlbar ohne Abzug innerhalb 14 Tagen");
                    });
                });
            });
        }
    }
}