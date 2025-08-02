using FluentValidation;
using TechnicalSupport.Application.Features.Admin.DTOs;

namespace TechnicalSupport.Application.Features.Admin.Validators
{
    public class UpdateUserByAdminModelValidator : AbstractValidator<UpdateUserByAdminModel>
    {
        public UpdateUserByAdminModelValidator()
        {
            // Cho phép các trường là null, nhưng nếu có giá trị thì không được rỗng.
            RuleFor(x => x.DisplayName)
                .NotEmpty()
                .When(x => x.DisplayName != null)
                .WithMessage("Display name cannot be empty.");
        }
    }
} 