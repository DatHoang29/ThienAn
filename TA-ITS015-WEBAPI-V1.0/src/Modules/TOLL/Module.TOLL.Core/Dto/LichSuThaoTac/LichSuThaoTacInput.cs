using Shared.Core.Utilities;
using System;

namespace Module.TOLL.Core.Dto.LichSuThaoTac
{
    /// <summary>
    /// Input tìm kiếm lịch sử truy cập ứng dụng của nhân viên thu phí (phân trang).
    /// Nguồn dữ liệu: bảng TollAccessHistory (đồng bộ từ TCS - appaccesslog/accesshistorytcs).
    /// StartTime/EndTime là giờ UTC+0 (FE gửi lên); server convert về giờ local trước khi lọc.
    /// Author: ChuongLP
    /// Created date: 24/09/2026
    /// </summary>
    public class PageLichSuThaoTacInput : BasePageInput
    {
        /// <summary>
        /// Thời gian bắt đầu khoảng tra cứu (UTC+0). Lọc theo AccessTime &gt;= StartTime.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Thời gian kết thúc khoảng tra cứu (UTC+0). Lọc theo AccessTime &lt;= EndTime.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Tên hành động cần lọc (vd: Login / Logout). Bỏ trống = lấy tất cả.
        /// </summary>
        public string? ActionName { get; set; }

        /// <summary>
        /// Mã nhân viên / tài khoản truy cập cần lọc (tìm gần đúng theo EmployeeCode).
        /// </summary>
        public string? Account { get; set; }
    }
}
