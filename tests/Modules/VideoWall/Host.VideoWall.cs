namespace Tests;

/// <summary>
/// Author: Đạt
/// Description: Phần mở rộng của Test Host dành riêng cho phân hệ VideoWall — sở hữu và quản lý
///              vòng đời MockServer giả lập thiết bị Hikvision (HttpListener thật trên 127.0.0.1,
///              các port 18080-18083). File nằm trong tests/Modules/VideoWall/ nên khi Module.VideoWall
///              không còn trong repo, file bị loại khỏi biên dịch và Host tự động không còn MockServer
///              (phương thức partial mất phần thân, lời gọi trong Host.cs bị trình biên dịch xoá).
/// Created date: 21/08/2026
/// </summary>
public partial class Host
{
    /// <summary>
    /// Author: Đạt
    /// Description: MockServer dùng chung cho toàn bộ test VideoWall trong Collection "api"
    /// Created date: 21/08/2026
    /// </summary>
    public VwISAPIMockServerHikvision MockServer { get; } = new();

    /// <summary>
    /// Author: Đạt
    /// Description: Mở HttpListener trên toàn bộ port mặc định để phục vụ kịch bản đa controller
    /// Created date: 21/08/2026
    /// </summary>
    partial void StartModuleTestServers() => MockServer.Start(VwISAPIMockServerHikvision.DefaultPorts);

    /// <summary>
    /// Author: Đạt
    /// Description: Giải phóng HttpListener khi Test Collection kết thúc để không giữ cổng cho lần chạy sau
    /// Created date: 21/08/2026
    /// </summary>
    partial void StopModuleTestServers() => MockServer.Dispose();
}
