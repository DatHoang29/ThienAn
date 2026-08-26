using Module.VideoWall.Core.Dto.ISAPI;
using Module.VideoWall.Core.Interfaces;

namespace Tests.Modules.VideoWall;

/// <summary>
/// Description: Bộ kiểm thử tích hợp cho phân hệ VwWindowScene (Cửa sổ hiển thị trong kịch bản & Ràng buộc toạ độ)
/// Created date: 15/08/2026
/// </summary>
[Collection("api")]
public class VwWindowSceneTests(Host host)
{
    private const string TestPrefix = "TEST_VWWIN_";
    private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();
    private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
    private readonly BaseCacheService _cache = host.Services.GetRequiredService<BaseCacheService>();
    private readonly NewLife.Caching.ICacheProvider _cacheProvider = host.Services.GetRequiredService<NewLife.Caching.ICacheProvider>();
    private readonly IStringLocalizer _localizer = host.Localizer;

    /// <summary>
    /// Description: Kiểm tra phân trang VwWindowScene trả về danh sách hợp lệ
    /// Created date: 15/08/2026
    /// </summary>
    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 5)]
    [InlineData(2, 20)]
    public async Task VwWindowSceneQuery_Page_ReturnsSuccess_Test(int page, int pageSize)
    {
        // Arrange
        var input = new VwPageWindowSceneInput
        {
            Page = page,
            PageSize = pageSize
        };

        // Act
        var result = await _bus.InvokeAsync<SqlSugarPagedList<VwPageWindowSceneOutput>>(input);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Records);
    }

    /// <summary>
    /// Description: Kiểm tra GetList VwWindowScene trả về thành công sau khi xóa cache
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneQuery_GetList_ReturnsSuccess_Test()
    {
        // Arrange
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);
        var input = new VwWindowSceneInput();

        // Act
        var result = await _bus.InvokeAsync<List<VwWindowSceneOutput>>(input);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Description: Kiểm tra GetById VwWindowScene trả về đúng bản ghi đã tạo
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneQuery_GetById_ReturnsSuccess_Test()
    {
        // Arrange
        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var win = new VwWindowScene
        {
            Code = uniqueCode,
            Name = "Test Window ById",
            X = 0,
            Y = 0,
            W = 1920,
            H = 1080,
            Visible = BaseEnums.SceneWindowVisible.Visible,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(win).ExecuteCommandAsync();
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);

        var input = new VwIdWindowSceneInput { ID = win.ID };

        // Act
        var result = await _bus.InvokeAsync<VwWindowSceneOutput>(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(uniqueCode, result.Code);
    }

    /// <summary>
    /// Description: Kiểm tra thêm mới VwWindowScene trong kịch bản toàn tường (FullScreen) thành công
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneCommand_AddVwWindowScene_InsertsRecord_Test()
    {
        // Arrange
        var sceneCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var scene = new VwScene
        {
            Code = sceneCode,
            Name = "Test Scene",
            ControllerId = null,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();

        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var input = new VwAddWindowSceneInput
        {
            Code = uniqueCode,
            Name = "Test Add Window",
            SceneId = scene.ID,
            X = 0,
            Y = 0,
            W = 1920,
            H = 1080,
            Visible = BaseEnums.SceneWindowVisible.Visible
        };

        // Validate (FluentValidation)
        var validator = new VwAddWindowSceneValidator(_localizer);
        var valResult = await validator.ValidateAsync(input);
        Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

        // Act
        await _bus.InvokeAsync(input);

        // Assert
        var inserted = await _db.Queryable<VwWindowScene>()
            .FirstAsync(u => u.Code == uniqueCode && u.IsDelete == null);

        Assert.NotNull(inserted);
        Assert.Equal("Test Add Window", inserted.Name);
    }

    /// <summary>
    /// Description: Kiểm tra cập nhật VwWindowScene thay đổi thông tin bản ghi trong CSDL
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneCommand_UpdateVwWindowScene_UpdatesRecord_Test()
    {
        // Arrange
        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var win = new VwWindowScene
        {
            Code = uniqueCode,
            Name = "Original Window Name",
            X = 0,
            Y = 0,
            W = 1920,
            H = 1080,
            Visible = BaseEnums.SceneWindowVisible.Visible,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(win).ExecuteCommandAsync();
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);

        var updateInput = new VwUpdateWindowSceneInput
        {
            ID = win.ID,
            Code = uniqueCode,
            Name = "Updated Window Name",
            X = 100,
            Y = 100,
            W = 800,
            H = 600,
            Visible = BaseEnums.SceneWindowVisible.Visible
        };

        // Validate (FluentValidation)
        var validator = new VwUpdateWindowSceneValidator(_localizer);
        var valResult = await validator.ValidateAsync(updateInput);
        Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

        // Act
        await _bus.InvokeAsync(updateInput);

        // Assert
        var updated = await _db.Queryable<VwWindowScene>()
            .FirstAsync(u => u.ID == win.ID && u.IsDelete == null);

        Assert.NotNull(updated);
        Assert.Equal("Updated Window Name", updated.Name);
    }

    /// <summary>
    /// Description: Kiểm tra VwUpdateWindowSceneValidator từ chối khi ID không hợp lệ
    /// Created date: 17/08/2026
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VwWindowSceneCommand_UpdateVwWindowScene_ValidationRejectsInvalidId_Test(string? invalidId)
    {
        var validator = new VwUpdateWindowSceneValidator(_localizer);
        var result = await validator.ValidateAsync(new VwUpdateWindowSceneInput { ID = invalidId, Name = "Name" });
        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Description: Kiểm tra VwDeleteWindowSceneValidator từ chối khi ID không hợp lệ
    /// Created date: 17/08/2026
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VwWindowSceneCommand_DeleteVwWindowScene_ValidationRejectsInvalidId_Test(string? invalidId)
    {
        var validator = new VwDeleteWindowSceneValidator(_localizer);
        var invalidResult = await validator.ValidateAsync(new VwDeleteWindowSceneInput { ID = invalidId });
        Assert.False(invalidResult.IsValid);
    }

    /// <summary>
    /// Description: Kiểm tra xóa 1 VwWindowScene thực hiện xóa mềm (IsDelete != null)
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneCommand_DeleteVwWindowScene_SoftDeletesRecord_Test()
    {
        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var win = new VwWindowScene
        {
            Code = uniqueCode,
            Name = "Window To Delete",
            Visible = BaseEnums.SceneWindowVisible.Visible,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(win).ExecuteCommandAsync();
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);

        var deleteInput = new VwDeleteWindowSceneInput { ID = win.ID };
        var validator = new VwDeleteWindowSceneValidator(_localizer);
        var valResult = await validator.ValidateAsync(deleteInput);
        Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

        await _bus.InvokeAsync(deleteInput);

        var active = await _db.Queryable<VwWindowScene>()
            .FirstAsync(u => u.ID == win.ID && u.IsDelete == null);
        Assert.Null(active);

        var deleted = await _db.Queryable<VwWindowScene>()
            .ClearFilter()
            .FirstAsync(u => u.ID == win.ID && u.IsDelete != null);
        Assert.NotNull(deleted);
    }

    /// <summary>
    /// Description: Kiểm tra xóa nhiều VwWindowScene thực hiện xóa mềm danh sách bản ghi
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneCommand_BatchDeleteVwWindowScene_SoftDeletesRecords_Test()
    {
        var validator = new VwDeleteWindowSceneValidator(_localizer);
        var uniqueCode1 = $"{TestPrefix}{Guid.NewGuid():N}";
        var uniqueCode2 = $"{TestPrefix}{Guid.NewGuid():N}";

        var win1 = new VwWindowScene
        {
            Code = uniqueCode1,
            Name = "Batch Delete Win 1",
            Visible = BaseEnums.SceneWindowVisible.Visible,
            CreateTime = DateTime.Now
        };
        var win2 = new VwWindowScene
        {
            Code = uniqueCode2,
            Name = "Batch Delete Win 2",
            Visible = BaseEnums.SceneWindowVisible.Visible,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(new[] { win1, win2 }).ExecuteCommandAsync();
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);

        var batchInput = new List<VwDeleteWindowSceneInput>
        {
            new() { ID = win1.ID },
            new() { ID = win2.ID }
        };

        foreach (var item in batchInput)
        {
            var valResult = await validator.ValidateAsync(item);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));
        }

        await _bus.InvokeAsync(batchInput);

        var activeCount = await _db.Queryable<VwWindowScene>()
            .Where(u => (u.ID == win1.ID || u.ID == win2.ID) && u.IsDelete == null)
            .CountAsync();
        Assert.Equal(0, activeCount);
    }

    /// <summary>
    /// Description: Kiểm tra thêm cửa sổ tràn sang panel của bộ điều khiển khác sẽ bị VwSceneRegionService chặn
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneCommand_AddWindow_OutsideSceneRegion_ThrowsException_Test()
    {
        // Arrange
        var ctrlA = new VwController
        {
            Code = $"{TestPrefix}CTRL_A_{Guid.NewGuid():N}",
            Name = "Controller A",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        var ctrlB = new VwController
        {
            Code = $"{TestPrefix}CTRL_B_{Guid.NewGuid():N}",
            Name = "Controller B",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(new[] { ctrlA, ctrlB }).ExecuteCommandAsync();

        var baseColA = Random.Shared.Next(100, 10000) * 10;
        // Panel 0 thuộc Controller A (GridCol = baseColA, GridRow = 0)
        var screenA = new VwScreen
        {
            Code = $"{TestPrefix}SCR_A_{Guid.NewGuid():N}",
            Name = "Panel A",
            ControllerId = ctrlA.ID,
            GridCol = baseColA,
            GridRow = 0,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        // Panel 1 thuộc Controller B (GridCol = baseColA + 1, GridRow = 0)
        var screenB = new VwScreen
        {
            Code = $"{TestPrefix}SCR_B_{Guid.NewGuid():N}",
            Name = "Panel B",
            ControllerId = ctrlB.ID,
            GridCol = baseColA + 1,
            GridRow = 0,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(new[] { screenA, screenB }).ExecuteCommandAsync();

        // Kịch bản thuộc quyền sở hữu của Controller A
        var sceneA = new VwScene
        {
            Code = $"{TestPrefix}SCN_A_{Guid.NewGuid():N}",
            Name = "Scene of Controller A",
            ControllerId = ctrlA.ID,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(sceneA).ExecuteCommandAsync();

        // Cố gắng đặt cửa sổ tràn sang toạ độ của Panel B
        var input = new VwAddWindowSceneInput
        {
            Code = $"{TestPrefix}WIN_OVERLAP_{Guid.NewGuid():N}",
            Name = "Illegal Overlapping Window",
            SceneId = sceneA.ID,
            X = (baseColA + 1) * VwSceneRegionService.PanelWidthPx + 160,
            Y = 100,
            W = 800,
            H = 600,
            Visible = BaseEnums.SceneWindowVisible.Visible
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _bus.InvokeAsync(input));
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Scene thuộc 1 controller có cấu hình IP (đã map thiết bị) — thêm cửa sổ phải
    ///              đồng bộ qua IVwISAPIDeviceClient (DeleteAllWindows + AddWindow) TRƯỚC khi ghi DB;
    ///              thiết bị (giả) trả thành công thì DB vẫn cập nhật đúng như luồng không map
    ///              thiết bị.
    /// Created date: 16/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneCommand_AddWindow_WithMappedDevice_SyncsToDeviceAndInsertsRecord_Test()
    {
        // Arrange
        var ctrl = new VwController
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}CTRL_DEV_{Guid.NewGuid():N}",
            Name = "Mapped Device Controller",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(ctrl).ExecuteCommandAsync();

        var baseColDev = Random.Shared.Next(100, 10000) * 10;
        var screen = new VwScreen
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCR_DEV_{Guid.NewGuid():N}",
            Name = "Panel Dev",
            ControllerId = ctrl.ID,
            GridCol = baseColDev,
            GridRow = 0,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(screen).ExecuteCommandAsync();

        var scene = new VwScene
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCN_DEV_{Guid.NewGuid():N}",
            Name = "Scene With Device",
            ControllerId = ctrl.ID,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();

        var uniqueCode = $"{TestPrefix}WIN_DEV_{Guid.NewGuid():N}";
        var input = new VwAddWindowSceneInput
        {
            Code = uniqueCode,
            Name = "Window With Device Sync",
            SceneId = scene.ID,
            X = baseColDev * VwSceneRegionService.PanelWidthPx + 100,
            Y = 100,
            W = 800,
            H = 600,
            Visible = BaseEnums.SceneWindowVisible.Visible
        };

        // Act
        await _bus.InvokeAsync(input);

        // Assert
        var inserted = await _db.Queryable<VwWindowScene>()
            .FirstAsync(u => u.Code == uniqueCode && u.IsDelete == null);

        Assert.NotNull(inserted);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Thiết bị gặp lỗi (Sync lỗi, không hỗ trợ Scene, hoặc SaveSceneData lỗi) —
    ///              handler phải fail-fast: throw ngay và KHÔNG ghi DB.
    /// Created date: 19/08/2026
    /// </summary>
    [Theory]
    [InlineData(true, true, false, null)]   // DeviceSyncFailure: SimulateDeviceFailure = true
    [InlineData(false, false, false, null)] // CapabilityUnsupported: IsSupportScene = false
    [InlineData(false, true, true, "1")]    // SaveDataFailure: SimulateSaveDataFailure = true
    public async Task VwWindowSceneCommand_AddWindow_DeviceFailureModes_ThrowsAndDoesNotInsertRecord_Theory(
        bool simulateDeviceFailure,
        bool isSupportScene,
        bool simulateSaveDataFailure,
        string? sceneOutputId)
    {
        // Arrange
        var ctrl = new VwController
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}CTRL_FAIL_{Guid.NewGuid():N}",
            Name = "Failing Device Controller",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(ctrl).ExecuteCommandAsync();

        var baseColFail = Random.Shared.Next(100, 10000) * 10;
        var screen = new VwScreen
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCR_FAIL_{Guid.NewGuid():N}",
            Name = "Panel Fail",
            ControllerId = ctrl.ID,
            GridCol = baseColFail,
            GridRow = 0,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(screen).ExecuteCommandAsync();

        var scene = new VwScene
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCN_FAIL_{Guid.NewGuid():N}",
            Name = "Scene Device Fails",
            ControllerId = ctrl.ID,
            OutputId = sceneOutputId,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();

        host.MockServer.SimulateDeviceFailure = simulateDeviceFailure;
        host.MockServer.IsSupportScene = isSupportScene;
        host.MockServer.SimulateSaveDataFailure = simulateSaveDataFailure;

        var uniqueCode = $"{TestPrefix}WIN_FAIL_{Guid.NewGuid():N}";
        var input = new VwAddWindowSceneInput
        {
            Code = uniqueCode,
            Name = "Window Device Fails",
            SceneId = scene.ID,
            X = baseColFail * VwSceneRegionService.PanelWidthPx + 100,
            Y = 100,
            W = 800,
            H = 600,
            Visible = BaseEnums.SceneWindowVisible.Visible
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _bus.InvokeAsync(input));

        var inserted = await _db.Queryable<VwWindowScene>()
            .FirstAsync(u => u.Code == uniqueCode && u.IsDelete == null);
        Assert.Null(inserted);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Test hồi quy cho lỗi đã phát hiện + sửa: trước đây SyncSceneWindowsToDeviceAsync
    ///              return sớm khi danh sách window resync về RỖNG (xoá window cuối cùng của scene),
    ///              bỏ qua luôn bước SaveSceneDataAsync — khiến thiết bị "hồi sinh" lại window vừa
    ///              xoá ở lần activate kế tiếp (khôi phục snapshot cũ còn window). Test này xoá đúng
    ///              window DUY NHẤT của 1 scene và assert SaveSceneDataAsync vẫn được gọi.
    /// Created date: 16/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneCommand_DeleteLastWindowInScene_StillCallsSaveSceneData_Test()
    {
        host.MockServer.ResetDefaults();

        // Arrange
        var ctrl = new VwController
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}CTRL_LAST_{Guid.NewGuid():N}",
            Name = "Delete Last Window Controller",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(ctrl).ExecuteCommandAsync();

        var baseColLast = Random.Shared.Next(100, 10000) * 10;
        var screen = new VwScreen
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCR_LAST_{Guid.NewGuid():N}",
            Name = "Panel Last",
            ControllerId = ctrl.ID,
            GridCol = baseColLast,
            GridRow = 0,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(screen).ExecuteCommandAsync();

        var scene = new VwScene
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCN_LAST_{Guid.NewGuid():N}",
            Name = "Scene Delete Last Window",
            ControllerId = ctrl.ID,
            OutputId = "1", // Bắt buộc có OutputId thì SaveSceneDataAsync mới được gọi
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();

        var uniqueCode = $"{TestPrefix}WIN_LAST_{Guid.NewGuid():N}";
        var win = new VwWindowScene
        {
            ID = Guid.NewGuid().ToString(),
            Code = uniqueCode,
            Name = "Only Window In Scene",
            SceneId = scene.ID,
            X = baseColLast * VwSceneRegionService.PanelWidthPx,
            Y = 0,
            W = 800,
            H = 600,
            Visible = BaseEnums.SceneWindowVisible.Visible,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(win).ExecuteCommandAsync();

        var deleteInput = new VwDeleteWindowSceneInput { ID = win.ID };

        // Act
        await _bus.InvokeAsync(deleteInput);

        // Assert
        Assert.Equal(1, host.MockServer.SaveSceneDataCallCount);

        var active = await _db.Queryable<VwWindowScene>()
            .FirstAsync(u => u.ID == win.ID && u.IsDelete == null);
        Assert.Null(active);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Kịch bản TOÀN TƯỜNG (ControllerId == null) — thêm 1 cửa sổ vắt qua 2 controller (2x1 panel setup)
    ///              phải tự động cắt lát toạ độ và gửi AddWindowAsync + SaveSceneDataAsync tới CẢ HAI controller.
    /// Created date: 17/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneWorkflow_WholeWallScene_SyncsAndSlicesWindowsToAllControllers_Test()
    {
        host.MockServer.ResetDefaults();

        // 1. Arrange: Xoá controller/screen cũ để kịch bản toàn tường chỉ thấy đúng 2 controller của bài test
        await _db.Deleteable<VwController>().ExecuteCommandAsync();
        await _db.Deleteable<VwScreen>().ExecuteCommandAsync();

        var baseColWW = Random.Shared.Next(100, 10000) * 10;
        var ctrlA = new VwController
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}CTRL_WW_A_{Guid.NewGuid():N}",
            Name = "Whole Wall Ctrl A",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPorts[0]}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        var ctrlB = new VwController
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}CTRL_WW_B_{Guid.NewGuid():N}",
            Name = "Whole Wall Ctrl B",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPorts[1]}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(new[] { ctrlA, ctrlB }).ExecuteCommandAsync();

        var screenA = new VwScreen
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCR_WW_A_{Guid.NewGuid():N}",
            Name = "Screen WW A",
            ControllerId = ctrlA.ID,
            GridCol = baseColWW,
            GridRow = 0,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        var screenB = new VwScreen
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCR_WW_B_{Guid.NewGuid():N}",
            Name = "Screen WW B",
            ControllerId = ctrlB.ID,
            GridCol = baseColWW + 1,
            GridRow = 0,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(new[] { screenA, screenB }).ExecuteCommandAsync();
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);

        var wholeWallScene = new VwScene
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCN_WW_{Guid.NewGuid():N}",
            Name = "Whole Wall Scene Test",
            ControllerId = null, // Toàn tường
            OutputId = "1",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(wholeWallScene).ExecuteCommandAsync();

        var addInput = new VwAddWindowSceneInput
        {
            Code = $"{TestPrefix}WIN_WW_{Guid.NewGuid():N}",
            Name = "Cross Controller Window",
            SceneId = wholeWallScene.ID,
            X = baseColWW * VwSceneRegionService.PanelWidthPx,
            Y = 0,
            W = VwSceneRegionService.PanelWidthPx * 2, // Chiếm cả 2 controller
            H = VwSceneRegionService.PanelHeightPx,
            Visible = BaseEnums.SceneWindowVisible.Visible
        };

        // 2. Act
        await _bus.InvokeAsync(addInput);

        // 3. Assert
        var inserted = await _db.Queryable<VwWindowScene>()
            .FirstAsync(u => u.Code == addInput.Code && u.IsDelete == null);
        Assert.NotNull(inserted);
        // Mỗi controller được dọn dẹp canvas (DeleteAllWindows = 2)
        Assert.Equal(2, host.MockServer.DeleteAllWindowsCallCount);
        // Cửa sổ 2x1 được cắt thành 2 slice cho Ctrl A và Ctrl B (AddWindow = 2)
        Assert.Equal(2, host.MockServer.AddWindowCallCount);
        // Cả 2 controller đều lưu snapshot scene (SaveSceneData = 2)
        Assert.Equal(2, host.MockServer.SaveSceneDataCallCount);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Kịch bản TOÀN TƯỜNG (ControllerId == null) — xoá 1 cửa sổ phải đồng bộ resync
    ///              lại và lưu snapshot trống xuống CẢ HAI controller.
    /// Created date: 17/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneWorkflow_WholeWallScene_DeleteWindow_ResyncsAndClearsAllControllers_Test()
    {
        host.MockServer.ResetDefaults();

        // 1. Arrange: Xoá controller/screen cũ để kịch bản toàn tường chỉ thấy đúng 2 controller của bài test
        await _db.Deleteable<VwController>().ExecuteCommandAsync();
        await _db.Deleteable<VwScreen>().ExecuteCommandAsync();

        var baseColDel = Random.Shared.Next(100, 10000) * 10;
        var ctrlA = new VwController
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}CTRL_WW_DEL_A_{Guid.NewGuid():N}",
            Name = "Whole Wall Ctrl Del A",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPorts[0]}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        var ctrlB = new VwController
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}CTRL_WW_DEL_B_{Guid.NewGuid():N}",
            Name = "Whole Wall Ctrl Del B",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPorts[1]}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(new[] { ctrlA, ctrlB }).ExecuteCommandAsync();

        var screenA = new VwScreen
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCR_WW_DEL_A_{Guid.NewGuid():N}",
            Name = "Screen WW Del A",
            ControllerId = ctrlA.ID,
            GridCol = baseColDel,
            GridRow = 0,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        var screenB = new VwScreen
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCR_WW_DEL_B_{Guid.NewGuid():N}",
            Name = "Screen WW Del B",
            ControllerId = ctrlB.ID,
            GridCol = baseColDel + 1,
            GridRow = 0,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(new[] { screenA, screenB }).ExecuteCommandAsync();
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);

        var wholeWallScene = new VwScene
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCN_WW_DEL_{Guid.NewGuid():N}",
            Name = "Whole Wall Scene Delete Test",
            ControllerId = null, // Toàn tường
            OutputId = "1",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(wholeWallScene).ExecuteCommandAsync();

        var existingWin = new VwWindowScene
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}WIN_WW_DEL_{Guid.NewGuid():N}",
            Name = "Window To Delete",
            SceneId = wholeWallScene.ID,
            X = baseColDel * VwSceneRegionService.PanelWidthPx,
            Y = 0,
            W = VwSceneRegionService.PanelWidthPx * 2,
            H = VwSceneRegionService.PanelHeightPx,
            Visible = BaseEnums.SceneWindowVisible.Visible,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(existingWin).ExecuteCommandAsync();

        host.MockServer.ResetDefaults();

        var deleteInput = new VwDeleteWindowSceneInput { ID = existingWin.ID };

        // 2. Act
        await _bus.InvokeAsync(deleteInput);

        // 3. Assert
        var active = await _db.Queryable<VwWindowScene>()
            .FirstAsync(u => u.ID == existingWin.ID && u.IsDelete == null);
        Assert.Null(active);

        // Cả 2 controller đều được xoá window và lưu snapshot rỗng
        Assert.Equal(2, host.MockServer.DeleteAllWindowsCallCount);
        Assert.Equal(0, host.MockServer.AddWindowCallCount); // Không còn window nào
        Assert.Equal(2, host.MockServer.SaveSceneDataCallCount);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Kiểm thử đổi nguồn tín hiệu trực tiếp cho cửa sổ (Switch Window Source - #3)
    ///              cập nhật thành công SourceId trong CSDL và đồng bộ tới thiết bị.
    /// Created date: 17/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneCommand_SwitchSource_UpdatesRecordAndSyncsDevice_Test()
    {
        // 1. Validate negative first
        var validator = new VwSwitchWindowSourceValidator(_localizer);
        var invalidInput = new VwSwitchWindowSourceInput { ID = "", SourceId = "" };
        var validationResult = await validator.ValidateAsync(invalidInput);
        Assert.False(validationResult.IsValid);

        // 2. Arrange
        var controller = new VwController
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}CTRL_SW_{Guid.NewGuid():N}",
            Name = "Controller Switch Test",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(controller).ExecuteCommandAsync();

        var scene = new VwScene
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCN_SW_{Guid.NewGuid():N}",
            Name = "Scene Switch Test",
            ControllerId = controller.ID,
            OutputId = "1",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();

        var source1 = new VwSource
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SRC_SW1_{Guid.NewGuid():N}",
            Name = "Source 1",
            SignalNo = 16842753,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        var source2 = new VwSource
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SRC_SW2_{Guid.NewGuid():N}",
            Name = "Source 2",
            SignalNo = 16842754,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(new[] { source1, source2 }).ExecuteCommandAsync();

        var window = new VwWindowScene
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}WIN_SW_{Guid.NewGuid():N}",
            Name = "Window Switch Test",
            SceneId = scene.ID,
            SourceId = source1.ID,
            X = 0,
            Y = 0,
            W = 1920,
            H = 1080,
            Visible = BaseEnums.SceneWindowVisible.Visible,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(window).ExecuteCommandAsync();

        host.MockServer.ResetDefaults();

        // 3. Act
        var switchInput = new VwSwitchWindowSourceInput
        {
            ID = window.ID,
            SourceId = source2.ID
        };
        await _bus.InvokeAsync(switchInput);

        // 4. Assert
        var updated = await _db.Queryable<VwWindowScene>().FirstAsync(u => u.ID == window.ID);
        Assert.NotNull(updated);
        Assert.Equal(source2.ID, updated.SourceId);
        Assert.Equal(1, host.MockServer.AddWindowCallCount);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Kiểm thử điều chỉnh Z-Order đưa cửa sổ lên trên cùng (Top Layer - #5).
    /// Created date: 17/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneCommand_SetLayerTop_CallsDeviceSuccessfully_Test()
    {
        // 1. Validate negative first
        var validator = new VwSetWindowLayerValidator(_localizer);
        var invalidInput = new VwSetWindowLayerInput { ID = "" };
        var validationResult = await validator.ValidateAsync(invalidInput);
        Assert.False(validationResult.IsValid);

        // 2. Arrange
        var controller = new VwController
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}CTRL_TOP_{Guid.NewGuid():N}",
            Name = "Controller Top Test",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(controller).ExecuteCommandAsync();

        var scene = new VwScene
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCN_TOP_{Guid.NewGuid():N}",
            Name = "Scene Top Test",
            ControllerId = controller.ID,
            OutputId = "1",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();

        var baseColTop = Random.Shared.Next(100, 10000) * 10;
        var screen = new VwScreen
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCR_TOP_{Guid.NewGuid():N}",
            Name = "Screen Top Test",
            ControllerId = controller.ID,
            GridCol = baseColTop,
            GridRow = 0,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(screen).ExecuteCommandAsync();
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);

        var source = new VwSource
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SRC_TOP_{Guid.NewGuid():N}",
            Name = "Source Top",
            SignalNo = 16842753,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(source).ExecuteCommandAsync();

        var winCode = $"{TestPrefix}WIN_TOP_{Guid.NewGuid():N}";
        var addInput = new VwAddWindowSceneInput
        {
            Code = winCode,
            Name = "Window Top Test",
            SceneId = scene.ID,
            SourceId = source.ID,
            X = baseColTop * VwSceneRegionService.PanelWidthPx,
            Y = 0,
            W = 1920,
            H = 1080,
            ZIndex = 1,
            Visible = BaseEnums.SceneWindowVisible.Visible
        };
        await _bus.InvokeAsync(addInput);

        var window = await _db.Queryable<VwWindowScene>()
            .FirstAsync(w => w.Code == winCode && w.IsDelete == null);
        Assert.NotNull(window);
        Assert.NotNull(window.DeviceWindowId);

        host.MockServer.ResetDefaults();

        // 3. Act
        var setLayerInput = new VwSetWindowLayerInput
        {
            ID = window.ID,
            Action = VwWindowLayerAction.Top
        };
        await _bus.InvokeAsync(setLayerInput);

        // 4. Assert
        Assert.Equal(1, host.MockServer.WindowTopCallCount);
        var updated = await _db.Queryable<VwWindowScene>().FirstAsync(u => u.ID == window.ID);
        Assert.NotNull(updated);
        Assert.True(updated.ZIndex > 1);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Kiểm thử điều chỉnh Z-Order đưa cửa sổ xuống dưới cùng (Bottom Layer - #5).
    /// Created date: 17/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneCommand_SetLayerBottom_CallsDeviceSuccessfully_Test()
    {
        // 1. Arrange
        var controller = new VwController
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}CTRL_BOT_{Guid.NewGuid():N}",
            Name = "Controller Bottom Test",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(controller).ExecuteCommandAsync();

        var scene = new VwScene
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCN_BOT_{Guid.NewGuid():N}",
            Name = "Scene Bottom Test",
            ControllerId = controller.ID,
            OutputId = "1",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();

        var baseColBot = Random.Shared.Next(100, 10000) * 10;
        var screen = new VwScreen
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCR_BOT_{Guid.NewGuid():N}",
            Name = "Screen Bottom Test",
            ControllerId = controller.ID,
            GridCol = baseColBot,
            GridRow = 0,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(screen).ExecuteCommandAsync();
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);

        var source = new VwSource
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SRC_BOT_{Guid.NewGuid():N}",
            Name = "Source Bottom",
            SignalNo = 16842753,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(source).ExecuteCommandAsync();

        var winCode = $"{TestPrefix}WIN_BOT_{Guid.NewGuid():N}";
        var addInput = new VwAddWindowSceneInput
        {
            Code = winCode,
            Name = "Window Bottom Test",
            SceneId = scene.ID,
            SourceId = source.ID,
            X = baseColBot * VwSceneRegionService.PanelWidthPx,
            Y = 0,
            W = 1920,
            H = 1080,
            ZIndex = 5,
            Visible = BaseEnums.SceneWindowVisible.Visible
        };
        await _bus.InvokeAsync(addInput);

        var window = await _db.Queryable<VwWindowScene>()
            .FirstAsync(w => w.Code == winCode && w.IsDelete == null);
        Assert.NotNull(window);
        Assert.NotNull(window.DeviceWindowId);

        host.MockServer.ResetDefaults();

        // 2. Act
        var setLayerInput = new VwSetWindowLayerInput
        {
            ID = window.ID,
            Action = VwWindowLayerAction.Bottom
        };
        await _bus.InvokeAsync(setLayerInput);

        // 3. Assert
        Assert.Equal(1, host.MockServer.WindowBottomCallCount);
        var updated = await _db.Queryable<VwWindowScene>().FirstAsync(u => u.ID == window.ID);
        Assert.NotNull(updated);
        Assert.Equal(0, updated.ZIndex);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Kiểm thử điều chỉnh Z-Order khi DeviceWindowId là null (chưa từng resync thiết bị) —
    ///              vẫn cập nhật Z-Index trong DB thành công mà không throw lỗi và không gọi thiết bị.
    /// Created date: 18/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneCommand_SetLayer_WhenDeviceWindowIdIsNull_UpdatesZIndexWithoutDeviceCall_Test()
    {
        // 1. Arrange
        var scene = new VwScene
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SCN_NULL_{Guid.NewGuid():N}",
            Name = "Scene Null DevWinId Test",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();

        var source = new VwSource
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}SRC_NULL_{Guid.NewGuid():N}",
            Name = "Source Null DevWinId",
            SignalNo = 16842753,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(source).ExecuteCommandAsync();

        var window = new VwWindowScene
        {
            ID = Guid.NewGuid().ToString(),
            Code = $"{TestPrefix}WIN_NULL_{Guid.NewGuid():N}",
            Name = "Window Null DevWinId",
            SceneId = scene.ID,
            SourceId = source.ID,
            DeviceWindowId = null,
            X = 0,
            Y = 0,
            W = 1920,
            H = 1080,
            ZIndex = 1,
            Visible = BaseEnums.SceneWindowVisible.Visible,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(window).ExecuteCommandAsync();
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);

        host.MockServer.ResetDefaults();

        // 2. Act
        var setLayerInput = new VwSetWindowLayerInput
        {
            ID = window.ID,
            Action = VwWindowLayerAction.Top
        };
        await _bus.InvokeAsync(setLayerInput);

        // 3. Assert
        Assert.Equal(0, host.MockServer.WindowTopCallCount);
        var updated = await _db.Queryable<VwWindowScene>().FirstAsync(u => u.ID == window.ID);
        Assert.NotNull(updated);
        Assert.True(updated.ZIndex > 1);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Cập nhật toạ độ cửa sổ trên MockServer qua IVwISAPIDeviceClient và xác thực tăng bộ đếm UpdateWindowCallCount.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneCommand_UpdateWindow_UpdatesGeometryAndIncrementsMockCount_Test()
    {
        host.MockServer.ResetDefaults();
        var client = host.Services.GetRequiredService<IVwISAPIDeviceClient>();
        var controller = new VwController
        {
            ID = $"ctrl-win-upd-{Guid.NewGuid():N}",
            Name = "Test Window Update Controller",
            Code = $"{TestPrefix}CTRL_UPD_{Guid.NewGuid():N}",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable
        };

        var windowRequest = new VwISAPIWindowRequest
        {
            Rect = new VwISAPIRect
            {
                Coordinate = new VwISAPICoordinate { X = 100, Y = 100 },
                Width = 1920,
                Height = 1080
            }
        };

        var result = await client.UpdateWindowAsync(controller, "33554433", windowRequest, wallNo: 2);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(host.MockServer.UpdateWindowCallCount >= 1);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Xoá một cửa sổ đơn lẻ trên MockServer qua IVwISAPIDeviceClient và xác thực tăng bộ đếm DeleteWindowCallCount.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task VwWindowSceneCommand_DeleteSingleWindow_DeletesOneAndIncrementsMockCount_Test()
    {
        host.MockServer.ResetDefaults();
        var client = host.Services.GetRequiredService<IVwISAPIDeviceClient>();
        var controller = new VwController
        {
            ID = $"ctrl-win-del-{Guid.NewGuid():N}",
            Name = "Test Window Delete Controller",
            Code = $"{TestPrefix}CTRL_DEL_{Guid.NewGuid():N}",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable
        };

        var result = await client.DeleteWindowAsync(controller, "33554433", wallNo: 2);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(host.MockServer.DeleteWindowCallCount >= 1);
    }
}
