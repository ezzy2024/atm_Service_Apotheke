using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using s2industries.ZUGFeRD;

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

            var pdfBytes = Document.Create(container =>
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

                    page.Footer().Element(c => ComposeFooter(c, pharmacist.FullName, $"{pharmacist.Street} {pharmacist.HouseNumber}", $"{pharmacist.PostalCode} {pharmacist.City}", "Bitte kontaktieren Sie mich bei Rückfragen", pharmacist.TaxId ?? "Nicht angegeben", $"IBAN: {pharmacist.Iban ?? "-"} | BIC: {pharmacist.Bic ?? "-"}"));
                });
            }).GeneratePdf();
            
            return pdfBytes;
        }

        public byte[] GenerateZugferdXml(int invoiceId, Models.Timesheet timesheet, Models.Pharmacist pharmacist, string pharmacyName)
        {
            var totalHours = (decimal)(timesheet.ActualEndTime - timesheet.ActualStartTime).TotalHours;
            if (totalHours < 0) totalHours += 24m;
            var laborCost = totalHours * timesheet.HourlyRate;
            var total = laborCost + timesheet.TravelCosts + timesheet.AccommodationCosts;

            var invoiceNumber = $"INV-{DateTime.UtcNow:yyyy}-{invoiceId:D6}";

            var desc = InvoiceDescriptor.CreateInvoice(
                invoiceNumber,
                DateTime.UtcNow,
                CurrencyCodes.EUR,
                $"{invoiceNumber}-P"
            );
            
            desc.SetSeller(pharmacist.FullName, pharmacist.Street, pharmacist.PostalCode, pharmacist.City, CountryCodes.DE, pharmacist.TaxId);
            desc.SetBuyer(pharmacyName, "", "", "", CountryCodes.DE, "");
            
            desc.AddTradeLineItem(
                name: "Apothekerische Dienstleistung",
                description: "Apothekerliche Vertretung",
                unitCode: QuantityCodes.HUR,
                billedQuantity: totalHours,
                netUnitPrice: timesheet.HourlyRate
            );

            // Add travel costs
            if (timesheet.TravelCosts > 0)
            {
                desc.AddTradeLineItem(name: "Reisekosten", description: "Reisekosten", unitCode: QuantityCodes.C62, billedQuantity: 1m, netUnitPrice: timesheet.TravelCosts);
            }

            desc.SetTotals(total, 0, total, total);

            using var stream = new MemoryStream();
            desc.Save(stream, ZUGFeRDVersion.Version23, Profile.Basic);
            return stream.ToArray();
        }

        public byte[] GeneratePlatformCommissionInvoice(int invoiceId, Models.Timesheet timesheet, string pharmacyName, string pharmacyAddress, string contactPerson)
        {
            var totalHours = (decimal)(timesheet.ActualEndTime - timesheet.ActualStartTime).TotalHours;
            if (totalHours < 0) totalHours += 24m;
            var laborCost = totalHours * timesheet.HourlyRate;
            var platformFee = laborCost * 0.15m; // 15% Provision
            var total = platformFee;

            var invoiceNumber = $"SA-PROV-{DateTime.UtcNow:yyyy}-{invoiceId:D6}";

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

                    page.Header().Element(c => ComposeHeader(c, "PROVISIONSRECHNUNG", "ServiceApotheke – Ezzeldin Hassan"));
                    
                    page.Content().Element(content => 
                    {
                        string senderLine = "ServiceApotheke – Ezzeldin Hassan • Musterstr. 1 • 10115 Berlin";
                        ComposeContent(content, senderLine, invoiceNumber, timesheet, pharmacyName, pharmacyAddress, contactPerson, totalHours, laborCost, total, includeTravel: false, includePlatformFee: true);
                    });

                    page.Footer().Element(c => ComposeFooter(c, "ServiceApotheke – Ezzeldin Hassan", "Musterstr. 1", "10115 Berlin", "Vielen Dank für die Nutzung unserer Plattform", "Kleinunternehmerregelung gemäß § 19 UStG", ""));
                });
            }).GeneratePdf();
        }

        private void ComposeHeader(IContainer container, string title, string issuerName)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(title).FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text(issuerName).FontSize(14).FontColor(Colors.Grey.Darken2);
                });
            });
        }

        private void ComposeContent(IContainer container, string senderLine, string invoiceNumber, Models.Timesheet timesheet, string pName, string pAddr, string pContact, decimal hours, decimal labor, decimal total, bool includeTravel, bool includePlatformFee)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Item().Text(senderLine).FontSize(8).Underline().FontColor(Colors.Grey.Darken1);
                column.Item().PaddingTop(5).Text(pName).SemiBold();
                if (!string.IsNullOrEmpty(pContact)) column.Item().Text($"z.Hd. {pContact}");
                column.Item().Text(pAddr);

                column.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem().Text($"Rechnungsnummer: {invoiceNumber}").SemiBold();
                    row.RelativeItem().AlignRight().Text($"Datum: {DateTime.UtcNow:dd.MM.yyyy}");
                });

                column.Item().PaddingTop(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Beschreibung").SemiBold();
                        header.Cell().AlignRight().Text("Menge").SemiBold();
                        header.Cell().AlignRight().Text("Einzelpreis").SemiBold();
                        header.Cell().AlignRight().Text("Gesamt").SemiBold();
                    });

                    if (includePlatformFee)
                    {
                        table.Cell().Text($"Vermittlungsprovision (15% von {labor:C})");
                        table.Cell().AlignRight().Text("1");
                        table.Cell().AlignRight().Text($"{total:C}");
                        table.Cell().AlignRight().Text($"{total:C}");
                    }
                    else
                    {
                        table.Cell().Text($"Apothekerische Dienstleistung ({timesheet.ActualStartDate:dd.MM.yyyy})");
                        table.Cell().AlignRight().Text($"{hours:F2} Std.");
                        table.Cell().AlignRight().Text($"{timesheet.HourlyRate:C}");
                        table.Cell().AlignRight().Text($"{labor:C}");

                        if (includeTravel && timesheet.TravelCosts > 0)
                        {
                            table.Cell().Text("Reisekosten (Pauschale / Beleg)");
                            table.Cell().AlignRight().Text("1");
                            table.Cell().AlignRight().Text($"{timesheet.TravelCosts:C}");
                            table.Cell().AlignRight().Text($"{timesheet.TravelCosts:C}");
                        }
                    }

                    table.Cell().ColumnSpan(4).PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    table.Cell().ColumnSpan(3).AlignRight().PaddingTop(5).Text("Endbetrag:").SemiBold();
                    table.Cell().AlignRight().PaddingTop(5).Text($"{total:C}").SemiBold();
                });
            });
        }

        private void ComposeFooter(IContainer container, string name, string str, string city, string remark, string taxId, string bank)
        {
            container.AlignCenter().Text(t =>
            {
                t.Span($"{remark}\n").FontSize(9);
                t.Span($"{name} • {str} • {city}\n").FontSize(8).FontColor(Colors.Grey.Medium);
                t.Span($"Steuernummer: {taxId}").FontSize(8).FontColor(Colors.Grey.Medium);
                if (!string.IsNullOrEmpty(bank))
                {
                    t.Span($"\nBankverbindung: {bank}").FontSize(8).FontColor(Colors.Grey.Medium);
                }
            });
        }
    }
}