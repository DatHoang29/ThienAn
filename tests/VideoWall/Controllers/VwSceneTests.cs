using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Module.VideoWall.Core.Interfaces;
using Newtonsoft.Json;
using Shared.Core.Utilities.Constants;

namespace Tests.Modules.VideoWall;

/// <summary>
/// Description: Bộ kiểm thử tích hợp cho phân hệ VwScene (Kịch bản hiển thị & Workflow kích hoạt đa vùng)
/// Created date: 15/08/2026
/// </summary>
[Collection("api")]
public class VwSceneTests(Host host)
{
    private const string TestPrefix = "TEST_VWSCN_";
    private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();
    private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
    private readonly BaseCacheService _cache = host.Services.GetRequiredService<BaseCacheService>();
    private readonly IStringLocalizer _localizer = host.Localizer;

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
    /// Description: Tạo Scene với ControllerId rỗng phải bị chặn bởi validator — kể cả SuperAdmin,
    ///              không còn khái niệm kịch bản "toàn tường" từ 2026-09-14.
    /// Created date: 14/09/2026
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VwSceneCommand_AddVwScene_NullOrEmptyControllerId_IsRejectedByValidator_Test(string? controllerId)
    {
        var input = new VwAddSceneInput
        {
            Name = "Test Scene",
            Code = $"TST_{Guid.NewGuid():N}",
            Status = BaseEnums.StatusEnum.Enable,
            ControllerId = controllerId
        };
        var validator = new VwAddSceneValidator(_localizer);
        var result = await validator.ValidateAsync(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "ControllerId" &&
            e.ErrorMessage.Contains("Vui lòng chọn bộ điều khiển"));
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

        // ControllerId bắt buộc từ 2026-09-14 — tạo một controller tạm để test
        var ctrl = new VwController
        {
            Code = $"{TestPrefix}CTRL_{Guid.NewGuid():N}",
            Name = "Test Controller (Add Scene)",
            IP = "192.168.99.1",
            Account = "admin",
            PassWord = "pass",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(ctrl).ExecuteCommandAsync();

        var input = new VwAddSceneInput
        {
            Code = uniqueCode,
            Name = "Test Add Scene",
            Status = BaseEnums.StatusEnum.Enable,
            IsDefault = BaseEnums.DefaultEnum.None,
            ControllerId = ctrl.ID
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
        Assert.Equal(ctrl.ID, inserted.ControllerId);
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

        // ControllerId bắt buộc từ 2026-09-14 — tạo một controller tạm để test
        var ctrl = new VwController
        {
            Code = $"{TestPrefix}CTRL_{Guid.NewGuid():N}",
            Name = "Test Controller (Update Scene)",
            IP = "192.168.99.2",
            Account = "admin",
            PassWord = "pass",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(ctrl).ExecuteCommandAsync();

        var scene = new VwScene
        {
            Code = uniqueCode,
            Name = "Original Scene Name",
            ControllerId = ctrl.ID,
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
            Status = BaseEnums.StatusEnum.Enable,
            ControllerId = ctrl.ID
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
        Assert.Equal(ctrl.ID, updated.ControllerId);
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

        var input = new VwActiveSceneInput
        {
            Code = uniqueCode
        };

        // Act
        var result = await _bus.InvokeAsync<VwActiveSceneOutput>(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(uniqueCode, result.Code);
        Assert.NotNull(result.TriggerLogId);

        var dbScene = await _db.Queryable<VwScene>().FirstAsync(u => u.ID == scene.ID);
        Assert.Equal(BaseEnums.ActiveScene.DeActivate, dbScene.ActiveScene);
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
    /// Description: Kịch bản có liên kết thiết bị — theo cơ chế Fire-and-Forget, WebAPI publish lệnh xuống Worker qua NATS và trả về ngay mà không tự cập nhật ActiveSceneId trong DB
    /// Created date: 15/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneWorkflow_ActivateScene_WithMappedDevice_PublishesCommandFireAndForget_Test()
    {
        host.MockServer.ResetDefaults();

        // Arrange
        var ctrlCode = $"{TestPrefix}{Guid.NewGuid():N}";
        var sceneCode = $"{TestPrefix}{Guid.NewGuid():N}";

        var controller = new VwController
        {
            Code = ctrlCode,
            Name = "Mapped Device Controller",
            Role = "center",
            IntegrationMode = "cascade",
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

        var input = new VwActiveSceneInput { Code = sceneCode };

        // Act
        var result = await _bus.InvokeAsync<VwActiveSceneOutput>(input);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.TriggerLogId);

        var dbController = await _db.Queryable<VwController>().FirstAsync(u => u.ID == controller.ID);
        Assert.Null(dbController.ActiveSceneId);
    }

    #region EventTriggerLog Verification Tests

    /// <summary>
    /// Author: Đạt
    /// Description: Kích hoạt trực tiếp bằng SceneCode phải ghi log Success với SceneId đúng
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
    ///              Fail với SceneId đúng — không được bỏ sót log khi thất bại.
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

    /// <summary>
    /// Author: Đạt
    /// Description: Đọc kịch bản đang hoạt động trên thiết bị qua IVwISAPIDeviceClient sau khi ActivateScene và xác thực tăng GetActiveSceneCallCount.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneWorkflow_GetActiveScene_ReturnsCurrentRunningScene_Test()
    {
        host.MockServer.ResetDefaults();
        var client = host.Services.GetRequiredService<IVwISAPIDeviceClient>();
        var controller = new VwController
        {
            ID = $"ctrl-scn-act-{Guid.NewGuid():N}",
            Name = "Test Active Scene Controller",
            Code = $"{TestPrefix}CTRL_ACT_{Guid.NewGuid():N}",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable
        };

        // 1. GetActiveScene ban đầu (mặc định SID = 1)
        var result1 = await client.GetActiveSceneAsync(controller, wallNo: 2);
        Assert.NotNull(result1);
        Assert.True(result1.Success);
        Assert.NotNull(result1.Data);
        Assert.Equal(1, result1.Data.SceneId);
        Assert.True(host.MockServer.GetActiveSceneCallCount >= 1);

        // 2. Kích hoạt sang scene SID 3
        var actResult = await client.ActivateSceneAsync(controller, "3", wallNo: 2);
        Assert.True(actResult.Success);

        // 3. GetActiveScene sau kích hoạt phải trả về SID 3
        var result2 = await client.GetActiveSceneAsync(controller, wallNo: 2);
        Assert.NotNull(result2);
        Assert.True(result2.Success);
        Assert.NotNull(result2.Data);
        Assert.Equal(3, result2.Data.SceneId);
        Assert.True(host.MockServer.GetActiveSceneCallCount >= 2);
    }

    [Fact]
    public async Task VwSceneWorkflow_ActivateByEvent_PicksHigherPriority_WhenMultipleRulesExist_Test()
    {
        // Arrange
        var eventTypeId = $"EVT_PRIO_{Guid.NewGuid():N}";

        var sceneLow = new VwScene
        {
            Code = $"{TestPrefix}SCN_LOW_{Guid.NewGuid():N}",
            Name = "Scene Low Priority",
            Status = BaseEnums.StatusEnum.Enable,
            ActiveScene = BaseEnums.ActiveScene.DeActivate,
            CreateTime = DateTime.Now.AddMinutes(-10)
        };
        var sceneNormal = new VwScene
        {
            Code = $"{TestPrefix}SCN_NORM_{Guid.NewGuid():N}",
            Name = "Scene Normal Priority",
            Status = BaseEnums.StatusEnum.Enable,
            ActiveScene = BaseEnums.ActiveScene.DeActivate,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(new[] { sceneLow, sceneNormal }).ExecuteCommandAsync();

        var ruleLow = new VwEventRule
        {
            Code = $"{TestPrefix}RULE_LOW_{Guid.NewGuid():N}",
            EventTypeId = eventTypeId,
            TargetSceneId = sceneLow.ID,
            Priority = "LOW",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now.AddMinutes(-5)
        };
        var ruleNormal = new VwEventRule
        {
            Code = $"{TestPrefix}RULE_NORM_{Guid.NewGuid():N}",
            EventTypeId = eventTypeId,
            TargetSceneId = sceneNormal.ID,
            Priority = "NORMAL",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(new[] { ruleLow, ruleNormal }).ExecuteCommandAsync();

        var input = new VwActiveSceneInput { EventTypeId = eventTypeId };

        // Act
        var result = await _bus.InvokeAsync<VwActiveSceneOutput>(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sceneNormal.ID, result.ID);
        Assert.Equal(sceneNormal.Code, result.Code);
    }

    [Fact]
    public async Task VwSceneWorkflow_ActivateByEvent_PicksCriticalOverHigh_Test()
    {
        // Arrange
        var eventTypeId = $"EVT_CRIT_{Guid.NewGuid():N}";

        var sceneHigh = new VwScene
        {
            Code = $"{TestPrefix}SCN_HIGH_{Guid.NewGuid():N}",
            Name = "Scene High Priority",
            Status = BaseEnums.StatusEnum.Enable,
            ActiveScene = BaseEnums.ActiveScene.DeActivate,
            CreateTime = DateTime.Now.AddMinutes(-10)
        };
        var sceneCritical = new VwScene
        {
            Code = $"{TestPrefix}SCN_CRIT_{Guid.NewGuid():N}",
            Name = "Scene Critical Priority",
            Status = BaseEnums.StatusEnum.Enable,
            ActiveScene = BaseEnums.ActiveScene.DeActivate,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(new[] { sceneHigh, sceneCritical }).ExecuteCommandAsync();

        var ruleHigh = new VwEventRule
        {
            Code = $"{TestPrefix}RULE_HIGH_{Guid.NewGuid():N}",
            EventTypeId = eventTypeId,
            TargetSceneId = sceneHigh.ID,
            Priority = "HIGH",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now.AddMinutes(-5)
        };
        var ruleCritical = new VwEventRule
        {
            Code = $"{TestPrefix}RULE_CRIT_{Guid.NewGuid():N}",
            EventTypeId = eventTypeId,
            TargetSceneId = sceneCritical.ID,
            Priority = "CRITICAL",
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(new[] { ruleHigh, ruleCritical }).ExecuteCommandAsync();

        var input = new VwActiveSceneInput { EventTypeId = eventTypeId };

        // Act
        var result = await _bus.InvokeAsync<VwActiveSceneOutput>(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sceneCritical.ID, result.ID);
        Assert.Equal(sceneCritical.Code, result.Code);
    }

    /// <summary>
    /// Description: Kích hoạt kịch bản có chứa ít nhất 1 cửa sổ nằm ngoài vùng lưới được cấp của người dùng -> throw và không kích hoạt (Task 2)
    /// </summary>
    [Fact]
    public async Task VwSceneWorkflow_ActivateScene_WindowOutsideAllowedArea_ThrowsException_Test()
    {
        var orgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
        var account = $"{TestPrefix}USER_{Guid.NewGuid():N}";
        var httpContextAccessor = host.Services.GetRequiredService<IHttpContextAccessor>();

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimConst.AccountType, "333"),
            new Claim(ClaimConst.OrgId, orgId),
            new Claim(ClaimConst.UserId, "test-user-id"),
            new Claim(ClaimConst.Account, account),
            new Claim(ClaimTypes.Name, account)
        }, "TestAuth");
        httpContextAccessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        VwController? controller = null;
        VwScene? scene = null;
        VwWindowScene? win = null;

        try
        {
            // Controller & Scene
            controller = new VwController
            {
                Code = $"{TestPrefix}CTRL_{Guid.NewGuid():N}",
                Name = "Test Controller",
                Role = "sub",
                OrgId = orgId,
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(controller).ExecuteCommandAsync();

            scene = new VwScene
            {
                Code = $"{TestPrefix}SCN_{Guid.NewGuid():N}",
                Name = "Test Scene Outside Win",
                ControllerId = controller.ID,
                Status = BaseEnums.StatusEnum.Enable,
                ActiveScene = BaseEnums.ActiveScene.DeActivate,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            // Permission: user only has cell (0, 0)
            var allowedCells = new List<VwGridCell> { new() { Col = 0, Row = 0 } };
            var perm = new VwWallPermission
            {
                UserId = account,
                OrgId = orgId,
                Config = JsonConvert.SerializeObject(allowedCells),
                CreateTime = DateTime.Now
            };
            await _db.Insertable(perm).ExecuteCommandAsync();

            // Window in scene placed at cell (2, 2) -> outside
            win = new VwWindowScene
            {
                Code = $"{TestPrefix}WIN_{Guid.NewGuid():N}",
                Name = "Outside Window in Scene",
                SceneId = scene.ID,
                X = 3840,
                Y = 2160,
                W = 1920,
                H = 1080,
                Visible = BaseEnums.SceneWindowVisible.Visible,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(win).ExecuteCommandAsync();

            var input = new VwActiveSceneInput { Code = scene.Code };

            var ex = await Record.ExceptionAsync(() => _bus.InvokeAsync<VwActiveSceneOutput>(input));

            Assert.NotNull(ex);
            Assert.Contains("ngoài khu vực màn hình", ex.Message);

            // Assert DB is not activated
            var dbScene = await _db.Queryable<VwScene>().FirstAsync(s => s.ID == scene.ID);
            Assert.Equal(BaseEnums.ActiveScene.DeActivate, dbScene.ActiveScene);
        }
        finally
        {
            httpContextAccessor.HttpContext = null;
            await _db.Deleteable<VwWallPermission>(p => p.UserId == account).ExecuteCommandAsync();
            if (win != null) await _db.Deleteable<VwWindowScene>(w => w.ID == win.ID).ExecuteCommandAsync();
            if (scene != null) await _db.Deleteable<VwScene>(s => s.ID == scene.ID).ExecuteCommandAsync();
            if (controller != null) await _db.Deleteable<VwController>(c => c.ID == controller.ID).ExecuteCommandAsync();
        }
    }

    #endregion
}
