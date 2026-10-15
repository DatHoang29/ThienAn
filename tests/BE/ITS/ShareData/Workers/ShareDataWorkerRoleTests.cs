namespace Tests.ShareData.Workers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShareDataWorker.Extensions;
using ShareDataWorker.Infrastructure.Workers;
using Xunit;

public class ShareDataWorkerRoleTests
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
    /// Description: SV-4 - Khởi động worker mặc định luôn đăng ký đầy đủ 4 workers
    ///              (DataOutboundWorker, DataInboundWorker, DataChangeTrackingWorker, DataNatsWorker)
    ///              phục vụ vận hành 2 chiều đồng thời theo thiết kế Production.
    /// Created date: 12/10/2026
    /// </summary>
    [Theory]
    [Trait("Feature", "SV-4")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Both")]
    [InlineData("both")]
    public void WhenWorkerStarts_RegistersAllFourWorkers_EnsuresFullTwoWayOperation(string? role)
    {
        // Act
        var services = BuildServicesWithRole(role);
        var workers = GetRegisteredHostedServices(services);

        // Assert: Mặc định đăng ký đủ 5 workers (vừa gửi vừa nhận theo đúng thiết kế Production)
        Assert.Equal(5, workers.Count);
        Assert.Contains(typeof(DataOutboundWorker), workers);
        Assert.Contains(typeof(DataInboundWorker), workers);
        Assert.Contains(typeof(DataChangeTrackingWorker), workers);
        Assert.Contains(typeof(DataNatsWorker), workers);
        Assert.Contains(typeof(ShareDataCleanupWorker), workers);
    }
}
