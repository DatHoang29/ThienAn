using Shared.Core.Utilities;
using Shared.DTO.Enums;
using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Entities;

namespace Modules.Mail.Core.Dto.LogMail
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Lọc nhật ký gửi mail (phân trang). Mặc định mới nhất lên đầu.
    /// Created date: 02/10/2026
    /// </summary>
    public class MailPageLogInput : BasePageInput
    {
        /// <summary>Lịch gửi (RptEmailSchedule.ID).</summary>
        public string? ScheduleId { get; set; }

        /// <summary>Kết quả: Success / Fail.</summary>
        public BaseEnums.SuccessEnums? Status { get; set; }

        /// <summary>Nguồn: theo lịch / gửi ngay / gửi thử cấu hình.</summary>
        public BaseEnums.TriggerType? TriggerType { get; set; }

        /// <summary>Gửi từ ngày (tính cả ngày này).</summary>
        public DateTime? FromDate { get; set; }

        /// <summary>Gửi đến ngày (tính cả ngày này).</summary>
        public DateTime? ToDate { get; set; }

        /// <summary>Mã lịch gửi (tìm gần đúng, gồm cả lịch đã xoá).</summary>
        public string? Code { get; set; }

        /// <summary>Tên lịch gửi (tìm gần đúng, gồm cả lịch đã xoá).</summary>
        public string? Name { get; set; }

        /// <summary>Địa chỉ người nhận — tìm gần đúng trong To / CC / BCC của lần gửi.</summary>
        public string? Recipient { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO lấy chi tiết 1 lần gửi theo ID
    /// Created date: 02/10/2026
    /// </summary>
    public class MailIdLogInput : BaseIdInput { }

}
