using Shared.Core.Utilities;

namespace Module.TOLL.Core.Dto.LoaiPhuongTien
{
    /// <summary>
    /// Input tìm kiếm danh sách loại phương tiện (phân trang).
    /// Nguồn dữ liệu: TollMasterData với DataType = VehicleType
    /// Author: HieuNV
    /// Created date: 08/08/2026
    /// </summary>
    public class PageLoaiPhuongTienInput : BasePageInput
    {
        /// <summary>
        /// Từ khoá tìm kiếm: mã gốc ETC (SourceId) hoặc tên loại phương tiện (Name)
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Nhóm phân loại theo ETC (GroupCode)
        /// </summary>
        public string? GroupCode { get; set; }

        /// <summary>
        /// Kích hoạt/ vô hiệu (1 = kích hoạt, 0 = vô hiệu)
        /// </summary>
        public int? Active { get; set; }
    }

    /// <summary>
    /// Input lấy danh sách loại phương tiện (không phân trang) - dùng cho dropdown.
    /// Author: HieuNV
    /// Created date: 08/08/2026
    /// </summary>
    public class LoaiPhuongTienInput
    {
        /// <summary>
        /// Từ khoá tìm kiếm: mã gốc ETC (SourceId) hoặc tên loại phương tiện (Name)
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Nhóm phân loại theo ETC (GroupCode)
        /// </summary>
        public string? GroupCode { get; set; }

        /// <summary>
        /// Kích hoạt/ vô hiệu (1 = kích hoạt, 0 = vô hiệu)
        /// </summary>
        public int? Active { get; set; }
    }
}
