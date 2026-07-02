using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceApotheke.API.Services
{
    public class MatchResultDto
    {
        public int JobPostId { get; set; }
        public int PharmacistId { get; set; }
        public double MatchScore { get; set; }
        public double DistanceKm { get; set; }
        public string? JobTitle { get; set; }
        public string? PharmacyName { get; set; }
        public string? PharmacistName { get; set; }
    }

    public interface IMatchingService
    {
        Task<IEnumerable<MatchResultDto>> FindMatchesForPharmacistAsync(int pharmacistId);
        Task<IEnumerable<MatchResultDto>> FindMatchesForJobPostAsync(int jobPostId);
    }
}
