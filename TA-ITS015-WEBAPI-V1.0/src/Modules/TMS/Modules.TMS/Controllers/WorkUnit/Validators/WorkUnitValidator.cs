using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.WorkUnit.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.WorkUnit.Validators
{
    //TmsWorkUnit {
    //	ID varchar(64) pk
    //    Code string (64) null
    //	Name string (128) null
    //	Phone string (32) null
    //	Address string (128) null
    //	Info string (128) null
    //	Type string (16) null
    //	Status integer null
    //	Remark string (256) null
    //	Location string (64) null
    //}

    /// <summary>
    /// Author: ThangNN
    /// Description: Kiểm tra dữ liệu insert-update TmsWorkUnit
    /// Created date: 25/11/2024
    /// </summary>
    public class WorkUnitValidator : AbstractValidator<TmsWorkUnit>
    {
        public WorkUnitValidator(IStringLocalizer lz, bool isCreate = true)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => !isCreate)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.Code])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || (!isCreate && x.Code != null))
                .MaximumLength(EntityConst.Length128)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.Name])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || (!isCreate && x.Name != null))
                .MaximumLength(EntityConst.Length128)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Phone)
                .MaximumLength(EntityConst.Length32)
                .WithName(x => lz[BaseMsg.TmsWorkUnit.Entity.Phone])
                .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Address)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => lz[BaseMsg.TmsWorkUnit.Entity.Address])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Info)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => lz[BaseMsg.BaseEntity.Info])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Type)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.TmsWorkUnit.Entity.Type])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || (!isCreate && x.Type != null))
                .MaximumLength(EntityConst.ConfigFieldLength)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                .WithName(x => lz[BaseMsg.BaseEntity.Remark])
                .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Status)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.Status])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                .IsInEnum()
                    .WithMessage(lz[BaseMsg.BaseValidation.MustBeValidValue])
                .When(x => isCreate || (!isCreate && x.Status != null));
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Created date: 25/11/2024
    /// Description: Kiểm tra dữ liệu insert
    /// </summary>
    public class AddWorkUnitValidator : AbstractValidator<AddTmsWorkUnitInput>
    {
        public AddWorkUnitValidator(IStringLocalizer localizer)
        {
            Include(new WorkUnitValidator(localizer));
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Created date: 25/11/2024
    /// Description: Kiểm tra dữ liệu update
    /// </summary>
    public class UpdateWorkUnitValidator : AbstractValidator<UpdateTmsWorkUnitInput>
    {

        public UpdateWorkUnitValidator(IStringLocalizer localizer)
        {
            Include(new WorkUnitValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Created date: 25/11/2024
    /// Description: Kiểm tra dữ liệu delete
    /// </summary>
    public class DeleteWorkUnitValidator : AbstractValidator<DeleteTmsWorkUnitInput>
    {
        public DeleteWorkUnitValidator(IStringLocalizer lz)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);
        }
    }
}
