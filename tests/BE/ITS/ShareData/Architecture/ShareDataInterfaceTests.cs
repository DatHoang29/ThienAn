using System.Reflection;
using Modules.Integration.Delivery.Base.Interfaces;
using Modules.Integration.Delivery.Base.Interfaces.Formats;
using Modules.Integration.Delivery.Base.Interfaces.Encrypt;
using Modules.Integration.Delivery;
using Module.ShareData.Core.Entities;
using Module.ShareData.Controllers.Packet;
using ShareDataWorker.Core.Configuration;
using ShareDataWorker.Infrastructure.Services.DataOutbound;

namespace Tests.ShareData.Architecture;

/// <summary>
/// Author: DatHQ
/// Description: Kiểm thử kiến trúc đa hình thực chất cho phân hệ ShareData và thư viện Modules.Integration.Delivery:
///              - Chỉ giữ lại các interface đa hình thực sự (có ít nhất 2 lớp cài đặt cụ thể trong cùng nhóm assembly).
///              - Tập các interface được khai báo trong 6 assembly chỉ gồm đúng 3 interface: IDeliverySender, IPayloadFormat, IEncrypt.
/// Created date: 19/10/2026
/// </summary>
[Collection("api")]
public class ShareDataInterfaceTests
{
    [Fact]
    [Trait("Feature", "Architecture")]
    public void DeclaredInterfaces_EachHasAtLeastTwoImplementations_Test()
    {
        // Arrange: 6 assembly thuộc phân hệ ShareData và thư viện Delivery
        var assemblies = new[]
        {
            typeof(IDeliverySender).Assembly, // Modules.Integration.Delivery.Base
            typeof(DeliveryDispatcher).Assembly, // Modules.Integration.Delivery
            typeof(ShareDataPacket).Assembly, // Module.ShareData.Core
            typeof(ShareDataPacketController).Assembly, // Module.ShareData
            typeof(DataWorkerConfig).Assembly, // ShareDataWorker.Core
            typeof(DataOutboundService).Assembly // ShareDataWorker
        };

        var expectedInterfaces = new HashSet<Type>
        {
            typeof(IDeliverySender),
            typeof(IPayloadFormat),
            typeof(IEncrypt)
        };

        // Act 1: Lấy tất cả interface được khai báo trong 6 assembly
        var declaredInterfaces = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsInterface)
            .ToHashSet();

        // Assert 1: Tập interface đúng bằng {IDeliverySender, IPayloadFormat, IEncrypt}
        Assert.Equal(expectedInterfaces.Count, declaredInterfaces.Count);
        Assert.True(expectedInterfaces.SetEquals(declaredInterfaces),
            $"Tập interface khai báo ({string.Join(", ", declaredInterfaces.Select(t => t.Name))}) không khớp tập mong đợi ({string.Join(", ", expectedInterfaces.Select(t => t.Name))}).");

        // Act 2: Lấy tất cả lớp cụ thể (class, không abstract) trong 6 assembly
        var concreteClasses = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        // Assert 2: Mỗi interface có ít nhất 2 lớp cụ thể cài đặt
        foreach (var iface in declaredInterfaces)
        {
            var implementations = concreteClasses
                .Where(c => iface.IsAssignableFrom(c))
                .ToList();

            Assert.True(implementations.Count >= 2,
                $"Interface '{iface.Name}' chỉ có {implementations.Count} lớp cài đặt ({string.Join(", ", implementations.Select(c => c.Name))}), yêu cầu tối thiểu 2 lớp cài đặt.");
        }
    }
}
