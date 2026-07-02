using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceApotheke.API.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceApotheke.API.Services.Workers
{
    public class DataRetentionWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DataRetentionWorker> _logger;

        public DataRetentionWorker(IServiceProvider serviceProvider, ILogger<DataRetentionWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Executing GDPR Data Retention Policy...");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<DataContext>();

                    // 1. Delete unverified accounts
                    var staleAccounts = await context.Pharmacists
                        .Where(p => !p.IsEmailConfirmed) 
                        .ToListAsync(stoppingToken);

                    if (staleAccounts.Any())
                    {
                        context.Pharmacists.RemoveRange(staleAccounts);
                        _logger.LogInformation($"Purged {staleAccounts.Count} unverified accounts.");
                    }

                    // 2. Anonymize accounts inactive for 10 years (HGB/AO Compliance)
                    var retentionCutoff = DateTime.UtcNow.AddYears(-10);
                    
                    var expiredPharmacists = await context.Pharmacists
                        .Include(p => p.JobApplications)
                        .Where(p => p.JobApplications.Any() && p.JobApplications.All(ja => ja.AppliedAt < retentionCutoff))
                        .ToListAsync(stoppingToken);

                    foreach (var pharmacist in expiredPharmacists)
                    {
                        pharmacist.FullName = "ANONYMIZED";
                        pharmacist.Email = $"deleted_{Guid.NewGuid()}@anonymized.local";
                        pharmacist.PhoneNumber = "ANONYMIZED";
                        pharmacist.Address = "ANONYMIZED";
                        pharmacist.PasswordHash = string.Empty;
                        pharmacist.ApprobationDocumentPath = null;
                        pharmacist.CvDocumentPath = null;
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }

                // Execute cycle once every 24 hours
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}