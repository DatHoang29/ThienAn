using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.VideoWall.Core.Dto.EventTriggerLog;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;

namespace Module.VideoWall.Controllers.EventTriggerLog.Validators
{
    /// <summary>
    /// Author: Đạt
    /// Description: Rule cho việc ghi 1 dòng nhật ký thao tác thiết bị / kích hoạt kịch bản.
    ///
    ///              CHỈ validate phần client được quyền khai. Các trường server tự đóng dấu
    ///              (OrgId, ControllerName, TriggeredAt, OperatorName, OperatorIp) KHÔNG validate ở
    ///              đây vì giá trị client gửi lên đã bị bỏ qua — validate chúng chỉ tạo cảm giác an
    ///              toàn giả.
    ///
    ///              RequestPayload / ResponsePayload là cột BigString nên không giới hạn độ dài;
    ///              Message / Endpoint / StepName / Method thì writer tự cắt cho vừa cột, validator
    ///              chặn sớm để client biết mình gửi quá dài thay vì bị cắt âm thầm.
    /// Created date: 26/08/2026
    /// </summary>
    public class VwAddEventTriggerLogValidator : AbstractValidator<VwAddEventTriggerLogInput>
    {
        /// <summary>HTTP method hợp lệ của một lệnh ISAPI.</summary>
        private static readonly string[] AllowedMethods = ["GET", "POST", "PUT", "DELETE"];

        public VwAddEventTriggerLogValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Action)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Type])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length32)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Type])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength])
                .When(x => string.IsNullOrWhiteSpace(x.RuleId));

            RuleFor(x => x.Method)
                .Must(method => AllowedMethods.Contains(method!.Trim().ToUpperInvariant()))
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Type])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => !string.IsNullOrWhiteSpace(x.Method));

            RuleFor(x => x.StepName)
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Endpoint)
                .MaximumLength(EntityConst.Length512)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Type])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Message)
                .MaximumLength(EntityConst.Length512)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.StepOrder)
                .GreaterThan(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeGreaterThanZero])
                .When(x => x.StepOrder.HasValue);

            RuleFor(x => x.DurationMs)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.DurationMs.HasValue);

            RuleFor(x => x.RuleId)
                .MaximumLength(EntityConst.KeyFieldLength)
                    // BaseEntity.RuleId KHÔNG tồn tại trong BaseLocaleManager — dùng ID cho khớp
                    // cách rule EventTypeId ngay dưới đang dùng Type. Muốn nhãn riêng cho RuleId thì
                    // phải bổ sung key vào Shared.DTO/Constants/Localization/BaseLocaleManager.cs.
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength])
                .When(x => !string.IsNullOrWhiteSpace(x.RuleId));

            RuleFor(x => x.EventTypeId)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Type])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength])
                .When(x => !string.IsNullOrWhiteSpace(x.EventTypeId));
        }
    }
}
