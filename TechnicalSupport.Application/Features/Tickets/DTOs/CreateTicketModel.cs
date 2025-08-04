namespace TechnicalSupport.Application.Features.Tickets.DTOs
{
    public class CreateTicketModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int? ProblemTypeId { get; set; } // Thay đổi từ StatusId và cho phép null
        public string Priority { get; set; }
    }
} 