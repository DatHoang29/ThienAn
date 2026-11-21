using FluentValidation;
using Microsoft.Extensions.Localization;
using Module.ShareData.Core.Dto.Inbound;
using Module.ShareData.Core.Exceptions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;

namespace Module.ShareData.Controllers.Inbound.Validators
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Kiểm tra body + header (đã gán vào input ở Controller) của API nhận gói tin.
    ///              PartnerCode / DatatypeId (PacketCode) giờ bắt buộc vì là định danh chính đọc từ header.
    ///              Kiểm tra đối tác tồn tại / đang bật nằm ở handler (cần CSDL).
    ///              Chưa kiểm tra cấu trúc JSON của RawContent — sẽ bổ sung sau.
    /// Created date: 15/09/2026
    /// Updated date: 20/09/2026
    /// </summary>
    public class AddInboundValidator : AbstractValidator<ShareDataAddInboundInput>
    {
        public AddInboundValidator(IStringLocalizer localizer)
        {
            RuleFor(x => x.RawContent)
                .NotEmpty()
                    .WithName(x => localizer[BaseLocaleManager.BaseEntity.Content])
                    .WithMessage(x => localizer[BaseLocaleManager.BaseValidation.Required]);

            // Header PartnerCode — thiếu thì không tra được đối tác (xem ShareDataInboundCommandHandler).
            RuleFor(x => x.PartnerCode)
                .NotEmpty()
                    .WithMessage(x => localizer[BaseMsg.ShareData.Validation.PartnerCodeHeaderRequired]);

            // Header PacketCode — được Controller gán vào DatatypeId; thiếu thì không tra được đăng ký/gói tin.
            RuleFor(x => x.PacketCode)
                .NotEmpty()
                    .WithMessage(x => localizer[BaseMsg.ShareData.Validation.PacketCodeHeaderRequired]);
        }
    }
}
