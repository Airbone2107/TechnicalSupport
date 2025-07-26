namespace TechnicalSupport.Domain.Entities
{
    public class TechnicianGroup
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int GroupId { get; set; }
        public Group Group { get; set; }
    }
} 