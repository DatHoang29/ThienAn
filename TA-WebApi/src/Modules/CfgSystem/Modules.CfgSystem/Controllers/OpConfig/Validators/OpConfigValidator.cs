using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.CfgSystem.Controllers.OpConfig.Dto;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.CfgSystem.Controllers.OpConfig.Validators
{
    //SysOpConfig {
    // ID varchar(64) pk
    // GroupCode string(64) null
    // Name string(64) null
    // Value string(128) null
    // IsSysConfig binary null def(0)
    // OrderNo integer null
    // Status integer null
    // Remark string(1024) null
    // Code string(128) null
    //} 

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: Kiểm tra dữ liệu insert-update SysOpConfig
    /// Created date: 09/08/2024
    /// </summary>
    public class OpConfigValidator : AbstractValidator<SysOpConfig>
    {
        public OpConfigValidator(IStringLocalizer lz, bool isCreate = true) {
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
                .MaximumLength(EntityConst.Length64)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.GroupCode)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.SysOpConfig.Entity.GroupCode])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || (!isCreate && x.GroupCode != null))
                .MaximumLength(EntityConst.ConfigFieldLength)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.Code])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || (!isCreate && x.Code != null))
                .MaximumLength(EntityConst.Length128)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Value)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => lz[BaseMsg.SysOpConfig.Entity.Value])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
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
    public class AddOpConfigValidator : AbstractValidator<AddSysOpConfigInput>
    {
        public AddOpConfigValidator(IStringLocalizer localizer)
        {
            Include(new OpConfigValidator(localizer));
        }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    public class UpdateOpConfigValidator : AbstractValidator<UpdateSysOpConfigInput> {
        public UpdateOpConfigValidator(IStringLocalizer localizer) {
            Include(new OpConfigValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: Kiểm tra dữ liệu xóa
    /// Created date: 09/08/2024
    /// </summary>
    public class DeleteOpConfigValidator: AbstractValidator<DeleteSysOpConfigInput> {
        public DeleteOpConfigValidator(IStringLocalizer lz)
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
