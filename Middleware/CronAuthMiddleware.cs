using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace ServiceApotheke.API.Middleware
{
    public class CronAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _cronSecret;

        public CronAuthMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _cronSecret = configuration["CronSecret"] ?? "default-cron-secret-for-dev";
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/api/admin/cron"))
            {
                if (!context.Request.Headers.TryGetValue("x-cron-secret", out var extractedSecret))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { message = "Cron Secret missing" });
                    return;
                }

                if (extractedSecret != _cronSecret)
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsJsonAsync(new { message = "Cron Secret invalid" });
                    return;
                }
            }

            await _next(context);
        }
    }
}
