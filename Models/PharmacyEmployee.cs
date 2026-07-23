using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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
        
        [JsonIgnore]
        public virtual Pharmacy? Pharmacy { get; set; }
        [JsonIgnore]
        public virtual ICollection<InternalShift> Shifts { get; set; } = new List<InternalShift>();
    }
}
