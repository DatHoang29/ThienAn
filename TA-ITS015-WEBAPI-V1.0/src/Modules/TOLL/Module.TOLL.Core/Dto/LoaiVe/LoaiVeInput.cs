using Shared.Core.Utilities;

namespace Module.TOLL.Core.Dto.LoaiVe
{
    /// <summary>
    /// Input tìm kiếm danh sách loại vé (phân trang).
    /// Nguồn dữ liệu: TollMasterData với DataType = TicketType
    /// Author: HieuNV
    /// Created date: 13/08/2026
    /// </summary>
    public class PageLoaiVeInput : BasePageInput
    {
        /// <summary>
        /// Từ khoá tìm kiếm: mã gốc ETC (SourceId) hoặc tên loại vé (Name)
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
    /// Input lấy danh sách loại vé (không phân trang) - dùng cho dropdown.
    /// Author: HieuNV
    /// Created date: 13/08/2026
    /// </summary>
    public class LoaiVeInput
    {
        /// <summary>
        /// Từ khoá tìm kiếm: mã gốc ETC (SourceId) hoặc tên loại vé (Name)
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
