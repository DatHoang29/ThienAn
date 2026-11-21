using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.WallPermission;
using Newtonsoft.Json;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Module.VideoWall.Core.Exceptions;

namespace Module.VideoWall.Controllers.WallPermission.Validators
{
    /// <summary>
    /// Description: Rule dùng chung cho Add và Update VwWallPermission
    /// Created date: 11/09/2026
    /// </summary>
    public class VwWallPermissionValidator : AbstractValidator<VwAddWallPermissionInput>
    {
        public VwWallPermissionValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length128)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleForEach(x => x.UserIds)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithName(x => localizer[BaseMsg.Vw.Entity.UserId])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleForEach(x => x.OrgIds)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseMsg.Vw.Entity.OrgId])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // Phải gán cho ít nhất 1 người dùng hoặc 1 đơn vị
            RuleFor(x => x)
                .Must(x => (x.UserIds?.Any(u => !string.IsNullOrWhiteSpace(u)) ?? false) || (x.OrgIds?.Any(u => !string.IsNullOrWhiteSpace(u)) ?? false))
                    .WithMessage(x => localizer[BaseMsg.Vw.Validation.WallPermissionActorRequired]);

            RuleFor(x => x.Status)
                .IsInEnum()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Status])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => x.Status != null);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // Toàn quyền (IsFullAccess = true) thì không bắt buộc Config; ngược lại phải có ít nhất 1 ô hợp lệ.
            RuleFor(x => x.Config)
                .NotEmpty()
                    .WithMessage(x => localizer[BaseMsg.Vw.Validation.WallPermissionNoCellSelected])
                .Must(BeValidConfig)
                    .WithMessage(x => localizer[BaseMsg.Vw.Validation.WallPermissionConfigInvalid])
                .When(x => x.IsFullAccess != true);

            RuleFor(x => x.Config)
                .Must(config => string.IsNullOrWhiteSpace(config) || BeValidConfig(config))
                    .WithMessage(x => localizer[BaseMsg.Vw.Validation.WallPermissionConfigInvalid])
                .When(x => x.IsFullAccess == true);
        }

        private static bool BeValidConfig(string? config)
        {
            if (string.IsNullOrWhiteSpace(config))
                return false;

            List<VwGridCell>? cells;
            try
            {
                cells = JsonConvert.DeserializeObject<List<VwGridCell>>(config);
            }
            catch
            {
                return false;
            }

            if (cells == null || cells.Count == 0)
                return false;

            var uniqueCount = cells.Select(c => (c.Block, c.Col, c.Row)).Distinct().Count();
            if (uniqueCount != cells.Count)
                return false;

            // Chỉ chặn giá trị vô lý ở đây; ô có nằm trong bố cục tường hiện tại hay không
            // kiểm ở VwWallPermissionCommandHandler vì cần đọc VwWallBlock.
            return cells.All(c =>
                c.Block >= 0 && c.Block < VwWallProfile.MaxBlocks &&
                c.Col >= 0 && c.Col < VwWallProfile.MaxBlockCols &&
                c.Row >= 0 && c.Row < VwWallProfile.MaxBlockRows);
        }
    }

    /// <summary>
    /// Description: Validator cho thêm mới VwWallPermission
    /// Created date: 11/09/2026
    /// </summary>
    public class VwAddWallPermissionValidator : AbstractValidator<VwAddWallPermissionInput>
    {
        public VwAddWallPermissionValidator(IStringLocalizer localizer)
        {
            Include(new VwWallPermissionValidator(localizer));
        }
    }

    /// <summary>
    /// Description: Validator cho cập nhật VwWallPermission
    /// Created date: 11/09/2026
    /// </summary>
    public class VwUpdateWallPermissionValidator : AbstractValidator<VwUpdateWallPermissionInput>
    {
        public VwUpdateWallPermissionValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new VwWallPermissionValidator(localizer, isCreate: false));
        }
    }

    /// <summary>
    /// Description: Validator cho xóa VwWallPermission
    /// Created date: 11/09/2026
    /// </summary>
    public class VwDeleteWallPermissionValidator : AbstractValidator<VwDeleteWallPermissionInput>
    {
        public VwDeleteWallPermissionValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }
}
