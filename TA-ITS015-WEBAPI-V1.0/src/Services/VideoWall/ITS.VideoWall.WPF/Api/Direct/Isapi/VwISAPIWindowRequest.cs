using System.Collections.Generic;
using System.Xml.Serialization;

namespace Module.VideoWall.WPF.Api.Direct.Isapi;

/// <summary>
/// Description: Hằng số kỹ thuật kết nối trực tiếp và toạ độ thiết bị VideoWall ISAPI
/// Created date: 13/09/2026
/// </summary>
public static class VwDirectDeviceConstants
{
    public const string WndOperateMode = "uniformCoordinate";
    public const string SignalMode = "video input";
    public const string SignalModeStreamSetting = "stream setting";
    public const string SignalModeStreamId = "stream id";
    public const string XmlNamespace = "http://www.isapi.org/ver20/XMLSchema";
    public const string DigestNonceCount = "00000001";
    public const string Scheme = "http";
    public const int DefaultPort = 80;
    public const int PanelWidthPx = 3840;
    public const int PanelHeightPx = 2160;
    public const int BaseOutputSize = 1920;
    public const int UniformTileSize = 1920;

    /// <summary>
    /// Description: Lấy kích thước ô cơ sở (tile) hiệu dụng từ BaseOutputSize thiết bị, fallback về UniformTileSize khi &lt;= 0.
    /// Created date: 08/09/2026
    /// </summary>
    public static int EffectiveTile(int? deviceBaseOutputSize) =>
        deviceBaseOutputSize is > 0 ? deviceBaseOutputSize.Value : UniformTileSize;
}

/// <summary>
/// Description: Mô hình cấu trúc yêu cầu XML cửa sổ WallWindow ISAPI gửi tới thiết bị
/// Created date: 13/09/2026
/// </summary>
[XmlRoot("WallWindow", Namespace = VwDirectDeviceConstants.XmlNamespace)]
[XmlType(Namespace = VwDirectDeviceConstants.XmlNamespace)]
public class VwISAPIWindowRequest
{
    /// <summary>ID của cửa sổ trên tường (VWMWID).</summary>
    [XmlElement("id")]
    public int Id { get; set; }

    /// <summary>Cờ chỉ định có serialize trường Id hay không.</summary>
    [XmlIgnore]
    public bool IdSpecified { get; set; }

    /// <summary>Chế độ tọa độ thao tác cửa sổ (mặc định uniformCoordinate).</summary>
    [XmlElement("wndOperateMode")]
    public string WndOperateMode { get; set; } = VwDirectDeviceConstants.WndOperateMode;

    /// <summary>Hình chữ nhật tọa độ và kích thước của cửa sổ trên tường.</summary>
    [XmlElement("Rect")]
    public VwISAPIRect? Rect { get; set; }

    /// <summary>Chế độ cửa sổ (WindowMode).</summary>
    [XmlElement("windowMode")]
    public int WindowMode { get; set; }

    /// <summary>Cờ chỉ định có serialize trường WindowMode hay không.</summary>
    [XmlIgnore]
    public bool WindowModeSpecified { get; set; }

    /// <summary>Chế độ hiển thị cửa sổ (wndShowMode).</summary>
    [XmlElement("wndShowMode")]
    public string? WndShowMode { get; set; }

    /// <summary>
    /// Danh sách sub-window bên trong cửa sổ này — thiết bị thật DS-C66S LUÔN trả kèm
    /// (628/628 bản ghi log thật endpoint .../windows). Mang thông tin nguồn đang chiếu:
    /// videoInputChannelID (HDMI/DP đầu vào) hoặc streamingChannelID (stream IP).
    /// Chưa write vào entity — giữ để đối chiếu/hiển thị, không bỏ rơi dữ liệu khi parse.
    /// </summary>
    [XmlElement("SubWindowList")]
    public VwISAPISubWindowList? SubWindowList { get; set; }
}

/// <summary>
/// Description: Danh sách sub-window con của cửa sổ WallWindow trong request ISAPI
/// Created date: 13/09/2026
/// </summary>
[XmlType(Namespace = VwDirectDeviceConstants.XmlNamespace)]
public class VwISAPISubWindowList
{
    /// <summary>Danh sách các sub-window con trong cửa sổ.</summary>
    [XmlElement("SubWindow")]
    public List<VwISAPISubWindow> SubWindow { get; set; } = new();
}

/// <summary>
/// Description: Thông tin một sub-window con trong request WallWindow ISAPI
/// Created date: 13/09/2026
/// </summary>
[XmlRoot("SubWindow", Namespace = VwDirectDeviceConstants.XmlNamespace)]
[XmlType(Namespace = VwDirectDeviceConstants.XmlNamespace)]
public class VwISAPISubWindow
{
    /// <summary>ID của sub-window.</summary>
    [XmlElement("id")]
    public int Id { get; set; }

    /// <summary>Tham số cấu hình nguồn phát của sub-window.</summary>
    [XmlElement("SubWindowParam")]
    public VwISAPISubWindowParam? SubWindowParam { get; set; }
}

/// <summary>
/// Description: Tham số cấu hình nguồn tín hiệu của sub-window trong request ISAPI
/// Created date: 13/09/2026
/// </summary>
[XmlType(Namespace = VwDirectDeviceConstants.XmlNamespace)]
public class VwISAPISubWindowParam
{
    /// <summary>Enum quan sát: "video input" (kênh HDMI/DP vật lý), "streaming" (stream IP).</summary>
    [XmlElement("signalMode")]
    public string SignalMode { get; set; } = VwDirectDeviceConstants.SignalMode;

    /// <summary>ID kênh đầu vào video (khớp VwISAPIInputChannel.Id khi signalMode="video input").</summary>
    [XmlElement("videoInputChannelID")]
    public string? VideoInputChannelId { get; set; }

    /// <summary>ID kênh stream (khi signalMode="streaming").</summary>
    [XmlElement("streamingChannelID")]
    public string? StreamingChannelId { get; set; }

    /// <summary>Cấu hình luồng IP stream trực tiếp.</summary>
    [XmlElement("StreamInput")]
    public VwISAPIStreamInput? StreamInput { get; set; }
}

/// <summary>
/// Description: Cấu hình luồng stream đầu vào cho sub-window trong request ISAPI
/// Created date: 13/09/2026
/// </summary>
[XmlType(Namespace = VwDirectDeviceConstants.XmlNamespace)]
public class VwISAPIStreamInput
{
    /// <summary>Chế độ nhập luồng (mặc định realtime).</summary>
    [XmlElement("streamInputMode")]
    public string StreamInputMode { get; set; } = "realtime";

    /// <summary>Cấu hình luồng thời gian thực.</summary>
    [XmlElement("StreamInputRealtime")]
    public VwISAPIStreamInputRealtime StreamInputRealtime { get; set; } = new();
}

/// <summary>
/// Description: Chi tiết đơn vị luồng phát thời gian thực trong request ISAPI
/// Created date: 13/09/2026
/// </summary>
[XmlType(Namespace = VwDirectDeviceConstants.XmlNamespace)]
public class VwISAPIStreamInputRealtime
{
    /// <summary>Thời lượng đơn vị phát luồng.</summary>
    [XmlElement("durationInUnit")]
    public int DurationInUnit { get; set; } = 1;

    /// <summary>Danh sách đơn vị cấu hình luồng thời gian thực.</summary>
    [XmlArray("StreamRealtimeUnitList")]
    [XmlArrayItem("StreamRealtimeUnit")]
    public List<VwISAPIStreamRealtimeUnit> StreamRealtimeUnitList { get; set; } = new();
}

/// <summary>
/// Description: Đơn vị luồng phát thời gian thực chứa URL trong request ISAPI
/// Created date: 13/09/2026
/// </summary>
[XmlType(Namespace = VwDirectDeviceConstants.XmlNamespace)]
public class VwISAPIStreamRealtimeUnit
{
    /// <summary>Loại luồng (mặc định in URL).</summary>
    [XmlElement("streamType")]
    public string StreamType { get; set; } = "in URL";

    /// <summary>Thông tin URL luồng phát.</summary>
    [XmlElement("StreamInURL")]
    public VwISAPIStreamInUrl StreamInUrl { get; set; } = new();
}

/// <summary>
/// Description: Thông tin chi tiết địa chỉ URL luồng phát video trong request ISAPI
/// Created date: 13/09/2026
/// </summary>
[XmlType(Namespace = VwDirectDeviceConstants.XmlNamespace)]
public class VwISAPIStreamInUrl
{
    /// <summary>Đường dẫn URL luồng video (RTSP/HTTP...).</summary>
    [XmlElement("URL")]
    public string Url { get; set; } = string.Empty;
}
