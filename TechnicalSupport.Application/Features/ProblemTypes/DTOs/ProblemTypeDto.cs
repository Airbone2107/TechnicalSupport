namespace TechnicalSupport.Application.Features.ProblemTypes.DTOs
{
    public class ProblemTypeDto
    {
        public int ProblemTypeId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? GroupId { get; set; }
    }
} 