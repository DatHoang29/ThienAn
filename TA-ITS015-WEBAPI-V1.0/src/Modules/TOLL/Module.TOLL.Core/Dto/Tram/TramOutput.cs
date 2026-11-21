using System;

namespace Module.TOLL.Core.Dto.Tram
{
    /// <summary>
    /// Output hiển thị danh sách trạm thu phí (map theo entity TollStation).
    /// Author: ChuongLP
    /// Created date: 08/08/2026
    /// </summary>
    public class TramOutput
    {
        /// <summary>
        /// ID bản ghi trong ITS
        /// </summary>
        public string? ID { get; set; }

        /// <summary>
        /// Mã trạm thu phí
        /// </summary>
        public string? StationId { get; set; }

        /// <summary>
        /// Tên trạm thu phí
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// Đơn vị quản lý
        /// </summary>
        public string? Company { get; set; }

        /// <summary>
        /// Loại trạm thu phí
        /// </summary>
        public int? StationType { get; set; }

        /// <summary>
        /// Mã trạm theo BE ETC
        /// </summary>
        public string? StationBE { get; set; }

        /// <summary>
        /// Hệ xử lý của ETC
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Kích hoạt/ vô hiệu
        /// </summary>
        public int? Active { get; set; }

        /// <summary>
        /// Thời điểm ITS đồng bộ bản ghi
        /// </summary>
        public DateTime? LastSyncTime { get; set; }

        /// <summary>
        /// ID đơn vị TMS được gán (TmsWorkUnit.ID)
        /// </summary>
        public string? RefId { get; set; }

        /// <summary>
        /// Mã đơn vị TMS được gán
        /// </summary>
        public string? RefCode { get; set; }

        /// <summary>
        /// Tên đơn vị TMS được gán
        /// </summary>
        public string? RefName { get; set; }

        /// <summary>
        /// Thời gian tạo bản ghi
        /// </summary>
        public DateTime? CreateTime { get; set; }

        /// <summary>
        /// Thời gian cập nhật gần nhất
        /// </summary>
        public DateTime? UpdateTime { get; set; }
    }
}
