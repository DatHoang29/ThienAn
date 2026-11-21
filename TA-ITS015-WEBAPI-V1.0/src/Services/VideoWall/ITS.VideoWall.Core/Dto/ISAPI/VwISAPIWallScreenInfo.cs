using System.Text.Json.Serialization;

namespace ITS.VideoWall.Core.Dto.ISAPI
{
    /// <summary>
    /// Description: GET /ISAPI/DisplayDev/ScreenCtrl/GetWallScreenInfo?format=json — màn LCD đang cắm trên từng ô
    ///              của tường (đo trên slave 10.1.123.18 ngày 06/11/2026: rect bước 1920, serial, nhiệt độ, deviceUpTime giây).
    /// Created date: 06/11/2026
    /// </summary>
    public class VwISAPIWallScreenInfoResponse
    {
        [JsonPropertyName("screenPosList")]
        public List<VwISAPIWallScreenPos> ScreenPosList { get; set; } = [];
    }

    public class VwISAPIWallScreenPos
    {
        [JsonPropertyName("wallID")]
        public int WallId { get; set; }

        [JsonPropertyName("screenInfoList")]
        public List<VwISAPIWallScreenInfo> ScreenInfoList { get; set; } = [];
    }

    public class VwISAPIWallScreenInfo
    {
        [JsonPropertyName("rect")]
        public VwISAPIWallScreenRect? Rect { get; set; }

        [JsonPropertyName("serialNumber")]
        public string? SerialNumber { get; set; }

        [JsonPropertyName("softwareVersion")]
        public string? SoftwareVersion { get; set; }

        [JsonPropertyName("screenID")]
        public string? ScreenId { get; set; }

        [JsonPropertyName("screenType")]
        public string? ScreenType { get; set; }

        /// <summary>Thời gian chạy, đơn vị giây (14400 = 4 giờ).</summary>
        [JsonPropertyName("deviceUpTime")]
        public long? DeviceUpTime { get; set; }

        [JsonPropertyName("temperature")]
        public int? Temperature { get; set; }
    }

    /// <summary>
    /// Description: POST /ISAPI/DisplayDev/ScreenCtrl/screenStatus?format=json (statusType "all", 1 màn) — chỉ lấy phần dùng tới.
    ///              Đo 11/11/2026: backlight 0-100 đọc được; brightness/contrast... luôn 0. Màn đã tháo ⇒ lỗi gettingScreenStatusFailed.
    /// Created date: 11/11/2026
    /// </summary>
    public class VwISAPIScreenStatusResponse
    {
        [JsonPropertyName("ScreenStatus")]
        public VwISAPIScreenStatus? ScreenStatus { get; set; }
    }

    public class VwISAPIScreenStatus
    {
        [JsonPropertyName("switchStatus")]
        public string? SwitchStatus { get; set; }

        [JsonPropertyName("ColorInfo")]
        public VwISAPIScreenColorInfo? ColorInfo { get; set; }
    }

    public class VwISAPIScreenColorInfo
    {
        /// <summary>Đèn nền 0-100 = độ sáng thực tế của màn LCD.</summary>
        [JsonPropertyName("backlight")]
        public int? Backlight { get; set; }
    }

    public class VwISAPIWallScreenRect
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }
}
