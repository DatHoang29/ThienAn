using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Modules.WP.Controllers.TrafficLaw.Dto;
using Modules.WP.Core.Exceptions;

namespace Modules.WP.Controllers.TrafficLaw.Validators
{
    /// <summary>
    /// Description: Khoảng hợp lệ của ngày ban hành: từ 2 năm trước đến 1 năm sau so với hôm nay (FE editTrafficLaw dùng cùng mốc).
    /// Created date: 30/10/2026
    /// </summary>
    internal static class TrafficLawPublishTimeRange
    {
        public static DateTime Min => DateTime.Today.AddYears(-2);
        public static DateTime Max => DateTime.Today.AddYears(1);

        public static bool IsValid(DateTime? value) => value == null || (value.Value.Date >= Min && value.Value.Date <= Max);

        public static string Message(IStringLocalizer localizer)
            => localizer[BaseMsg.WP.Validation.PublishTimeOutOfRange, Min.ToString("dd/MM/yyyy"), Max.ToString("dd/MM/yyyy")];
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validate dữ liệu Add/Update/Delete cho quy định giao thông hệ thống WP (TrafficLaw)
    /// Created date: 03/07/2026
    /// </summary>
    public class AddTrafficLawValidator:AbstractValidator<WpAddTrafficLawInput>
    {
        public AddTrafficLawValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Title).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Title]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                 .MaximumLength(512).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.PublishTime).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.PublishTime]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .Must(TrafficLawPublishTimeRange.IsValid).WithMessage(x => TrafficLawPublishTimeRange.Message(localizer));
            RuleFor(x => x.Code).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Content).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Content]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
            .MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Author).MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Remark).MaximumLength(512).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

        }

    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Update Traffic Law
    /// Created date: 14/08/2026
    /// </summary>

    public class UpdateTrafficLawValidator:AbstractValidator<WpUpdateTrafficLawInput>
    {
        public UpdateTrafficLawValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Title).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Title]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                 .MaximumLength(512).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.PublishTime).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.PublishTime]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .Must(TrafficLawPublishTimeRange.IsValid).WithMessage(x => TrafficLawPublishTimeRange.Message(localizer));
            RuleFor(x => x.Code).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Content).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Content]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
            .MaximumLength(-1).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Author).MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.Remark).MaximumLength(512).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

        }
    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Delete Traffic Law
    /// Created date: 14/08/2026
    /// </summary>

    public class DeleteTrafficLawValidator:AbstractValidator<WpDeleteTrafficLawInput>
    {
          public DeleteTrafficLawValidator(IStringLocalizer localizer)
            {
                RuleFor(x => x.ID).NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
            }
    }
}
