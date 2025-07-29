// TechnicalSupport.Application/Features/Tickets/Validators/AssignTicketModelValidator.cs
using FluentValidation;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Application.Features.Tickets.Validators
{
    public class AssignTicketModelValidator : AbstractValidator<AssignTicketModel>
    {
        public AssignTicketModelValidator()
        {
            RuleFor(x => x.AssigneeId)
                .NotEmpty().WithMessage("AssigneeId is required.");
        }
    }
}