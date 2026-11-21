using System;

namespace Module.TOLL.Core.Dto.Lan
{
    /// <summary>
    /// Output hiển thị danh sách làn thu phí (map theo entity TollLane).
    /// Author: HieuNV
    /// Created date: 08/08/2026
    /// </summary>
    public class LanOutput
    {
        /// <summary>
        /// ID bản ghi trong ITS
        /// </summary>
        public string? ID { get; set; }

        /// <summary>
        /// Mã làn thu phí
        /// </summary>
        public string? LaneId { get; set; }

        /// <summary>
        /// Tên làn thu phí
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// Mã trạm thu phí
        /// </summary>
        public string? StationId { get; set; }

        /// <summary>
        /// Hướng vào ra của ETC
        /// </summary>
        public string? Direction { get; set; }

        /// <summary>
        /// Mã làn theo BE ETC
        /// </summary>
        public string? LaneBE { get; set; }

        /// <summary>
        /// Thời điểm ITS đồng bộ bản ghi
        /// </summary>
        public DateTime? LastSyncTime { get; set; }
    }
}
