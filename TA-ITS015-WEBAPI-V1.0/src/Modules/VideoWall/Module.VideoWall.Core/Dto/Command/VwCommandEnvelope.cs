using Module.VideoWall.Core.Constants;

namespace Module.VideoWall.Core.Dto.Command
{
    /// <summary>
    /// Description: Gói tin vỏ bọc (Message Envelope) chuẩn gửi qua NATS cho luồng điều khiển Video Wall
    /// Created date: 10/09/2026
    /// </summary>
    public class VwCommandEnvelope
    {
        /// <summary>Mã định danh duy nhất của gói tin (để lần vết / idempotency)</summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>Loại hành động điều khiển (xem VwCommandActions)</summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>Kiểu gói tin / danh mục lệnh (phục vụ filter / routing)</summary>
        public string? Type { get; set; } = VwPackageType.Control;

        /// <summary>ID bộ điều khiển đích (nếu chỉ định đích danh 1 controller)</summary>
        public string? ControllerId { get; set; }

        /// <summary>ID kịch bản (scene) liên quan tới lệnh</summary>
        public string? SceneId { get; set; }

        /// <summary>Nguồn phát lệnh (mặc định WebAPI)</summary>
        public string Sender { get; set; } = "WebAPI";

        /// <summary>Thời gian phát sinh Unix timestamp (giây)</summary>
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>Dữ liệu chi tiết của lệnh (kiểu payload cụ thể)</summary>
        public object? Payload { get; set; }
    }
}
