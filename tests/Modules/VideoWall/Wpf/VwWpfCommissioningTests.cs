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
}
