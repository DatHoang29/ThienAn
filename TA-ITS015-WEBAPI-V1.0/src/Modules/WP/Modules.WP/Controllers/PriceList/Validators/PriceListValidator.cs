using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Modules.WP.Controllers.PriceList.Dto;
using Modules.WP.Core.Exceptions;

namespace Modules.WP.Controllers.PriceList.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Validate dữ liệu Add/Update/Delete cho bảng giá hệ thống WP (PriceList)
    /// Created date: 03/07/2026
    /// Update: 07/10/2026 - Theo cột TollPriceList: SourceId (mã, tối đa 64), StartDate, EndDate.
    /// </summary>
    public class AddPriceListValidator:AbstractValidator<WpAddPriceListInput>
    {
        public AddPriceListValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Name).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Name]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.SourceId).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.StartDate).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.FromDate]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
            RuleFor(x => x.EndDate).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.ToDate]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);

                }

    }
    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Update Price List
    /// Created date: 14/08/2026
    /// </summary>

    public class UpdatePriceListValidator:AbstractValidator<WpUpdatePriceListInput>
    {
        public UpdatePriceListValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Name).NotEmpty().WithName(x => localizer[BaseMsg.WP.Entity.Name]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(256).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.SourceId).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.Code]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(64).WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
            RuleFor(x => x.StartDate).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.FromDate]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
            RuleFor(x => x.EndDate).NotEmpty().WithName(x => localizer[BaseMsg.BaseEntity.ToDate]).WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);


        }
    } 
    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra dữ liệu đầu vào của Delete Price List
    /// Created date: 14/08/2026
    /// </summary>

    public class DeletePriceListValidator:AbstractValidator<WpDeletePriceListInput>
    {
          public DeletePriceListValidator(IStringLocalizer localizer)
            {
                RuleFor(x => x.ID).NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
            }
    }
}
