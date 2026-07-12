using System;
using System.Collections.Generic;

namespace ServiceApotheke.API.Models
{
    public class PharmacyEmployee
    {
        public int Id { get; set; }
        public int PharmacyId { get; set; }
        
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // e.g. "Apotheker", "PTA", "PKA", "Botenfahrer"
        public string ColorCode { get; set; } = "#3b82f6"; // Default blue
        
        public virtual Pharmacy? Pharmacy { get; set; }
        public virtual ICollection<InternalShift> Shifts { get; set; } = new List<InternalShift>();
    }
}
