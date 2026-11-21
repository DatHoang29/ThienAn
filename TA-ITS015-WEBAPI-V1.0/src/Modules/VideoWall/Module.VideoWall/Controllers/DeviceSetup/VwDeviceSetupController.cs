using Module.VideoWall.Core.Dto.DeviceSetup;

namespace Module.VideoWall.Controllers.DeviceSetup;

/// <summary>
/// API chạy kịch bản đấu nối thiết bị Video Wall.
/// Toàn bộ endpoint yêu cầu xác thực JWT qua pipeline mặc định của hệ thống.
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 490)]
public class VwDeviceSetupController : BaseController
{
    /// <summary>Xác nhận Digest Auth vào bộ điều khiển.</summary>
    [ApiDescriptionSettings(Name = "Ping"), HttpPost]
    [DisplayName("Ping video wall controller")]
    public async Task<VwDeviceSetupAcceptedOutput> Ping(VwPingDeviceInput input)
    {
        return await MessBus.InvokeAsync<VwDeviceSetupAcceptedOutput>(input);
    }

    /// <summary>Khảo sát thiết bị bằng lệnh chỉ-đọc và đối chiếu với dữ liệu đang lưu.</summary>
    [ApiDescriptionSettings(Name = "Probe"), HttpPost]
    [DisplayName("Probe video wall device")]
    public async Task<VwDeviceSetupAcceptedOutput> Probe(VwProbeDeviceInput input)
    {
        return await MessBus.InvokeAsync<VwDeviceSetupAcceptedOutput>(input);
    }

    /// <summary>
    /// Đọc cổng vào vật lý của bộ điều khiển (chỉ đọc) để chọn khi khai báo nguồn cục bộ.
    /// Kết quả về FE qua NATS theo MessageId: Data.Data.VideoInputChannel[] (Id = videoInputChannelID ⇒ VwSource.SignalNo).
    /// </summary>
    [ApiDescriptionSettings(Name = "InputChannels"), HttpPost]
    [DisplayName("Read video wall controller input channels")]
    public async Task<VwDeviceSetupAcceptedOutput> InputChannels(VwInputChannelsInput input)
    {
        return await MessBus.InvokeAsync<VwDeviceSetupAcceptedOutput>(input);
    }

    /// <summary>Nạp videoInputChannelID thật của thiết bị vào VwSource.SignalNo.</summary>
    [ApiDescriptionSettings(Name = "SyncSources"), HttpPost]
    [DisplayName("Sync video wall sources from device")]
    public async Task<VwDeviceSetupAcceptedOutput> SyncSources(VwSyncSourcesInput input)
    {
        return await MessBus.InvokeAsync<VwDeviceSetupAcceptedOutput>(input);
    }

    /// <summary>
    /// Đồng bộ cổng ra (bảng gán cổng → ô) từ thiết bị về CSDL.
    /// Bỏ trống ID ⇒ đồng bộ mọi bộ điều khiển (bộ trung tâm + bộ con).
    /// </summary>
    [ApiDescriptionSettings(Name = "SyncOutputs"), HttpPost]
    [DisplayName("Sync video wall outputs from device")]
    public async Task<VwDeviceSetupAcceptedOutput> SyncOutputs(VwSyncOutputsInput input)
    {
        return await MessBus.InvokeAsync<VwDeviceSetupAcceptedOutput>(input);
    }

    /// <summary>
    /// Đồng bộ màn đang cắm trên các bộ con về CSDL (ghép vào cổng ra đã lưu).
    /// Bỏ trống ID ⇒ mọi bộ con.
    /// </summary>
    [ApiDescriptionSettings(Name = "SyncScreens"), HttpPost]
    [DisplayName("Sync video wall screens from device")]
    public async Task<VwDeviceSetupAcceptedOutput> SyncScreens(VwSyncScreensInput input)
    {
        return await MessBus.InvokeAsync<VwDeviceSetupAcceptedOutput>(input);
    }

    /// <summary>
    /// Ghi bảng cổng ra → vị trí (WallOutputList) xuống bộ con theo Bố cục gán cổng.
    /// DryRun = true (mặc định) ⇒ chỉ đọc thiết bị và trả bảng so sánh; kết quả về FE qua NATS (theo MessageId).
    /// </summary>
    [ApiDescriptionSettings(Name = "ApplyWallOutputs"), HttpPost]
    [DisplayName("Apply wall output positions to display controllers")]
    public async Task<VwDeviceSetupAcceptedOutput> ApplyWallOutputs(VwApplyWallOutputsInput input)
    {
        return await MessBus.InvokeAsync<VwDeviceSetupAcceptedOutput>(input);
    }

    /// <summary>
    /// Bật (On = true) / tắt toàn bộ màn của bộ hiển thị. ControllerIds trống ⇒ toàn tường.
    /// Kết quả về FE qua NATS theo MessageId.
    /// </summary>
    [ApiDescriptionSettings(Name = "ScreenPower"), HttpPost]
    [DisplayName("Turn video wall screens on/off")]
    public async Task<VwDeviceSetupAcceptedOutput> ScreenPower(VwScreenPowerInput input)
    {
        return await MessBus.InvokeAsync<VwDeviceSetupAcceptedOutput>(input);
    }

    /// <summary>
    /// Đặt độ sáng (đèn nền 0-100) cho từng màn (ScreenIds, màn phải đang gán cổng ra).
    /// Kết quả về FE qua NATS theo MessageId.
    /// </summary>
    [ApiDescriptionSettings(Name = "ScreenBacklight"), HttpPost]
    [DisplayName("Set video wall screen brightness")]
    public async Task<VwDeviceSetupAcceptedOutput> ScreenBacklight(VwScreenBacklightInput input)
    {
        return await MessBus.InvokeAsync<VwDeviceSetupAcceptedOutput>(input);
    }

    /// <summary>
    /// Đọc trạng thái 1 màn từ thiết bị (bật/tắt, độ sáng). ID = VwScreen.ID. Kết quả về FE qua NATS theo MessageId.
    /// </summary>
    [ApiDescriptionSettings(Name = "ScreenStatus"), HttpPost]
    [DisplayName("Read video wall screen status")]
    public async Task<VwDeviceSetupAcceptedOutput> ScreenStatus(VwScreenStatusInput input)
    {
        return await MessBus.InvokeAsync<VwDeviceSetupAcceptedOutput>(input);
    }

    /// <summary>Đối chiếu/đồng bộ ngược kịch bản đang chạy thật trên thiết bị về CSDL.</summary>
    [ApiDescriptionSettings(Name = "SyncActiveScene"), HttpPost]
    [DisplayName("Sync video wall active scene from device")]
    public async Task<VwDeviceSetupAcceptedOutput> SyncActiveScene(VwSyncActiveSceneInput input)
    {
        return await MessBus.InvokeAsync<VwDeviceSetupAcceptedOutput>(input);
    }

    /// <summary>Chạy kịch bản dựng kịch bản hiển thị xuống thiết bị.</summary>
    [ApiDescriptionSettings(Name = "SetupScene"), HttpPost]
    [DisplayName("Setup video wall scene on device")]
    public async Task<VwDeviceSetupAcceptedOutput> SetupScene(VwSetupSceneInput input)
    {
        return await MessBus.InvokeAsync<VwDeviceSetupAcceptedOutput>(input);
    }

    /// <summary>Xoá trạng thái chặn lỗi xác thực thiết bị để thử lại sau khi sửa tài khoản/IP.</summary>
    [ApiDescriptionSettings(Name = "ResetDeviceAuthFailure"), HttpPost]
    [DisplayName("Reset video wall device auth failure")]
    public async Task ResetDeviceAuthFailure(VwResetDeviceAuthFailureInput input)
    {
        await MessBus.InvokeAsync(input);
    }

    /// <summary>Chuyển tiếp ISAPI thô cho phép tùy biến Method, Path, Body tự do.</summary>
    [ApiDescriptionSettings(Name = "Isapi"), HttpPost]
    [DisplayName("ISAPI passthrough")]
    public async Task<VwDeviceSetupAcceptedOutput> Isapi(VwISAPIPassthroughInput input)
    {
        return await MessBus.InvokeAsync<VwDeviceSetupAcceptedOutput>(input);
    }
}
