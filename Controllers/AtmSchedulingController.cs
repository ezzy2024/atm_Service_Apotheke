using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AtmSchedulingController : ControllerBase
    {
        // In a real implementation, this would use a database context and a telemed service
        // private readonly DataContext _context;

        public AtmSchedulingController()
        {
        }

        [HttpGet("slots")]
        public IActionResult GetAvailableSlots([FromQuery] string date)
        {
            // Stub: return some available slots for aTM telemedicine
            var slots = new List<object>
            {
                new { Id = 1, Time = "10:00", IsAvailable = true },
                new { Id = 2, Time = "10:30", IsAvailable = false },
                new { Id = 3, Time = "11:00", IsAvailable = true }
            };

            return Ok(slots);
        }

        [HttpPost("book")]
        public IActionResult BookSlot([FromBody] BookSlotRequest request)
        {
            if (request == null || request.SlotId <= 0)
                return BadRequest("Invalid booking request.");

            // Stub: process booking
            return Ok(new { message = "Slot successfully booked.", appointmentId = Guid.NewGuid() });
        }

        [HttpPost("join-room/{appointmentId}")]
        public IActionResult JoinRoom(Guid appointmentId)
        {
            // Stub: return WebRTC token or room URL for telemedicine session
            var token = Guid.NewGuid().ToString(); // mock token
            var roomUrl = $"https://telemed.serviceapotheke.tech/room/{appointmentId}?token={token}";

            return Ok(new { RoomUrl = roomUrl, Token = token });
        }
    }

    public class BookSlotRequest
    {
        public int SlotId { get; set; }
        public string PatientName { get; set; }
        public string Notes { get; set; }
    }
}
