using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Cấu hình rule cảnh báo bảo trì
    /// Author: System
    /// Created date: 10/12/2025
    /// </summary>
    [SugarTable("TmsMaintenanceAlertRule")]
    [SugarIndex("index_{table}_ET", nameof(EquipmentTypeId), OrderByType.Asc)]
    [SugarIndex("index_{table}_A", nameof(IsActive), OrderByType.Asc)]
    public class TmsMaintenanceAlertRule : EntityTenant
    {
        /// <summary>
        /// Tên rule
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length128, ColumnDescription = "Tên rule")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// ID loại thiết bị (FK → TmsEquipmentType, null = tất cả loại)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID loại thiết bị")]
        public string? EquipmentTypeId { get; set; }

        /// <summary>
        /// Hãng sản xuất (null = tất cả hãng)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Hãng sản xuất")]
        public string? Manufacturer { get; set; }

        /// <summary>
        /// Số lần lỗi tối thiểu để trigger cảnh báo
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "Ngưỡng số lần lỗi")]
        public int ErrorThreshold { get; set; } = 3;

        /// <summary>
        /// Trong số ngày (period)
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "Số ngày")]
        public int PeriodDays { get; set; } = 1;

        /// <summary>
        /// Mức độ ưu tiên của cảnh báo: High, Medium, Low
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length16, ColumnDescription = "Mức độ ưu tiên cảnh báo")]
        public string AlertPriority { get; set; } = "Medium";

        /// <summary>
        /// ID hướng dẫn bảo trì gợi ý (FK → TmsMaintenanceGuide)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID guide gợi ý")]
        public string? SuggestedGuideId { get; set; }

        /// <summary>
        /// Trạng thái hoạt động
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "Đang hoạt động")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Ghi chú
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Ghi chú")]
        public string? Remark { get; set; }
    }
}
