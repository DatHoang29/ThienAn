using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.VideoWall.Core.Dto.DeviceSetup;
using Shared.DTO.Constants.Localization;
using Module.VideoWall.Core.Exceptions;

namespace Module.VideoWall.Controllers.DeviceSetup.Validators
{
    /// <summary>
    /// Description: Validator cho lệnh Ping thiết bị VideoWall
    /// Created date: 11/09/2026
    /// </summary>
    public class VwPingDeviceInputValidator : AbstractValidator<VwPingDeviceInput>
    {
        public VwPingDeviceInputValidator(IStringLocalizer localizer)
        {
            RuleFor(u => u.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Description: Validator cho lệnh Probe thiết bị VideoWall
    /// Created date: 11/09/2026
    /// </summary>
    public class VwProbeDeviceInputValidator : AbstractValidator<VwProbeDeviceInput>
    {
        public VwProbeDeviceInputValidator(IStringLocalizer localizer)
        {
            RuleFor(u => u.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            RuleFor(u => u.WallNo)
                .GreaterThan(0)
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeGreaterThanZero])
                .When(u => u.WallNo.HasValue);
        }
    }

    /// <summary>
    /// Description: Validator cho lệnh đồng bộ nguồn tín hiệu VwSyncSourcesInput
    /// Created date: 11/09/2026
    /// </summary>
    public class VwSyncSourcesInputValidator : AbstractValidator<VwSyncSourcesInput>
    {
        public VwSyncSourcesInputValidator(IStringLocalizer localizer)
        {
            RuleFor(u => u.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Description: Bỏ trống ControllerId là chủ đích: nghĩa là xóa trạng thái lỗi xác thực cho toàn bộ bộ điều khiển.
    /// Created date: 11/09/2026
    /// </summary>
    public class VwResetDeviceAuthFailureInputValidator : AbstractValidator<VwResetDeviceAuthFailureInput>
    {
        public VwResetDeviceAuthFailureInputValidator(IStringLocalizer localizer)
        {
            RuleFor(u => u.ControllerId)
                .NotEmpty()
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .When(u => u.ControllerId != null);
        }
    }

    /// <summary>
    /// Description: Validator cho kịch bản dựng kịch bản hiển thị xuống thiết bị
    /// Created date: 11/09/2026
    /// </summary>
    public class VwSetupSceneInputValidator : AbstractValidator<VwSetupSceneInput>
    {
        public VwSetupSceneInputValidator(IStringLocalizer localizer)
        {
            RuleFor(u => u.ControllerId)
                .NotEmpty()
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            RuleFor(u => u.WallNo)
                .GreaterThan(0)
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeGreaterThanZero])
                .When(u => u.WallNo.HasValue);

            // Phải xác định được kịch bản bằng một trong hai khoá, nếu không thì không biết đẩy gì xuống tường.
            RuleFor(u => u)
                .Must(u => !string.IsNullOrWhiteSpace(u.SceneId) || !string.IsNullOrWhiteSpace(u.SceneCode))
                .WithMessage(x => localizer[BaseMsg.Vw.Validation.SceneIdOrSceneCodeRequired]);

            // Kích hoạt mà vẫn ở chế độ chạy thử là mâu thuẫn — chặn sớm để người dùng không tưởng
            // là đã đẩy lên tường trong khi thực tế không có lệnh nào được gửi.
            RuleFor(u => u.Activate)
                .Equal(false)
                .When(u => u.DryRun)
                .WithMessage(x => localizer[BaseMsg.Vw.Validation.CannotActivateInDryRun]);
        }
    }

    /// <summary>
    /// Description: Validator cho lệnh chuyển tiếp ISAPI thô VwISAPIPassthroughInput
    /// Created date: 13/09/2026
    /// </summary>
    public class VwISAPIPassthroughInputValidator : AbstractValidator<VwISAPIPassthroughInput>
    {
        private static readonly string[] AllowedMethods = ["GET", "POST", "PUT", "DELETE"];

        public VwISAPIPassthroughInputValidator(IStringLocalizer localizer)
        {
            RuleFor(u => u)
                .Must(u => !string.IsNullOrWhiteSpace(u.ControllerId) || u.Device != null)
                .WithMessage(x => localizer[BaseMsg.Vw.Validation.ControllerIdOrDeviceRequired]);

            RuleFor(u => u.Method)
                .NotEmpty()
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .Must(m => AllowedMethods.Contains(m.Trim().ToUpperInvariant()))
                    .WithMessage(x => localizer[BaseMsg.Vw.Validation.HttpMethodInvalid]);

            RuleFor(u => u.Path)
                .NotEmpty()
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            RuleFor(u => u.Device!.Ip)
                .NotEmpty()
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .When(u => u.Device != null);
        }
    }
}
