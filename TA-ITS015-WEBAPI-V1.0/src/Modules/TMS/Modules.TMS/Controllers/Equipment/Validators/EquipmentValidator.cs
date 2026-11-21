using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.Equipment.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.Equipment.Validators
{
    public class EquipmentValidator : AbstractValidator<TmsEquipment>
    {
        public EquipmentValidator(IStringLocalizer localizer, bool isCreate = true)
        {

            RuleFor(x => x.Port)
                .MaximumLength(EntityConst.Length8)
                .WithName(x => localizer[BaseMsg.BaseEntity.Port])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Ip)
                .MaximumLength(EntityConst.Length32)
                .WithName(x => localizer[BaseMsg.BaseEntity.IpAddress])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Url)
                .MaximumLength(EntityConst.Length128)
                .WithName(x => localizer[BaseMsg.BaseEntity.IpAddress])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Username)
                .MaximumLength(EntityConst.Length64)
                .WithName(x => localizer[BaseMsg.BaseEntity.Account])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Password)
                .MaximumLength(EntityConst.Length64)
                .WithName(x => localizer[BaseMsg.BaseEntity.Password])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.LaneId)
                .MaximumLength(EntityConst.ConfigFieldLength)
                .WithName(x => localizer[BaseMsg.Tms.Entity.Lane])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.EquipmentTypeId)
                .MaximumLength(EntityConst.KeyFieldLength)
                .WithName(x => localizer[BaseMsg.Tms.Entity.EquipmentType])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);




            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseMsg.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            // >=0
            RuleFor(x => x.KmNumber)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.TmsZone.Entity.FromKmNumber])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);


            //0-999
            RuleFor(x => x.MetNumber)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.TmsZone.Entity.FromMetNumber])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual])
                .LessThan(1000)
                    .WithName(x => localizer[BaseMsg.TmsZone.Entity.FromMetNumber])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.LessThan]);

            // >=0
            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.BaseEntity.OrderNo])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);
            // >=0
            RuleFor(x => x.EquipmentState)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.Tms.Entity.EquipmentState])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

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
    public class AddEquipmentValidator : AbstractValidator<AddEquipmentInput>
    {
        public AddEquipmentValidator(IStringLocalizer localizer)
        {
            Include(new EquipmentValidator(localizer));

        }

    }

    public class UpdateEquipmentValidator : AbstractValidator<UpdateEquipmentInput>
    {
        public UpdateEquipmentValidator(IStringLocalizer localizer)
        {
            Include(new EquipmentValidator(localizer, false));


        }
    }

    public class DeleteEquipmentValidator : AbstractValidator<DeleteEquipmentInput>
    {
        public DeleteEquipmentValidator(IStringLocalizer localizer)
        {
            RuleFor(data => data.ID)
                .NotEmpty()
                .WithName(localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
