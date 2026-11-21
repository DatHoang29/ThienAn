
using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VMS.Controllers.Current.Validators;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.VMS.Controllers.Process.Validators
{
    public class VmsProcessValidator : AbstractValidator<VmsProcess>
    {
		public VmsProcessValidator(IStringLocalizer localizer, bool isCreate = true)
		{
			RuleFor(x => x.Brightness)
				.GreaterThanOrEqualTo(0)
				.WithName(x => localizer[BaseMsg.VmsProcess.Entity.Brightness])
			    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

			RuleFor(x => x.Type)
				.MaximumLength(EntityConst.ConfigFieldLength)
				.WithName(x => localizer[BaseMsg.VmsProcess.Entity.Type])
				.WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

			// >=0
			RuleFor(x => x.Priority)
				.GreaterThanOrEqualTo(0)
					.WithName(x => localizer[BaseMsg.VmsProcess.Entity.Priority])
					.WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);
		}
	}
  
}
