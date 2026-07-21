using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Models;
using ServiceApotheke.API.Data;
using System;
using System.Threading.Tasks;

namespace ServiceApotheke.API.Controllers
{
#if DEBUG
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly DataContext _context;

        public TestController(DataContext context)
        {
            _context = context;
        }

        [HttpPost("seed-invoice")]
        [AllowAnonymous] // Strikt nur für lokales Testing / Staging
        public async Task<IActionResult> SeedCommissionInvoice()
        {
            try {
                // 1. Hole eine Test-Apotheke aus dem K6-Run (z. B. Krefeld-Mitte)
                var testPharmacy = await _context.Pharmacies
                    .FirstOrDefaultAsync(p => p.UtmTerm == "47798");

                if (testPharmacy == null) 
                {
                    testPharmacy = new Pharmacy { 
                        PharmacyName = "Test Apotheke", 
                        Email = "testapo@k6.com", 
                        PasswordHash = "k6test", 
                        PhoneNumber = "123", 
                        Street = "Test", 
                        HouseNumber = "1", 
                        PostalCode = "47798", 
                        City = "Krefeld", 
                        UtmTerm = "47798" 
                    };
                    _context.Pharmacies.Add(testPharmacy);
                    await _context.SaveChangesAsync();
                }

                // Ensure a dummy pharmacist exists
                var dummyPharmacist = await _context.Pharmacists.FirstOrDefaultAsync(p => p.Email == "dummy@test.com");
                if (dummyPharmacist == null)
                {
                    dummyPharmacist = new Pharmacist 
                    { 
                        FullName = "K6 Tester", 
                        Email = "dummy@test.com", 
                        PasswordHash = "dummy", 
                        PhoneNumber = "123", 
                        Street = "Test", 
                        HouseNumber = "1", 
                        PostalCode = "47798", 
                        City = "Krefeld", 
                        Qualification = "Approbation", 
                        WwsProficiency = "Aposoft" 
                    };
                    _context.Pharmacists.Add(dummyPharmacist);
                    await _context.SaveChangesAsync();
                }

                // Create the required object graph for PAC metrics: JobPost -> JobApplication -> Timesheet -> Invoice
                var dummyJob = new JobPost
                {
                    PharmacyId = testPharmacy.Id,
                    Title = "K6 Dummy Shift",
                    Description = "Dummy",
                    StartDate = DateTime.UtcNow.Date.AddDays(1),
                    EndDate = DateTime.UtcNow.Date.AddDays(1),
                    Salary = 50.0m,
                    Status = "Active"
                };
                _context.JobPosts.Add(dummyJob);
                await _context.SaveChangesAsync();

                var dummyApp = new JobApplication
                {
                    JobPostId = dummyJob.Id,
                    PharmacistId = dummyPharmacist.Id,
                    Status = "Accepted",
                    AppliedAt = DateTime.UtcNow
                };
                _context.JobApplications.Add(dummyApp);
                await _context.SaveChangesAsync();

                var dummyTimesheet = new Timesheet
                {
                    JobApplicationId = dummyApp.Id,
                    ActualStartDate = DateTime.UtcNow.Date.AddDays(1),
                    ActualStartTime = new TimeSpan(8, 0, 0),
                    ActualEndTime = new TimeSpan(16, 0, 0),
                    HourlyRate = 50.0m,
                    Status = "Approved"
                };
                _context.Timesheets.Add(dummyTimesheet);
                await _context.SaveChangesAsync();

                // 2. Generiere die fiktive 100 € Provision
                var dummyInvoice = new Invoice
                {
                    TimesheetId = dummyTimesheet.Id,
                    Type = "PlatformCommissionInvoice",
                    InvoiceNumber = "K6-" + Guid.NewGuid().ToString().Substring(0, 8),
                    TotalAmount = 100.00m,
                    Status = "Paid",
                    CreatedAt = DateTime.UtcNow,
                    PdfFilePath = "/uploads/dummy-commission.pdf"
                };

                _context.Invoices.Add(dummyInvoice);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    Message = "100 € Provision erfolgreich gebucht", 
                    PharmacyId = testPharmacy.Id,
                    UtmTerm = testPharmacy.UtmTerm,
                    InvoiceNumber = dummyInvoice.InvoiceNumber
                });
            } catch(Exception ex) {
                return Ok(new { message = ex.ToString() });
            }
        }
    }
#endif
}
