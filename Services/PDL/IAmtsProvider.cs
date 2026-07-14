using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceApotheke.API.Services.PDL
{
    /// <summary>
    /// Pluggable interface for AMTS (Arzneimitteltherapiesicherheit) checks.
    /// Target integration: pharma4u (MediCheck) - integration pending licensing.
    /// </summary>
    public interface IAmtsProvider
    {
        Task<AmtsCheckResult> PerformMedicationCheckAsync(IEnumerable<string> medicationPzns, string patientId);
    }

    public class AmtsCheckResult
    {
        public bool HasInteractions { get; set; }
        public List<string> InteractionWarnings { get; set; } = new List<string>();
        public bool IsEligibleForPdlMedicationCheck { get; set; }
    }
}
