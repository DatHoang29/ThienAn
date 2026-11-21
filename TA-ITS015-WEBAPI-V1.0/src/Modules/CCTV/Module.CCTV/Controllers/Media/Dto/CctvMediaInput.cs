using System;
using System.Collections.Generic;
using System.Text;

namespace Module.CCTV.Controllers.CctvMedia.Dto
{
    public class CctvSnapshotInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        /// <summary>
        /// Timestamp ms. Null hoặc -1 = lấy frame mới nhất (live).
        /// Truyền giá trị cụ thể = lấy frame tại thời điểm đó (archive).
        /// </summary>
        public DateTime? FromDate { get; set; }
        /// <summary>Kích thước ảnh. Ví dụ: "1920x1080", "640x360". Null = giữ nguyên.</summary>
        public string? Size { get; set; }
        /// <summary>Định dạng ảnh: jpg (default), png, tif.</summary>
        public string Format { get; set; } = "jpg";
        /// <summary>
        /// Crop ảnh theo tỉ lệ. Format: "x,y,widthxheight" — giá trị trong [0..1].
        /// Ví dụ: "0.25,0.25,0.5x0.5" = lấy phần giữa ảnh.
        /// Null = không crop.
        /// </summary>
        public string? Crop { get; set; }
        /// <summary>Góc xoay. Null = xoay mặc định của camera.</summary>
        public int? Rotation { get; set; }
        public string? Vendor { get; set; }

    }

    public class CctvWebRtcUrlInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        public string Stream { get; set; } = "primary";
        public string Resolution { get; set; } = "1920x1080";
        public DateTime? FromDate { get; set; } // Unix timestamp ms. Null = live, có giá trị = playback
        public string? Vendor { get; set; }

    }


    /// <summary>
    /// FE truyền Ip + thời điểm + độ dài để xuất file.
    /// </summary>
    public class CctvMediaDownloadInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        public string Stream { get; set; } = "primary";
        public string Resolution { get; set; } = "1920x1080";

        /// <summary>Mốc bắt đầu — Unix timestamp ms. Bắt buộc.</summary>
        public DateTime FromDate { get; set; }

        /// <summary>Thời lượng cần xuất — ms. Ví dụ 300000 = 5 phút.</summary>
        public DateTime ToDate { get; set; }

        public string VideoCodec { get; set; } = "h264";
        public string? Vendor { get; set; }

    }

    /// <summary>
    /// FE truyền DeviceId + khoảng thời gian để lấy danh sách footage đã ghi.
    /// </summary>
    public class CctvFootageInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        /// <summary>Mốc bắt đầu (giờ VN). Null = không giới hạn đầu.</summary>
        public DateTime? FromDate { get; set; }
        /// <summary>Mốc kết thúc (giờ VN). Null = không giới hạn cuối.</summary>
        public DateTime? ToDate { get; set; }
        /// <summary>Gộp các chunk ngắn hơn mức này (ms). Null = mặc định hãng.</summary>
        public long? DetailLevelMs { get; set; }
        /// <summary>Loại chunk: recording | motion | analytics. Null = recording.</summary>
        public string? PeriodType { get; set; }
        public string? Vendor { get; set; }
    }

    /// <summary>
    /// FE lấy URL ảnh snapshot qua Cctv Proxy (kèm ticket) để gắn thẳng vào &lt;img src&gt;
    /// — dùng khi xem camera dạng ảnh định kỳ ở mạng public. Author: kien.le - 2026-10-06.
    /// </summary>
    public class CctvSnapshotUrlInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        /// <summary>Null = ảnh mới nhất (live). Có giá trị = ảnh tại thời điểm đó (archive).</summary>
        public DateTime? FromDate { get; set; }
        /// <summary>Kích thước ảnh. Ví dụ: "640x360". Null = giữ nguyên.</summary>
        public string? Size { get; set; }
        /// <summary>Định dạng ảnh: jpg (default), png, tif.</summary>
        public string Format { get; set; } = "jpg";
        public string? Vendor { get; set; }
    }
}
