using System;

namespace ServiceApotheke.API.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        
        public int TimesheetId { get; set; }
        public Timesheet? Timesheet { get; set; }
        
        // "PharmacyInvoice" (Plattform -> Apotheke) oder "PharmacistInvoice" (Apotheker -> Plattform)
        public string Type { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        
        // Status: "Unpaid" (Offen), "PaidToPlatform" (Bezahlt an Plattform), "PaidToPharmacist" (Auszahlung erfolgt)
        public string Status { get; set; } = "Unpaid";
        public string PdfFilePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}