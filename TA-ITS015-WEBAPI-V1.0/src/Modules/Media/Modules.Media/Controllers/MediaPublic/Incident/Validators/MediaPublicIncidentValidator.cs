using Modules.Media.Controllers.MediaPublic.Incident.Dto;
using Modules.Media.Core.Dto.Incident;
using FluentValidation;
using Modules.Media.Infrastructure.Helpers;

namespace Modules.Media.Controllers.MediaPublic.Incident.Validators;

/// <summary>Description: Validate tham số ảnh sự cố phía WP. Created date: 08/10/2026</summary>
public class MediaPublicIncidentValidator : AbstractValidator<MediaPublicIncidentInput>
{
    public MediaPublicIncidentValidator()
    {
        RuleFor(x => x.ID).NotEmpty().Matches(MediaHelper.IdPattern);
        RuleFor(x => x.PictureType).InclusiveBetween(1, 4);
    }
}
