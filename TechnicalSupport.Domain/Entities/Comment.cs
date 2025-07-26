using System.ComponentModel.DataAnnotations;

namespace TechnicalSupport.Domain.Entities
{
    public class Comment
    {
        public int CommentId { get; set; }

        [Required]
        public int TicketId { get; set; }
        public Ticket Ticket { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 