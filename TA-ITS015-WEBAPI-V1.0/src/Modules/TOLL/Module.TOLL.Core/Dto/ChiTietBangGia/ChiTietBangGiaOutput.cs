using System;

namespace Module.TOLL.Core.Dto.ChiTietBangGia
{
    /// <summary>
    /// Output hiển thị danh sách chi tiết bảng giá thu phí (map theo entity TollPriceListDetail).
    /// Author: HieuNV
    /// Created date: 08/08/2026
    /// </summary>
    public class ChiTietBangGiaOutput
    {
        /// <summary>
        /// ID bản ghi trong ITS
        /// </summary>
        public string? ID { get; set; }

        /// <summary>
        /// Mã chi tiết bảng giá nguồn ETC
        /// </summary>
        public string? SourceId { get; set; }

        /// <summary>
        /// Mã chi tiết bảng giá
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Tên chi tiết bảng giá
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Mức giá
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Mã loại xe nguồn ETC
        /// </summary>
        public string? VehicleTypeSourceId { get; set; }

        /// <summary>
        /// Mã loại vé nguồn ETC
        /// </summary>
        public string? TicketTypeSourceId { get; set; }

        /// <summary>
        /// Mã nhóm giá nguồn ETC
        /// </summary>
        public string? PriceTypeSourceId { get; set; }

        /// <summary>
        /// Mã bảng giá nguồn ETC
        /// </summary>
        public string? PriceListSourceId { get; set; }

        /// <summary>
        /// Mã tuyến đường nguồn ETC
        /// </summary>
        public string? RoadSourceId { get; set; }

        /// <summary>
        /// Trạng thái hoạt động
        /// </summary>
        public bool? Active { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Thời điểm ITS đồng bộ bản ghi
        /// </summary>
        public DateTime? SyncTime { get; set; }
    }
}
