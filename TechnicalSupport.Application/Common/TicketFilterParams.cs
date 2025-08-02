namespace TechnicalSupport.Application.Common
{
    public class TicketFilterParams : PaginationParams
    {
        public int? StatusId { get; set; }
        public string? Priority { get; set; }
        public string? AssigneeId { get; set; }
        public string? SearchQuery { get; set; }
    }
} 