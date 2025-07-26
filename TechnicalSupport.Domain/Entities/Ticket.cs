using System.ComponentModel.DataAnnotations;

namespace TechnicalSupport.Domain.Entities
{
    public class Ticket
    {
        public int TicketId { get; set; }

        [Required, StringLength(255)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string CustomerId { get; set; }
        public ApplicationUser Customer { get; set; }

        public string? AssigneeId { get; set; }
        public ApplicationUser? Assignee { get; set; }

        public int? GroupId { get; set; }
        public Group? Group { get; set; }

        [Required]
        public int StatusId { get; set; }
        public Status Status { get; set; }

        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
    }
} 