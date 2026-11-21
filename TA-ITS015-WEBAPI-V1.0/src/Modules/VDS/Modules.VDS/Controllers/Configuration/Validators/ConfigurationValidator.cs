using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VDS.Controllers.Configuration.Dto;
using Modules.VDS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.VDS.Controllers.Configuration.Validators
{
    /// <summary>
    /// Author: KienLT
    /// Description: Luật CẤU TRÚC của một lần lưu tham số vận hành VDS: mã và giá trị bắt buộc, giới hạn
    /// độ dài.
    ///
    /// <para><b>Vì sao chuyển từ DataAnnotations sang đây:</b> giống <c>SegmentSaveValidator</c> — để tránh
    /// mỗi trường sai trả hai câu thông báo (một của framework bằng tiếng Anh, một đã localize). Luật giữ
    /// nguyên nội dung, chỉ đổi nơi khai.</para>
    ///
    /// <para><b>Chỉ có validator cho <see cref="VdsConfigValueInput"/>:</b> action <c>Refresh</c> nhận body
    /// là số nguyên trần rồi controller tự dựng <c>ConfigurationRefreshInput</c> để dispatch, nên
    /// FluentValidation không chạy cho DTO đó — dải 0..6 được chặn bằng <c>[FromBody, Range(0, 6)]</c> ngay
    /// trên tham số action.</para>
    ///
    /// <para><b>Ranh giới với command handler:</b> mỗi mã tham số có luật riêng chặt hơn (tốc độ 0–300, chu
    /// kỳ thống kê 1–60 phút, thư mục media phải là đường dẫn tuyệt đối, danh sách camera phải tồn tại, thời
    /// lượng mặc định phải có hậu tố s/m/h/d, mã ngoài danh sách bị từ chối). Toàn bộ nằm ở
    /// <c>ConfigurationCommandHandler</c> vì phụ thuộc DB và còn phải bảo vệ đường message bus.</para>
    /// Created date: 22/09/2026
    /// </summary>
    public class ConfigurationSaveValidator : AbstractValidator<VdsConfigValueInput>
    {
        public ConfigurationSaveValidator(IStringLocalizer lz)
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(_ => lz[BaseMsg.BaseEntity.Code])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length128)
                    .WithName(_ => lz[BaseMsg.BaseEntity.Code])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);

            // Giá trị rỗng KHÔNG hợp lệ ở đây: mọi mã mà feature này nhận đều cần một giá trị cụ thể.
            RuleFor(x => x.Value)
                .NotEmpty()
                    .WithName(_ => lz[BaseMsg.BaseEntity.Data])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length256)
                    .WithName(_ => lz[BaseMsg.BaseEntity.Data])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);
        }
    }
}
