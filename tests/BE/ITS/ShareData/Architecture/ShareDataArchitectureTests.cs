using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery;
using Modules.Integration.Delivery.Senders;
using Module.ShareData.Core.Entities;
using Module.ShareData.Controllers.Packet;
using ShareDataWorker.Core.Configuration;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.ShareData.Architecture;

/// <summary>
/// Author: DatHQ
/// Description: Kiểm thử kiến trúc bảo vệ ranh giới của tầng kết nối Modules.Integration.Delivery và Modules.Integration.Delivery.Base:
///              - Tầng kết nối Base là thư viện thuần BCL (System.*, netstandard, mscorlib).
///              - Tầng kết nối xử lý dùng Furion cho DI, Microsoft.Extensions.*, Confluent.Kafka và Modules.Integration.Delivery.Base (Rule 19.94).
///              - Tuyệt đối cấm phụ thuộc Web (Microsoft.AspNetCore), ORM (SqlSugar), Wolverine hay các module/worker khác.
/// </summary>
[Collection("api")]
public class ShareDataArchitectureTests(Host host)
{
    [Fact]
    [Trait("Feature", "Architecture")]
    public void IntegrationBaseAssembly_ReferencesOnlyBclAssemblies_Test()
    {
        // Arrange
        var assembly = typeof(ConnectKeys).Assembly;
        var referencedAssemblies = assembly.GetReferencedAssemblies();

        var forbiddenPrefixes = new[]
        {
            "Furion",
            "Microsoft.AspNetCore",
            "Wolverine",
            "SqlSugar",
            "Module.ShareData",
            "ShareDataWorker",
            "Confluent.Kafka",
            "Microsoft.Extensions."
        };

        // Assert
        Assert.NotEmpty(referencedAssemblies);

        foreach (var referenced in referencedAssemblies)
        {
            var name = referenced.Name ?? string.Empty;

            foreach (var forbidden in forbiddenPrefixes)
            {
                Assert.DoesNotContain(forbidden, name, StringComparison.OrdinalIgnoreCase);
            }

            var isAllowed = name.StartsWith("System", StringComparison.OrdinalIgnoreCase)
                     || name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
                     || name.Equals("netstandard", StringComparison.OrdinalIgnoreCase)
                     || name.Equals("Shared.DTO", StringComparison.OrdinalIgnoreCase);

            Assert.True(isAllowed, $"Assembly '{name}' không thuộc danh sách thư viện được phép của Modules.Integration.Delivery.Base.");
        }
    }

    [Fact]
    [Trait("Feature", "Architecture")]
    public void IntegrationAssembly_ReferencesOnlyBclAssemblies_Test()
    {
        // Arrange
        var assembly = typeof(DeliveryDispatcher).Assembly;
        var referencedAssemblies = assembly.GetReferencedAssemblies();

        var forbiddenPrefixes = new[]
        {
            "Microsoft.AspNetCore",
            "Wolverine",
            "SqlSugar",
            "Module.ShareData",
            "ShareDataWorker"
        };

        // Assert
        Assert.NotEmpty(referencedAssemblies);

        foreach (var referenced in referencedAssemblies)
        {
            var name = referenced.Name ?? string.Empty;

            foreach (var forbidden in forbiddenPrefixes)
            {
                Assert.DoesNotContain(forbidden, name, StringComparison.OrdinalIgnoreCase);
            }

            var isAllowed = name.StartsWith("System", StringComparison.OrdinalIgnoreCase)
                     || name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
                     || name.Equals("netstandard", StringComparison.OrdinalIgnoreCase)
                     || name.StartsWith("Microsoft.Extensions.", StringComparison.OrdinalIgnoreCase)
                     || name.StartsWith("Confluent.Kafka", StringComparison.OrdinalIgnoreCase)
                     || name.Equals("Modules.Integration.Delivery.Base", StringComparison.OrdinalIgnoreCase)
                     || name.Equals("Shared.DTO", StringComparison.OrdinalIgnoreCase)
                     || name.StartsWith("Furion", StringComparison.OrdinalIgnoreCase)
                     || name.Equals("Shared.Reference", StringComparison.OrdinalIgnoreCase);

            Assert.True(isAllowed, $"Assembly '{name}' không thuộc danh sách thư viện được phép của Modules.Integration.Delivery.");
        }
    }

    [Fact]
    [Trait("Feature", "Architecture")]
    public void ShareDataAssemblies_DeclareNoInterfaces_Test()
    {
        // Arrange: 6 assembly thuộc phân hệ ShareData và thư viện Delivery
        var assemblies = new[]
        {
            typeof(ConnectKeys).Assembly, // Modules.Integration.Delivery.Base
            typeof(DeliveryDispatcher).Assembly, // Modules.Integration.Delivery
            typeof(ShareDataPacket).Assembly, // Module.ShareData.Core
            typeof(ShareDataPacketController).Assembly, // Module.ShareData
            typeof(DataWorkerConfig).Assembly, // ShareDataWorker.Core
            typeof(DataOutboundService).Assembly // ShareDataWorker
        };

        // Act
        var declaredInterfaces = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsInterface)
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        // Assert
        Assert.True(declaredInterfaces.Count == 0,
            $"6 assembly ShareData/Delivery không được khai báo interface, nhưng tìm thấy: {string.Join(", ", declaredInterfaces)}.");
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Kiểm tra các dịch vụ tầng Delivery và Worker đăng ký qua marker ISingleton
    ///              khi resolve hai lần từ Host IServiceProvider sẽ trả về cùng một thể hiện duy nhất (Singleton).
    /// Created date: 20/10/2026
    /// </summary>
    [Fact]
    [Trait("Feature", "Architecture")]
    public void DeliveryAndWorkerServices_WhenResolvedTwice_ReturnSameInstance_Test()
    {
        // 1. DeliveryDispatcher
        var dispatcher1 = host.Services.GetRequiredService<DeliveryDispatcher>();
        var dispatcher2 = host.Services.GetRequiredService<DeliveryDispatcher>();
        Assert.NotNull(dispatcher1);
        Assert.Same(dispatcher1, dispatcher2);

        // 2. Bốn lớp gửi cụ thể
        var http1 = host.Services.GetRequiredService<HttpDeliverySender>();
        var http2 = host.Services.GetRequiredService<HttpDeliverySender>();
        Assert.NotNull(http1);
        Assert.Same(http1, http2);

        var file1 = host.Services.GetRequiredService<FileDeliverySender>();
        var file2 = host.Services.GetRequiredService<FileDeliverySender>();
        Assert.NotNull(file1);
        Assert.Same(file1, file2);

        var ftp1 = host.Services.GetRequiredService<FtpDeliverySender>();
        var ftp2 = host.Services.GetRequiredService<FtpDeliverySender>();
        Assert.NotNull(ftp1);
        Assert.Same(ftp1, ftp2);

        var kafka1 = host.Services.GetRequiredService<KafkaDeliverySender>();
        var kafka2 = host.Services.GetRequiredService<KafkaDeliverySender>();
        Assert.NotNull(kafka1);
        Assert.Same(kafka1, kafka2);

        // 3. Worker service
        var outbound1 = host.Services.GetRequiredService<DataOutboundService>();
        var outbound2 = host.Services.GetRequiredService<DataOutboundService>();
        Assert.NotNull(outbound1);
        Assert.Same(outbound1, outbound2);
    }
}
