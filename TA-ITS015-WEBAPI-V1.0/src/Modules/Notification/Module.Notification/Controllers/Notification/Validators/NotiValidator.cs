using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.Notification.Core.Constants;
using Modules.Notification.Core.Enums;
using Modules.Notification.Core.Exceptions;
using Modules.Notification.Core.Messages;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;

namespace Modules.Notification.Controllers.Notification.Validators
{
    /// <summary>
    /// Author: SonTH
    /// Description: Validate lệnh gửi thông báo (contract cho mọi module gọi tới).
    ///              Chỉ kiểm định dạng/độ dài từng trường; ràng buộc theo TargetType
    ///              (UserIds/OrgIds có đủ không) nằm ở NotiSendHandler vì cần gọi sang
    ///              phân hệ System mới biết có người nhận hay không.
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiSendCommandValidator : AbstractValidator<SendNotificationCommand>
    {
        public NotiSendCommandValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length256)
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Content)
                .NotEmpty()
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required]);

            RuleFor(x => x.Category)
                .NotEmpty()
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Severity)
                .Must(s => Enum.TryParse<NotiEnums.SeverityEnum>(s, true, out _))
                .When(x => !string.IsNullOrWhiteSpace(x.Severity))
                .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required]);

            RuleFor(x => x.IdempotencyKey)
                .MaximumLength(EntityConst.Length128)
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.SourceKey)
                .MaximumLength(EntityConst.Length128)
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }
    }
}
