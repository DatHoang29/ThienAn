using Microsoft.Extensions.Logging.Abstractions;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Auth;
using Module.VideoWall.WPF.ViewModels;
using Services.Shared.Events;
using WpfDto = Module.VideoWall.WPF.Api.Dto;

namespace Tests.Modules.VideoWall.Wpf;

/// <summary>
/// Author: Đạt
/// Description: Integration test cho client WPF đấu nối tường ghép. Đi TRỌN luồng thật:
///              ViewModel của client -> ApiInvoker (HTTP) -> TAC_WebAPI in-memory (WebApplicationFactory)
///              -> handler nghiệp vụ thật -> VwISAPIMockServerHikvision (digest auth thật) khi chạm thiết bị.
///              Không mock tầng nào ở giữa, không cần thiết bị thật.
///
///              Vì sao alias WpfDto: DTO của client CỐ Ý trùng tên ngắn với DTO backend
///              (VwSetupSceneStep, VwProbeDeviceOutput...) mà GlobalUsings.VideoWall.cs đã global-using
///              các namespace backend đó — import thẳng sẽ đỏ CS0104.
/// Created date: 25/08/2026
/// </summary>
[Collection("api")]
public class VwWpfCommissioningTests(Host host)
{
    private const string TestPrefix = "TEST_WPF_";

    private readonly ISqlSugarClient _db = host.ApiServices.GetRequiredService<ISqlSugarClient>();
    private readonly BaseCacheService _cache = host.ApiServices.GetRequiredService<BaseCacheService>();

    /// <summary>
    /// Author: Đạt
    /// Description: Dựng cụm client WPF trỏ vào backend in-memory, có chèn JwtAuthHandler thật vào
    ///              chuỗi handler để đi đúng đường như lúc chạy thật.
    /// Created date: 25/08/2026
    /// </summary>
    private VwWpfClientStackTest BuildClientStack()
    {
        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var session = new SessionState();

        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(host.ApiClient), activityPublisher);

        return new VwWpfClientStackTest
        {
            Publisher = publisher,
            ActivityPublisher = activityPublisher,
            ApiClient = new VideoWallApiClient(invoker, publisher, activityPublisher),
            Session = session
        };
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Seed một VwController trỏ vào MockServer + màn hình + nguồn tín hiệu, rồi xoá cache
    ///              để query handler đọc lại từ CSDL. PHẢI xoá cache qua provider của web app vì đó là
    ///              BaseCacheService mà API thật đang đọc.
    /// Created date: 25/08/2026
    /// </summary>
    private async Task<(VwController Controller, VwScreen ScreenA, VwScreen ScreenB, VwSource Source)> SeedWallAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");

        // ── Dọn panel của các phiên seed trước ────────────────────────────────────────────────
        // BẮT BUỘC, không phải cho gọn: VwSceneRegionService.EnsureWindowInsideSceneRegionAsync đọc
        // TOÀN BỘ VwScreen còn hiệu lực (GetScreensAsync không lọc theo controller) rồi chặn cửa sổ
        // nếu nó phủ panel thuộc controller khác. Mọi bài test ở đây đều seed panel vào đúng hai ô
        // tường (0,0) và (1,0), mà ApplyIndividualWindows luôn tạo cửa sổ tại (0,0) — nên panel còn
        // sót của bài trước làm bài sau bị chặn với thông báo "nằm ngoài vùng màn hình", dù dữ liệu
        // của chính nó hoàn toàn đúng.
        //
        // Vì sao prefix + GUID không đủ ở đây: định danh có cô lập được, nhưng TOẠ ĐỘ TƯỜNG thì
        // không — hai controller khác nhau vẫn tranh nhau đúng một ô (0,0). Nên phải dọn thật.
        // Xoá mềm (IsDelete là DateTime?) để vẫn tra lại được nếu cần điều tra.
        await _db.Updateable<VwScreen>()
            .SetColumns(u => new VwScreen { IsDelete = DateTime.Now })
            .Where(u => u.IsDelete == null)
            .ExecuteCommandAsync();

        var controller = new VwController
        {
            ID = $"{TestPrefix}CTRL_{suffix}",
            Code = $"{TestPrefix}CTRL_{suffix}",
            Name = "WPF Integration Controller",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(controller).ExecuteCommandAsync();

        var screenA = new VwScreen
        {
            ID = $"{TestPrefix}SCR_A_{suffix}",
            Code = $"{TestPrefix}SCR_A_{suffix}",
            Name = "Screen A",
            ControllerId = controller.ID,
            OutPutPort = "HDMI1",
            OutputId = "17235971",
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
            OutputId = "17235972",
            GridCol = 1,
            GridRow = 0,
            // CỐ Ý bỏ trống WidthPx/HeightPx để kiểm nhánh suy kích thước từ Resolution.
            Resolution = "1920x1080",
            Status = BaseEnums.StatusEnum.Enable
        };
        await _db.Insertable(new[] { screenA, screenB }).ExecuteCommandAsync();

        var source = new VwSource
        {
            ID = $"{TestPrefix}SRC_{suffix}",
            Code = $"{TestPrefix}SRC_{suffix}",
            Name = "Camera 01",
            ControllerId = controller.ID,
            SignalNo = 1,
            Status = BaseEnums.StatusEnum.Enable
        };
        await _db.Insertable(source).ExecuteCommandAsync();

        _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwSource);
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwScene);

        return (controller, screenA, screenB, source);
    }

    private static ConnectionViewModel BuildConnection(VwWpfClientStackTest stack, VwController controller) =>
        new(stack.ApiClient, stack.ActivityPublisher, stack.Publisher)
        {
            SelectedController = new WpfDto.VwControllerDto
            {
                ID = controller.ID,
                Code = controller.Code,
                Name = controller.Name,
                IP = controller.IP
            }
        };

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
        host.MockServer.ResetDefaults();

        // Arrange
        var (controller, _, _, _) = await SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
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
        Assert.NotEmpty(host.MockServer.ReceivedRequests);
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
        host.MockServer.ResetDefaults();
        host.MockServer.VerifyDigestResponseHash = true;

        // Arrange — mật khẩu sai so với MockServer.
        var (controller, _, _, _) = await SeedWallAsync();
        await _db.Updateable<VwController>()
            .SetColumns(item => item.PassWord == "SaiMatKhau!")
            .Where(item => item.ID == controller.ID)
            .ExecuteCommandAsync();
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);

        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
        stack.Publisher.Clear();

        try
        {
            // Act
            await connection.ConnectCommand.ExecuteAsync(null);

            // Assert
            Assert.False(connection.IsConnected);
            Assert.True(stack.Publisher.TotalLogRows >= 1, "Thất bại phải để lại dấu vết trên bảng log.");
        }
        finally
        {
            // Ngắt mạch (circuit breaker) khoá theo IP, mà mọi test trong Collection "api" đều dùng
            // chung 127.0.0.1:18080 — không xoá thì các test sau bị chặn oan tới 5 phút.
            host.ApiServices.GetRequiredService<IVwISAPIDeviceClient>()
                .ResetCircuitBreaker(controller.IP!);
        }
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Probe (lệnh chỉ-đọc) đọc được tường / cổng ra / kênh vào từ MockServer, và số dòng
    ///              log bằng ĐÚNG số bước ISAPI — không có dòng chung cộng thêm.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfConnection_Probe_ReadsDeviceTopologyAndLogsOneRowPerStep_Test()
    {
        host.MockServer.ResetDefaults();

        // Arrange
        var (controller, _, _, _) = await SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
        stack.Publisher.Clear();

        // Act
        await connection.ProbeCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(connection.ProbeResult);
        Assert.True(connection.ProbeResult!.Reachable, connection.StatusMessage);
        Assert.NotEmpty(connection.ProbeWalls);
        Assert.NotEmpty(connection.ProbeOutputs);
        Assert.NotEmpty(connection.ProbeInputChannels);

        var expectedRows = connection.ProbeResult.Steps?.Count ?? 0;
        Assert.True(expectedRows > 0, "Probe phải trả về ít nhất một bước ISAPI.");
        Assert.Equal(expectedRows, stack.Publisher.TotalLogRows);
        Assert.Empty(stack.Publisher.ActivityRows);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Tab Dựng kịch bản: chế độ "từng cổng ra" và "cửa sổ xếp lớp".
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
        host.MockServer.ResetDefaults();

        // Arrange
        var (controller, screenA, screenB, source) = await SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
        var confirmation = new UserConfirmationTest(answer: false);
        var sceneSetup = new SceneSetupViewModel(stack.ApiClient, stack.ActivityPublisher, connection, confirmation)
        {
            SceneName = $"Canh WPF {Guid.NewGuid():N}",
            SceneOutputId = "1",
            GridCols = 2,
            GridRows = 1
        };

        // Act 1 — nạp màn hình + nguồn từ backend thật
        await sceneSetup.LoadScreensAndSourcesCommand.ExecuteAsync(null);

        Assert.Equal(2, sceneSetup.ScreenAssignments.Count);
        Assert.Contains(sceneSetup.Sources, item => item.ID == source.ID);

        // Kích thước: màn A lấy từ WidthPx/HeightPx, màn B suy ra từ Resolution.
        Assert.All(sceneSetup.ScreenAssignments, row =>
        {
            Assert.Equal(1920, row.Width);
            Assert.Equal(1080, row.Height);
        });

        foreach (var row in sceneSetup.ScreenAssignments)
            row.SelectedSource = sceneSetup.Sources.First(item => item.ID == source.ID);

        // Act 2 — tạo kịch bản
        await sceneSetup.CreateSceneCommand.ExecuteAsync(null);

        Assert.NotNull(sceneSetup.CurrentScene);
        Assert.False(string.IsNullOrWhiteSpace(sceneSetup.CurrentScene!.ID), sceneSetup.StatusMessage);

        var sceneInDb = await _db.Queryable<VwScene>()
            .FirstAsync(item => item.Code == sceneSetup.CurrentScene.Code && item.IsDelete == null);
        Assert.NotNull(sceneInDb);
        Assert.Equal(controller.ID, sceneInDb.ControllerId);

        // Act 3 — dựng một cửa sổ phủ kín cho mỗi màn hình
        await sceneSetup.ApplyIndividualWindowsCommand.ExecuteAsync(null);

        // Assert — DB có đúng 2 cửa sổ, mỗi cửa sổ nằm trên ĐÚNG màn hình của nó
        var windows = await _db.Queryable<VwWindowScene>()
            .Where(item => item.SceneId == sceneSetup.CurrentScene.ID && item.IsDelete == null)
            .ToListAsync();

        Assert.Equal(2, windows.Count);
        Assert.All(windows, window =>
        {
            Assert.Equal(1920, window.W);
            Assert.Equal(1080, window.H);
            Assert.Equal(1, window.ZIndex);
            Assert.Equal(source.ID, window.SourceId);
        });

        // Toạ độ là pixel TUYỆT ĐỐI trên tường: màn hình ở GridCol=1 phải cho cửa sổ ở
        // X = 1 × pitch panel, KHÔNG phải 0. Trước đây cả hai cửa sổ đều X=0 nên dồn lên panel đầu
        // tiên và màn hình thứ hai không nhận gì — bài test này là chốt chống lặp lại lỗi đó.
        //
        // Khớp theo TẬP toạ độ thay vì theo tên: window.Name là DisplayName của màn hình (có kèm
        // cổng ra) nên so bằng VwScreen.Name sẽ không trùng.
        var expectedX = new[]
        {
            screenA.GridCol!.Value * VwWallProfile.PanelWidthPx,
            screenB.GridCol!.Value * VwWallProfile.PanelWidthPx,
        }.OrderBy(value => value).ToList();

        var actualX = windows.Select(window => window.X ?? -1).OrderBy(value => value).ToList();
        Assert.Equal(expectedX, actualX);

        var expectedY = screenA.GridRow!.Value * VwWallProfile.PanelHeightPx;
        Assert.All(windows, window => Assert.Equal(expectedY, window.Y));

        // Hai cửa sổ KHÔNG được trùng toạ độ X — trùng chính là biểu hiện của bug cũ.
        Assert.Equal(2, actualX.Distinct().Count());

        // Act 4 — đọc lại thông tin kịch bản qua API
        await sceneSetup.LoadSceneWindowsCommand.ExecuteAsync(null);
        Assert.Equal(2, sceneSetup.SceneWindows.Count);

        // Act 5 — kích hoạt kịch bản
        await sceneSetup.ActivateSceneCommand.ExecuteAsync(null);

        var activated = await _db.Queryable<VwScene>()
            .FirstAsync(item => item.ID == sceneSetup.CurrentScene.ID);
        Assert.Equal(BaseEnums.ActiveScene.Activate, activated.ActiveScene);
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
        host.MockServer.ResetDefaults();

        // Arrange
        var (controller, _, _, source) = await SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
        var sceneSetup = new SceneSetupViewModel(
            stack.ApiClient,
            stack.ActivityPublisher,
            connection,
            new UserConfirmationTest(answer: false))
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

        var pickedSource = sceneSetup.Sources.First(item => item.ID == source.ID);

        // Hai cửa sổ chồng nhau cùng vùng, khác lớp
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
        var windows = await _db.Queryable<VwWindowScene>()
            .Where(item => item.SceneId == sceneSetup.CurrentScene!.ID && item.IsDelete == null)
            .ToListAsync();

        Assert.Equal(2, windows.Count);
        Assert.Equal(2, windows.Select(item => item.ZIndex).Distinct().Count());
        Assert.All(windows, window =>
        {
            Assert.Equal(960, window.W);
            Assert.Equal(540, window.H);
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // Sửa / xoá kịch bản và cửa sổ đã lưu.
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Author: Đạt
    /// Description: Dựng sẵn một kịch bản kèm cửa sổ để các test sửa/xoá dùng chung.
    /// Created date: 25/08/2026
    /// </summary>
    private async Task<SceneSetupViewModel> CreateSceneWithWindowsAsync(
        VwWpfClientStackTest stack,
        ConnectionViewModel connection,
        UserConfirmationTest confirmation,
        VwSource source)
    {
        var sceneSetup = new SceneSetupViewModel(stack.ApiClient, stack.ActivityPublisher, connection, confirmation)
        {
            SceneName = $"Canh Sua {Guid.NewGuid():N}",
            SceneOutputId = "1",
            GridCols = 2,
            GridRows = 1
        };

        await sceneSetup.LoadScreensAndSourcesCommand.ExecuteAsync(null);
        foreach (var row in sceneSetup.ScreenAssignments)
            row.SelectedSource = sceneSetup.Sources.First(item => item.ID == source.ID);

        await sceneSetup.CreateSceneCommand.ExecuteAsync(null);
        await sceneSetup.ApplyIndividualWindowsCommand.ExecuteAsync(null);
        await sceneSetup.LoadSceneWindowsCommand.ExecuteAsync(null);

        Assert.NotNull(sceneSetup.CurrentScene);
        Assert.Equal(2, sceneSetup.SceneWindows.Count);
        return sceneSetup;
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Đổi tên kịch bản phải LƯU ĐƯỢC XUỐNG SERVER — kiểm bằng cách đọc lại CSDL, không
    ///              chỉ tin vào đối tượng trong bộ nhớ.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_UpdateScene_PersistsRenameServerSide_Test()
    {
        host.MockServer.ResetDefaults();

        // Arrange
        var (controller, _, _, source) = await SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
        var sceneSetup = await CreateSceneWithWindowsAsync(stack, connection, new UserConfirmationTest(false), source);
        var sceneId = sceneSetup.CurrentScene!.ID!;

        var renamed = $"Canh Da Doi Ten {Guid.NewGuid():N}";
        sceneSetup.SceneName = renamed;
        sceneSetup.GridCols = 4;
        sceneSetup.GridRows = 2;

        // Act
        await sceneSetup.UpdateSceneCommand.ExecuteAsync(null);

        // Assert — đọc lại từ CSDL
        var inDb = await _db.Queryable<VwScene>().FirstAsync(item => item.ID == sceneId && item.IsDelete == null);
        Assert.Equal(renamed, inDb.Name);
        Assert.Equal(4, inDb.GridCols);
        Assert.Equal(2, inDb.GridRows);

        // Combobox phải hiện tên mới ngay
        Assert.Equal(renamed, sceneSetup.CurrentScene!.Name);
        Assert.Contains(renamed, sceneSetup.CurrentSceneSummary);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Sửa toạ độ/kích thước một cửa sổ ĐÃ LƯU rồi bấm Lưu — phải ghi xuống server.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_UpdateSceneWindow_PersistsGeometryServerSide_Test()
    {
        host.MockServer.ResetDefaults();

        // Arrange
        var (controller, _, _, source) = await SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
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
        var inDb = await _db.Queryable<VwWindowScene>().FirstAsync(item => item.ID == targetId && item.IsDelete == null);
        Assert.Equal(640, inDb.X);
        Assert.Equal(360, inDb.Y);
        Assert.Equal(1280, inDb.W);
        Assert.Equal(720, inDb.H);
        Assert.Equal(5, inDb.ZIndex);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Từ chối lưu khi kích thước không hợp lệ (W/H phải > 0) — chặn ngay ở client,
    ///              không gửi request rác lên server.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_UpdateSceneWindow_WithInvalidSize_RejectsWithoutSaving_Test()
    {
        host.MockServer.ResetDefaults();

        // Arrange
        var (controller, _, _, source) = await SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
        var sceneSetup = await CreateSceneWithWindowsAsync(stack, connection, new UserConfirmationTest(false), source);

        var target = sceneSetup.SceneWindows[0];
        var targetId = target.ID!;
        target.W = 0;

        // Act
        await sceneSetup.UpdateSceneWindowCommand.ExecuteAsync(target);

        // Assert — CSDL giữ nguyên 1920
        var inDb = await _db.Queryable<VwWindowScene>().FirstAsync(item => item.ID == targetId && item.IsDelete == null);
        Assert.Equal(1920, inDb.W);
        Assert.Contains("W, H > 0", sceneSetup.StatusMessage);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Xoá một cửa sổ phải mất THẬT ở server — kiểm cả bộ sưu tập trên UI, CSDL (xoá mềm),
    ///              và một lượt LoadSceneWindows mới (không phải chỉ bỏ khỏi collection cục bộ).
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_DeleteSceneWindow_RemovesWindowServerSide_Test()
    {
        host.MockServer.ResetDefaults();

        // Arrange
        var (controller, _, _, source) = await SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
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

        // CSDL: đã xoá mềm
        var active = await _db.Queryable<VwWindowScene>()
            .FirstAsync(item => item.ID == doomedId && item.IsDelete == null);
        Assert.Null(active);

        // Lượt đọc lại từ server cũng không còn
        await sceneSetup.LoadSceneWindowsCommand.ExecuteAsync(null);
        Assert.Single(sceneSetup.SceneWindows);

        var remaining = await _db.Queryable<VwWindowScene>()
            .Where(item => item.SceneId == sceneId && item.IsDelete == null)
            .CountAsync();
        Assert.Equal(1, remaining);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Bấm Xoá kịch bản rồi CHỌN KHÔNG ở hộp thoại — kịch bản phải còn nguyên, không
    ///              gửi lệnh xoá nào lên server.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_DeleteScene_WhenCancelled_LeavesSceneUntouched_Test()
    {
        host.MockServer.ResetDefaults();

        // Arrange
        var (controller, _, _, source) = await SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
        var confirmation = new UserConfirmationTest(answer: false);
        var sceneSetup = await CreateSceneWithWindowsAsync(stack, connection, confirmation, source);
        var sceneId = sceneSetup.CurrentScene!.ID!;

        // Act
        await sceneSetup.DeleteSceneCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(1, confirmation.CallCount);
        Assert.NotNull(sceneSetup.CurrentScene);
        Assert.Contains("huỷ", sceneSetup.StatusMessage, StringComparison.OrdinalIgnoreCase);

        var stillThere = await _db.Queryable<VwScene>()
            .FirstAsync(item => item.ID == sceneId && item.IsDelete == null);
        Assert.NotNull(stillThere);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Xác nhận Xoá kịch bản — server xoá mềm, và giao diện bỏ kịch bản khỏi dropdown,
    ///              xoá danh sách cửa sổ, bỏ chọn kịch bản đang làm việc.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSceneSetup_DeleteScene_WhenConfirmed_SoftDeletesAndClearsSelection_Test()
    {
        host.MockServer.ResetDefaults();

        // Arrange
        var (controller, _, _, source) = await SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
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

        // Assert — server đã xoá mềm
        var active = await _db.Queryable<VwScene>()
            .FirstAsync(item => item.ID == sceneId && item.IsDelete == null);
        Assert.Null(active);

        var softDeleted = await _db.Queryable<VwScene>()
            .ClearFilter()
            .FirstAsync(item => item.ID == sceneId && item.IsDelete != null);
        Assert.NotNull(softDeleted);
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
        host.MockServer.ResetDefaults();

        // Arrange
        var (controller, _, _, source) = await SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
        var confirmation = new UserConfirmationTest(answer: false);
        var sceneSetup = new SceneSetupViewModel(stack.ApiClient, stack.ActivityPublisher, connection, confirmation)
        {
            SceneName = $"Canh DryRun {Guid.NewGuid():N}",
            SceneOutputId = "1",
            GridCols = 2,
            GridRows = 1
        };

        await sceneSetup.LoadScreensAndSourcesCommand.ExecuteAsync(null);
        foreach (var row in sceneSetup.ScreenAssignments)
            row.SelectedSource = sceneSetup.Sources.First(item => item.ID == source.ID);

        await sceneSetup.CreateSceneCommand.ExecuteAsync(null);
        await sceneSetup.ApplyIndividualWindowsCommand.ExecuteAsync(null);

        Assert.True(sceneSetup.DryRun, "DryRun phải là mặc định an toàn.");

        // Reset bộ đếm NGAY TRƯỚC lượt đẩy: bản thân vwwindowscene/add ở trên đã đồng bộ cửa sổ xuống
        // thiết bị (DeleteAllWindows + AddWindow), nên bộ đếm đang khác 0. Không reset thì phép so
        // "DryRun không ghi gì" sẽ đếm lẫn cả lượt tạo cửa sổ.
        host.MockServer.ResetDefaults();
        stack.Publisher.Clear();

        // Act
        await sceneSetup.PushToDeviceCommand.ExecuteAsync(null);

        // Assert — không hỏi xác nhận, không có lệnh ghi cửa sổ xuống thiết bị
        Assert.Equal(0, confirmation.CallCount);
        Assert.Equal(0, host.MockServer.AddWindowCallCount);
        Assert.Equal(0, host.MockServer.SaveSceneDataCallCount);

        // Mỗi bước ISAPI đúng một dòng log, không có dòng chung cộng thêm
        Assert.True(stack.Publisher.DeviceStepRows.Count > 0, sceneSetup.StatusMessage);
        Assert.Empty(stack.Publisher.ActivityRows);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Tab Lịch — CRUD thuần qua API thật.
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Author: Đạt
    /// Description: Lịch bật/tắt: thêm -> nạp lại -> sửa -> xoá, toàn bộ qua ViewModel và API thật.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfSchedule_AddUpdateDelete_RoundTripsThroughApi_Test()
    {
        host.MockServer.ResetDefaults();

        // Arrange
        var stack = BuildClientStack();
        var schedule = new ScheduleViewModel(stack.ApiClient, stack.ActivityPublisher);
        var name = $"{TestPrefix}Lich_{Guid.NewGuid():N}";

        schedule.NewScheduleCommand.Execute(null);
        schedule.FormName = name;
        schedule.FormTime = "07:30";
        schedule.FormWeekdays = "1,2,3,4,5";
        schedule.FormAction = "on";

        // Act 1 — thêm
        await schedule.SaveScheduleCommand.ExecuteAsync(null);

        var added = await _db.Queryable<VwSchedule>()
            .FirstAsync(item => item.Name == name && item.IsDelete == null);
        Assert.NotNull(added);
        Assert.Equal("07:30", added.Time);

        // Act 2 — nạp lại và chọn đúng bản ghi vừa thêm
        await schedule.RefreshCommand.ExecuteAsync(null);
        var loaded = schedule.Schedules.FirstOrDefault(item => item.ID == added.ID);
        Assert.NotNull(loaded);

        // Act 3 — sửa giờ chạy
        schedule.SelectedSchedule = loaded;
        schedule.FormTime = "08:45";
        await schedule.SaveScheduleCommand.ExecuteAsync(null);

        var updated = await _db.Queryable<VwSchedule>().FirstAsync(item => item.ID == added.ID);
        Assert.Equal("08:45", updated.Time);

        // Act 4 — xoá mềm
        schedule.SelectedSchedule = schedule.Schedules.First(item => item.ID == added.ID);
        await schedule.DeleteScheduleCommand.ExecuteAsync(null);

        var deleted = await _db.Queryable<VwSchedule>()
            .ClearFilter()
            .FirstAsync(item => item.ID == added.ID && item.IsDelete != null);
        Assert.NotNull(deleted);
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
        host.MockServer.ResetDefaults();

        // Arrange
        var (controller, screenA, screenB, _) = await SeedWallAsync();
        var stack = BuildClientStack();
        var connection = BuildConnection(stack, controller);
        var parameters = new ParametersViewModel(stack.ApiClient, connection);

        // Act
        await parameters.LoadCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(2, parameters.Screens.Count);
        Assert.Contains(parameters.Screens, item => item.ID == screenA.ID);
        Assert.Contains(parameters.Screens, item => item.ID == screenB.ID);
        Assert.Contains(parameters.Screens, item => item.OutPutPort == "HDMI1");
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Nạp danh sách bộ điều khiển từ API thật — bản ghi vừa seed phải xuất hiện trên
    ///              combobox của tab Kết nối.
    /// Created date: 25/08/2026
    /// </summary>
    [Fact]
    public async Task VwWpfConnection_LoadControllers_ReturnsSeededController_Test()
    {
        host.MockServer.ResetDefaults();

        // Arrange
        var (controller, _, _, _) = await SeedWallAsync();
        var stack = BuildClientStack();
        var connection = new ConnectionViewModel(stack.ApiClient, stack.ActivityPublisher, stack.Publisher);

        // Act
        await connection.LoadControllersCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(connection.Controllers, item => item.ID == controller.ID);

        // Endpoint nghiệp vụ thường VẪN giữ dòng log chung (khác device-setup)
        Assert.Single(stack.Publisher.ActivityRows);
        Assert.Empty(stack.Publisher.DeviceStepRows);
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
        var vm = new ConnectionViewModel(stack.ApiClient, stack.ActivityPublisher, stack.Publisher);

        Assert.NotEmpty(vm.IsapiPresets);
        Assert.Contains(vm.IsapiPresets, p => p.Section == "9.7.5.6" && p.Method == "GET");
        Assert.Contains(vm.IsapiPresets, p => p.Section == "9.7.8.1" && p.Method == "PUT");

        var preset = vm.IsapiPresets.First(p => p.Section == "9.7.8.1");
        vm.SelectedIsapiPreset = preset;

        Assert.Equal("PUT", vm.IsapiMethod);
        Assert.Equal("ISAPI/DisplayDev/ScreenCtrl/closeAll", vm.IsapiPath);
    }
}
