using Module.ShareData.Core.Entities;

namespace Module.ShareData.Core.Dto.ActivityLog
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 bản ghi nhật ký hoạt động (danh sách / chi tiết).
    ///              BeforeJson / AfterJson giữ nguyên chuỗi JSON để FE tự render phần so sánh.
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataActivityLogOutput : ShareDataActivityLog { }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 bản ghi nhật ký (phân trang).
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataPageActivityLogOutput : ShareDataActivityLog { }

    /// <summary>
    /// Author: HieuNV
    /// Description: Một dòng thống kê theo hành động
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataActivityCountOutput
    {
        public string? Code { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thống kê nhanh phục vụ các thẻ số liệu phía trên lưới nhật ký
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataSummaryActivityLogOutput
    {
        public int Total { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int SentCount { get; set; }
        public int ReceivedCount { get; set; }
        public List<ShareDataActivityCountOutput> ByAction { get; set; } = [];
    }
}
