using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.EquipmentType.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.EquipmentType.Validators
{
    public class EquipmentTypeValidator : AbstractValidator<TmsEquipmentType>
    {
        public EquipmentTypeValidator(IStringLocalizer localizer, bool isCreate = true)
        {


            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseMsg.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Size)
                .MaximumLength(EntityConst.ConfigFieldLength)
                .WithName(x => localizer[BaseMsg.BaseEntity.Size])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.ModuleId)
                .MaximumLength(EntityConst.KeyFieldLength)
                .WithName(x => localizer[BaseMsg.Tms.Entity.Module])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);


            //When
            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Name != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length64)
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.Code])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Code != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length64)
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Status)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.Status])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Status != null && !isCreate) || isCreate)
                .IsInEnum()
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);
        }

    }
    public class AddEquipmentTypeValidator : AbstractValidator<AddEquipmentTypesInput>
    {
        public AddEquipmentTypeValidator(IStringLocalizer localizer)
        {
            Include(new EquipmentTypeValidator(localizer));
        }

    }

    public class UpdateEquipmentTypeValidator : AbstractValidator<UpdateEquipmentTypesInput>
    {
        public UpdateEquipmentTypeValidator(IStringLocalizer localizer)
        {
            Include(new EquipmentTypeValidator(localizer, false));
        }
    }

    public class DeleteEquipmentTypeValidator : AbstractValidator<DeleteEquipmentTypesInput>
    {
        public DeleteEquipmentTypeValidator(IStringLocalizer localizer)
        {
            RuleFor(data => data.ID)
                .NotEmpty()
                .WithName(localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
