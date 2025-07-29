using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using TechnicalSupport.Application.Features.Admin.DTOs;

namespace TechnicalSupport.Application.Features.Admin.Validators
{
    public class UpdateUserByAdminModelValidator : AbstractValidator<UpdateUserByAdminModel>
    {
        public UpdateUserByAdminModelValidator()
        {
            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display name is required.");

            RuleFor(x => x.Roles)
                .NotEmpty().WithMessage("User must have at least one role.");
        }
    }
}
