using FluentValidation;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Application.Features.Tickets.Validators
{
    public class UpdateCommentModelValidator : AbstractValidator<UpdateCommentModel>
    {
        public UpdateCommentModelValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Comment content cannot be empty.");
        }
    }
} 