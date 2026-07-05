using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;

namespace ServiceApotheke.API.Services
{
    public class InvoiceService
    {
        public byte[] GeneratePharmacistServiceInvoice(int invoiceId, Models.Timesheet timesheet, string pharmacyName, string pharmacyAddress, string contactPerson, Models.Pharmacist pharmacist)
        {
            var totalHours = (decimal)(timesheet.ActualEndTime - timesheet.ActualStartTime).TotalHours;
            if (totalHours < 0) totalHours += 24m;
            var laborCost = totalHours * timesheet.HourlyRate;
            var total = laborCost + timesheet.TravelCosts + timesheet.AccommodationCosts;

            var invoiceNumber = $"INV-{DateTime.UtcNow:yyyy}-{invoiceId:D6}";

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(2.0f, Unit.Centimetre);
                    page.MarginBottom(2.0f, Unit.Centimetre);
                    page.MarginLeft(2.5f, Unit.Centimetre);
                    page.MarginRight(2.0f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(c => ComposeHeader(c, "RECHNUNG", pharmacist.FullName));
                    
                    page.Content().Element(content => 
                    {
                        string senderLine = $"{pharmacist.FullName} • {pharmacist.Street} {pharmacist.HouseNumber} • {pharmacist.PostalCode} {pharmacist.City}";
                        ComposeContent(content, senderLine, invoiceNumber, timesheet, pharmacyName, pharmacyAddress, contactPerson, totalHours, laborCost, total, includeTravel: true, includePlatformFee: false);
                    });

                    page.Footer().Element(c => ComposeFooter(c, pharmacist.FullName, $"{pharmacist.Street} {pharmacist.HouseNumber}", $"{pharmacist.PostalCode} {pharmacist.City}", "Bitte kontaktieren Sie mich bei Rückfragen", pharmacist.TaxId ?? "Nicht angegeben", ""));
                });
            }).GeneratePdf();
        }

        public byte[] GeneratePlatformCommissionInvoice(int invoiceId, Models.Timesheet timesheet, string pharmacyName, string pharmacyAddress, string contactPerson)
        {
            var totalHours = (decimal)(timesheet.ActualEndTime - timesheet.ActualStartTime).TotalHours;
            if (totalHours < 0) totalHours += 24m;
            var laborCost = totalHours * timesheet.HourlyRate;
            var platformFee = laborCost * 0.15m; // 15% Provision
            var total = platformFee;

            var invoiceNumber = $"COM-{DateTime.UtcNow:yyyy}-{invoiceId:D6}";

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(2.0f, Unit.Centimetre);
                    page.MarginBottom(2.0f, Unit.Centimetre);
                    page.MarginLeft(2.5f, Unit.Centimetre);
                    page.MarginRight(2.0f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(c => ComposeHeader(c, "PROVISIONSRECHNUNG", "Service Apotheke"));
                    
                    page.Content().Element(content => 
                    {
                        string senderLine = "Service Apotheke • Karlsplatz 2 • 47798 Krefeld";
                        ComposeContent(content, senderLine, invoiceNumber, timesheet, pharmacyName, pharmacyAddress, contactPerson, totalHours, laborCost, total, includeTravel: false, includePlatformFee: true);
                    });

                    page.Footer().Element(c => ComposeFooter(c, "Service Apotheke", "Karlsplatz 2", "47798 Krefeld", "Bank: N26\nIBAN: DE79 1001 1001 2692 4103 20", "11750863892", "team@serviceapotheke.tech"));
                });
            }).GeneratePdf();
        }

        private void ComposeHeader(IContainer container, string title, string senderName)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(title).FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
                });
                
                row.ConstantItem(200).AlignRight().Column(col => 
                {
                    col.Item().Text(senderName).FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                });
            });
        }

        private void ComposeContent(IContainer container, string senderLine, string invoiceNumber, Models.Timesheet timesheet, string pharmacyName, string pharmacyAddress, string contactPerson, decimal totalHours, decimal laborCost, decimal total, bool includeTravel, bool includePlatformFee)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(col =>
            {
                col.Item().PaddingTop(2.5f, Unit.Centimetre).Column(address =>
                {
                    address.Item().Text(senderLine).FontSize(8).FontColor(Colors.Grey.Darken2).Underline();
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

                col.Item().PaddingTop(30).EnsureSpace().Element(e => ComposeTable(e, timesheet, totalHours, laborCost, includeTravel, includePlatformFee));

                col.Item().EnsureSpace().PaddingTop(20).Column(inner =>
                {
                    inner.Item().AlignRight().Text($"Gesamtbetrag: {total:F2} €").FontSize(14).SemiBold();
                    
                    inner.Item().PaddingTop(20).Text("Hinweis zur Umsatzsteuer:").SemiBold();
                    inner.Item().Text("Umsatzsteuerfrei nach § 4 Nr. 14 UStG bzw. § 19 UStG (Kleinunternehmerregelung), sofern nicht anders vereinbart.").FontSize(10);
                });
            });
        }

        private void ComposeTable(IContainer container, Models.Timesheet timesheet, decimal totalHours, decimal laborCost, bool includeTravel, bool includePlatformFee)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30); 
                    columns.ConstantColumn(80); 
                    columns.ConstantColumn(80); 
                    columns.RelativeColumn();   
                    columns.ConstantColumn(60); 
                    columns.ConstantColumn(70); 
                    columns.ConstantColumn(70); 
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

                int pos = 1;

                if (!includePlatformFee)
                {
                    table.Cell().Element(BlockStyle).Text(pos.ToString());
                    table.Cell().Element(BlockStyle).Text($"{timesheet.ActualStartDate:dd.MM.yyyy}");
                    table.Cell().Element(BlockStyle).Text($"{timesheet.ActualStartTime:hh\\:mm} - {timesheet.ActualEndTime:hh\\:mm}");
                    table.Cell().Element(BlockStyle).Text("Vertretungseinsatz Apotheker/in");
                    table.Cell().Element(BlockStyle).AlignRight().Text($"{totalHours:F1} Std.");
                    table.Cell().Element(BlockStyle).AlignRight().Text($"{timesheet.HourlyRate:F2} €");
                    table.Cell().Element(BlockStyle).AlignRight().Text($"{laborCost:F2} €");
                    pos++;
                }

                if (includeTravel)
                {
                    if (timesheet.TravelCosts > 0)
                    {
                        table.Cell().Element(BlockStyle).Text(pos.ToString());
                        table.Cell().Element(BlockStyle).Text("");
                        table.Cell().Element(BlockStyle).Text("");
                        table.Cell().Element(BlockStyle).Text("Fahrtkosten");
                        table.Cell().Element(BlockStyle).AlignRight().Text("");
                        table.Cell().Element(BlockStyle).AlignRight().Text("");
                        table.Cell().Element(BlockStyle).AlignRight().Text($"{timesheet.TravelCosts:F2} €");
                        pos++;
                    }

                    if (timesheet.AccommodationCosts > 0)
                    {
                        table.Cell().Element(BlockStyle).Text(pos.ToString());
                        table.Cell().Element(BlockStyle).Text("");
                        table.Cell().Element(BlockStyle).Text("");
                        table.Cell().Element(BlockStyle).Text("Unterkunftskosten");
                        table.Cell().Element(BlockStyle).AlignRight().Text("");
                        table.Cell().Element(BlockStyle).AlignRight().Text("");
                        table.Cell().Element(BlockStyle).AlignRight().Text($"{timesheet.AccommodationCosts:F2} €");
                        pos++;
                    }
                }

                if (includePlatformFee)
                {
                    var platformFee = laborCost * 0.15m;
                    table.Cell().Element(BlockStyle).Text(pos.ToString());
                    table.Cell().Element(BlockStyle).Text($"{timesheet.ActualStartDate:dd.MM.yyyy}");
                    table.Cell().Element(BlockStyle).Text("");
                    table.Cell().Element(BlockStyle).Text("Vermittlungs-/Servicegebühr (15% auf Honorar)");
                    table.Cell().Element(BlockStyle).AlignRight().Text("");
                    table.Cell().Element(BlockStyle).AlignRight().Text("");
                    table.Cell().Element(BlockStyle).AlignRight().Text($"{platformFee:F2} €");
                }
            });
        }

        private void ComposeFooter(IContainer container, string name, string street, string city, string bankInfo, string taxId, string email)
        {
            container.Column(col =>
            {
                col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(name).SemiBold();
                        c.Item().Text(street);
                        c.Item().Text(city);
                    });
                    
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Bankverbindung").SemiBold();
                        c.Item().Text(bankInfo);
                    });

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Kontakt & Rechtliches").SemiBold();
                        if (!string.IsNullOrEmpty(email)) c.Item().Text($"Email: {email}");
                        c.Item().Text($"Steuernummer: {taxId}");
                        c.Item().Text("Zahlbar ohne Abzug innerhalb 14 Tagen");
                    });
                });
            });
        }
    }
}