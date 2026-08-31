using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Auth;
using Module.VideoWall.WPF.Storage;
using Module.VideoWall.WPF.ViewModels;
using Module.VideoWall.WPF.Views;
using Services.Shared.Events;
using Tests.Modules.VideoWall.MockServer;
using Xunit;
using WpfDto = Module.VideoWall.WPF.Api.Dto;

namespace Tests.Modules.VideoWall.Wpf;

public class VwWpfCommissioningTests
{
    private const string TestPrefix = "TEST_WPF_";

    private VwWpfClientStackTest BuildClientStack()
    {
        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var session = new SessionState();
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(new HttpClient()), activityPublisher);

        return new VwWpfClientStackTest
        {
            Publisher = publisher,
            ActivityPublisher = activityPublisher,
            ApiClient = new VideoWallApiClient(invoker, publisher, activityPublisher),
            Session = session
        };
    }

    private static (VwController Controller, VwScreen ScreenA, VwScreen ScreenB, VwSource Source) SeedWallAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");

        var controller = new VwController
        {
            ID = $"{TestPrefix}CTRL_{suffix}",
            Code = $"{TestPrefix}CTRL_{suffix}",
            Name = "WPF Integration Controller",
            IP = "127.0.0.1",
            Account = "admin",
            PassWord = "Password123!",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };

        var screenA = new VwScreen
        {
            ID = $"{TestPrefix}SCR_A_{suffix}",
            Code = $"{TestPrefix}SCR_A_{suffix}",
            Name = "Screen A",
            ControllerId = controller.ID,
            OutPutPort = "HDMI1",
            OutputId = "1",
            GridCol = 0,
            GridRow = 0,
            WidthPx = "1920",
            HeightPx = "1080",
            Resolution = "1920x1080",
            Status = BaseEnums.StatusEnum.Enable
        };

        var screenB = new VwScreen
        {
            ID = $"{TestPrefix}SCR_B_{suffix}",
            Code = $"{TestPrefix}SCR_B_{suffix}",
            Name = "Screen B",
            ControllerId = controller.ID,
            OutPutPort = "HDMI2",
            OutputId = "2",
            GridCol = 1,
            GridRow = 0,
            Resolution = "1920x1080",
            Status = BaseEnums.StatusEnum.Enable
        };

        var source = new VwSource
        {
            ID = $"{TestPrefix}SRC_{suffix}",
            Code = $"{TestPrefix}SRC_{suffix}",
            Name = "Camera 01",
            ControllerId = controller.ID,
            SignalNo = 1,
            Status = BaseEnums.StatusEnum.Enable
        };

        return (controller, screenA, screenB, source);
    }

    private static ConnectionViewModel BuildConnection(
        VwWpfClientStackTest stack,
        VwController controller,
        VwScreen? screenA = null,
        VwScreen? screenB = null,
        VwSource? source = null)
    {
        var conn = new ConnectionViewModel(stack.ActivityPublisher, stack.Publisher, new UserConfirmationTest(true))
        {
            AdHocIp = controller.IP ?? "127.0.0.1",
            AdHocPort = 18080,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
        };

        var screens = new List<WpfDto.VwScreenDto>();
        if (screenA != null)
        {
            screens.Add(new WpfDto.VwScreenDto
            {
                ID = screenA.ID,
                Name = screenA.Name,
                OutPutPort = screenA.OutPutPort,
                GridCol = screenA.GridCol,
                GridRow = screenA.GridRow,
                WidthPx = screenA.WidthPx,
                HeightPx = screenA.HeightPx,
                Resolution = screenA.Resolution,
                ControllerId = conn.DeviceKey,
            });
        }
        if (screenB != null)
        {
            screens.Add(new WpfDto.VwScreenDto
            {
                ID = screenB.ID,
                Name = screenB.Name,
                OutPutPort = screenB.OutPutPort,
                GridCol = screenB.GridCol,
                GridRow = screenB.GridRow,
                WidthPx = screenB.WidthPx,
                HeightPx = screenB.HeightPx,
                Resolution = screenB.Resolution,
                ControllerId = conn.DeviceKey,
            });
        }
        if (screens.Count > 0)
        {
            VwLocalScreenStore.Save(conn.DeviceKey, screens);
        }

        conn.ProbeResult = new WpfDto.VwProbeDeviceOutput
        {
            ControllerId = conn.DeviceKey,
            MaxWindowNums = 512,
            MaxSceneNums = 128,
            InputChannels = new List<WpfDto.VwISAPIInputChannel>
            {
                new() { Id = source?.SignalNo ?? 1, Name = source?.Name ?? "Camera 01" }
            },
            Outputs = new List<WpfDto.VwISAPIOutputItem>
            {
                new() { Id = 1, OutputId = 1 },
                new() { Id = 2, OutputId = 2 }
            }
        };

        return conn;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Tab Kết nối: Ping + Probe qua MockServer.
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Author: Đạt
    /// Description: Bấm "Kết nối" trên ViewModel -> POST vwdevicesetup/ping -> backend digest-auth vào
    ///              MockServer -> thành công. Đồng thời khẳng định MỘT lệnh chỉ sinh MỘT dòng log
    ///              (regression lỗi log trùng: trước đây ApiInvoker + PublishStep cùng phát 2 dòng).
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfConnection_Connect_PingsMockDeviceAndLogsExactlyOneRow_Test()
    {
        const int port = 18111;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        // Arrange
        var (controller, _, _, _) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
        connection.AdHocPort = port;
        stack.Publisher.Clear();

        // Act
        await connection.ConnectCommand.ExecuteAsync(null);

        // Assert
        Assert.True(connection.IsConnected, connection.StatusMessage);
        Assert.Contains("thành công", connection.StatusMessage);

        Assert.Equal(1, stack.Publisher.TotalLogRows);
        Assert.Empty(stack.Publisher.ActivityRows);
        var step = Assert.Single(stack.Publisher.DeviceStepRows);
        Assert.True(step.Step.Success);
        Assert.NotEmpty(mockServer.ReceivedRequests);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Sai mật khẩu thiết bị -> Ping thất bại nhưng KHÔNG ném exception lên UI: ViewModel
    ///              phải hiện thông báo lỗi và bảng log phải có đúng dấu vết, không im lặng.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfConnection_Connect_WithWrongDeviceCredentials_ReportsFailureWithoutThrowing_Test()
    {
        const int port = 18112;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);
        mockServer.VerifyDigestResponseHash = true;

        // Arrange — mật khẩu sai so với MockServer.
        var (controller, _, _, _) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
        connection.AdHocPort = port;
        connection.AdHocPassword = "SaiMatKhau!";
        stack.Publisher.Clear();

        // Act
        await connection.ConnectCommand.ExecuteAsync(null);

        // Assert
        Assert.False(connection.IsConnected);
        Assert.True(stack.Publisher.TotalLogRows >= 1, "Thất bại phải để lại dấu vết trên bảng log.");
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Bấm "Kết nối" trên ViewModel -> Ping trực tiếp tới MockServer thành công.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfConnection_Connect_PingsMockDeviceSuccessfully_Test()
    {
        const int port = 18113;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        // Arrange
        var (controller, _, _, _) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
        connection.AdHocPort = port;

        // Act
        await connection.ConnectCommand.ExecuteAsync(null);

        // Assert
        Assert.True(connection.IsConnected);
        Assert.Contains("Kết nối trực tiếp thành công", connection.StatusMessage);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Tab Thiết lập Cảnh: nạp màn hình, tạo kịch bản, dựng cửa sổ, kích hoạt.
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Author: Đạt
    /// Description: Luồng đầy đủ chế độ "từng cổng ra": nạp màn hình/nguồn -> tạo kịch bản -> mỗi màn
    ///              hình một cửa sổ phủ kín -> đọc lại thông tin -> kích hoạt. Màn hình B cố ý thiếu
    ///              WidthPx/HeightPx nên phải tự suy ra từ Resolution.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_IndividualOutputs_CreatesOneWindowPerScreenThenActivates_Test()
    {
        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var confirmation = new UserConfirmationTest(answer: false);
        var sceneSetup = new SceneSetupViewModel(stack.ActivityPublisher, connection, confirmation, stack.Publisher)
        {
            SceneName = $"Canh WPF {Guid.NewGuid():N}",
            SceneOutputId = "1",
            GridCols = 2,
            GridRows = 1
        };

        // Act 1 — nạp màn hình + nguồn từ direct probe & local screen store
        await sceneSetup.LoadScreensAndSourcesCommand.ExecuteAsync(null);

        Assert.Equal(2, sceneSetup.ScreenAssignments.Count);
        Assert.Contains(sceneSetup.Sources, item => item.SignalNo == source.SignalNo);

        Assert.All(sceneSetup.ScreenAssignments, row =>
        {
            Assert.Equal(1920, row.Width);
            Assert.Equal(1080, row.Height);
        });

        foreach (var row in sceneSetup.ScreenAssignments)
            row.SelectedSource = sceneSetup.Sources.First(item => item.SignalNo == source.SignalNo);

        // Act 2 — tạo kịch bản
        await sceneSetup.CreateSceneCommand.ExecuteAsync(null);

        Assert.NotNull(sceneSetup.CurrentScene);
        Assert.False(string.IsNullOrWhiteSpace(sceneSetup.CurrentScene!.ID), sceneSetup.StatusMessage);

        var sceneInStore = VwLocalSceneStore.ListScenes(connection.DeviceKey)
            .FirstOrDefault(item => item.Code == sceneSetup.CurrentScene.Code);
        Assert.NotNull(sceneInStore);

        // Act 3 — dựng một cửa sổ phủ kín cho mỗi màn hình
        await sceneSetup.ApplyIndividualWindowsCommand.ExecuteAsync(null);

        // Assert — Store có đúng 2 cửa sổ, mỗi cửa sổ nằm trên ĐÚNG màn hình của nó
        var windows = VwLocalSceneStore.ListWindowScenes(connection.DeviceKey, sceneSetup.CurrentScene.ID);

        Assert.Equal(2, windows.Count);
        Assert.All(windows, window =>
        {
            Assert.Equal(1920, window.W);
            Assert.Equal(1080, window.H);
            Assert.Equal(1, window.ZIndex);
            Assert.Equal(source.SignalNo.ToString(), window.SourceId);
        });

        var expectedX = new[]
        {
            screenA.GridCol!.Value * VwWallProfile.PanelWidthPx,
            screenB.GridCol!.Value * VwWallProfile.PanelWidthPx,
        }.OrderBy(value => value).ToList();

        var actualX = windows.Select(window => window.X ?? -1).OrderBy(value => value).ToList();
        Assert.Equal(expectedX, actualX);

        var expectedY = screenA.GridRow!.Value * VwWallProfile.PanelHeightPx;
        Assert.All(windows, window => Assert.Equal(expectedY, window.Y));

        Assert.Equal(2, actualX.Distinct().Count());

        // Act 4 — đọc lại thông tin kịch bản qua store
        await sceneSetup.LoadSceneWindowsCommand.ExecuteAsync(null);
        Assert.Equal(2, sceneSetup.SceneWindows.Count);

        // Act 5 — kích hoạt kịch bản
        await sceneSetup.ActivateSceneCommand.ExecuteAsync(null);

        var activated = VwLocalSceneStore.GetActiveScene(connection.DeviceKey);
        Assert.NotNull(activated);
        Assert.Equal(sceneSetup.CurrentScene.ID, activated.ID);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Chế độ "cửa sổ xếp lớp": nhiều cửa sổ chồng nhau trên CÙNG vùng, khác ZIndex —
    ///              cùng entity VwWindowScene, chỉ khác hình học và ZIndex.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_OverlappingWindows_CreatesStackedWindowsWithDistinctZIndex_Test()
    {
        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var sceneSetup = new SceneSetupViewModel(
            stack.ActivityPublisher,
            connection,
            new UserConfirmationTest(answer: false),
            stack.Publisher)
        {
            SceneName = $"Canh Xep Lop {Guid.NewGuid():N}",
            SceneOutputId = "2",
            GridCols = 1,
            GridRows = 1,
            IsIndividualMode = false
        };

        await sceneSetup.LoadScreensAndSourcesCommand.ExecuteAsync(null);
        await sceneSetup.CreateSceneCommand.ExecuteAsync(null);
        Assert.NotNull(sceneSetup.CurrentScene);

        var pickedSource = sceneSetup.Sources.First(item => item.SignalNo == source.SignalNo);

        sceneSetup.AddWindowRowCommand.Execute(null);
        sceneSetup.AddWindowRowCommand.Execute(null);
        Assert.Equal(2, sceneSetup.WindowRows.Count);

        for (var index = 0; index < sceneSetup.WindowRows.Count; index++)
        {
            var row = sceneSetup.WindowRows[index];
            row.SelectedSource = pickedSource;
            row.X = 100 * index;
            row.Y = 100 * index;
            row.Width = 960;
            row.Height = 540;
        }

        // Act
        await sceneSetup.ApplyOverlappingWindowsCommand.ExecuteAsync(null);

        // Assert
        var windows = VwLocalSceneStore.ListWindowScenes(connection.DeviceKey, sceneSetup.CurrentScene!.ID!);

        Assert.Equal(2, windows.Count);
        Assert.Equal(2, windows.Select(item => item.ZIndex).Distinct().Count());
        Assert.All(windows, window =>
        {
            Assert.Equal(960, window.W);
            Assert.Equal(540, window.H);
        });
    }

    private async Task<SceneSetupViewModel> CreateSceneWithWindowsAsync(
        VwWpfClientStackTest stack,
        ConnectionViewModel connection,
        UserConfirmationTest confirmation,
        VwSource source)
    {
        VwLocalSceneStore.SaveData(connection.DeviceKey, new VwLocalSceneData());
        var sceneSetup = new SceneSetupViewModel(stack.ActivityPublisher, connection, confirmation, stack.Publisher)
        {
            SceneName = $"Canh Sua {Guid.NewGuid():N}",
            SceneOutputId = "1",
            GridCols = 2,
            GridRows = 1
        };

        await sceneSetup.LoadScreensAndSourcesCommand.ExecuteAsync(null);
        foreach (var row in sceneSetup.ScreenAssignments)
            row.SelectedSource = sceneSetup.Sources.First(item => item.SignalNo == source.SignalNo);

        await sceneSetup.CreateSceneCommand.ExecuteAsync(null);
        await sceneSetup.ApplyIndividualWindowsCommand.ExecuteAsync(null);
        await sceneSetup.LoadSceneWindowsCommand.ExecuteAsync(null);

        Assert.NotNull(sceneSetup.CurrentScene);
        Assert.Equal(2, sceneSetup.SceneWindows.Count);
        return sceneSetup;
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Đổi tên kịch bản phải LƯU ĐƯỢC XUỐNG STORE.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_UpdateScene_PersistsRenameServerSide_Test()
    {
        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var sceneSetup = await CreateSceneWithWindowsAsync(stack, connection, new UserConfirmationTest(false), source);
        var sceneId = sceneSetup.CurrentScene!.ID!;

        var renamed = $"Canh Da Doi Ten {Guid.NewGuid():N}";
        sceneSetup.SceneName = renamed;
        sceneSetup.GridCols = 4;
        sceneSetup.GridRows = 2;

        // Act
        await sceneSetup.UpdateSceneCommand.ExecuteAsync(null);

        // Assert — đọc lại từ Store
        var inStore = VwLocalSceneStore.ListScenes(connection.DeviceKey).FirstOrDefault(item => item.ID == sceneId);
        Assert.NotNull(inStore);
        Assert.Equal(renamed, inStore.Name);
        Assert.Equal(4, inStore.GridCols);
        Assert.Equal(2, inStore.GridRows);

        // Combobox phải hiện tên mới ngay
        Assert.Equal(renamed, sceneSetup.CurrentScene!.Name);
        Assert.Contains(renamed, sceneSetup.CurrentSceneSummary);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Sửa toạ độ/kích thước một cửa sổ ĐÃ LƯU rồi bấm Lưu — phải ghi xuống store.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_UpdateSceneWindow_PersistsGeometryServerSide_Test()
    {
        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var sceneSetup = await CreateSceneWithWindowsAsync(stack, connection, new UserConfirmationTest(false), source);

        var target = sceneSetup.SceneWindows[0];
        var targetId = target.ID!;
        target.X = 640;
        target.Y = 360;
        target.W = 1280;
        target.H = 720;
        target.ZIndex = 5;

        // Act
        await sceneSetup.UpdateSceneWindowCommand.ExecuteAsync(target);

        // Assert
        var inStore = VwLocalSceneStore.ListWindowScenes(connection.DeviceKey, sceneSetup.CurrentScene!.ID!)
            .FirstOrDefault(item => item.ID == targetId);
        Assert.NotNull(inStore);
        Assert.Equal(640, inStore.X);
        Assert.Equal(360, inStore.Y);
        Assert.Equal(1280, inStore.W);
        Assert.Equal(720, inStore.H);
        Assert.Equal(5, inStore.ZIndex);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Từ chối lưu khi kích thước không hợp lệ (W/H phải > 0) — chặn ngay ở client.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_UpdateSceneWindow_WithInvalidSize_RejectsWithoutSaving_Test()
    {
        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var sceneSetup = await CreateSceneWithWindowsAsync(stack, connection, new UserConfirmationTest(false), source);

        var target = sceneSetup.SceneWindows[0];
        var targetId = target.ID!;
        target.W = 0;

        // Act
        await sceneSetup.UpdateSceneWindowCommand.ExecuteAsync(target);

        // Assert — Store giữ nguyên 1920
        var inStore = VwLocalSceneStore.ListWindowScenes(connection.DeviceKey, sceneSetup.CurrentScene!.ID!)
            .First(item => item.ID == targetId);
        Assert.Equal(1920, inStore.W);
        Assert.Contains("W, H > 0", sceneSetup.StatusMessage);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Xoá một cửa sổ phải mất THẬT ở store — kiểm cả bộ sưu tập trên UI, store,
    ///              và một lượt LoadSceneWindows mới (không phải chỉ bỏ khỏi collection cục bộ).
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_DeleteSceneWindow_RemovesWindowServerSide_Test()
    {
        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var confirmation = new UserConfirmationTest(answer: true);
        var sceneSetup = await CreateSceneWithWindowsAsync(stack, connection, confirmation, source);

        var doomed = sceneSetup.SceneWindows[0];
        var doomedId = doomed.ID!;
        var sceneId = sceneSetup.CurrentScene!.ID!;

        // Act
        await sceneSetup.DeleteSceneWindowCommand.ExecuteAsync(doomed);

        // Assert — có hỏi xác nhận, UI còn 1 cửa sổ
        Assert.Equal(1, confirmation.CallCount);
        Assert.Single(sceneSetup.SceneWindows);
        Assert.DoesNotContain(sceneSetup.SceneWindows, item => item.ID == doomedId);

        // Store: đã xoá
        var inStore = VwLocalSceneStore.ListWindowScenes(connection.DeviceKey, sceneId)
            .FirstOrDefault(item => item.ID == doomedId);
        Assert.Null(inStore);

        // Lượt đọc lại từ store cũng không còn
        await sceneSetup.LoadSceneWindowsCommand.ExecuteAsync(null);
        Assert.Single(sceneSetup.SceneWindows);

        var remaining = VwLocalSceneStore.ListWindowScenes(connection.DeviceKey, sceneId).Count;
        Assert.Equal(1, remaining);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Xoá nhiều cửa sổ: khi chưa tick chọn cửa sổ nào thì hiển thị thông báo nhắc nhở.
    /// Created date: 30/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_DeleteSelectedSceneWindows_WhenNoSelection_ShowsMessage_Test()
    {
        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var confirmation = new UserConfirmationTest(answer: true);
        var sceneSetup = await CreateSceneWithWindowsAsync(stack, connection, confirmation, source);

        // Act
        await sceneSetup.DeleteSelectedSceneWindowsCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(0, confirmation.CallCount);
        Assert.Contains("ít nhất", sceneSetup.StatusMessage);
        Assert.Equal(2, sceneSetup.SceneWindows.Count);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Multi-select & Xoá hàng loạt: chọn tất cả và xoá sạch toàn bộ cửa sổ đã chọn.
    /// Created date: 30/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_DeleteSelectedSceneWindows_RemovesAllSelected_Test()
    {
        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var confirmation = new UserConfirmationTest(answer: true);
        var sceneSetup = await CreateSceneWithWindowsAsync(stack, connection, confirmation, source);
        var sceneId = sceneSetup.CurrentScene!.ID!;

        // Act: Chọn tất cả
        sceneSetup.IsAllSceneWindowsSelected = true;
        Assert.True(sceneSetup.SceneWindows.All(w => w.IsSelected));

        await sceneSetup.DeleteSelectedSceneWindowsCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(1, confirmation.CallCount);
        Assert.Empty(sceneSetup.SceneWindows);
        var remaining = VwLocalSceneStore.ListWindowScenes(connection.DeviceKey, sceneId);
        Assert.Empty(remaining);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Multi-select & Xoá chỉ các mục được chọn: giữ lại các mục không tick.
    /// Created date: 30/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_DeleteSelectedSceneWindows_RemovesOnlySelected_Test()
    {
        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var confirmation = new UserConfirmationTest(answer: true);
        var sceneSetup = await CreateSceneWithWindowsAsync(stack, connection, confirmation, source);
        var sceneId = sceneSetup.CurrentScene!.ID!;

        var doomedId = sceneSetup.SceneWindows[0].ID!;
        var keptId = sceneSetup.SceneWindows[1].ID!;

        // Act: Chỉ chọn mục đầu tiên
        sceneSetup.SceneWindows[0].IsSelected = true;
        sceneSetup.SceneWindows[1].IsSelected = false;

        await sceneSetup.DeleteSelectedSceneWindowsCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(1, confirmation.CallCount);
        Assert.Single(sceneSetup.SceneWindows);
        Assert.Equal(keptId, sceneSetup.SceneWindows[0].ID);

        var remaining = VwLocalSceneStore.ListWindowScenes(connection.DeviceKey, sceneId);
        Assert.Single(remaining);
        Assert.Equal(keptId, remaining[0].ID);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Multi-select & Xoá hàng loạt: khi người dùng bấm KHÔNG ở hộp thoại xác nhận,
    ///              toàn bộ cửa sổ phải còn nguyên vẹn trong store và UI.
    /// Created date: 30/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_DeleteSelectedSceneWindows_WhenCancelled_LeavesWindowsUntouched_Test()
    {
        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var confirmation = new UserConfirmationTest(answer: false);
        var sceneSetup = await CreateSceneWithWindowsAsync(stack, connection, confirmation, source);
        var sceneId = sceneSetup.CurrentScene!.ID!;

        // Act: Chọn tất cả rồi bấm xoá nhưng từ chối xác nhận
        sceneSetup.IsAllSceneWindowsSelected = true;
        await sceneSetup.DeleteSelectedSceneWindowsCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(1, confirmation.CallCount);
        Assert.Contains("huỷ", sceneSetup.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, sceneSetup.SceneWindows.Count);

        var remaining = VwLocalSceneStore.ListWindowScenes(connection.DeviceKey, sceneId);
        Assert.Equal(2, remaining.Count);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Toggle IsAllSceneWindowsSelected đồng bộ trạng thái IsSelected của toàn bộ các dòng.
    /// Created date: 30/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_IsAllSceneWindowsSelected_TogglesAllRows_Test()
    {
        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var confirmation = new UserConfirmationTest(answer: true);
        var sceneSetup = await CreateSceneWithWindowsAsync(stack, connection, confirmation, source);

        Assert.Equal(2, sceneSetup.SceneWindows.Count);
        Assert.True(sceneSetup.SceneWindows.All(w => !w.IsSelected));

        // Act 1: Chọn tất cả
        sceneSetup.IsAllSceneWindowsSelected = true;
        Assert.True(sceneSetup.SceneWindows.All(w => w.IsSelected));

        // Act 2: Bỏ chọn tất cả
        sceneSetup.IsAllSceneWindowsSelected = false;
        Assert.True(sceneSetup.SceneWindows.All(w => !w.IsSelected));
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Bấm Xoá kịch bản rồi CHỌN KHÔNG ở hộp thoại — kịch bản phải còn nguyên.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_DeleteScene_WhenCancelled_LeavesSceneUntouched_Test()
    {
        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var confirmation = new UserConfirmationTest(answer: false);
        var sceneSetup = await CreateSceneWithWindowsAsync(stack, connection, confirmation, source);
        var sceneId = sceneSetup.CurrentScene!.ID!;

        // Act
        await sceneSetup.DeleteSceneCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(1, confirmation.CallCount);
        Assert.NotNull(sceneSetup.CurrentScene);
        Assert.Contains("huỷ", sceneSetup.StatusMessage, StringComparison.OrdinalIgnoreCase);

        var stillThere = VwLocalSceneStore.ListScenes(connection.DeviceKey).FirstOrDefault(item => item.ID == sceneId);
        Assert.NotNull(stillThere);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Xác nhận Xoá kịch bản — store xoá, và giao diện bỏ kịch bản khỏi dropdown,
    ///              xoá danh sách cửa sổ, bỏ chọn kịch bản đang làm việc.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_DeleteScene_WhenConfirmed_SoftDeletesAndClearsSelection_Test()
    {
        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var confirmation = new UserConfirmationTest(answer: true);
        var sceneSetup = await CreateSceneWithWindowsAsync(stack, connection, confirmation, source);
        var scene = sceneSetup.CurrentScene!;
        var sceneId = scene.ID!;

        Assert.Contains(sceneSetup.Scenes, item => item.ID == sceneId);

        // Act
        await sceneSetup.DeleteSceneCommand.ExecuteAsync(null);

        // Assert — giao diện
        Assert.Equal(1, confirmation.CallCount);
        Assert.Null(sceneSetup.CurrentScene);
        Assert.Empty(sceneSetup.SceneWindows);
        Assert.DoesNotContain(sceneSetup.Scenes, item => item.ID == sceneId);

        // Assert — store đã xoá
        var inStore = VwLocalSceneStore.ListScenes(connection.DeviceKey).FirstOrDefault(item => item.ID == sceneId);
        Assert.Null(inStore);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Đẩy xuống thiết bị — chỉ DryRun (mặc định an toàn).
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Author: Đạt
    /// Description: PushToDevice ở chế độ DryRun mặc định: chạy trọn kịch bản khảo sát + dựng payload
    ///              xuống MockServer nhưng KHÔNG phát lệnh ghi, và KHÔNG hỏi xác nhận (hộp thoại chỉ
    ///              xuất hiện khi DryRun = false).
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_PushToDevice_WithDryRun_RunsWithoutConfirmationOrDeviceWrites_Test()
    {
        const int port = 18114;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        connection.AdHocPort = port;
        var confirmation = new UserConfirmationTest(answer: false);
        var sceneSetup = new SceneSetupViewModel(stack.ActivityPublisher, connection, confirmation, stack.Publisher)
        {
            SceneName = $"Canh DryRun {Guid.NewGuid():N}",
            SceneOutputId = "1",
            GridCols = 2,
            GridRows = 1
        };

        await sceneSetup.LoadScreensAndSourcesCommand.ExecuteAsync(null);
        foreach (var row in sceneSetup.ScreenAssignments)
            row.SelectedSource = sceneSetup.Sources.First(item => item.SignalNo == source.SignalNo);

        await sceneSetup.CreateSceneCommand.ExecuteAsync(null);
        await sceneSetup.ApplyIndividualWindowsCommand.ExecuteAsync(null);

        Assert.True(sceneSetup.DryRun, "DryRun phải là mặc định an toàn.");

        mockServer.ResetDefaults();
        stack.Publisher.Clear();

        // Act
        await sceneSetup.PushToDeviceCommand.ExecuteAsync(null);

        // Assert — không hỏi xác nhận, không có lệnh ghi cửa sổ xuống thiết bị
        Assert.Equal(0, confirmation.CallCount);
        Assert.Equal(0, mockServer.AddWindowCallCount);
        Assert.Equal(0, mockServer.SaveSceneDataCallCount);

        // Mỗi bước ISAPI đúng một dòng log, không có dòng chung cộng thêm
        Assert.True(stack.Publisher.DeviceStepRows.Count > 0, sceneSetup.StatusMessage);
        Assert.Empty(stack.Publisher.ActivityRows);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Tab Tham số — đọc thông số bộ điều khiển + màn hình.
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Author: Đạt
    /// Description: Tab Tham số nạp được danh sách màn hình của đúng bộ điều khiển đang chọn qua API thật.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfParameters_Load_ReadsScreensOfSelectedController_Test()
    {
        // Arrange
        var (controller, _, _, _) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
        var parameters = new ParametersViewModel(connection);

        // Act
        await parameters.LoadCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("Chế độ trực tiếp", parameters.StatusMessage);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Kết nối trực tiếp Ping tới thiết bị MockServer thành công.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfConnection_DirectConnect_PingsDeviceSuccessfully_Test()
    {
        const int port = 18115;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        // Arrange
        var (controller, _, _, _) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = new ConnectionViewModel(stack.ActivityPublisher, stack.Publisher, new UserConfirmationTest(true))
        {
            AdHocIp = controller.IP ?? "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
        };

        // Act
        await connection.ConnectCommand.ExecuteAsync(null);

        // Assert
        Assert.True(connection.IsConnected);
        Assert.Contains("Kết nối trực tiếp thành công", connection.StatusMessage);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Kiểm tra EventTriggerLogEntry và DataGridCheckBoxColumn của tab Log:
    ///              HasDetails là getter-only (read-only), cột phải có IsReadOnly = true và Mode = OneWay
    ///              để không ném InvalidOperationException khi WPF DataGrid khởi tạo binding.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public void VwWpfLogEntry_HasDetails_OneWayBinding_DoesNotThrow_Test()
    {
        var activity = new Activity(DateTime.UtcNow, "TestStage", "TestDetail", ActivityLevel.Info);
        var entryWithoutStep = new EventTriggerLogEntry(activity);
        Assert.False(entryWithoutStep.HasDetails);
        Assert.Null(entryWithoutStep.Step);

        var step = new WpfDto.VwSetupSceneStep
        {
            Name = "Step1",
            Success = true,
            RequestXml = "<test/>",
            ResponseXml = "<response/>"
        };
        var entryWithStep = new EventTriggerLogEntry(activity, step);
        Assert.True(entryWithStep.HasDetails);
        Assert.Same(step, entryWithStep.Step);
        Assert.Equal("TestStage", entryWithStep.Stage);
        Assert.Equal("TestDetail", entryWithStep.Detail);

        // Khởi tạo DataGridCheckBoxColumn với Mode=OneWay và IsReadOnly=true trên luồng STA
        var thread = new Thread(() =>
        {
            var column = new System.Windows.Controls.DataGridCheckBoxColumn
            {
                Header = "Có XML",
                Width = 65,
                IsReadOnly = true,
                Binding = new System.Windows.Data.Binding(nameof(EventTriggerLogEntry.HasDetails))
                {
                    Mode = System.Windows.Data.BindingMode.OneWay
                }
            };

            var grid = new System.Windows.Controls.DataGrid();
            grid.Columns.Add(column);
            grid.ItemsSource = new[] { entryWithoutStep, entryWithStep };

            Assert.Equal(System.Windows.Data.BindingMode.OneWay, ((System.Windows.Data.Binding)column.Binding).Mode);
            Assert.True(column.IsReadOnly);
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    [Fact]
    public void VwWpfConnectionViewModel_IsapiPresets_SelectionSetsMethodAndPath_Test()
    {
        var stack = BuildClientStack();
        var vm = new ConnectionViewModel(stack.ActivityPublisher, stack.Publisher, new UserConfirmationTest(true));

        Assert.NotEmpty(vm.IsapiPresets);
        Assert.Contains(vm.IsapiPresets, p => p.Section == "9.7.5.6" && p.Method == "GET");
        Assert.Contains(vm.IsapiPresets, p => p.Section == "9.7.8.1" && p.Method == "PUT");

        var preset = vm.IsapiPresets.First(p => p.Section == "9.7.8.1");
        vm.SelectedIsapiPreset = preset;

        Assert.Equal("PUT", vm.IsapiMethod);
        Assert.Equal("ISAPI/DisplayDev/ScreenCtrl/closeAll", vm.IsapiPath);
    }

    [Fact]
    public async Task VwWpfSceneSetup_SeedSampleScenesCommand_PopulatesPresetScenesAndWindows_Test()
    {
        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var confirmation = new UserConfirmationTest(answer: false);
        var sceneSetup = new SceneSetupViewModel(stack.ActivityPublisher, connection, confirmation, stack.Publisher);

        // Act: Click "Nạp 3 Scene mẫu"
        await sceneSetup.SeedSampleScenesCommand.ExecuteAsync(null);

        // Assert: 3 sample scenes are populated and Scene 1 is selected
        Assert.True(sceneSetup.Scenes.Count >= 3);
        Assert.Contains(sceneSetup.Scenes, s => s.Code == "VWSCENE_SAMPLE_01");
        Assert.Contains(sceneSetup.Scenes, s => s.Code == "VWSCENE_SAMPLE_02");
        Assert.Contains(sceneSetup.Scenes, s => s.Code == "VWSCENE_SAMPLE_03");
        Assert.NotNull(sceneSetup.CurrentScene);
        Assert.Equal("VWSCENE_SAMPLE_01", sceneSetup.CurrentScene.Code);
        Assert.Equal(12, sceneSetup.SceneWindows.Count);

        // Switch to Scene 2: Ban đêm (Bản đồ sự cố & 4 Trạm thu phí)
        sceneSetup.CurrentScene = sceneSetup.Scenes.First(s => s.Code == "VWSCENE_SAMPLE_02");
        Assert.Equal(5, sceneSetup.SceneWindows.Count);
        Assert.Contains(sceneSetup.SceneWindows, w => w.W == 1280 && w.H == 1080);
        Assert.Equal(4, sceneSetup.SceneWindows.Count(w => w.W == 640 && w.H == 270));

        // Switch to Scene 3: Khẩn cấp (Full màn hình tai nạn)
        sceneSetup.CurrentScene = sceneSetup.Scenes.First(s => s.Code == "VWSCENE_SAMPLE_03");
        Assert.Single(sceneSetup.SceneWindows);
        Assert.Equal(1920, sceneSetup.SceneWindows[0].W);
        Assert.Equal(1080, sceneSetup.SceneWindows[0].H);
    }

    [Fact]
    public void VwWpfSceneSetup_StartAndCancelCreateNewScene_RollsBackState_Test()
    {
        // Arrange
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var confirmation = new UserConfirmationTest(answer: false);
        var sceneSetup = new SceneSetupViewModel(stack.ActivityPublisher, connection, confirmation, stack.Publisher);

        var initialScene = sceneSetup.CurrentScene;
        Assert.NotNull(initialScene);
        Assert.False(sceneSetup.IsCreatingNewScene);

        // Act 1: Click "Tạo mới"
        sceneSetup.StartCreateNewSceneCommand.Execute(null);

        // Assert 1: Inputs are cleared and in creating state
        Assert.True(sceneSetup.IsCreatingNewScene);
        Assert.Empty(sceneSetup.SceneName);
        Assert.Null(sceneSetup.GridCols);
        Assert.Null(sceneSetup.GridRows);
        Assert.Contains("Đang tạo kịch bản mới", sceneSetup.CurrentSceneSummary);

        // Act 2: Click "Huỷ"
        sceneSetup.CancelCreateNewSceneCommand.Execute(null);

        // Assert 2: Rolled back to previous selected scene
        Assert.False(sceneSetup.IsCreatingNewScene);
        Assert.Equal(initialScene.ID, sceneSetup.CurrentScene?.ID);
        Assert.Equal(initialScene.Name, sceneSetup.SceneName);
        Assert.Contains($"Đang chọn: {initialScene.Name}", sceneSetup.CurrentSceneSummary);
    }

    [Fact]
    public void VwWpfMainWindow_GridSplitterLayout_IsValidAndDraggable_Test()
    {
        RunOnStaThread(() =>
        {
            var (controller, screenA, screenB, source) = SeedWallAsync();
            var stack = BuildClientStack();
            var connection = BuildConnection(stack, controller, screenA, screenB, source);
            var mainVm = BuildMainViewModel(stack, connection);

            var window = new MainWindow(mainVm);
            var mainGrid = window.Content as System.Windows.Controls.Grid;
            Assert.NotNull(mainGrid);

            // Row 1 (TabControl) and Row 3 (Logs) must be Star rows to allow resizing
            Assert.True(mainGrid.RowDefinitions[1].Height.IsStar);
            Assert.True(mainGrid.RowDefinitions[3].Height.IsStar);

            // Row 2 (GridSplitter) must have fixed pixel height >= 3, not Auto
            Assert.True(mainGrid.RowDefinitions[2].Height.IsAbsolute);
            Assert.True(mainGrid.RowDefinitions[2].Height.Value >= 3);

            // Find GridSplitter at Row 2
            var splitters = mainGrid.Children.OfType<System.Windows.Controls.GridSplitter>().ToList();
            var logSplitter = splitters.FirstOrDefault(s => System.Windows.Controls.Grid.GetRow(s) == 2);
            Assert.NotNull(logSplitter);
            Assert.Equal(System.Windows.Controls.GridResizeBehavior.PreviousAndNext, logSplitter.ResizeBehavior);
            Assert.Equal(System.Windows.Controls.GridResizeDirection.Rows, logSplitter.ResizeDirection);
            Assert.True(logSplitter.ShowsPreview);
        });
    }

    [Fact]
    public void VwWpfSceneSetupTabView_SingleUnifiedGridLayout_IsValid_Test()
    {
        RunOnStaThread(() =>
        {
            var view = new SceneSetupTabView();
            var rootGrid = view.Content as System.Windows.Controls.Grid;
            Assert.NotNull(rootGrid);

            // Row 1 (Khung 2: Bố cục ô Camera trên Tường) is Star with MinHeight >= 200
            Assert.True(rootGrid.RowDefinitions[1].Height.IsStar);
            Assert.True(rootGrid.RowDefinitions[1].MinHeight >= 200);

            // Verify single unified DataGrid exists in Row 1
            var frame2Border = rootGrid.Children.OfType<System.Windows.Controls.Border>().FirstOrDefault(b => System.Windows.Controls.Grid.GetRow(b) == 1);
            Assert.NotNull(frame2Border);
            var innerGrid = frame2Border.Child as System.Windows.Controls.Grid;
            Assert.NotNull(innerGrid);
            var dataGrids = FindVisualChildren<System.Windows.Controls.DataGrid>(innerGrid).ToList();
            Assert.NotEmpty(dataGrids);
        });
    }

    [Fact]
    public void VwWpfSceneSetupTabView_HeaderAndBorders_LayoutStructure_Test()
    {
        RunOnStaThread(() =>
        {
            var view = new SceneSetupTabView();
            var rootGrid = view.Content as System.Windows.Controls.Grid;
            Assert.NotNull(rootGrid);

            // Khung 1 (Row 0): Check Header + Scene ComboBox on the same first WrapPanel
            var frame1Border = rootGrid.Children.OfType<System.Windows.Controls.Border>().FirstOrDefault(b => System.Windows.Controls.Grid.GetRow(b) == 0);
            Assert.NotNull(frame1Border);
            var frame1Stack = frame1Border.Child as System.Windows.Controls.StackPanel;
            Assert.NotNull(frame1Stack);

            var firstWrapPanel = frame1Stack.Children.OfType<System.Windows.Controls.WrapPanel>().FirstOrDefault();
            Assert.NotNull(firstWrapPanel);

            // Verify Title, Create button, and ComboBox are all children of the first WrapPanel
            var textBlocks = firstWrapPanel.Children.OfType<System.Windows.Controls.TextBlock>().ToList();
            Assert.Contains(textBlocks, tb => tb.Text.Contains("Thiết lập Kịch bản"));
            var buttons = firstWrapPanel.Children.OfType<System.Windows.Controls.Button>().ToList();
            Assert.Contains(buttons, b => b.Content?.ToString()?.Contains("Tạo mới") == true);
            var stackPanels = firstWrapPanel.Children.OfType<System.Windows.Controls.StackPanel>().ToList();
            Assert.Contains(stackPanels, sp => sp.Children.OfType<System.Windows.Controls.Grid>().Any(g => g.Children.OfType<System.Windows.Controls.ComboBox>().Any()));

            // Khung 2 (Row 1): Single unified Camera grid border
            var frame2Border = rootGrid.Children.OfType<System.Windows.Controls.Border>().FirstOrDefault(b => System.Windows.Controls.Grid.GetRow(b) == 1);
            Assert.NotNull(frame2Border);
            Assert.Equal(4, frame2Border.CornerRadius.TopLeft);
            Assert.Equal(4, frame2Border.CornerRadius.BottomRight);
        });
    }

    [Fact]
    public void VwWpfSceneSetupTabView_NoSeedSampleScenesButton_AutomaticSeedingOnStartup_Test()
    {
        RunOnStaThread(() =>
        {
            var view = new SceneSetupTabView();
            var allButtons = FindVisualChildren<System.Windows.Controls.Button>(view).ToList();

            // Verify there is NO "Nạp 3 Scene mẫu" button in the view
            Assert.DoesNotContain(allButtons, b => b.Content?.ToString()?.Contains("Nạp") == true);
            Assert.DoesNotContain(allButtons, b => b.Content?.ToString()?.Contains("mẫu") == true);

            // Verify ViewModel auto-seeds default scenes on startup
            var (controller, screenA, screenB, source) = SeedWallAsync();
            var stack = BuildClientStack();
            var connection = BuildConnection(stack, controller, screenA, screenB, source);
            var freshKey = $"10.99.{Random.Shared.Next(10, 99)}.{Random.Shared.Next(10, 99)}";
            connection.AdHocIp = freshKey;
            var sceneVm = new SceneSetupViewModel(stack.ActivityPublisher, connection, new UserConfirmationTest(true), stack.Publisher);

            Assert.True(sceneVm.Scenes.Count >= 3);
            Assert.NotNull(sceneVm.CurrentScene);
            Assert.True(sceneVm.SceneWindows.Count > 0);
        });
    }

    [Fact]
    public void VwWpfSceneSetupTabView_CameraDropdown_And_SizePreset_UpdatesValues_Test()
    {
        var row = new SceneWindowRow(new WpfDto.VwWindowSceneDto
        {
            ID = "win-1",
            Name = "Ô 1",
            SourceId = "src-1",
            W = 1920,
            H = 1080,
            ZIndex = 1
        });

        Assert.Equal("Toàn màn (1x1)", row.SizeLabel);
        Assert.NotNull(row.SelectedSizePreset);
        Assert.Equal("1x1 (1 Màn)", row.SelectedSizePreset.Name);

        // Test changing size preset to 2x2
        var preset2x2 = SceneWindowRow.AvailableSizePresets.First(p => p.Name.Contains("2x2"));
        row.SelectedSizePreset = preset2x2;
        Assert.Equal(3840, row.W);
        Assert.Equal(2160, row.H);
        Assert.Equal("Khối lớn (2x2)", row.SizeLabel);

        var newSource = new WpfDto.VwSourceDto
        {
            ID = "src-cam-99",
            Name = "Camera IC99",
            SignalNo = 99
        };

        row.SelectedSource = newSource;
        Assert.Equal("src-cam-99", row.SourceId);
        Assert.Equal(newSource, row.SelectedSource);
    }

    [Fact]
    public async Task VwWpfSceneSetupTabView_CreateNewScene_AutoPopulates12Grid_Test()
    {
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var freshKey = $"10.88.{Random.Shared.Next(10, 99)}.{Random.Shared.Next(10, 99)}";
        connection.AdHocIp = freshKey;
        var sceneVm = new SceneSetupViewModel(stack.ActivityPublisher, connection, new UserConfirmationTest(true), stack.Publisher);

        sceneVm.StartCreateNewSceneCommand.Execute(null);
        Assert.True(sceneVm.IsCreatingNewScene);
        Assert.Empty(sceneVm.SceneWindows);

        sceneVm.SceneName = "Kịch bản Mới Test";
        await sceneVm.CreateSceneCommand.ExecuteAsync(null);

        Assert.False(sceneVm.IsCreatingNewScene);
        Assert.NotNull(sceneVm.CurrentScene);
        Assert.Equal("Kịch bản Mới Test", sceneVm.CurrentScene.Name);

        sceneVm.Apply12GridTemplateCommand.Execute(null);
        Assert.Equal(12, sceneVm.SceneWindows.Count);
    }

    [Fact]
    public void VwWpfSceneSetupTabView_AddSceneWindow_And_LayoutTemplates_Test()
    {
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var freshKey = $"10.77.{Random.Shared.Next(10, 99)}.{Random.Shared.Next(10, 99)}";
        connection.AdHocIp = freshKey;
        var sceneVm = new SceneSetupViewModel(stack.ActivityPublisher, connection, new UserConfirmationTest(true), stack.Publisher);

        Assert.NotNull(sceneVm.CurrentScene);

        // Test ApplyFullWallTemplateCommand
        sceneVm.ApplyFullWallTemplateCommand.Execute(null);
        Assert.Single(sceneVm.SceneWindows);
        Assert.Equal(7680, sceneVm.SceneWindows[0].W);
        Assert.Equal(5760, sceneVm.SceneWindows[0].H);

        // Test AddSceneWindowCommand
        sceneVm.AddSceneWindowCommand.Execute(null);
        Assert.Equal(2, sceneVm.SceneWindows.Count);

        // Test ApplyBigCenterTemplateCommand
        sceneVm.ApplyBigCenterTemplateCommand.Execute(null);
        Assert.Equal(5, sceneVm.SceneWindows.Count);
        Assert.Equal(3840, sceneVm.SceneWindows[0].W);
        Assert.Equal(2160, sceneVm.SceneWindows[0].H);

        // Test Apply12GridTemplateCommand
        sceneVm.Apply12GridTemplateCommand.Execute(null);
        Assert.Equal(12, sceneVm.SceneWindows.Count);
    }

    [Fact]
    public async Task VwWpfSceneSetupTabView_AddWindow_SelectsRow_EnablesDeleteButton_Test()
    {
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var freshKey = $"10.66.{Random.Shared.Next(10, 99)}.{Random.Shared.Next(10, 99)}";
        connection.AdHocIp = freshKey;
        var sceneVm = new SceneSetupViewModel(stack.ActivityPublisher, connection, new UserConfirmationTest(true), stack.Publisher);

        // Apply 1-window template
        sceneVm.ApplyFullWallTemplateCommand.Execute(null);
        Assert.Single(sceneVm.SceneWindows);

        // Add a new window
        sceneVm.AddSceneWindowCommand.Execute(null);
        Assert.Equal(2, sceneVm.SceneWindows.Count);

        // The newly added window should be selected and Delete command should be executable
        Assert.NotNull(sceneVm.SelectedSceneWindow);
        Assert.True(sceneVm.DeleteSelectedSceneWindowsCommand.CanExecute(null));

        // Execute DeleteSelectedSceneWindowsCommand -> deletes selected row
        await sceneVm.DeleteSelectedSceneWindowsCommand.ExecuteAsync(null);
        Assert.Single(sceneVm.SceneWindows);
    }

    [Fact]
    public void VwWpfSceneSetupTabView_CreateNewSceneMode_HidesSceneDropdown_Test()
    {
        var (controller, screenA, screenB, source) = SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller, screenA, screenB, source);
        var freshKey = $"10.55.{Random.Shared.Next(10, 99)}.{Random.Shared.Next(10, 99)}";
        connection.AdHocIp = freshKey;
        var sceneVm = new SceneSetupViewModel(stack.ActivityPublisher, connection, new UserConfirmationTest(true), stack.Publisher);

        // Initially in normal mode
        Assert.False(sceneVm.IsCreatingNewScene);
        Assert.True(sceneVm.IsNotCreatingNewScene);

        // Start creating new scene
        sceneVm.StartCreateNewSceneCommand.Execute(null);
        Assert.True(sceneVm.IsCreatingNewScene);
        Assert.False(sceneVm.IsNotCreatingNewScene);

        // Cancel creating new scene -> returns to normal mode
        sceneVm.CancelCreateNewSceneCommand.Execute(null);
        Assert.False(sceneVm.IsCreatingNewScene);
        Assert.True(sceneVm.IsNotCreatingNewScene);
    }

    [Fact]
    public void VwWpfMainWindow_SingleConnectAndProbeButton_HeaderLayout_Test()
    {
        RunOnStaThread(() =>
        {
            var (controller, screenA, screenB, source) = SeedWallAsync();
            var stack = BuildClientStack();
            var connection = BuildConnection(stack, controller, screenA, screenB, source);
            var mainVm = BuildMainViewModel(stack, connection);

            var window = new MainWindow(mainVm);
            var mainGrid = window.Content as System.Windows.Controls.Grid;
            Assert.NotNull(mainGrid);

            var headerBorder = mainGrid.Children.OfType<System.Windows.Controls.Border>().FirstOrDefault(b => System.Windows.Controls.Grid.GetRow(b) == 0);
            Assert.NotNull(headerBorder);

            // Verify the single merged Connect & Probe button exists
            var headerButtons = FindVisualChildren<System.Windows.Controls.Button>(headerBorder).ToList();
            Assert.Contains(headerButtons, b => b.Content?.ToString()?.Contains("Khảo sát") == true);
            // Verify there is no separate standalone "Ping" button
            Assert.DoesNotContain(headerButtons, b => b.Content?.ToString() == "Ping");

            // Verify TabControl IsEnabled is bound to Connection.IsConnected
            var tabControl = FindVisualChildren<System.Windows.Controls.TabControl>(mainGrid).FirstOrDefault();
            Assert.NotNull(tabControl);
            var isEnabledBinding = System.Windows.Data.BindingOperations.GetBinding(tabControl, System.Windows.UIElement.IsEnabledProperty);
            Assert.NotNull(isEnabledBinding);
            Assert.Equal("Connection.IsConnected", isEnabledBinding.Path.Path);
            Assert.NotNull(tabControl.Style);
            Assert.Contains(tabControl.Style.Triggers.OfType<System.Windows.Trigger>(), tr => tr.Property == System.Windows.UIElement.IsEnabledProperty);
        });
    }

    private static IEnumerable<T> FindVisualChildren<T>(System.Windows.DependencyObject? parent) where T : System.Windows.DependencyObject
    {
        if (parent == null)
            yield break;

        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                yield return typedChild;

            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }

    private static readonly object AppInitLock = new();

    private static void RunOnStaThread(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                lock (AppInitLock)
                {
                    if (System.Windows.Application.Current == null)
                    {
                        var app = new System.Windows.Application();
                        app.Resources["BoolToVisibility"] = new System.Windows.Controls.BooleanToVisibilityConverter();
                        app.Resources["InverseBoolToVisibility"] = new Module.VideoWall.WPF.Interaction.InverseBoolToVisibilityConverter();
                        app.Resources["NullToVisibility"] = new Module.VideoWall.WPF.Interaction.NullToVisibilityConverter();
                        app.Resources["InverseNullToVisibility"] = new Module.VideoWall.WPF.Interaction.InverseNullToVisibilityConverter();
                        app.Resources["StatusColorConverter"] = new Module.VideoWall.WPF.Interaction.StatusColorConverter();

                        app.Resources["FieldLabel"] = new System.Windows.Style(typeof(System.Windows.Controls.TextBlock));
                        app.Resources["ToolbarButton"] = new System.Windows.Style(typeof(System.Windows.Controls.Button));
                        app.Resources["PrimaryButton"] = new System.Windows.Style(typeof(System.Windows.Controls.Button));
                        app.Resources["AccentButton"] = new System.Windows.Style(typeof(System.Windows.Controls.Button));
                        app.Resources["SectionHeader"] = new System.Windows.Style(typeof(System.Windows.Controls.TextBlock));
                        app.Resources["ReadOnlyGrid"] = new System.Windows.Style(typeof(System.Windows.Controls.DataGrid));
                    }
                }

                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            throw exception;
        }
    }

    private static MainViewModel BuildMainViewModel(VwWpfClientStackTest stack, ConnectionViewModel connection)
    {
        var confirmation = new UserConfirmationTest(true);
        var sceneSetup = new SceneSetupViewModel(stack.ActivityPublisher, connection, confirmation, stack.Publisher);
        var parameters = new ParametersViewModel(connection);
        var schedule = new ScheduleViewModel(stack.ApiClient, stack.ActivityPublisher);
        var scenario = new ScenarioViewModel(connection, stack.ActivityPublisher, stack.Publisher, confirmation);

        return new MainViewModel(
            stack.Session,
            stack.ActivityPublisher,
            connection,
            parameters,
            sceneSetup,
            schedule,
            scenario);
    }
}
