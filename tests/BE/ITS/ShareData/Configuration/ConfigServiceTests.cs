using Microsoft.Extensions.Logging.Abstractions;
using Module.ShareData.Infrastructure.Services.ShareDataConfig;
using Modules.CfgSystem.Core.Entities;

namespace Tests.ShareData.Configuration;

/// <summary>
/// Author: DatHQ
/// Description: Bộ kiểm thử đơn vị và tích hợp cho ConfigService (WebAPI đọc các giới hạn vận hành từ SysOpConfig qua IMessageBus).
///              Kiểm tra cơ chế fallback mặc định và khoảng an toàn tuyệt đối.
/// Created date: 18/10/2026
/// Modified date: 24/10/2026
/// </summary>
[Collection("api")]
public class ConfigServiceTests(Host host)
{
    private readonly Host _host = host;

    [Fact]
    public async Task ConfigService_WhenNoDatabaseConfig_ReturnsExactDefaults_Test()
    {
        // Arrange
        var isolatedGroup = $"sd_webapi_missing_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup
            })
            .Build();

        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var service = new ConfigService(
            bus: bus,
            logger: NullLogger<ConfigService>.Instance,
            configuration: config);

        // Act
        var values = await service.Get();

        // Assert
        Assert.Equal(ConfigService.Defaults, values);
        Assert.Equal(ConfigService.Defaults.PacketLookbackDays.Min, values.PacketLookbackDays.Min);
        Assert.Equal(ConfigService.Defaults.PacketLookbackDays.Max, values.PacketLookbackDays.Max);
        Assert.Equal(ConfigService.Defaults.SqlSampleTop, values.SqlSampleTop);
        Assert.Equal(ConfigService.Defaults.MappingSampleTop, values.MappingSampleTop);
        Assert.Equal(ConfigService.Defaults.LogListDefaultTop, values.LogListDefaultTop);
        Assert.Equal(ConfigService.Defaults.SubscriptionInterval, values.SubscriptionInterval);
        Assert.Equal(ConfigService.Defaults.SubscriptionPriority, values.SubscriptionPriority);
        Assert.Equal(ConfigService.Defaults.SubscriptionDebounceSec, values.SubscriptionDebounceSec);
    }

    [Fact]
    public async Task ConfigService_WhenValidConfigInDatabase_LoadsCustomLimits_Test()
    {
        // Arrange
        var isolatedGroup = $"sd_webapi_valid_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup
            })
            .Build();

        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
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
        scope.ServiceProvider.GetRequiredService<Shared.Infrastructure.Services.BaseCacheService>().RemoveByPrefixKey("sysOpConfig");

        var service = new ConfigService(
            bus: bus,
            logger: NullLogger<ConfigService>.Instance,
            configuration: config);

        // Act
        var values = await service.Get();

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
    public async Task ConfigService_WhenTripletMisaligned_FallsBackToDefaultTriplet_Test()
    {
        // Arrange: Cấu hình default > max hoặc max < min -> lệch bộ
        var isolatedGroup = $"sd_webapi_misaligned_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup
            })
            .Build();

        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var testRows = new List<SysOpConfig>
        {
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "sql_sample_top_min", Value = "20", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "sql_sample_top_max", Value = "10", Status = BaseEnums.StatusEnum.Enable }, // Max < Min!
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "sql_sample_top_default", Value = "15", Status = BaseEnums.StatusEnum.Enable }
        };
        await db.Insertable(testRows).ExecuteCommandAsync();
        scope.ServiceProvider.GetRequiredService<Shared.Infrastructure.Services.BaseCacheService>().RemoveByPrefixKey("sysOpConfig");

        var service = new ConfigService(
            bus: bus,
            logger: NullLogger<ConfigService>.Instance,
            configuration: config);

        // Act
        var values = await service.Get();

        // Assert: Phải quay về nguyên bộ dự phòng (1, 50, 10)
        Assert.Equal(ConfigService.Defaults.SqlSampleTop, values.SqlSampleTop);
        Assert.Equal(1, values.SqlSampleTop.Min);
        Assert.Equal(50, values.SqlSampleTop.Max);
        Assert.Equal(10, values.SqlSampleTop.Default);
    }

    [Fact]
    public async Task ConfigService_WhenValuesInvalidOrOutOfRangeOrDuplicate_HandlesSafely_Test()
    {
        // Arrange: Chèn giá trị chữ, ngoài khoảng, Code lạ và Code trùng (dòng sau thắng)
        var isolatedGroup = $"sd_webapi_invalid_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup
            })
            .Build();

        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var baseId = DateTime.UtcNow.Ticks;
        var testRows = new List<SysOpConfig>
        {
            // Giá trị chữ -> fallback
            new() { ID = $"{baseId}_1", GroupCode = isolatedGroup, Code = "packet_lookback_days_min", Value = "NOT_A_NUMBER", Status = BaseEnums.StatusEnum.Enable },
            // Ngoài khoảng max -> fallback
            new() { ID = $"{baseId}_2", GroupCode = isolatedGroup, Code = "packet_lookback_days_max", Value = "999999", Status = BaseEnums.StatusEnum.Enable },
            // Ngoài khoảng min -> fallback
            new() { ID = $"{baseId}_3", GroupCode = isolatedGroup, Code = "subscription_interval_sec_min", Value = "-10", Status = BaseEnums.StatusEnum.Enable },
            // Code lạ không có trong spec -> bị bỏ qua
            new() { ID = $"{baseId}_4", GroupCode = isolatedGroup, Code = "unknown_webapi_config_code", Value = "123", Status = BaseEnums.StatusEnum.Enable },
            // Code trùng: dòng 1 đặt 100, dòng 2 đặt 500 -> dòng sau thắng (500)
            new() { ID = $"{baseId}_5", GroupCode = isolatedGroup, Code = "log_list_default_top", Value = "100", Status = BaseEnums.StatusEnum.Enable },
            new() { ID = $"{baseId}_6", GroupCode = isolatedGroup, Code = "log_list_default_top", Value = "500", Status = BaseEnums.StatusEnum.Enable }
        };
        await db.Insertable(testRows).ExecuteCommandAsync();
        scope.ServiceProvider.GetRequiredService<Shared.Infrastructure.Services.BaseCacheService>().RemoveByPrefixKey("sysOpConfig");

        var service = new ConfigService(
            bus: bus,
            logger: NullLogger<ConfigService>.Instance,
            configuration: config);

        // Act
        var values = await service.Get();

        // Assert
        Assert.Equal(ConfigService.Defaults.PacketLookbackDays.Min, values.PacketLookbackDays.Min);
        Assert.Equal(ConfigService.Defaults.PacketLookbackDays.Max, values.PacketLookbackDays.Max);
        Assert.Equal(ConfigService.Defaults.SubscriptionInterval.Min, values.SubscriptionInterval.Min);
        Assert.Equal(500, values.LogListDefaultTop);
    }
}
