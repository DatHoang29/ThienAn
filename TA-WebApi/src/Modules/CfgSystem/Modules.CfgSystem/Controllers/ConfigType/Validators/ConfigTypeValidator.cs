using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.CfgSystem.Controllers.ConfigType.Dto;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.CfgSystem.Controllers.ConfigType.Validators
{
    //SysConfigType {
    // ID varchar(64) pk
    // Code string (128) null
    // Name string (64) null
    // OrderNo integer null
    // Remark string (256) null
    // Status integer null
    //}

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: Kiểm tra dữ liệu insert-update SysConfigType
    /// Created date: 09/08/2024
    /// </summary>
    /// 
    public class ConfigTypeValidator : AbstractValidator<SysConfigType>
    {
        public ConfigTypeValidator(IStringLocalizer lz, bool isCreate = true) {
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

            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.Code])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || (!isCreate && x.Code != null))
                .MaximumLength(EntityConst.Length64)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
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
    public class AddConfigTypeValidator : AbstractValidator<AddSysConfigTypeInput>
    {
        public AddConfigTypeValidator(IStringLocalizer localizer)
        {
            Include(new ConfigTypeValidator(localizer));
        }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    public class UpdateConfigTypeValidator : AbstractValidator<UpdateSysConfigTypeInput> {
        public UpdateConfigTypeValidator(IStringLocalizer localizer) {
            Include(new ConfigTypeValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    public class DeleteConfigTypeValidator: AbstractValidator<DeleteSysConfigTypeInput> {
        public DeleteConfigTypeValidator(IStringLocalizer lz)
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
