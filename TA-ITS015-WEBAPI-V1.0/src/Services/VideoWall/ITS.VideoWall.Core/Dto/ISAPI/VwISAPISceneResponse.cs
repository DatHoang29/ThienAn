using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace ITS.VideoWall.Core.Dto.ISAPI
{
    // ═══════════════════════════════════════════════════════════════════
    // #9  GET .../scene → VwISAPIWallSceneList
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Description: Danh sách kịch bản trên một tường (GET .../{wallNo}/scene).
    /// Created date: 07/09/2026
    /// </summary>
    [XmlRoot("WallSceneList", Namespace = ISAPIXml.Ns)]
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIWallSceneList
    {
        [XmlElement("WallScene")]
        public List<VwISAPIWallSceneItem> WallScene { get; set; } = [];
    }

    /// <summary>
    /// Description: Một kịch bản trong danh sách — chỉ có id và name.
    /// Created date: 07/09/2026
    /// </summary>
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIWallSceneItem
    {
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("name")]
        public string? Name { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // #12 POST .../scene  +  #13 PUT .../scene/{sid}  → VwISAPIWallSceneRequest
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Description: Body gửi lên POST/PUT .../{wallNo}/scene để tạo/đổi tên kịch bản.
    /// POST (tạo): KHÔNG gửi id, response trả SID mới ở ResponseStatus.ID.
    /// PUT (đổi tên): CÓ id, phải khớp {sid} trên URL.
    /// Tên kịch bản chỉ nhận ký tự ASCII ([\x20-\x7E]), có dấu → badParameters.
    /// Created date: 07/09/2026
    /// </summary>
    [XmlRoot("WallScene", Namespace = ISAPIXml.Ns)]
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIWallSceneRequest
    {
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlIgnore]
        public bool IdSpecified { get; set; }

        [XmlElement("name")]
        public string? Name { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // #11 GET .../scene/{sid}/sceneInfo?format=json → VwISAPISceneInfoResponse
    //     Endpoint duy nhất trả JSON, không phải XML.
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Description: Layout kịch bản trả JSON (GET .../{wallNo}/scene/{sid}/sceneInfo?format=json).
    /// Endpoint duy nhất trả JSON ⇒ bypass XML codec, deserialize bằng System.Text.Json.
    /// Created date: 07/09/2026
    /// </summary>
    public class VwISAPISceneInfoResponse
    {
        [JsonPropertyName("wallSceneInfo")]
        public VwISAPIWallSceneInfo? WallSceneInfo { get; set; }
    }

    public class VwISAPIWallSceneInfo
    {
        [JsonPropertyName("wallRect")]
        public VwISAPIWallRectJson? WallRect { get; set; }

        [JsonPropertyName("SceneWindowList")]
        public List<VwISAPISceneWindowWrapper>? SceneWindowList { get; set; }
    }

    public class VwISAPIWallRectJson
    {
        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    public class VwISAPISceneWindowWrapper
    {
        [JsonPropertyName("sceneWindow")]
        public VwISAPISceneWindowItem? SceneWindow { get; set; }
    }

    public class VwISAPISceneWindowItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("wndOperateMode")]
        public string? WndOperateMode { get; set; }

        [JsonPropertyName("Rect")]
        public VwISAPIRectJson? Rect { get; set; }

        [JsonPropertyName("layerIdx")]
        public int LayerIdx { get; set; }
    }

    public class VwISAPIRectJson
    {
        [JsonPropertyName("Coordinate")]
        public VwISAPICoordinateJson? Coordinate { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    public class VwISAPICoordinateJson
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // #7  GET .../streaming/channels → VwISAPIStreamChannelList
    //     🔒 KHÔNG map password — xem prompt mục 1.4 #7.
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Description: Danh sách kênh stream IP (GET ISAPI/DisplayDev/Video/streaming/channels).
    /// 🔒 BẮT BUỘC: DTO output KHÔNG map password, và KHÔNG log RawResponse của endpoint này.
    /// Created date: 07/09/2026
    /// </summary>
    [XmlRoot("StreamInputChannelList", Namespace = ISAPIXml.Ns)]
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIStreamChannelList
    {
        [XmlElement("StreamInputChannel")]
        public List<VwISAPIStreamChannelItem> StreamInputChannel { get; set; } = [];
    }

    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIStreamChannelItem
    {
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("name")]
        public string? Name { get; set; }

        [XmlElement("group")]
        public string? Group { get; set; }

        [XmlElement("startDecoding")]
        public bool? StartDecoding { get; set; }

        [XmlElement("StreamInput")]
        public VwISAPIStreamInput? StreamInput { get; set; }
    }

    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIStreamInput
    {
        [XmlElement("streamInputMode")]
        public string? StreamInputMode { get; set; }

        [XmlElement("StreamInputRealtime")]
        public VwISAPIStreamInputRealtime? StreamInputRealtime { get; set; }
    }

    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIStreamInputRealtime
    {
        [XmlElement("StreamRealtimeUnitList")]
        public VwISAPIStreamRealtimeUnitList? StreamRealtimeUnitList { get; set; }
    }

    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIStreamRealtimeUnitList
    {
        [XmlElement("StreamRealtimeUnit")]
        public List<VwISAPIStreamRealtimeUnit> StreamRealtimeUnit { get; set; } = [];
    }

    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIStreamRealtimeUnit
    {
        [XmlElement("streamType")]
        public string? StreamType { get; set; }

        [XmlElement("StreamByDomain")]
        public VwISAPIStreamByDomain? StreamByDomain { get; set; }
    }

    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIStreamByDomain
    {
        [XmlElement("EncodeDevInfo")]
        public VwISAPIEncodeDevInfo? EncodeDevInfo { get; set; }
    }

    /// <summary>
    /// Description: Thông tin thiết bị mã hoá nguồn stream.
    /// 🔒 CỐ Ý KHÔNG có property password/Password — trả credential về client là lỗ hổng.
    /// Created date: 07/09/2026
    /// </summary>
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIEncodeDevInfo
    {
        [XmlElement("domain")]
        public string? Domain { get; set; }

        [XmlElement("port")]
        public int Port { get; set; }

        [XmlElement("transmitProtocol")]
        public string? TransmitProtocol { get; set; }

        [XmlElement("protocol")]
        public string? Protocol { get; set; }

        // 🔒 username giữ lại để đối chiếu kênh, KHÔNG map password
        [XmlElement("username")]
        public string? Username { get; set; }

        [XmlElement("channelMode")]
        public string? ChannelMode { get; set; }

        [XmlElement("channelType")]
        public string? ChannelType { get; set; }

        [XmlElement("channelNormal")]
        public int ChannelNormal { get; set; }
    }
}
