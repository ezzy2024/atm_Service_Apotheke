using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceApotheke.API.Data;
using ServiceApotheke.API.Models;

namespace ServiceApotheke.API.Services
{
    public class SaturdayRotationService
    {
        private readonly DataContext _dbContext;
        private readonly ILogger<SaturdayRotationService> _logger;

        public SaturdayRotationService(DataContext dbContext, ILogger<SaturdayRotationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<int?> GetParticipationTeamIdAsync(DateTime targetDate, int pharmacyId, CancellationToken ct = default)
        {
            if (targetDate.DayOfWeek != DayOfWeek.Saturday)
            {
                throw new ArgumentException("SaturdayRotationService only accepts Saturdays as input.");
            }

            // Check for holiday
            var isHoliday = await _dbContext.Holidays.AnyAsync(h => h.Date.Date == targetDate.Date, ct);
            if (isHoliday)
            {
                _logger.LogInformation("Target date {Date} is a holiday. No Saturday rotation.", targetDate);
                return null;
            }

            // Check if rotation already exists for this date
            var existingRotation = await _dbContext.SaturdayRotations
                .FirstOrDefaultAsync(r => r.Date.Date == targetDate.Date && r.PharmacyId == pharmacyId, ct);

            if (existingRotation != null)
            {
                return existingRotation.TeamId;
            }

            // Calculate new rotation team
            var teamId = await CalculateNextTeamAsync(targetDate, pharmacyId, ct);
            if (teamId.HasValue)
            {
                // Write participation to database
                var newRotation = new SaturdayRotation
                {
                    Date = targetDate.Date,
                    TeamId = teamId.Value,
                    PharmacyId = pharmacyId
                };

                _dbContext.SaturdayRotations.Add(newRotation);
                await _dbContext.SaveChangesAsync(ct);

                // Prune old and future records
                await CleanupRotationsAsync(pharmacyId, ct);
            }

            return teamId;
        }

        private async Task<int?> CalculateNextTeamAsync(DateTime targetDate, int pharmacyId, CancellationToken ct)
        {
            var teams = await _dbContext.SaturdayRotationTeams
                .Where(t => t.PharmacyId == pharmacyId)
                .OrderBy(t => t.Id)
                .Select(t => t.Id)
                .ToListAsync(ct);

            if (!teams.Any())
            {
                return null;
            }

            // Find last known rotation
            var lastRotation = await _dbContext.SaturdayRotations
                .Where(r => r.PharmacyId == pharmacyId && r.Date <= targetDate)
                .OrderByDescending(r => r.Date)
                .FirstOrDefaultAsync(ct);

            DateTime loopDate;
            int currentTeamIndex;

            if (lastRotation == null)
            {
                // Baseline anchor date (e.g. 1970-01-03 which is a Saturday)
                loopDate = new DateTime(1970, 1, 3);
                currentTeamIndex = 0;
            }
            else
            {
                loopDate = lastRotation.Date;
                currentTeamIndex = teams.IndexOf(lastRotation.TeamId);
                if (currentTeamIndex == -1) currentTeamIndex = 0; // Fallback if team was deleted
            }

            // Iterate week by week P7D
            for (var d = loopDate.AddDays(7); d <= targetDate; d = d.AddDays(7))
            {
                // Is this date a holiday?
                var isHoliday = await _dbContext.Holidays.AnyAsync(h => h.Date.Date == d.Date, ct);
                if (isHoliday)
                {
                    // Skip rotation advance on holiday
                    continue;
                }

                currentTeamIndex++;
                if (currentTeamIndex >= teams.Count)
                {
                    currentTeamIndex = 0;
                }
            }

            return teams[currentTeamIndex];
        }

        private async Task CleanupRotationsAsync(int pharmacyId, CancellationToken ct)
        {
            var prunePastDate = DateTime.UtcNow.Date.AddMonths(-12);
            var pruneFutureDate = DateTime.UtcNow.Date.AddMonths(2);

            var oldOrDistantRotations = await _dbContext.SaturdayRotations
                .Where(r => r.PharmacyId == pharmacyId && (r.Date < prunePastDate || r.Date > pruneFutureDate))
                .ToListAsync(ct);

            if (oldOrDistantRotations.Any())
            {
                _dbContext.SaturdayRotations.RemoveRange(oldOrDistantRotations);
                await _dbContext.SaveChangesAsync(ct);
            }
        }
    }
}
