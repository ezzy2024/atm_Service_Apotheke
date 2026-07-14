using Microsoft.AspNetCore.Mvc;
using Stripe;
using ServiceApotheke.API.Data;
using Microsoft.EntityFrameworkCore;

namespace ServiceApotheke.API.Controllers
{
    [ApiController]
    [Route("api/webhook/stripe")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(DataContext context, IConfiguration configuration, ILogger<StripeWebhookController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var endpointSecret = _configuration["Stripe:WebhookSecret"];

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    endpointSecret
                );

                if (stripeEvent.Type == EventTypes.InvoicePaid)
                {
                    var invoice = stripeEvent.Data.Object as Stripe.Invoice;
                    if (!string.IsNullOrEmpty(invoice?.CustomerId))
                    {
                        var customerId = invoice.CustomerId;
                        var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.StripeCustomerId == customerId);
                        if (pharmacy != null)
                        {
                            // In a real app, map the Stripe product ID to the SubscriptionTier
                            pharmacy.SubscriptionTier = "Pro";
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Pharmacy {PharmacyId} upgraded to Pro.", pharmacy.Id);
                        }
                    }
                }
                else if (stripeEvent.Type == EventTypes.CustomerSubscriptionDeleted)
                {
                    var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                    if (subscription != null)
                    {
                        var customerId = subscription.CustomerId;
                        var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.StripeCustomerId == customerId);
                        if (pharmacy != null)
                        {
                            pharmacy.SubscriptionTier = "Free";
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Pharmacy {PharmacyId} downgraded to Free.", pharmacy.Id);
                        }
                    }
                }
                else if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    // Handle shift payment success
                    var shiftIdStr = paymentIntent?.Metadata?.GetValueOrDefault("InternalShiftId");
                    if (int.TryParse(shiftIdStr, out int shiftId))
                    {
                        var shift = await _context.InternalShifts.FindAsync(shiftId);
                        if (shift != null)
                        {
                            shift.EscrowStatus = "PayoutCompleted";
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                else if (stripeEvent.Type == "transfer.created")
                {
                    var transfer = stripeEvent.Data.Object as Transfer;
                    var shiftIdStr = transfer?.Metadata?.GetValueOrDefault("InternalShiftId");
                    if (int.TryParse(shiftIdStr, out int shiftId))
                    {
                        var shift = await _context.InternalShifts.FindAsync(shiftId);
                        if (shift != null && shift.EscrowStatus != "Released")
                        {
                            shift.EscrowStatus = "Released";
                            shift.StripeTransferId = transfer?.Id;
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Escrow successfully released to Locum via Stripe Transfer for Shift {ShiftId}", shift.Id);
                        }
                    }
                }
                else if (stripeEvent.Type == "transfer.failed")
                {
                    var transfer = stripeEvent.Data.Object as Transfer;
                    var shiftIdStr = transfer?.Metadata?.GetValueOrDefault("InternalShiftId");
                    if (int.TryParse(shiftIdStr, out int shiftId))
                    {
                        var shift = await _context.InternalShifts.FindAsync(shiftId);
                        if (shift != null)
                        {
                            shift.EscrowStatus = "Failed";
                            await _context.SaveChangesAsync();
                            _logger.LogError("Escrow release FAILED for Shift {ShiftId}. Administrator notification required.", shift.Id);
                            // Generate notification logic to admin would be placed here
                        }
                    }
                }

                return Ok();
            }
            catch (StripeException e)
            {
                _logger.LogError("Stripe Webhook Error: {Message}", e.Message);
                return BadRequest();
            }
            catch (Exception e)
            {
                _logger.LogError("Internal Error processing webhook: {Message}", e.Message);
                return StatusCode(500);
            }
        }
    }
}
