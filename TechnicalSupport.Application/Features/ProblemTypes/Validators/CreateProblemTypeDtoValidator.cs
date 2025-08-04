using FluentValidation;
using TechnicalSupport.Application.Features.ProblemTypes.DTOs;

namespace TechnicalSupport.Application.Features.ProblemTypes.Validators
{
    public class CreateProblemTypeDtoValidator : AbstractValidator<CreateProblemTypeDto>
    {
        public CreateProblemTypeDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Problem type name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
        }
    }
} 