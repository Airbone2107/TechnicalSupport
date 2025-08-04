using TechnicalSupport.Application.Features.Attachments.DTOs;
using TechnicalSupport.Application.Features.Groups.DTOs;

namespace TechnicalSupport.Application.Features.Tickets.DTOs
{
    public class TicketDto
    {
        public int TicketId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public StatusDto Status { get; set; }
        public UserDto Customer { get; set; }
        public UserDto? Assignee { get; set; }
        public GroupDto? Group { get; set; }
        
        // Thêm các thuộc tính mới
        public List<CommentDto> Comments { get; set; } = new();
        public List<AttachmentDto> Attachments { get; set; } = new();
    }
}