using System.Xml.Serialization;

namespace ITS.VideoWall.Core.Dto.ISAPI
{
    /// <summary>
    /// Namespace XML mặc định của toàn bộ document ISAPI (khai báo 1 lần ở root qua
    /// xmlns="..."), áp dụng cho MỌI thẻ con không có prefix riêng. Author: Đạt. Created date: 15/08/2026.
    /// </summary>
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
    /// Author: Đạt
    /// Created date: 15/08/2026
    /// </summary>
    [XmlRoot("ResponseStatus", Namespace = ISAPIXml.Ns)]
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIResponseStatus
    {
        [XmlElement("statusCode")]
        public int StatusCode { get; set; }

        [XmlElement("statusString")]
        public string? StatusString { get; set; }

        /// <summary>Đọc trường này để debug — KHÔNG dựa vào statusString (§Phụ lục API_Postman_Videowall.md "Luôn đọc subStatusCode").</summary>
        [XmlElement("subStatusCode")]
        public string? SubStatusCode { get; set; }

        /// <summary>opt — không xuất hiện trong mọi response thật đã đo (D.2/E.2), có thể null.</summary>
        [XmlElement("description")]
        public string? Description { get; set; }

        /// <summary>
        /// ID cửa sổ vừa tạo — CHỈ theo tài liệu vendor mục 9.7.11.4 (Add Window), chưa được xác
        /// nhận trên thiết bị thật (mục "API CHƯA CHẠY" trong API_Postman_Videowall.md liệt kê rõ
        /// POST .../windows là thao tác chưa test). Mặc định 0 ở mọi response khác.
        /// </summary>
        [XmlElement("ID")]
        public int Id { get; set; }

        [XmlIgnore]
        public bool IdSpecified { get; set; }

        /// <summary>statusCode 0 hoặc 1 đều là thành công (đã xác nhận qua response thật D.2/E.2).</summary>
        [XmlIgnore]
        public bool IsSuccess => StatusCode is 0 or 1;
    }

    /// <summary>
    /// Response của GET /ISAPI/Security/userCheck (§9.1.12.1 / Mục A.1 API_Postman_Videowall.md) —
    /// API xác nhận kết nối và đăng nhập Digest vào thiết bị.
    /// </summary>
    [XmlRoot("userCheck", Namespace = ISAPIXml.Ns)]
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIUserCheckResponse
    {
        [XmlElement("statusValue")]
        public int StatusValue { get; set; }

        [XmlElement("statusString")]
        public string? StatusString { get; set; }

        [XmlElement("isRiskPassword")]
        public bool IsRiskPassword { get; set; }

        [XmlElement("isActivated")]
        public bool IsActivated { get; set; }

        [XmlElement("lockStatus")]
        public string? LockStatus { get; set; }

        [XmlElement("unlockTime")]
        public int? UnlockTime { get; set; }

        [XmlElement("retryLoginTime")]
        public int? RetryLoginTime { get; set; }

        [XmlIgnore]
        public bool IsSuccess => StatusValue == 200;
    }

    /// <summary>Response của GET .../scene/isRunning — mục 9.7.7.6 tài liệu vendor (chưa có bản đo thật).</summary>
    [XmlRoot("RunningScene", Namespace = ISAPIXml.Ns)]
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPISceneStatusResponse
    {
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
        [XmlElement("isSupportScene")]
        public bool? IsSupportScene { get; set; }

        [XmlElement("maxWallNums")]
        public int? MaxWallNums { get; set; }

        [XmlElement("maxWindowNums")]
        public int? MaxWindowNums { get; set; }

        /// <summary>Trần số SID khi nằm ở root (mock hoặc firmware cũ).</summary>
        [XmlElement("maxSceneNums")]
        public int? FlatMaxSceneNums { get; set; }

        [XmlElement("baseOutputSize")]
        public int? BaseOutputSize { get; set; }

        [XmlElement("isSupportSceneInfo")]
        public bool? FlatIsSupportSceneInfo { get; set; }

        [XmlElement("isSupportRoam")]
        public bool? IsSupportRoam { get; set; }

        [XmlElement("isSupportPlan")]
        public bool? IsSupportPlan { get; set; }

        /// <summary>
        /// Trên thiết bị thật (DS-C30S-S11), maxSceneNums và isSupportSceneInfo nằm trong SceneCap.
        /// </summary>
        [XmlElement("SceneCap")]
        public VwISAPISceneCap? SceneCap { get; set; }

        [XmlIgnore]
        public int? MaxSceneNums => SceneCap?.MaxSceneNums ?? FlatMaxSceneNums;

        [XmlIgnore]
        public bool? IsSupportSceneInfo => SceneCap?.IsSupportSceneInfo ?? FlatIsSupportSceneInfo;
    }

    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPISceneCap
    {
        [XmlElement("maxSceneNums")]
        public int? MaxSceneNums { get; set; }

        [XmlElement("isSupportSceneInfo")]
        public bool? IsSupportSceneInfo { get; set; }
    }

    /// <summary>GET .../outputs — root XML lặp trực tiếp nhiều &lt;WallOutput&gt;, không bọc thêm lớp nào (§D.3).</summary>
    [XmlRoot("WallOutputList", Namespace = ISAPIXml.Ns)]
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIOutputsResponse
    {
        [XmlElement("WallOutput")]
        public List<VwISAPIOutputItem> WallOutput { get; set; } = new();
    }

    /// <summary>Một dòng output/screen vật lý đã gán lên tường — outputID khớp VwScreen.OutputId.</summary>
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIOutputItem
    {
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("outputID")]
        public int OutputId { get; set; }

        [XmlElement("Rect")]
        public VwISAPIRect? Rect { get; set; }

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
        [XmlElement("Coordinate")]
        public VwISAPICoordinate? Coordinate { get; set; }

        [XmlElement("width")]
        public int Width { get; set; }

        [XmlElement("height")]
        public int Height { get; set; }
    }

    /// <summary>Toạ độ điểm gốc (góc trên-trái) của <see cref="VwISAPIRect"/>.</summary>
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPICoordinate
    {
        [XmlElement("x")]
        public int X { get; set; }

        [XmlElement("y")]
        public int Y { get; set; }
    }

    /// <summary>
    /// Danh sách các kênh tín hiệu đầu vào video (GET /ISAPI/DisplayDev/Video/inputs/channels).
    /// Khớp dữ liệu đo thực tế trên thiết bị DS-C66S (§B.2 / §H.1).
    /// Author: Đạt
    /// Created date: 17/08/2026
    /// </summary>
    [XmlRoot("VideoInputChannelList", Namespace = ISAPIXml.Ns)]
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIInputChannelsResponse
    {
        [XmlElement("VideoInputChannel")]
        public List<VwISAPIInputChannel> VideoInputChannel { get; set; } = new();
    }

    /// <summary>
    /// Chi tiết một kênh đầu vào video (HDMI/DP/DVI) trên board điều khiển.
    /// Lưu ý: thiết bị thật DS-C66S trả tag "portType" và "signalStatus"
    /// (không phải "inputPortType"/"videoInputChannelAccessStatus" theo đặc tả hãng).
    /// Dùng pattern dual-XmlElement để đổ cả 2 tên tag vào cùng 1 property — tương thích
    /// ngược nếu firmware/thiết bị khác vẫn dùng tên cũ.
    /// Author: Đạt
    /// Created date: 17/08/2026
    /// </summary>
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIInputChannel
    {
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

        [XmlElement("name")]
        public string? Name { get; set; }

        /// <summary>Tag cũ theo đặc tả hãng — firmware thực tế dùng "signalStatus".</summary>
        [XmlElement("videoInputChannelAccessStatus")]
        public string? VideoInputChannelAccessStatus { get; set; }

        /// <summary>
        /// Tag thật trên thiết bị DS-C66S. Giá trị quan sát: "signal" (có tín hiệu).
        /// Dùng để tính VwSource.SignalStatus trong VwDeviceHeartbeatService.
        /// </summary>
        [XmlElement("signalStatus")]
        public string? SignalStatus
        {
            get => VideoInputChannelAccessStatus;
            set => VideoInputChannelAccessStatus ??= value;
        }

        [XmlElement("PortInBoard")]
        public VwISAPIPortInBoard? PortInBoard { get; set; }
    }

    /// <summary>
    /// Thông tin vị trí board và cổng vật lý trong khung máy controller.
    /// Author: Đạt
    /// Created date: 17/08/2026
    /// </summary>
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIPortInBoard
    {
        [XmlElement("boardID")]
        public int BoardId { get; set; }

        [XmlElement("portID")]
        public int PortId { get; set; }

        [XmlElement("ipAddress")]
        public string? IpAddress { get; set; }

        [XmlElement("port")]
        public int Port { get; set; }
    }

    /// <summary>
    /// Danh sách các kênh cổng ra video (GET /ISAPI/DisplayDev/Video/outputs/channels - §C.1).
    /// </summary>
    [XmlRoot("VideoOutputChannelList", Namespace = ISAPIXml.Ns)]
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIOutputChannelsResponse
    {
        [XmlElement("VideoOutputChannel")]
        public List<VwISAPIOutputChannel> VideoOutputChannel { get; set; } = [];
    }

    /// <summary>
    /// Thông tin kênh cổng ra video và trạng thái cắm cáp kết nối vật lý.
    /// </summary>
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIOutputChannel
    {
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("portType")]
        public string? PortType { get; set; }

        [XmlElement("name")]
        public string? Name { get; set; }

        [XmlElement("outputPortAccessStatus")]
        public string? OutputPortAccessStatus { get; set; }

        [XmlElement("abnormalReason")]
        public string? AbnormalReason { get; set; }

        [XmlElement("PortInBoard")]
        public VwISAPIPortInBoard? PortInBoard { get; set; }

        [XmlIgnore]
        public bool IsConnected => OutputPortAccessStatus == "normal";
    }
}
