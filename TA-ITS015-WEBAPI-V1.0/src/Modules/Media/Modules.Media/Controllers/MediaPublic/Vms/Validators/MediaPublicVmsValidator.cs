using Modules.Media.Controllers.MediaPublic.Vms.Dto;
using Modules.Media.Core.Dto.Vms;
using FluentValidation;
using Modules.Media.Infrastructure.Helpers;

namespace Modules.Media.Controllers.MediaPublic.Vms.Validators;

/// <summary>Description: Validate tham số ảnh VMS phía WP. Created date: 08/10/2026</summary>
public class MediaPublicVmsValidator : AbstractValidator<MediaPublicVmsInput>
{
    public MediaPublicVmsValidator()
    {
        RuleFor(x => x.ID).NotEmpty().Matches(MediaHelper.IdPattern);
        RuleFor(x => x.Field)
            .Must(f => string.IsNullOrWhiteSpace(f)
                       || string.Equals(f.Trim(), "Url", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(f.Trim(), "DefaultUrl", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Field chỉ nhận Url | DefaultUrl.");
    }
}
