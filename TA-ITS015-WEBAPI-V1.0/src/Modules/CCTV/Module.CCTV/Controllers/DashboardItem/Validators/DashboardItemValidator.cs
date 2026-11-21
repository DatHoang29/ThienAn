using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.CCTV.Controllers.DashboardItem.Dto;
using Modules.CCTV.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.CCTV.Controllers.DashboardItem.Validators
{
    /// <summary>
    /// Validate từng item trong payload.
    /// Cần EquipmentId hoặc Ip (handler tự quy đổi Ip → EquipmentId); W và H phải > 0 vì kích thước 0 không có ý nghĩa trên grid.
    /// </summary>
    public class DashboardItemPayloadValidator : AbstractValidator<DashboardItemPayload>
    {
        public DashboardItemPayloadValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.EquipmentId)
                .NotEmpty()
                    .When(x => string.IsNullOrWhiteSpace(x.Ip))
                    .WithName(localizer[BaseMsg.BaseEntity.EquipmentId])
                    .WithMessage(localizer[BaseMsg.BaseValidation.Required]);

            // W và H là kích thước ô trên grid — phải > 0
            RuleFor(x => x.W)
                .GreaterThan(0)
                    .When(x => x.W.HasValue)
                    .WithName(localizer[BaseMsg.BaseEntity.Width])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeGreaterThanZero]);

            RuleFor(x => x.H)
                .GreaterThan(0)
                    .When(x => x.H.HasValue)
                    .WithName(localizer[BaseMsg.BaseEntity.Height])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeGreaterThanZero]);
        }
    }

    /// <summary>Rule dùng chung cho Add và Update DashboardItem.</summary>
    public class DashboardItemValidator : AbstractValidator<AddDashboardItemInput>
    {
        public DashboardItemValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.DashboardId)
                .NotEmpty()
                    .WithName(localizer[BaseMsg.BaseEntity.DashboardId])
                    .WithMessage(localizer[BaseMsg.BaseValidation.Required]);

            // Cho phép Items rỗng: FE lưu layout không còn camera nào (Update xóa hết item, Add bỏ qua).

            // Validate từng phần tử trong Items
            RuleForEach(x => x.Items)
                .SetValidator(new DashboardItemPayloadValidator(localizer));
        }
    }

    public class AddDashboardItemValidator : AbstractValidator<AddDashboardItemInput>
    {
        public AddDashboardItemValidator(IStringLocalizer localizer)
        {
            Include(new DashboardItemValidator(localizer));
        }
    }

    public class UpdateDashboardItemValidator : AbstractValidator<UpdateDashboardItemInput>
    {
        public UpdateDashboardItemValidator(IStringLocalizer localizer)
        {
            Include(new DashboardItemValidator(localizer));
        }
    }

    public class DeleteDashboardItemValidator : AbstractValidator<DeleteDashboardItemInput>
    {
        public DeleteDashboardItemValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseMsg.BaseEntity.ID])
                    .WithMessage(localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}