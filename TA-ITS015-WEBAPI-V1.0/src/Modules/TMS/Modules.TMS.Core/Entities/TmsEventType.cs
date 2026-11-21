using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Entities
{
    [SugarTable("TmsEventType")]
    public class TmsEventType : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string Name { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Event/Incident")]
        public string Type { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Kích thước icon")]
        public string? Size { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength, ColumnDescription = "Cảnh bảo/ Ghi nhận")]
        public string WarningType { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string ModuleId { get; set; }
        [SugarColumn(IsNullable = true, ColumnDescription = "Độ ưu tiên số càng nhỏ càng ưu tiên. Ưu tiên nhất: 0")]
        public int? Priority { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string Remark { get; set; }

        // Add by ThangNN - 27/10/2024
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Url { get; set; }

        /// <summary>
        /// Chế độ phát mặc định cho thiết bị VMS khi sự cố kích hoạt tự động: override (ghi đè), append (nối tiếp).
        /// Nếu NULL, hệ thống sẽ lấy theo cấu hình VmsEventDefault hoặc SysOpConfig.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "Chế độ phát mặc định (override/append)")]
        public string? DefaultPlayMode { get; set; }
    }
}
