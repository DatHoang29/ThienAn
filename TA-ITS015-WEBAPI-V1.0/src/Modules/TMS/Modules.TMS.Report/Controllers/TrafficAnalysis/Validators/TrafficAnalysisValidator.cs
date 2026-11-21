using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Report.Controllers.TrafficAnalysis.Dto;
using Modules.TMS.Report.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.TMS.Report.Controllers.TrafficAnalysis.Validators
{
	/// <summary>
	/// Author: Tuan HA
	/// Create Date: 10/02/2024
	/// </summary>
	public class TrafficAnalysisValidator : AbstractValidator<PageTrafficAnalysisInput>
	{
		public TrafficAnalysisValidator(IStringLocalizer localizer)
		{
			RuleFor(x => x.LengthFrom)
				.GreaterThanOrEqualTo(0)
				.WithName(x => localizer[BaseMsg.RptTrafficAnalysis.Entity.LengthFrom])
				.WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);
			RuleFor(x => x.LengthTo)
				.GreaterThanOrEqualTo(0)
			    .WithName(x => localizer[BaseMsg.RptTrafficAnalysis.Entity.LengthTo])
				.WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);
			RuleFor(x => x.LicensePlate)
				.MaximumLength(10)
				.WithName(x => localizer[BaseMsg.RptTrafficAnalysis.Entity.LicensePlate])
				.WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
		}
	}
}
