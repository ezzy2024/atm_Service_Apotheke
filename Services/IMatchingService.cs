using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceApotheke.API.Models;
namespace ServiceApotheke.API.Services
{
    public class MatchResultDto
    {
        public double Score { get; set; }
        public double DistanceKm { get; set; }
        public JobPost JobPost { get; set; }
        public Pharmacist Pharmacist { get; set; }
    }

    public interface IMatchingService
    {
        Task<IEnumerable<MatchResultDto>> FindMatchesForPharmacistAsync(int pharmacistId);
        Task<IEnumerable<MatchResultDto>> FindMatchesForJobPostAsync(int jobPostId);
    }
}
