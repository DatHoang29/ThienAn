using Shared.Core.Utilities;

namespace Modules.Notification.Controllers.Notification.Dto
{
    /// <summary>
    /// Author: SonTH
    /// Description: DTO phân trang danh sách thông báo của user hiện tại.
    ///              Mọi điều kiện đều scope theo UserId lấy từ claims — KHÔNG nhận userId từ FE.
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiPageInput : BasePageInput
    {
        /// <summary>null = tất cả; true = đã đọc; false = chưa đọc (badge).</summary>
        public bool? IsRead { get; set; }

        /// <summary>Lọc theo Category (vd TMS.Incident).</summary>
        public string? Category { get; set; }

        /// <summary>Lọc theo mức độ: Info | Warning | Critical.</summary>
        public string? Severity { get; set; }

        /// <summary>Tìm theo một phần tiêu đề/nội dung.</summary>
        public string? Keyword { get; set; }

        /// <summary>Lọc theo khoảng thời gian gửi.</summary>
        public DateTime? FromTime { get; set; }

        public DateTime? ToTime { get; set; }
    }

    /// <summary>
    /// Author: SonTH
    /// Description: DTO lấy chi tiết 1 thông báo theo Id dòng người nhận (NotiRecipient.ID).
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiIdInput : BaseIdInput { }

    /// <summary>
    /// Author: SonTH
    /// Description: DTO đánh dấu đã đọc — theo Ids hoặc tất cả (All = true cho nút "đọc hết").
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiMarkReadInput
    {
        public List<string>? Ids { get; set; }

        public bool All { get; set; }
    }

    /// <summary>
    /// Author: SonTH
    /// Description: DTO xoá (soft-delete) thông báo của chính mình, theo Ids dòng người nhận.
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiDeleteInput
    {
        public List<string> Ids { get; set; } = null!;
    }

    /// <summary>
    /// Author: SonTH
    /// Description: DTO đếm thông báo chưa đọc (badge). Không cần tham số.
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiUnreadCountInput { }

    /// <summary>
    /// Author: SonTH
    /// Description: Kết quả đếm thông báo chưa đọc.
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiUnreadCountOutput
    {
        public int Count { get; set; }
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Một dòng trên lưới thông báo của user.
    ///              Id là NotiRecipient.ID (đúng dòng của user) — dùng cho mark-read/delete/detail.
    /// </summary>
    public class NotiListOutput
    {
        public string ID { get; set; } = null!;

        public string MessageId { get; set; } = null!;

        public string Title { get; set; } = null!;

        public string? Content { get; set; }

        public string Category { get; set; } = null!;

        public string Severity { get; set; } = null!;

        public string? SourceKey { get; set; }

        public DateTime SendTime { get; set; }

        public DateTime? ReadTime { get; set; }
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Chi tiết một thông báo của user (nội dung đầy đủ).
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiDetailOutput : NotiListOutput
    {
        public string TargetType { get; set; } = null!;
    }
}
