using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ServiceApotheke.API.Data;
using Microsoft.EntityFrameworkCore;

namespace ServiceApotheke.API.Attributes
{
    public class KioskAuthorizeAttribute : TypeFilterAttribute
    {
        public KioskAuthorizeAttribute() : base(typeof(KioskAuthorizeFilter))
        {
        }
    }

    public class KioskAuthorizeFilter : IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue("x-device-token", out var extractedToken))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Missing x-device-token header" });
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetRequiredService<DataContext>();
            var token = extractedToken.ToString();

            var terminal = await dbContext.KioskTerminals
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.DeviceToken == token && k.Status == "active");

            if (terminal == null)
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Invalid or revoked device token" });
                return;
            }

            // Optional: You could attach the TerminalId or PharmacyId to HttpContext.Items
            context.HttpContext.Items["KioskTerminalId"] = terminal.Id;
            context.HttpContext.Items["KioskPharmacyId"] = terminal.PharmacyId;
        }
    }
}
