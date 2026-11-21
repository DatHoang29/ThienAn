using Shared.Core.Utilities;

namespace Module.TOLL.Core.Dto.Tram
{
    /// <summary>
    /// Input tìm kiếm danh sách trạm thu phí (phân trang).
    /// Author: HieuNV
    /// Description: Điều kiện lọc dữ liệu bảng TollStation phục vụ tra cứu
    /// Created date: 08/08/2026
    /// </summary>
    public class PageTramInput : BasePageInput
    {
        /// <summary>
        /// Từ khoá tìm kiếm: mã trạm (StationId) hoặc tên trạm (Name)
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Loại trạm thu phí
        /// </summary>
        public int? StationType { get; set; }

        /// <summary>
        /// Kích hoạt/ vô hiệu (1 = kích hoạt, 0 = vô hiệu)
        /// </summary>
        public int? Active { get; set; }
    }

    /// <summary>
    /// Input lấy danh sách trạm thu phí (không phân trang).
    /// Dùng cho dropdown / combobox chọn trạm.
    /// Author: HieuNV
    /// Created date: 08/08/2026
    /// </summary>
    public class TramInput
    {
        /// <summary>
        /// Từ khoá tìm kiếm: mã trạm (StationId) hoặc tên trạm (Name)
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Loại trạm thu phí
        /// </summary>
        public int? StationType { get; set; }

        /// <summary>
        /// Kích hoạt/ vô hiệu (1 = kích hoạt, 0 = vô hiệu)
        /// </summary>
        public int? Active { get; set; }
    }

    /// <summary>
    /// Input gán (map) trạm thu phí với đơn vị TMS loại Trạm thu phí (TmsWorkUnit.Type = toll).
    /// RefId rỗng/null = bỏ gán.
    /// Author: HieuNV
    /// Created date: 07/10/2026
    /// </summary>
    public class MapWorkUnitTramInput : BaseIdInput
    {
        /// <summary>
        /// ID đơn vị TMS (TmsWorkUnit.ID). Để trống để bỏ gán.
        /// </summary>
        public string? RefId { get; set; }
    }
}
