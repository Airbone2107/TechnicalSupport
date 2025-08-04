using FluentValidation;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Application.Features.Tickets.Validators
{
    public class AssignGroupModelValidator : AbstractValidator<AssignGroupModel>
    {
        public AssignGroupModelValidator()
        {
            RuleFor(x => x.GroupId)
                .GreaterThan(0).WithMessage("A valid GroupId is required.");
        }
    }
} 