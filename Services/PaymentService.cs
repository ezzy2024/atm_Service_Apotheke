using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Stripe;
using ServiceApotheke.API.Models;

namespace ServiceApotheke.API.Services
{
    public interface IPaymentService
    {
        Task<PaymentIntent> CreateDirectChargePaymentIntentAsync(InternalShift shift, decimal grossAmount, decimal platformFeeAmount, string pharmacyStripeCustomerId, string pharmacistStripeConnectAccountId);
        Task<Account> CreateConnectedAccountAsync(Pharmacist pharmacist);
        Task<Transfer> ReleaseEscrowAsync(InternalShift shift, Timesheet timesheet);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;

        public PaymentService(IConfiguration configuration)
        {
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<PaymentIntent> CreateDirectChargePaymentIntentAsync(InternalShift shift, decimal grossAmount, decimal platformFeeAmount, string pharmacyStripeCustomerId, string pharmacistStripeConnectAccountId)
        {
            // Under Stripe Connect Direct Charges:
            // 1. We create the PaymentIntent on the connected account (the Pharmacist)
            // 2. The Pharmacist is the Merchant of Record.
            // 3. We take an Application Fee.

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(grossAmount * 100), // in cents
                Currency = "eur",
                Customer = pharmacyStripeCustomerId,
                PaymentMethodTypes = new List<string> { "card", "sepa_debit" },
                ApplicationFeeAmount = (long)(platformFeeAmount * 100),
                Metadata = new Dictionary<string, string>
                {
                    { "InternalShiftId", shift.Id.ToString() },
                    { "PharmacyId", shift.PharmacyId.ToString() }
                }
            };

            var requestOptions = new RequestOptions
            {
                StripeAccount = pharmacistStripeConnectAccountId
            };

            var service = new PaymentIntentService();
            return await service.CreateAsync(options, requestOptions);
        }

        public async Task<Account> CreateConnectedAccountAsync(Pharmacist pharmacist)
        {
            var options = new AccountCreateOptions
            {
                Type = "express", // Express is recommended for locum pharmacists
                Country = "DE",
                Email = pharmacist.Email,
                Capabilities = new AccountCapabilitiesOptions
                {
                    CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                },
                BusinessType = "individual",
                Individual = new AccountIndividualOptions
                {
                    Email = pharmacist.Email,
                    FirstName = pharmacist.FullName.Split(' ').FirstOrDefault(),
                    LastName = pharmacist.FullName.Split(' ').LastOrDefault()
                }
            };

            var service = new AccountService();
            return await service.CreateAsync(options);
        }
        public async Task<Transfer> ReleaseEscrowAsync(InternalShift shift, Timesheet timesheet)
        {
            if (shift.EscrowStatus != "Held" && shift.EscrowStatus != "Pending")
            {
                throw new System.InvalidOperationException($"Escrow release failed. Current status is {shift.EscrowStatus}. Expected 'Held' or 'Pending'.");
            }

            var pharmacist = timesheet.JobApplication?.Pharmacist;
            if (pharmacist == null || string.IsNullOrEmpty(pharmacist.StripeConnectAccountId))
            {
                throw new System.InvalidOperationException("Locum Pharmacist has no connected Stripe account.");
            }

            // Calculate payout
            decimal hours = (decimal)(timesheet.ActualEndTime - timesheet.ActualStartTime).TotalHours;
            if (hours < 0) hours += 24m;
            decimal laborCost = hours * timesheet.HourlyRate;
            
            decimal serviceTotalAmount = laborCost + timesheet.TravelCosts + timesheet.AccommodationCosts;
            decimal commissionTotalAmount = laborCost * 0.15m; // 15% flat fee
            decimal netTransferAmount = serviceTotalAmount - commissionTotalAmount;

            var options = new TransferCreateOptions
            {
                Amount = (long)(netTransferAmount * 100), // in cents
                Currency = "eur",
                Destination = pharmacist.StripeConnectAccountId,
                Metadata = new Dictionary<string, string>
                {
                    { "InternalShiftId", shift.Id.ToString() },
                    { "TimesheetId", timesheet.Id.ToString() }
                }
            };

            var service = new TransferService();
            return await service.CreateAsync(options);
        }
    }
}
