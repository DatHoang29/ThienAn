using System.Xml.Serialization;

namespace ITS.VideoWall.Core.Dto.ISAPI
{
    /// <summary>
    /// GET /ISAPI/DisplayDev/VideoWall
    /// </summary>
    [XmlRoot("VideoWallList", Namespace = ISAPIXml.Ns)]
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIVideoWallList
    {
        [XmlElement("VideoWall")]
        public List<VwISAPIVideoWallItem> VideoWall { get; set; } = new();
    }

    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIVideoWallItem
    {
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Lưu ý: firmware viết sai chính tả thiếu chữ 'n' (backgroud thay vì background),
        /// và đặc tả hãng cũng khai đúng 'backgroudColor' — XmlElement phải map đúng cái sai này.
        /// Enum: "black", "blue", "white".
        /// </summary>
        [XmlElement("backgroudColor")]
        public string? BackgroudColor { get; set; }

        [XmlElement("streamFailedMode")]
        public string? StreamFailedMode { get; set; }

        [XmlElement("enabledOverlayLogo")]
        public bool? EnabledOverlayLogo { get; set; }

        [XmlElement("alarmFilterTime")]
        public string? AlarmFilterTime { get; set; }

        /// <summary>
        /// opt theo đặc tả hãng (09-api-reference.md, ro, opt, enum, subType:string).
        /// Thiết bị thật KHÔNG trả trường này (0/2113 bản ghi). Giữ lại để mock và firmware
        /// khác vẫn dùng được. Khi null → xác định bound bằng GetOutputsAsync.
        /// </summary>
        [XmlElement("wallBindOutputStatus")]
        public string? WallBindOutputStatus { get; set; }

        /// <summary>
        /// Tín hiệu ưu tiên nếu có: "bound"/"unbound".
        /// Null khi thiết bị không trả wallBindOutputStatus (thiết bị thật hiện trường).
        /// </summary>
        [XmlIgnore]
        public bool? IsBoundByStatus => WallBindOutputStatus switch
        {
            "bound" => true,
            "unbound" => false,
            _ => null
        };
    }

    /// <summary>
    /// GET /ISAPI/DisplayDev/VideoWall/{wallNo}/windows — cửa sổ đang mở trên tường.
    /// Dùng cho hai việc: chụp ảnh trạng thái trước khi ghi (để hoàn tác) và tra VWMWID sau khi
    /// POST /windows, vì firmware không đảm bảo trả ID cửa sổ vừa tạo trong ResponseStatus.
    /// </summary>
    [XmlRoot("WallWindowList", Namespace = ISAPIXml.Ns)]
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIWallWindowList
    {
        [XmlElement("WallWindow")]
        public List<VwISAPIWallWindowItem> WallWindow { get; set; } = new();
    }

    [XmlRoot("WallWindow", Namespace = ISAPIXml.Ns)]
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIWallWindowItem
    {
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("wndOperateMode")]
        public string? WndOperateMode { get; set; }

        [XmlElement("Rect")]
        public VwISAPIRect? Rect { get; set; }

        /// <summary>Chỉ đọc trên thiết bị thật — đổi thứ tự lớp phải gọi /top hoặc /bottom.</summary>
        [XmlElement("layerIdx")]
        public int LayerIdx { get; set; }

        [XmlElement("windowMode")]
        public int WindowMode { get; set; }

        [XmlElement("wndShowMode")]
        public string? WndShowMode { get; set; }

        /// <summary>
        /// Danh sách sub-window bên trong cửa sổ này — thiết bị thật DS-C66S LUÔN trả kèm
        /// (628/628 bản ghi log thật endpoint .../windows). Mang thông tin nguồn đang chiếu:
        /// videoInputChannelID (HDMI/DP đầu vào) hoặc streamingChannelID (stream IP).
        /// Chưa write vào entity — giữ để đối chiếu/hiển thị, không bỏ rơi dữ liệu khi parse.
        /// </summary>
        [XmlArray("SubWindowList")]
        [XmlArrayItem("SubWindow")]
        public List<VwISAPIWallSubWindow>? SubWindowList { get; set; }
    }

    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIWallSubWindow
    {
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("SubWindowParam")]
        public VwISAPIWallSubWindowParam? SubWindowParam { get; set; }
    }

    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIWallSubWindowParam
    {
        /// <summary>Enum quan sát: "video input" (kênh HDMI/DP vật lý), "streaming" (stream IP).</summary>
        [XmlElement("signalMode")]
        public string? SignalMode { get; set; }

        /// <summary>ID kênh đầu vào video (khớp VwISAPIInputChannel.Id khi signalMode="video input").</summary>
        [XmlElement("videoInputChannelID")]
        public int? VideoInputChannelId { get; set; }

        /// <summary>ID kênh stream (khi signalMode="streaming").</summary>
        [XmlElement("streamingChannelID")]
        public int? StreamingChannelId { get; set; }
    }
}

