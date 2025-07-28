using FluentValidation;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Application.Features.Tickets.Validators
{
    public class UpdateStatusModelValidator : AbstractValidator<UpdateStatusModel>
    {
        public UpdateStatusModelValidator()
        {
            RuleFor(x => x.StatusId)
                .GreaterThan(0).WithMessage("A valid StatusId is required.");
        }
    }
} 