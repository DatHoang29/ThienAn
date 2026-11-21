namespace Module.ShareData.Core.Dto.Session
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO trả về 1 phiên kết nối (phân trang)
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataPageSessionOutput : ShareDataSessionOutput { }

    /// <summary>
    /// Author: HieuNV
    /// Description: Số liệu tổng hợp cho các thẻ thống kê đầu màn vận hành:
    /// Created date: 2026-08-05
    /// </summary>
    public class ShareDataSummarySessionOutput
    {
        /// <summary>Số phiên đang ở trạng thái ACTIVE.</summary>
        public int ActiveSessions { get; set; }

        /// <summary>Tổng số gói đã gửi.</summary>
        public long PacketsSent { get; set; }

        /// <summary>Tổng số gói đã nhận.</summary>
        public long PacketsRecv { get; set; }

        /// <summary>Số đăng ký đang ở trạng thái ACTIVE.</summary>
        public int ActiveSubscriptions { get; set; }

        /// <summary>Số đăng ký INBOUND đang chờ duyệt.</summary>
        public int PendingApprovals { get; set; }

        /// <summary>
        /// Số cảnh báo chưa xử lý. Luôn bằng 0 ở giai đoạn này —
        /// TODO(BE): nối vào ShareDataAlertLog khi dựng màn "Cảnh báo &amp; lỗi".
        /// </summary>
        public int UnackedAlerts { get; set; }
    }
}
