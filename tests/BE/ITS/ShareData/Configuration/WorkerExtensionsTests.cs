using Microsoft.Extensions.Hosting;
using Modules.Integration.Delivery.Base.Helpers;
using Services.Shared.Messaging;
using ShareDataWorker.Extensions;
using ShareDataWorker.Infrastructure.Services.DataInbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
using ShareDataWorker.Infrastructure.Workers;

namespace Tests.ShareData.Configuration;

/// <summary>
/// Author: DatHQ
/// Description: Kiểm thử đăng ký tiến trình nền theo cờ cấu hình ShareData:Role trong ShareDataWorkerExtensions.AddWorkerInfrastructure.
/// Created date: 02/11/2026
/// </summary>
public class WorkerExtensionsTests
{
    private static IServiceCollection BuildServicesWithRole(string? role)
    {
        var inMemory = new Dictionary<string, string?>();
        if (role != null)
        {
            inMemory["ShareData:Role"] = role;
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();

        var services = new ServiceCollection();
        services.AddWorkerInfrastructure(config);
        return services;
    }

    private static List<Type> GetRegisteredHostedServices(IServiceCollection services)
    {
        return services
            .Where(s => s.ServiceType == typeof(IHostedService))
            .Select(s => s.ImplementationType!)
            .ToList();
    }

    /// <summary>
    /// Description: SV-4 · Đăng ký đúng các tiến trình nền theo vai trò ShareData:Role (không phân biệt hoa thường):
    ///              - Không khai / rỗng / Both: đủ 5 worker + HeartbeatPublisher.
    ///              - SendOnly: DataOutboundWorker, DataChangeTrackingWorker, DataNatsWorker, DataCleanupWorker + HeartbeatPublisher.
    ///              - ReceiveOnly: DataInboundWorker, DataCleanupWorker + HeartbeatPublisher.
    /// Created date: 02/11/2026
    /// </summary>
    [Theory]
    [Trait("Feature", "SV-4")]
    [InlineData(null, true, true)]
    [InlineData("", true, true)]
    [InlineData("   ", true, true)]
    [InlineData("Both", true, true)]
    [InlineData("both", true, true)]
    [InlineData("SendOnly", true, false)]
    [InlineData("sendonly", true, false)]
    [InlineData("ReceiveOnly", false, true)]
    [InlineData("receiveonly", false, true)]
    [InlineData("  SendOnly  ", true, false)]
    [InlineData(" receiveonly ", false, true)]
    public void AddWorkerInfrastructure_WhenValidRoleConfigured_RegistersExpectedWorkers_Test(
        string? role,
        bool expectSendWorkers,
        bool expectReceiveWorker)
    {
        // Act
        var services = BuildServicesWithRole(role);
        var workers = GetRegisteredHostedServices(services);

        // Assert
        var expectedCount = 2 + (expectSendWorkers ? 3 : 0) + (expectReceiveWorker ? 1 : 0);
        Assert.Equal(expectedCount, workers.Count);
        Assert.Contains(typeof(DataCleanupWorker), workers);
        Assert.Contains(typeof(HeartbeatPublisher), workers);

        if (expectSendWorkers)
        {
            Assert.Contains(typeof(DataOutboundWorker), workers);
            Assert.Contains(typeof(DataChangeTrackingWorker), workers);
            Assert.Contains(typeof(DataNatsWorker), workers);
        }
        else
        {
            Assert.DoesNotContain(typeof(DataOutboundWorker), workers);
            Assert.DoesNotContain(typeof(DataChangeTrackingWorker), workers);
            Assert.DoesNotContain(typeof(DataNatsWorker), workers);
        }

        if (expectReceiveWorker)
        {
            Assert.Contains(typeof(DataInboundWorker), workers);
        }
        else
        {
            Assert.DoesNotContain(typeof(DataInboundWorker), workers);
        }

        Assert.Contains(services, s => s.ServiceType == typeof(DataOutboundService));
        Assert.Contains(services, s => s.ServiceType == typeof(DataInboundService));
    }

    /// <summary>
    /// Description: SV-4 · Cấu hình ShareData:Role ngoài ba giá trị hợp lệ (Both, SendOnly, ReceiveOnly)
    ///              phải ném lỗi ngay lúc khởi động để ngăn chạy nhầm thành Both làm nhiều tiến trình cùng nhận gói tin.
    /// Created date: 02/11/2026
    /// </summary>
    [Theory]
    [Trait("Feature", "SV-4")]
    [InlineData("InvalidRole")]
    [InlineData("Sender")]
    [InlineData("Receiver")]
    public void AddWorkerInfrastructure_WhenRoleInvalid_ThrowsInvalidOperationException_Test(string invalidRole)
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => BuildServicesWithRole(invalidRole));
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: NasStorage:BasePath tương đối hoặc để trống được tính theo ContentRoot của host (dịch vụ Windows đặt ContentRoot là thư mục chứa exe),
    ///              không theo thư mục hiện hành; đường dẫn tuyệt đối giữ nguyên.
    /// Created date: 10/11/2026
    /// </summary>
    [Theory]
    [InlineData("sharedata/send", false)]
    [InlineData(null, false)]
    [InlineData("   ", false)]
    [InlineData("sharedata/send", true)]
    public void UseContentRootStoragePath_WhenHostBuilt_ResolvesBasePathFromContentRoot_Test(string? relativePath, bool absolute)
    {
        // Arrange
        var contentRoot = Directory.CreateTempSubdirectory("sharedata-root-").FullName;
        var absolutePath = Path.Combine(Path.GetTempPath(), "sharedata-shared", "send");
        var configured = absolute ? absolutePath : relativePath;
        var settings = new Dictionary<string, string?> { [ExportPathHelper.BasePathConfigKey] = configured };
        var builder = new HostBuilder()
            .UseContentRoot(contentRoot)
            .ConfigureAppConfiguration(config => config.AddInMemoryCollection(settings))
            .UseContentRootStoragePath();

        // Act
        using var host = builder.Build();
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var resolved = configuration[ExportPathHelper.BasePathConfigKey];

        // Assert
        var expected = absolute ? absolutePath : Path.Combine(contentRoot, ExportPathHelper.DefaultBasePath);
        Assert.Equal(expected, resolved);
    }
}
