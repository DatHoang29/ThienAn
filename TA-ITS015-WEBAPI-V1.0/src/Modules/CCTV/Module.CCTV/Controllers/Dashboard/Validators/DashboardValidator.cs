using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.CCTV.Controllers.Dashboard.Dto;
using Modules.CCTV.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.CCTV.Controllers.Dashboard.Validators
{
    public class DashboardValidator : AbstractValidator<AddDashboardInput>
    {
        public DashboardValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Name != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            //RuleFor(x => x.Tags)
            //    .MaximumLength(EntityConst.Length256)
            //        .WithName(x => localizer[BaseMsg.BaseEntity.Tags])
            //        .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }
    }

    public class AddDashboardValidator : AbstractValidator<AddDashboardInput>
    {
        public AddDashboardValidator(IStringLocalizer localizer)
        {
            Include(new DashboardValidator(localizer));
        }
    }

    public class UpdateDashboardValidator : AbstractValidator<UpdateDashboardInput>
    {
        public UpdateDashboardValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseMsg.BaseEntity.ID])
                    .WithMessage(localizer[BaseMsg.BaseValidation.Required]);

            Include(new DashboardValidator(localizer, isCreate: false));
        }
    }

    public class DeleteDashboardValidator : AbstractValidator<DeleteDashboardInput>
    {
        public DeleteDashboardValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseMsg.BaseEntity.ID])
                    .WithMessage(localizer[BaseMsg.BaseValidation.Required]);
        }
    }

    public class CopyDashboardValidator : AbstractValidator<CopyDashboardInput>
    {
        public CopyDashboardValidator(IStringLocalizer localizer)
        {
            // ID của dashboard gốc cần copy — bắt buộc
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseMsg.BaseEntity.ID])
                    .WithMessage(localizer[BaseMsg.BaseValidation.Required]);

            // Name của dashboard mới — bắt buộc và không vượt độ dài
            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }
    }
}