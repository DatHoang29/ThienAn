using Shared.Core.Utilities;

namespace Module.TOLL.Core.Dto.CaLamViec
{
    /// <summary>
    /// Input tìm kiếm danh sách ca làm việc (phân trang).
    /// Author: HieuNV
    /// Description: Điều kiện lọc dữ liệu bảng TollShift phục vụ tra cứu
    /// Created date: 08/08/2026
    /// </summary>
    public class PageCaLamViecInput : BasePageInput
    {
        /// <summary>
        /// Từ khoá tìm kiếm: mã ca (ShiftId) hoặc tên ca (Name)
        /// </summary>
        public string? Keyword { get; set; }
    }

    /// <summary>
    /// Input lấy danh sách ca làm việc (không phân trang) - dùng cho dropdown.
    /// Author: HieuNV
    /// Created date: 08/08/2026
    /// </summary>
    public class CaLamViecInput
    {
        /// <summary>
        /// Từ khoá tìm kiếm: mã ca (ShiftId) hoặc tên ca (Name)
        /// </summary>
        public string? Keyword { get; set; }
    }
}
