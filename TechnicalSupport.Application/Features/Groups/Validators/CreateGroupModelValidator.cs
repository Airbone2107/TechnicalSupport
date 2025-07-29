using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using TechnicalSupport.Application.Features.Groups.DTOs;

namespace TechnicalSupport.Application.Features.Groups.Validators
{
    public class CreateGroupModelValidator : AbstractValidator<CreateGroupModel>
    {
        public CreateGroupModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Group name is required.")
                .MaximumLength(100).WithMessage("Group name cannot exceed 100 characters.");
        }
    }
}
