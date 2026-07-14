using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceApotheke.API.Data;

namespace ServiceApotheke.API.Services.Workers
{
    public class GeocodingBackfillWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GeocodingBackfillWorker> _logger;

        public GeocodingBackfillWorker(IServiceProvider serviceProvider, ILogger<GeocodingBackfillWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GeocodingBackfillWorker started.");

            // Wait a few seconds to ensure the main application has started and bound to the port.
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBackfillAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during Geocoding backfill.");
                }

                // Stop the loop after a single full pass. 
                // The worker doesn't need to run continuously since new registrations use geocoding directly.
                _logger.LogInformation("GeocodingBackfillWorker completed its pass. Stopping.");
                break;
            }
        }

        private async Task ProcessBackfillAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            var geocodingService = scope.ServiceProvider.GetRequiredService<IGeocodingService>();

            // Process Pharmacies
            var pharmaciesToGeocode = await dbContext.Pharmacies
                .Where(p => p.Latitude == null || p.Longitude == null)
                .Where(p => !string.IsNullOrEmpty(p.Street) && !string.IsNullOrEmpty(p.City))
                .Where(p => !(p.Street == "Teststraße 1" && p.City == "Krefeld"))
                .ToListAsync(stoppingToken);

            foreach (var pharmacy in pharmaciesToGeocode)
            {
                if (stoppingToken.IsCancellationRequested) break;

                var address = $"{pharmacy.Street} {pharmacy.HouseNumber}, {pharmacy.PostalCode} {pharmacy.City}, Germany";
                _logger.LogInformation($"Geocoding Pharmacy {pharmacy.Id}: {address}");

                var coords = await geocodingService.GetCoordinatesAsync(address);
                if (coords != null)
                {
                    pharmacy.Latitude = coords.Value.Latitude;
                    pharmacy.Longitude = coords.Value.Longitude;
                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"Successfully geocoded Pharmacy {pharmacy.Id}");
                }
                else
                {
                    _logger.LogWarning($"Failed to geocode Pharmacy {pharmacy.Id}");
                }

                // Enforce minimum 1-second delay for Nominatim TOS compliance
                await Task.Delay(1500, stoppingToken);
            }

            // Process Pharmacists
            var pharmacistsToGeocode = await dbContext.Pharmacists
                .Where(p => p.Latitude == null || p.Longitude == null)
                .Where(p => !string.IsNullOrEmpty(p.Street) && !string.IsNullOrEmpty(p.City))
                .Where(p => !(p.Street == "Teststraße 1" && p.City == "Krefeld"))
                .ToListAsync(stoppingToken);

            foreach (var pharmacist in pharmacistsToGeocode)
            {
                if (stoppingToken.IsCancellationRequested) break;

                var address = $"{pharmacist.Street} {pharmacist.HouseNumber}, {pharmacist.PostalCode} {pharmacist.City}, Germany";
                _logger.LogInformation($"Geocoding Pharmacist {pharmacist.Id}: {address}");

                var coords = await geocodingService.GetCoordinatesAsync(address);
                if (coords != null)
                {
                    pharmacist.Latitude = coords.Value.Latitude;
                    pharmacist.Longitude = coords.Value.Longitude;
                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"Successfully geocoded Pharmacist {pharmacist.Id}");
                }
                else
                {
                    _logger.LogWarning($"Failed to geocode Pharmacist {pharmacist.Id}");
                }

                // Enforce minimum 1-second delay for Nominatim TOS compliance
                await Task.Delay(1500, stoppingToken);
            }
        }
    }
}
