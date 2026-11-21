using FluentValidation;
using Modules.Media.Controllers.ItsMedia.Incident.Dto;
using Modules.Media.Infrastructure.Helpers;

namespace Modules.Media.Controllers.ItsMedia.Incident.Validators;

/// <summary>Description: Validate tham số lấy ảnh sự cố phía ITS. Created date: 08/10/2026</summary>
public class ItsMediaIncidentValidator : AbstractValidator<ItsMediaIncidentInput>
{
    public ItsMediaIncidentValidator()
    {
        RuleFor(x => x.ID).NotEmpty().Matches(MediaHelper.IdPattern);
        RuleFor(x => x.PictureType).InclusiveBetween(1, 4);
    }
}
