using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.Incident.Validators
{
    //TmsIncident {
    //ID varchar(64) pk
    //Code string(64) null
    //Name string(64) null
    //EventTypeId varchar(64) null > TmsEventType.ID
    //StartDate datetime null
    //Source string(16) null def(0)
    //Location string(16) null def(0)
    //ZoneId varchar(1024) null >* TmsZone.ID
    //KmNumber integer null
    //MetNumber integer null
    //InfluenceScope string(16) null def(0)
    //InjuredNumber integer null
    //VehicleNumber integer null
    //CreatedBy varchar(64) null > SysUser.Username
    //CreatedDate datetime null
    //ValidatedBy varchar(64) null > SysUser.Username
    //ValidatedDate datetime null
    //ApprovedBy varchar(64) > SysUser.Username
    //ApprovedDate datetime null
    //FinishedBy varchar(64) null > SysUser.Username
    //FinishedDate datetime null
    //TrafficPlanId varchar(64) null > TmsTrafficPlan.ID
    //EquipmentId varchar(1024) null >* TmsEquipment.ID
    //State varchar(16) null def(1)
    //Description string(512) null
    //Remark string(256) null
    //}

    /// <summary>
    /// Author: ThangNN
    /// Description: Kiểm tra dữ liệu insert-update TmsIncident
    /// Created date: 09/08/2024
    /// </summary>
    // Keep the public validator name; all rules live in the Core validator used by VDS too.
    public class IncidentValidator : Modules.TMS.Core.Validators.IncidentEntityValidator
    {
        public IncidentValidator(IStringLocalizer lz, bool isCreate = true) : base(lz, isCreate) { }
    }
    /// <summary>
    /// Author: ThangNN
    /// Created date: 09/08/2024
    /// </summary>
    public class AddIncidentValidator : AbstractValidator<AddTmsIncidentInput>
    {
        public AddIncidentValidator(IStringLocalizer localizer)
        {
            Include(new IncidentValidator(localizer));
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Created date: 09/08/2024
    /// </summary>
    public class UpdateIncidentValidator : AbstractValidator<UpdateTmsIncidentInput>
    {
        public UpdateIncidentValidator(IStringLocalizer localizer)
        {
            Include(new IncidentValidator(localizer, false));
        }
    }

    /// <summary>
    /// Author: ThangNN
    /// Created date: 09/08/2024
    /// </summary>
    public class DeleteIncidentValidator : AbstractValidator<DeleteTmsIncidentInput>
    {
        public DeleteIncidentValidator(IStringLocalizer lz)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);
        }
    }
}
