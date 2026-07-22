using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.CfgSystem.Controllers.ConfigData.Dto;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.CfgSystem.Controllers.ConfigData.Validators
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>

    //SysConfigData {
    // ID varchar(64) pk
    // ConfigTypeId varchar(64) null > SysConfigType.ID
    // Name string (256) null
    // Value string (128) null
    // StyleSetting string (512) null
    // ClassSetting string (512) null
    // OrderNo integer null
    // Remark string (256) null
    // Status integer null
    // TagType string (16) null
    //}
    public class ConfigDataValidator: AbstractValidator<SysConfigData>
    {
        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Kiểm tra dữ liệu insert-update SysConfigData
        /// Created date: 09/08/2024
        /// </summary>
        public ConfigDataValidator(IStringLocalizer lz, bool isCreate = true) {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => !isCreate)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.ConfigTypeId)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.SysConfigData.Entity.ConfigTypeId])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || (!isCreate && x.ConfigTypeId != null))
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
                .MaximumLength(EntityConst.Length64)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.StyleSetting)
                .MaximumLength(EntityConst.Length512)
                    .WithName(x => lz[BaseMsg.SysConfigData.Entity.StyleSetting])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.ClassSetting)
                .MaximumLength(EntityConst.Length512)
                    .WithName(x => lz[BaseMsg.SysConfigData.Entity.ClassSetting])
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
    public class AddConfigDataValidator : AbstractValidator<AddSysConfigDataInput>
    {
        public AddConfigDataValidator(IStringLocalizer localizer)
        {
            Include(new ConfigDataValidator(localizer));
        }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    public class UpdateConfigDataValidator : AbstractValidator<UpdateSysConfigDataInput>
    {
        public UpdateConfigDataValidator(IStringLocalizer localizer) {
            Include(new ConfigDataValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    public class DeleteConfigDataValidator: AbstractValidator<DeleteSysConfigDataInput> {
        public DeleteConfigDataValidator(IStringLocalizer lz)
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
