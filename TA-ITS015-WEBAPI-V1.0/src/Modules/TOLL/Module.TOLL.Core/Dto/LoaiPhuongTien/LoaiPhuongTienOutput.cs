using System;

namespace Module.TOLL.Core.Dto.LoaiPhuongTien
{
    /// <summary>
    /// Output hiển thị danh sách loại phương tiện (map theo entity TollMasterData, DataType = VehicleType).
    /// Author: HieuNV
    /// Created date: 08/08/2026
    /// </summary>
    public class LoaiPhuongTienOutput
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
        /// Tên loại phương tiện
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
