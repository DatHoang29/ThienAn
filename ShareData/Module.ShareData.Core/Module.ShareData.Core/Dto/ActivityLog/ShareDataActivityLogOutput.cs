using Module.ShareData.Core.Entities;

namespace Module.ShareData.Core.Dto.ActivityLog
{
    /// <summary>
    /// Description: DTO trả về 1 bản ghi nhật ký hoạt động (danh sách / chi tiết).
    ///              BeforeJson / AfterJson giữ nguyên chuỗi JSON để FE tự render phần so sánh.
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataActivityLogOutput : ShareDataActivityLog { }

    /// <summary>
    /// Description: DTO trả về 1 bản ghi nhật ký (phân trang).
    ///              QueryHandler KHÔNG select BeforeJson / AfterJson để lưới không phải tải JSON lớn
    ///              (không dùng thủ thuật `new` che property vì Newtonsoft vẫn đọc property lớp cha).
    ///              Muốn xem nội dung so sánh thì gọi GetById.
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataPageActivityLogOutput : ShareDataActivityLog { }

    /// <summary>
    /// Description: Một dòng thống kê theo hành động
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataActivityCountOutput
    {
        /// <summary>Mã hành động hoặc mã kết quả.</summary>
        public string? Code { get; set; }

        /// <summary>Số bản ghi.</summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// Description: DTO thống kê nhanh phục vụ các thẻ số liệu phía trên lưới nhật ký
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataSummaryActivityLogOutput
    {
        /// <summary>Tổng số bản ghi trong khoảng lọc.</summary>
        public int Total { get; set; }

        /// <summary>Số bản ghi thành công.</summary>
        public int SuccessCount { get; set; }

        /// <summary>Số bản ghi thất bại.</summary>
        public int FailedCount { get; set; }

        /// <summary>Số gói gửi đi (chỉ có nghĩa với LogType = TRANSFER).</summary>
        public int SentCount { get; set; }

        /// <summary>Số gói nhận về (chỉ có nghĩa với LogType = TRANSFER).</summary>
        public int ReceivedCount { get; set; }

        /// <summary>Phân bố theo hành động.</summary>
        public List<ShareDataActivityCountOutput> ByAction { get; set; } = new();
    }
}
