using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Partner;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;

namespace Module.ShareData.Controllers.Partner.Validators
{
    /// <summary>
    /// Description: Rule dùng chung cho Add và Update đối tác chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataPartnerValidator : AbstractValidator<ShareDataAddPartnerInput>
    {
        public ShareDataPartnerValidator(IStringLocalizer localizer, bool isCreate = true)
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
                .When(x => (x.Address != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Port)
                .NotNull()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => (x.Port != null && !isCreate) || isCreate)
                .InclusiveBetween(1, 65535)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.Port.HasValue);

            RuleFor(x => x.ProtocolProfile)
                .Must(v => v == null || ShareDataConst.ProtocolProfile.All.Contains(v))
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.InitiatorMode)
                .Must(v => v == null || ShareDataConst.InitiatorMode.All.Contains(v))
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.Status)
                .Must(v => v == null || ShareDataConst.PartnerStatus.All.Contains(v))
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Status])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.Username)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => (x.Username != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // Thêm mới bắt buộc có mật khẩu; cập nhật để trống nghĩa là giữ nguyên.
            RuleFor(x => x.Password)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => isCreate);

            RuleFor(x => x.HeartbeatMaxSec)
                .InclusiveBetween(5, 300)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.HeartbeatMaxSec.HasValue);

            RuleFor(x => x.DatagramSize)
                .InclusiveBetween(512, 65535)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.DatagramSize.HasValue);

            RuleFor(x => x.ResponseTimeoutSec)
                .InclusiveBetween(3, 60)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.ResponseTimeoutSec.HasValue);

            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.OrderNo.HasValue);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Description: Validator cho thêm mới đối tác chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataAddPartnerValidator : AbstractValidator<ShareDataAddPartnerInput>
    {
        public ShareDataAddPartnerValidator(IStringLocalizer localizer)
        {
            Include(new ShareDataPartnerValidator(localizer));
        }
    }

    /// <summary>
    /// Description: Validator cho cập nhật đối tác chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataUpdatePartnerValidator : AbstractValidator<ShareDataUpdatePartnerInput>
    {
        public ShareDataUpdatePartnerValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new ShareDataPartnerValidator(localizer, isCreate: false));
        }
    }

    /// <summary>
    /// Description: Validator cho xóa đối tác chia sẻ
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataDeletePartnerValidator : AbstractValidator<ShareDataDeletePartnerInput>
    {
        public ShareDataDeletePartnerValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Description: Validator cho mở phiên kết nối
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataConnectPartnerValidator : AbstractValidator<ShareDataConnectPartnerInput>
    {
        public ShareDataConnectPartnerValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Description: Validator cho ngắt phiên kết nối
    /// Created date: 2026-08-04
    /// </summary>
    public class ShareDataDisconnectPartnerValidator : AbstractValidator<ShareDataDisconnectPartnerInput>
    {
        public ShareDataDisconnectPartnerValidator(IStringLocalizer localizer)
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
