namespace TechnicalSupport.Application.Features.Tickets.DTOs
{
    public class CreateTicketModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int StatusId { get; set; }
        public string? Priority { get; set; }
    }
} 