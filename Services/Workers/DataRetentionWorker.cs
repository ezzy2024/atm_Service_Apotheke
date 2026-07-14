using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceApotheke.API.Data;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Services;

namespace ServiceApotheke.API.Services.Workers
{
    public class DataRetentionWorker : BackgroundService
    {
        private readonly ILogger<DataRetentionWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public DataRetentionWorker(ILogger<DataRetentionWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("DataRetentionWorker running at: {time}", DateTimeOffset.Now);

                try
                {
                    await PerformDataCleanupAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during data cleanup.");
                }

                // Run once a day
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }

        private async Task PerformDataCleanupAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var cryptoStorage = scope.ServiceProvider.GetRequiredService<ICryptographicStorageService>();

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            // 1. Purge expired EmailConfirmationTokens
            var expiredPharmacists = await context.Pharmacists
                .Where(p => p.EmailConfirmationToken != null && p.EmailConfirmationTokenExpiry < DateTime.UtcNow)
                .ToListAsync(stoppingToken);
            
            foreach (var pharmacist in expiredPharmacists)
            {
                pharmacist.EmailConfirmationToken = null;
            }

            var expiredPharmacies = await context.Pharmacies
                .Where(p => p.EmailConfirmationToken != null && p.EmailConfirmationTokenExpiry < DateTime.UtcNow)
                .ToListAsync(stoppingToken);

            foreach (var pharmacy in expiredPharmacies)
            {
                pharmacy.EmailConfirmationToken = null;
            }



            // 3. Purge revoked/expired mobile refresh tokens
            var staleTokens = await context.MobileRefreshTokens
                .Where(t => t.ExpiresAt < DateTime.UtcNow || t.RevokedAt != null)
                .ToListAsync(stoppingToken);
            
            if (staleTokens.Any())
            {
                context.MobileRefreshTokens.RemoveRange(staleTokens);
                _logger.LogInformation($"Deleted {staleTokens.Count} stale mobile refresh tokens.");
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }
}
