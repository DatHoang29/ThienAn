using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Modules.WP.Controllers.Config.Dto;
using Modules.WP.Core.Exceptions;

namespace Modules.WP.Controllers.Config.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Validate dữ liệu Update cho cấu hình hệ thống WP(Config) - hiện chưa sử dụng
    /// Created date: 03/07/2026
    /// </summary>
    public class UpdateConfigValidator : AbstractValidator<WpConfigUpdateInput>
    {
        public UpdateConfigValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.sysWebTitle).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.SysWebTitle])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.sysWebTelephone).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.SysWebTelephone])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(128).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.sysWebAddress).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.SysWebAddress])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.sysWebDescription).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.SysWebDescription])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.sysWebRepresent).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.SysWebRepresent])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(128).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.sysWebCopyright).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.SysWebCopyright])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.sysWebEmail).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.SysWebEmail])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(128).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.sysWebImgTitle1).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.SysWebImgTitle1])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.sysWebImgTitle2).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.SysWebImgTitle2])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.sysWebImgTitle3).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.SysWebImgTitle3])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.sysWebOffice).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.SysWebOffice])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.sysOfficeTelephone).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.SysOfficeTelephone])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(128).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.sysBusinessLicense).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.SysBusinessLicense])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.sysOfficeAddress).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.SysOfficeAddress])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }
    }
}
