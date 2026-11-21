using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.CCTV.Controllers.CctvMedia.Dto;
using Modules.CCTV.Core.Exceptions;
using Shared.DTO.Constants.Application;
using Shared.Utility.Helpers;
using System.Text.RegularExpressions;

namespace Module.CCTV.Controllers.CctvMedia.Validators
{
    public class CctvWebRtcUrlValidator : AbstractValidator<CctvWebRtcUrlInput>
    {
        private static readonly string[] _allowedStreams = { "primary", "secondary" };

        public CctvWebRtcUrlValidator(IStringLocalizer localizer)
        {
            // Chấp nhận DeviceId HOẶC Ip — FE truyền 1 trong 2 là hợp lệ.
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.DeviceId) || !string.IsNullOrWhiteSpace(x.Ip))
                    .WithName(localizer[BaseMsg.BaseEntity.DeviceId])
                    .WithMessage(localizer[BaseMsg.CctvDevice.Validation.DeviceIdOrIpRequired]);

            // Stream phải là "primary" hoặc "secondary"
            RuleFor(x => x.Stream)
                .Must(s => _allowedStreams.Contains(s?.ToLower()))
                    .WithName(localizer[BaseMsg.BaseEntity.Stream])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);

            // Resolution phải đúng format: số x số — ví dụ "1920x1080", "640x360"
            RuleFor(x => x.Resolution)
                .Matches(@"^\d+x\d+$")
                    .WithName(localizer[BaseMsg.BaseEntity.Resolution])
                    .WithMessage(localizer[BaseMsg.BaseValidation.InvalidResolutionFormat]);

        }
    }

    public class CctvMediaDownloadValidator : AbstractValidator<CctvMediaDownloadInput>
    {
        private static readonly string[] _allowedStreams = { "primary", "secondary" };

        public CctvMediaDownloadValidator(IStringLocalizer localizer)
        {
            // Chấp nhận DeviceId HOẶC Ip — FE truyền 1 trong 2 là hợp lệ.
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.DeviceId) || !string.IsNullOrWhiteSpace(x.Ip))
                    .WithName(localizer[BaseMsg.BaseEntity.DeviceId])
                    .WithMessage(localizer[BaseMsg.CctvDevice.Validation.DeviceIdOrIpRequired]);

            RuleFor(x => x.Stream)
                .Must(s => _allowedStreams.Contains(s?.ToLower()))
                    .WithName(localizer[BaseMsg.BaseEntity.Stream])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.Resolution)
                .Matches(@"^\d+x\d+$")
                    .WithName(localizer[BaseMsg.BaseEntity.Resolution])
                    .WithMessage(localizer[BaseMsg.BaseValidation.InvalidResolutionFormat]);

            // (21/10/2026) Khoảng thời gian tải: ToDate > FromDate, tối đa 60 phút / lần
            // (Cctv Proxy cũng chặn durationMs theo cùng hằng số).
            RuleFor(x => x.ToDate)
                .GreaterThan(x => x.FromDate)
                    .WithName(localizer[BaseMsg.CctvMedia.Entity.ToDate])
                    .WithMessage(localizer[BaseMsg.CctvMedia.Validation.ToDateAfterFromDate]);

            RuleFor(x => x)
                .Must(x => x.ToDate - x.FromDate <= TimeSpan.FromMinutes(BaseHelper.CctvStreamTicketHp.MaxMediaDownloadMinutes))
                    .When(x => x.ToDate > x.FromDate)
                    .WithName(localizer[BaseMsg.CctvMedia.Entity.DurationMs])
                    .WithMessage(localizer[BaseMsg.CctvMedia.Validation.DownloadDurationExceeded]);

            RuleFor(x => x.VideoCodec)
                .Must(c => _allowedVideoCodecs.Contains(c?.ToLower()))
                    .WithName(localizer[BaseMsg.CctvMedia.Entity.VideoCodec])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);
        }

        private static readonly string[] _allowedVideoCodecs = { "h264", "h265", "mpeg4", "mjpeg" };
    }

    /// <summary>
    /// Validate input lấy danh sách footage: khoảng thời gian hợp lệ, PeriodType thuộc danh sách cho phép.
    /// Date: 21/10/2026
    /// </summary>
    public class CctvFootageValidator : AbstractValidator<CctvFootageInput>
    {
        private static readonly string[] _allowedPeriodTypes = { "recording", "motion", "analytics" };

        public CctvFootageValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.DeviceId) || !string.IsNullOrWhiteSpace(x.Ip))
                    .WithName(localizer[BaseMsg.BaseEntity.DeviceId])
                    .WithMessage(localizer[BaseMsg.CctvDevice.Validation.DeviceIdOrIpRequired]);

            RuleFor(x => x.ToDate)
                .GreaterThan(x => x.FromDate)
                    .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
                    .WithName(localizer[BaseMsg.CctvMedia.Entity.ToDate])
                    .WithMessage(localizer[BaseMsg.CctvMedia.Validation.ToDateAfterFromDate]);

            RuleFor(x => x.PeriodType)
                .Must(p => _allowedPeriodTypes.Contains(p!.ToLower()))
                    .When(x => !string.IsNullOrWhiteSpace(x.PeriodType))
                    .WithName(localizer[BaseMsg.CctvMedia.Entity.PeriodType])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.DetailLevelMs)
                .GreaterThanOrEqualTo(0)
                    .When(x => x.DetailLevelMs.HasValue)
                    .WithName(localizer[BaseMsg.CctvMedia.Entity.DurationMs])
                    .WithMessage(localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);
        }
    }

    public class CctvSnapshotValidator : AbstractValidator<CctvSnapshotInput>
    {
        public CctvSnapshotValidator(IStringLocalizer localizer)
        {
            // Chấp nhận DeviceId HOẶC Ip — FE truyền 1 trong 2 là hợp lệ.
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.DeviceId) || !string.IsNullOrWhiteSpace(x.Ip))
                    .WithName(localizer[BaseMsg.BaseEntity.DeviceId])
                    .WithMessage(localizer[BaseMsg.CctvDevice.Validation.DeviceIdOrIpRequired]);

            // Size nếu có truyền phải đúng format "1920x1080"
            RuleFor(x => x.Size)
                .Matches(@"^\d+x\d+$")
                    .When(x => !string.IsNullOrWhiteSpace(x.Size))
                    .WithName(localizer[BaseMsg.BaseEntity.Size])
                    .WithMessage(localizer[BaseMsg.BaseValidation.InvalidResolutionFormat]);

            // Format chỉ chấp nhận jpg, png, tif
            RuleFor(x => x.Format)
                .Must(f => new[] { "jpg", "png", "tif" }.Contains(f?.ToLower()))
                    .WithName(localizer[BaseMsg.BaseEntity.Format])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);

            // (21/10/2026) Crop ghép thẳng vào query NxVMS → chỉ nhận đúng định dạng "x,y,wxh" (số thực 0..1).
            RuleFor(x => x.Crop)
                .Matches(@"^(0|1|0?\.\d+)(,(0|1|0?\.\d+)){2}x(0|1|0?\.\d+)$")
                    .When(x => !string.IsNullOrWhiteSpace(x.Crop))
                    .WithName(localizer[BaseMsg.CctvMedia.Entity.Crop])
                    .WithMessage(localizer[BaseMsg.CctvMedia.Validation.InvalidCrop]);

            RuleFor(x => x.Rotation)
                .Must(r => r is 0 or 90 or 180 or 270)
                    .When(x => x.Rotation.HasValue)
                    .WithName(localizer[BaseMsg.CctvMedia.Entity.Rotation])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);
        }
    }

    /// <summary>
    /// Validate input lấy URL snapshot qua Cctv Proxy — cùng quy tắc với CctvSnapshotValidator.
    /// Author: kien.le - Date: 2026-10-06
    /// </summary>
    public class CctvSnapshotUrlValidator : AbstractValidator<CctvSnapshotUrlInput>
    {
        public CctvSnapshotUrlValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.DeviceId) || !string.IsNullOrWhiteSpace(x.Ip))
                    .WithName(localizer[BaseMsg.BaseEntity.DeviceId])
                    .WithMessage(localizer[BaseMsg.CctvDevice.Validation.DeviceIdOrIpRequired]);

            RuleFor(x => x.Size)
                .Matches(@"^\d+x\d+$")
                    .When(x => !string.IsNullOrWhiteSpace(x.Size))
                    .WithName(localizer[BaseMsg.BaseEntity.Size])
                    .WithMessage(localizer[BaseMsg.BaseValidation.InvalidResolutionFormat]);

            RuleFor(x => x.Format)
                .Must(f => new[] { "jpg", "png", "tif" }.Contains(f?.ToLower()))
                    .WithName(localizer[BaseMsg.BaseEntity.Format])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);
        }
    }
}