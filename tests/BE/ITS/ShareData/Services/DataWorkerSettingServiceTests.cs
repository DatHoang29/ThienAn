using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Modules.CfgSystem.Core.Entities;
using Shared.DTO.Enums;
using ShareDataWorker.Core.Interfaces;
using ShareDataWorker.Infrastructure.Services.DataWorkerSettings;
using SqlSugar;
using Xunit;

namespace Tests.ShareData.Services;

/// <summary>
/// Author: DatHQ
/// Description: Bộ kiểm thử đơn vị và tích hợp cho DataWorkerSettingService (đọc 17 tham số cấu hình vận hành Worker từ SysOpConfig).
///              Kiểm tra cơ chế fallback mặc định khi DB rỗng/lỗi/dòng tắt/ngoài khoảng an toàn.
/// Created date: 17/10/2026
/// </summary>
[Collection("api")]
public class DataWorkerSettingServiceTests(Host host)
{
    private readonly Host _host = host;

    [Fact]
    public async Task GetSettings_WhenNoConfigGroupExists_ReturnsExactFallbackDefaults_Test()
    {
        // Arrange: Cấu hình group code không tồn tại để kiểm tra fallback hoàn toàn
        var isolatedGroup = $"sd_cfg_missing_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShareDataWorkerSetting:GroupCode"] = isolatedGroup
            })
            .Build();

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var service = new DataWorkerSettingService(scopeFactory, NullLogger<DataWorkerSettingService>.Instance, config);

        // Act
        var settings = await service.GetSettings();

        // Assert: 17 tham số đều khớp 100% với giá trị dự phòng chuẩn
        Assert.NotNull(settings);
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
    }

    [Fact]
    public async Task GetSettings_WhenValidConfigInDatabase_LoadsConfiguredValues_Test()
    {
        // Arrange: Tạo group code cô lập và chèn các tham số hợp lệ
        var isolatedGroup = $"sd_cfg_valid_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShareDataWorkerSetting:GroupCode"] = isolatedGroup
            })
            .Build();

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var testRows = new List<SysOpConfig>
        {
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "outbound_check_interval_sec", Value = "12", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "inbound_check_interval_sec", Value = "15", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "outbound_page_size", Value = "250", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "tracking_poll_interval_sec", Value = "3", Status = BaseEnums.StatusEnum.Enable }
        };
        await db.Insertable(testRows).ExecuteCommandAsync();

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var service = new DataWorkerSettingService(scopeFactory, NullLogger<DataWorkerSettingService>.Instance, config);

        // Act
        var settings = await service.GetSettings();

        // Assert: Các trường có trong DB nhận giá trị cấu hình, các trường khác giữ nguyên dự phòng
        Assert.Equal(12, settings.OutboundCheckIntervalSec);
        Assert.Equal(15, settings.InboundCheckIntervalSec);
        Assert.Equal(250, settings.OutboundPageSize);
        Assert.Equal(3, settings.TrackingPollIntervalSec);
        Assert.Equal(20, settings.OutboundMaxPagesPerRun); // Fallback
    }

    [Fact]
    public async Task GetSettings_WhenValuesInvalidOrOutOfRange_FallsBackToDefaultsWithoutThrowing_Test()
    {
        // Arrange: Chèn giá trị không phải số, số âm, hoặc ngoài khoảng cho phép
        var isolatedGroup = $"sd_cfg_invalid_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShareDataWorkerSetting:GroupCode"] = isolatedGroup
            })
            .Build();

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var testRows = new List<SysOpConfig>
        {
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "outbound_check_interval_sec", Value = "NOT_A_NUMBER", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "inbound_check_interval_sec", Value = "999999", Status = BaseEnums.StatusEnum.Enable }, // Out of max 300
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "tracking_poll_interval_sec", Value = "-5", Status = BaseEnums.StatusEnum.Enable }, // Out of min 1
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "outbound_page_size", Value = "   ", Status = BaseEnums.StatusEnum.Enable } // Whitespace
        };
        await db.Insertable(testRows).ExecuteCommandAsync();

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var service = new DataWorkerSettingService(scopeFactory, NullLogger<DataWorkerSettingService>.Instance, config);

        // Act
        var settings = await service.GetSettings();

        // Assert: Tự động dùng fallback an toàn, không ném exception
        Assert.Equal(5, settings.OutboundCheckIntervalSec);
        Assert.Equal(5, settings.InboundCheckIntervalSec);
        Assert.Equal(1, settings.TrackingPollIntervalSec);
        Assert.Equal(100, settings.OutboundPageSize);
    }

    [Fact]
    public async Task GetSettings_WhenRowsDisabledOrSoftDeleted_IgnoresThem_Test()
    {
        // Arrange: Chèn dòng bị tắt hoặc xóa mềm
        var isolatedGroup = $"sd_cfg_disabled_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShareDataWorkerSetting:GroupCode"] = isolatedGroup
            })
            .Build();

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var testRows = new List<SysOpConfig>
        {
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "outbound_check_interval_sec", Value = "80", Status = BaseEnums.StatusEnum.Disable }, // Disabled
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "inbound_check_interval_sec", Value = "90", Status = BaseEnums.StatusEnum.Enable, IsDelete = DateTime.Now } // Soft deleted
        };
        await db.Insertable(testRows).ExecuteCommandAsync();

        var scopeFactory = _host.Services.GetRequiredService<IServiceScopeFactory>();
        var service = new DataWorkerSettingService(scopeFactory, NullLogger<DataWorkerSettingService>.Instance, config);

        // Act
        var settings = await service.GetSettings();

        // Assert: Bị bỏ qua, giữ giá trị dự phòng
        Assert.Equal(5, settings.OutboundCheckIntervalSec);
        Assert.Equal(5, settings.InboundCheckIntervalSec);
    }
}
