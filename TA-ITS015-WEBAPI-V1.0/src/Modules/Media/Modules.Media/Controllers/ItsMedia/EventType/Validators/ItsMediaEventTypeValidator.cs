using FluentValidation;
using Modules.Media.Controllers.ItsMedia.EventType.Dto;
using Modules.Media.Infrastructure.Helpers;

namespace Modules.Media.Controllers.ItsMedia.EventType.Validators;

/// <summary>Description: Validate tham số icon loại sự kiện phía ITS. Created date: 08/10/2026</summary>
public class ItsMediaEventTypeValidator : AbstractValidator<ItsMediaEventTypeInput>
{
    public ItsMediaEventTypeValidator()
    {
        RuleFor(x => x.ID).NotEmpty().Matches(MediaHelper.IdPattern);
    }
}
