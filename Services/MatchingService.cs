using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Models;

namespace ServiceApotheke.API.Services
{
    public class MatchingService : IMatchingService
    {
        private readonly DataContext _context;

        public MatchingService(DataContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MatchResultDto>> FindMatchesForPharmacistAsync(int pharmacistId)
        {
            var pharmacist = await _context.Pharmacists.FindAsync(pharmacistId);
            if (pharmacist == null) return Enumerable.Empty<MatchResultDto>();

            // 1. Data Pre-Filtering: Budget Alignment
            // We only load jobs where the pharmacy's TargetHourlyRate is >= the Pharmacist's HourlyRate,
            // or the JobPost's Salary is >= Pharmacist's HourlyRate.
            var potentialJobs = await _context.JobPosts
                .Include(j => j.Pharmacy)
                .Where(j => j.Status == "Active")
                .Where(j => (j.Pharmacy.TargetHourlyRate != null && j.Pharmacy.TargetHourlyRate >= pharmacist.HourlyRate) 
                         || (j.Salary != null && j.Salary >= pharmacist.HourlyRate))
                .Where(j => string.IsNullOrEmpty(j.RequiredQualifications) || j.RequiredQualifications == pharmacist.Qualification)
                .ToListAsync();

            var matches = new List<MatchResultDto>();

            // 2. Haversine & Score Evaluation
            foreach (var job in potentialJobs)
            {
                if (pharmacist.Latitude == null || pharmacist.Longitude == null ||
                    job.Pharmacy?.Latitude == null || job.Pharmacy?.Longitude == null)
                {
                    continue; // Skip if coordinates are missing
                }

                double distance = CalculateHaversine(
                    pharmacist.Latitude.Value, pharmacist.Longitude.Value,
                    job.Pharmacy.Latitude.Value, job.Pharmacy.Longitude.Value);

                // Hard Filter: Distance must be within Pharmacist's MaxDistanceKm
                if (distance > pharmacist.MaxDistanceKm && distance > pharmacist.RadiusKm)
                    continue;

                double score = CalculateScore(pharmacist, job, distance);

                matches.Add(new MatchResultDto
                {
                    JobPostId = job.Id,
                    PharmacistId = pharmacist.Id,
                    DistanceKm = Math.Round(distance, 1),
                    MatchScore = Math.Round(score, 1),
                    JobTitle = job.Title,
                    PharmacyName = job.Pharmacy.PharmacyName,
                    PharmacistName = pharmacist.FullName
                });
            }

            return matches.OrderByDescending(m => m.MatchScore);
        }

        public async Task<IEnumerable<MatchResultDto>> FindMatchesForJobPostAsync(int jobPostId)
        {
            var job = await _context.JobPosts
                .Include(j => j.Pharmacy)
                .FirstOrDefaultAsync(j => j.Id == jobPostId);

            if (job == null || job.Pharmacy == null) return Enumerable.Empty<MatchResultDto>();

            // 1. Data Pre-Filtering: Budget Alignment
            decimal maxBudget = job.Pharmacy.TargetHourlyRate ?? job.Salary ?? 0;

            var potentialPharmacists = await _context.Pharmacists
                .Where(p => p.IsVerified)
                .Where(p => p.HourlyRate <= maxBudget || maxBudget == 0) // if maxBudget is 0, we assume no budget constraint
                .Where(p => string.IsNullOrEmpty(job.RequiredQualifications) || job.RequiredQualifications == p.Qualification)
                .ToListAsync();

            var matches = new List<MatchResultDto>();

            // 2. Haversine & Score Evaluation
            foreach (var pharmacist in potentialPharmacists)
            {
                if (pharmacist.Latitude == null || pharmacist.Longitude == null ||
                    job.Pharmacy.Latitude == null || job.Pharmacy.Longitude == null)
                {
                    continue;
                }

                double distance = CalculateHaversine(
                    pharmacist.Latitude.Value, pharmacist.Longitude.Value,
                    job.Pharmacy.Latitude.Value, job.Pharmacy.Longitude.Value);

                // Hard Filter
                if (distance > pharmacist.MaxDistanceKm && distance > pharmacist.RadiusKm)
                    continue;

                double score = CalculateScore(pharmacist, job, distance);

                matches.Add(new MatchResultDto
                {
                    JobPostId = job.Id,
                    PharmacistId = pharmacist.Id,
                    DistanceKm = Math.Round(distance, 1),
                    MatchScore = Math.Round(score, 1),
                    JobTitle = job.Title,
                    PharmacyName = job.Pharmacy.PharmacyName,
                    PharmacistName = pharmacist.FullName
                });
            }

            return matches.OrderByDescending(m => m.MatchScore);
        }

        private double CalculateScore(Pharmacist pharmacist, JobPost job, double distance)
        {
            double score = 100.0;

            // Distance penalty: reduce score slightly for each km, up to a max penalty
            // E.g. 0 km = no penalty. Max distance = 20% penalty.
            double maxAllowedDistance = Math.Max(pharmacist.MaxDistanceKm, pharmacist.RadiusKm);
            if (maxAllowedDistance > 0)
            {
                double distancePenalty = (distance / maxAllowedDistance) * 20.0;
                score -= Math.Min(distancePenalty, 20.0);
            }

            // Software/WWS Experience Bonus
            string jobWws = job.RequiredWws ?? job.Pharmacy?.SoftwareSystem ?? "";
            string pharmacistWws = pharmacist.WwsProficiency ?? pharmacist.SoftwareExperience ?? "";

            if (!string.IsNullOrEmpty(pharmacistWws) && !string.IsNullOrEmpty(jobWws))
            {
                if (pharmacistWws.Contains(jobWws, StringComparison.OrdinalIgnoreCase))
                {
                    // +10% bonus for matching WWS
                    score = Math.Min(100.0, score + 10.0);
                }
                else
                {
                    // -10% penalty for mismatched WWS (assuming training is needed)
                    score -= 10.0;
                }
            }

            return score;
        }

        private double CalculateHaversine(double lat1, double lon1, double lat2, double lon2)
        {
            var r = 6371.0; // Earth's radius in kilometers
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return r * c;
        }

        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }
    }
}
