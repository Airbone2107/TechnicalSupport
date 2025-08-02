namespace TechnicalSupport.Application.Features.Admin.DTOs
{
    public class UserDetailDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string? Expertise { get; set; }
        public IList<string> Roles { get; set; }
        public bool EmailConfirmed { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
    }
} 