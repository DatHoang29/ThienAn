using Shared.DTO.Enums;

namespace Module.CCTV.Controllers.CctvDevices.Dto
{
    // ── Output ────────────────────────────────────────────────────────────────────

    /// <summary>Kết quả sau khi thực hiện đồng bộ NxVMS → DB.</summary>
    public class CctvDeviceSyncOutput
    {
        public int TotalFromVms { get; set; }
        public int TotalInserted { get; set; }
        public int TotalUpdated { get; set; }
        public int TotalSkipped { get; set; }
        public int TotalFailed { get; set; }
        public int TotalDeleted { get; set; }
    }

    /// <summary>
    /// Thông tin thiết bị trả về cho UI (lấy từ DB).
    /// Phân biệt rõ 2 khái niệm trạng thái:
    ///   DeviceState  = chuỗi gốc NxVMS ("Online", "Offline"...)
    ///   DeviceStatus = mã int ITS sau khi map (1=HoatDong, 4=KhongTheKetNoi...)
    ///   Status       = cờ thiết bị có đang được sử dụng không (1=có, 0=không)
    /// </summary>
    public class CctvDeviceOutput
    {
        public string? ID { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Ip { get; set; }
        public int? DeviceState { get; set; }
        public BaseEnums.StatusEnum? Status { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string? SnapshotUrl { get; set; }
        public DateTime? SnapshotTime { get; set; }
    }

    /// <summary>Thông tin tổng hợp ITS + NxVMS trả về cho UI.</summary>
    public class CctvDeviceViewOutput
    {
        public string? ID { get; set; }
        public string? Ip { get; set; }
        public string? Port { get; set; }
        public string? Name { get; set; }
        public string? DeviceId { get; set; }
        public string? Url { get; set; }
        public string? Remark { get; set; }

        /// <summary>ID loại thiết bị (TmsEquipment.EquipmentTypeId).</summary>
        public string? EquipmentTypeId { get; set; }

        /// <summary>Tên loại thiết bị (TmsEquipmentType.Name).</summary>
        public string? EquipmentTypeName { get; set; }

        /// <summary>Trạng thái thiết bị theo ITS (từ TmsEquipment).</summary>
        public int? EquipmentState { get; set; }

        /// <summary>Trạng thái kết nối NxVMS đã map sang int ITS (1=HoatDong/Online, 4=KhongTheKetNoi...).</summary>
        public int? DeviceState { get; set; }

        public DateTime? CreateTime { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public string? SnapshotUrl { get; set; }
        public DateTime? SnapshotTime { get; set; }
    }
}