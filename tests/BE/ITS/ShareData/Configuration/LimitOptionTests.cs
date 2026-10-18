using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Module.ShareData.Infrastructure.Configuration;
using Modules.CfgSystem.Core.Entities;
using Shared.DTO.Enums;
using SqlSugar;
using Xunit;

namespace Tests.ShareData.Configuration;

/// <summary>
/// Author: DatHQ
/// Description: Bộ kiểm thử đơn vị và tích hợp cho LimitOption (WebAPI đọc các giới hạn vận hành từ SysOpConfig).
///              Kiểm tra cơ chế fallback mặc định và khoảng an toàn tuyệt đối.
/// Created date: 18/10/2026
/// </summary>
[Collection("api")]
public class LimitOptionTests(Host host)
{
    private readonly Host _host = host;

    [Fact]
    public void LimitOption_WhenNoDatabaseConfig_ReturnsExactDefaults_Test()
    {
        // Arrange
        var isolatedGroup = $"sd_webapi_missing_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShareDataConfig:GroupCode"] = isolatedGroup
            })
            .Build();

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var service = new LimitOption(
            db: db,
            logger: NullLogger<LimitOption>.Instance,
            configuration: config);

        // Act
        var values = service.Get();

        // Assert
        Assert.Equal(LimitOption.Defaults, values);
        Assert.Equal(LimitOption.Defaults.PacketLookbackDays.Min, values.PacketLookbackDays.Min);
        Assert.Equal(LimitOption.Defaults.PacketLookbackDays.Max, values.PacketLookbackDays.Max);
        Assert.Equal(LimitOption.Defaults.SqlSampleTop, values.SqlSampleTop);
        Assert.Equal(LimitOption.Defaults.MappingSampleTop, values.MappingSampleTop);
        Assert.Equal(LimitOption.Defaults.LogListDefaultTop, values.LogListDefaultTop);
        Assert.Equal(LimitOption.Defaults.SubscriptionInterval, values.SubscriptionInterval);
        Assert.Equal(LimitOption.Defaults.SubscriptionPriority, values.SubscriptionPriority);
        Assert.Equal(LimitOption.Defaults.SubscriptionDebounceSec, values.SubscriptionDebounceSec);
    }

    [Fact]
    public async Task LimitOption_WhenValidConfigInDatabase_LoadsCustomLimits_Test()
    {
        // Arrange
        var isolatedGroup = $"sd_webapi_valid_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShareDataConfig:GroupCode"] = isolatedGroup
            })
            .Build();

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var testRows = new List<SysOpConfig>
        {
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "packet_lookback_days_min", Value = "3", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "packet_lookback_days_max", Value = "90", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "log_list_default_top", Value = "500", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_interval_sec_min", Value = "10", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_interval_sec_max", Value = "3600", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_priority_min", Value = "1", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_priority_max", Value = "8", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_debounce_sec_min", Value = "5", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_debounce_sec_max", Value = "1800", Status = BaseEnums.StatusEnum.Enable }
        };
        await db.Insertable(testRows).ExecuteCommandAsync();

        var service = new LimitOption(
            db: db,
            logger: NullLogger<LimitOption>.Instance,
            configuration: config);

        // Act
        var values = service.Get();

        // Assert
        Assert.Equal(3, values.PacketLookbackDays.Min);
        Assert.Equal(90, values.PacketLookbackDays.Max);
        Assert.Equal(500, values.LogListDefaultTop);
        Assert.Equal(10, values.SubscriptionInterval.Min);
        Assert.Equal(3600, values.SubscriptionInterval.Max);
        Assert.Equal(1, values.SubscriptionPriority.Min);
        Assert.Equal(8, values.SubscriptionPriority.Max);
        Assert.Equal(5, values.SubscriptionDebounceSec.Min);
        Assert.Equal(1800, values.SubscriptionDebounceSec.Max);
    }

    [Fact]
    public async Task LimitOption_WhenTripletMisaligned_FallsBackToDefaultTriplet_Test()
    {
        // Arrange: Cấu hình default > max hoặc max < min -> lệch bộ
        var isolatedGroup = $"sd_webapi_misaligned_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShareDataConfig:GroupCode"] = isolatedGroup
            })
            .Build();

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var testRows = new List<SysOpConfig>
        {
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "sql_sample_top_min", Value = "20", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "sql_sample_top_max", Value = "10", Status = BaseEnums.StatusEnum.Enable }, // Max < Min!
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "sql_sample_top_default", Value = "15", Status = BaseEnums.StatusEnum.Enable }
        };
        await db.Insertable(testRows).ExecuteCommandAsync();

        var service = new LimitOption(
            db: db,
            logger: NullLogger<LimitOption>.Instance,
            configuration: config);

        // Act
        var values = service.Get();

        // Assert: Phải quay về nguyên bộ dự phòng (1, 50, 10)
        Assert.Equal(LimitOption.Defaults.SqlSampleTop, values.SqlSampleTop);
        Assert.Equal(1, values.SqlSampleTop.Min);
        Assert.Equal(50, values.SqlSampleTop.Max);
        Assert.Equal(10, values.SqlSampleTop.Default);
    }
}
