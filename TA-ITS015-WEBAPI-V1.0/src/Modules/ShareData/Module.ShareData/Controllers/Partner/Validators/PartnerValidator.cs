using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.ShareData.Core.Dto.Partner;
using Module.ShareData.Core.Exceptions;
using Modules.Integration.Delivery.Base.Enums;
using Modules.Integration.Delivery.Base.Helpers;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;

namespace Module.ShareData.Controllers.Partner.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Rule dùng chung cho Add và Update đối tác chia sẻ
    /// Created date: 2026-08-04
    /// Modified date: 10/11/2026
    /// </summary>
    public class PartnerValidator : AbstractValidator<ShareDataAddPartnerInput>
    {
        public PartnerValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => (x.Code != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length32)
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

            RuleFor(x => x.Address)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => ((x.Address != null && !isCreate) || isCreate) && x.ConnectionType != ConnectionType.File)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Port)
                .NotNull()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => ((x.Port != null && !isCreate) || isCreate) && x.ConnectionType != ConnectionType.File)
                .InclusiveBetween(1, 65535)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.Port.HasValue);
            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.OrderNo.HasValue);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // Endpoint: chỉ phần đường dẫn (VD: /sharedata/inbox), ghép cùng Address/Port khi cần dùng tới
            // (xem preview "Địa chỉ đầy đủ" ở FE) — không bắt buộc, nhưng nếu có phải bắt đầu bằng "/".
            RuleFor(x => x.EndPointApiUrl)
                .MaximumLength(EntityConst.Length256)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength])
                .Must(v => string.IsNullOrWhiteSpace(v)
                    || v!.Trim().StartsWith("/")
                    || v!.Trim().StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || v!.Trim().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => !string.IsNullOrWhiteSpace(x.EndPointApiUrl));

            RuleFor(x => x.ConnectionType)
                .IsInEnum()
                    .WithName(localizer[BaseMsg.BaseEntity.Info])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue])
                .When(x => x.ConnectionType.HasValue);

            When(x => !string.IsNullOrWhiteSpace(x.ConnectionConfig), () =>
            {
                RuleFor(x => x.ConnectionConfig)
                    .Custom((config, ctx) =>
                    {
                        var issue = ConnectConfigValidator.Check(config);
                        var msgKey = issue switch
                        {
                            ConnectConfigIssue.NotJsonObject => BaseMsg.ShareData.Validation.ConnectConfigNotJsonObject,
                            ConnectConfigIssue.SyntaxError => BaseMsg.ShareData.Validation.ConnectConfigSyntaxError,
                            ConnectConfigIssue.EmptyKey => BaseMsg.ShareData.Validation.ConnectConfigEmptyKey,
                            ConnectConfigIssue.ReservedKey => BaseMsg.ShareData.Validation.ConnectConfigReservedKey,
                            ConnectConfigIssue.ObsoleteKey => BaseMsg.ShareData.Validation.ConnectConfigObsoleteKey,
                            ConnectConfigIssue.DuplicateKey => BaseMsg.ShareData.Validation.ConnectConfigDuplicateKey,
                            ConnectConfigIssue.NestedValue => BaseMsg.ShareData.Validation.ConnectConfigNestedValue,
                            ConnectConfigIssue.InvalidTimeout => BaseMsg.ShareData.Validation.ConnectConfigInvalidTimeout,
                            ConnectConfigIssue.InvalidBoolean => BaseMsg.ShareData.Validation.ConnectConfigInvalidBoolean,
                            ConnectConfigIssue.UnsupportedAuthType => BaseMsg.ShareData.Validation.ConnectConfigUnsupportedAuthType,
                            ConnectConfigIssue.UnsupportedEncryptor => BaseMsg.ShareData.Validation.ConnectConfigUnsupportedEncryptor,
                            ConnectConfigIssue.FileNamePatternNoToken => BaseMsg.ShareData.Validation.ConnectConfigFileNamePatternNoToken,
                            _ => null
                        };

                        if (msgKey != null)
                        {
                            ctx.AddFailure(localizer[msgKey]);
                        }
                    });
            });
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho thêm mới đối tác chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class AddPartnerValidator : AbstractValidator<ShareDataAddPartnerInput>
    {
        public AddPartnerValidator(IStringLocalizer localizer)
        {
            Include(new PartnerValidator(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho cập nhật đối tác chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class UpdatePartnerValidator : AbstractValidator<ShareDataUpdatePartnerInput>
    {
        public UpdatePartnerValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new PartnerValidator(localizer, isCreate: false));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho xóa đối tác chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class DeletePartnerValidator : AbstractValidator<ShareDataDeletePartnerInput>
    {
        public DeletePartnerValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho mở phiên kết nối
    /// Created date: 2026-08-04
    /// </summary>
    public class ConnectPartnerValidator : AbstractValidator<ShareDataConnectPartnerInput>
    {
        public ConnectPartnerValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho ngắt phiên kết nối
    /// Created date: 2026-08-04
    /// </summary>
    public class DisconnectPartnerValidator : AbstractValidator<ShareDataDisconnectPartnerInput>
    {
        public DisconnectPartnerValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            RuleFor(x => x.Reason)
                .MaximumLength(EntityConst.Length64)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }
    }
}
