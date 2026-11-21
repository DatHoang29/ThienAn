using Shared.Core.Utilities;

namespace Module.TOLL.Core.Dto.ChiTietBangGia
{
    /// <summary>
    /// Input tìm kiếm danh sách chi tiết bảng giá thu phí (phân trang).
    /// Nguồn dữ liệu: TollPriceListDetail
    /// Author: HieuNV
    /// Created date: 08/08/2026
    /// </summary>
    public class PageChiTietBangGiaInput : BasePageInput
    {
        /// <summary>
        /// Mã chi tiết bảng giá (Code)
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Tên chi tiết bảng giá (Name)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Mã bảng giá nguồn ETC (PriceListSourceId)
        /// </summary>
        public string? PriceListSourceId { get; set; }

        /// <summary>
        /// Mã loại xe nguồn ETC (VehicleTypeSourceId)
        /// </summary>
        public string? VehicleTypeSourceId { get; set; }

        /// <summary>
        /// Trạng thái hoạt động
        /// </summary>
        public bool? Active { get; set; }
    }

    /// <summary>
    /// Input lấy danh sách chi tiết bảng giá (không phân trang) - dùng cho dropdown.
    /// Author: HieuNV
    /// Created date: 08/08/2026
    /// </summary>
    public class ChiTietBangGiaInput
    {
        /// <summary>
        /// Mã chi tiết bảng giá (Code)
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Tên chi tiết bảng giá (Name)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Mã bảng giá nguồn ETC (PriceListSourceId)
        /// </summary>
        public string? PriceListSourceId { get; set; }

        /// <summary>
        /// Mã loại xe nguồn ETC (VehicleTypeSourceId)
        /// </summary>
        public string? VehicleTypeSourceId { get; set; }

        /// <summary>
        /// Trạng thái hoạt động
        /// </summary>
        public bool? Active { get; set; }
    }
}
