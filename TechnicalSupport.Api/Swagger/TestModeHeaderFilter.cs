using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TechnicalSupport.Api.Swagger
{
    /// <summary>
    /// Adds a custom header to all Swagger UI operations to toggle transactional test mode.
    /// </summary>
    public class TestModeHeaderFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
            {
                operation.Parameters = new List<OpenApiParameter>();
            }

            var httpMethod = context.ApiDescription.HttpMethod?.ToUpper();
            
            // Only add the header option to write operations (POST, PUT, DELETE, PATCH)
            if (httpMethod == "POST" || httpMethod == "PUT" || httpMethod == "DELETE" || httpMethod == "PATCH")
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "X-Test-Mode",
                    In = ParameterLocation.Header,
                    Description = "Set to 'true' to run this request in test mode. The database transaction will be rolled back, leaving data unchanged.",
                    Required = false,
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Default = new Microsoft.OpenApi.Any.OpenApiString("false"),
                        Enum = new List<Microsoft.OpenApi.Any.IOpenApiAny>
                        {
                            new Microsoft.OpenApi.Any.OpenApiString("true"),
                            new Microsoft.OpenApi.Any.OpenApiString("false")
                        }
                    }
                });
            }
        }
    }
} 