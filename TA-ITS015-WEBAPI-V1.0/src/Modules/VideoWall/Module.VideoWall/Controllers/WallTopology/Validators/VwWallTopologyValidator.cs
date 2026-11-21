using FluentValidation;
using Microsoft.Extensions.Localization;

using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.WallTopology;
using Module.VideoWall.Core.Exceptions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;

namespace Module.VideoWall.Controllers.WallTopology.Validators
{
    /// <summary>
    /// Description: Validator cho lưu bố cục tường (VwWallTopology + VwWallBlock + màn hình trên lưới)
    /// Created date: 09/09/2026 — cập nhật 22/10/2026 (bố cục nhiều khối)
    /// </summary>
    public class VwSaveWallLayoutValidator : AbstractValidator<VwSaveWallLayoutInput>
    {
        public VwSaveWallLayoutValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Name)
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // Lưu từ trang Màn hình không gửi tỉ lệ ô ⇒ giữ tỉ lệ đang lưu; có gửi thì phải hợp lệ.
            RuleFor(x => x.CellRatio)
                .Must(ratio => VwWallProfile.CellRatios.Contains(ratio!.Trim()))
                    .WithName(localizer[BaseMsg.Vw.Entity.CellRatio])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue])
                .When(x => !string.IsNullOrWhiteSpace(x.CellRatio));

            RuleFor(x => x.DefaultPanelModel)
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Info])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.DefaultPanelType)
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Type])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.DefaultResolution)
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Resolution])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.DefaultPanelSize)
                .GreaterThan(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Size])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeGreaterThanZero])
                .When(x => x.DefaultPanelSize.HasValue);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // 1..MaxBlocks khối, BlockNo liên tục 0..n-1: màn hình / cửa sổ / phân quyền tham chiếu khối qua số này.
            RuleFor(x => x.Blocks)
                .Must(blocks => blocks != null
                    && blocks.Count is > 0 and <= VwWallProfile.MaxBlocks
                    && blocks.Select(u => u.BlockNo ?? -1).OrderBy(n => n).SequenceEqual(Enumerable.Range(0, blocks.Count)))
                    .WithMessage(localizer[BaseMsg.Vw.Validation.WallBlocksInvalid]);

            RuleForEach(x => x.Blocks).ChildRules(block =>
            {
                block.RuleFor(u => u.Cols)
                    .NotEmpty()
                        .WithName(localizer[BaseLocaleManager.BaseEntity.Width])
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                    .InclusiveBetween(1, VwWallProfile.MaxBlockCols)
                        .WithName(localizer[BaseLocaleManager.BaseEntity.Width])
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

                block.RuleFor(u => u.Rows)
                    .NotEmpty()
                        .WithName(localizer[BaseLocaleManager.BaseEntity.Height])
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                    .InclusiveBetween(1, VwWallProfile.MaxBlockRows)
                        .WithName(localizer[BaseLocaleManager.BaseEntity.Height])
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

                block.RuleFor(u => u.Name)
                    .MaximumLength(EntityConst.Length64)
                        .WithName(localizer[BaseLocaleManager.BaseEntity.Name])
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

                block.RuleFor(u => u.Remark)
                    .MaximumLength(EntityConst.Length256)
                        .WithName(localizer[BaseLocaleManager.BaseEntity.Remark])
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);
            });
        }
    }

    /// <summary>
    /// Description: Rule chung cho thêm / sửa bố cục trong danh sách bố cục: mã, tên bắt buộc; kích thước ô; khối liên tục 0..n-1.
    /// Created date: 26/10/2026
    /// </summary>
    public abstract class VwWallTopologyInfoValidator<T> : AbstractValidator<T> where T : VwAddWallTopologyInput
    {
        protected VwWallTopologyInfoValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Code])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length128)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.CellRatio)
                .NotEmpty()
                    .WithName(localizer[BaseMsg.Vw.Entity.CellRatio])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .Must(ratio => VwWallProfile.CellRatios.Contains(ratio?.Trim()))
                    .WithName(localizer[BaseMsg.Vw.Entity.CellRatio])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            // Số ô bắt buộc khi thêm / sửa; tổng ô của lưới phải đúng bằng số ô kiểm ở handler (chỉ khi đổi lưới)
            RuleFor(x => x.CellCount)
                .NotEmpty()
                    .WithName(localizer[BaseMsg.Vw.Entity.CellCount])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .InclusiveBetween(1, VwWallProfile.MaxCellCount)
                    .WithName(localizer[BaseMsg.Vw.Entity.CellCount])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Blocks)
                .Must(blocks => blocks != null
                    && blocks.Count is > 0 and <= VwWallProfile.MaxBlocks
                    && blocks.Select(u => u.BlockNo ?? -1).OrderBy(n => n).SequenceEqual(Enumerable.Range(0, blocks.Count)))
                    .WithMessage(localizer[BaseMsg.Vw.Validation.WallBlocksInvalid]);

            RuleForEach(x => x.Blocks).ChildRules(block =>
            {
                block.RuleFor(u => u.Cols)
                    .NotEmpty()
                        .WithName(localizer[BaseLocaleManager.BaseEntity.Width])
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                    .InclusiveBetween(1, VwWallProfile.MaxBlockCols)
                        .WithName(localizer[BaseLocaleManager.BaseEntity.Width])
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);

                block.RuleFor(u => u.Rows)
                    .NotEmpty()
                        .WithName(localizer[BaseLocaleManager.BaseEntity.Height])
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                    .InclusiveBetween(1, VwWallProfile.MaxBlockRows)
                        .WithName(localizer[BaseLocaleManager.BaseEntity.Height])
                        .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);
            });
        }
    }

    /// <summary>
    /// Description: Validator thêm bố cục tường
    /// Created date: 26/10/2026
    /// </summary>
    public class VwAddWallTopologyValidator : VwWallTopologyInfoValidator<VwAddWallTopologyInput>
    {
        public VwAddWallTopologyValidator(IStringLocalizer localizer) : base(localizer) { }
    }

    /// <summary>
    /// Description: Validator sửa bố cục tường
    /// Created date: 26/10/2026
    /// </summary>
    public class VwUpdateWallTopologyValidator : VwWallTopologyInfoValidator<VwUpdateWallTopologyInput>
    {
        public VwUpdateWallTopologyValidator(IStringLocalizer localizer) : base(localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Description: Validator xoá bố cục tường
    /// Created date: 26/10/2026
    /// </summary>
    public class VwDeleteWallTopologyValidator : AbstractValidator<VwDeleteWallTopologyInput>
    {
        public VwDeleteWallTopologyValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Description: Validator chọn bố cục đang dùng
    /// Created date: 22/10/2026
    /// </summary>
    public class VwSetCurrentWallTopologyValidator : AbstractValidator<VwSetCurrentWallTopologyInput>
    {
        public VwSetCurrentWallTopologyValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }
}
