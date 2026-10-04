using ITS.VideoWall.Extensions;

namespace Tests;

/// <summary>
/// Author: Đạt
/// Description: Phần mở rộng của Test Host dành riêng cho phân hệ VideoWall — sở hữu và quản lý
///              vòng đời MockServer giả lập thiết bị Hikvision (HttpListener thật trên 127.0.0.1,
///              các port 18080-18083). File nằm trong tests/VideoWall/ nên khi Module.VideoWall
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
    public VwISAPIServerHikvisionMock MockServer { get; } = new();

    /// <summary>
    /// Author: Đạt
    /// Description: Mở HttpListener trên toàn bộ port mặc định để phục vụ kịch bản đa controller
    /// Created date: 21/08/2026
    /// </summary>
    partial void StartModuleTestServers()
    {
        MockServer.Start(VwISAPIServerHikvisionMock.DefaultPorts);
        var db = Services.GetService<ISqlSugarClient>();
        if (db != null)
        {
            db.Aop.DataExecuted = (value, entity) =>
            {
                if (entity.EntityValue is Module.VideoWall.Core.Entities.VwSource src && src.SignalStatus == null)
                {
                    src.SignalStatus = new BaseEnums.SignalStatus();
                }
            };
        }
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Giải phóng HttpListener khi Test Collection kết thúc để không giữ cổng cho lần chạy sau
    /// Created date: 21/08/2026
    /// </summary>
    partial void StopModuleTestServers() => MockServer.Dispose();

    /// <summary>
    /// Description: Đăng ký các dịch vụ của VideoWall Worker vào Test Host để phục vụ kiểm thử tích hợp qua NATS thật
    /// Created date: 11/09/2026
    /// </summary>
    partial void ConfigureModuleTestServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddVideoWallWorkerCoreServices(configuration);
        services.AddHostedService<VwCommandConsumer>();
    }
}
