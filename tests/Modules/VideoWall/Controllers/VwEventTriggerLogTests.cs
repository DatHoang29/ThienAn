namespace Tests.Modules.VideoWall;

/// <summary>
/// Author: Đạt
/// Description: Kiểm thử tích hợp nhật ký kích hoạt kịch bản và thao tác thiết bị VwEventTriggerLog.
///
///              Phủ ba nhóm hành vi:
///                1. REST Add + Page/GetList, gồm việc server đóng dấu OrgId và bỏ qua giá trị
///                   client tự khai; lọc theo RuleId, EventTypeId, ControllerId, OccurredAt.
///                2. Ghi log tự động sau mỗi lệnh device-setup — MỘT DÒNG cho MỘT BƯỚC ISAPI, kể cả
///                   bước Skipped của chế độ chạy thử, và kể cả khi lệnh ném ngoại lệ.
///                3. Ghi log sau ActivateScene: cả dòng nghiệp vụ và dòng bước thiết bị được ghi
///                   vào cùng bảng VwEventTriggerLog, phân biệt bằng thông tin bước/thiết bị.
///
///              Mọi bài test tự cô lập dữ liệu bằng prefix + GUID, không dọn dữ liệu ở class này
///              (việc đó chỉ làm một lần ở Host.ClearAllData).
/// Created date: 26/08/2026
/// </summary>
[Collection("api")]
public class VwEventTriggerLogTests(Host host)
{
    private const string TestPrefix = "TEST_VWLOG_";

    private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();
    private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
    private readonly IStringLocalizer _localizer = host.Localizer;

    /// <summary>
    /// Description: Seed một bộ điều khiển trỏ vào MockServer, kèm OrgId để kiểm việc đóng dấu.
    /// </summary>
    private async Task<VwController> SeedControllerAsync(string? orgId = null)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var controller = new VwController
        {
            ID = $"{TestPrefix}CTRL_{suffix}",
            Code = $"{TestPrefix}CTRL_{suffix}",
            Name = "Event Trigger Log Controller",
            OrgId = orgId,
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable,
            CreateTime = DateTime.Now
        };

        await _db.Insertable(controller).ExecuteCommandAsync();
        return controller;
    }

    /// <summary>Đọc thẳng từ CSDL các dòng log của một bộ điều khiển, mới nhất trước.</summary>
    private Task<List<VwEventTriggerLog>> ReadRowsAsync(string controllerId) =>
        _db.Queryable<VwEventTriggerLog>()
            .Where(u => u.IsDelete == null && u.ControllerId == controllerId)
            .OrderBy(u => u.StepOrder)
            .ToListAsync();

    // ════════════════════════════════════════════════════════════════════════
    // 1. REST Add + Page/GetList
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Author: Đạt
    /// Description: Add phải ghi được bản ghi, đóng dấu OrgId + ControllerName từ VwController và
    ///              BỎ QUA OrgId do client tự khai (endpoint Add là anonymous nên không tin client).
    ///              Validator được kiểm ngay trong luồng test này theo quy ước dự án.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task VwEventTriggerLogCommand_Add_StampsOrgIdFromControllerAndIgnoresClientValue_Test()
    {
        // Arrange
        var orgId = $"{TestPrefix}ORG_{Guid.NewGuid():N}";
        var controller = await SeedControllerAsync(orgId);

        var input = new VwAddEventTriggerLogInput
        {
            Action = VwEventTriggerAction.Isapi,
            ControllerId = controller.ID,
            OrgId = "ORG_CLIENT_TU_KHAI",
            ControllerName = "TÊN CLIENT TỰ KHAI",
            StepOrder = 1,
            StepName = "GET capabilities",
            Method = "GET",
            Endpoint = "ISAPI/DisplayDev/VideoWall/capabilities",
            HttpStatus = 200,
            Message = "OK",
            RequestPayload = "<request/>",
            ResponsePayload = "<response/>",
            DurationMs = 12
        };

        var validator = new VwAddEventTriggerLogValidator(_localizer);
        var valResult = await validator.ValidateAsync(input);
        Assert.True(valResult.IsValid);

        // Act
        await _bus.InvokeAsync(input);

        // Assert
        var rows = await ReadRowsAsync(controller.ID);
        var row = Assert.Single(rows);

        Assert.Equal(orgId, row.OrgId);
        Assert.Equal(controller.Name, row.ControllerName);
        Assert.Equal(VwEventTriggerAction.Isapi, row.Action);
        Assert.Equal("<request/>", row.RequestPayload);
        Assert.Equal("<response/>", row.ResponsePayload);
        Assert.NotNull(row.OccurredAt);
        Assert.False(string.IsNullOrWhiteSpace(row.OperatorName));

        // Negative rule: Action rỗng và Method lạ đều phải bị validator chặn khi không có RuleId.
        var invalidAction = new VwAddEventTriggerLogInput { Action = null };
        Assert.False((await validator.ValidateAsync(invalidAction)).IsValid);

        var invalidMethod = new VwAddEventTriggerLogInput { Action = VwEventTriggerAction.Isapi, Method = "FETCH" };
        Assert.False((await validator.ValidateAsync(invalidMethod)).IsValid);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Không truyền ControllerId (nhánh Passthrough ad-hoc) thì OrgId để rỗng — theo quy
    ///              ước riêng của bảng này, dòng đó chỉ tài khoản toàn quyền đọc được.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task VwEventTriggerLogCommand_Add_WithoutController_LeavesOrgIdEmpty_Test()
    {
        // Arrange
        var stepName = $"{TestPrefix}ADHOC_{Guid.NewGuid():N}";
        var input = new VwAddEventTriggerLogInput
        {
            Action = VwEventTriggerAction.Isapi,
            StepName = stepName,
            Method = "PUT"
        };

        // Act
        await _bus.InvokeAsync(input);

        // Assert
        var row = await _db.Queryable<VwEventTriggerLog>().FirstAsync(u => u.StepName == stepName);
        Assert.NotNull(row);
        Assert.True(string.IsNullOrWhiteSpace(row.OrgId));
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Page phải lọc đúng theo ControllerId và khoảng OccurredAt.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task VwEventTriggerLogQuery_Page_FiltersByControllerAndTimeRange_Test()
    {
        // Arrange
        var controller = await SeedControllerAsync();
        var oldMoment = DateTime.Now.AddDays(-30);

        await _db.Insertable(new VwEventTriggerLog
        {
            Action = VwEventTriggerAction.Probe,
            ControllerId = controller.ID,
            StepOrder = 1,
            StepName = "Bước cũ",
            OccurredAt = oldMoment,
            CreateTime = oldMoment
        }).ExecuteCommandAsync();

        await _db.Insertable(new VwEventTriggerLog
        {
            Action = VwEventTriggerAction.Probe,
            ControllerId = controller.ID,
            StepOrder = 2,
            StepName = "Bước mới",
            OccurredAt = DateTime.Now,
            CreateTime = DateTime.Now
        }).ExecuteCommandAsync();

        // Act — chỉ lấy khoảng 7 ngày gần đây
        var result = await _bus.InvokeAsync<SqlSugarPagedList<VwPageEventTriggerLogOutput>>(
            new VwPageEventTriggerLogInput
            {
                Page = 1,
                PageSize = 50,
                ControllerId = controller.ID,
                OccurredAtFrom = DateTime.Now.AddDays(-7)
            });

        // Assert
        Assert.NotNull(result.Records);
        var row = Assert.Single(result.Records);
        Assert.Equal("Bước mới", row.StepName);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Page phải lọc đúng theo RuleId và EventTypeId cho các dòng nhật ký nghiệp vụ.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task VwEventTriggerLogQuery_Page_FiltersByRuleIdAndEventTypeId_Test()
    {
        // Arrange
        var ruleId = $"{TestPrefix}RULE_{Guid.NewGuid():N}";
        var eventTypeId = $"{TestPrefix}ET_{Guid.NewGuid():N}";

        await _db.Insertable(new VwEventTriggerLog
        {
            RuleId = ruleId,
            EventTypeId = eventTypeId,
            Action = VwEventTriggerAction.ActiveScene,
            OccurredAt = DateTime.Now,
            CreateTime = DateTime.Now
        }).ExecuteCommandAsync();

        // Act
        var result = await _bus.InvokeAsync<SqlSugarPagedList<VwPageEventTriggerLogOutput>>(
            new VwPageEventTriggerLogInput
            {
                Page = 1,
                PageSize = 10,
                RuleId = ruleId,
                EventTypeId = eventTypeId
            });

        // Assert
        Assert.NotNull(result.Records);
        var row = Assert.Single(result.Records);
        Assert.Equal(ruleId, row.RuleId);
        Assert.Equal(eventTypeId, row.EventTypeId);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: GetList phải tôn trọng trần Take, kể cả khi client gửi giá trị vô lý.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task VwEventTriggerLogQuery_GetList_RespectsTakeCap_Test()
    {
        // Arrange
        var controller = await SeedControllerAsync();
        var rows = Enumerable.Range(1, 5).Select(index => new VwEventTriggerLog
        {
            Action = VwEventTriggerAction.Probe,
            ControllerId = controller.ID,
            StepOrder = index,
            StepName = $"Bước {index}",
            OccurredAt = DateTime.Now.AddSeconds(index),
            CreateTime = DateTime.Now
        }).ToList();
        await _db.Insertable(rows).ExecuteCommandAsync();

        // Act
        var limited = await _bus.InvokeAsync<List<VwEventTriggerLogOutput>>(
            new VwEventTriggerLogInput { ControllerId = controller.ID, Take = 2 });

        // Take = 0 là giá trị vô lý ⇒ handler quay về mặc định, KHÔNG trả về 0 bản ghi.
        var defaulted = await _bus.InvokeAsync<List<VwEventTriggerLogOutput>>(
            new VwEventTriggerLogInput { ControllerId = controller.ID, Take = 0 });

        // Assert
        Assert.Equal(2, limited.Count);
        Assert.Equal(5, defaulted.Count);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 2. Ghi log tự động sau lệnh device-setup
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Author: Đạt
    /// Description: Ping sinh đúng một bước ISAPI nên phải để lại đúng một dòng log, mang Action Ping
    ///              và có đóng dấu thời điểm.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task VwDeviceSetup_Ping_WritesExactlyOneEventTriggerLogRow_Test()
    {
        host.MockServer.ResetDefaults();

        // Arrange
        var controller = await SeedControllerAsync();

        // Act
        var step = await _bus.InvokeAsync<VwSetupSceneStep>(new VwPingDeviceInput { ID = controller.ID });

        // Assert
        Assert.True(step.Success, step.Message);

        var rows = await ReadRowsAsync(controller.ID);
        var row = Assert.Single(rows);

        Assert.Equal(VwEventTriggerAction.Ping, row.Action);
        Assert.Equal(BaseEnums.SuccessEnums.Success, row.Success);
        Assert.Equal(controller.Name, row.ControllerName);
        Assert.NotNull(row.OccurredAt);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Probe trả về nhiều bước ⇒ phải ghi ĐÚNG số dòng bằng số bước. Đây là chốt của
    ///              quyết định "một dòng = một bước ISAPI": ghi gộp một dòng cho cả lượt gọi sẽ làm
    ///              bài test này đỏ.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task VwDeviceSetup_Probe_WritesOneRowPerIsapiStep_Test()
    {
        host.MockServer.ResetDefaults();

        // Arrange
        var controller = await SeedControllerAsync();

        // Act
        var output = await _bus.InvokeAsync<VwProbeDeviceOutput>(new VwProbeDeviceInput { ID = controller.ID });

        // Assert
        Assert.NotEmpty(output.Steps);

        var rows = await ReadRowsAsync(controller.ID);
        Assert.Equal(output.Steps.Count, rows.Count);
        Assert.All(rows, row => Assert.Equal(VwEventTriggerAction.Probe, row.Action));

        // Thứ tự bước phải giữ được để tra cứu lại đúng trình tự đã gửi.
        var expectedOrders = output.Steps.OrderBy(step => step.Order).Select(step => step.Order).ToList();
        var actualOrders = rows.Select(row => row.StepOrder ?? 0).ToList();
        Assert.Equal(expectedOrders, actualOrders);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: SetupScene ở chế độ chạy thử (DryRun mặc định) vẫn phải để lại dấu vết: các bước
    ///              Skipped được ghi với cờ Skipped = true và Success để RỖNG — bước bỏ qua không
    ///              phải một lượt gửi thành công, tính vào tỉ lệ thành công là sai số liệu.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task VwDeviceSetup_SetupSceneDryRun_WritesSkippedStepsWithoutSuccessFlag_Test()
    {
        host.MockServer.ResetDefaults();

        // Arrange
        var controller = await SeedControllerAsync();
        var sceneCode = $"{TestPrefix}SCENE_{Guid.NewGuid():N}";
        var scene = new VwScene
        {
            Code = sceneCode,
            Name = "Event Trigger Log DryRun Scene",
            ControllerId = controller.ID,
            OutputId = "1",
            Status = BaseEnums.StatusEnum.Enable,
            ActiveScene = BaseEnums.ActiveScene.DeActivate,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();

        // Act
        var output = await _bus.InvokeAsync<VwSetupSceneOutput>(new VwSetupSceneInput
        {
            ControllerId = controller.ID,
            SceneCode = sceneCode,
            DryRun = true
        });

        // Assert
        Assert.True(output.DryRun);
        Assert.NotEmpty(output.Steps);

        var rows = await ReadRowsAsync(controller.ID);
        Assert.Equal(output.Steps.Count, rows.Count);
        Assert.All(rows, row => Assert.Equal(scene.ID, row.SceneId));

        // Bước bị bỏ qua: cờ Skipped bật, Success rỗng.
        foreach (var skipped in rows.Where(row => row.Skipped == true))
            Assert.Null(skipped.Success);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Lệnh ném ngoại lệ (bộ điều khiển không tồn tại) KHÔNG có bước ISAPI nào, nhưng
    ///              vẫn phải để lại một dòng log mang thông báo lỗi — mất dòng này là mất đúng thứ
    ///              cần điều tra khi đấu nối tại hiện trường.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task VwDeviceSetup_Ping_UnknownController_StillWritesFailureRow_Test()
    {
        // Arrange
        var missingId = $"{TestPrefix}MISSING_{Guid.NewGuid():N}";

        // Act
        await Assert.ThrowsAnyAsync<Exception>(
            () => _bus.InvokeAsync<VwSetupSceneStep>(new VwPingDeviceInput { ID = missingId }));

        // Assert
        var rows = await ReadRowsAsync(missingId);
        var row = Assert.Single(rows);

        Assert.Equal(VwEventTriggerAction.Ping, row.Action);
        Assert.Equal(BaseEnums.SuccessEnums.Fail, row.Success);
        Assert.Equal(VwEventTriggerAction.FailedStepName, row.StepName);
        Assert.False(string.IsNullOrWhiteSpace(row.Message));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 3. ActivateScene — ghi cả dòng nghiệp vụ và dòng bước thiết bị vào cùng bảng
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Author: Đạt
    /// Description: Kích hoạt kịch bản ghi 2 dòng vào bảng VwEventTriggerLog: 1 dòng quyết định nghiệp vụ
    ///              (mang SceneId và Success) và 1 dòng bước ISAPI gửi xuống thiết bị (mang StepName,
    ///              Method PUT, Endpoint, DurationMs).
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task VwSceneWorkflow_ActiveScene_WritesBusinessAndDeviceLogsInSameTable_Test()
    {
        host.MockServer.ResetDefaults();

        // Arrange
        var controller = await SeedControllerAsync();
        var sceneCode = $"{TestPrefix}ACTIVE_{Guid.NewGuid():N}";
        var scene = new VwScene
        {
            Code = sceneCode,
            Name = "Event Trigger Log Active Scene",
            ControllerId = controller.ID,

            // OutputId = SID kịch bản trên thiết bị. Bỏ trống thì luồng KHÔNG gửi lệnh xuống thiết bị
            // và không sinh ra dòng bước thiết bị.
            OutputId = "1",
            Status = BaseEnums.StatusEnum.Enable,
            ActiveScene = BaseEnums.ActiveScene.DeActivate,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(scene).ExecuteCommandAsync();

        // Act
        var result = await _bus.InvokeAsync<VwActiveSceneOutput>(new VwActiveSceneInput { Code = sceneCode });

        // Assert — cả 2 dòng cùng nằm trong VwEventTriggerLog
        var rows = await _db.Queryable<VwEventTriggerLog>()
            .Where(u => u.IsDelete == null && u.SceneId == scene.ID && u.Action == VwEventTriggerAction.ActiveScene)
            .ToListAsync();

        Assert.Equal(2, rows.Count);

        // 1. Dòng nghiệp vụ (TriggerLogId do lệnh trả về)
        var businessRow = rows.FirstOrDefault(r => r.ID == result.TriggerLogId);
        Assert.NotNull(businessRow);
        Assert.Equal(scene.ID, businessRow.SceneId);
        Assert.Equal(BaseEnums.SuccessEnums.Success, businessRow.Success);

        // 2. Dòng bước thiết bị ISAPI
        var deviceStepRow = rows.FirstOrDefault(r => r.ID != result.TriggerLogId);
        Assert.NotNull(deviceStepRow);
        Assert.Equal(sceneCode, deviceStepRow.SceneCode);
        Assert.Equal("PUT", deviceStepRow.Method);
        Assert.Equal("Kích hoạt kịch bản", deviceStepRow.StepName);
        Assert.Equal(controller.ID, deviceStepRow.ControllerId);
        Assert.Equal(BaseEnums.SuccessEnums.Success, deviceStepRow.Success);
        Assert.NotNull(deviceStepRow.OccurredAt);
    }
}
