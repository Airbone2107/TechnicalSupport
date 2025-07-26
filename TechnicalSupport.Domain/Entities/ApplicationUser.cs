using Microsoft.AspNetCore.Identity;

namespace TechnicalSupport.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; }
        public string? Expertise { get; set; } // For technicians (e.g., "Hardware", "Software")
    }
}
