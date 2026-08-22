namespace Tests.Modules.VideoWall;

/// <summary>
/// Description: Bộ kiểm thử tích hợp cho phân hệ VwScene (Kịch bản hiển thị & Workflow kích hoạt đa vùng)
/// Created date: 15/08/2026
/// </summary>
[Collection("api")]
public class VwSceneTests(Host host) : IDisposable
{
    private const string TestPrefix = "TEST_VWSCN_";
    private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();
    private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
    private readonly BaseCacheService _cache = host.Services.GetRequiredService<BaseCacheService>();
    private readonly IStringLocalizer _localizer = host.Localizer;

    /// <summary>VwEventTriggerLog.TargetSceneId lưu scene.ID (GUID), không mang TestPrefix như
    /// Code — chỉ các test có gọi VwActiveSceneInput (ghi log) mới cần track vào đây để dọn dẹp.</summary>
    private readonly List<string> _activatedSceneIds = [];

    /// <summary>
    /// Author: Đạt
    /// Description: Dọn dẹp dữ liệu test trong CSDL và cache sau khi thực thi xong test suite
    /// Created date: 15/08/2026
    /// </summary>
    public void Dispose()
    {
        host.MockServer.ResetDefaults();

        _db.Deleteable<VwScene>()
            .Where(u => u.Code != null && u.Code.StartsWith(TestPrefix))
            .ExecuteCommand();

        _db.Deleteable<VwController>()
            .Where(u => u.Code != null && u.Code.StartsWith(TestPrefix))
            .ExecuteCommand();

        _db.Deleteable<VwEventRule>()
            .Where(u => u.Code != null && u.Code.StartsWith(TestPrefix))
            .ExecuteCommand();

        if (_activatedSceneIds.Count > 0)
        {
            _db.Deleteable<VwEventTriggerLog>()
                .Where(u => _activatedSceneIds.Contains(u.TargetSceneId!))
                .ExecuteCommand();
        }

        _cache.Remove(CacheConst.Vw.VwScene);
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwEventRule);
        _cache.RemoveByPrefixKey(CacheConst.Vw.VwEventTriggerLog);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Description: Kiểm tra phân trang VwScene trả về danh sách hợp lệ
    /// Created date: 15/08/2026
    /// </summary>
    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 5)]
    [InlineData(2, 20)]
    public async Task VwSceneQuery_Page_ReturnsSuccess_Test(int page, int pageSize)
    {
        // Arrange
        var input = new VwPageSceneInput
        {
            Page = page,
            PageSize = pageSize
        };

        // Act
        var result = await _bus.InvokeAsync<SqlSugarPagedList<VwPageSceneOutput>>(input);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Records);
    }

    /// <summary>
    /// Description: Kiểm tra GetList VwScene trả về thành công sau khi xóa cache
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneQuery_GetList_ReturnsSuccess_Test()
    {
        // Arrange
        _cache.Remove(CacheConst.Vw.VwScene);
        var input = new VwSceneInput();

        // Act
        var result = await _bus.InvokeAsync<List<VwSceneOutput>>(input);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Description: Kiểm tra GetById VwScene trả về đúng kịch bản đã tạo
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneQuery_GetById_ReturnsSuccess_Test()
    {
        // Arrange
        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var scene = new VwScene
        {
            Code = uniqueCode,
            Name = "Test Scene ById",
            Status = BaseEnums.StatusEnum.Enable,
            ActiveScene = BaseEnums.ActiveScene.DeActivate,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();
        _cache.Remove(CacheConst.Vw.VwScene);

        var input = new VwIdSceneInput { ID = scene.ID };

        // Act
        var result = await _bus.InvokeAsync<VwSceneOutput>(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(uniqueCode, result.Code);
    }

    /// <summary>
    /// Description: Kiểm tra VwAddSceneValidator từ chối các input không hợp lệ
    /// Created date: 17/08/2026
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VwSceneCommand_AddVwScene_ValidationRejectsInvalidPayload_Test(string? name)
    {
        var input = new VwAddSceneInput { Name = name };
        var validator = new VwAddSceneValidator(_localizer);
        var result = await validator.ValidateAsync(input);
        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Description: Kiểm tra thêm mới VwScene ghi nhận bản ghi vào CSDL
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneCommand_AddVwScene_InsertsRecord_Test()
    {
        // Arrange
        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var input = new VwAddSceneInput
        {
            Code = uniqueCode,
            Name = "Test Add Scene",
            Status = BaseEnums.StatusEnum.Enable,
            IsDefault = BaseEnums.DefaultEnum.None
        };

        // Validate (FluentValidation)
        var validator = new VwAddSceneValidator(_localizer);
        var valResult = await validator.ValidateAsync(input);
        Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

        // Act
        await _bus.InvokeAsync(input);

        // Assert
        var inserted = await _db.Queryable<VwScene>()
            .FirstAsync(u => u.Code == uniqueCode && u.IsDelete == null);

        Assert.NotNull(inserted);
        Assert.Equal("Test Add Scene", inserted.Name);
    }

    /// <summary>
    /// Description: Kiểm tra cập nhật VwScene thay đổi thông tin bản ghi trong CSDL
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneCommand_UpdateVwScene_UpdatesRecord_Test()
    {
        // Arrange
        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var scene = new VwScene
        {
            Code = uniqueCode,
            Name = "Original Scene Name",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();
        _cache.Remove(CacheConst.Vw.VwScene);

        var updateInput = new VwUpdateSceneInput
        {
            ID = scene.ID,
            Code = uniqueCode,
            Name = "Updated Scene Name",
            Status = BaseEnums.StatusEnum.Enable
        };

        // Validate (FluentValidation)
        var validator = new VwUpdateSceneValidator(_localizer);
        var valResult = await validator.ValidateAsync(updateInput);
        Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));

        // Act
        await _bus.InvokeAsync(updateInput);

        // Assert
        var updated = await _db.Queryable<VwScene>()
            .FirstAsync(u => u.ID == scene.ID && u.IsDelete == null);

        Assert.NotNull(updated);
        Assert.Equal("Updated Scene Name", updated.Name);
    }

    /// <summary>
    /// Description: Kiểm tra VwUpdateSceneValidator từ chối khi ID không hợp lệ
    /// Created date: 17/08/2026
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VwSceneCommand_UpdateVwScene_ValidationRejectsInvalidId_Test(string? invalidId)
    {
        var validator = new VwUpdateSceneValidator(_localizer);
        var result = await validator.ValidateAsync(new VwUpdateSceneInput { ID = invalidId, Name = "Name" });
        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Description: Kiểm tra VwDeleteSceneValidator từ chối khi ID không hợp lệ
    /// Created date: 17/08/2026
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VwSceneCommand_DeleteVwScene_ValidationRejectsInvalidId_Test(string? invalidId)
    {
        var validator = new VwDeleteSceneValidator(_localizer);
        var invalidResult = await validator.ValidateAsync(new VwDeleteSceneInput { ID = invalidId });
        Assert.False(invalidResult.IsValid);
    }

    /// <summary>
    /// Description: Kiểm tra xóa 1 VwScene thực hiện xóa mềm (IsDelete != null)
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneCommand_DeleteVwScene_SoftDeletesRecord_Test()
    {
        // Chuẩn bị dữ liệu trong CSDL
        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var scene = new VwScene
        {
            Code = uniqueCode,
            Name = "Scene To Delete",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();
        _cache.Remove(CacheConst.Vw.VwScene);

        var deleteInput = new VwDeleteSceneInput { ID = scene.ID };
        var validator = new VwDeleteSceneValidator(_localizer);
        var valResult = await validator.ValidateAsync(deleteInput);
        Assert.True(valResult.IsValid, string.Join(";", valResult.Errors.Select(e => e.ErrorMessage)));

        // 4. Thực thi và assert
        await _bus.InvokeAsync(deleteInput);

        var active = await _db.Queryable<VwScene>()
            .FirstAsync(u => u.ID == scene.ID && u.IsDelete == null);
        Assert.Null(active);

        var deleted = await _db.Queryable<VwScene>()
            .ClearFilter()
            .FirstAsync(u => u.ID == scene.ID && u.IsDelete != null);
        Assert.NotNull(deleted);
    }

    /// <summary>
    /// Description: Kiểm tra xóa nhiều VwScene thực hiện xóa mềm danh sách bản ghi
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneCommand_BatchDeleteVwScene_SoftDeletesRecords_Test()
    {
        var validator = new VwDeleteSceneValidator(_localizer);
        var uniqueCode1 = $"{TestPrefix}{Guid.NewGuid():N}";
        var uniqueCode2 = $"{TestPrefix}{Guid.NewGuid():N}";

        var scene1 = new VwScene
        {
            Code = uniqueCode1,
            Name = "Batch Delete Scene 1",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        var scene2 = new VwScene
        {
            Code = uniqueCode2,
            Name = "Batch Delete Scene 2",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(new[] { scene1, scene2 }).ExecuteCommandAsync();
        _cache.Remove(CacheConst.Vw.VwScene);

        var batchInput = new List<VwDeleteSceneInput>
        {
            new() { ID = scene1.ID },
            new() { ID = scene2.ID }
        };

        foreach (var item in batchInput)
        {
            var valResult = await validator.ValidateAsync(item);
            Assert.True(valResult.IsValid, string.Join("; ", valResult.Errors.Select(e => e.ErrorMessage)));
        }

        await _bus.InvokeAsync(batchInput);

        var countActive = await _db.Queryable<VwScene>()
            .Where(u => (u.ID == scene1.ID || u.ID == scene2.ID) && u.IsDelete == null)
            .CountAsync();
        Assert.Equal(0, countActive);
    }

    /// <summary>
    /// Description: Kiểm tra kích hoạt kịch bản trực tiếp bằng SceneCode (ActiveVwScene)
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneWorkflow_ActiveBySceneCode_DirectActivation_Test()
    {
        // Arrange
        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var scene = new VwScene
        {
            Code = uniqueCode,
            Name = "Scene Direct Activation",
            Status = BaseEnums.StatusEnum.Enable,
            ActiveScene = BaseEnums.ActiveScene.DeActivate,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();
        _activatedSceneIds.Add(scene.ID);

        var input = new VwActiveSceneInput
        {
            Code = uniqueCode
        };

        // Act
        var result = await _bus.InvokeAsync<VwActiveSceneOutput>(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(uniqueCode, result.Code);
        Assert.Equal(BaseEnums.ActiveScene.Activate, result.ActiveScene);

        var dbScene = await _db.Queryable<VwScene>().FirstAsync(u => u.ID == scene.ID);
        Assert.Equal(BaseEnums.ActiveScene.Activate, dbScene.ActiveScene);
    }

    /// <summary>
    /// Description: Kiểm tra kích hoạt kịch bản theo EventTypeId qua bảng luật VwEventRule
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneWorkflow_ActiveByEventTypeId_UsesEventRule_Test()
    {
        // Arrange
        var sceneCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var scene = new VwScene
        {
            Code = sceneCode,
            Name = "Event Target Scene",
            Status = BaseEnums.StatusEnum.Enable,
            ActiveScene = BaseEnums.ActiveScene.DeActivate,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();
        _activatedSceneIds.Add(scene.ID);

        var eventTypeId = $"EVENT_{Guid.NewGuid():N}";
        var ruleCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var rule = new VwEventRule
        {
            Code = ruleCode,
            EventTypeId = eventTypeId,
            TargetSceneId = scene.ID,
            Priority = "HIGH",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(rule).ExecuteCommandAsync();

        var input = new VwActiveSceneInput
        {
            EventTypeId = eventTypeId
        };

        // Act
        var result = await _bus.InvokeAsync<VwActiveSceneOutput>(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(scene.ID, result.ID);
        Assert.Equal(rule.ID, result.RuleId);
        Assert.NotNull(result.TriggerLogId);
    }


    /// <summary>
    /// Description: Kiểm tra GetActive trả về kịch bản đang được kích hoạt gần nhất
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneWorkflow_GetActive_ReturnsActiveScene_Test()
    {
        // Arrange
        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var scene = new VwScene
        {
            Code = uniqueCode,
            Name = "Active Scene For GetActive",
            Status = BaseEnums.StatusEnum.Enable,
            ActiveScene = BaseEnums.ActiveScene.Activate,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();

        var input = new VwGetActiveSceneInput();

        // Act
        var result = await _bus.InvokeAsync<VwGetActiveSceneOutput>(input);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Description: Kiểm tra GetActiveRegions trả về danh sách trạng thái kịch bản từng vùng controller
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneWorkflow_GetActiveRegions_ReturnsActiveScenePerController_Test()
    {
        // Arrange
        var ctrlCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var sceneCode = $"{TestPrefix}{Guid.NewGuid():N}";

        var scene = new VwScene
        {
            Code = sceneCode,
            Name = "Region Scene",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();

        var controller = new VwController
        {
            Code = ctrlCode,
            Name = "Region Controller",
            ActiveSceneId = scene.ID,
            ActiveSceneAt = DateTime.Now,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(controller).ExecuteCommandAsync();

        var input = new VwGetActiveRegionInput { ControllerId = controller.ID };

        // Act
        var result = await _bus.InvokeAsync<List<VwActiveRegionOutput>>(input);

        // Assert
        Assert.NotNull(result);
        var region = result.FirstOrDefault(r => r.ControllerId == controller.ID);
        Assert.NotNull(region);
        Assert.Equal(scene.ID, region.SceneId);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Scene có VwScene.OutputId (map ISAPI Scene ID) — khi kích hoạt phải gọi
    ///              IVwISAPIDeviceClient.ActivateSceneAsync trước khi ghi DB; thiết bị (giả) trả về
    ///              thành công thì DB vẫn cập nhật đúng như luồng không map thiết bị.
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneWorkflow_ActivateScene_WithMappedDevice_PushesToDeviceAndUpdatesDb_Test()
    {
        // Arrange
        var ctrlCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var sceneCode = $"{TestPrefix}{Guid.NewGuid():N}";

        var controller = new VwController
        {
            Code = ctrlCode,
            Name = "Mapped Device Controller",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(controller).ExecuteCommandAsync();

        var scene = new VwScene
        {
            Code = sceneCode,
            Name = "Scene Mapped To Device",
            ControllerId = controller.ID,
            OutputId = "1",
            Status = BaseEnums.StatusEnum.Enable,
            ActiveScene = BaseEnums.ActiveScene.DeActivate,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();
        _activatedSceneIds.Add(scene.ID);

        var input = new VwActiveSceneInput { Code = sceneCode };

        // Act
        var result = await _bus.InvokeAsync<VwActiveSceneOutput>(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(BaseEnums.ActiveScene.Activate, result.ActiveScene);

        var dbController = await _db.Queryable<VwController>().FirstAsync(u => u.ID == controller.ID);
        Assert.Equal(scene.ID, dbController.ActiveSceneId);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Thiết bị gặp lỗi hoặc không hỗ trợ chức năng scene — handler phải fail-fast:
    ///              throw ngay và KHÔNG ghi DB (VwController.ActiveSceneId/VwScene.ActiveScene giữ nguyên).
    /// Created date: 19/08/2026
    /// </summary>
    [Theory]
    [InlineData(true, true)]   // Thiết bị lỗi (SimulateDeviceFailure = true)
    [InlineData(false, false)] // Thiết bị không hỗ trợ tính năng (IsSupportScene = false)
    public async Task VwSceneWorkflow_ActivateScene_DeviceOrCapabilityFails_ThrowsAndDoesNotUpdateDb_Theory(
        bool simulateDeviceFailure,
        bool isSupportScene)
    {
        // Arrange
        var ctrlCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var sceneCode = $"{TestPrefix}{Guid.NewGuid():N}";

        var controller = new VwController
        {
            Code = ctrlCode,
            Name = "Failing Device Controller",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(controller).ExecuteCommandAsync();

        var scene = new VwScene
        {
            Code = sceneCode,
            Name = "Scene Device Fails",
            ControllerId = controller.ID,
            OutputId = "1",
            Status = BaseEnums.StatusEnum.Enable,
            ActiveScene = BaseEnums.ActiveScene.DeActivate,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();
        _activatedSceneIds.Add(scene.ID);

        host.MockServer.SimulateDeviceFailure = simulateDeviceFailure;
        host.MockServer.IsSupportScene = isSupportScene;

        var input = new VwActiveSceneInput { Code = sceneCode };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _bus.InvokeAsync<VwActiveSceneOutput>(input));

        var dbController = await _db.Queryable<VwController>().FirstAsync(u => u.ID == controller.ID);
        Assert.Null(dbController.ActiveSceneId);

        var dbScene = await _db.Queryable<VwScene>().FirstAsync(u => u.ID == scene.ID);
        Assert.Equal(BaseEnums.ActiveScene.DeActivate, dbScene.ActiveScene);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Kịch bản TOÀN TƯỜNG chiếm ≥2 controller — controller A activate thành công,
    ///              controller B thất bại giữa vòng lặp. Handler phải fail-fast NGAY khi B lỗi:
    ///              không rollback được lệnh đã gửi cho A ở tầng thiết bị (giới hạn đã biết), nhưng
    ///              bắt buộc KHÔNG ghi DB cho CẢ HAI controller (transaction chỉ mở sau khi toàn bộ
    ///              vòng lặp thiết bị thành công) — tránh trạng thái DB nói "đã activate" nhưng thực
    ///              tế 1 vùng tường vẫn chiếu kịch bản cũ.
    /// Created date: 16/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneWorkflow_ActivateScene_WholeWall_PartialDeviceFailure_ThrowsAndDoesNotUpdateAnyController_Test()
    {
        // Arrange
        var sceneCode = $"{TestPrefix}{Guid.NewGuid():N}";

        var ctrlA = new VwController
        {
            Code = $"{TestPrefix}CTRL_A_{Guid.NewGuid():N}",
            Name = "Whole Wall Controller A",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPorts[0]}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        var ctrlB = new VwController
        {
            Code = $"{TestPrefix}CTRL_B_{Guid.NewGuid():N}",
            Name = "Whole Wall Controller B",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPorts[1]}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(new[] { ctrlA, ctrlB }).ExecuteCommandAsync();

        host.MockServer.SimulateFailurePorts.Add(VwISAPIMockServerHikvision.DefaultPorts[1]);

        var scene = new VwScene
        {
            Code = sceneCode,
            Name = "Whole Wall Scene Partial Failure",
            ControllerId = null, // Toàn tường — chiếm mọi controller
            OutputId = "1",
            Status = BaseEnums.StatusEnum.Enable,
            ActiveScene = BaseEnums.ActiveScene.DeActivate,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();
        _activatedSceneIds.Add(scene.ID);

        var input = new VwActiveSceneInput { Code = sceneCode };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => _bus.InvokeAsync<VwActiveSceneOutput>(input));

        var dbCtrlA = await _db.Queryable<VwController>().FirstAsync(u => u.ID == ctrlA.ID);
        var dbCtrlB = await _db.Queryable<VwController>().FirstAsync(u => u.ID == ctrlB.ID);
        Assert.Null(dbCtrlA.ActiveSceneId);
        Assert.Null(dbCtrlB.ActiveSceneId);

        var dbScene = await _db.Queryable<VwScene>().FirstAsync(u => u.ID == scene.ID);
        Assert.Equal(BaseEnums.ActiveScene.DeActivate, dbScene.ActiveScene);

        var triggerLog = await _db.Queryable<VwEventTriggerLog>()
            .FirstAsync(u => u.TargetSceneId == scene.ID);
        Assert.NotNull(triggerLog);
        Assert.Equal(BaseEnums.SuccessEnums.Fail, triggerLog.Success);
    }

    #region EventTriggerLog Verification Tests

    /// <summary>
    /// Author: Đạt
    /// Description: Kích hoạt trực tiếp bằng SceneCode phải ghi log Success với TargetSceneId đúng
    ///              và RuleId rỗng (không đi qua VwEventRule).
    /// Created date: 16/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneWorkflow_ActivateSceneByCode_WritesSuccessLogWithDetails_Test()
    {
        // Arrange
        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var scene = new VwScene
        {
            Code = uniqueCode,
            Name = "Trigger Log Scene By Code",
            Status = BaseEnums.StatusEnum.Enable,
            ActiveScene = BaseEnums.ActiveScene.DeActivate,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();
        _activatedSceneIds.Add(scene.ID);

        var input = new VwActiveSceneInput { Code = uniqueCode };

        // Act
        var result = await _bus.InvokeAsync<VwActiveSceneOutput>(input);

        // Assert
        var log = await _db.Queryable<VwEventTriggerLog>()
            .FirstAsync(u => u.ID == result.TriggerLogId);

        Assert.NotNull(log);
        Assert.Equal(scene.ID, log.TargetSceneId);
        Assert.Equal(BaseEnums.SuccessEnums.Success, log.Success);
        Assert.Null(log.RuleId);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Kích hoạt qua EventTypeId/VwEventRule phải ghi log Success kèm đúng RuleId và
    ///              EventTypeId của luật đã dùng để kích hoạt.
    /// Created date: 16/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneWorkflow_ActivateByEventTypeId_WritesSuccessLogWithRuleId_Test()
    {
        // Arrange
        var sceneCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var scene = new VwScene
        {
            Code = sceneCode,
            Name = "Trigger Log Scene By Rule",
            Status = BaseEnums.StatusEnum.Enable,
            ActiveScene = BaseEnums.ActiveScene.DeActivate,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();
        _activatedSceneIds.Add(scene.ID);

        var eventTypeId = $"EVENT_{Guid.NewGuid():N}";
        var ruleCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var rule = new VwEventRule
        {
            Code = ruleCode,
            EventTypeId = eventTypeId,
            TargetSceneId = scene.ID,
            Priority = "HIGH",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(rule).ExecuteCommandAsync();

        var input = new VwActiveSceneInput { EventTypeId = eventTypeId };

        // Act
        var result = await _bus.InvokeAsync<VwActiveSceneOutput>(input);

        // Assert
        var log = await _db.Queryable<VwEventTriggerLog>()
            .FirstAsync(u => u.ID == result.TriggerLogId);

        Assert.NotNull(log);
        Assert.Equal(scene.ID, log.TargetSceneId);
        Assert.Equal(BaseEnums.SuccessEnums.Success, log.Success);
        Assert.Equal(rule.ID, log.RuleId);
        Assert.Equal(eventTypeId, log.EventTypeId);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Kích hoạt kịch bản đã Disable ném ngoại lệ nhưng vẫn phải ghi 1 bản ghi log
    ///              Fail với TargetSceneId đúng — không được bỏ sót log khi thất bại.
    /// Created date: 16/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneWorkflow_ActivateDisabledScene_WritesFailLog_Test()
    {
        // Arrange
        var uniqueCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var scene = new VwScene
        {
            Code = uniqueCode,
            Name = "Trigger Log Disabled Scene",
            Status = BaseEnums.StatusEnum.Disable,
            ActiveScene = BaseEnums.ActiveScene.DeActivate,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();
        _activatedSceneIds.Add(scene.ID);

        var input = new VwActiveSceneInput { Code = uniqueCode };

        // Act
        await Assert.ThrowsAnyAsync<Exception>(() => _bus.InvokeAsync<VwActiveSceneOutput>(input));

        // Assert
        var log = await _db.Queryable<VwEventTriggerLog>()
            .Where(u => u.TargetSceneId == scene.ID)
            .OrderByDescending(u => u.CreateTime)
            .FirstAsync();

        Assert.NotNull(log);
        Assert.Equal(BaseEnums.SuccessEnums.Fail, log.Success);
    }

    #endregion
}
