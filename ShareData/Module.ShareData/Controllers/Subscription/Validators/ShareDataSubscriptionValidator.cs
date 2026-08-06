using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Subscription;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Msg = Module.ShareData.Core.Exceptions.BaseMsg.ShareData;

namespace Module.ShareData.Controllers.Subscription.Validators
{
    /// <summary>
    /// Description: Rule dùng chung cho Add và Update đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataSubscriptionValidator : AbstractValidator<ShareDataAddSubscriptionInput>
    {
        public ShareDataSubscriptionValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.PartnerId)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PartnerId])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => isCreate);

            RuleFor(x => x.DatatypeId)
                .NotNull()
                    .WithName(localizer[Msg.Entity.DatatypeId])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => isCreate)
                .InclusiveBetween(ShareDataConst.Datatype.Min, ShareDataConst.Datatype.Max)
                    .WithName(localizer[Msg.Entity.DatatypeId])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.DatatypeId.HasValue);

            RuleFor(x => x.Direction)
                .Must(v => v == null || ShareDataConst.SubDirection.All.Contains(v))
                    .WithName(localizer[Msg.Entity.Direction])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.Mode)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.Mode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .Must(v => v == null || ShareDataConst.SubMode.All.Contains(v))
                    .WithName(localizer[Msg.Entity.Mode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.Format)
                .Must(v => v == null || ShareDataConst.PublishFormat.All.Contains(v))
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.Priority)
                .InclusiveBetween(0, 10)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.Priority.HasValue);

            RuleFor(x => x.DebounceSec)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.DebounceSec.HasValue);

            // Chế độ theo sự kiện bắt buộc chọn nguồn sự kiện.
            RuleFor(x => x.EventSourceId)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.EventSourceId])
                    .WithMessage(localizer[Msg.Validation.EventSourceRequired])
                .When(x => x.Mode == ShareDataConst.SubMode.Event);

            // Chế độ định kỳ bắt buộc có lịch gửi hợp lệ.
            RuleFor(x => x.Schedule)
                .NotNull()
                    .WithName(localizer[Msg.Entity.Schedule])
                    .WithMessage(localizer[Msg.Validation.ScheduleRequired])
                .When(x => x.Mode == ShareDataConst.SubMode.Periodic);

            When(x => x.Mode == ShareDataConst.SubMode.Periodic && x.Schedule != null, () =>
            {
                RuleFor(x => x.Schedule!.Kind)
                    .Must(v => v == null || ShareDataConst.ScheduleKind.All.Contains(v))
                        .WithName(localizer[Msg.Entity.Schedule])
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

                RuleFor(x => x.Schedule!.UpdateDelaySec)
                    .GreaterThanOrEqualTo(5)
                        .WithName(localizer[Msg.Entity.Schedule])
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                    .When(x => x.Schedule!.UpdateDelaySec.HasValue);

                RuleFor(x => x.Schedule!.DurationMinutes)
                    .InclusiveBetween(1, 1440)
                        .WithName(localizer[Msg.Entity.Schedule])
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                    .When(x => x.Schedule!.DurationMinutes.HasValue);

                RuleFor(x => x.Schedule!.DaysOfWeek)
                    .NotEmpty()
                        .WithName(localizer[Msg.Entity.Schedule])
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                    .When(x => x.Schedule!.Kind == ShareDataConst.ScheduleKind.Daily);
            });

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }

    }

    /// <summary>
    /// Description: Validator cho thêm mới đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataAddSubscriptionValidator : AbstractValidator<ShareDataAddSubscriptionInput>
    {
        public ShareDataAddSubscriptionValidator(IStringLocalizer localizer)
        {
            Include(new ShareDataSubscriptionValidator(localizer));
        }
    }

    /// <summary>
    /// Description: Validator cho cập nhật đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataUpdateSubscriptionValidator : AbstractValidator<ShareDataUpdateSubscriptionInput>
    {
        public ShareDataUpdateSubscriptionValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new ShareDataSubscriptionValidator(localizer, isCreate: false));
        }
    }

    /// <summary>
    /// Description: Validator cho xóa đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataDeleteSubscriptionValidator : AbstractValidator<ShareDataDeleteSubscriptionInput>
    {
        public ShareDataDeleteSubscriptionValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Description: Validator cho hủy đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataCancelSubscriptionValidator : AbstractValidator<ShareDataCancelSubscriptionInput>
    {
        public ShareDataCancelSubscriptionValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            RuleFor(x => x.Reason)
                .MaximumLength(EntityConst.Length256)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Description: Validator cho từ chối đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataRejectSubscriptionValidator : AbstractValidator<ShareDataRejectSubscriptionInput>
    {
        public ShareDataRejectSubscriptionValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            RuleFor(x => x.Reason)
                .MaximumLength(EntityConst.Length256)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }
    }
}
