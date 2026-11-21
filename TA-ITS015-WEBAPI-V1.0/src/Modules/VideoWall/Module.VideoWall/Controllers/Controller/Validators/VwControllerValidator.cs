using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.VideoWall.Core.Dto.Controller;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Module.VideoWall.Core.Exceptions;

namespace Module.VideoWall.Controllers.Controller.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Rule dùng chung cho Add và Update VwController
    /// Created date: 2026-08-01
    /// </summary>
    public class VwControllerValidator : AbstractValidator<VwAddControllerInput>
    {
        public VwControllerValidator(
            IStringLocalizer localizer,
            bool isCreate = true,
            BaseRepository<VwController>? controllerRep = null)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => (x.Name != null && !isCreate) || isCreate)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Name])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Model)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Type])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Chassis)
                .MaximumLength(EntityConst.Length64)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Type])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // IP / Account / PassWord không nhận từ FE: VwControllerCommandHandler chép từ thiết bị (RefId) khi lưu.

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length256)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Remark])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            RuleFor(x => x.Status)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Status])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .When(x => (x.Status != null && !isCreate) || isCreate)
                .IsInEnum()
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MustBeValidValue]);


            RuleFor(x => x.InputSlotsNumber)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.SlotNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.InputSlotsNumber.HasValue);

            RuleFor(x => x.SlotsNumber)
                .GreaterThanOrEqualTo(0)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.SlotNo])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.GreaterThanOrEqual])
                .When(x => x.SlotsNumber.HasValue);

            RuleFor(x => x.IntegrationMode)
                .Must(mode => string.IsNullOrWhiteSpace(mode)
                              || mode == "cascade"
                              || mode == "standalone")
                    .WithMessage(x => localizer[BaseMsg.Vw.Validation.IntegrationModeInvalid]);

            // Quan hệ cha–con của tường phân cấp (thay cho Role đã bỏ 26/09/2026) được kiểm tra
            // bất đồng bộ trong VwControllerCommandHandler.EnsureValidParentAsync.
            RuleFor(x => x.ParentControllerId)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);

            // RefId = TmsEquipment.ID (bắt buộc — thông tin kết nối lấy từ thiết bị);
            // thiết bị tồn tại / đủ thông tin / chưa gắn bộ khác được kiểm tra trong VwControllerCommandHandler.
            RuleFor(x => x.RefId)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.EquipmentId])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.EquipmentId])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho thêm mới VwController
    /// Created date: 2026-08-01
    /// </summary>
    public class VwAddControllerValidator : AbstractValidator<VwAddControllerInput>
    {
        public VwAddControllerValidator(IStringLocalizer localizer)
        {
            Include(new VwControllerValidator(localizer));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho cập nhật VwController
    /// Created date: 2026-08-01
    /// </summary>
    public class VwUpdateControllerValidator : AbstractValidator<VwUpdateControllerInput>
    {
        public VwUpdateControllerValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);

            Include(new VwControllerValidator(localizer, isCreate: false));
        }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Validator cho xóa VwController
    /// Created date: 2026-08-01
    /// </summary>
    public class VwDeleteControllerValidator : AbstractValidator<VwDeleteControllerInput>
    {
        public VwDeleteControllerValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required]);
        }
    }

    /// <summary>
    /// Description: Validator cho truy vấn kênh tín hiệu đầu vào của VwController
    /// Created date: 13/09/2026
    /// </summary>
    public class VwGetInputChannelsControllerValidator : AbstractValidator<VwGetInputChannelsControllerInput>
    {
        public VwGetInputChannelsControllerValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.Required])
                .MaximumLength(EntityConst.Length32)
                    .WithName(localizer[BaseLocaleManager.BaseEntity.ID])
                    .WithMessage(localizer[BaseLocaleManager.BaseValidation.MaxLength]);
        }
    }
}
