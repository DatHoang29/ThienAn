using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Modules.CCTV.Core.Entities
{
    [SugarTable("CctvDevice")]
    public class CctvDevice : EntityTenant
    {
        /// <summary>GUID do NxVMS cấp — định danh chính để gọi API.</summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength)]
        public string DeviceId { get; set; } = null!;

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? Ip { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }

        /// <summary>
        /// WSS URL live stream — được build từ DeviceId, không lưu token.
        /// Ví dụ: wss://host/rest/v4/devices/{id}/webrtc?stream=secondary&resolution=640x360
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? Url { get; set; }

        /// <summary>Trạng thái kết nối NxVMS đã map sang int ITS.</summary>
        [SugarColumn(IsNullable = true)]
        public int? DeviceState { get; set; }

        /// <summary>Cờ nghiệp vụ: 1 = đang sử dụng, 0 = ngừng sử dụng. Do người dùng quản lý.</summary>
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? LastSyncTime { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? SnapshotUrl { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? SnapshotTime { get; set; }
    }
}