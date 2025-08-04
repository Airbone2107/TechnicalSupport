namespace TechnicalSupport.Application.Features.ProblemTypes.DTOs
{
    public class UpdateProblemTypeDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int? GroupId { get; set; }
    }
} 