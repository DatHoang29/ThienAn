using FluentValidation;
using Modules.Media.Controllers.ItsMedia.EquipmentType.Dto;
using Modules.Media.Infrastructure.Helpers;

namespace Modules.Media.Controllers.ItsMedia.EquipmentType.Validators;

/// <summary>Description: Validate tham số icon loại thiết bị phía ITS. Created date: 08/10/2026</summary>
public class ItsMediaEquipmentTypeValidator : AbstractValidator<ItsMediaEquipmentTypeInput>
{
    public ItsMediaEquipmentTypeValidator()
    {
        RuleFor(x => x.ID).NotEmpty().Matches(MediaHelper.IdPattern);
        RuleFor(x => x.Field)
            .Must(f => string.IsNullOrWhiteSpace(f)
                       || new[] { "default", "active", "inactive", "fault" }.Contains(f.Trim().ToLowerInvariant()))
            .WithMessage("Field chỉ nhận Default | Active | Inactive | Fault.");
    }
}
