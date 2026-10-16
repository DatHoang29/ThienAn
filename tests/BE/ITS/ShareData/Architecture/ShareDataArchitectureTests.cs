using System;
using System.Reflection;
using Modules.Integration.ShareData;
using Xunit;

namespace Tests.ShareData.Architecture;

/// <summary>
/// Author: DatHQ
/// Description: Kiểm thử kiến trúc bảo vệ ranh giới của tầng kết nối Modules.Integration.ShareData:
///              - Tầng kết nối là thư viện thuần BCL (System.*, netstandard, mscorlib).
///              - Tuyệt đối cấm phụ thuộc Furion, Web, ORM (SqlSugar), Wolverine hay các module/worker khác.
/// </summary>
public class ShareDataArchitectureTests
{
    /// <summary>
    /// [Trait] trong xUnit: Đây là thuộc tính gắn siêu dữ liệu (metadata/tag/category) cho bài test,
    /// cho phép gán nhãn để phân loại bài kiểm thử theo nhóm tính năng, mức độ ưu tiên hoặc môi trường thực thi.
    /// 
    /// Ý nghĩa của Feature = "Architecture":
    /// Phân loại đây là nhóm kiểm thử kiến trúc (Architecture Tests / Convention Tests) nhằm kiểm tra cấu trúc
    /// mã nguồn, quy tắc tham chiếu và tính độc lập giữa các tầng/dự án mà không cần khởi động CSDL hay môi trường mạng.
    /// Giúp lập trình viên có thể lọc và chạy riêng nhóm test kiến trúc nhanh chóng bằng lệnh:
    /// `dotnet test --filter "Feature=Architecture"`.
    /// </summary>
    [Fact]
    [Trait("Feature", "Architecture")]
    public void IntegrationAssembly_ReferencesOnlyBclAssemblies_Test()
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
            "Shared.",
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
                     || name.StartsWith("Confluent.Kafka", StringComparison.OrdinalIgnoreCase);

            Assert.True(isAllowed, $"Assembly '{name}' không thuộc danh sách thư viện được phép của Modules.Integration.ShareData.");
        }
    }
}
