using Modules.Mail.Core.Constants;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.Mail.Core.Dto.EmailSchedule
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO phân trang danh sách lịch gửi báo cáo
    /// Created date: 14/08/2026
    /// </summary>
    public class RptPageEmailScheduleInput : BasePageInput
    {
        public string? Name { get; set; }

        /// <summary>
        /// Lọc lịch có chứa báo cáo này.
        /// </summary>
        public string? ReportCode { get; set; }

        /// <summary>Tìm theo một phần địa chỉ người nhận.</summary>
        public string? Recipient { get; set; }

        /// <summary>Tần suất chạy của lịch: year | quarter | month | week | day.</summary>
        public string? Frequency { get; set; }

        public BaseEnums.StatusEnum? Status { get; set; }

        /// <summary>Lọc theo kết quả lần gửi gần nhất — dùng cho màn "lịch đang lỗi".</summary>
        public BaseEnums.SuccessEnums? LastStatus { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO tìm kiếm danh sách lịch gửi báo cáo (không phân trang)
    /// Created date: 14/08/2026
    /// </summary>
    public class RptEmailScheduleInput
    {
        public string? ID { get; set; }
        public string? Name { get; set; }
        public string? Frequency { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 lịch gửi báo cáo theo ID
    /// Created date: 14/08/2026
    /// </summary>
    public class MailIdScheduleInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: Gợi ý địa chỉ email từ sổ địa chỉ (bảng RptEmailAddress). Keyword rỗng = lấy tất cả.
    /// Created date: 05/10/2026
    /// </summary>
    public class MailAddressInput
    {
        /// <summary>Tìm gần đúng theo địa chỉ.</summary>
        public string? Keyword { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Một báo cáo đính kèm trong lịch.
    /// Created date: 14/08/2026
    /// </summary>
    public class MailScheduleItemInput
    {
        public string ReportCode { get; set; } = null!;

        /// <summary>Bỏ qua — kỳ luôn lấy theo kỳ khai trong code (RptReportCode).</summary>
        public string? PeriodType { get; set; }

        /// <summary>0 = kỳ hiện tại (lũy kế), 1 = kỳ trước — danh sách ở SysConfigData loại mail_period.</summary>
        public int PeriodOffset { get; set; } = MailRptPeriodOffset.Previous;

        /// <summary>Bỏ trống thì mặc định pdf.</summary>
        public string? Format { get; set; }

        /// <summary>Tham số chi tiết dạng JSON — dành cho sau này, hiện chưa dùng.</summary>
        public string? ParamsJson { get; set; }
        public int? OrderNo { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thêm mới 1 lịch gửi báo cáo.
    /// Created date: 14/08/2026
    /// </summary>
    public class MailAddScheduleInput
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string Frequency { get; set; } = null!;
        public int? DayOfMonth { get; set; }

        /// <summary>quarter: 1-3 (tháng thứ mấy của quý) | year: 1-12. Tần suất khác bỏ qua.</summary>
        public int? MonthInPeriod { get; set; }

        /// <summary>ISO 1 = thứ Hai … 7 = Chủ nhật, ngăn bằng dấu phẩy. week: đúng 1 thứ | day: rỗng = mọi ngày.</summary>
        public string? DaysOfWeek { get; set; }

        public int HourOfDay { get; set; }
        public int MinuteOfHour { get; set; }
        public string Recipients { get; set; } = null!;
        /// <summary>CC, ngăn bằng ; hoặc , hoặc xuống dòng. Không bắt buộc.</summary>
        public string? Cc { get; set; }
        /// <summary>BCC, ngăn bằng ; hoặc , hoặc xuống dòng. Không bắt buộc.</summary>
        public string? Bcc { get; set; }
        /// <summary>Mã mẫu đã chọn (RptEmailTemplate.Code) — chỉ để giao diện biết mẫu đang chọn.</summary>
        public string? TemplateCode { get; set; }

        /// <summary>Tiêu đề email, có tag hệ thống. Bỏ trống thì dùng RptEmailConst.DefaultSubject.</summary>
        public string? Subject { get; set; }
        public string? Content { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
        public int? OrderNo { get; set; }
        public string? Remark { get; set; }
        public List<MailScheduleItemInput> Items { get; set; } = new();
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO cập nhật 1 lịch gửi báo cáo.
    /// Created date: 14/08/2026
    /// </summary>
    public class MailUpdateScheduleInput : MailAddScheduleInput
    {
        public string ID { get; set; } = null!;
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO xóa 1 lịch gửi báo cáo
    /// Created date: 14/08/2026
    /// </summary>
    public class MailDeleteScheduleInput : BaseIdInput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO bật / tắt 1 lịch gửi báo cáo.
    /// Created date: 14/08/2026
    /// </summary>
    public class MailToggleScheduleInput : BaseIdInput
    {
        public BaseEnums.StatusEnum Status { get; set; }
    }
}
