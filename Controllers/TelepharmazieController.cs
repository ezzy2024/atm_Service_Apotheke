using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ServiceApotheke.API.Services.Telepharmazie;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelepharmazieController : ControllerBase
    {
        private readonly IGematikTiService _tiService;

        public TelepharmazieController(IGematikTiService tiService)
        {
            _tiService = tiService;
        }

        [HttpGet("erezept/{prescriptionId}")]
        public async Task<IActionResult> GetERezept(string prescriptionId, [FromQuery] string accessCode)
        {
            if (string.IsNullOrWhiteSpace(accessCode))
                return BadRequest(new { message = "Access code required." });

            var result = await _tiService.RetrieveERezeptAsync(prescriptionId, accessCode);
            return Ok(result);
        }

        [HttpPost("erezept/validate")]
        public async Task<IActionResult> ValidateSignature([FromBody] ERezeptDto prescription)
        {
            var isValid = await _tiService.VerifyPrescriptionSignatureAsync(prescription);
            if (!isValid)
                return BadRequest(new { message = "QES validation failed." });

            return Ok(new { message = "Signature valid." });
        }
    }
}
