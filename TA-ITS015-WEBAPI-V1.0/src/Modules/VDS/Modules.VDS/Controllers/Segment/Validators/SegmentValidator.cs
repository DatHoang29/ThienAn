using FluentValidation;
using Microsoft.Extensions.Localization;
using Modules.TMS.Core.Entities;
using Modules.VDS.Controllers.Segment.Dto;
using Modules.VDS.Core.Exceptions;
using Shared.DTO.Constants.Application;

namespace Modules.VDS.Controllers.Segment.Validators
{
    /// <summary>
    /// Author: KienLT
    /// Description: Luật CẤU TRÚC của một đoạn tuyến VDS: trường bắt buộc, độ dài, dải số.
    ///
    /// <para><b>Đặt trên ENTITY, không phải trên input</b> — đúng mẫu <c>{Feature}Validator :
    /// AbstractValidator&lt;{Entity}&gt;</c> của repo. Add/Update chỉ cần <c>Include</c>, và không có hai đăng
    /// ký <c>IValidator&lt;T&gt;</c> cùng closed generic: cái này là
    /// <c>IValidator&lt;TmsTrafficSegment&gt;</c>, Add là <c>IValidator&lt;AddSegmentInput&gt;</c>.</para>
    ///
    /// <para><b>Bản trước là <c>static SegmentRules.Apply</c> và lý do ghi trong đó đã sai:</b> nó nói "input
    /// không kế thừa entity nên validator cơ sở sẽ trùng kiểu với AddSegmentValidator" — lấy hệ quả của một
    /// lựa chọn để biện minh cho chính lựa chọn đó. Nay input kế thừa entity nên vấn đề không tồn tại.</para>
    ///
    /// <para><b>Vì sao không dùng DataAnnotations:</b> <c>[Required]</c>/<c>[StringLength]</c> sinh câu thông
    /// báo tiếng Anh của framework kèm tên property thô, còn toàn module đã localize qua
    /// <c>IStringLocalizer</c>. Để cả hai thì mỗi trường sai trả HAI câu.</para>
    ///
    /// <para><b>Ranh giới với command handler:</b> ở đây chỉ chặn lỗi hình dạng request. Luật NGHIỆP VỤ —
    /// camera đầu khác camera cuối, camera phải tồn tại trong <c>TmsEquipment</c>, đoạn A→B không trùng, bản
    /// ghi cần sửa phải còn — vẫn nằm ở <c>SegmentCommandHandler</c> vì handler còn được gọi qua message bus,
    /// nơi FluentValidation không chạy.</para>
    /// Created date: 22/09/2026
    /// </summary>
    public class SegmentValidator : AbstractValidator<TmsTrafficSegment>
    {
        /// <summary>Chiều dài đoạn tuyến tối thiểu (mét).</summary>
        internal const int MinDistanceMeter = 1;

        /// <summary>Chiều dài đoạn tuyến tối đa (mét) — 1000 km, thừa cho mọi tuyến trong hệ thống.</summary>
        internal const int MaxDistanceMeter = 1_000_000;

        /// <summary>Thời gian di chuyển tối thiểu cho phép khai (giây).</summary>
        internal const int MinTravelSeconds = 1;

        /// <summary>Thời gian di chuyển tối đa cho phép khai (giây) — trọn một ngày.</summary>
        internal const int MaxTravelSeconds = 86_400;

        /// <param name="lz">Localizer cho tên trường và câu thông báo.</param>
        /// <param name="isCreate">true khi thêm mới — lúc đó ID được phép để trống.</param>
        public SegmentValidator(IStringLocalizer lz, bool isCreate = true)
        {
            RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(_ => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.IdRequired])
                    .When(_ => !isCreate)
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithName(_ => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Name)
                .NotEmpty()
                    .WithName(_ => lz[BaseMsg.BaseEntity.Name])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length128)
                    .WithName(_ => lz[BaseMsg.BaseEntity.Name])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.FromEquipmentId)
                .NotEmpty()
                    .WithName(_ => lz[BaseMsg.Vds.Entity.FromEquipment])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                    .WithName(_ => lz[BaseMsg.Vds.Entity.FromEquipment])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.ToEquipmentId)
                .NotEmpty()
                    .WithName(_ => lz[BaseMsg.Vds.Entity.ToEquipment])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.Required])
                .MaximumLength(EntityConst.Length64)
                    .WithName(_ => lz[BaseMsg.Vds.Entity.ToEquipment])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);

            RuleFor(x => x.Direction)
                .MaximumLength(EntityConst.Length16)
                    .WithName(_ => lz[BaseMsg.Vds.Entity.Direction])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);

            // Cột nullable nhưng khoảng cách là dữ liệu BẮT BUỘC để tính tốc độ đoạn: thiếu thì đoạn vô dụng.
            RuleFor(x => x.DistanceMeter)
                .NotNull()
                    .WithName(_ => lz[BaseMsg.Vds.Entity.DistanceMeter])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.Required])
                .InclusiveBetween(MinDistanceMeter, MaxDistanceMeter)
                    .WithName(_ => lz[BaseMsg.Vds.Entity.DistanceMeter])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MustBeValidValue]);

            // Hai biên thời gian được PHÉP để trống: handler đặt mặc định 1s và 3600s, giữ đúng hành vi hồi
            // DTO còn khai default. Chỉ kiểm dải khi client có gửi.
            RuleFor(x => x.MinTravelSeconds)
                .InclusiveBetween(MinTravelSeconds, MaxTravelSeconds)
                    .WithName(_ => lz[BaseMsg.Vds.Entity.MinTravelSeconds])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MustBeValidValue])
                    .When(x => x.MinTravelSeconds != null);

            RuleFor(x => x.MaxTravelSeconds)
                .InclusiveBetween(MinTravelSeconds, MaxTravelSeconds)
                    .WithName(_ => lz[BaseMsg.Vds.Entity.MaxTravelSeconds])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MustBeValidValue])
                    .When(x => x.MaxTravelSeconds != null);

            // Cột Status là chuỗi "1"/"0" (không phải enum như VdsConfig): để trống nghĩa là bật.
            // Bộ mã khai ở chính entity TmsTrafficSegment để module và worker không mỗi bên gõ một chuỗi.
            RuleFor(x => x.Status)
                .Must(v => v is TmsTrafficSegment.StatusDisabled or TmsTrafficSegment.StatusEnabled)
                    .WithName(_ => lz[BaseMsg.BaseEntity.Status])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MustBeValidValue])
                    .When(x => x.Status != null);

            RuleFor(x => x.OrderNo)
                .GreaterThanOrEqualTo(0)
                    .WithName(_ => lz[BaseMsg.BaseEntity.OrderNo])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.GreaterThanOrEqual])
                    .When(x => x.OrderNo != null);

            RuleFor(x => x.Remark)
                .MaximumLength(EntityConst.RemarkFieldLength)
                    .WithName(_ => lz[BaseMsg.BaseEntity.Remark])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);
        }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Kiểm dữ liệu thêm đoạn tuyến VDS — ID để trống là bình thường.
    /// Created date: 22/09/2026
    /// </summary>
    public class AddSegmentValidator : AbstractValidator<AddSegmentInput>
    {
        public AddSegmentValidator(IStringLocalizer lz) => Include(new SegmentValidator(lz));
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Kiểm dữ liệu cập nhật đoạn tuyến VDS — ID BẮT BUỘC.
    ///
    /// <para>Đây là lớp chặn mà bản gộp Add/Update trước đây không thể có: validator lúc đó phải nhận ID rỗng
    /// để còn thêm mới được.</para>
    /// Created date: 22/09/2026
    /// </summary>
    public class UpdateSegmentValidator : AbstractValidator<UpdateSegmentInput>
    {
        public UpdateSegmentValidator(IStringLocalizer lz) => Include(new SegmentValidator(lz, false));
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Kiểm dữ liệu xóa đoạn tuyến VDS.
    ///
    /// <para>Báo "ID bắt buộc" NGAY tại đây thay vì để handler truy DB rồi trả "không tìm thấy đoạn tuyến":
    /// hai lỗi đó khác nhau về nguyên nhân, gộp lại làm người dùng đi sai hướng khi sửa.</para>
    /// Created date: 22/09/2026
    /// </summary>
    public class DeleteSegmentValidator : AbstractValidator<DeleteSegmentInput>
    {
        public DeleteSegmentValidator(IStringLocalizer lz)
            => RuleFor(x => x.ID)
                .NotEmpty()
                    .WithName(_ => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.IdRequired])
                .MaximumLength(EntityConst.KeyFieldLength)
                    .WithName(_ => lz[BaseMsg.BaseEntity.ID])
                    .WithMessage(_ => lz[BaseMsg.BaseValidation.MaxLength]);
    }
}
