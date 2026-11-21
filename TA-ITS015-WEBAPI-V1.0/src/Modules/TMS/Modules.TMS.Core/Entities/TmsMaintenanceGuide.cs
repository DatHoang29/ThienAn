using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Hướng dẫn bảo trì duy tu theo loại thiết bị và hãng sản xuất
    /// Author: System
    /// Created date: 10/12/2025
    /// </summary>
    [SugarTable("TmsMaintenanceGuide")]
    [SugarIndex("index_{table}_ET", nameof(EquipmentTypeId), OrderByType.Asc)]
    [SugarIndex("index_{table}_M", nameof(Manufacturer), OrderByType.Asc)]
    [SugarIndex("index_{table}_S", nameof(Status), OrderByType.Asc)]
    public class TmsMaintenanceGuide : EntityTenant
    {
        /// <summary>
        /// ID loại thiết bị (FK → TmsEquipmentType)
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID loại thiết bị")]
        public string EquipmentTypeId { get; set; } = string.Empty;

        /// <summary>
        /// Hãng sản xuất (null = áp dụng cho tất cả hãng của loại thiết bị này)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Hãng sản xuất")]
        public string? Manufacturer { get; set; }

        /// <summary>
        /// Tên hướng dẫn bảo trì
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length128, ColumnDescription = "Tên hướng dẫn")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Mô tả chi tiết
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "text", ColumnDescription = "Mô tả chi tiết")]
        public string? Description { get; set; }

        /// <summary>
        /// Tần suất bảo trì: Daily, Weekly, Monthly, Quarterly, Yearly
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.Length32, ColumnDescription = "Tần suất bảo trì")]
        public string Frequency { get; set; } = "Monthly";

        /// <summary>
        /// Giá trị tần suất (vd: 2 tuần, 3 tháng)
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "Giá trị tần suất")]
        public int FrequencyValue { get; set; } = 1;

        /// <summary>
        /// Thời gian dự kiến thực hiện (phút)
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Thời gian dự kiến (phút)")]
        public int? EstimatedDuration { get; set; }

        /// <summary>
        /// Danh sách các bước thực hiện (JSON array)
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "text", ColumnDescription = "Checklist JSON")]
        public string? Checklist { get; set; }

        /// <summary>
        /// Tài liệu đính kèm (JSON array URLs)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Tài liệu đính kèm")]
        public string? Documents { get; set; }

        /// <summary>
        /// Mức ưu tiên mặc định: High, Medium, Low
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "Mức ưu tiên")]
        public string? Priority { get; set; } = "Medium";

        /// <summary>
        /// Trạng thái: 0 = Inactive, 1 = Active
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Trạng thái")]
        public int? Status { get; set; } = 1;

        /// <summary>
        /// Ghi chú
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Ghi chú")]
        public string? Remark { get; set; }
    }
}
