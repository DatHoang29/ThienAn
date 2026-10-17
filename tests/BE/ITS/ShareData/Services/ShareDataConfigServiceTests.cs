using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Module.ShareData.Infrastructure.Services.ShareDataConfig;
using Modules.CfgSystem.Core.Entities;
using Shared.DTO.Enums;
using SqlSugar;
using Xunit;

namespace Tests.ShareData.Services;

/// <summary>
/// Author: DatHQ
/// Description: Bộ kiểm thử đơn vị và tích hợp cho ShareDataConfigService (WebAPI đọc các giới hạn vận hành từ SysOpConfig).
///              Kiểm tra cơ chế fallback mặc định và khoảng an toàn tuyệt đối.
/// Created date: 17/10/2026
/// </summary>
[Collection("api")]
public class ShareDataConfigServiceTests(Host host)
{
    private readonly Host _host = host;

    [Fact]
    public void ShareDataConfigService_WhenNoDatabaseConfig_ReturnsExactDefaults_Test()
    {
        // Arrange
        var isolatedGroup = $"sd_webapi_missing_{Guid.NewGuid():N}";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ShareDataConfig:GroupCode"] = isolatedGroup
            })
            .Build();

        var service = new ShareDataConfigService(
            db: null,
            logger: NullLogger<ShareDataConfigService>.Instance,
            configuration: config);

        // Act & Assert
        var (lookbackMin, lookbackMax) = service.GetPacketLookbackDaysRange();
        Assert.Equal(1, lookbackMin);
        Assert.Equal(365, lookbackMax);

        var (sqlMin, sqlMax, sqlDef) = service.GetSqlSampleTopBounds();
        Assert.Equal(1, sqlMin);
        Assert.Equal(50, sqlMax);
        Assert.Equal(10, sqlDef);

        var (mapMin, mapMax, mapDef) = service.GetMappingSampleTopBounds();
        Assert.Equal(1, mapMin);
        Assert.Equal(50, mapMax);
        Assert.Equal(5, mapDef);

        var logTop = service.GetLogListDefaultTop();
        Assert.Equal(1000, logTop);

        var (subMin, subMax) = service.GetSubscriptionIntervalRange();
        Assert.Equal(5, subMin);
        Assert.Equal(86400, subMax);
    }

    [Fact]
    public async Task ShareDataConfigService_WhenValidConfigInDatabase_LoadsCustomLimits_Test()
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
            new() { ID = Guid.NewGuid().ToString("N"), GroupCode = isolatedGroup, Code = "subscription_interval_sec_max", Value = "3600", Status = BaseEnums.StatusEnum.Enable }
        };
        await db.Insertable(testRows).ExecuteCommandAsync();

        var service = new ShareDataConfigService(
            db: db,
            logger: NullLogger<ShareDataConfigService>.Instance,
            configuration: config);

        // Act & Assert
        var (lookbackMin, lookbackMax) = service.GetPacketLookbackDaysRange();
        Assert.Equal(3, lookbackMin);
        Assert.Equal(90, lookbackMax);

        var logTop = service.GetLogListDefaultTop();
        Assert.Equal(500, logTop);

        var (subMin, subMax) = service.GetSubscriptionIntervalRange();
        Assert.Equal(10, subMin);
        Assert.Equal(3600, subMax);
    }

    [Fact]
    public async Task ShareDataConfigService_WhenTripletMisaligned_FallsBackToDefaultTriplet_Test()
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

        var service = new ShareDataConfigService(
            db: db,
            logger: NullLogger<ShareDataConfigService>.Instance,
            configuration: config);

        // Act & Assert: Phải quay về nguyên bộ dự phòng (1, 50, 10)
        var (sqlMin, sqlMax, sqlDef) = service.GetSqlSampleTopBounds();
        Assert.Equal(1, sqlMin);
        Assert.Equal(50, sqlMax);
        Assert.Equal(10, sqlDef);
    }
}
