using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Modules.WP.Controllers.Config.Dto;
using Modules.WP.Core.Exceptions;

namespace Modules.WP.Controllers.Statistical.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Validate dữ liệu Update cho cấu hình hệ thống WP (
    /// Config) - hiện chưa sử dụng
    /// Created date: 03/07/2026
    /// </summary>
    //public class UpdateConfigValidator:AbstractValidator<WPConfigUpdateInput>
    //{
    //    public UpdateConfigValidator(IStringLocalizer localizer)
    //    {
    //        RuleFor(x => x.Name).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Name])
    //            .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
    //            .MaximumLength(512).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
    //    }
    //}
}
