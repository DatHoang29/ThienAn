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
public class ShareDataIntegrationAssemblyTests
{
    [Fact]
    [Trait("Feature", "Architecture")]
    public void IntegrationAssembly_ReferencesOnlyBclAssemblies_Test()
    {
        // Arrange
        var assembly = typeof(ShareDataConnectionKeys).Assembly;
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

            var isBcl = name.StartsWith("System", StringComparison.OrdinalIgnoreCase)
                     || name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
                     || name.Equals("netstandard", StringComparison.OrdinalIgnoreCase);

            Assert.True(isBcl, $"Assembly '{name}' không thuộc BCL (.NET Core Base Class Library). Modules.Integration.ShareData phải là pure BCL assembly.");
        }
    }
}
