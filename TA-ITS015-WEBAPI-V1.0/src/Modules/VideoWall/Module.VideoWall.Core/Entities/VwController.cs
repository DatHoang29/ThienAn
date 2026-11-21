using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.VideoWall.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Bộ điều khiển Video Wall (VwController)
    /// Created date: 14/08/2026
    /// </summary>
    [SugarTable("VwController")]
    public class VwController : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Model { get; set; }
        /// <summary>
        /// Description: Địa chỉ IP thiết bị (chỉ IP, cổng ở cột Port). Chép từ thiết bị tham chiếu (TmsEquipment).
        /// Created date: 14/08/2026 — cập nhật 26/10/2026 (tách cổng sang cột Port)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? IP { get; set; }

        /// <summary>
        /// Description: Cổng kết nối ISAPI của thiết bị. Trống ⇒ Worker dùng cổng cấu hình / mặc định (VwDeviceProfile.DefaultPort).
        /// Created date: 26/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? Port { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Account { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? PassWord { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Chassis { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? ActiveSceneId { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? ActiveSceneAt { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ColorHex { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? InputSlotsNumber { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? SlotsNumber { get; set; }

        /// <summary>
        /// Description: Vai trò (VwControllerRole): distribution / display / standalone.
        ///              display bắt buộc có ParentControllerId trỏ tới 1 distribution; distribution / standalone không có cha.
        /// Created date: 07/11/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? Role { get; set; }
        [SugarColumn(IsIgnore = true)]
        public BaseEnums.GenlockConnectEnum? GenlockInConnected { get; set; }
        [SugarColumn(IsIgnore = true)]
        public BaseEnums.GenlockConnectEnum? GenlockOutConnected { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? IntegrationMode { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? MaxWindowNums { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? MaxSceneNums { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? BaseOutputSize { get; set; }
        [SugarColumn(IsNullable = true)]
        public bool? IsSupportScene { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? WallNo { get; set; }
 
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? ParentControllerId { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID thiết bị tham chiếu (TmsEquipment.ID)")]
        public string? RefId { get; set; }
    }
}