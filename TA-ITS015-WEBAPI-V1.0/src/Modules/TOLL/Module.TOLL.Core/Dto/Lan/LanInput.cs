using Shared.Core.Utilities;

namespace Module.TOLL.Core.Dto.Lan
{
    /// <summary>
    /// Input tìm kiếm danh sách làn thu phí (phân trang).
    /// Author: HieuNV
    /// Description: Điều kiện lọc dữ liệu bảng TollLane phục vụ tra cứu
    /// Created date: 08/08/2026
    /// </summary>
    public class PageLanInput : BasePageInput
    {
        /// <summary>
        /// Từ khoá tìm kiếm: mã làn (LaneId) hoặc tên làn (Name)
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Mã trạm thu phí (lọc làn theo trạm)
        /// </summary>
        public string? StationId { get; set; }

        /// <summary>
        /// Hướng vào ra của ETC
        /// </summary>
        public string? Direction { get; set; }
    }

    /// <summary>
    /// Input lấy danh sách làn thu phí (không phân trang) - dùng cho dropdown.
    /// Author: HieuNV
    /// Created date: 08/08/2026
    /// </summary>
    public class LanInput
    {
        /// <summary>
        /// Từ khoá tìm kiếm: mã làn (LaneId) hoặc tên làn (Name)
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Mã trạm thu phí (lọc làn theo trạm)
        /// </summary>
        public string? StationId { get; set; }

        /// <summary>
        /// Hướng vào ra của ETC
        /// </summary>
        public string? Direction { get; set; }
    }
}
