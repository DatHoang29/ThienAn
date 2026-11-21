using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Cấu hình logic hiển thị VMS/LCS theo loại sự kiện sự cố.
    ///              Mỗi rule quy định: với 1 loại sự kiện (EventTypeId) + 1 loại thiết bị (EquipmentTypeId)
    ///              thì hiển thị TRƯỚC (Before) hay SAU (After) vị trí sự cố.
    ///              ProcessType lưu chuỗi const tiếng Anh: xem EquipmentEventRuleConst.ProcessType.
    /// Created date: 16/07/2026
    /// </summary>
    [SugarTable("TmsEquipmentEventRule", TableDescription = "Cấu hình hiển thị VMS/LCS theo loại sự kiện (trước/sau sự cố)")]
    [SugarIndex("index_{table}_Ev", nameof(EventTypeId), OrderByType.Asc)]
    [SugarIndex("index_{table}_Eq", nameof(EquipmentTypeId), OrderByType.Asc)]
    public class TmsEquipmentEventRule : EntityTenant
    {
        /// <summary>
        /// Khóa liên kết loại sự kiện sự cố (TmsEventType.ID)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Mã loại sự kiện sự cố (TmsEventType)")]
        public string? EventTypeId { get; set; }

        /// <summary>
        /// Khóa liên kết loại thiết bị VMS hoặc LCS (TmsEquipmentType.ID)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "Mã loại thiết bị VMS hoặc LCS (TmsEquipmentType)")]
        public string? EquipmentTypeId { get; set; }

        /// <summary>
        /// Loại xử lý hiển thị: "Before" (trước sự cố) hoặc "After" (sau sự cố).
        /// Xem hằng số EquipmentEventRuleConst.ProcessType.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Loại xử lý: Before (trước sự cố) / After (sau sự cố)")]
        public string? ProcessType { get; set; }

        /// <summary>
        /// [NGỪNG DÙNG - 19/08/2026] Phạm vi (mét) gợi ý thiết bị.
        ///
        /// Phạm vi gợi ý đã chốt lấy từ SysOpConfig (nhóm operation_config):
        ///   incident_equipment_suggestion_small_range (tích sẵn) và
        ///   incident_equipment_suggestion_large_range (chỉ gợi ý).
        /// Xem EquipmentSuggestionConst.OpConfig. Trường này KHÔNG còn được đọc ở bất kỳ nghiệp vụ nào
        /// và cũng không có UI cấu hình.
        ///
        /// Property được giữ lại để insert không vi phạm ràng buộc NOT NULL của cột cũ trong DB.
        /// Muốn xóa hẳn: chạy Infrastructure/Migrations/20260819_EquipmentEventRule_Drop_SuggestionRangeMeters.sql
        /// rồi xóa property này.
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "4000", ColumnDescription = "[Ngừng dùng] Phạm vi (mét) gợi ý/chọn thiết bị tự động")]
        public int SuggestionRangeMeters { get; set; } = 4000;

        /// <summary>
        /// Số thiết bị tối đa được chọn tự động cho một action. Null = không giới hạn.
        /// Luồng tự động giữ N thiết bị GẦN vị trí sự cố nhất trong số thiết bị đã được khuyến nghị.
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Số thiết bị tối đa chọn tự động, null = không giới hạn")]
        public int? MaxTargetCount { get; set; }

        /// <summary>
        /// Trạng thái: 0 không hoạt động; 1 hoạt động
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "0: không hoạt động; 1: hoạt động")]
        public BaseEnums.StatusEnum? Status { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength, ColumnDescription = "Ghi chú")]
        public string? Remark { get; set; }
    }
}
