using Microsoft.Extensions.Logging.Abstractions;
using Modules.CfgSystem.Core.Entities;
using ShareDataWorker.Core.Configuration;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;

namespace Tests.ShareData.Configuration;

/// <summary>
/// Author: DatHQ
/// Description: Bộ kiểm thử tích hợp cho DataWorkerConfigService (quản lý tham số cấu hình vận hành Worker từ SysOpConfig).
///              Kiểm tra cơ chế nạp cấu hình và fallback mặc định khi DB rỗng/lỗi/dòng tắt/ngoài khoảng an toàn.
/// Created date: 18/10/2026
/// Modified date: 24/10/2026
/// </summary>
[Collection("api")]
public class DataWorkerConfigServiceTests(Host host)
{
    private readonly Host _host = host;

    [Fact]
    public async Task Get_WhenNoConfigGroupExists_ReturnsExactFallbackDefaults_Test()
    {
        // Arrange: Cấu hình group code không tồn tại để kiểm tra fallback hoàn toàn
        var isolatedGroup = $"sd_cfg_missing_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup
            })
            .Build();

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var service = new DataWorkerConfigService(scopeFactory, NullLogger<DataWorkerConfigService>.Instance, config);

        // Act
        var settings = await service.Get();

        // Assert: 19 tham số đều khớp 100% với giá trị dự phòng chuẩn
        Assert.NotNull(settings);
        Assert.Equivalent(new DataWorkerConfig(), settings);
        Assert.Equal(5, settings.OutboundCheckIntervalSec);
        Assert.Equal(5, settings.InboundCheckIntervalSec);
        Assert.Equal(1, settings.TrackingPollIntervalSec);
        Assert.Equal(20, settings.OutboundMaxPagesPerRun);
        Assert.Equal(50, settings.OutboundLockBudgetPercent);
        Assert.Equal(300, settings.OutboundLockMinSec);
        Assert.Equal(3, settings.OutboundLockIntervalMultiplier);
        Assert.Equal(100, settings.OutboundPageSize);
        Assert.Equal(30, settings.OutboundDefaultIntervalSec);
        Assert.Equal(50, settings.InboundMaxPacketsPerRun);
        Assert.Equal(10, settings.InboundStuckTimeoutMin);
        Assert.Equal(5, settings.InboundWarnIntervalMin);
        Assert.Equal(120, settings.InboundWriteTimeoutSec);
        Assert.Equal(5, settings.TrackingTableRetryMin);
        Assert.Equal(7, settings.TrackingLogRetentionDays);
        Assert.Equal(1000, settings.TrackingLogPurgeBatch);
        Assert.Equal(2, settings.TrackingChangeRetentionDays);
        Assert.Equal(1440, settings.CleanupIntervalMin);
        Assert.Equal(30, settings.ReloadIntervalSec);
    }

    [Fact]
    public async Task Get_WhenValidConfigInDatabase_LoadsConfiguredValues_Test()
    {
        // Arrange: Tạo group code cô lập và chèn các tham số hợp lệ
        var isolatedGroup = $"sd_cfg_valid_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup
            })
            .Build();

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var testRows = new List<SysOpConfig>
        {
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "outbound_check_interval_sec", Value = "12", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "inbound_check_interval_sec", Value = "15", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "outbound_page_size", Value = "250", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "tracking_poll_interval_sec", Value = "3", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "cleanup_interval_min", Value = "720", Status = BaseEnums.StatusEnum.Enable }
        };
        await db.Insertable(testRows).ExecuteCommandAsync();

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var service = new DataWorkerConfigService(scopeFactory, NullLogger<DataWorkerConfigService>.Instance, config);

        // Act
        var settings = await service.Get();

        // Assert: Các trường có trong DB nhận giá trị cấu hình, các trường khác giữ nguyên dự phòng
        Assert.Equal(12, settings.OutboundCheckIntervalSec);
        Assert.Equal(15, settings.InboundCheckIntervalSec);
        Assert.Equal(250, settings.OutboundPageSize);
        Assert.Equal(3, settings.TrackingPollIntervalSec);
        Assert.Equal(720, settings.CleanupIntervalMin);
        Assert.Equal(20, settings.OutboundMaxPagesPerRun); // Fallback
    }

    [Fact]
    public async Task Get_WhenValuesInvalidOrOutOfRange_FallsBackToDefaultsWithoutThrowing_Test()
    {
        // Arrange: Chèn giá trị không phải số, số âm, hoặc ngoài khoảng cho phép
        var isolatedGroup = $"sd_cfg_invalid_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup
            })
            .Build();

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var testRows = new List<SysOpConfig>
        {
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "outbound_check_interval_sec", Value = "NOT_A_NUMBER", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "inbound_check_interval_sec", Value = "999999", Status = BaseEnums.StatusEnum.Enable }, // Out of max 300
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "tracking_poll_interval_sec", Value = "-5", Status = BaseEnums.StatusEnum.Enable }, // Out of min 1
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "outbound_page_size", Value = "   ", Status = BaseEnums.StatusEnum.Enable }, // Whitespace
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "cleanup_interval_min", Value = "0", Status = BaseEnums.StatusEnum.Enable } // Out of min 1
        };
        await db.Insertable(testRows).ExecuteCommandAsync();

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var service = new DataWorkerConfigService(scopeFactory, NullLogger<DataWorkerConfigService>.Instance, config);

        // Act
        var settings = await service.Get();

        // Assert: Tự động dùng fallback an toàn, không ném exception
        Assert.Equal(5, settings.OutboundCheckIntervalSec);
        Assert.Equal(5, settings.InboundCheckIntervalSec);
        Assert.Equal(1, settings.TrackingPollIntervalSec);
        Assert.Equal(100, settings.OutboundPageSize);
        Assert.Equal(1440, settings.CleanupIntervalMin);
    }

    [Fact]
    public async Task Get_WhenRowsDisabledOrSoftDeleted_IgnoresThem_Test()
    {
        // Arrange: Chèn dòng bị tắt hoặc xóa mềm
        var isolatedGroup = $"sd_cfg_disabled_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup
            })
            .Build();

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var testRows = new List<SysOpConfig>
        {
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "outbound_check_interval_sec", Value = "80", Status = BaseEnums.StatusEnum.Disable }, // Disabled
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "inbound_check_interval_sec", Value = "90", Status = BaseEnums.StatusEnum.Enable, IsDelete = DateTime.Now }, // Soft deleted
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "cleanup_interval_min", Value = "500", Status = BaseEnums.StatusEnum.Disable } // Disabled
        };
        await db.Insertable(testRows).ExecuteCommandAsync();

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var service = new DataWorkerConfigService(scopeFactory, NullLogger<DataWorkerConfigService>.Instance, config);

        // Act
        var settings = await service.Get();

        // Assert: Bị bỏ qua, giữ giá trị dự phòng
        Assert.Equal(5, settings.OutboundCheckIntervalSec);
        Assert.Equal(5, settings.InboundCheckIntervalSec);
        Assert.Equal(1440, settings.CleanupIntervalMin);
    }

    [Fact]
    public async Task Get_WhenUnknownOrDuplicateCodesInDatabase_HandlesCorrectly_Test()
    {
        // Arrange: Chèn Code lạ và Code trùng (dòng sau thắng)
        var isolatedGroup = $"sd_cfg_dup_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup
            })
            .Build();

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var baseId = DateTime.UtcNow.Ticks;
        var testRows = new List<SysOpConfig>
        {
            // Code lạ ngoài spec -> bị bỏ qua
            new() { ID = $"{baseId}_0", GroupCode = isolatedGroup, Code = "unknown_worker_config_code", Value = "999", Status = BaseEnums.StatusEnum.Enable },
            // Code trùng: dòng 1 đặt 10, dòng 2 đặt 25 -> dòng sau thắng (25)
            new() { ID = $"{baseId}_1", GroupCode = isolatedGroup, Code = "outbound_check_interval_sec", Value = "10", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = $"{baseId}_2", GroupCode = isolatedGroup, Code = "outbound_check_interval_sec", Value = "25", Status = BaseEnums.StatusEnum.Enable }
        };
        await db.Insertable(testRows).ExecuteCommandAsync();

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var service = new DataWorkerConfigService(scopeFactory, NullLogger<DataWorkerConfigService>.Instance, config);

        // Act
        var settings = await service.Get();

        // Assert
        Assert.Equal(25, settings.OutboundCheckIntervalSec);
        Assert.Equal(new DataWorkerConfig().InboundCheckIntervalSec, settings.InboundCheckIntervalSec);
    }

    /// <summary>
    /// Description: Kiểm chứng cấu hình reload_interval_sec nạp đúng giá trị hợp lệ
    ///              hoặc fallback về mặc định khi giá trị ngoài khoảng [5..3600] hoặc sai định dạng.
    /// Created date: 18/10/2026
    /// Modified date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("45", 45)]
    [InlineData("3", ShareDataConst.DefaultReloadIntervalSec)]
    [InlineData("5000", ShareDataConst.DefaultReloadIntervalSec)]
    [InlineData("abc", ShareDataConst.DefaultReloadIntervalSec)]
    public async Task Get_WhenReloadIntervalSecConfigured_ReturnsExpectedValue_Test(string configuredValue, int expectedValue)
    {
        // Arrange
        var isolatedGroup = $"sd_cfg_reload_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup
            })
            .Build();

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var testRow = new SysOpConfig
        {
            ID = Guid.NewGuid().ToString("N"),
            GroupCode = isolatedGroup,
            Code = ShareDataConst.ConfigKeyReloadIntervalSec,
            Value = configuredValue,
            Status = BaseEnums.StatusEnum.Enable
        };
        await db.Insertable(testRow).ExecuteCommandAsync();

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var service = new DataWorkerConfigService(scopeFactory, NullLogger<DataWorkerConfigService>.Instance, config);

        // Act
        var settings = await service.Get();

        // Assert
        Assert.Equal(expectedValue, settings.ReloadIntervalSec);
    }
}
