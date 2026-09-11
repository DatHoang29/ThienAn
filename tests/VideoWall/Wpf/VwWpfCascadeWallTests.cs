using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Api.Direct;
using Module.VideoWall.WPF.Api.Direct.Isapi;
using Module.VideoWall.WPF.Api.Dto;
using Module.VideoWall.WPF.Controls;
using Module.VideoWall.WPF.Interaction;
using Module.VideoWall.WPF.ViewModels;
using Services.Shared.Events;
using Tests.Modules.VideoWall.MockServer;
using Xunit;
using VwProbeDeviceOutput = Module.VideoWall.WPF.Api.Dto.VwProbeDeviceOutput;

namespace Tests.Modules.VideoWall.Wpf;

/// <summary>
/// Description: Bộ kiểm thử chuyên biệt cho tường ghép cascade DS-C66S (lưới 8x4, 32 màn hình) trên giao diện Module.VideoWall.WPF.
/// Created date: 08/09/2026
/// </summary>
public class VwWpfCascadeWallTests
{
    /// <summary>
    /// Description: Kiểm tra VwDirectDeviceConstants.EffectiveTile trả về đúng kích thước BaseOutputSize hoặc fallback 1920 khi giá trị không hợp lệ.
    /// Created date: 08/09/2026
    /// </summary>
    [Fact]
    public void EffectiveTile_ReturnsBaseOutputSizeOrFallback1920_Test()
    {
        // Arrange & Act & Assert
        Assert.Equal(1920, VwDirectDeviceConstants.EffectiveTile(null));
        Assert.Equal(1920, VwDirectDeviceConstants.EffectiveTile(0));
        Assert.Equal(1920, VwDirectDeviceConstants.EffectiveTile(-100));
        Assert.Equal(1920, VwDirectDeviceConstants.EffectiveTile(1920));
        Assert.Equal(3840, VwDirectDeviceConstants.EffectiveTile(3840));
    }

    /// <summary>
    /// Description: Kiểm tra VwDirectDeviceConnectionClient.Probe phát hiện BoundWallNo từ danh sách tường có wallBindOutputStatus = bound.
    /// Created date: 08/09/2026
    /// </summary>
    [Fact]
    public async Task Probe_ResolvesBoundWallNo_WhenWallBoundOutputStatusIsBound_Test()
    {
        // Arrange
        const int port = 18131;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var isApiClient = BuildIsapiClient(port);
        var publisher = new RecordingPublisherTest();
        var client = new VwDirectDeviceConnectionClient(isApiClient, publisher);

        // Act
        var result = await client.Probe(null);

        // Assert
        Assert.True(result.Reachable);
        Assert.Equal(1, result.BoundWallNo);
        Assert.Equal(1, result.WallNo);
        Assert.NotNull(result.Outputs);
        Assert.Equal(12, result.Outputs.Count);
    }

    /// <summary>
    /// Description: Kiểm tra ConnectionViewModel tự động đồng bộ WallNo từ BoundWallNo sau khi Probe thiết bị thành công.
    /// Created date: 08/09/2026
    /// </summary>
    [Fact]
    public async Task ConnectionViewModel_Probe_SetsWallNoFromBoundWallNo_Test()
    {
        // Arrange
        const int port = 18132;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPublisher, publisher, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = null,
        };

        // Act
        await connection.ConnectCommand.ExecuteAsync(null);
        await connection.ProbeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(1, connection.WallNo);
        Assert.NotNull(connection.ProbeResult);
        Assert.Equal(1, connection.ProbeResult.BoundWallNo);
    }

    /// <summary>
    /// Description: Kiểm tra SceneSetupViewModel.Tile cập nhật linh hoạt theo BaseOutputSize của ProbeResult và fallback 1920 khi chưa probe.
    /// Created date: 08/09/2026
    /// </summary>
    [Fact]
    public void SceneSetupViewModel_Tile_ScalesWithProbeResultBaseOutputSize_Test()
    {
        // Arrange
        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPublisher, publisher, new UserConfirmationTest(true));
        var sceneSetup = new SceneSetupViewModel(activityPublisher, connection, new UserConfirmationTest(true));

        // Act & Assert 1: ProbeResult null -> Tile = 1920
        Assert.Equal(1920, sceneSetup.Tile);

        // Act & Assert 2: ProbeResult có BaseOutputSize = 3840 -> Tile = 3840
        connection.ProbeResult = new VwProbeDeviceOutput
        {
            Reachable = true,
            BaseOutputSize = 3840,
        };
        Assert.Equal(3840, sceneSetup.Tile);

        // Act & Assert 3: ProbeResult có BaseOutputSize = 0 -> Tile = 1920 fallback
        connection.ProbeResult = new VwProbeDeviceOutput
        {
            Reachable = true,
            BaseOutputSize = 0,
        };
        Assert.Equal(1920, sceneSetup.Tile);
    }

    /// <summary>
    /// Description: Kiểm tra SceneSetupViewModel mặc định khởi tạo lưới 8x4 cho cascade 32 màn và tính đúng TotalWallWidth/TotalWallHeight.
    /// Created date: 08/09/2026
    /// </summary>
    [Fact]
    public void SceneSetupViewModel_DefaultGrid_Is8x4_And_CalculatesDimensions_Test()
    {
        // Arrange
        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPublisher, publisher, new UserConfirmationTest(true));
        var sceneSetup = new SceneSetupViewModel(activityPublisher, connection, new UserConfirmationTest(true));

        // Assert default 8x4
        Assert.Equal(8, sceneSetup.GridCols);
        Assert.Equal(4, sceneSetup.GridRows);

        // Assert wall dimensions with default tile 1920: 8 * 1920 = 15360, 4 * 1920 = 7680
        Assert.Equal(8 * 1920, sceneSetup.TotalWallWidth);
        Assert.Equal(4 * 1920, sceneSetup.TotalWallHeight);

        // Assert wall dimensions with custom tile 3840
        connection.ProbeResult = new VwProbeDeviceOutput
        {
            Reachable = true,
            BaseOutputSize = 3840,
        };
        Assert.Equal(8 * 3840, sceneSetup.TotalWallWidth);
        Assert.Equal(4 * 3840, sceneSetup.TotalWallHeight);
    }

    /// <summary>
    /// Description: Kiểm tra UpdateStartScreenPresets tự động sinh đủ 32 màn hình fallback cho tường cascade 8x4 với tọa độ và nhãn chính xác.
    /// Created date: 08/09/2026
    /// </summary>
    [Fact]
    public void SceneSetupViewModel_UpdateStartScreenPresets_Generates8x4Grid_32Screens_Test()
    {
        // Arrange
        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPublisher, publisher, new UserConfirmationTest(true));
        var sceneSetup = new SceneSetupViewModel(activityPublisher, connection, new UserConfirmationTest(true))
        {
            GridCols = 8,
            GridRows = 4,
        };

        // Act
        sceneSetup.UpdateStartScreenPresets();

        // Assert
        Assert.Equal(32, sceneSetup.AvailableStartScreens.Count);

        var firstScreen = sceneSetup.AvailableStartScreens[0];
        Assert.Equal("Màn 1", firstScreen.Name);
        Assert.Equal(1, firstScreen.Col);
        Assert.Equal(1, firstScreen.Row);
        Assert.Equal(0, firstScreen.X);
        Assert.Equal(0, firstScreen.Y);

        var lastScreen = sceneSetup.AvailableStartScreens[31];
        Assert.Equal("Màn 32", lastScreen.Name);
        Assert.Equal(8, lastScreen.Col);
        Assert.Equal(4, lastScreen.Row);
        Assert.Equal(7 * 1920, lastScreen.X);
        Assert.Equal(3 * 1920, lastScreen.Y);
    }

    /// <summary>
    /// Description: Kiểm tra các lệnh thao tác thiết bị của SceneSetupViewModel bị chặn và thông báo cảnh báo khi WallNo là null.
    /// Created date: 08/09/2026
    /// </summary>
    [Fact]
    public async Task SceneSetupViewModel_WallNoGuard_BlocksCommandsWhenWallNoNull_Test()
    {
        // Arrange
        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPublisher, publisher, new UserConfirmationTest(true))
        {
            WallNo = null,
        };
        var sceneSetup = new SceneSetupViewModel(activityPublisher, connection, new UserConfirmationTest(true));
        var dummyScene = new VwSceneDto { ID = "1", Name = "Kịch bản Test" };

        // Act & Assert 1: ActivateSceneCommand khi WallNo null
        sceneSetup.CurrentScene = dummyScene;
        await sceneSetup.ActivateSceneCommand.ExecuteAsync(null);
        Assert.Contains("Cần nhập WallNo", sceneSetup.StatusMessage);

        // Act & Assert 2: SyncScenesFromDeviceCommand khi WallNo null
        sceneSetup.StatusMessage = "";
        await sceneSetup.SyncScenesFromDeviceCommand.ExecuteAsync(null);
        Assert.Contains("Cần nhập WallNo", sceneSetup.StatusMessage);

        // Act & Assert 3: PullWindowsFromDeviceCommand khi WallNo null
        sceneSetup.StatusMessage = "";
        await sceneSetup.PullWindowsFromDeviceCommand.ExecuteAsync(null);
        Assert.Contains("Cần nhập WallNo", sceneSetup.StatusMessage);

        // Act & Assert 4: CanPushToDevice bị vô hiệu hóa khi WallNo null
        Assert.False(sceneSetup.PushToDeviceCommand.CanExecute(null));
        Assert.False(sceneSetup.PushWindowsOnlyToDeviceCommand.CanExecute(null));
    }

    /// <summary>
    /// Description: Kiểm tra VisualWallCanvas.SnapToGrid và SnapSize tính toán chính xác theo kích thước tile động 1920 và 3840.
    /// Created date: 08/09/2026
    /// </summary>
    [Fact]
    public void VisualWallCanvas_SnapToGrid_And_SnapSize_WithDynamicTile_Test()
    {
        // SnapToGrid với Tile 1920
        Assert.Equal(0, VisualWallCanvas.SnapToGrid(500, 1920));
        Assert.Equal(1920, VisualWallCanvas.SnapToGrid(1200, 1920));
        Assert.Equal(1920, VisualWallCanvas.SnapToGrid(2100, 1920));
        Assert.Equal(3840, VisualWallCanvas.SnapToGrid(3500, 1920));

        // SnapToGrid với Tile 3840
        Assert.Equal(0, VisualWallCanvas.SnapToGrid(1000, 3840));
        Assert.Equal(3840, VisualWallCanvas.SnapToGrid(2500, 3840));
        Assert.Equal(7680, VisualWallCanvas.SnapToGrid(6000, 3840));

        // SnapSize với Tile 1920
        Assert.Equal(1920, VisualWallCanvas.SnapSize(0, 1920));
        Assert.Equal(1920, VisualWallCanvas.SnapSize(1080, 1920));
        Assert.Equal(1920, VisualWallCanvas.SnapSize(1920, 1920));
        Assert.Equal(3840, VisualWallCanvas.SnapSize(3000, 1920));

        // SnapSize với Tile 3840
        Assert.Equal(3840, VisualWallCanvas.SnapSize(0, 3840));
        Assert.Equal(3840, VisualWallCanvas.SnapSize(2000, 3840));
        Assert.Equal(7680, VisualWallCanvas.SnapSize(6000, 3840));

        // FormatScaleInfo & FormatScreenCoordinateRange
        var scaleInfo = VisualWallCanvas.FormatScaleInfo(0.1, 8, 4, 15360, 7680);
        Assert.Contains("Lưới 8 × 4 (32 Màn hình 16:9)", scaleInfo);
        Assert.Contains("X[0 ➔ 15360 px]", scaleInfo);
        Assert.Contains("Y[0 ➔ 7680 px]", scaleInfo);

        var coordRange = VisualWallCanvas.FormatScreenCoordinateRange(1920, 3840, 1920);
        Assert.Contains("X: 1920 ➔ 3840", coordRange);
        Assert.Contains("Y: 3840 ➔ 5760", coordRange);
    }

    private static VwDirectISAPIClient BuildIsapiClient(int port)
    {
        var credentials = new VwDirectDeviceCredentials("127.0.0.1", port, "admin", "Password123!");
        var digestHandler = new VwDirectDigestHandler { InnerHandler = new HttpClientHandler() };
        var httpClient = new HttpClient(digestHandler)
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}"),
            Timeout = TimeSpan.FromSeconds(30),
        };
        return new VwDirectISAPIClient(httpClient, credentials);
    }
}
