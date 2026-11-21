using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.ShareData.Core.Dto.CodeSet;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Msg = Module.ShareData.Core.Exceptions.BaseMsg.ShareData;

namespace Module.ShareData.Controllers.CodeSet.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Rule dùng chung cho Add và Update bộ mã.
    /// Created date: 2026-08-21
    /// </summary>
    public class CodeSetValidator : AbstractValidator<ShareDataAddCodeSetInput>
    {
        public CodeSetValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.CodeSetCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                    .WithName(localizer[Msg.Entity.CodeSetCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);
            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // Giá trị mặc định (dùng khi không khớp dòng nào trong Values) — cả 2 đều không bắt buộc.
            RuleFor(x => x.DefaultSourceValue)
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[Msg.Entity.CodeSetDefaultSourceValue])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.DefaultPartnerValue)
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[Msg.Entity.CodeSetDefaultPartnerValue])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Values)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.CodeSetValues])
                    .WithMessage(localizer[Msg.Validation.CodeSetValuesRequired]);

            RuleForEach(x => x.Values)
                .ChildRules(value =>
                {
                    // Giá trị chuẩn là đầu ra bắt buộc; SourceValue được phép rỗng ở dòng mặc định.
                    value.RuleFor(v => v.PartnerValue)
                        .NotEmpty()
                            .WithName(localizer[Msg.Entity.CodeSetValues])
                            .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

                    value.RuleFor(v => v.SourceValue)
                        .MaximumLength(EntityConst.Length128)
                            .WithName(localizer[Msg.Entity.CodeSetValues])
                            .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

                    value.RuleFor(v => v.PartnerValue)
                        .MaximumLength(EntityConst.Length128)
                            .WithName(localizer[Msg.Entity.CodeSetValues])
                            .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);
                })
                .When(x => x.Values != null && x.Values.Count > 0);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho thêm mới bộ mã
    /// Created date: 2026-08-21
    /// </summary>
    public class AddCodeSetValidator : AbstractValidator<ShareDataAddCodeSetInput>
    {
        public AddCodeSetValidator(IStringLocalizer localizer)
        {
            Include(new CodeSetValidator(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho cập nhật bộ mã
    /// Created date: 2026-08-21
    /// </summary>
    public class UpdateCodeSetValidator : AbstractValidator<ShareDataUpdateCodeSetInput>
    {
        public UpdateCodeSetValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new CodeSetValidator(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho xóa bộ mã
    /// Created date: 2026-08-21
    /// </summary>
    public class DeleteCodeSetValidator : AbstractValidator<ShareDataDeleteCodeSetInput>
    {
        public DeleteCodeSetValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }
}
