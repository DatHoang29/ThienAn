using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.WorkContact.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.WorkContact.Validators
{
    //TmsWorkContact {
    //	ID varchar(64) pk
    //    Code string (64) null
    //	Name string (128) null
    //	Phone string (32) null
    //	Address string (128) null
    //	Info string (128) null
    //	Type string (16) null
    //	RefId varchar(64) null
    //	Status integer null
    //	Remark string (256) null
    //}

    /// <summary>
    /// Author: ThangNN
    /// Description: Kiểm tra dữ liệu insert-update TmsWorkContact
    /// Created date: 05/11/2024
    /// </summary>
    public class WorkContactValidator : AbstractValidator<TmsWorkContact>
    {
        public WorkContactValidator(IStringLocalizer lz, bool isCreate = true)
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
                .MaximumLength(EntityConst.Length64)
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
                    .WithName(x => lz[BaseMsg.TmsWorkContact.Entity.Phone])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Address)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => lz[BaseMsg.TmsWorkContact.Entity.Address])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Info)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => lz[BaseMsg.BaseEntity.Info])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Type)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.TmsWorkContact.Entity.Type])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || (!isCreate && x.Type != null))
                .MaximumLength(EntityConst.ConfigFieldLength)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => lz[BaseMsg.BaseEntity.Remark])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.RefId)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithName(x => lz[BaseMsg.TmsWorkContact.Entity.RefId])
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
    /// Description: Kiểm tra dữ liệu insert
    /// Created date: 05/11/2024
    /// </summary>
    public class AddWorkContactValidator : AbstractValidator<AddTmsWorkContactInput>
    {
        public AddWorkContactValidator(IStringLocalizer localizer)
        {
            Include(new WorkContactValidator(localizer));
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: Kiểm tra dữ liệu update
    /// Created date: 05/11/2024
    /// </summary>
    public class UpdateWorkContactValidator : AbstractValidator<UpdateTmsWorkContactInput>
    {
        public UpdateWorkContactValidator(IStringLocalizer localizer)
        {
            Include(new WorkContactValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: Kiểm tra dữ liệu delete
    /// Created date: 05/11/2024
    /// </summary>
    public class DeleteWorkContactValidator : AbstractValidator<DeleteTmsWorkContactInput>
    {
        public DeleteWorkContactValidator(IStringLocalizer lz)
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
