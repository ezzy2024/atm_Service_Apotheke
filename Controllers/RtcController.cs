using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace ServiceApotheke.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RtcController : ControllerBase
    {
        private readonly IConfiguration _config;

        public RtcController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("turn")]
        public IActionResult GetTurnCredentials()
        {
            var accountSid = _config["Twilio:AccountSid"] ?? "AC_MOCK_SID_FOR_LOCAL_DEV_000000000";
            var authToken = _config["Twilio:AuthToken"] ?? "MOCK_TOKEN_FOR_LOCAL_DEV_00000000";
            
            try 
            {
                TwilioClient.Init(accountSid, authToken);
                var token = TokenResource.Create(ttl: 3600);
                
                return Ok(new {
                    iceServers = token.IceServers
                });
            } 
            catch (System.Exception ex)
            {
                // Fallback for local development if Twilio credentials are mock
                return Ok(new {
                    iceServers = new List<object> {
                        new { urls = "stun:stun.l.google.com:19302" }
                    }
                });
            }
        }
    }
}
