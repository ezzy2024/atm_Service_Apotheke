using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EarningsController : ControllerBase
    {
        private readonly DataContext _context;

        public EarningsController(DataContext context)
        {
            _context = context;
        }

        [HttpGet("pharmacist/{pharmacistId}")]
        public async Task<IActionResult> GetPharmacistEarnings(int pharmacistId)
        {
            var timesheets = await _context.Timesheets
                .Include(t => t.JobApplication)
                    .ThenInclude(a => a.JobPost)
                        .ThenInclude(j => j.Pharmacy)
                .Where(t => t.JobApplication.PharmacistId == pharmacistId)
                .OrderByDescending(t => t.ActualStartDate)
                .ToListAsync();

            var timesheetIds = timesheets.Select(t => t.Id).ToList();
            var invoices = await _context.Invoices
                .Where(i => timesheetIds.Contains(i.TimesheetId) && i.Type == "PharmacistServiceInvoice")
                .ToListAsync();

            double totalEarnings = 0;
            var history = new List<object>();

            foreach (var t in timesheets)
            {
                var duration = t.ActualEndTime - t.ActualStartTime;
                double hours = duration.TotalHours;
                if (hours < 0) hours += 24;
                double hourlyRate = (double)t.HourlyRate;
                double earning = hours * hourlyRate;
                
                totalEarnings += earning;

                var invoice = invoices.FirstOrDefault(i => i.TimesheetId == t.Id);

                history.Add(new {
                    timesheetId = t.Id,
                    date = t.ActualStartDate.ToString("yyyy-MM-dd"),
                    startTime = t.ActualStartTime.ToString(@"hh\:mm"),
                    endTime = t.ActualEndTime.ToString(@"hh\:mm"),
                    travelCosts = t.TravelCosts,
                    accommodationCosts = t.AccommodationCosts,
                    pharmacyName = t.JobApplication.JobPost.Pharmacy.PharmacyName,
                    hours = Math.Round(hours, 2),
                    hourlyRate = hourlyRate,
                    total = Math.Round(earning, 2),
                    status = t.Status,
                    disputeReason = t.DisputeReason,
                    invoiceId = invoice?.Id
                });
            }

            return Ok(new {
                totalEarnings = Math.Round(totalEarnings, 2),
                pendingPayments = 0, // Placeholder
                history = history
            });
        }
    }
}
