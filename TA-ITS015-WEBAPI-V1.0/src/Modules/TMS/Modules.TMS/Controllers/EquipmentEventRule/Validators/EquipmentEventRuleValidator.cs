using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.EquipmentEventRule;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.EquipmentEventRule.Validators
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Validator dùng chung cho rule hiển thị VMS/LCS theo loại sự kiện
    /// Created date: 16/07/2026
    /// </summary>
    public class EquipmentEventRule : AbstractValidator<TmsEquipmentEventRule>
    {
        private static readonly string[] AllowedProcessTypes =
        {
            EquipmentEventRuleConst.ProcessType.Before,
            EquipmentEventRuleConst.ProcessType.After
        };

        public EquipmentEventRule(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.EventTypeId)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.TmsEquipmentEventRule.Entity.EventTypeId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => isCreate)
                .MaximumLength(EntityConst.KeyFieldLength)
                .WithName(x => localizer[BaseMsg.TmsEquipmentEventRule.Entity.EventTypeId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.EquipmentTypeId)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.TmsEquipmentEventRule.Entity.EquipmentTypeId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => isCreate)
                .MaximumLength(EntityConst.KeyFieldLength)
                .WithName(x => localizer[BaseMsg.TmsEquipmentEventRule.Entity.EquipmentTypeId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.ProcessType)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.TmsEquipmentEventRule.Entity.ProcessType])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => isCreate)
                .Must(value => value != null && AllowedProcessTypes.Contains(value.Trim()))
                .WithMessage(x => localizer[BaseMsg.TmsEquipmentEventRule.Validation.ProcessTypeInvalid])
                .When(x => !string.IsNullOrWhiteSpace(x.ProcessType));

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithName(x => localizer[BaseMsg.BaseEntity.Status])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MustBeValidValue])
                .When(x => x.Status != null);

            // SuggestionRangeMeters đã NGỪNG DÙNG: phạm vi gợi ý lấy từ SysOpConfig (operation_config).
            // Bỏ luôn rule GreaterThan(0) vì nó không có .When() -> chỉ cần FE gửi 0 là toàn bộ request
            // thêm/sửa rule bị chặn dù nghiệp vụ không dùng field này.

            RuleFor(x => x.MaxTargetCount)
                .GreaterThan(0)
                .WithName(x => localizer[BaseMsg.TmsEquipmentEventRule.Entity.MaxTargetCount])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThan])
                .When(x => x.MaxTargetCount != null);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.RemarkFieldLength)
                .WithName(x => localizer[BaseMsg.BaseEntity.Remark])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Created date: 16/07/2026
    /// </summary>
    public class AddEquipmentEventRuleValidator : AbstractValidator<AddEquipmentEventRuleInput>
    {
        public AddEquipmentEventRuleValidator(IStringLocalizer localizer)
        {
            Include(new EquipmentEventRule(localizer));
        }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Created date: 16/07/2026
    /// </summary>
    public class UpdateEquipmentEventRuleValidator : AbstractValidator<UpdateEquipmentEventRuleInput>
    {
        public UpdateEquipmentEventRuleValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);

            Include(new EquipmentEventRule(localizer, false));
        }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Created date: 16/07/2026
    /// </summary>
    public class DeleteEquipmentEventRuleValidator : AbstractValidator<DeleteEquipmentEventRuleInput>
    {
        public DeleteEquipmentEventRuleValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
