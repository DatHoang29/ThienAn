using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.ShareData.Core.Dto.Mapping;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Msg = Module.ShareData.Core.Exceptions.BaseMsg.ShareData;

namespace Module.ShareData.Controllers.Mapping.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Rule dùng chung cho Add và Update phễu lọc.
    /// Created date: 2026-08-21
    /// </summary>
    public class MappingValidator : AbstractValidator<ShareDataAddMappingInput>
    {
        public MappingValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.PartnerId)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.PartnerId])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => isCreate);

            RuleFor(x => x.DatatypeId)
                .NotEmpty()
                    .WithName(localizer[Msg.Entity.DatatypeId])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            RuleFor(x => x.Direction)
                .IsInEnum()
                    .WithName(localizer[Msg.Entity.Direction])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.Direction.HasValue);

            RuleFor(x => x.Code)
                .MaximumLength(EntityConst.Length64)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.PacketVersion)
                .MaximumLength(EntityConst.Length16)
                    .WithName(localizer[Msg.Entity.PacketCode])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho thêm mới phễu lọc
    /// Created date: 2026-08-21
    /// </summary>
    public class AddMappingValidator : AbstractValidator<ShareDataAddMappingInput>
    {
        public AddMappingValidator(IStringLocalizer localizer)
        {
            Include(new MappingValidator(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho cập nhật phễu lọc
    /// Created date: 2026-08-21
    /// </summary>
    public class UpdateMappingValidator : AbstractValidator<ShareDataUpdateMappingInput>
    {
        public UpdateMappingValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new MappingValidator(localizer, isCreate: false));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho xóa phễu lọc
    /// Created date: 2026-08-21
    /// </summary>
    public class DeleteMappingValidator : AbstractValidator<ShareDataDeleteMappingInput>
    {
        public DeleteMappingValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho bật / tắt phễu lọc
    /// Created date: 2026-08-21
    /// </summary>
    public class SetActiveMappingValidator : AbstractValidator<ShareDataSetActiveMappingInput>
    {
        public SetActiveMappingValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }
}
