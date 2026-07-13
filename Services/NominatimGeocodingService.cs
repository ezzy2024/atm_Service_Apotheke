using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace ServiceApotheke.API.Services
{
    public class NominatimGeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;

        public NominatimGeocodingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Nominatim requires a User-Agent header
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ServiceApotheke/1.0 (team@serviceapotheke.tech)");
        }

        public async Task<(double Latitude, double Longitude)?> GetCoordinatesAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            try
            {
                var requestUrl = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";
                var response = await _httpClient.GetAsync(requestUrl);
                
                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(content);
                
                var rootElement = document.RootElement;
                if (rootElement.ValueKind == JsonValueKind.Array && rootElement.GetArrayLength() > 0)
                {
                    var firstResult = rootElement.EnumerateArray().First();
                    if (firstResult.TryGetProperty("lat", out var latProp) && 
                        firstResult.TryGetProperty("lon", out var lonProp))
                    {
                        if (double.TryParse(latProp.GetString(), System.Globalization.CultureInfo.InvariantCulture, out var lat) &&
                            double.TryParse(lonProp.GetString(), System.Globalization.CultureInfo.InvariantCulture, out var lon))
                        {
                            return (lat, lon);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Geocoding Error]: {ex.Message}");
            }

            return null;
        }
    }
}
