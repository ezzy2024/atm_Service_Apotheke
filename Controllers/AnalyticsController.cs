using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceApotheke.API.Data;

namespace ServiceApotheke.API.Controllers
{
    public class DashboardStatsDto
    {
        // Vermittlungs-KPIs
        public int TotalOpenJobs { get; set; }
        public double FillRatePercentage { get; set; }
        public double AveragePlacementTimeHours { get; set; }

        // Finanz-KPIs
        public decimal TotalRevenue { get; set; }
        public decimal TotalPlatformFees { get; set; }
        public int PaidInvoicesCount { get; set; }
        public int UnpaidInvoicesCount { get; set; }

        // Infrastruktur-Telemetrie
        public int TotalAnomalies { get; set; }
        public double GlobalAnomalyRatePercentage { get; set; }
        public List<PharmacyAnomalyStat> TopAnomalousPharmacies { get; set; } = new List<PharmacyAnomalyStat>();
    }

    public class PharmacyAnomalyStat
    {
        public int PharmacyId { get; set; }
        public string PharmacyName { get; set; } = string.Empty;
        public int AnomalyCount { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : ControllerBase
    {
        private readonly DataContext _context;

        public AnalyticsController(DataContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
        {
            var stats = new DashboardStatsDto();

            // 1. Vermittlungs-KPIs
            var totalJobs = await _context.JobPosts.CountAsync();
            stats.TotalOpenJobs = await _context.JobPosts.CountAsync(j => j.Status == "Active");
            
            var filledJobs = await _context.JobPosts.CountAsync(j => j.Status == "Filled");
            stats.FillRatePercentage = totalJobs > 0 ? (double)filledJobs / totalJobs * 100.0 : 0;

            // Durchschnittliche Vermittlungsdauer für Jobs, die Filled sind.
            // Wir messen die Zeit von JobPost.CreatedAt bis zur angenommenen JobApplication.AppliedAt
            // Hinweis: EF Core unterstützt DateDiffHour via EF.Functions nur eingeschränkt je nach Provider (SQL Server). 
            // In PostgreSQL wird das oft als (AppliedAt - CreatedAt) unterstützt oder wir ziehen den Schnitt der Differenzticks.
            // Um Provider-übergreifend kompatibel zu sein, nutzen wir EF.Functions.DateDiffHour, falls wir SQL Server nutzen,
            // ansonsten approximieren wir es, indem wir die durchschnittlichen Ticks berechnen. Da EF Core 8 primitive collections
            // oder direkte math. operationen für DateTime in PostgreSQL unterstützt, nutzen wir TimeSpan extraction.
            // Um absolut sicher zu sein, dass es nativ übersetzt wird:
            var placementTimes = await _context.JobApplications
                .Where(a => a.Status == "Accepted" || a.Status == "Completed" || a.Status == "Invoiced")
                .Select(a => EF.Functions.DateDiffHour(a.JobPost!.CreatedAt, a.AppliedAt))
                .ToListAsync();

            if (placementTimes.Any())
            {
                stats.AveragePlacementTimeHours = placementTimes.Average();
            }

            // 2. Finanz-KPIs
            // Wir beziehen uns auf alle Rechnungen.
            var invoices = await _context.Invoices
                .Include(i => i.Timesheet)
                .Select(i => new {
                    i.TotalAmount,
                    i.PaidAt,
                    TravelCosts = i.Timesheet != null ? i.Timesheet.TravelCosts : 0,
                    AccommodationCosts = i.Timesheet != null ? i.Timesheet.AccommodationCosts : 0
                })
                .ToListAsync(); // Wir holen nur diese 4 Spalten, um im Speicher effizient zu summieren.
            
            // Um Client-Side Warnungen bei komplexer Mathematik (Division etc) komplett zu vermeiden,
            // ist es manchmal sicherer die Summen nach dem Select zu berechnen, da es nur wenige Datensätze (Invoices) sind, 
            // aber wir machen es wie vom User verlangt direkt als LINQ-Projektion vor der Evaluierung (soweit EF das erlaubt).
            
            // Alternativ: Komplett native Aggregation
            var financials = await _context.Invoices
                .Include(i => i.Timesheet)
                .GroupBy(i => 1)
                .Select(g => new {
                    TotalRevenue = g.Sum(i => i.TotalAmount),
                    PaidCount = g.Count(i => i.PaidAt != null),
                    UnpaidCount = g.Count(i => i.PaidAt == null),
                    // Platform Fee: (TotalAmount - Travel - Accom) / 1.15 * 0.15
                    TotalPlatformFees = g.Sum(i => (i.TotalAmount - (i.Timesheet != null ? i.Timesheet.TravelCosts : 0) - (i.Timesheet != null ? i.Timesheet.AccommodationCosts : 0)) / 1.15m * 0.15m)
                })
                .FirstOrDefaultAsync();

            if (financials != null)
            {
                stats.TotalRevenue = financials.TotalRevenue;
                stats.TotalPlatformFees = financials.TotalPlatformFees;
                stats.PaidInvoicesCount = financials.PaidCount;
                stats.UnpaidInvoicesCount = financials.UnpaidCount;
            }

            // 3. Infrastruktur-Telemetrie
            var totalLogs = await _context.TemperatureLogs.CountAsync();
            stats.TotalAnomalies = await _context.TemperatureLogs.CountAsync(t => t.IsAnomaly);
            
            stats.GlobalAnomalyRatePercentage = totalLogs > 0 ? (double)stats.TotalAnomalies / totalLogs * 100.0 : 0;

            stats.TopAnomalousPharmacies = await _context.TemperatureLogs
                .Where(t => t.IsAnomaly)
                .GroupBy(t => new { t.PharmacyId, t.Pharmacy!.PharmacyName })
                .Select(g => new PharmacyAnomalyStat
                {
                    PharmacyId = g.Key.PharmacyId,
                    PharmacyName = g.Key.PharmacyName,
                    AnomalyCount = g.Count()
                })
                .OrderByDescending(x => x.AnomalyCount)
                .Take(5)
                .ToListAsync();

            return Ok(stats);
        }
    }
}
