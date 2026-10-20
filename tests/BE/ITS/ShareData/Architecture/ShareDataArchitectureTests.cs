using Modules.Integration.Delivery.Base.Constants;
using Modules.Integration.Delivery;
using Module.ShareData.Core.Entities;
using Module.ShareData.Controllers.Packet;
using ShareDataWorker.Core.Configuration;
using ShareDataWorker.Infrastructure.Services.DataOutbound;

namespace Tests.ShareData.Architecture;

/// <summary>
/// Author: DatHQ
/// Description: Kiểm thử kiến trúc bảo vệ ranh giới của tầng kết nối Modules.Integration.Delivery và Modules.Integration.Delivery.Base:
///              - Tầng kết nối Base là thư viện thuần BCL (System.*, netstandard, mscorlib).
///              - Tầng kết nối xử lý chỉ thêm Microsoft.Extensions.*, Confluent.Kafka và Modules.Integration.Delivery.Base.
///              - Tuyệt đối cấm phụ thuộc Furion, Web, ORM (SqlSugar), Wolverine hay các module/worker khác.
/// </summary>
public class ShareDataArchitectureTests
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
            "Furion",
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
                     || name.Equals("Shared.DTO", StringComparison.OrdinalIgnoreCase);

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
}
