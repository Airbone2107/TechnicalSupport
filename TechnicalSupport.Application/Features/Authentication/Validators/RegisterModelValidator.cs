using FluentValidation;
using TechnicalSupport.Application.Features.Authentication.DTOs;

namespace TechnicalSupport.Application.Features.Authentication.Validators
{
    public class RegisterModelValidator : AbstractValidator<RegisterModel>
    {
        public RegisterModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display name is required.");
        }
    }
} 