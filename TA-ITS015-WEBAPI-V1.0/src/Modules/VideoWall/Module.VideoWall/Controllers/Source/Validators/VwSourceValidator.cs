using Module.VideoWall.Core.Exceptions;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Source;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;

namespace Module.VideoWall.Controllers.Source.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Rule dùng chung cho Add và Update VwSource
    /// Created date: 2026-08-01
    /// </summary>
    public class VwSourceValidator : AbstractValidator<VwAddSourceInput>
    {
        public VwSourceValidator(
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

            RuleFor(x => x.SourceType)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.SignalType)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Url)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Url])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.InputPort)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.MaxResolution)
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.AspectRatio)
                .MaximumLength(EntityConst.Length128)
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

            RuleFor(x => x.SignalNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.SignalNo.HasValue);

            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.OrderNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.OrderNo.HasValue);

            // Mọi nguồn (cục bộ, luồng IP, ...) đều phải gắn 1 bộ điều khiển — không còn nguồn "dùng chung" (23/10/2026).
            RuleFor(x => x.ControllerId)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.Vw.Entity.ControllerId])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required]);

            // TẠM TẮT PHÂN QUYỀN (25/09/2026) — bật lại: bỏ comment khối dưới.
            // RuleFor(x => x.ControllerId)
            //     // Must (đồng bộ): ASP.NET automatic validation không chạy được rule bất đồng bộ.
            //     .Must(controllerId =>
            //     {
            //         if (string.IsNullOrWhiteSpace(controllerId))
            //             return true;
// 
            //         if (isSubControllerCheck != null)
            //             return !isSubControllerCheck(controllerId).GetAwaiter().GetResult();
// 
            //         var rep = controllerRep ?? App.GetService<BaseRepository<VwController>>();
            //         if (rep == null)
            //             return true;
// 
            //         var ctrl = rep.GetFirst(u => u.IsDelete == null && u.ID == controllerId);
            //         return !VwControllerTopology.IsSub(ctrl);
            //     })
            //     .WithMessage("Nguồn cục bộ chỉ gắn được vào bộ điều khiển trung tâm (không gắn vào bộ con).")
            //     .When(x => !string.IsNullOrWhiteSpace(x.ControllerId) && VwSourceProfile.IsLocal(x.SourceType));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho thêm mới VwSource
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddSourceValidator : AbstractValidator<VwAddSourceInput>
    {
        public VwAddSourceValidator(
            IStringLocalizer localizer,
            BaseRepository<VwController>? controllerRep = null,
            Func<string?, Task<bool>>? isSubControllerCheck = null)
        {
            Include(new VwSourceValidator(localizer, controllerRep: controllerRep, isSubControllerCheck: isSubControllerCheck));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho cập nhật VwSource
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateSourceValidator : AbstractValidator<VwUpdateSourceInput>
    {
        public VwUpdateSourceValidator(
            IStringLocalizer localizer,
            BaseRepository<VwController>? controllerRep = null,
            Func<string?, Task<bool>>? isSubControllerCheck = null)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new VwSourceValidator(localizer, isCreate: false, controllerRep: controllerRep, isSubControllerCheck: isSubControllerCheck));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho xóa VwSource
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteSourceValidator : AbstractValidator<VwDeleteSourceInput>
    {
        public VwDeleteSourceValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }
}
