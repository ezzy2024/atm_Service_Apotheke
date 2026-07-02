using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly DataContext _context;

        public InvoiceController(DataContext context)
        {
            _context = context;
        }

        [HttpGet("pharmacy/{pharmacyId}")]
        public async Task<IActionResult> GetInvoicesForPharmacy(int pharmacyId)
        {
            var invoices = await _context.Invoices
                .Include(i => i.Timesheet!)
                    .ThenInclude(t => t.JobApplication!)
                        .ThenInclude(ja => ja.JobPost!)
                .Where(i => i.Timesheet!.JobApplication!.JobPost!.PharmacyId == pharmacyId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return Ok(invoices);
        }
    }
}