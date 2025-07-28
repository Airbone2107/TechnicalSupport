using Swashbuckle.AspNetCore.Filters;
using TechnicalSupport.Application.Features.Authentication.DTOs;

namespace TechnicalSupport.Api.SwaggerExamples.Authentication
{
    public class LoginModelExample : IMultipleExamplesProvider<LoginModel>
    {
        public IEnumerable<SwaggerExample<LoginModel>> GetExamples()
        {
            yield return SwaggerExample.Create(
                "Login as Client",
                new LoginModel
                {
                    Email = "client@example.com",
                    Password = "Password123!"
                }
            );

            yield return SwaggerExample.Create(
                "Login as Technician",
                new LoginModel
                {
                    Email = "tech@example.com",
                    Password = "Password123!"
                }
            );
        }
    }
} 