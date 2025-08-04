using FluentValidation;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Application.Features.Tickets.Validators
{
    public class CreateTicketModelValidator : AbstractValidator<CreateTicketModel>
    {
        public CreateTicketModelValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(255).WithMessage("Title cannot be longer than 255 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.");

            RuleFor(x => x.ProblemTypeId)
                .NotNull().WithMessage("A Problem Type must be selected.");
        }
    }
} 