using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.VideoWall.Core.Dto.Scene;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Module.VideoWall.Core.Exceptions;

namespace Module.VideoWall.Controllers.Scene.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Rule dùng chung cho Add và Update VwScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwSceneValidator : AbstractValidator<VwAddSceneInput>
    {
        public VwSceneValidator(
            IStringLocalizer localizer,
            bool isCreate = true,
            BaseRepository<VwController>? controllerRep = null,
            Func<string?, Task<bool>>? isSubControllerCheck = null)
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

            RuleFor(x => x.Thumbnail)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Image])
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

            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.OrderNo.HasValue);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho thêm mới VwScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddSceneValidator : AbstractValidator<VwAddSceneInput>
    {
        public VwAddSceneValidator(
            IStringLocalizer localizer,
            BaseRepository<VwController>? controllerRep = null,
            Func<string?, Task<bool>>? isSubControllerCheck = null)
        {
            Include(new VwSceneValidator(localizer, controllerRep: controllerRep, isSubControllerCheck: isSubControllerCheck));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho cập nhật VwScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateSceneValidator : AbstractValidator<VwUpdateSceneInput>
    {
        public VwUpdateSceneValidator(
            IStringLocalizer localizer,
            BaseRepository<VwController>? controllerRep = null,
            Func<string?, Task<bool>>? isSubControllerCheck = null)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new VwSceneValidator(localizer, isCreate: false, controllerRep: controllerRep, isSubControllerCheck: isSubControllerCheck));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho xóa VwScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteSceneValidator : AbstractValidator<VwDeleteSceneInput>
    {
        public VwDeleteSceneValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Description: Validator cho đặt kịch bản mặc định VwScene
    /// Created date: 28/09/2026
    /// </summary>
    public class VwSetDefaultSceneValidator : AbstractValidator<VwSetDefaultSceneInput>
    {
        public VwSetDefaultSceneValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Description: Validator cho kích hoạt kịch bản VwActiveSceneInput
    /// Created date: 13/09/2026
    /// </summary>
    public class VwActiveSceneValidator : AbstractValidator<VwActiveSceneInput>
    {
        public VwActiveSceneValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.Code) || !string.IsNullOrWhiteSpace(x.EventTypeId))
                .WithMessage(x => localizer[BaseMsg.Vw.Validation.SceneCodeOrEventTypeIdRequired]);

            RuleFor(x => x.Code)
                .MaximumLength(EntityConst.Length64)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength])
                .When(x => !string.IsNullOrWhiteSpace(x.Code));

            RuleFor(x => x.EventTypeId)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseMsg.Vw.Entity.EventTypeId])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength])
                .When(x => !string.IsNullOrWhiteSpace(x.EventTypeId));
        }
    }
}
