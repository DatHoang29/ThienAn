using FluentValidation;
using Modules.Media.Controllers.ItsMedia.Vms.Dto;
using Modules.Media.Infrastructure.Helpers;

namespace Modules.Media.Controllers.ItsMedia.Vms.Validators;

/// <summary>Description: Validate tham số ảnh VMS phía ITS. Created date: 08/10/2026</summary>
public class ItsMediaVmsValidator : AbstractValidator<ItsMediaVmsInput>
{
    public ItsMediaVmsValidator()
    {
        RuleFor(x => x.ID).NotEmpty().Matches(MediaHelper.IdPattern);
        RuleFor(x => x.Field)
            .Must(f => string.IsNullOrWhiteSpace(f)
                       || string.Equals(f.Trim(), "Url", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(f.Trim(), "DefaultUrl", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Field chỉ nhận Url | DefaultUrl.");
    }
}
