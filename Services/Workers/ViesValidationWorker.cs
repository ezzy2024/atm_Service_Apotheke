using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceApotheke.API.Data;

namespace ServiceApotheke.API.Services.Workers
{
    public class ViesValidationWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ViesValidationWorker> _logger;
        private readonly HttpClient _httpClient;

        public ViesValidationWorker(IServiceProvider serviceProvider, ILogger<ViesValidationWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _httpClient = new HttpClient { BaseAddress = new Uri("https://ec.europa.eu/") };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
                    
                    var pendingPharmacies = db.Pharmacies
                        .Where(p => p.UstIdValidationStatus == "Pending" || p.UstIdValidationStatus == null)
                        .Take(50)
                        .ToList();

                    foreach (var pharmacy in pendingPharmacies)
                    {
                        if (string.IsNullOrWhiteSpace(pharmacy.UstIdNr) || !pharmacy.UstIdNr.StartsWith("DE"))
                        {
                            pharmacy.UstIdValidationStatus = "Invalid";
                            continue;
                        }

                        var vatNumber = pharmacy.UstIdNr.Substring(2);
                        try
                        {
                            // VIES REST API Check
                            var request = new
                            {
                                memberStateCode = "DE",
                                vatNumber = vatNumber
                            };

                            var response = await _httpClient.PostAsJsonAsync("taxation_customs/vies/rest-api/check-vat-number", request, stoppingToken);

                            if (response.IsSuccessStatusCode)
                            {
                                var result = await response.Content.ReadFromJsonAsync<ViesResponse>(cancellationToken: stoppingToken);
                                pharmacy.UstIdValidationStatus = result?.valid == true ? "Valid" : "Invalid";
                            }
                            else
                            {
                                _logger.LogWarning($"VIES API returned {response.StatusCode} for VAT {pharmacy.UstIdNr}. Retrying later.");
                                // Keep status as Pending for retry
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to validate VAT {pharmacy.UstIdNr} with VIES API. Will retry.");
                        }

                        await Task.Delay(500, stoppingToken); // Rate limiting between calls
                    }

                    if (pendingPharmacies.Any())
                    {
                        await db.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"Processed {pendingPharmacies.Count} VAT validations.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Critical error in ViesValidationWorker loop.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    public class ViesResponse
    {
        public bool valid { get; set; }
        public string requestDate { get; set; }
        public string userError { get; set; }
        public string name { get; set; }
        public string address { get; set; }
    }
}
