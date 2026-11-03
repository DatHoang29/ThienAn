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
/// Modified date: 24/10/2026
/// </summary>
[Collection("api")]
public class DataCleanupServiceTests(Host host)
{
    private readonly Host _host = host;

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

    /// <summary>
    /// Author: DatHQ
    /// Description: Bật cleanup_enabled thì CleanArchive xóa thư mục ngày, nhật ký nghiệp vụ, cảnh báo và gói nhận đã xong cũ hơn số ngày giữ lại;
    ///              giữ phần còn trong hạn, gói nhận chưa xong và nhật ký hạ tầng ESH-16xx. Tắt (hoặc không khai) thì không xóa gì.
    /// Created date: 02/11/2026
    /// </summary>
    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData(null, false)]
    public async Task CleanArchive_WhenEnabledOrDisabled_DeletesOnlyExpiredFinishedData_Test(string? enabled, bool expectDeleted)
    {
        // Arrange
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var inboundDb = db.AsTenant().GetConnectionScope(ShareDataWorker.Extensions.ShareDataWorkerExtensions.InboundConnectionKey);
        var unique = Guid.NewGuid().ToString("N")[..8];
        var isolatedGroup = $"sd_cfg_arc_{unique}";
        var basePath = Path.Combine(Path.GetTempPath(), $"sd_archive_{unique}");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup,
                ["NasStorage:BasePath"] = basePath
            })
            .Build();

        var cfgRows = new List<SysOpConfig>
        {
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = ShareDataConst.ConfigKeyCleanupRetentionDays, Value = "30", Status = BaseEnums.StatusEnum.Enable }
        };
        if (enabled != null)
        {
            cfgRows.Add(new SysOpConfig { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = ShareDataConst.ConfigKeyCleanupEnabled, Value = enabled, Status = BaseEnums.StatusEnum.Enable });
        }
        await db.Insertable(cfgRows).ExecuteCommandAsync();

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var workerConfig = new DataWorkerConfigService(scopeFactory, NullLogger<DataWorkerConfigService>.Instance, config);
        var cleanupService = new DataCleanupService(scopeFactory, NullLogger<DataCleanupService>.Instance, workerConfig, config);

        var now = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
        var oldTime = now.AddDays(-40);
        var recentTime = now.AddDays(-5);

        string DayDir(DateTime t) => Path.Combine(basePath, "Out", $"P_{unique}", t.ToString("yyyy"), t.ToString("MM"), t.ToString("dd"));
        var oldDir = DayDir(oldTime);
        var recentDir = DayDir(recentTime);
        foreach (var dir in new[] { oldDir, recentDir })
        {
            Directory.CreateDirectory(Path.Combine(dir, "101"));
            await File.WriteAllTextAsync(Path.Combine(dir, "101", "a.json"), "{}");
        }

        var oldLogId = Guid.NewGuid().ToString("N");
        var recentLogId = Guid.NewGuid().ToString("N");
        var oldTrackingId = Guid.NewGuid().ToString("N");
        await db.Insertable(new List<ShareDataActivityLog>
        {
            new() { ID = oldLogId, LogType = BaseEnums.LogTypeEnum.Transfer, Description = "Nghiệp vụ cũ" },
            new() { ID = recentLogId, LogType = BaseEnums.LogTypeEnum.Transfer, Description = "Nghiệp vụ mới" },
            new() { ID = oldTrackingId, LogType = BaseEnums.LogTypeEnum.Transfer, Remark = ShareDataAlertCode.Tracking.SelfHeal, Description = "Hạ tầng cũ" }
        }).ExecuteCommandAsync();
        await db.Ado.ExecuteCommandAsync("UPDATE ShareDataActivityLog SET CreateTime = @oldTime WHERE ID IN (@oldLogId, @oldTrackingId);", new { oldTime, oldLogId, oldTrackingId });
        await db.Ado.ExecuteCommandAsync("UPDATE ShareDataActivityLog SET CreateTime = @recentTime WHERE ID = @recentLogId;", new { recentTime, recentLogId });

        var oldAlertId = Guid.NewGuid().ToString("N");
        var recentAlertId = Guid.NewGuid().ToString("N");
        await db.Insertable(new List<ShareDataAlertLog>
        {
            new() { ID = oldAlertId, AlertCode = "ESH-1401", Message = "Cảnh báo cũ" },
            new() { ID = recentAlertId, AlertCode = "ESH-1401", Message = "Cảnh báo mới" }
        }).ExecuteCommandAsync();
        await db.Ado.ExecuteCommandAsync("UPDATE ShareDataAlertLog SET CreateTime = @oldTime WHERE ID = @oldAlertId;", new { oldTime, oldAlertId });
        await db.Ado.ExecuteCommandAsync("UPDATE ShareDataAlertLog SET CreateTime = @recentTime WHERE ID = @recentAlertId;", new { recentTime, recentAlertId });

        var oldDoneId = Guid.NewGuid().ToString("N");
        var oldPendingId = Guid.NewGuid().ToString("N");
        var recentDoneId = Guid.NewGuid().ToString("N");
        await inboundDb.Insertable(new List<ShareDataInboundPacket>
        {
            new() { ID = oldDoneId, PartnerCode = $"P_{unique}", PacketCode = "101", ProcessState = BaseEnums.InboundProcessState.Done },
            new() { ID = oldPendingId, PartnerCode = $"P_{unique}", PacketCode = "101", ProcessState = BaseEnums.InboundProcessState.Pending },
            new() { ID = recentDoneId, PartnerCode = $"P_{unique}", PacketCode = "101", ProcessState = BaseEnums.InboundProcessState.Done }
        }).ExecuteCommandAsync();
        await inboundDb.Ado.ExecuteCommandAsync("UPDATE ShareDataInboundPacket SET CreateTime = @oldTime WHERE ID IN (@oldDoneId, @oldPendingId);", new { oldTime, oldDoneId, oldPendingId });
        await inboundDb.Ado.ExecuteCommandAsync("UPDATE ShareDataInboundPacket SET CreateTime = @recentTime WHERE ID = @recentDoneId;", new { recentTime, recentDoneId });

        try
        {
            // Act
            await cleanupService.CleanArchive(CancellationToken.None);

            // Assert: phần quá hạn đã xong bị xóa khi bật, còn nguyên khi tắt
            Assert.Equal(!expectDeleted, Directory.Exists(oldDir));
            Assert.Equal(!expectDeleted, await db.Queryable<ShareDataActivityLog>().AnyAsync(x => x.ID == oldLogId));
            Assert.Equal(!expectDeleted, await db.Queryable<ShareDataAlertLog>().AnyAsync(x => x.ID == oldAlertId));
            Assert.Equal(!expectDeleted, await inboundDb.Queryable<ShareDataInboundPacket>().AnyAsync(x => x.ID == oldDoneId));

            // Assert: phần còn trong hạn, gói chưa xong và nhật ký hạ tầng luôn được giữ
            Assert.True(File.Exists(Path.Combine(recentDir, "101", "a.json")));
            Assert.True(await db.Queryable<ShareDataActivityLog>().AnyAsync(x => x.ID == recentLogId));
            Assert.True(await db.Queryable<ShareDataActivityLog>().AnyAsync(x => x.ID == oldTrackingId));
            Assert.True(await db.Queryable<ShareDataAlertLog>().AnyAsync(x => x.ID == recentAlertId));
            Assert.True(await inboundDb.Queryable<ShareDataInboundPacket>().AnyAsync(x => x.ID == oldPendingId));
            Assert.True(await inboundDb.Queryable<ShareDataInboundPacket>().AnyAsync(x => x.ID == recentDoneId));
        }
        finally
        {
            if (Directory.Exists(basePath)) Directory.Delete(basePath, recursive: true);
            await db.Deleteable<ShareDataActivityLog>().Where(x => x.ID == oldLogId || x.ID == recentLogId || x.ID == oldTrackingId).ExecuteCommandAsync();
            await db.Deleteable<ShareDataAlertLog>().Where(x => x.ID == oldAlertId || x.ID == recentAlertId).ExecuteCommandAsync();
            await inboundDb.Deleteable<ShareDataInboundPacket>().Where(x => x.PartnerCode == $"P_{unique}").ExecuteCommandAsync();
            await db.Deleteable<SysOpConfig>().Where(x => x.GroupCode == isolatedGroup).ExecuteCommandAsync();
        }
    }
}
