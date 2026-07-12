using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        [HttpGet("me")]
        public IActionResult GetMe()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Unauthorized(new { message = "Not authenticated" });
            }

            var id = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var role = User.FindFirstValue(ClaimTypes.Role);

            return Ok(new
            {
                id,
                email,
                role
            });
        }
    }
}
