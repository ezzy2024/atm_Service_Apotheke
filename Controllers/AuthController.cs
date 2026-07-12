using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAntiforgery _antiforgery;

        public AuthController(IAntiforgery antiforgery)
        {
            _antiforgery = antiforgery;
        }

        [HttpGet("csrf-token")]
        public IActionResult GetCsrfToken()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            
            // Send the token in a cookie that JS can read, OR just return it in JSON
            // Since we set HttpOnly/Secure policies globally, returning in JSON is safe
            // if we are relying on the client to put it into the X-CSRF-TOKEN header.
            return Ok(new { csrfToken = tokens.RequestToken });
        }
    }
}
