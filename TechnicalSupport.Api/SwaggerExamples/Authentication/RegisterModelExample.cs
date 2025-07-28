using Swashbuckle.AspNetCore.Filters;
using TechnicalSupport.Application.Features.Authentication.DTOs;

namespace TechnicalSupport.Api.SwaggerExamples.Authentication
{
    public class RegisterModelExample : IMultipleExamplesProvider<RegisterModel>
    {
        public IEnumerable<SwaggerExample<RegisterModel>> GetExamples()
        {
            yield return SwaggerExample.Create(
                "Register a new Client",
                new RegisterModel
                {
                    Email = "new.client@example.com",
                    Password = "Password123!",
                    DisplayName = "New Test Client",
                    Role = "Client",
                    Expertise = null
                }
            );

            yield return SwaggerExample.Create(
                "Register a new Technician",
                new RegisterModel
                {
                    Email = "new.tech@example.com",
                    Password = "Password123!",
                    DisplayName = "New Test Technician",
                    Role = "Technician",
                    Expertise = "Hardware"
                }
            );
        }
    }
} 