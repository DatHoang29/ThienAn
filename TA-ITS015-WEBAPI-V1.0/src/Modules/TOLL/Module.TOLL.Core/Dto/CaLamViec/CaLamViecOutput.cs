using System;

namespace Module.TOLL.Core.Dto.CaLamViec
{
    /// <summary>
    /// Output hiển thị danh sách ca làm việc (map theo entity TollShift).
    /// Author: HieuNV
    /// Created date: 08/08/2026
    /// </summary>
    public class CaLamViecOutput
    {
        /// <summary>
        /// ID bản ghi trong ITS
        /// </summary>
        public string? ID { get; set; }

        /// <summary>
        /// Mã ca làm việc
        /// </summary>
        public string? ShiftId { get; set; }

        /// <summary>
        /// Tên ca làm việc
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Thời gian vào ca
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Thời gian hết ca
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Mã ca theo BE ETC
        /// </summary>
        public string? ShiftBE { get; set; }

        /// <summary>
        /// Thời điểm ITS đồng bộ bản ghi
        /// </summary>
        public DateTime? LastSyncTime { get; set; }
    }
}
