using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>
    /// Description: Rule backend §2 — mọi CommandHandler / QueryHandler của WebAPI ShareData cài IWolverineHandler,
    ///              không đăng ký vòng đời DI bằng marker Furion (ITransient / IScoped / ISingleton).
    /// Created date: 24/10/2026
    /// </summary>
    [Fact]
    [Trait("Feature", "Architecture")]
    public void ShareDataHandlers_ImplementWolverineHandlerWithoutDiLifetimeMarker_Test()
    {
        // Arrange
        var lifetimeMarkers = new[] { "ITransient", "IScoped", "ISingleton" };
        var handlers = typeof(ShareDataPacketController).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && (t.Name.EndsWith("CommandHandler") || t.Name.EndsWith("QueryHandler")))
            .ToList();

        // Act
        var violations = handlers
            .Select(t => (Type: t, Interfaces: t.GetInterfaces().Select(i => i.Name).ToList()))
            .Where(x => !x.Interfaces.Contains("IWolverineHandler") || x.Interfaces.Any(lifetimeMarkers.Contains))
            .Select(x => $"{x.Type.FullName} [{string.Join(", ", x.Interfaces)}]")
            .ToList();

        // Assert
        Assert.NotEmpty(handlers);
        Assert.True(violations.Count == 0,
            $"Handler phải cài IWolverineHandler và không dùng ITransient/IScoped/ISingleton (backend-dotnet_thienan.md §2): {string.Join("; ", violations)}");
    }

    /// <summary>
    /// Description: Rule backend §2 — DTO của ShareData không dùng DataAnnotation ([Required], [Range], [StringLength]...);
    ///              kiểm tra dữ liệu 100% qua FluentValidation trong Controllers/&lt;ChứcNăng&gt;/Validators.
    /// Created date: 24/10/2026
    /// </summary>
    [Fact]
    [Trait("Feature", "Architecture")]
    public void ShareDataDtos_DeclareNoDataAnnotationAttributes_Test()
    {
        // Arrange
        var dtoTypes = new[] { typeof(ShareDataPacket).Assembly, typeof(ShareDataPacketController).Assembly }
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && (t.Namespace ?? string.Empty).Contains(".Dto", StringComparison.Ordinal))
            .ToList();

        // Act
        var violations = dtoTypes
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SelectMany(p => p.GetCustomAttributes(typeof(ValidationAttribute), inherit: false)
                    .Select(a => $"{t.Name}.{p.Name} [{a.GetType().Name}]")))
            .ToList();

        // Assert
        Assert.NotEmpty(dtoTypes);
        Assert.True(violations.Count == 0,
            $"DTO không được dùng DataAnnotation, chuyển sang FluentValidation (backend-dotnet_thienan.md §2): {string.Join("; ", violations)}");
    }

    /// <summary>
    /// Description: Rule 19.44 — ở WebAPI, ID do AOP tự sinh khi Insert; mã nguồn Module.ShareData / Module.ShareData.Core
    ///              không gán tay ID = Guid.NewGuid() (Worker được gán, không thuộc phạm vi bài này).
    /// Created date: 24/10/2026
    /// </summary>
    [Fact]
    [Trait("Feature", "Architecture")]
    public void ShareDataWebApiSource_DoesNotAssignIdManually_Test()
    {
        // Arrange
        var moduleDir = ResolveShareDataModuleDir();
        var assignId = new Regex(@"\bID\s*=\s*Guid\.NewGuid\(", RegexOptions.Compiled);
        var sourceFiles = new[] { "Module.ShareData", "Module.ShareData.Core" }
            .SelectMany(project => Directory.EnumerateFiles(Path.Combine(moduleDir, project), "*.cs", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToList();

        // Act
        var violations = sourceFiles
            .SelectMany(f => File.ReadLines(f)
                .Select((line, index) => (Line: line, Number: index + 1))
                .Where(x => assignId.IsMatch(x.Line))
                .Select(x => $"{Path.GetRelativePath(moduleDir, f)}:{x.Number}"))
            .ToList();

        // Assert
        Assert.NotEmpty(sourceFiles);
        Assert.True(violations.Count == 0,
            $"WebAPI không gán tay ID khi Insert, AOP tự sinh (Rule 19.44): {string.Join("; ", violations)}");
    }

    /// <summary>
    /// Description: Thư mục mã nguồn src/Modules/ShareData của WebAPI, dò ngược từ thư mục chạy test.
    /// Created date: 24/10/2026
    /// </summary>
    private static string ResolveShareDataModuleDir()
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null)
        {
            var candidate = Path.Combine(currentDir.FullName, "TA-ITS015-WEBAPI-V1.0", "src", "Modules", "ShareData");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            currentDir = currentDir.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Không tìm thấy thư mục mã nguồn ShareData khi dò từ '{AppContext.BaseDirectory}'.");
    }
}

/// <summary>
/// Author: DatHQ
/// Description: Đối chiếu client sinh sẵn của FE (api-services/shareData) với route thật của WebAPI: mọi đường dẫn + phương thức HTTP
///              FE gọi phải có hành động tương ứng ở BE. Bắt sớm lỗi 404 khi BE đổi / bỏ API mà client FE chưa sinh lại (hoặc ngược lại).
/// Created date: 28/10/2026
/// </summary>
[Collection("api")]
public class ShareDataApiContractTests(Host host)
{
    [Fact]
    [Trait("Feature", "Architecture")]
    public void FrontendShareDataClient_CallsOnlyExistingWebApiRoutes_Test()
    {
        // Arrange: route thật của WebAPI (template + phương thức HTTP)
        var routes = host.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items
            .Where(a => a.AttributeRouteInfo?.Template != null)
            .SelectMany(a => (a.ActionConstraints?.OfType<HttpMethodActionConstraint>().SelectMany(c => c.HttpMethods) ?? ["GET"])
                .Select(m => $"{m.ToUpperInvariant()} /{a.AttributeRouteInfo!.Template!.TrimStart('/').ToLowerInvariant()}"))
            .ToHashSet();

        // Đường dẫn + phương thức trong client sinh của FE: mỗi hàm có "const localVarPath = `/api/...`" rồi "{ method: 'GET' ...}"
        var clientDir = Path.Combine(ResolveRepoRoot(), "TA-ITS015-WEBVUE-V1.0", "src", "src", "api-services", "shareData", "apis");
        var calls = Directory.EnumerateFiles(clientDir, "*.ts")
            .SelectMany(file => Regex.Matches(File.ReadAllText(file),
                    @"const localVarPath = `(?<path>/api/[^`]+)`;[\s\S]*?method: '(?<method>[A-Z]+)'")
                .Select(m => $"{m.Groups["method"].Value} {m.Groups["path"].Value.ToLowerInvariant()}"))
            .Distinct()
            .ToList();

        // Act
        var missing = calls.Where(c => !routes.Contains(c)).OrderBy(c => c).ToList();

        // Assert
        Assert.NotEmpty(calls);
        Assert.True(missing.Count == 0, $"FE gọi {missing.Count} API không có ở WebAPI (sẽ 404):\n{string.Join("\n", missing)}");
    }

    /// <summary>Thư mục gốc chứa cả TA-ITS015-WEBAPI-V1.0 và TA-ITS015-WEBVUE-V1.0, dò ngược từ thư mục chạy test.</summary>
    private static string ResolveRepoRoot()
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null)
        {
            if (Directory.Exists(Path.Combine(currentDir.FullName, "TA-ITS015-WEBVUE-V1.0", "src", "src", "api-services")))
            {
                return currentDir.FullName;
            }

            currentDir = currentDir.Parent;
        }

        throw new DirectoryNotFoundException($"Không tìm thấy thư mục TA-ITS015-WEBVUE-V1.0 khi dò từ '{AppContext.BaseDirectory}'.");
    }
}
