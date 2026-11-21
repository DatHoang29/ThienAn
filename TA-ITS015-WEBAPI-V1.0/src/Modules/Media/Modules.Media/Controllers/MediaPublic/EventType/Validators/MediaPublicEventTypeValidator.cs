using Modules.Media.Controllers.MediaPublic.EventType.Dto;
using Modules.Media.Core.Dto.EventType;
using FluentValidation;
using Modules.Media.Infrastructure.Helpers;

namespace Modules.Media.Controllers.MediaPublic.EventType.Validators;

/// <summary>Description: Validate tham số icon loại sự kiện phía WP. Created date: 08/10/2026</summary>
public class MediaPublicEventTypeValidator : AbstractValidator<MediaPublicEventTypeInput>
{
    public MediaPublicEventTypeValidator()
    {
        RuleFor(x => x.ID).NotEmpty().Matches(MediaHelper.IdPattern);
    }
}
