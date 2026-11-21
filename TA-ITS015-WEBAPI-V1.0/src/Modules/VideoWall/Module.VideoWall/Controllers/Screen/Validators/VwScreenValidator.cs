using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Screen;
using Module.VideoWall.Core.Exceptions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;

namespace Module.VideoWall.Controllers.Screen.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Rule dùng chung cho Add và Update VwScreen
    /// Created date: 2026-08-01
    /// </summary>
    public class VwScreenValidator : AbstractValidator<VwAddScreenInput>
    {
        public VwScreenValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => (x.Code != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => (x.Name != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Resolution)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Resolution])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.CellRatio)
                .Must(ratio => VwWallProfile.CellRatios.Contains(ratio!.Trim()))
                    .WithName(x => localizer[BaseMsg.Vw.Entity.CellRatio])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => !string.IsNullOrWhiteSpace(x.CellRatio));

            RuleFor(x => x.PanelType)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.PanelModel)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.PanelSize)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Status)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Status])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => (x.Status != null && !isCreate) || isCreate)
                .IsInEnum()
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            // Cổng đang cắm (ControllerOutputId) do đồng bộ thiết bị gán — không nhận từ form.
            RuleFor(x => x.SerialNumber)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseMsg.Vw.Entity.SerialNumber])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.OrderNo.HasValue);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho thêm mới VwScreen
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddScreenValidator : AbstractValidator<VwAddScreenInput>
    {
        public VwAddScreenValidator(IStringLocalizer localizer)
        {
            Include(new VwScreenValidator(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho cập nhật VwScreen
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateScreenValidator : AbstractValidator<VwUpdateScreenInput>
    {
        public VwUpdateScreenValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new VwScreenValidator(localizer, isCreate: false));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho xóa VwScreen
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteScreenValidator : AbstractValidator<VwDeleteScreenInput>
    {
        public VwDeleteScreenValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }
}
