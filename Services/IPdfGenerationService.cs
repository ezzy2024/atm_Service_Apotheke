using System.Threading.Tasks;
using ServiceApotheke.API.Models;

namespace ServiceApotheke.API.Services
{
    public interface IPdfGenerationService
    {
        Task<(byte[] PdfBytes, string DocumentHash)> GenerateTimesheetAsync(Timesheet timesheet, InternalShift shift);
    }
}
