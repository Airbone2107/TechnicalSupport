using FluentValidation;
using TechnicalSupport.Application.Features.Groups.DTOs;

namespace TechnicalSupport.Application.Features.Groups.Validators
{
    public class AddMemberModelValidator : AbstractValidator<AddMemberModel>
    {
        public AddMemberModelValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required.");
        }
    }
} 