using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.CfgSystem.Controllers.Terminology.Dto;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.CfgSystem.Controllers.Terminology.Validators
{
    //SysTerminology {
    // ID varchar(64) pk
    // Code string(256) unique
    // Name string(256) null
    // Value string(256) null
    // Lang varchar(16) def(vi-VN)
    // OrderNo integer null
    // Remark string(1024) null
    // Status integer null def(1)
    //}

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: Kiểm tra dữ liệu insert-update SysTerminology
    /// Created date: 09/08/2024
    /// </summary>
    public class TerminologyValidator : AbstractValidator<SysTerminology>
    {
        public TerminologyValidator(IStringLocalizer lz, bool isCreate = true) {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => !isCreate)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.Name])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || (!isCreate && x.Name != null))
                .MaximumLength(EntityConst.Length256)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.Code])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || (!isCreate && x.Code != null))
                .MaximumLength(EntityConst.Length256)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Value)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.SysTerminolog.Entity.Value])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || (!isCreate && x.Value != null))
                .MaximumLength(EntityConst.Length256)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Lang)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.Lang])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || (!isCreate && x.Lang != null))
                .MaximumLength(EntityConst.ConfigFieldLength)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(data => data.Remark)
                .MaximumLength(EntityConst.Length1024)
                    .WithName(x => lz[BaseMsg.BaseEntity.Remark])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            // >=0
            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => lz[BaseMsg.BaseEntity.OrderNo])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.GreaterThanOrEqual])
                    .When(x => isCreate || (!isCreate && x.OrderNo != null));

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
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    public class AddTerminologyValidator : AbstractValidator<AddSysTerminologyInput>
    {
        public AddTerminologyValidator(IStringLocalizer localizer)
        {
            Include(new TerminologyValidator(localizer));
        }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    public class UpdateTerminologyValidator : AbstractValidator<UpdateSysTerminologyInput> {
        public UpdateTerminologyValidator(IStringLocalizer localizer) {
            Include(new TerminologyValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    public class DeleteTerminologyValidator: AbstractValidator<DeleteSysTerminologyInput> {
        public DeleteTerminologyValidator(IStringLocalizer lz)
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
