using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Validators
{
    /// <summary>
    /// Author: KienLT
    /// Description: Triển khai IncidentEntityValidator.
    /// Created date: 02/10/2026
    /// </summary>
    public class IncidentEntityValidator : AbstractValidator<TmsIncident>
    {
        public IncidentEntityValidator(IStringLocalizer? lz = null, bool isCreate = true)
        {
            lz ??= RuleLocalizer.Instance;
            RuleFor(x => x.Code).NotEmpty().MaximumLength(EntityConst.Length64)
                .When(x => isCreate || x.Code != null);
            // Đồng bộ entity/cột Name 128; tên unknown kèm AID có thể vượt 64.
            RuleFor(x => x.Name).NotEmpty().MaximumLength(EntityConst.Length128)
                .When(x => isCreate || x.Name != null);
            RuleFor(x => x.Metadata).MaximumLength(EntityConst.Length512);
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => !isCreate)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.EventTypeId)
                .NotEmpty()
                    .WithName(x => lz[BaseMsg.TmsIncident.Entity.EventTypeId])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.Required])
                    .When(x => isCreate || (!isCreate && x.EventTypeId != null))
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Source)
                .MaximumLength(EntityConst.ConfigFieldLength)
                    .WithName(x => lz[BaseMsg.BaseEntity.Source])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Location)
                .MaximumLength(EntityConst.ConfigFieldLength)
                    .WithName(x => lz[BaseMsg.TmsIncident.Entity.Location])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.ZoneId)
                .MaximumLength(EntityConst.Length1024)
                    .WithName(x => lz[BaseMsg.TmsIncident.Entity.ZoneId])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.InfluenceScope)
                .MaximumLength(EntityConst.ConfigFieldLength)
                    .WithName(x => lz[BaseMsg.TmsIncident.Entity.InfluenceScope])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.TrafficPlanId)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithName(x => lz[BaseMsg.TmsIncident.Entity.TrafficPlanId])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.EquipmentId)
                .MaximumLength(EntityConst.Length1024)
                    .WithName(x => lz[BaseMsg.TmsIncident.Entity.EquipmentId])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.State)
                .MaximumLength(EntityConst.ConfigFieldLength)
                    .WithName(x => lz[BaseMsg.BaseEntity.State])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Description)
                .MaximumLength(EntityConst.Length512)
                    .WithName(x => lz[BaseMsg.BaseEntity.Description])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
                .MaximumLength(IncidentRemarkConst.MaxLength)
                    .WithName(x => lz[BaseMsg.BaseEntity.Remark])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.MaxLength]);

            // >=0
            RuleFor(x => x.KmNumber)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => lz[BaseMsg.TmsIncident.Entity.KmNumber])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.GreaterThanOrEqual])
                    .When(x => isCreate || (!isCreate && x.KmNumber != null));

            // 0 - 999
            // Điều kiện When phải xét CHÍNH MetNumber: trước đây xét KmNumber nên update chỉ gửi mét
            // (KmNumber = null) sẽ bỏ qua luật 0-999 và ghi được giá trị ngoài khoảng.
            RuleFor(x => x.MetNumber)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => lz[BaseMsg.TmsIncident.Entity.MetNumber])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.GreaterThanOrEqual])
                .LessThan(1000)
                    .WithName(x => lz[BaseMsg.TmsIncident.Entity.MetNumber])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.LessThan])
                    .When(x => isCreate || (!isCreate && x.MetNumber != null));

            // >=0
            RuleFor(x => x.InjuredNumber)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => lz[BaseMsg.TmsIncident.Entity.InjuredNumber])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.GreaterThanOrEqual])
                    .When(x => isCreate || (!isCreate && x.InjuredNumber != null));

            // >=0
            RuleFor(x => x.VehicleNumber)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => lz[BaseMsg.TmsIncident.Entity.VehicleNumber])
                    .WithMessage(x => lz[BaseMsg.BaseValidation.GreaterThanOrEqual])
                    .When(x => isCreate || (!isCreate && x.VehicleNumber != null));
        }
    }
    internal sealed class RuleLocalizer : IStringLocalizer
    {
        public static readonly RuleLocalizer Instance = new();
        public LocalizedString this[string name] => new(name, name, true);
        public LocalizedString this[string name, params object[] arguments] => new(name, name, true);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
