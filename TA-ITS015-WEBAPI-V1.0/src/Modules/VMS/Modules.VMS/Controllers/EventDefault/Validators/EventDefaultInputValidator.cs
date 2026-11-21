using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VMS.Controllers.EventDefault.Dto;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.VMS.Controllers.EventDefault.Validators
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Bộ quy tắc kiểm tra dữ liệu cho VmsEventDefault (dùng chung cho Add/Update).
    /// Created date: 15/07/2026
    /// </summary>
    public class VmsEventDefaultValidator : AbstractValidator<VmsEventDefault>
    {
        public VmsEventDefaultValidator(IStringLocalizer localizer, bool isCreate = true)
        {
            RuleFor(x => x.EventTypeId)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.VmsEventDefault.Entity.EventTypeId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);

            RuleFor(x => x.EquipmentTypeId)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.VmsEventDefault.Entity.EquipmentTypeId])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);

            RuleFor(x => x.Size)
                .MaximumLength(EntityConst.ConfigFieldLength)
                .WithName(x => localizer[BaseMsg.BaseEntity.Size])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .MaximumLength(EntityConst.Length64)
                .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.RowData)
                .MaximumLength(EntityConst.Length1024)
                .WithName(x => localizer[BaseMsg.VmsEventDefault.Entity.RowData])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Url)
                .MaximumLength(EntityConst.Length512)
                .WithName(x => localizer[BaseMsg.VmsEventDefault.Entity.Url])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength])
                .When(x => !string.IsNullOrEmpty(x.Url) && !x.Url.StartsWith("data:")); // bỏ qua khi là base64

            RuleFor(x => x.Font)
                .MaximumLength(EntityConst.ConfigFieldLength)
                .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Color)
                .MaximumLength(EntityConst.ConfigFieldLength)
                .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.BackColor)
                .MaximumLength(EntityConst.ConfigFieldLength)
                .WithName(x => localizer[BaseMsg.BaseEntity.Name])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Kiểm tra dữ liệu đầu vào khi thêm mới AddVmsEventDefaultInput.
    /// Created date: 15/07/2026
    /// </summary>
    public class AddVmsEventDefaultInputValidator : AbstractValidator<AddVmsEventDefaultInput>
    {
        public AddVmsEventDefaultInputValidator(IStringLocalizer localizer)
        {
            Include(new VmsEventDefaultValidator(localizer));
        }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Kiểm tra dữ liệu đầu vào khi cập nhật UpdateVmsEventDefaultInput.
    /// Created date: 15/07/2026
    /// </summary>
    public class UpdateVmsEventDefaultInputValidator : AbstractValidator<UpdateVmsEventDefaultInput>
    {
        public UpdateVmsEventDefaultInputValidator(IStringLocalizer localizer)
        {
            Include(new VmsEventDefaultValidator(localizer, false));
            RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.KeyFieldLength)
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: Kiểm tra dữ liệu đầu vào khi xóa DeleteVmsEventDefaultInput.
    /// Created date: 15/07/2026
    /// </summary>
    public class DeleteVmsEventDefaultInputValidator : AbstractValidator<DeleteVmsEventDefaultInput>
    {
        public DeleteVmsEventDefaultInputValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                .WithName(x => localizer[BaseMsg.BaseEntity.ID])
                .WithMessage(x => localizer[BaseMsg.BaseValidation.Required]);
        }
    }
}
