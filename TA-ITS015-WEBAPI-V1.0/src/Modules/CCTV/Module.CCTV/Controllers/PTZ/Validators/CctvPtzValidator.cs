using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.CCTV.Controllers.CctvPTZ.Dto;
using Modules.CCTV.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Module.CCTV.Controllers.CctvPtz.Validators
{
    /// <summary>
    /// Rule "DeviceId HOẶC Ip" dùng chung cho tất cả PTZ command.
    /// FE có thể định danh camera bằng IP (chuẩn toàn hệ thống) hoặc DeviceId (GUID NxVMS).
    /// Tách ra để không lặp code ở từng validator.
    /// </summary>
    public class CctvPtzBaseValidator<T> : AbstractValidator<T> where T : class
    {
        protected void AddDeviceIdOrIpRule(
            Func<T, string?> deviceIdSelector,
            Func<T, string?> ipSelector,
            IStringLocalizer localizer)
        {
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(deviceIdSelector(x)) || !string.IsNullOrWhiteSpace(ipSelector(x)))
                    .WithName(localizer[BaseMsg.BaseEntity.DeviceId])
                    .WithMessage(localizer[BaseMsg.CctvDevice.Validation.DeviceIdOrIpRequired]);
        }
    }

    public class CctvPtzPositionValidator : CctvPtzBaseValidator<CctvPtzPositionInput>
    {
        public CctvPtzPositionValidator(IStringLocalizer localizer)
        {
            AddDeviceIdOrIpRule(x => x.DeviceId, x => x.Ip, localizer);

            // Pan, Tilt, Zoom, Speed đều phải trong [-1, 1]
            RuleFor(x => x.Pan)
                .InclusiveBetween(-1, 1)
                    .WithName(localizer[BaseMsg.BaseEntity.Pan])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeBetweenMinusOneAndOne]);

            RuleFor(x => x.Tilt)
                .InclusiveBetween(-1, 1)
                    .WithName(localizer[BaseMsg.BaseEntity.Tilt])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeBetweenMinusOneAndOne]);

            RuleFor(x => x.Zoom)
                .InclusiveBetween(-1, 1)
                    .WithName(localizer[BaseMsg.BaseEntity.Zoom])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeBetweenMinusOneAndOne]);

        }
    }


    public class CctvPtzStopValidator : CctvPtzBaseValidator<CctvPtzStopInput>
    {
        public CctvPtzStopValidator(IStringLocalizer localizer)
        {
            AddDeviceIdOrIpRule(x => x.DeviceId, x => x.Ip, localizer);
        }
    }

    public class CctvActivatePresetValidator : CctvPtzBaseValidator<CctvActivatePresetInput>
    {
        public CctvActivatePresetValidator(IStringLocalizer localizer)
        {
            AddDeviceIdOrIpRule(x => x.DeviceId, x => x.Ip, localizer);

            RuleFor(x => x.PresetId)
                .NotEmpty()
                    .WithName(localizer[BaseMsg.BaseEntity.PresetId])
                    .WithMessage(localizer[BaseMsg.BaseValidation.Required]);
        }
    }

    public class CctvActivateTourValidator : CctvPtzBaseValidator<CctvActivateTourInput>
    {
        public CctvActivateTourValidator(IStringLocalizer localizer)
        {
            AddDeviceIdOrIpRule(x => x.DeviceId, x => x.Ip, localizer);

            RuleFor(x => x.TourId)
                .NotEmpty()
                    .WithName(localizer[BaseMsg.BaseEntity.TourId])
                    .WithMessage(localizer[BaseMsg.BaseValidation.Required]);
        }
    }

    public class CctvGetPtzPositionValidator : CctvPtzBaseValidator<CctvGetPtzPositionInput>
    {
        public CctvGetPtzPositionValidator(IStringLocalizer localizer)
        {
            AddDeviceIdOrIpRule(x => x.DeviceId, x => x.Ip, localizer);
        }
    }

    public class CctvGetPtzLimitsValidator : CctvPtzBaseValidator<CctvGetPtzLimitsInput>
    {
        public CctvGetPtzLimitsValidator(IStringLocalizer localizer)
        {
            AddDeviceIdOrIpRule(x => x.DeviceId, x => x.Ip, localizer);
        }
    }

    public class CctvGetPresetsValidator : CctvPtzBaseValidator<CctvGetPresetInput>
    {
        public CctvGetPresetsValidator(IStringLocalizer localizer)
        {
            AddDeviceIdOrIpRule(x => x.DeviceId, x => x.Ip, localizer);
        }
    }

    /// <summary>
    /// Đổi tên preset: bắt buộc PresetId + tên mới (đổi trên NxVMS rồi đồng bộ Config).
    /// Date: 21/10/2026
    /// </summary>
    public class CctvUpdatePresetValidator : CctvPtzBaseValidator<CctvUpdatePresetInput>
    {
        public CctvUpdatePresetValidator(IStringLocalizer localizer)
        {
            AddDeviceIdOrIpRule(x => x.DeviceId, x => x.Ip, localizer);

            RuleFor(x => x.PresetId)
                .NotEmpty()
                    .WithName(localizer[BaseMsg.BaseEntity.PresetId])
                    .WithMessage(localizer[BaseMsg.BaseValidation.Required]);

            RuleFor(x => x.PresetName)
                .NotEmpty()
                    .WithName(localizer[BaseMsg.BaseEntity.Name])
                    .WithMessage(localizer[BaseMsg.BaseValidation.Required]);
        }
    }

    public class CctvDeletePresetValidator : CctvPtzBaseValidator<CctvDeletePresetInput>
    {
        public CctvDeletePresetValidator(IStringLocalizer localizer)
        {
            AddDeviceIdOrIpRule(x => x.DeviceId, x => x.Ip, localizer);

            RuleFor(x => x.PresetId)
                .NotEmpty()
                    .WithName(localizer[BaseMsg.BaseEntity.PresetId])
                    .WithMessage(localizer[BaseMsg.BaseValidation.Required]);
        }
    }

    public class CctvGetToursValidator : CctvPtzBaseValidator<CctvGetToursInput>
    {
        public CctvGetToursValidator(IStringLocalizer localizer)
        {
            AddDeviceIdOrIpRule(x => x.DeviceId, x => x.Ip, localizer);
        }
    }
}
