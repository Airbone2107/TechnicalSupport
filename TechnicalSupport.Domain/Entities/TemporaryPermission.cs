using System.ComponentModel.DataAnnotations;

namespace TechnicalSupport.Domain.Entities
{
    public class TemporaryPermission
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public string ClaimType { get; set; } // VD: "Ticket", "Comment"

        [Required]
        public string ClaimValue { get; set; } // VD: "123:Update" (ResourceId:Operation)

        public DateTime ExpirationAt { get; set; }
    }
} 