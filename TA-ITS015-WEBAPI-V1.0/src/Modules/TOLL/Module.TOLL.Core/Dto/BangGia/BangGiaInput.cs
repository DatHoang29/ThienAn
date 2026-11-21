using Shared.Core.Utilities;

namespace Module.TOLL.Core.Dto.BangGia
{
    /// <summary>
    /// Input tìm kiếm danh sách bảng giá thu phí (phân trang).
    /// Nguồn dữ liệu: TollPriceList
    /// Author: HieuNV
    /// Created date: 07/10/2026
    /// </summary>
    public class PageBangGiaInput : BasePageInput
    {
        /// <summary>
        /// Mã bảng giá (SourceId)
        /// </summary>
        public string? SourceId { get; set; }

        /// <summary>
        /// Tên bảng giá (Name)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Trạng thái hoạt động
        /// </summary>
        public bool? Active { get; set; }
    }

    /// <summary>
    /// Input lấy danh sách bảng giá (không phân trang) - dùng cho dropdown.
    /// Author: HieuNV
    /// Created date: 07/10/2026
    /// </summary>
    public class BangGiaInput
    {
        /// <summary>
        /// Mã bảng giá (SourceId)
        /// </summary>
        public string? SourceId { get; set; }

        /// <summary>
        /// Tên bảng giá (Name)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Trạng thái hoạt động
        /// </summary>
        public bool? Active { get; set; }
    }
}
