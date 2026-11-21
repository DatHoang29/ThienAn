using System;
using System.Collections.Generic;
using System.Text;

namespace Module.CCTV.Controllers.CctvMedia.Dto
{
    public class CctvSnapshotOutput
    {
        public byte[] Data { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }

    /// <summary>
    /// Trả về WSS URL cho FE kết nối WebRTC.
    /// FE dùng: new WebSocket(WssUrl) → RTCPeerConnection.
    /// </summary>
    public class CctvWebRtcStreamOutput
    {
        public string? DeviceId { get; set; }

        /// <summary>cctv: Url là WebSocket proxy; rtsp: Url là RTSP gốc để FE gửi vào relay.</summary>
        public string Source { get; set; } = "cctv";

        /// <summary>URL RTSP gốc của TmsEquipment, dùng khi không hỗ trợ CCTV hoặc kết nối live lỗi.</summary>
        public string? FallbackUrl { get; set; }

        /// <summary>Lý do dùng nguồn dự phòng; không chứa thông tin xác thực hay URL.</summary>
        public string? Reason { get; set; }

        /// <summary>Trạng thái thiết bị VMS đã map sang quy ước ITS.</summary>
        public int? DeviceState { get; set; }

        /// <summary>
        /// Source=cctv: URL Cctv Proxy; Source=rtsp: URL RTSP gốc, không phải URL WebSocket.
        /// Thêm &positionMs=xxx nếu là playback. FE kết nối thẳng, không cần cookie/token NxVMS.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>Live nếu PositionMs null, Playback nếu có giá trị.</summary>
        public string? Mode { get; set; }

        /// <summary>Hạn ticket trong Url (giờ server). Hết hạn thì gọi lại API để lấy URL mới.</summary>
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>Trả về HTTPS URL để FE mở link tải file.</summary>
    public class CctvMediaDownloadOutput
    {
        public string? DeviceId { get; set; }
        /// <summary>URL Cctv Proxy kèm ticket: https://{proxy}/rest/v4/devices/{id}/media?...&download=true&ticket=..</summary>
        public string? DownloadUrl { get; set; }
        /// <summary>Hạn ticket trong DownloadUrl (giờ server).</summary>
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>URL ảnh snapshot qua Cctv Proxy kèm ticket — FE gắn thẳng vào &lt;img src&gt;.</summary>
    public class CctvSnapshotUrlOutput
    {
        public string? DeviceId { get; set; }
        /// <summary>https://{proxy}/rest/v4/devices/{id}/image?size=..&format=..&ticket=..</summary>
        public string? Url { get; set; }
        /// <summary>Hạn ticket trong Url (giờ server). Hết hạn thì gọi lại API để lấy URL mới.</summary>
        public DateTime? ExpiresAt { get; set; }
    }


    /// <summary>Một đoạn footage đã ghi, trả cả Unix ms lẫn DateTime cho FE.</summary>
    public class CctvFootageItemOutput
    {
        public string? DeviceId { get; set; }
        public long StartTimeMs { get; set; }
        public long DurationMs { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class CctvFootageOutput
    {
        public string? DeviceId { get; set; }
        public int Count { get; set; }
        public List<CctvFootageItemOutput> Items { get; set; } = new();
    }
}
