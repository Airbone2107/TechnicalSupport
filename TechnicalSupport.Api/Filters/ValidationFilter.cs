using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TechnicalSupport.Api.Common;

namespace TechnicalSupport.Api.Filters
{
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Tìm tham số được đánh dấu [FromBody]
            var parameter = context.ActionDescriptor.Parameters
                .FirstOrDefault(p => p.BindingInfo?.BindingSource == BindingSource.Body);

            if (parameter == null)
            {
                await next();
                return;
            }

            // Lấy giá trị của tham số từ action arguments
            if (!context.ActionArguments.TryGetValue(parameter.Name, out var argument))
            {
                await next();
                return;
            }

            if (argument == null)
            {
                // Trả về lỗi nếu model là null
                context.Result = new BadRequestObjectResult(ApiResponse.Fail("A non-empty request body is required."));
                return;
            }

            // Lấy validator từ DI container
            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

            if (validator == null)
            {
                await next();
                return;
            }

            // Thực hiện validation
            var validationContext = new ValidationContext<object>(argument);
            var validationResult = await validator.ValidateAsync(validationContext);

            if (!validationResult.IsValid)
            {
                // Nếu không hợp lệ, trả về lỗi 400 Bad Request
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                context.Result = new BadRequestObjectResult(ApiResponse.Fail("Validation failed.", errors));
                return;
            }
            
            // Nếu hợp lệ, tiếp tục thực thi action
            await next();
        }
    }
} 