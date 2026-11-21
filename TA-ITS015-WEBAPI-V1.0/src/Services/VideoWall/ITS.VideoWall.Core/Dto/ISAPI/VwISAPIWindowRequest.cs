using System.Xml.Serialization;
using ITS.VideoWall.Core.Constants;

namespace ITS.VideoWall.Core.Dto.ISAPI
{

    /// <summary>
    /// Body gửi lên POST/PUT .../windows để tạo/cập nhật 1 cửa sổ trên video wall — khớp cấu trúc
    /// XML đã đo thật (§E.1/E.2 API_Postman_Videowall.md). QUY TẮC BẮT BUỘC khi build request
    /// (cảnh báo "quan trọng nhất toàn bộ tài liệu"): KHÔNG BAO GIỜ gửi lại nguyên response GET —
    /// chỉ gửi field thật sự muốn đổi, để trống các field khác. Gửi phần tử rỗng (ví dụ
    /// wndOperateMode để trống) hoặc field chỉ-đọc (layerIdx, videoInputChannelID tính toán) sẽ bị
    /// thiết bị từ chối với lỗi badParameters. X/Y/W/H nhận vào Rect là toạ độ CỤC BỘ trong canvas
    /// của controller đích (đã trừ điểm gốc vùng panel sở hữu) — việc quy đổi từ toạ độ tuyệt đối
    /// toàn tường của VwWindowScene sang toạ độ cục bộ này thuộc trách nhiệm bên gọi
    /// (VwWindowSceneCommandHandler), KHÔNG xử lý ở đây.
    /// Author: Đạt
    /// Created date: 15/08/2026
    /// </summary>
    [XmlRoot("WallWindow", Namespace = ISAPIXml.Ns)]
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPIWindowRequest
    {
        /// <summary>Chỉ gửi khi Update (PUT) — Add (POST) không set field này.</summary>
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlIgnore]
        public bool IdSpecified { get; set; }

        /// <summary>
        /// BẮT BUỘC gửi — quyết định Rect được hiểu theo hệ toạ độ nào. Bỏ trống sẽ bị thiết bị
        /// từ chối với lỗi badParameters (đã xác nhận qua §E.2). Giá trị dùng: "uniformCoordinate".
        /// </summary>
        [XmlElement("wndOperateMode")]
        public string WndOperateMode { get; set; } = VwDeviceProfile.WndOperateMode;

        [XmlElement("Rect")]
        public VwISAPIRect Rect { get; set; } = new();

        // Cố ý KHÔNG có property LayerIdx: field này read-only trên thiết bị thật (đã kiểm chứng
        // §E.1/E.2) — đổi z-order phải gọi endpoint riêng /top hoặc /bottom, set qua PUT vô tác dụng.

        /// <summary>Số ô chia trong window — hợp lệ: 1, 4, 9, 16. Chỉ gửi khi thay đổi.</summary>
        [XmlElement("windowMode")]
        public int WindowMode { get; set; }

        [XmlIgnore]
        public bool WindowModeSpecified { get; set; }

        /// <summary>Giá trị quan sát: "subWndMode". Giá trị khác theo tài liệu: "fullScreenMode".</summary>
        [XmlElement("wndShowMode")]
        public string? WndShowMode { get; set; }

        [XmlElement("SubWindowList")]
        public VwISAPISubWindowList? SubWindowList { get; set; }
    }

    /// <summary>SubWindowList bọc thêm 1 lớp quanh mảng SubWindow theo đúng cấu trúc XML gốc (§E.1).</summary>
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPISubWindowList
    {
        [XmlElement("SubWindow")]
        public List<VwISAPISubWindow> SubWindow { get; set; } = new();
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Đối tượng SubWindow biểu diễn 1 ô chia nguồn tín hiệu trong cửa sổ (dùng cả trong WallWindow và gọi PUT .../sub/{id}).
    /// Created date: 15/08/2026
    /// </summary>
    [XmlRoot("SubWindow", Namespace = ISAPIXml.Ns)]
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPISubWindow
    {
        [XmlElement("id")]
        public int Id { get; set; }

        [XmlElement("SubWindowParam")]
        public VwISAPISubWindowParam SubWindowParam { get; set; } = new();
    }

    /// <summary>
    /// Tham số nguồn tín hiệu gắn vào sub-window. Kiến trúc dự án chỉ dùng nguồn qua HDMI
    /// (videoInputChannelID) — xem 00-api-catalog.md "mọi nguồn qua HDMI input, nhóm
    /// streaming/channels ngoài phạm vi". Không có field streamingChannelID.
    /// </summary>
    [XmlType(Namespace = ISAPIXml.Ns)]
    public class VwISAPISubWindowParam
    {
        /// <summary>Ví dụ đo thật: "video input" (có dấu cách, §E.1).</summary>
        [XmlElement("signalMode")]
        public string SignalMode { get; set; } = VwDeviceProfile.SignalMode;

        /// <summary>ID kênh input HDMI — khớp VwSource, dạng số nguyên đóng gói theo board/port (xem §H.1 công thức ID).</summary>
        [XmlElement("videoInputChannelID")]
        public string VideoInputChannelId { get; set; } = string.Empty;
    }
}
