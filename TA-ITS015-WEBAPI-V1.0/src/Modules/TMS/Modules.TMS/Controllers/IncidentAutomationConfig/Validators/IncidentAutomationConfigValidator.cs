using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.IncidentAutomationConfig;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.IncidentAutomationConfig.Validators
{
    /// <summary>
    /// Author: SonTH
    /// Description: Validator dùng chung cho cấu hình tự động xử lý sự kiện/sự cố
    /// Created date: 24/07/2026
    /// </summary>
    public class IncidentAutomationConfigValidator : AbstractValidator<TmsIncidentAutomationConfig>
    {
        private static readonly string[] AllowedTriggerStates =
        {
            IncidentAutomationConst.TriggerState.Registration,
            IncidentAutomationConst.TriggerState.Verified
        };

        private static readonly string[] AllowedFailurePolicies =
        {
            IncidentAutomationConst.FailurePolicy.StopAndResetDefault,
            IncidentAutomationConst.FailurePolicy.Continue,
            IncidentAutomationConst.FailurePolicy.RetryThenStop
        };

        private static readonly string[] AllowedResetPolicies =
        {
            IncidentAutomationConst.ResetPolicy.ResetToDefault,
            IncidentAutomationConst.ResetPolicy.NoReset
        };

        public IncidentAutomationConfigValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.EventTypeId)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.TmsIncidentAutomationConfig.Entity.EventTypeId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => isCreate)
                .MaximumLength(EntityConst.KeyFieldLength)
                .WithName(x => localizer[BaseMsg.TmsIncidentAutomationConfig.Entity.EventTypeId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.TriggerState)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.TmsIncidentAutomationConfig.Entity.TriggerState])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .Must(value => AllowedTriggerStates.Contains(value))
                .WithName(x => localizer[BaseMsg.TmsIncidentAutomationConfig.Entity.TriggerState])
                .WithMessage(x => localizer[BaseMsg.TmsIncidentAutomationConfig.Validation.TriggerStateInvalid])
                .When(x => !string.IsNullOrWhiteSpace(x.TriggerState));

            RuleFor(x => x.WarningDelaySeconds)
                .InclusiveBetween(0, 86400)
                .WithName(x => localizer[BaseMsg.TmsIncidentAutomationConfig.Entity.WarningDelaySeconds])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.TrafficPlanId)
                .MaximumLength(EntityConst.KeyFieldLength)
                .WithName(x => localizer[BaseMsg.TmsIncidentAutomationConfig.Entity.TrafficPlanId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.AutoFinishAfterSeconds)
                .NotNull()
                .WithName(x => localizer[BaseMsg.TmsIncidentAutomationConfig.Entity.AutoFinishAfterSeconds])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => x.IsAutoFinishEnabled);

            RuleFor(x => x.AutoFinishAfterSeconds)
                .GreaterThan(0)
                .WithName(x => localizer[BaseMsg.TmsIncidentAutomationConfig.Entity.AutoFinishAfterSeconds])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThan])
                .When(x => x.AutoFinishAfterSeconds != null);

            RuleFor(x => x.UnclaimedAutoFinishAfterSeconds)
                .NotNull()
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .InclusiveBetween(1, 604800)
                .WithMessage("Thời gian tự hoàn thành chưa xử lý phải từ 1 đến 604800 giây.")
                .When(x => x.IsUnclaimedAutoFinishEnabled);

            RuleFor(x => x.FailurePolicy)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.TmsIncidentAutomationConfig.Entity.FailurePolicy])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .Must(value => AllowedFailurePolicies.Contains(value))
                .WithName(x => localizer[BaseMsg.TmsIncidentAutomationConfig.Entity.FailurePolicy])
                .WithMessage(x => localizer[BaseMsg.TmsIncidentAutomationConfig.Validation.FailurePolicyInvalid])
                .When(x => !string.IsNullOrWhiteSpace(x.FailurePolicy));

            RuleFor(x => x.ResetPolicy)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.TmsIncidentAutomationConfig.Entity.ResetPolicy])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .Must(value => AllowedResetPolicies.Contains(value))
                .WithName(x => localizer[BaseMsg.TmsIncidentAutomationConfig.Entity.ResetPolicy])
                .WithMessage(x => localizer[BaseMsg.TmsIncidentAutomationConfig.Validation.ResetPolicyInvalid])
                .When(x => !string.IsNullOrWhiteSpace(x.ResetPolicy));

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithName(x => localizer[BaseMsg.BaseEntity.Status])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MustBeValidValue])
                .When(x => x.Status != null);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.RemarkFieldLength)
                .WithName(x => localizer[BaseMsg.BaseEntity.Remark])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Author: SonTH
    /// Created date: 24/07/2026
    /// </summary>
    public class AddIncidentAutomationConfigValidator : AbstractValidator<AddIncidentAutomationConfigInput>
    {
        public AddIncidentAutomationConfigValidator(IStringLocalizer localizer)
        {
            Include(new IncidentAutomationConfigValidator(localizer));
        }
    }

    /// <summary>
    /// Author: SonTH
    /// Created date: 24/07/2026
    /// </summary>
    public class UpdateIncidentAutomationConfigValidator : AbstractValidator<UpdateIncidentAutomationConfigInput>
    {
        public UpdateIncidentAutomationConfigValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);

            Include(new IncidentAutomationConfigValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: SonTH
    /// Created date: 24/07/2026
    /// </summary>
    public class DeleteIncidentAutomationConfigValidator : AbstractValidator<DeleteIncidentAutomationConfigInput>
    {
        public DeleteIncidentAutomationConfigValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
