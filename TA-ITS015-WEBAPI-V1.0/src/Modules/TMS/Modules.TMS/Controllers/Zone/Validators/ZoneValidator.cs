using Modules.TMS.Infrastructure.Services.Zone.Rules;
using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Controllers.Zone.Dto;
using Modules.TMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Controllers.Zone.Validators
{
    public class ZoneValidator : AbstractValidator<AddZoneInput>
    {
        public ZoneValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            // Add/Update và batch dùng cùng luật; null là không khai ngưỡng riêng của vùng.
            RuleFor(x => x.MinSpeed)
                .Must(speed => TmsZoneSpeedBatchRules.IsValidSpeed(speed))
                .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);
            RuleFor(x => x.MaxSpeed)
                .Must(speed => TmsZoneSpeedBatchRules.IsValidSpeed(speed))
                .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue])
                .Must((input, max) => TmsZoneSpeedBatchRules.IsOrdered(input.MinSpeed, max))
                .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);

            RuleFor(x => x.Pid)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithName(x => localizer[BaseMsg.BaseEntity.Pid])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength])
                .NotEqual(x => x.ID)
                    .WithMessage(x => localizer[BaseMsg.BaseException.CannotSelfParent]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseMsg.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            // >=0
            RuleFor(x => x.FromKmNumber)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.TmsZone.Entity.FromKmNumber])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);

            // >=0
            RuleFor(x => x.ToKmNumber)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.TmsZone.Entity.ToKmNumber])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual]);


            //0-999
            RuleFor(x => x.FromMetNumber)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.TmsZone.Entity.FromMetNumber])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual])
                .LessThan(1000)
                    .WithName(x => localizer[BaseMsg.TmsZone.Entity.FromMetNumber])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.LessThan]);
            //0-999
            RuleFor(x => x.ToMetNumber)
                .GreaterThanOrEqualTo(0)
                    .WithName(x => localizer[BaseMsg.TmsZone.Entity.ToMetNumber])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.GreaterThanOrEqual])
                .LessThan(1000)
                    .WithName(x => localizer[BaseMsg.TmsZone.Entity.ToMetNumber])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.LessThan]);



            //When
            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Name != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length64)
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.Code])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Code != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length64)
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Type)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.Type])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Type != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.ConfigFieldLength)
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Status)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.Status])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Status != null && !isCreate) || isCreate)
                .IsInEnum()
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);
        }
    }
    public class AddZoneValidator : AbstractValidator<AddZoneInput>
    {
        public AddZoneValidator(IStringLocalizer localizer)
        {
            Include(new ZoneValidator(localizer));
        }
    }

    public class UpdateZoneValidator : AbstractValidator<UpdateZoneInput>
    {
        public UpdateZoneValidator(IStringLocalizer localizer)
        {
            Include(new ZoneValidator(localizer, false));
        }
    }

    public class DeleteZoneValidator : AbstractValidator<DeleteZoneInput>
    {
        public DeleteZoneValidator(IStringLocalizer localizer)
        {
            RuleFor(data => data.ID)
                .NotEmpty()
                .WithName(localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
