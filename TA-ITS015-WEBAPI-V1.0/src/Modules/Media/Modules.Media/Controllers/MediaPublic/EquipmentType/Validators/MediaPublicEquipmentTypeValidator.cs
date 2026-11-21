using Modules.Media.Controllers.MediaPublic.EquipmentType.Dto;
using Modules.Media.Core.Dto.EquipmentType;
using FluentValidation;
using Modules.Media.Infrastructure.Helpers;

namespace Modules.Media.Controllers.MediaPublic.EquipmentType.Validators;

/// <summary>Description: Validate tham số icon loại thiết bị phía WP. Created date: 08/10/2026</summary>
public class MediaPublicEquipmentTypeValidator : AbstractValidator<MediaPublicEquipmentTypeInput>
{
    public MediaPublicEquipmentTypeValidator()
    {
        RuleFor(x => x.ID).NotEmpty().Matches(MediaHelper.IdPattern);
        RuleFor(x => x.Field)
            .Must(f => string.IsNullOrWhiteSpace(f)
                       || new[] { "default", "active", "inactive", "fault" }.Contains(f.Trim().ToLowerInvariant()))
            .WithMessage("Field chỉ nhận Default | Active | Inactive | Fault.");
    }
}
