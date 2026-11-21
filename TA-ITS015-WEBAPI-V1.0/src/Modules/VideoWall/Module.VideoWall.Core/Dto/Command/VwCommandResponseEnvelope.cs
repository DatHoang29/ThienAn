using Module.VideoWall.Core.Constants;

namespace Module.VideoWall.Core.Dto.Command
{
    /// <summary>
    /// Description: Gói tin phản hồi telemetry / event chuẩn qua NATS Data cho luồng điều khiển Video Wall (Worker -> FE trực tiếp)
    /// Created date: 12/09/2026
    /// </summary>
    public class VwCommandResponseEnvelope
    {
        /// <summary>Mã định danh của lệnh gốc (để tương quan / correlate)</summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>Loại hành động điều khiển (VwCommandActions)</summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>Loại sự kiện để FE phân biệt (SceneActivated / DeviceProbeCompleted / HardwareOutOfSync / ...)</summary>
        public string? EventType { get; set; }

        /// <summary>Kết quả thực thi (true: thành công, false: thất bại)</summary>
        public bool Success { get; set; }

        /// <summary>Thông báo lỗi chi tiết (nếu Success = false)</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Dữ liệu phản hồi kèm theo (nếu có)</summary>
        public object? Data { get; set; }

        /// <summary>Thời gian phản hồi Unix timestamp (giây)</summary>
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>Kiểu gói tin</summary>
        public string PackageType { get; set; } = VwPackageType.ControlResponse;
    }
}
