using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.SignalLogs.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.SignalLogs.Validators
{
    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class TmsSignalLogValidator : AbstractValidator<TmsSignalLog>
    {
        public TmsSignalLogValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(data => data.EquipmentId)
              .NotEmpty()
              .WithName(x => localizer[BaseMsg.TmsSignalLog.Entity.EquipmentId])
              .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
              .When(x => isCreate)
              .MaximumLength(EntityConst.KeyFieldLength)
              .WithName(x => localizer[BaseMsg.TmsSignalLog.Entity.EquipmentId])
              .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Type)
                .MaximumLength(EntityConst.ConfigFieldLength)
                .WithName(x => localizer[BaseMsg.BaseEntity.Type])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            //RuleFor(x => x.Status)
            //.NotEmpty()
            //   .WithName(x => localizer[BaseMsg.BaseEntity.Status])
            //   .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
            //   .When(x => (x.Status != null && !isCreate) || isCreate)
            //   .IsInEnum()
            //   .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);

            RuleFor(data => data.Remark)
                .MaximumLength(EntityConst.Length512)
                .WithName(x => localizer[BaseMsg.BaseEntity.Remark])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(data => data.OldData)
                .MaximumLength(EntityConst.Length512)
                .WithName(x => localizer[BaseMsg.TmsSignalLog.Entity.OldData])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(data => data.NewData)
                .MaximumLength(EntityConst.Length512)
                .WithName(x => localizer[BaseMsg.TmsSignalLog.Entity.NewData])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(data => data.OldUrl)
                .MaximumLength(EntityConst.Length512)
                .WithName(x => localizer[BaseMsg.TmsSignalLog.Entity.OldUrl])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(data => data.NewUrl)
                .MaximumLength(EntityConst.Length512)
                .WithName(x => localizer[BaseMsg.TmsSignalLog.Entity.NewUrl])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(data => data.IncidentId)
                .MaximumLength(EntityConst.KeyFieldLength)
                .WithName(x => localizer[BaseMsg.TmsSignalLog.Entity.IncidentId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class AddTmsSignalLogValidator : AbstractValidator<AddTmsSignalLogInput>
    {
        public AddTmsSignalLogValidator(IStringLocalizer localizer)
        {
            Include(new TmsSignalLogValidator(localizer));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class UpdateTmsSignalLogValidator : AbstractValidator<UpdateTmsSignalLogInput>
    {
        public UpdateTmsSignalLogValidator(IStringLocalizer localizer)
        {
            Include(new TmsSignalLogValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: TuanHA
    /// Created date: 15/01/2025
    /// </summary>
    public class DeleteTmsSignalLogValidator : AbstractValidator<DeleteTmsSignalLogInput>
    {
        public DeleteTmsSignalLogValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }
    }
}
