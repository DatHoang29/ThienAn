using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.Mail.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lịch gửi báo cáo qua email — phần đầu thư.
    ///
    ///              MỖI LỊCH MỘT recurring job Hangfire "rpt-email-{ID}" (RptEmailJobScheduler) —
    ///              lưu / bật tắt / xoá lịch thì đồng bộ job ngay, khởi động API thì đồng bộ lại toàn bộ.
    ///              Bảng này vẫn là nguồn đúng: Hangfire chỉ chạy theo.
    /// Created date: 14/08/2026
    /// </summary>
    [SugarTable("MailSchedule")]
    public class MailSchedule : EntityTenant
    {
        /// <summary>Tên lịch để người dùng nhận ra trên lưới.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Name { get; set; }
        /// <summary>
        /// Tần suất chạy: year | quarter | month | week | day.
        ///
        /// KHÔNG còn suy từ báo cáo được nữa: một thư có thể chứa báo cáo tháng lẫn báo cáo ngày,
        /// nên tần suất là lựa chọn độc lập của người dùng, tách hẳn khỏi kỳ dữ liệu của item.
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length16)]
        public string Frequency { get; set; } = null!;

        /// <summary>
        /// Ngày chạy trong tháng (1-31, 31 = ngày cuối tháng). Chỉ dùng khi Frequency = month | quarter | year.
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? DayOfMonth { get; set; }

        /// <summary>
        /// Tháng chạy TRONG KỲ của tần suất.
        ///   - quarter: 1-3 = tháng thứ mấy của quý (1 = tháng 1/4/7/10, 3 = tháng 3/6/9/12)
        ///   - year   : 1-12 = tháng trong năm
        /// Null = 1 để lịch tạo trước khi có cột này chạy đúng như cũ (tháng đầu quý / tháng 01).
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? MonthInPeriod { get; set; }

        /// <summary>
        /// Các thứ trong tuần được chạy, chuẩn ISO 1 = thứ Hai … 7 = Chủ nhật, ngăn bằng dấu phẩy ("1,2,3,4,5").
        ///   - week: đúng MỘT thứ
        ///   - day : các thứ được chạy; null = mọi ngày
        /// Đọc / ghi qua RptScheduleTime.ParseDaysOfWeek / FormatDaysOfWeek.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? DaysOfWeek { get; set; }

        /// <summary>Giờ chạy (0-23).</summary>
        [SugarColumn(IsNullable = false)]
        public int HourOfDay { get; set; }

        /// <summary>Phút chạy (0-59).</summary>
        [SugarColumn(IsNullable = false)]
        public int MinuteOfHour { get; set; }

        /// <summary>
        /// Mốc đến hạn đã gửi gần nhất, dạng yyyyMMddHHmm — CHỐT CHỐNG GỬI TRÙNG.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? LastRunKey { get; set; }
        /// <summary>Người nhận (To), ngăn bằng "; ".</summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length1024)]
        public string Recipients { get; set; } = null!;

        /// <summary>Đồng gửi (CC), ngăn bằng "; ". Không trùng địa chỉ ở Recipients.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024)]
        public string? Cc { get; set; }

        /// <summary>Gửi ẩn (BCC), ngăn bằng "; ". Không trùng địa chỉ ở Recipients / Cc.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024)]
        public string? Bcc { get; set; }

        /// <summary>
        /// Tiêu đề thư, có thể chứa tag hệ thống ({KyBaoCao} {DanhSachBaoCao} {NhomBaoCao}...) — xem RptEmailTag
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length256)]
        public string Subject { get; set; } = null!;
        /// <summary>
        /// Đường dẫn file NỘI DUNG THƯ của lịch (HTML soạn bằng wangEditor, có tag hệ thống), tính từ wwwroot:
        /// "Upload/Mail/ScheduleTemplate/{yyyy}/{MM}/{dd}/{ID}.html" — ngày = ngày tạo lịch. Sửa lịch thì ghi đè
        /// đúng file này. Mỗi lần gửi đọc file rồi thay tag (MailBodyRenderer).
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? ContentPath { get; set; }

        /// <summary>
        /// Mã mẫu (RptEmailTemplate.Code) đã chọn lúc soạn — CHỈ để giao diện biết mẫu đang chọn. Nội dung thư đã chép
        /// sang ContentPath nên mẫu bị sửa / xoá về sau thì lịch vẫn gửi đúng nội dung đã lưu.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? TemplateCode { get; set; }

        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? NextTimeRun { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? LastTimeRun { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.SuccessEnums? LastStatus { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public int? OrderNo { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
