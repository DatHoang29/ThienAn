using System.Xml.Serialization;

namespace Module.VideoWall.WPF.Api.Direct.Isapi;

internal static class ISAPIXml
{
    public const string Ns = "http://www.isapi.org/ver20/XMLSchema";
}

/// <summary>
/// Toàn bộ DTO trong file này khớp với response XML ĐO THẬT trên thiết bị
/// Hikvision DS-C66S-H88-CL (IP 10.10.9.236) — xem
/// DocBusinessThienAn/VideoWall/API/API_Postman_Videowall.md. Không dùng JSON: mọi API trong
/// scope hiện tại (trừ decoingDevice/status, ngoài scope) trả về XML mặc định, không cần
/// ?format=json. Envelope là root XML PHẲNG, không bọc thêm 1 lớp như JSON convention.
///
/// LƯU Ý XmlSerializer: [XmlType(Namespace=...)] BẮT BUỘC khai báo trên MỌI class (không chỉ
/// root) vì document dùng 1 namespace mặc định duy nhất cho toàn bộ thẻ con — thiếu ở class
/// lồng nhau sẽ khiến field đó âm thầm deserialize ra giá trị mặc định (0/null) thay vì lỗi rõ.
/// </summary>
[XmlRoot("ResponseStatus", Namespace = ISAPIXml.Ns)]
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIResponseStatus
{
    /// <summary>Mã trạng thái phản hồi từ thiết bị (0 hoặc 1 là thành công).</summary>
    [XmlElement("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>Chuỗi mô tả trạng thái phản hồi.</summary>
    [XmlElement("statusString")]
    public string? StatusString { get; set; }

    /// <summary>Đọc trường này để debug — KHÔNG dựa vào statusString (§Phụ lục API_Postman_Videowall.md "Luôn đọc subStatusCode").</summary>
    [XmlElement("subStatusCode")]
    public string? SubStatusCode { get; set; }

    /// <summary>opt — không xuất hiện trong mọi response thật đã đo (D.2/E.2), có thể null.</summary>
    [XmlElement("description")]
    public string? Description { get; set; }

    /// <summary>Thông báo lỗi chi tiết nếu có.</summary>
    [XmlElement("errorMsg")]
    public string? ErrorMsg { get; set; }

    /// <summary>
    /// ID cửa sổ vừa tạo — CHỈ theo tài liệu vendor mục 9.7.11.4 (Add Window), chưa được xác
    /// nhận trên thiết bị thật (mục "API CHƯA CHẠY" trong API_Postman_Videowall.md liệt kê rõ
    /// POST .../windows là thao tác chưa test). Mặc định 0 ở mọi response khác.
    /// </summary>
    [XmlElement("ID")]
    public int Id { get; set; }

    [XmlElement("id")]
    public int IdLower { get => Id; set => Id = value; }

    /// <summary>Cờ cho biết trường Id có được chỉ định hay không.</summary>
    [XmlIgnore]
    public bool IdSpecified { get; set; }

    /// <summary>statusCode 0 hoặc 1 đều là thành công (đã xác nhận qua response thật D.2/E.2).</summary>
    [XmlIgnore]
    public bool IsSuccess => StatusCode == 0 || StatusCode == 1;
}

/// <summary>
/// Response của GET /ISAPI/Security/userCheck (§9.1.12.1 / Mục A.1 API_Postman_Videowall.md) —
/// API xác nhận kết nối và đăng nhập Digest vào thiết bị.
/// </summary>
[XmlRoot("userCheck", Namespace = ISAPIXml.Ns)]
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIUserCheckResponse
{
    /// <summary>Mã trạng thái kiểm tra người dùng (200 là hợp lệ).</summary>
    [XmlElement("statusValue")]
    public int StatusValue { get; set; }

    /// <summary>Chuỗi trạng thái kiểm tra người dùng.</summary>
    [XmlElement("statusString")]
    public string? StatusString { get; set; }

    /// <summary>Cảnh báo mật khẩu có rủi ro cao.</summary>
    [XmlElement("isRiskPassword")]
    public bool IsRiskPassword { get; set; }

    /// <summary>Trạng thái kích hoạt của thiết bị.</summary>
    [XmlElement("isActivated")]
    public bool IsActivated { get; set; }

    /// <summary>Trạng thái khóa tài khoản trên thiết bị.</summary>
    [XmlElement("lockStatus")]
    public string? LockStatus { get; set; }

    /// <summary>Thời gian còn lại để mở khóa tài khoản (giây).</summary>
    [XmlElement("unlockTime")]
    public int? UnlockTime { get; set; }

    /// <summary>Số lần thử đăng nhập còn lại trước khi bị khóa.</summary>
    [XmlElement("retryLoginTime")]
    public int? RetryLoginTime { get; set; }

    /// <summary>Cờ xác định yêu cầu kiểm tra người dùng thành công (mã trạng thái 200).</summary>
    [XmlIgnore]
    public bool IsSuccess => StatusValue == 200;
}

/// <summary>Response của GET .../scene/isRunning — mục 9.7.7.6 tài liệu vendor (chưa có bản đo thật).</summary>
[XmlRoot("RunningScene", Namespace = ISAPIXml.Ns)]
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPISceneStatusResponse
{
    /// <summary>ID kịch bản hiện đang chạy trên thiết bị (sceneID).</summary>
    [XmlElement("sceneID")]
    public int SceneId { get; set; }
}

/// <summary>
/// Response của GET /ISAPI/DisplayDev/VideoWall/capabilities (§9.7.5.6) — năng lực RIÊNG của
/// video wall (isSupportScene/maxWindowNums...), KHÁC với GET /ISAPI/DisplayDev/capabilities
/// (§9.7.5.1, DisplayCap — năng lực TOÀN THIẾT BỊ, cây khổng lồ không liên quan trực tiếp
/// nghiệp vụ scene/window nên không dùng endpoint đó). Gọi trước Activate/Sync theo đúng quy
/// ước "gọi ĐẦU TIÊN" trong 00-api-catalog.md.
/// </summary>
[XmlRoot("VideoWallCap", Namespace = ISAPIXml.Ns)]
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPICapabilitiesResponse
{
    /// <summary>Thiết bị có hỗ trợ kịch bản (Scene) hay không.</summary>
    [XmlElement("isSupportScene")]
    public bool? IsSupportScene { get; set; }

    /// <summary>Số lượng tường tối đa thiết bị hỗ trợ.</summary>
    [XmlElement("maxWallNums")]
    public int? MaxWallNums { get; set; }

    /// <summary>Số lượng cửa sổ tối đa thiết bị hỗ trợ.</summary>
    [XmlElement("maxWindowNums")]
    public int? MaxWindowNums { get; set; }

    /// <summary>Trần số SID khi nằm ở root (mock hoặc firmware cũ).</summary>
    [XmlElement("maxSceneNums")]
    public int? FlatMaxSceneNums { get; set; }

    /// <summary>Kích thước pixel cơ sở của một ô cổng ra (BaseOutputSize).</summary>
    [XmlElement("baseOutputSize")]
    public int? BaseOutputSize { get; set; }

    /// <summary>Trên thiết bị thật (DS-C30S-S11), maxSceneNums và isSupportSceneInfo nằm trong SceneCap.</summary>
    [XmlElement("isSupportSceneInfo")]
    public bool? FlatIsSupportSceneInfo { get; set; }

    /// <summary>Thiết bị có hỗ trợ tính năng chuyển vùng (Roam) hay không.</summary>
    [XmlElement("isSupportRoam")]
    public bool? IsSupportRoam { get; set; }

    /// <summary>Thiết bị có hỗ trợ kế hoạch trình chiếu (Plan) hay không.</summary>
    [XmlElement("isSupportPlan")]
    public bool? IsSupportPlan { get; set; }

    /// <summary>
    /// Trên thiết bị thật (DS-C30S-S11), maxSceneNums và isSupportSceneInfo nằm trong SceneCap.
    /// </summary>
    [XmlElement("SceneCap")]
    public VwISAPISceneCap? SceneCap { get; set; }

    /// <summary>Số lượng Scene tối đa thiết bị hỗ trợ (ưu tiên đọc từ SceneCap).</summary>
    [XmlIgnore]
    public int? MaxSceneNums => SceneCap?.MaxSceneNums ?? FlatMaxSceneNums;

    /// <summary>Cờ cho biết thiết bị có hỗ trợ thông tin Scene hay không (ưu tiên đọc từ SceneCap).</summary>
    [XmlIgnore]
    public bool? IsSupportSceneInfo => SceneCap?.IsSupportSceneInfo ?? FlatIsSupportSceneInfo;
}

/// <summary>
/// Description: Thông tin năng lực kịch bản (SceneCap) trả về từ ISAPI capabilities
/// Created date: 13/09/2026
/// </summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPISceneCap
{
    /// <summary>Số lượng kịch bản tối đa thiết bị hỗ trợ trong SceneCap.</summary>
    [XmlElement("maxSceneNums")]
    public int? MaxSceneNums { get; set; }

    /// <summary>Thiết bị có hỗ trợ thông tin kịch bản mở rộng hay không.</summary>
    [XmlElement("isSupportSceneInfo")]
    public bool? IsSupportSceneInfo { get; set; }
}

/// <summary>GET .../outputs — root XML lặp trực tiếp nhiều &lt;WallOutput&gt;, không bọc thêm lớp nào (§D.3).</summary>
[XmlRoot("WallOutputList", Namespace = ISAPIXml.Ns)]
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIOutputsResponse
{
    /// <summary>Danh sách các cổng ra vật lý trên tường.</summary>
    [XmlElement("WallOutput")]
    public List<VwISAPIOutputItem> WallOutput { get; set; } = new();
}

/// <summary>Một dòng output/screen vật lý đã gán lên tường — outputID khớp VwScreen.OutputId.</summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIOutputItem
{
    /// <summary>Chỉ số thứ tự cổng ra trên tường.</summary>
    [XmlElement("id")]
    public int Id { get; set; }

    /// <summary>Mã định danh cổng ra vật lý (outputID khớp VwScreen.OutputId).</summary>
    [XmlElement("outputID")]
    public int OutputId { get; set; }

    /// <summary>Vùng chữ nhật tọa độ của cổng ra trên tường.</summary>
    [XmlElement("Rect")]
    public VwISAPIRect? Rect { get; set; }

    /// <summary>Số cửa sổ tối đa cổng ra này hỗ trợ hiển thị.</summary>
    [XmlElement("outputWinNum")]
    public int OutputWinNum { get; set; }

    /// <summary>Giá trị quan sát thật: "uniformCoordinate". Giá trị còn lại theo tài liệu: "resolutionCoordinate".</summary>
    [XmlElement("coordinateMode")]
    public string? CoordinateMode { get; set; }
}

/// <summary>
/// Hình chữ nhật toạ độ ISAPI. QUAN TRỌNG (đã đo thật §D.3): mỗi ô màn luôn VUÔNG
/// baseOutputSize × baseOutputSize (1920×1920) bất kể độ phân giải vật lý thật, và giá trị
/// width/height CÓ THỂ vượt 1920 (tài liệu vendor ghi range:[0,1920] là SAI — đã kiểm chứng
/// height=3840 được chấp nhận và hiển thị đúng, §E.2).
/// </summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIRect
{
    /// <summary>Tọa độ điểm gốc (X, Y) của vùng chữ nhật.</summary>
    [XmlElement("Coordinate")]
    public VwISAPICoordinate? Coordinate { get; set; }

    /// <summary>Chiều rộng vùng chữ nhật (pixel).</summary>
    [XmlElement("width")]
    public int Width { get; set; }

    /// <summary>Chiều cao vùng chữ nhật (pixel).</summary>
    [XmlElement("height")]
    public int Height { get; set; }
}

/// <summary>Toạ độ điểm gốc (góc trên-trái) của <see cref="VwISAPIRect"/>.</summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPICoordinate
{
    /// <summary>Tọa độ X.</summary>
    [XmlElement("x")]
    public int X { get; set; }

    /// <summary>Tọa độ Y.</summary>
    [XmlElement("y")]
    public int Y { get; set; }
}

/// <summary>
/// Danh sách các kênh tín hiệu đầu vào video (GET /ISAPI/DisplayDev/Video/inputs/channels).
/// Khớp dữ liệu đo thực tế trên thiết bị DS-C66S (§B.2 / §H.1).
/// </summary>
[XmlRoot("VideoInputChannelList", Namespace = ISAPIXml.Ns)]
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIInputChannelsResponse
{
    /// <summary>Danh sách các kênh tín hiệu đầu vào video.</summary>
    [XmlElement("VideoInputChannel")]
    public List<VwISAPIInputChannel> VideoInputChannel { get; set; } = new();
}

/// <summary>
/// Chi tiết một kênh đầu vào video (HDMI/DP/DVI) trên board điều khiển.
/// Lưu ý: thiết bị thật DS-C66S trả tag "portType" và "signalStatus"
/// (không phải "inputPortType"/"videoInputChannelAccessStatus" theo đặc tả hãng).
/// Dùng pattern dual-XmlElement để đổ cả 2 tên tag vào cùng 1 property — tương thích
/// ngược nếu firmware/thiết bị khác vẫn dùng tên cũ.
/// </summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIInputChannel
{
    /// <summary>ID kênh đầu vào video (videoInputChannelID).</summary>
    [XmlElement("id")]
    public int Id { get; set; }

    /// <summary>Tag cũ theo đặc tả hãng — firmware thực tế dùng "portType".</summary>
    [XmlElement("inputPortType")]
    public string? InputPortType { get; set; }

    /// <summary>Tag thật trên thiết bị DS-C66S (đo 67 bản ghi log thật §B.2/§H.1).</summary>
    [XmlElement("portType")]
    public string? PortType
    {
        get => InputPortType;
        set => InputPortType ??= value;
    }

    /// <summary>Tên kênh tín hiệu đầu vào.</summary>
    [XmlElement("name")]
    public string? Name { get; set; }

    /// <summary>Tag cũ theo đặc tả hãng — firmware thực tế dùng "signalStatus".</summary>
    [XmlElement("videoInputChannelAccessStatus")]
    public string? VideoInputChannelAccessStatus { get; set; }

    /// <summary>
    /// Tag thật trên thiết bị DS-C66S. Giá trị quan sát: "signal" (có tín hiệu).
    /// </summary>
    [XmlElement("signalStatus")]
    public string? SignalStatus
    {
        get => VideoInputChannelAccessStatus;
        set => VideoInputChannelAccessStatus ??= value;
    }

    /// <summary>Vị trí cổng trên bo mạch phần cứng.</summary>
    [XmlElement("PortInBoard")]
    public VwISAPIPortInBoard? PortInBoard { get; set; }
}

/// <summary>
/// Thông tin vị trí board và cổng vật lý trong khung máy controller.
/// </summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIPortInBoard
{
    /// <summary>ID bo mạch trong khung máy.</summary>
    [XmlElement("boardID")]
    public int BoardId { get; set; }

    /// <summary>ID cổng trên bo mạch.</summary>
    [XmlElement("portID")]
    public int PortId { get; set; }

    /// <summary>Địa chỉ IP của bo mạch mạng.</summary>
    [XmlElement("ipAddress")]
    public string? IpAddress { get; set; }

    /// <summary>Cổng mạng kết nối bo mạch.</summary>
    [XmlElement("port")]
    public int Port { get; set; }
}

/// <summary>
/// Description: Danh sách kịch bản trên một tường (GET .../{wallNo}/scene).
/// Created date: 07/09/2026
/// </summary>
[XmlRoot("WallSceneList", Namespace = ISAPIXml.Ns)]
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIWallSceneList
{
    /// <summary>Danh sách kịch bản trên tường logic.</summary>
    [XmlElement("WallScene")]
    public List<VwISAPIWallScene> WallScene { get; set; } = [];
}

/// <summary>
/// Description: Một kịch bản trong danh sách — chỉ có id và name.
/// Created date: 07/09/2026
/// </summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIWallScene
{
    /// <summary>ID kịch bản trên thiết bị.</summary>
    [XmlElement("id")]
    public int Id { get; set; }

    /// <summary>Tên kịch bản trên thiết bị.</summary>
    [XmlElement("name")]
    public string? Name { get; set; }
}

/// <summary>
/// Description: Danh sách kênh stream IP (GET ISAPI/DisplayDev/Video/streaming/channels).
/// 🔒 BẮT BUỘC: DTO output KHÔNG map password, và KHÔNG log RawResponse của endpoint này.
/// Lưu ý: bản BE cố ý không map password vì lý do bảo mật (API web nhiều người dùng);
/// bản WPF (desktop, dùng nội bộ để dựng URL RTSP xem trước) CÓ giữ property Password —
/// xem VwDirectDeviceConnectionClient.cs, đây là khác biệt có chủ đích, không phải thiếu sót.
/// Created date: 07/09/2026
/// </summary>
[XmlRoot("StreamInputChannelList", Namespace = ISAPIXml.Ns)]
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIStreamingChannelsResponse
{
    /// <summary>Danh sách các kênh phát luồng IP.</summary>
    [XmlElement("StreamInputChannel")]
    public List<VwISAPIStreamInputChannel> StreamInputChannel { get; set; } = [];
}

/// <summary>
/// Description: Kênh phát luồng IP trả về từ ISAPI StreamingChannels
/// Created date: 13/09/2026
/// </summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIStreamInputChannel
{
    /// <summary>ID kênh phát luồng IP.</summary>
    [XmlElement("id")]
    public int Id { get; set; }

    /// <summary>Tên kênh phát luồng IP.</summary>
    [XmlElement("name")]
    public string? Name { get; set; }

    /// <summary>Nhóm phân loại kênh luồng.</summary>
    [XmlElement("group")]
    public string? Group { get; set; }

    /// <summary>Trạng thái bắt đầu giải mã luồng.</summary>
    [XmlElement("startDecoding")]
    public bool StartDecoding { get; set; }

    /// <summary>Chi tiết cấu hình luồng đầu vào.</summary>
    [XmlElement("StreamInput")]
    public VwISAPIStreamInputDetail? StreamInput { get; set; }
}

/// <summary>
/// Description: Chi tiết cấu hình luồng đầu vào ISAPI
/// Created date: 13/09/2026
/// </summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIStreamInputDetail
{
    /// <summary>Chế độ nhập luồng (ví dụ: realtime).</summary>
    [XmlElement("streamInputMode")]
    public string? StreamInputMode { get; set; }

    /// <summary>Chi tiết cấu hình luồng thời gian thực.</summary>
    [XmlElement("StreamInputRealtime")]
    public VwISAPIStreamInputRealtimeDetail? StreamInputRealtime { get; set; }
}

/// <summary>
/// Description: Danh sách đơn vị phát luồng thời gian thực ISAPI
/// Created date: 13/09/2026
/// </summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIStreamInputRealtimeDetail
{
    /// <summary>Danh sách các đơn vị luồng phát thời gian thực.</summary>
    [XmlArray("StreamRealtimeUnitList")]
    [XmlArrayItem("StreamRealtimeUnit")]
    public List<VwISAPIStreamRealtimeUnitDetail> StreamRealtimeUnitList { get; set; } = [];
}

/// <summary>
/// Description: Đơn vị phát luồng thời gian thực ISAPI (URL hoặc tên miền thiết bị)
/// Created date: 13/09/2026
/// </summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIStreamRealtimeUnitDetail
{
    /// <summary>Loại luồng phát (in URL, by domain...).</summary>
    [XmlElement("streamType")]
    public string? StreamType { get; set; }

    /// <summary>Cấu hình luồng thông qua tên miền thiết bị mã hóa.</summary>
    [XmlElement("StreamByDomain")]
    public VwISAPIStreamByDomain? StreamByDomain { get; set; }

    /// <summary>Cấu hình luồng thông qua URL trực tiếp.</summary>
    [XmlElement("StreamInURL")]
    public VwISAPIStreamInUrlDetail? StreamInUrl { get; set; }
}

/// <summary>
/// Description: Thông tin luồng giải mã theo tên miền thiết bị
/// Created date: 13/09/2026
/// </summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIStreamByDomain
{
    /// <summary>Thông tin thiết bị mã hóa nguồn stream.</summary>
    [XmlElement("EncodeDevInfo")]
    public VwISAPIEncodeDevInfo? EncodeDevInfo { get; set; }
}

/// <summary>
/// Description: Thông tin thiết bị mã hoá nguồn stream.
/// Lưu ý bảo mật: bản WPF giữ property Password để phục vụ preview RTSP cục bộ trên máy kỹ thuật viên.
/// </summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIEncodeDevInfo
{
    /// <summary>Tên miền hoặc IP của thiết bị mã hóa.</summary>
    [XmlElement("domain")]
    public string? Domain { get; set; }

    /// <summary>Cổng dịch vụ của thiết bị mã hóa.</summary>
    [XmlElement("port")]
    public int Port { get; set; }

    /// <summary>Giao thức truyền dẫn (TCP, UDP...).</summary>
    [XmlElement("transmitProtocol")]
    public string? TransmitProtocol { get; set; }

    /// <summary>Giao thức kết nối thiết bị.</summary>
    [XmlElement("protocol")]
    public string? Protocol { get; set; }

    /// <summary>Tài khoản xác thực thiết bị mã hóa.</summary>
    [XmlElement("username")]
    public string? Username { get; set; }

    /// <summary>Mật khẩu xác thực thiết bị mã hóa.</summary>
    [XmlElement("password")]
    public string? Password { get; set; }

    /// <summary>Chế độ kênh của thiết bị mã hóa.</summary>
    [XmlElement("channelMode")]
    public string? ChannelMode { get; set; }

    /// <summary>Loại kênh của thiết bị mã hóa.</summary>
    [XmlElement("channelType")]
    public string? ChannelType { get; set; }

    /// <summary>Kênh chuẩn bình thường của thiết bị.</summary>
    [XmlElement("channelNormal")]
    public int ChannelNormal { get; set; }
}

/// <summary>
/// Description: Chi tiết URL luồng phát trực tiếp ISAPI
/// Created date: 13/09/2026
/// </summary>
[XmlType(Namespace = ISAPIXml.Ns)]
public class VwISAPIStreamInUrlDetail
{
    /// <summary>Đường dẫn URL luồng video phát trực tiếp.</summary>
    [XmlElement("URL")]
    public string? Url { get; set; }
}
