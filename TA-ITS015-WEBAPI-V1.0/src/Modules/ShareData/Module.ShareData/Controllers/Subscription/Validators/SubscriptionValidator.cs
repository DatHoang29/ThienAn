using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Subscription;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Msg = Module.ShareData.Core.Exceptions.BaseMsg.ShareData;

namespace Module.ShareData.Controllers.Subscription.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Rule dùng chung cho Add và Update đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class SubscriptionValidator : AbstractValidator<ShareDataAddSubscriptionInput>
    {
        public SubscriptionValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.PartnerId)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PartnerId])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => isCreate);

            // DatatypeId là mã chuỗi lấy từ bảng cấu hình (nhóm shareData_type),
            // không còn giới hạn cứng 101–111 nên chỉ kiểm tra bắt buộc nhập.
            RuleFor(x => x.DatatypeId)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.DatatypeId])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);


            RuleFor(x => x.Direction)
                .IsInEnum()
                    .WithName(localizer[Msg.Entity.Direction])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.Direction.HasValue);
            RuleFor(x => x.Mode)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.Mode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .IsInEnum()
                    .WithName(localizer[Msg.Entity.Mode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.Format)
                .IsInEnum()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            // Theo sự kiện (Mode = EVENT) chỉ BỔ SUNG việc gửi ngay khi có sự kiện — không thay thế
            // lịch gửi, nên bắt buộc chọn nguồn sự kiện riêng, độc lập với việc có lịch hay không.
            RuleFor(x => x.EventSourceId)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.EventSourceId])
                    .WithMessage(localizer[Msg.Validation.EventSourceRequired])
                .When(x => x.Mode == BaseEnums.SubMode.Event);

            // Lịch gửi bắt buộc với đăng ký có GỬI ĐI, bất kể Mode — Mode = EVENT không thay thế lịch.
            // Chiều NHẬN thì không có lịch: thời điểm nhận do đối tác quyết định, không phải mình.
            RuleFor(x => x.Schedule)
                .NotNull()
                    .WithName(localizer[Msg.Entity.Schedule])
                    .WithMessage(localizer[Msg.Validation.ScheduleRequired])
                .When(x => x.Direction != BaseEnums.Direction.Inbound);

            When(x => x.Schedule != null, () =>
            {
                RuleFor(x => x.Schedule!.Kind)
                    .Must(v => v == null || ShareDataConst.ScheduleKind.All.Contains(v))
                        .WithName(localizer[Msg.Entity.Schedule])
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

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

                // Đảo ngược quyết định "khung giờ qua đêm" (chốt 08/10/2026): EndTime không còn
                // được phép nhỏ hơn StartTime khi cả hai cùng có giá trị.
                RuleFor(x => x.Schedule)
                    .Must(schedule =>
                        !TimeSpan.TryParse(schedule!.StartTime, out var start) ||
                        !TimeSpan.TryParse(schedule.EndTime, out var end) ||
                        end >= start)
                        .WithName(localizer[Msg.Entity.Schedule])
                        .WithMessage(localizer[Msg.Validation.ScheduleTimeRangeInvalid])
                    .When(x => !string.IsNullOrWhiteSpace(x.Schedule!.StartTime) && !string.IsNullOrWhiteSpace(x.Schedule!.EndTime));
            });

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }

    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho thêm mới đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class AddSubscriptionValidator : AbstractValidator<ShareDataAddSubscriptionInput>
    {
        public AddSubscriptionValidator(IStringLocalizer localizer)
        {
            Include(new SubscriptionValidator(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho cập nhật đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class UpdateSubscriptionValidator : AbstractValidator<ShareDataUpdateSubscriptionInput>
    {
        public UpdateSubscriptionValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new SubscriptionValidator(localizer, isCreate: false));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho xóa đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class DeleteSubscriptionValidator : AbstractValidator<ShareDataDeleteSubscriptionInput>
    {
        public DeleteSubscriptionValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho hủy đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class CancelSubscriptionValidator : AbstractValidator<ShareDataCancelSubscriptionInput>
    {
        public CancelSubscriptionValidator(IStringLocalizer localizer)
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
    /// Author: HieuNV
    /// Description: Validator cho từ chối đăng ký chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class RejectSubscriptionValidator : AbstractValidator<ShareDataRejectSubscriptionInput>
    {
        public RejectSubscriptionValidator(IStringLocalizer localizer)
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
