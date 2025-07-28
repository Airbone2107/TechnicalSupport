using System.Text.Json;

namespace TechnicalSupport.Api.Common
{
    public class ApiResponse<T>
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; }

        public ApiResponse(bool succeeded, string message, T data, List<string> errors = null)
        {
            Succeeded = succeeded;
            Message = message;
            Data = data;
            Errors = errors;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public static class ApiResponse
    {
        public static ApiResponse<T> Success<T>(T data, string message = "Request processed successfully.")
        {
            return new ApiResponse<T>(true, message, data);
        }

        public static ApiResponse<object> Fail(string message, List<string> errors = null)
        {
            return new ApiResponse<object>(false, message, null, errors);
        }
    }
} 