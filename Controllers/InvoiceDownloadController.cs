using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceDownloadController : ControllerBase
    {
        private readonly DataContext _context;

        public InvoiceDownloadController(DataContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var token = Request.Query["token"].ToString();
            
            if (string.IsNullOrEmpty(token))
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ")) 
                {
                    token = authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            if (string.IsNullOrEmpty(token))
                return Unauthorized("Missing or invalid token.");
            int userId;

            try 
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
                
                if (!int.TryParse(userIdClaim, out userId)) 
                    return Unauthorized("Invalid token payload: missing 'id' claim.");
            } 
            catch 
            {
                return Unauthorized("Token validation failed.");
            }

            var invoice = await _context.Invoices
                .Include(i => i.Timesheet!)
                    .ThenInclude(t => t.JobApplication!)
                        .ThenInclude(ja => ja.JobPost!)
                            .ThenInclude(jp => jp.Pharmacy)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) 
                return NotFound("Invoice not found.");

            bool isPharmacyOwner = invoice.Timesheet?.JobApplication?.JobPost?.PharmacyId == userId;
            bool isPharmacist = invoice.Timesheet?.JobApplication?.PharmacistId == userId;

            if (!isPharmacyOwner && !isPharmacist) 
                return StatusCode(403, "Access denied to this document.");

            if (invoice.Timesheet != null && invoice.Timesheet.JobApplication == null) {
                invoice.Timesheet.JobApplication = await _context.JobApplications.FindAsync(invoice.Timesheet.JobApplicationId);
            }
            if (invoice.Timesheet?.JobApplication != null && invoice.Timesheet.JobApplication.JobPost == null) {
                invoice.Timesheet.JobApplication.JobPost = await _context.JobPosts.FindAsync(invoice.Timesheet.JobApplication.JobPostId);
            }
            if (invoice.Timesheet?.JobApplication?.JobPost != null && invoice.Timesheet.JobApplication.JobPost.Pharmacy == null) {
                invoice.Timesheet.JobApplication.JobPost.Pharmacy = await _context.Pharmacies.FindAsync(invoice.Timesheet.JobApplication.JobPost.PharmacyId);
            }

            var pharmacyName = invoice.Timesheet?.JobApplication?.JobPost?.Pharmacy?.PharmacyName;
            if (string.IsNullOrWhiteSpace(pharmacyName)) pharmacyName = "Unbekannt";
            
            var pharmacyAddress = invoice.Timesheet?.JobApplication?.JobPost?.Pharmacy?.Street + " " + invoice.Timesheet?.JobApplication?.JobPost?.Pharmacy?.HouseNumber + ", " + invoice.Timesheet?.JobApplication?.JobPost?.Pharmacy?.PostalCode + " " + invoice.Timesheet?.JobApplication?.JobPost?.Pharmacy?.City;
            if (string.IsNullOrWhiteSpace(pharmacyAddress)) pharmacyAddress = "Unbekannt";
            
            var contactPerson = invoice.Timesheet?.JobApplication?.JobPost?.Pharmacy?.ContactPerson;
            if (string.IsNullOrWhiteSpace(contactPerson)) contactPerson = "Unbekannt";

            var invoiceService = new ServiceApotheke.API.Services.InvoiceService();
            byte[] pdfBytes;

            if (!string.IsNullOrEmpty(invoice.PdfFilePath))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", invoice.PdfFilePath.TrimStart('/').Replace("/", "\\"));
                if (System.IO.File.Exists(filePath))
                {
                    pdfBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                    return File(pdfBytes, "application/pdf", $"Rechnung_{invoice.InvoiceNumber}.pdf");
                }
            }

            if (invoice.Timesheet != null && invoice.Timesheet.JobApplication != null && invoice.Timesheet.JobApplication.Pharmacist == null) {
                invoice.Timesheet.JobApplication.Pharmacist = await _context.Pharmacists.FindAsync(invoice.Timesheet.JobApplication.PharmacistId);
            }
            var pharmacist = invoice.Timesheet?.JobApplication?.Pharmacist;

            if (invoice.Type == "PharmacistServiceInvoice" && pharmacist != null)
            {
                pdfBytes = invoiceService.GeneratePharmacistServiceInvoice(invoice.Id, invoice.Timesheet, pharmacyName, pharmacyAddress, contactPerson, pharmacist);
            }
            else
            {
                pdfBytes = invoiceService.GeneratePlatformCommissionInvoice(invoice.Id, invoice.Timesheet, pharmacyName, pharmacyAddress, contactPerson);
            }

            return File(pdfBytes, "application/pdf", $"Rechnung_{invoice.InvoiceNumber}.pdf");
        }

        [HttpGet("{id}/zugferd")]
        public async Task<IActionResult> DownloadZugferdXml(int id)
        {
            var token = Request.Query["token"].ToString();
            
            if (string.IsNullOrEmpty(token))
            {
                var authHeader = Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ")) 
                {
                    token = authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            if (string.IsNullOrEmpty(token))
                return Unauthorized("Missing or invalid token.");
            int userId;

            try 
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
                
                if (!int.TryParse(userIdClaim, out userId)) 
                    return Unauthorized("Invalid token payload: missing 'id' claim.");
            } 
            catch 
            {
                return Unauthorized("Token validation failed.");
            }

            var invoice = await _context.Invoices
                .Include(i => i.Timesheet!)
                    .ThenInclude(t => t.JobApplication!)
                        .ThenInclude(ja => ja.JobPost!)
                            .ThenInclude(jp => jp.Pharmacy)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) 
                return NotFound("Invoice not found.");

            bool isPharmacyOwner = invoice.Timesheet?.JobApplication?.JobPost?.PharmacyId == userId;
            bool isPharmacist = invoice.Timesheet?.JobApplication?.PharmacistId == userId;

            if (!isPharmacyOwner && !isPharmacist) 
                return StatusCode(403, "Access denied to this document.");

            if (invoice.Type != "PharmacistServiceInvoice")
            {
                return BadRequest("ZUGFeRD is only available for PharmacistServiceInvoice.");
            }

            if (invoice.Timesheet != null && invoice.Timesheet.JobApplication == null) {
                invoice.Timesheet.JobApplication = await _context.JobApplications.FindAsync(invoice.Timesheet.JobApplicationId);
            }
            if (invoice.Timesheet?.JobApplication != null && invoice.Timesheet.JobApplication.JobPost == null) {
                invoice.Timesheet.JobApplication.JobPost = await _context.JobPosts.FindAsync(invoice.Timesheet.JobApplication.JobPostId);
            }
            if (invoice.Timesheet?.JobApplication?.JobPost != null && invoice.Timesheet.JobApplication.JobPost.Pharmacy == null) {
                invoice.Timesheet.JobApplication.JobPost.Pharmacy = await _context.Pharmacies.FindAsync(invoice.Timesheet.JobApplication.JobPost.PharmacyId);
            }

            var pharmacyName = invoice.Timesheet?.JobApplication?.JobPost?.Pharmacy?.PharmacyName;
            if (string.IsNullOrWhiteSpace(pharmacyName)) pharmacyName = "Unbekannt";
            
            if (invoice.Timesheet != null && invoice.Timesheet.JobApplication != null && invoice.Timesheet.JobApplication.Pharmacist == null) {
                invoice.Timesheet.JobApplication.Pharmacist = await _context.Pharmacists.FindAsync(invoice.Timesheet.JobApplication.PharmacistId);
            }
            var pharmacist = invoice.Timesheet?.JobApplication?.Pharmacist;

            if (pharmacist == null)
            {
                return BadRequest("Pharmacist data is missing.");
            }

            var invoiceService = new ServiceApotheke.API.Services.InvoiceService();
            var xmlBytes = invoiceService.GenerateZugferdXml(invoice.Id, invoice.Timesheet!, pharmacist, pharmacyName);

            return File(xmlBytes, "application/xml", $"Rechnung_{invoice.InvoiceNumber}_ZUGFeRD.xml");
        }
    }
}
