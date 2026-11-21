using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.Report.Core.Dto.InfoConfig;
using Modules.Report.Core.Entities;
using Modules.Report.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.Report.Controllers.InfoConfig.Validators
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Kiểm tra dữ liệu insert-update RptInfoConfig
    /// Created date: 27/07/2026
    /// </summary>
    public class RptInfoConfigValidator : AbstractValidator<RptInfoConfig>
    {
        public RptInfoConfigValidator(IStringLocalizer lz, bool isCreate = true)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => !isCreate)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.GroupCode)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.RptInfoConfig.Entity.GroupCode])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || x.GroupCode != null)
                .MaximumLength(EntityConst.Length64)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.Code])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || x.Code != null)
                .MaximumLength(EntityConst.Length128)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.Name])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || x.Name != null)
                .MaximumLength(EntityConst.Length128)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.ReportCode)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => lz[BaseMsg.RptInfoConfig.Entity.ReportCode])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Value)
                .MaximumLength(EntityConst.Length2048)
                    .WithName(x => lz[BaseMsg.RptInfoConfig.Entity.Value])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.ValueType)
                .MaximumLength(EntityConst.Length32)
                    .WithName(x => lz[BaseMsg.RptInfoConfig.Entity.ValueType])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length1024)
                    .WithName(x => lz[BaseMsg.BaseEntity.Remark])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => lz[BaseMsg.BaseEntity.OrderNo])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.GreaterThanOrEqual])
                    .When(x => isCreate || x.OrderNo != null);

            RuleFor(x => x.Status)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.Status])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                .IsInEnum()
                    .WithMessage(lz[BaseMsg.BaseValidation.MustBeValidValue])
                .When(x => isCreate || x.Status != null);
        }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Kiểm tra dữ liệu insert
    /// Created date: 27/07/2026
    /// </summary>
    public class AddRptInfoConfigValidator : AbstractValidator<AddRptInfoConfigInput>
    {
        public AddRptInfoConfigValidator(IStringLocalizer localizer)
        {
            Include(new RptInfoConfigValidator(localizer));
        }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Kiểm tra dữ liệu update
    /// Created date: 27/07/2026
    /// </summary>
    public class UpdateRptInfoConfigValidator : AbstractValidator<UpdateRptInfoConfigInput>
    {
        public UpdateRptInfoConfigValidator(IStringLocalizer localizer)
        {
            Include(new RptInfoConfigValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Kiểm tra dữ liệu delete
    /// Created date: 27/07/2026
    /// </summary>
    public class DeleteRptInfoConfigValidator : AbstractValidator<DeleteRptInfoConfigInput>
    {
        public DeleteRptInfoConfigValidator(IStringLocalizer lz)
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
