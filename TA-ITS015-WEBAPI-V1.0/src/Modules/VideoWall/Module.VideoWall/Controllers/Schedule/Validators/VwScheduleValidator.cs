using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.VideoWall.Core.Helper;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Schedule;
using Module.VideoWall.Infrastructure.Services.Schedule;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;

namespace Module.VideoWall.Controllers.Schedule.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Rule dùng chung cho Add và Update VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwScheduleValidator : AbstractValidator<VwAddScheduleInput>
    {
        public VwScheduleValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => (x.Code != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => (x.Name != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Time)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Weekdays)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.OnceDate)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Action)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // Chỉ nhận hành động đang hỗ trợ — close_screens / open_screens TẠM ẨN (xem VwScheduleConst.Action.Supported)
            RuleFor(x => x.Action)
                .Must(VwScheduleJobService.IsSupportedAction)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.Action != null || isCreate);

            RuleFor(x => x.TargetSceneId)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => x.Action == VwScheduleConst.Action.ActivateScene);

            /* ── Thời gian chạy theo từng loại lịch ── */

            RuleFor(x => x.Time)
                .Must(t => VwScheduleTimeHelper.TryParseTime(t, out _, out _))
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.ScheduleType == VwScheduleConst.ScheduleType.Daily
                        || x.ScheduleType == VwScheduleConst.ScheduleType.Weekly);

            RuleFor(x => x.Weekdays)
                .Must(w => VwScheduleTimeHelper.ParseWeekdays(w).Count > 0)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => x.ScheduleType == VwScheduleConst.ScheduleType.Weekly);

            RuleFor(x => x.OnceDate)
                .Must(d => VwScheduleTimeHelper.TryParseOnceDate(d, out _))
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.ScheduleType == VwScheduleConst.ScheduleType.Once);

            // Lịch một lần đang bật mà thời điểm đã qua thì sẽ không bao giờ chạy
            RuleFor(x => x.OnceDate)
                .Must(d => VwScheduleTimeHelper.TryParseOnceDate(d, out var at) && at > DateTime.Now)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.ScheduleType == VwScheduleConst.ScheduleType.Once
                        && x.Status == Shared.DTO.Enums.BaseEnums.StatusEnum.Enable
                        && VwScheduleTimeHelper.TryParseOnceDate(x.OnceDate, out _));

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Status)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Status])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => (x.Status != null && !isCreate) || isCreate)
                .IsInEnum()
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.ScheduleType)
                .NotNull()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Type])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .InclusiveBetween(VwScheduleConst.ScheduleType.Daily, VwScheduleConst.ScheduleType.Max)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Type])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.ScheduleType.HasValue || isCreate);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho thêm mới VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddScheduleValidator : AbstractValidator<VwAddScheduleInput>
    {
        public VwAddScheduleValidator(IStringLocalizer localizer)
        {
            Include(new VwScheduleValidator(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho cập nhật VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateScheduleValidator : AbstractValidator<VwUpdateScheduleInput>
    {
        public VwUpdateScheduleValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new VwScheduleValidator(localizer, isCreate: false));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho xóa VwSchedule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteScheduleValidator : AbstractValidator<VwDeleteScheduleInput>
    {
        public VwDeleteScheduleValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }
}
