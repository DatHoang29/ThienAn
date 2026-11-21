using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.CCTV.Controllers.Template.Dto;
using Modules.CCTV.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.CCTV.Controllers.Template.Validators
{
    /// <summary>
    /// Validate từng slot trong template.
    /// W và H phải > 0; SlotNo phải >= 1 (slot bắt đầu từ 1).
    /// </summary>
    public class TemplateSlotValidator : AbstractValidator<TemplateSlot>
    {
        public TemplateSlotValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.SlotNo)
                .GreaterThanOrEqualTo(1)
                    .WithName(localizer[BaseMsg.BaseEntity.SlotNo])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeGreaterThanZero]);

            RuleFor(x => x.W)
                .GreaterThan(0)
                    .WithName(localizer[BaseMsg.BaseEntity.Width])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeGreaterThanZero]);

            RuleFor(x => x.H)
                .GreaterThan(0)
                    .WithName(localizer[BaseMsg.BaseEntity.Height])
                    .WithMessage(localizer[BaseMsg.BaseValidation.MustBeGreaterThanZero]);
        }
    }

    /// <summary>Rule dùng chung cho Add và Update Template.</summary>
    public class TemplateValidator : AbstractValidator<AddTemplateInput>
    {
        public TemplateValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .When(x => (x.Name != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseMsg.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            // Status nếu có truyền thì phải là giá trị hợp lệ (0 hoặc 1)
            RuleFor(x => x.Status)
               .NotEmpty()
               .WithName(x => localizer[BaseMsg.BaseEntity.Status])
               .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
               .When(x => (x.Status != null && !isCreate) || isCreate)
               .IsInEnum()
               .WithMessage(localizer[BaseMsg.BaseValidation.MustBeValidValue]);

            // Validate từng slot nếu có truyền Items
            RuleForEach(x => x.Items)
                .SetValidator(new TemplateSlotValidator(localizer))
                .When(x => x.Items != null && x.Items.Any());
        }
    }

    public class AddTemplateValidator : AbstractValidator<AddTemplateInput>
    {
        public AddTemplateValidator(IStringLocalizer localizer)
        {
            Include(new TemplateValidator(localizer));
        }
    }

    public class UpdateTemplateValidator : AbstractValidator<UpdateTemplateInput>
    {
        public UpdateTemplateValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseMsg.BaseEntity.ID])
                    .WithMessage(localizer[BaseMsg.BaseValidation.Required]);

            Include(new TemplateValidator(localizer, isCreate: false));
        }
    }

    public class DeleteTemplateValidator : AbstractValidator<DeleteTemplateInput>
    {
        public DeleteTemplateValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseMsg.BaseEntity.ID])
                    .WithMessage(localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}