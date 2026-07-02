using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ServiceApotheke.API.Data;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _webhookSecret;
        private readonly DataContext _context;

        public PaymentController(IConfiguration config, DataContext context)
        {
            _config = config;
            _context = context;
            _webhookSecret = _config["Stripe:WebhookSecret"] ?? Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET") ?? "whsec_stub";
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"] ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY") ?? "sk_test_DUMMY_KEY_FOR_TESTING";
        }

        [HttpPost("create-checkout-session")]
        public ActionResult CreateCheckoutSession()
        {
            var domain = _config["FrontendUrl"] ?? "https://apotheken.serviceapotheke.tech";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card", "sepa_debit" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = 29900,
                            Currency = "eur",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "ServiceApotheke SaaS License",
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = domain + "/payment-success",
                CancelUrl = domain + "/payment-canceled",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            return Ok(new { sessionId = session.Id, checkoutUrl = session.Url });
        }

        [HttpPost("checkout/{invoiceId}")]
        public async Task<IActionResult> CheckoutInvoice(int invoiceId)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) return NotFound("Rechnung nicht gefunden.");

            var domain = _config["FrontendUrl"] ?? "https://apotheken.serviceapotheke.tech";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card", "sepa_debit" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(invoice.TotalAmount * 100),
                            Currency = "eur",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Rechnung {invoice.InvoiceNumber}",
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = domain + "/success.html",
                CancelUrl = domain + "/Apotheken/abrechnungen.html",
                ClientReferenceId = invoiceId.ToString()
            };

            var service = new SessionService();
            Session session = service.Create(options);

            return Ok(new { url = session.Url });
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                var signature = Request.Headers["Stripe-Signature"];
                var stripeEvent = EventUtility.ConstructEvent(json, signature, _webhookSecret);

                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (int.TryParse(session.ClientReferenceId, out int invId))
                    {
                        var invoice = await _context.Invoices.FindAsync(invId);
                        if (invoice != null)
                        {
                            invoice.Status = "Bezahlt";
                            invoice.PaidAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                return Ok();
            }
            catch (StripeException)
            {
                return BadRequest();
            }
        }
    }
}
