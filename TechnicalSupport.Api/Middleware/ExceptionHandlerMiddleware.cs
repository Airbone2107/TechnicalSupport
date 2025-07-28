using FluentValidation;
using System.Net;
using System.Text.Json;
using TechnicalSupport.Api.Common;

namespace TechnicalSupport.Api.Middleware
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlerMiddleware> _logger;

        public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception has occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var response = context.Response;

            ApiResponse<string> apiResponse;

            switch (exception)
            {
                case ValidationException validationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    var errors = validationException.Errors.Select(e => e.ErrorMessage).ToList();
                    apiResponse = new ApiResponse<string>(false, "Validation Error", null, errors);
                    break;
                // Có thể thêm các loại exception khác ở đây
                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    apiResponse = new ApiResponse<string>(false, "An internal server error has occurred.", null, new List<string> { exception.Message });
                    break;
            }

            var result = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            return context.Response.WriteAsync(result);
        }
    }
} 