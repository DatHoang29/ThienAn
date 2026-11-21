using System.Text.Json.Serialization;
using Module.VideoWall.WPF.Api.Direct.Isapi;

namespace Module.VideoWall.WPF.Api.Dto;

/// <summary>
/// Author: Đạt
/// Description: Body chung của các endpoint nhận đúng một ID (Ping, Delete, ById...).
/// Với device-setup, ID ở đây là VwController.ID trong CSDL.
/// Created date: 25/08/2026
/// </summary>
public class VwIdInput
{
    /// <summary>ID định danh của đối tượng (ví dụ: ControllerId).</summary>
    [JsonPropertyName("id")]
    public string ID { get; set; } = string.Empty;
}

/// <summary>
/// Author: Đạt
/// Description: Body của POST /api/videowall/vwdevicesetup/probe. Bỏ trống WallNo để backend
/// tự dò tường đang gắn màn hình.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwProbeDeviceInput : VwIdInput
{
    /// <summary>Số hiệu tường cần khảo sát (WallNo); bỏ trống để tự động dò.</summary>
    public int? WallNo { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Body của POST /api/videowall/vwdevicesetup/setupscene.
/// DryRun phía backend MẶC ĐỊNH true, nên muốn ghi thật buộc phải gửi rõ DryRun = false.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwSetupSceneInput
{
    /// <summary>ID bộ điều khiển cần thiết lập kịch bản.</summary>
    public string? ControllerId { get; set; }

    /// <summary>Ưu tiên SceneId; bỏ trống thì backend tra theo SceneCode.</summary>
    public string? SceneId { get; set; }

    /// <summary>Mã nghiệp vụ của kịch bản (SceneCode) dùng tra cứu khi không có SceneId.</summary>
    public string? SceneCode { get; set; }

    /// <summary>Số hiệu tường vật lý cần thiết lập kịch bản.</summary>
    public int? WallNo { get; set; }

    /// <summary>Chạy khảo sát và dựng payload nhưng KHÔNG phát lệnh ghi xuống thiết bị.</summary>
    public bool DryRun { get; set; } = true;

    /// <summary>Xoá trắng mọi cửa sổ đang có trên tường trước khi dựng lại — kể cả cửa sổ của người khác.</summary>
    public bool ResetWindows { get; set; }

    /// <summary>Đồng bộ danh sách nguồn tín hiệu (SyncSources) trước khi dựng cửa sổ.</summary>
    public bool SyncSources { get; set; }

    /// <summary>Bước duy nhất làm đổi hình đang chiếu trên tường.</summary>
    public bool Activate { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Thông tin kết nối ad-hoc khi thiết bị chưa được khai báo trong CSDL.
/// CHỈ dùng được với endpoint Isapi passthrough — Ping/Probe/SetupScene đều đòi ControllerId.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwISAPIPassthroughDevice
{
    /// <summary>Địa chỉ IP của thiết bị ISAPI.</summary>
    public string? Ip { get; set; }

    /// <summary>Cổng HTTP/ISAPI của thiết bị.</summary>
    public int? Port { get; set; }

    /// <summary>Tài khoản đăng nhập thiết bị ISAPI.</summary>
    public string? Account { get; set; }

    /// <summary>Mật khẩu đăng nhập thiết bị ISAPI.</summary>
    public string? Password { get; set; }

    /// <summary>Bỏ qua kiểm tra mật khẩu yếu (SkipPasswordCheck).</summary>
    public bool SkipPasswordCheck { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Body của POST /api/videowall/vwdevicesetup/isapi. Bắt buộc đúng một trong
/// ControllerId hoặc Device (backend validate ở tầng service).
/// Created date: 25/08/2026
/// </summary>
public sealed class VwISAPIPassthroughInput
{
    /// <summary>ID bộ điều khiển trong CSDL (dùng khi không truyền trực tiếp Device).</summary>
    public string? ControllerId { get; set; }

    /// <summary>Thông tin kết nối ad-hoc đến thiết bị (khi chưa lưu trong CSDL).</summary>
    public VwISAPIPassthroughDevice? Device { get; set; }

    /// <summary>Phương thức HTTP (GET, POST, PUT, DELETE...).</summary>
    public string Method { get; set; } = "GET";

    /// <summary>Đường dẫn tương đối, KHÔNG bắt đầu bằng "/". VD: "ISAPI/DisplayDev/VideoWall/capabilities".</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Nội dung gói tin gửi đi (XML hoặc chuỗi thô).</summary>
    public string? Body { get; set; }

    /// <summary>Kiểu nội dung gói tin (ví dụ: application/xml).</summary>
    public string? ContentType { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Một bước trong kịch bản đấu nối (VwSetupSceneStep). Giữ nguyên RequestXml/ResponseXml
/// để đọc lại khi thất bại — lỗi ISAPI nằm ở subStatusCode chứ không phải statusString.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwSetupSceneStep
{
    /// <summary>Thứ tự thực hiện bước trong quy trình thiết lập.</summary>
    public int Order { get; set; }

    /// <summary>Tên mô tả của bước thiết lập.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Phương thức HTTP (GET, PUT, POST...).</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Đường dẫn API endpoint trên thiết bị.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Cờ cho biết bước thực thi thành công hay thất bại.</summary>
    public bool Success { get; set; }

    /// <summary>Bước bị bỏ qua do cấu hình (DryRun, ResetWindows tắt...) — KHÔNG tính là lỗi.</summary>
    public bool Skipped { get; set; }

    /// <summary>Mã phản hồi HTTP từ thiết bị.</summary>
    public int? HttpStatus { get; set; }

    /// <summary>Thông điệp kết quả hoặc lỗi từ thiết bị.</summary>
    public string? Message { get; set; }

    /// <summary>Thời gian thực thi bước tính bằng mili-giây (ms).</summary>
    public long DurationMs { get; set; }

    /// <summary>Nội dung gói tin XML yêu cầu gửi đi.</summary>
    public string? RequestXml { get; set; }

    /// <summary>Nội dung gói tin XML phản hồi nhận về.</summary>
    public string? ResponseXml { get; set; }

    /// <summary>Nhãn trạng thái ngắn gọn cho dòng log và bảng hiển thị — chỉ dùng ở UI.</summary>
    [JsonIgnore]
    public string Outcome => Skipped ? "Bỏ qua" : Success ? "OK" : "Fail";
}

/// <summary>
/// Author: Đạt
/// Description: Một tường logic đọc từ thiết bị (VwWallSummary).
/// Created date: 25/08/2026
/// </summary>
public sealed class VwWallSummary
{
    /// <summary>Số hiệu tường (WallId/WallNo) trên thiết bị.</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>Tên tường hiển thị.</summary>
    public string? Name { get; set; }

    /// <summary>Trạng thái liên kết cổng ra (ví dụ: bound, unbound).</summary>
    public string? BindOutputStatus { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Một điểm lệch giữa giá trị đang lưu trong CSDL và giá trị thật đọc từ thiết bị.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwDeviceMismatch
{
    /// <summary>ID của thực thể trong CSDL bị lệch thông tin.</summary>
    public string? EntityId { get; set; }

    /// <summary>Tên thực thể bị lệch thông tin.</summary>
    public string? EntityName { get; set; }

    /// <summary>Tên trường dữ liệu có sự sai khác giữa CSDL và thiết bị.</summary>
    public string? Field { get; set; }

    /// <summary>Giá trị đang lưu trong CSDL.</summary>
    public string? DbValue { get; set; }

    /// <summary>Giá trị thực tế đọc từ thiết bị.</summary>
    public string? DeviceValue { get; set; }

    /// <summary>Ghi chú hoặc hướng dẫn khắc phục sai lệch.</summary>
    public string? Note { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Toạ độ góc của một vùng hiển thị trên thiết bị.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwISAPICoordinate
{
    /// <summary>Tọa độ hoành độ X.</summary>
    public int X { get; set; }

    /// <summary>Tọa độ tung độ Y.</summary>
    public int Y { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Hình chữ nhật vùng hiển thị đọc từ thiết bị.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwISAPIRect
{
    /// <summary>Tọa độ điểm gốc (X, Y) của vùng chữ nhật.</summary>
    public VwISAPICoordinate? Coordinate { get; set; }

    /// <summary>Chiều rộng của vùng chữ nhật tính bằng pixel.</summary>
    public int Width { get; set; }

    /// <summary>Chiều cao của vùng chữ nhật tính bằng pixel.</summary>
    public int Height { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Một cổng ra đọc từ thiết bị.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwISAPIOutputItem
{
    /// <summary>Chỉ số thứ tự cổng ra trên danh sách thiết bị.</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>Mã định danh cổng ra vật lý (OutputId).</summary>
    public int OutputId { get; set; }

    /// <summary>Vùng chữ nhật hiển thị của cổng ra trên tường.</summary>
    public VwISAPIRect? Rect { get; set; }

    /// <summary>Số lượng cửa sổ tối đa cổng ra này hỗ trợ hiển thị.</summary>
    public int OutputWinNum { get; set; }

    /// <summary>Chế độ tọa độ (uniformCoordinate hoặc resolutionCoordinate).</summary>
    public string? CoordinateMode { get; set; }

    /// <summary>Tọa độ X lấy từ Rect.</summary>
    [JsonIgnore]
    public int X => Rect?.Coordinate?.X ?? 0;

    /// <summary>Tọa độ Y lấy từ Rect.</summary>
    [JsonIgnore]
    public int Y => Rect?.Coordinate?.Y ?? 0;

    /// <summary>Chiều rộng lấy từ Rect.</summary>
    [JsonIgnore]
    public int Width => Rect?.Width ?? 1920;

    /// <summary>Chiều cao lấy từ Rect.</summary>
    [JsonIgnore]
    public int Height => Rect?.Height ?? 1080;

    /// <summary>Độ phân giải hiển thị dạng chuỗi W × H px.</summary>
    [JsonIgnore]
    public string Resolution => $"{Width} × {Height} px";

    /// <summary>Vị trí hiển thị dạng chuỗi X, Y.</summary>
    [JsonIgnore]
    public string Position => $"X: {X}, Y: {Y}";
}

/// <summary>
/// Author: Đạt
/// Description: Vị trí cổng trên bo mạch của thiết bị.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwISAPIPortInBoard
{
    /// <summary>ID bo mạch chứa cổng.</summary>
    public int BoardId { get; set; }

    /// <summary>ID cổng trên bo mạch.</summary>
    public int PortId { get; set; }

    /// <summary>Địa chỉ IP bo mạch (nếu là bo giải mã mạng).</summary>
    public string? IpAddress { get; set; }

    /// <summary>Cổng mạng kết nối đến bo mạch.</summary>
    public int Port { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Một kênh tín hiệu vào đọc từ thiết bị — Id chính là videoInputChannelID
/// mà VwSource.SignalNo phải khớp.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwISAPIInputChannel
{
    /// <summary>ID kênh đầu vào video (videoInputChannelID khớp với SignalNo).</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>Loại cổng tín hiệu vào (ví dụ: HDMI, DVI, IP...).</summary>
    public string? InputPortType { get; set; }

    /// <summary>Tên kênh tín hiệu đầu vào.</summary>
    public string? Name { get; set; }

    /// <summary>Trạng thái truy cập của kênh tín hiệu vào.</summary>
    public string? VideoInputChannelAccessStatus { get; set; }

    /// <summary>Vị trí cổng trên bo mạch phần cứng.</summary>
    public VwISAPIPortInBoard? PortInBoard { get; set; }

    /// <summary>Đường dẫn luồng phát trực tiếp của kênh.</summary>
    public string? StreamUrl { get; set; }

    /// <summary>ID kênh phát luồng mạng (StreamingChannelId).</summary>
    public int? StreamingChannelId { get; set; }

    /// <summary>Thông tin tóm tắt vị trí bo hoặc luồng hiển thị trên UI.</summary>
    [JsonIgnore]
    public string BoardInfo => PortInBoard != null && PortInBoard.BoardId > 0
        ? $"Bo {PortInBoard.BoardId} · Cổng {PortInBoard.PortId}"
        : (StreamingChannelId > 0 ? $"Stream #{StreamingChannelId}" : "—");
}

/// <summary>
/// Author: Đạt
/// Description: Kết quả khảo sát thiết bị (VwProbeDeviceOutput) — toàn bộ bằng lệnh chỉ-đọc
/// nên gọi lúc nào cũng an toàn.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwProbeDeviceOutput
{
    /// <summary>ID bộ điều khiển trong CSDL.</summary>
    public string? ControllerId { get; set; }

    /// <summary>Tên bộ điều khiển trong CSDL.</summary>
    public string? ControllerName { get; set; }

    /// <summary>Địa chỉ IP của thiết bị được khảo sát.</summary>
    public string? Ip { get; set; }

    /// <summary>Cờ cho biết thiết bị có phản hồi kết nối hay không.</summary>
    public bool Reachable { get; set; }

    /// <summary>Thiết bị có hỗ trợ tính năng kịch bản (Scene) hay không.</summary>
    public bool? IsSupportScene { get; set; }

    /// <summary>Số lượng cửa sổ tối đa thiết bị hỗ trợ mở cùng lúc.</summary>
    public int? MaxWindowNums { get; set; }

    /// <summary>Trần SID kịch bản — VwScene.OutputId phải nằm trong 1..giá trị này.</summary>
    public int? MaxSceneNums { get; set; }

    /// <summary>Kích thước pixel cơ sở của một ô cổng ra (thường là 1920).</summary>
    public int? BaseOutputSize { get; set; }

    /// <summary>Model thiết bị được nhận diện hoặc giả định.</summary>
    public string? AssumedModel { get; set; }

    /// <summary>Số hiệu tường logic đang khảo sát.</summary>
    public int? WallNo { get; set; }

    /// <summary>Số hiệu tường vật lý có wallBindOutputStatus = "bound" đọc từ thiết bị.</summary>
    public int? BoundWallNo { get; set; }

    /// <summary>Danh sách các tường logic đọc được từ thiết bị.</summary>
    public List<VwWallSummary>? Walls { get; set; }

    /// <summary>Danh sách cổng ra màn hình đọc được từ thiết bị.</summary>
    public List<VwISAPIOutputItem>? Outputs { get; set; }

    /// <summary>Danh sách kênh tín hiệu vào đọc được từ thiết bị.</summary>
    public List<VwISAPIInputChannel>? InputChannels { get; set; }

    /// <summary>Danh sách kịch bản hiện có trên thiết bị.</summary>
    public List<VwWallSceneSummary>? Scenes { get; set; }

    /// <summary>ID kịch bản hiện đang chạy trên tường.</summary>
    public int? RunningSceneId { get; set; }

    /// <summary>Danh sách cửa sổ đang mở trên tường hiện tại.</summary>
    public List<VwISAPIWallWindowItem>? ActiveWindows { get; set; }

    /// <summary>Danh sách điểm sai khác thông tin màn hình giữa CSDL và thiết bị.</summary>
    public List<VwDeviceMismatch>? ScreenMismatches { get; set; }

    /// <summary>Danh sách điểm sai khác thông tin nguồn giữa CSDL và thiết bị.</summary>
    public List<VwDeviceMismatch>? SourceMismatches { get; set; }

    /// <summary>Nhật ký các bước thực hiện trong quá trình khảo sát.</summary>
    public List<VwSetupSceneStep>? Steps { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Thông tin tóm tắt kịch bản trên tường logic (VwWallSceneSummary).
/// Created date: 25/08/2026
/// </summary>
public sealed class VwWallSceneSummary
{
    /// <summary>ID kịch bản trên thiết bị.</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>Tên kịch bản trên thiết bị.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Kết quả đẩy một cửa sổ của kịch bản xuống thiết bị (VwSetupWindowResult).
/// Created date: 25/08/2026
/// </summary>
public sealed class VwSetupWindowResult
{
    /// <summary>ID cửa sổ kịch bản trong CSDL (VwWindowScene.ID).</summary>
    public string? WindowSceneId { get; set; }

    /// <summary>Nhãn hiển thị trên cửa sổ.</summary>
    public string? Label { get; set; }

    /// <summary>ID nguồn tín hiệu gán cho cửa sổ.</summary>
    public string? SourceId { get; set; }

    /// <summary>Kênh tín hiệu vào (SignalNo/videoInputChannelID) trên thiết bị.</summary>
    public int? SignalNo { get; set; }

    /// <summary>Tọa độ X của cửa sổ trên toàn bức tường.</summary>
    public int WallX { get; set; }

    /// <summary>Tọa độ Y của cửa sổ trên toàn bức tường.</summary>
    public int WallY { get; set; }

    /// <summary>Chiều rộng của cửa sổ trên toàn bức tường.</summary>
    public int WallW { get; set; }

    /// <summary>Chiều cao của cửa sổ trên toàn bức tường.</summary>
    public int WallH { get; set; }

    /// <summary>Tọa độ X cục bộ trong phạm vi bộ điều khiển.</summary>
    public int LocalX { get; set; }

    /// <summary>Tọa độ Y cục bộ trong phạm vi bộ điều khiển.</summary>
    public int LocalY { get; set; }

    /// <summary>Chiều rộng cục bộ trong phạm vi bộ điều khiển.</summary>
    public int LocalW { get; set; }

    /// <summary>Chiều cao cục bộ trong phạm vi bộ điều khiển.</summary>
    public int LocalH { get; set; }

    /// <summary>ID cửa sổ sinh ra trên thiết bị ISAPI.</summary>
    public string? DeviceWindowId { get; set; }

    /// <summary>Cờ đánh dấu thiết lập cửa sổ thành công.</summary>
    public bool Success { get; set; }

    /// <summary>Thông điệp kết quả hoặc lỗi khi tạo cửa sổ.</summary>
    public string? Message { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Kết quả chạy kịch bản dựng cảnh xuống thiết bị (VwSetupSceneOutput).
/// Created date: 25/08/2026
/// </summary>
public sealed class VwSetupSceneOutput
{
    /// <summary>Cờ đánh dấu toàn bộ quy trình dựng cảnh thành công.</summary>
    public bool Success { get; set; }

    /// <summary>Cờ cho biết lần chạy này là chạy thử (DryRun) hay ghi thật xuống thiết bị.</summary>
    public bool DryRun { get; set; }

    /// <summary>ID bộ điều khiển được thiết lập.</summary>
    public string? ControllerId { get; set; }

    /// <summary>Tên bộ điều khiển được thiết lập.</summary>
    public string? ControllerName { get; set; }

    /// <summary>Số hiệu tường vật lý trên thiết bị.</summary>
    public int? WallNo { get; set; }

    /// <summary>ID kịch bản trong CSDL.</summary>
    public string? SceneId { get; set; }

    /// <summary>Mã nghiệp vụ kịch bản.</summary>
    public string? SceneCode { get; set; }

    /// <summary>Chỉ số Scene SID tương ứng trên thiết bị ISAPI.</summary>
    public string? SceneSid { get; set; }

    /// <summary>Số lượng kịch bản tối đa thiết bị hỗ trợ.</summary>
    public int? MaxSceneNums { get; set; }

    /// <summary>Thông điệp tổng kết kết quả thiết lập cảnh.</summary>
    public string? Message { get; set; }

    /// <summary>Danh sách thông tin các tường trên thiết bị.</summary>
    public List<VwWallSummary>? Walls { get; set; }

    /// <summary>Danh sách chi tiết từng bước trong quy trình thiết lập.</summary>
    public List<VwSetupSceneStep>? Steps { get; set; }

    /// <summary>Danh sách kết quả thiết lập từng cửa sổ.</summary>
    public List<VwSetupWindowResult>? Windows { get; set; }

    // CỐ Ý bỏ WindowSnapshot (List<VwISAPIWallWindowItem>): client không dựng ngược cửa sổ nên
    // không cần cây DTO đó. System.Text.Json mặc định bỏ qua trường JSON lạ nên vẫn parse được.
}
