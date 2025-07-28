namespace TechnicalSupport.Application.Features.Tickets.DTOs
{
    public class CommentDto
    {
        public int CommentId { get; set; }
        public int TicketId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserDto User { get; set; }
    }
} 