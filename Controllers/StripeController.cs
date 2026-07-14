using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using System.Security.Claims;
using ServiceApotheke.API.Data;
using System.Threading.Tasks;

namespace ServiceApotheke.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StripeController : ControllerBase
    {
        private readonly DataContext _context;

        public StripeController(DataContext context)
        {
            _context = context;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> CreateCheckoutSession()
        {
            var userIdStr = User.FindFirstValue("id");
            if (!int.TryParse(userIdStr, out int pharmacyId))
                return Unauthorized();

            var pharmacy = await _context.Pharmacies.FindAsync(pharmacyId);
            if (pharmacy == null) return NotFound("Pharmacy not found.");

            var domain = "https://serviceapotheke.tech";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new System.Collections.Generic.List<string> { "card", "sepa_debit" },
                Mode = "subscription",
                SuccessUrl = domain + "/dashboard/pharmacy/billing?success=true",
                CancelUrl = domain + "/dashboard/pharmacy/billing?canceled=true",
                Customer = pharmacy.StripeCustomerId, // If this is null, Stripe creates a new customer. We can also handle that logic here if needed.
                LineItems = new System.Collections.Generic.List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = "price_placeholder_12345", // Placeholder Price ID
                        Quantity = 1,
                    },
                },
            };

            // If we don't have a StripeCustomerId saved yet, we can set CustomerEmail instead
            if (string.IsNullOrEmpty(pharmacy.StripeCustomerId))
            {
                options.CustomerEmail = pharmacy.Email;
            }

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            // You could save session.Id or session.CustomerId to the db here if needed.

            return Ok(new { url = session.Url });
        }
    }
}
