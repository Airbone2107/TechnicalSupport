using FluentValidation;
using TechnicalSupport.Application.Features.Admin.DTOs;

namespace TechnicalSupport.Application.Features.Admin.Validators
{
    public class UpdateUserRolesModelValidator : AbstractValidator<UpdateUserRolesModel>
    {
        public UpdateUserRolesModelValidator()
        {
            RuleFor(x => x.Roles)
                .NotNull().WithMessage("Roles list cannot be null.")
                .Must(roles => roles.Count > 0).WithMessage("User must have at least one role.");

            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Your password is required for verification.");
        }
    }
} 