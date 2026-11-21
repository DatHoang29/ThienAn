using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Module.VideoWall.WPF.Api.Direct.Isapi;

/// <summary>
/// GET /ISAPI/DisplayDev/VideoWall — danh sách tường logic của bộ điều khiển.
/// Thiết bị thật (DS-C30S-S11) trả 8 tường, mỗi tường đúng 6 trường:
/// id, name, backgroudColor (sic), streamFailedMode, enabledOverlayLogo, alarmFilterTime.
/// Trường wallBindOutputStatus là opt theo đặc tả hãng (09-api-reference.md) và
/// thiết bị thật KHÔNG trả (0/2113 bản ghi trong LogsAPI/). Xác định bound bằng
/// GetOutputsAsync(wallId): WallOutputList rỗng ⇒ unbound.
/// </summary>
[XmlRoot("VideoWallList", Namespace = ISAPIXml.Ns)]
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIVideoWallList
{
    /// <summary>Danh sách các tường logic VideoWall trong phản hồi.</summary>
    [XmlElement("VideoWall")]
    public List<VwISAPIVideoWallItem> VideoWall { get; set; } = new();
}

/// <summary>
/// Description: Thông tin chi tiết một tường logic VideoWall trong phản hồi ISAPI
/// Created date: 13/09/2026
/// </summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIVideoWallItem
{
    /// <summary>Số hiệu tường (Id/WallNo) trên thiết bị.</summary>
    [XmlElement("id")]
    public int Id { get; set; }

    /// <summary>Tên tường logic hiển thị.</summary>
    [XmlElement("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Lưu ý: firmware viết sai chính tả thiếu chữ 'n' (backgroud thay vì background),
    /// và đặc tả hãng cũng khai đúng 'backgroudColor' — XmlElement phải map đúng cái sai này.
    /// Enum: "black", "blue", "white".
    /// </summary>
    [XmlElement("backgroudColor")]
    public string? BackgroudColor { get; set; }

    /// <summary>Chế độ xử lý khi mất luồng phát (streamFailedMode).</summary>
    [XmlElement("streamFailedMode")]
    public string? StreamFailedMode { get; set; }

    /// <summary>Cờ cho phép hiển thị logo đè lên màn hình.</summary>
    [XmlElement("enabledOverlayLogo")]
    public bool? EnabledOverlayLogo { get; set; }

    /// <summary>Thời gian lọc cảnh báo của tường.</summary>
    [XmlElement("alarmFilterTime")]
    public string? AlarmFilterTime { get; set; }

    /// <summary>
    /// opt theo đặc tả hãng (09-api-reference.md, ro, opt, enum, subType:string).
    /// Thiết bị thật KHÔNG trả trường này (0/2113 bản ghi). Giữ lại để mock và firmware
    /// khác vẫn dùng được. Khi null → xác định bound bằng GetOutputsAsync.
    /// </summary>
    [XmlElement("wallBindOutputStatus")]
    public string? WallBindOutputStatus { get; set; }

    /// <summary>Cho biết tường đã liên kết cổng ra hay chưa.</summary>
    [XmlIgnore]
    public bool IsBound => string.Equals(WallBindOutputStatus, "bound", StringComparison.OrdinalIgnoreCase);

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
    /// <summary>Danh sách các cửa sổ đang mở trên tường.</summary>
    [XmlElement("WallWindow")]
    public List<VwISAPIWallWindowItem> WallWindow { get; set; } = new();
}

/// <summary>
/// Description: Thông tin chi tiết một cửa sổ hiển thị trên tường VideoWall trong phản hồi ISAPI
/// Created date: 13/09/2026
/// </summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIWallWindowItem
{
    /// <summary>ID của cửa sổ trên tường.</summary>
    [XmlElement("id")]
    public int Id { get; set; }

    /// <summary>Chế độ thao tác cửa sổ (wndOperateMode).</summary>
    [XmlElement("wndOperateMode")]
    public string? WndOperateMode { get; set; }

    /// <summary>Vùng chữ nhật hiển thị của cửa sổ (Rect).</summary>
    [XmlElement("Rect")]
    public VwISAPIRect? Rect { get; set; }

    /// <summary>Chỉ đọc trên thiết bị thật — đổi thứ tự lớp phải gọi /top hoặc /bottom.</summary>
    [XmlElement("layerIdx")]
    public int LayerIdx { get; set; }

    /// <summary>Chế độ hiển thị cửa sổ (windowMode).</summary>
    [XmlElement("windowMode")]
    public int WindowMode { get; set; }

    /// <summary>Chế độ hiển thị chi tiết (wndShowMode).</summary>
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
