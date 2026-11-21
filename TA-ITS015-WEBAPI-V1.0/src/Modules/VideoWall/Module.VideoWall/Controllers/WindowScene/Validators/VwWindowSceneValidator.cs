using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.VideoWall.Core.Dto.WindowScene;
using Module.VideoWall.Core.Exceptions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;

namespace Module.VideoWall.Controllers.WindowScene.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Rule dùng chung cho Add và Update VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwWindowSceneValidator : AbstractValidator<VwAddWindowSceneInput>
    {
        public VwWindowSceneValidator(IStringLocalizer localizer, bool isCreate = true)
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

            RuleFor(x => x.Label)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Opacity)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // Khối và vị trí trong khối: nằm trọn trong khối hay không kiểm ở handler (cần bố cục tường)
            RuleFor(x => x.BlockNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.BlockNo.HasValue);

            RuleFor(x => x.X)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.X.HasValue);

            RuleFor(x => x.Y)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.Y.HasValue);

            RuleFor(x => x.W)
                .GreaterThan(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Width])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeGreaterThanZero])
                .When(x => x.W.HasValue);

            RuleFor(x => x.H)
                .GreaterThan(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Height])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeGreaterThanZero])
                .When(x => x.H.HasValue);

            RuleFor(x => x.ZIndex)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.ZIndex.HasValue);

            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.OrderNo.HasValue);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho thêm mới VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddWindowSceneValidator : AbstractValidator<VwAddWindowSceneInput>
    {
        public VwAddWindowSceneValidator(IStringLocalizer localizer)
        {
            Include(new VwWindowSceneValidator(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho cập nhật VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateWindowSceneValidator : AbstractValidator<VwUpdateWindowSceneInput>
    {
        public VwUpdateWindowSceneValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new VwWindowSceneValidator(localizer, isCreate: false));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho xóa VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteWindowSceneValidator : AbstractValidator<VwDeleteWindowSceneInput>
    {
        public VwDeleteWindowSceneValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Description: Validator cho thao tác đổi nguồn tín hiệu trực tiếp của cửa sổ VwWindowScene
    /// Created date: 17/08/2026
    /// </summary>
    public class VwSwitchWindowSourceValidator : AbstractValidator<VwSwitchWindowSourceInput>
    {
        public VwSwitchWindowSourceValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            RuleFor(x => x.SourceId)
                .NotEmpty()
                    .WithName(localizer[BaseMsg.Vw.Entity.SourceId])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Description: Validator cho thao tác điều chỉnh thứ tự lớp Z-Order (Top/Bottom) của cửa sổ VwWindowScene
    /// Created date: 17/08/2026
    /// </summary>
    public class VwSetWindowLayerValidator : AbstractValidator<VwSetWindowLayerInput>
    {
        public VwSetWindowLayerValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            RuleFor(x => x.Action)
                .IsInEnum()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }
}
