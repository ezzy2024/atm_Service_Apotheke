using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace ServiceApotheke.API.Filters
{
    public class PremiumFeatureAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var hasPremiumAccess = context.HttpContext.User.FindFirstValue("HasPremiumAccess");
            if (hasPremiumAccess != "True")
            {
                context.Result = new ObjectResult("Freigabe erforderlich für Premium-Funktionen. Bitte kontaktieren Sie den Support.")
                {
                    StatusCode = 403
                };
            }
            base.OnActionExecuting(context);
        }
    }
}
