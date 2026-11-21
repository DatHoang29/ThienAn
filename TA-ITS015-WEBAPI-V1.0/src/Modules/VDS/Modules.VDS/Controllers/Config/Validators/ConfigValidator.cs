using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.VDS.Controllers.Config.Dto;
using Modules.VDS.Core.Entities;
using Modules.VDS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.VDS.Controllers.Config.Validators
{
    /// <summary>
    /// Author: KienLT
    /// Description: Luật CẤU TRÚC của một dòng cấu hình VDS: trường bắt buộc, độ dài, dải trạng thái.
    ///
    /// <para><b>Đặt trên ENTITY, không phải trên input</b> — đúng mẫu <c>{Feature}Validator :
    /// AbstractValidator&lt;{Entity}&gt;</c> của repo (xem <c>OpConfigValidator</c>,
    /// <c>EquipmentValidator</c>). Nhờ vậy Add/Update chỉ cần <c>Include</c>, và không có hai đăng ký
    /// <c>IValidator&lt;T&gt;</c> nào cùng closed generic: cái này là
    /// <c>IValidator&lt;VdsConfig&gt;</c>, Add là <c>IValidator&lt;AddVdsConfigInput&gt;</c>.</para>
    ///
    /// <para><b>Bản trước là <c>static ConfigRules.Apply</c> và lý do ghi trong đó đã sai:</b> nó nói "input
    /// không kế thừa entity nên validator cơ sở sẽ trùng kiểu với AddConfigValidator". Điều đó chỉ đúng VÌ
    /// input khi ấy không kế thừa entity — tức là lấy hệ quả của một lựa chọn để biện minh cho chính lựa chọn
    /// đó. Nay input kế thừa entity như mọi feature khác nên vấn đề không tồn tại.</para>
    ///
    /// <para><b>Ranh giới với <c>VdsConfigRules.Validate</c>:</b> ở đây chỉ chặn lỗi hình dạng request để
    /// người dùng nhận được thông báo gắn đúng tên trường. Các luật NGHIỆP VỤ — tiền tố nhóm <c>vds_</c>,
    /// hậu tố thời lượng, dải ngưỡng zone/chu kỳ thống kê, khóa mapping cỡ xe, cặp min/max tốc độ, trùng mã,
    /// camera/loại sự kiện có tồn tại — vẫn nằm ở <c>VdsConfigRules</c> và command handler.</para>
    ///
    /// <para><b>Vì sao không dồn hết sang đây:</b> FluentValidation chỉ tự chạy cho tham số action. Handler
    /// còn được gọi qua message bus nên nếu bỏ kiểm tra bên đó thì đường bus mất bảo vệ. Hai lớp là CỐ Ý,
    /// không phải trùng lặp nhầm.</para>
    /// Created date: 21/09/2026
    /// </summary>
    public class ConfigValidator : AbstractValidator<VdsConfig>
    {
        /// <param name="lz">Localizer cho tên trường và câu thông báo.</param>
        /// <param name="isCreate">true khi thêm mới — lúc đó ID được phép để trống.</param>
        public ConfigValidator(IStringLocalizer lz, bool isCreate = true)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(_ => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.Required])
                    .When(_ => !isCreate)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithName(_ => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.GroupCode)
                .NotEmpty()
                    .WithName(_ => lz[BaseMsg.Vds.Entity.GroupCode])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                    .WithName(_ => lz[BaseMsg.Vds.Entity.GroupCode])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Code)
                .NotEmpty()
                    .WithName(_ => lz[BaseMsg.BaseEntity.Code])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length128)
                    .WithName(_ => lz[BaseMsg.BaseEntity.Code])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(_ => lz[BaseMsg.BaseEntity.Name])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length256)
                    .WithName(_ => lz[BaseMsg.BaseEntity.Name])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Value)
                .MaximumLength(EntityConst.Length256)
                    .WithName(_ => lz[BaseMsg.BaseEntity.Data])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.Length1024)
                    .WithName(_ => lz[BaseMsg.BaseEntity.Remark])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);

            // Cột Status là nullable: KHÔNG gửi nghĩa là để handler đặt mặc định Enable — đúng hành vi cũ khi
            // DTO còn khai Status non-nullable với default. Chỉ kiểm dải khi client có gửi, và dùng IsInEnum
            // thay vì NotEmpty vì Disable = 0 là giá trị hợp lệ (NotEmpty sẽ loại đúng giá trị này).
            RuleFor(x => x.Status)
                .IsInEnum()
                    .WithName(_ => lz[BaseMsg.BaseEntity.Status])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MustBeValidValue])
                    .When(x => x.Status != null);

            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(_ => lz[BaseMsg.BaseEntity.OrderNo])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.GreaterThanOrEqual])
                    .When(x => x.OrderNo != null);
        }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Giới hạn số ngày tra cứu loại AID chưa ánh xạ trong khoảng 1–90.
    /// Created date: 22/10/2026
    /// </summary>
    public class UnmappedEventTypesValidator : AbstractValidator<UnmappedEventTypesInput>
    {
        public UnmappedEventTypesValidator(IStringLocalizer lz)
        {
            RuleFor(x => x.Days)
                .InclusiveBetween(1, 90)
                    .WithName(_ => lz[BaseMsg.Vds.Entity.TimeWindow])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MustBeValidValue]);
        }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Kiểm tra dữ liệu thêm cấu hình VDS — ID để trống là bình thường.
    /// Created date: 21/09/2026
    /// </summary>
    public class AddConfigValidator : AbstractValidator<AddVdsConfigInput>
    {
        public AddConfigValidator(IStringLocalizer lz) => Include(new ConfigValidator(lz));
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Kiểm tra dữ liệu cập nhật cấu hình VDS — ID BẮT BUỘC.
    /// Created date: 21/09/2026
    /// </summary>
    public class UpdateConfigValidator : AbstractValidator<UpdateVdsConfigInput>
    {
        public UpdateConfigValidator(IStringLocalizer lz) => Include(new ConfigValidator(lz, false));
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Kiểm tra dữ liệu xóa cấu hình VDS.
    ///
    /// <para>Báo "ID bắt buộc" NGAY tại đây thay vì để handler truy DB rồi trả "không tìm thấy cấu hình":
    /// hai lỗi đó khác nhau về nguyên nhân, gộp lại làm người dùng đi sai hướng khi sửa.</para>
    /// Created date: 21/09/2026
    /// </summary>
    public class DeleteConfigValidator : AbstractValidator<DeleteVdsConfigInput>
    {
        public DeleteConfigValidator(IStringLocalizer lz)
            => RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(_ => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.IdRequired])
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithName(_ => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);
    }
}
