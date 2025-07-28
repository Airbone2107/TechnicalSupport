// TechnicalSupport.Application/Configurations/JwtSettings.cs
namespace TechnicalSupport.Application.Configurations
{
    public class JwtSettings
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string Key { get; set; }
    }
} 