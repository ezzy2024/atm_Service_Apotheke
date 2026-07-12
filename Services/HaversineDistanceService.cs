using System;

namespace ServiceApotheke.API.Services
{
    public interface IHaversineDistanceService
    {
        double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
    }

    public class HaversineDistanceService : IHaversineDistanceService
    {
        private const double EarthRadiusKm = 6371.0;

        public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusKm * c;
        }

        private static double ToRadians(double angleIn10thofaDegree)
        {
            return angleIn10thofaDegree * Math.PI / 180.0;
        }
    }
}
