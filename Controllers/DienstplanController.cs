using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DienstplanController : ControllerBase
    {
        private readonly DataContext _context;

        public DienstplanController(DataContext context)
        {
            _context = context;
        }

        [HttpGet("pharmacy/{pharmacyId}/employees")]
        public async Task<IActionResult> GetEmployees(int pharmacyId)
        {
            var employees = await _context.PharmacyEmployees
                .Where(e => e.PharmacyId == pharmacyId)
                .ToListAsync();
            return Ok(employees);
        }

        [HttpPost("pharmacy/{pharmacyId}/employees")]
        public async Task<IActionResult> AddEmployee(int pharmacyId, [FromBody] PharmacyEmployee employee)
        {
            employee.PharmacyId = pharmacyId;
            _context.PharmacyEmployees.Add(employee);
            await _context.SaveChangesAsync();
            return Ok(employee);
        }

        [HttpGet("pharmacy/{pharmacyId}/shifts")]
        public async Task<IActionResult> GetShifts(int pharmacyId, [FromQuery] string start, [FromQuery] string end)
        {
            if (!DateTime.TryParse(start, out var startDate) || !DateTime.TryParse(end, out var endDate))
            {
                return BadRequest("Invalid date format. Use YYYY-MM-DD.");
            }

            var shifts = await _context.InternalShifts
                .Include(s => s.Employee)
                .Where(s => s.PharmacyId == pharmacyId && s.Date >= startDate && s.Date <= endDate)
                .ToListAsync();
            
            return Ok(shifts);
        }

        [HttpPost("shifts")]
        public async Task<IActionResult> AddShift([FromBody] InternalShift shift)
        {
            _context.InternalShifts.Add(shift);
            await _context.SaveChangesAsync();
            return Ok(shift);
        }

        [HttpDelete("shifts/{id}")]
        public async Task<IActionResult> DeleteShift(int id)
        {
            var shift = await _context.InternalShifts.FindAsync(id);
            if (shift == null) return NotFound();
            
            _context.InternalShifts.Remove(shift);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
