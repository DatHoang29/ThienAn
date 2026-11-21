using System;

namespace Module.TOLL.Core.Dto.LoaiVe
{
    /// <summary>
    /// Output hiển thị danh sách loại vé (map theo entity TollMasterData, DataType = TicketType).
    /// Author: HieuNV
    /// Created date: 13/08/2026
    /// </summary>
    public class LoaiVeOutput
    {
        /// <summary>
        /// ID bản ghi trong ITS
        /// </summary>
        public string? ID { get; set; }

        /// <summary>
        /// Mã gốc theo BE ETC
        /// </summary>
        public string? SourceId { get; set; }

        /// <summary>
        /// Tên loại vé
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Nhóm phân loại theo ETC
        /// </summary>
        public string? GroupCode { get; set; }

        /// <summary>
        /// Mô tả
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// Kích hoạt/ vô hiệu
        /// </summary>
        public int? Active { get; set; }

        /// <summary>
        /// Thời điểm ITS đồng bộ bản ghi
        /// </summary>
        public DateTime? LastSyncTime { get; set; }
    }
}
