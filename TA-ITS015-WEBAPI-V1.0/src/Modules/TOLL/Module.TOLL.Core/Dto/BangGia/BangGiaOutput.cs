using System;

namespace Module.TOLL.Core.Dto.BangGia
{
    /// <summary>
    /// Output hiển thị danh sách bảng giá thu phí (map theo entity TollPriceList).
    /// Chi tiết của 1 bảng giá: ChiTietBangGiaOutput với PriceListSourceId = SourceId.
    /// Author: HieuNV
    /// Created date: 07/10/2026
    /// </summary>
    public class BangGiaOutput
    {
        /// <summary>
        /// ID bản ghi trong ITS
        /// </summary>
        public string? ID { get; set; }

        /// <summary>
        /// Mã bảng giá nguồn ETC
        /// </summary>
        public string? SourceId { get; set; }

        /// <summary>
        /// Tên bảng giá
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Ngày bắt đầu hiệu lực
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Ngày kết thúc hiệu lực
        /// </summary>
        public DateTime? EndDate { get; set; }

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
