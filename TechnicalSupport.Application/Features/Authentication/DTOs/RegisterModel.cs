namespace TechnicalSupport.Application.Features.Authentication.DTOs
{
    public class RegisterModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string DisplayName { get; set; }
        public string? Expertise { get; set; }
        public string? Role { get; set; } // "Client" hoặc "Technician"
    }
} 