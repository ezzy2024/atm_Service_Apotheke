using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceApotheke.API.Services;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Ensure endpoints are secured
    public class MatchingController : ControllerBase
    {
        private readonly IMatchingService _matchingService;

        public MatchingController(IMatchingService matchingService)
        {
            _matchingService = matchingService;
        }

        [HttpGet("pharmacist/{id}/jobs")]
        public async Task<ActionResult<IEnumerable<MatchResultDto>>> GetMatchesForPharmacist(int id)
        {
            var matches = await _matchingService.FindMatchesForPharmacistAsync(id);
            return Ok(matches);
        }

        [HttpGet("jobpost/{id}/pharmacists")]
        public async Task<ActionResult<IEnumerable<MatchResultDto>>> GetMatchesForJobPost(int id)
        {
            var matches = await _matchingService.FindMatchesForJobPostAsync(id);
            return Ok(matches);
        }
    }
}