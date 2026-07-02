using System.Threading.Tasks;

namespace ServiceApotheke.API.Services
{
    public interface IGeocodingService
    {
        Task<(double Latitude, double Longitude)?> GetCoordinatesAsync(string address);
    }
}
