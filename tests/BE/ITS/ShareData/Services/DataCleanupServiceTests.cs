using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Entities;
using Modules.CfgSystem.Core.Entities;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.DataCleanup;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;

namespace Tests.ShareData.Services;

/// <summary>
/// Description: Bộ kiểm thử đơn vị và tích hợp cho DataCleanupService (dọn dẹp nhật ký Change Tracking ESH-16xx).
/// Author: DatHQ
/// Created date: 18/10/2026
/// </summary>
[Collection("api")]
public class DataCleanupServiceTests(Host host)
{
    private readonly Host _host = host;

    /// <summary>
    /// Description: Kiểm thử dọn nhật ký hạ tầng Change Tracking: chỉ xoá các dòng bắt đầu bằng mã ESH-16xx quá hạn,
    ///              giữ nguyên các dòng nghiệp vụ (Remark null hoặc khác ESH-16xx).
    /// </summary>
    [Fact]
    public async Task CleanTrackingLogs_OnlyDeletesEsh16Rows_KeepsBusinessTransferRows_Test()
    {
        // Arrange
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var cleanupService = scope.ServiceProvider.GetRequiredService<DataCleanupService>();

        var oldTime = (await db.Ado.GetDateTimeAsync("SELECT GETDATE()")).AddDays(-30);
        var trackingId = Guid.NewGuid().ToString("N");
        var businessId = Guid.NewGuid().ToString("N");

        await db.Insertable(new ShareDataActivityLog
        {
            ID = trackingId,
            LogType = BaseEnums.LogTypeEnum.Transfer,
            Remark = ShareDataAlertCode.Tracking.SelfHeal,
            Description = "Dòng hạ tầng quá hạn dùng cho test dọn nhật ký.",
            CreateTime = oldTime,
            UpdateTime = oldTime
        }).ExecuteCommandAsync();
        await db.Ado.ExecuteCommandAsync("UPDATE ShareDataActivityLog SET CreateTime = @oldTime WHERE ID = @id", new { oldTime, id = trackingId });

        await db.Insertable(new ShareDataActivityLog
        {
            ID = businessId,
            LogType = BaseEnums.LogTypeEnum.Transfer,
            Remark = null,
            Description = "Dòng nghiệp vụ quá hạn — PHẢI còn nguyên sau khi dọn.",
            CreateTime = oldTime,
            UpdateTime = oldTime
        }).ExecuteCommandAsync();
        await db.Ado.ExecuteCommandAsync("UPDATE ShareDataActivityLog SET CreateTime = @oldTime WHERE ID = @id", new { oldTime, id = businessId });

        // Act
        await cleanupService.CleanTrackingLogs(CancellationToken.None);

        // Assert
        Assert.Null(await db.Queryable<ShareDataActivityLog>().Where(x => x.ID == trackingId).FirstAsync());
        Assert.NotNull(await db.Queryable<ShareDataActivityLog>().Where(x => x.ID == businessId).FirstAsync());
    }

    /// <summary>
    /// Description: C1 - Khi cấu hình TrackingLogRetentionDays = 3, chỉ xóa các dòng hạ tầng ESH-16xx cũ hơn 3 ngày (ví dụ 5 ngày trước),
    ///              giữ nguyên dòng hạ tầng mới hơn (1 ngày trước) và dòng nghiệp vụ cũ hơn (5 ngày trước).
    /// Created date: 20/10/2026
    /// </summary>
    [Fact]
    public async Task CleanTrackingLogs_WhenRetentionDaysConfigured_DeletesOnlyRowsOlderThanConfiguredDays_Test()
    {
        // Arrange
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var isolatedGroup = $"sd_cfg_c1_{unique}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup
            })
            .Build();

        var cfgRow = new SysOpConfig
        {
            ID = Guid.NewGuid().ToString("N"),
            GroupCode = isolatedGroup,
            Code = ShareDataConst.ConfigKeyTrackingLogRetentionDays,
            Value = "3",
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(cfgRow).ExecuteCommandAsync();

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var workerConfig = new DataWorkerConfigService(scopeFactory, NullLogger<DataWorkerConfigService>.Instance, config);
        var cleanupService = new DataCleanupService(scopeFactory, NullLogger<DataCleanupService>.Instance, workerConfig);

        var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
        var time5DaysAgo = now.AddDays(-5);
        var time1DayAgo = now.AddDays(-1);

        var oldTrackingId = Guid.NewGuid().ToString("N");
        var recentTrackingId = Guid.NewGuid().ToString("N");
        var oldBusinessId = Guid.NewGuid().ToString("N");

        await db.Insertable(new ShareDataActivityLog
        {
            ID = oldTrackingId,
            LogType = BaseEnums.LogTypeEnum.Transfer,
            Remark = ShareDataAlertCode.Tracking.SelfHeal,
            Description = "Dòng hạ tầng 5 ngày trước",
            CreateTime = time5DaysAgo,
            UpdateTime = time5DaysAgo
        }).ExecuteCommandAsync();
        await db.Ado.ExecuteCommandAsync("UPDATE ShareDataActivityLog SET CreateTime = @t WHERE ID = @id", new { t = time5DaysAgo, id = oldTrackingId });

        await db.Insertable(new ShareDataActivityLog
        {
            ID = recentTrackingId,
            LogType = BaseEnums.LogTypeEnum.Transfer,
            Remark = ShareDataAlertCode.Tracking.SelfHeal,
            Description = "Dòng hạ tầng 1 ngày trước",
            CreateTime = time1DayAgo,
            UpdateTime = time1DayAgo
        }).ExecuteCommandAsync();
        await db.Ado.ExecuteCommandAsync("UPDATE ShareDataActivityLog SET CreateTime = @t WHERE ID = @id", new { t = time1DayAgo, id = recentTrackingId });

        await db.Insertable(new ShareDataActivityLog
        {
            ID = oldBusinessId,
            LogType = BaseEnums.LogTypeEnum.Transfer,
            Remark = null,
            Description = "Dòng nghiệp vụ 5 ngày trước",
            CreateTime = time5DaysAgo,
            UpdateTime = time5DaysAgo
        }).ExecuteCommandAsync();
        await db.Ado.ExecuteCommandAsync("UPDATE ShareDataActivityLog SET CreateTime = @t WHERE ID = @id", new { t = time5DaysAgo, id = oldBusinessId });

        // Act
        await cleanupService.CleanTrackingLogs(CancellationToken.None);

        // Assert
        Assert.Null(await db.Queryable<ShareDataActivityLog>().Where(x => x.ID == oldTrackingId).FirstAsync());
        Assert.NotNull(await db.Queryable<ShareDataActivityLog>().Where(x => x.ID == recentTrackingId).FirstAsync());
        Assert.NotNull(await db.Queryable<ShareDataActivityLog>().Where(x => x.ID == oldBusinessId).FirstAsync());
    }

    /// <summary>
    /// Description: C2 - Khi số dòng hạ tầng quá hạn vượt quá kích thước một lô (TrackingLogPurgeBatch = 100),
    ///              CleanTrackingLogs sẽ xóa toàn bộ số dòng qua nhiều lô liên tiếp (2 * batch + 1 = 201 dòng).
    /// Created date: 20/10/2026
    /// </summary>
    [Fact]
    public async Task CleanTrackingLogs_WhenStaleRowsExceedPurgeBatch_DeletesAllAcrossBatches_Test()
    {
        // Arrange
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var isolatedGroup = $"sd_cfg_c2_{unique}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup
            })
            .Build();

        const int batchSize = ShareDataConst.MinTrackingLogPurgeBatch; // 100
        const int totalRows = 2 * batchSize + 1; // 201

        var cfgRow = new SysOpConfig
        {
            ID = Guid.NewGuid().ToString("N"),
            GroupCode = isolatedGroup,
            Code = ShareDataConst.ConfigKeyTrackingLogPurgeBatch,
            Value = batchSize.ToString(),
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(cfgRow).ExecuteCommandAsync();

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var workerConfig = new DataWorkerConfigService(scopeFactory, NullLogger<DataWorkerConfigService>.Instance, config);
        var cleanupService = new DataCleanupService(scopeFactory, NullLogger<DataCleanupService>.Instance, workerConfig);

        var oldTime = (await db.Ado.GetDateTimeAsync("SELECT GETDATE()")).AddDays(-30);
        var testLogs = new List<ShareDataActivityLog>(totalRows);
        for (var i = 0; i < totalRows; i++)
        {
            testLogs.Add(new ShareDataActivityLog
            {
                ID = $"LOG_C2_{unique}_{i:D3}",
                LogType = BaseEnums.LogTypeEnum.Transfer,
                Remark = ShareDataAlertCode.Tracking.SelfHeal,
                Description = $"Dòng hạ tầng lô {i}",
                CreateTime = oldTime,
                UpdateTime = oldTime
            });
        }
        await db.Insertable(testLogs).ExecuteCommandAsync();

        var ids = testLogs.Select(x => x.ID).ToList();
        await db.Ado.ExecuteCommandAsync(
            "UPDATE ShareDataActivityLog SET CreateTime = @oldTime WHERE ID IN (SELECT value FROM OPENJSON(@ids));",
            new { oldTime, ids = System.Text.Json.JsonSerializer.Serialize(ids) });

        // Act
        await cleanupService.CleanTrackingLogs(CancellationToken.None);

        // Assert
        var remainingCount = await db.Queryable<ShareDataActivityLog>()
            .Where(x => ids.Contains(x.ID))
            .CountAsync();
        Assert.Equal(0, remainingCount);
    }
}
