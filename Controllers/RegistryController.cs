using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistryController : ControllerBase
    {
        private readonly DataContext _context;

        public RegistryController(DataContext context)
        {
            _context = context;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query parameter is required.");

            // ILIKE equivalent in EF Core: EF.Functions.ILike (for PostgreSQL). 
            // Since we may run on SQLite locally or Postgres in prod, EF.Functions.Like works cross-platform.
            // For Postgres specifically, Npgsql supports EF.Functions.ILike. 
            // We use Like with lowered inputs to simulate ILIKE across DBs if needed.
            var searchPattern = $"%{query}%";

            var results = await _context.PharmacyRegistries
                .Where(p => EF.Functions.Like(p.Name, searchPattern) || EF.Functions.Like(p.PLZ, searchPattern))
                .Take(10)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Street,
                    p.PLZ,
                    p.City,
                    p.Phone,
                    p.Email
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}
