using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using FirebaseAdmin.Messaging;
using ServiceApotheke.API.Data;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace ServiceApotheke.API.Services
{
    public class FcmNotificationDispatcher : INotificationDispatcher
    {
        private readonly DataContext _context;
        private readonly ILogger<FcmNotificationDispatcher> _logger;

        public FcmNotificationDispatcher(DataContext context, ILogger<FcmNotificationDispatcher> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task DispatchToPharmacistAsync(int pharmacistId, string title, string body, object dataPayload = null)
        {
            var deviceTokens = await _context.DeviceTokens
                .Where(d => d.PharmacistId == pharmacistId)
                .Select(d => d.FcmToken)
                .ToListAsync();

            if (!deviceTokens.Any())
            {
                _logger.LogInformation($"No FCM tokens found for pharmacist {pharmacistId}. Notification skipped.");
                return;
            }

            var messageData = new Dictionary<string, string>();
            if (dataPayload != null)
            {
                // Serialize dataPayload object properties to dictionary
                var properties = dataPayload.GetType().GetProperties();
                foreach(var prop in properties)
                {
                    messageData.Add(prop.Name, prop.GetValue(dataPayload)?.ToString() ?? "");
                }
            }

            var multicastMessage = new MulticastMessage()
            {
                Tokens = deviceTokens,
                Notification = new Notification
                {
                    Title = title,
                    Body = body
                },
                Data = messageData
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(multicastMessage);
                _logger.LogInformation($"Successfully sent message to {response.SuccessCount} devices. Failed: {response.FailureCount}");
                
                // Optional: Clean up failed tokens if failure reason is Unregistered
                if (response.FailureCount > 0)
                {
                    for (int i = 0; i < response.Responses.Count; i++)
                    {
                        if (!response.Responses[i].IsSuccess)
                        {
                            var errorCode = response.Responses[i].Exception?.MessagingErrorCode;
                            if (errorCode == MessagingErrorCode.Unregistered)
                            {
                                var invalidToken = deviceTokens[i];
                                var dbToken = await _context.DeviceTokens.FirstOrDefaultAsync(t => t.FcmToken == invalidToken);
                                if (dbToken != null)
                                {
                                    _context.DeviceTokens.Remove(dbToken);
                                }
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to send FCM notification.");
            }
        }
    }
}
