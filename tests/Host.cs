using Furion;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NewLife.Caching;
using Shared.Core.Security;
using Shared.Core.Settings.Options;
using Shared.Infrastructure.Localization;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Tests;

[CollectionDefinition("api")]
public class ApiTestCollection : ICollectionFixture<Host> { }

public partial class Host : IAsyncLifetime
{
    private const string TestDatabaseName = "test";
    private const string TestCultureName = "vi-VN";

    private const string DefaultLocalConnectionString =
        "Server=localhost,14333;Database=test;User Id=sa;Password=Password123!;TrustServerCertificate=true;";

    private static readonly string[] AllowedLocalHosts = ["127.0.0.1", "localhost", "(localdb)", "."];

    private static readonly Dictionary<string, string?> InMemoryTestConfigurations = new()
    {
        // ─── ConnectionStrings ───
        ["ConnectionStrings:Default"] = DefaultLocalConnectionString,
        ["ConnectionStrings:DefaultConnection"] = DefaultLocalConnectionString,
        ["ConnectionStrings:LogDefault"] = DefaultLocalConnectionString,

        // ─── DbConnection ───
        ["DbConnection:EnableConsoleSql"] = "false",
        ["DbConnection:ConnectionConfigs:0:ConfigId"] = "Default",
        ["DbConnection:ConnectionConfigs:0:DbType"] = "SqlServer",
        ["DbConnection:ConnectionConfigs:0:ConnectionString"] = DefaultLocalConnectionString,
        ["DbConnection:ConnectionConfigs:0:DbSettings:EnableInitDb"] = "true",
        ["DbConnection:ConnectionConfigs:0:DbSettings:EnableDiffLog"] = "false",
        ["DbConnection:ConnectionConfigs:0:DbSettings:EnableUnderLine"] = "false",
        ["DbConnection:ConnectionConfigs:0:TableSettings:EnableInitTable"] = "false",
        ["DbConnection:ConnectionConfigs:0:TableSettings:EnableIncreTable"] = "false",
        ["DbConnection:ConnectionConfigs:0:SeedSettings:EnableInitSeed"] = "false",
        ["DbConnection:ConnectionConfigs:0:SeedSettings:EnableIncreSeed"] = "false",

        ["DbConnection:ConnectionConfigs:1:ConfigId"] = "LogDefault",
        ["DbConnection:ConnectionConfigs:1:DbType"] = "SqlServer",
        ["DbConnection:ConnectionConfigs:1:ConnectionString"] = DefaultLocalConnectionString,
        ["DbConnection:ConnectionConfigs:1:DbSettings:EnableInitDb"] = "true",
        ["DbConnection:ConnectionConfigs:1:DbSettings:EnableDiffLog"] = "false",
        ["DbConnection:ConnectionConfigs:1:DbSettings:EnableUnderLine"] = "false",
        ["DbConnection:ConnectionConfigs:1:TableSettings:EnableInitTable"] = "false",
        ["DbConnection:ConnectionConfigs:1:TableSettings:EnableIncreTable"] = "false",
        ["DbConnection:ConnectionConfigs:1:SeedSettings:EnableInitSeed"] = "false",
        ["DbConnection:ConnectionConfigs:1:SeedSettings:EnableIncreSeed"] = "false",

        // ─── JwtSettings ───
        ["JwtSettings:key"] = "sCDqZXgppm1WBNjhvksTrlRybEVtIEmF",
        ["JwtSettings:tokenExpirationInMinutes"] = "100000",
        ["JwtSettings:refreshTokenExpirationInDays"] = "7",

        // ─── LocalizationSettings ───
        ["LocalizationSettings:SupportedCultures:0"] = TestCultureName,
        ["LocalizationSettings:SupportedCultures:1"] = "en",
        ["LocalizationSettings:DefaultCulture"] = TestCultureName,
        ["LocalizationSettings:DateTimeFormatCulture"] = TestCultureName,

        // ─── DynamicApiControllerSettings ───
        ["DynamicApiControllerSettings:CamelCaseSeparator"] = "",
        ["DynamicApiControllerSettings:SplitCamelCase"] = "false",
        ["DynamicApiControllerSettings:LowercaseRoute"] = "false",
        ["DynamicApiControllerSettings:AsLowerCamelCase"] = "true",
        ["DynamicApiControllerSettings:KeepVerb"] = "false",
        ["DynamicApiControllerSettings:KeepName"] = "false",

        // ─── FriendlyExceptionSettings ───
        ["FriendlyExceptionSettings:DefaultErrorMessage"] = "System exception, please contact the administrator",
        ["FriendlyExceptionSettings:ThrowBah"] = "true",
        ["FriendlyExceptionSettings:LogError"] = "false",

        // ─── AppSettings ───
        ["AppSettings:InjectSpecificationDocument"] = "false",

        // ─── Cache (FusionCache Memory) ───
        ["Cache:Prefix"] = "tac_",
        ["Cache:Provider"] = "FusionCache",
        ["Cache:FusionCache:CacheType"] = "Memory",
        ["Cache:FusionCache:DefaultDuration"] = "00:30:00",
        ["Cache:FusionCache:FailSafeEnabled"] = "true",
        ["Cache:FusionCache:FailSafeMaxDuration"] = "02:00:00",
        ["Cache:FusionCache:FailSafeThrottleDuration"] = "00:00:30",
        ["Cache:FusionCache:FactorySoftTimeout"] = "00:00:00.100",
        ["Cache:FusionCache:FactoryHardTimeout"] = "00:00:01.500",
        ["Cache:FusionCache:AllowBackgroundDistributedCacheOperations"] = "true",

        // ─── CacheConfiguration ───
        ["CacheConfiguration:AbsoluteExpirationInHours"] = "10",
        ["CacheConfiguration:SlidingExpirationInMinutes"] = "30",

        // ─── Hangfire ───
        ["Hangfire:Enable"] = "false",

        // ─── Logging ───
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:File:Enabled"] = "false",
        ["Logging:Database:Enabled"] = "false"
    };

    private WebApplicationFactory<TAC_WebAPI.Program>? _host;
    private HttpClient? _apiClient;
    public HttpClient ApiClient => _apiClient ?? throw new InvalidOperationException("Host not initialized");
    public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("Host not initialized");
    public IServiceProvider ApiServices => Services;
    public IStringLocalizer Localizer => Services.GetRequiredService<IStringLocalizer>();
    public HttpClient CreateApiClient(params DelegatingHandler[] handlers) => _host?.CreateDefaultClient(handlers) ?? throw new InvalidOperationException("Host not initialized");

    public async Task InitializeAsync()
    {
        Console.OutputEncoding = Encoding.UTF8;
        ApplyTestCulture();
        EnsureValidTestLicense();

        _host = new WebApplicationFactory<TAC_WebAPI.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Urls", "http://127.0.0.1:0");

                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.Sources.Clear();
                    configBuilder.AddInMemoryCollection(InMemoryTestConfigurations);
                });

                builder.ConfigureServices(services =>
                {
#if HAS_SHAREDATAWORKER
                    ShareDataWorker.Extensions.ShareDataWorkerExtensions.AddShareDataWorkerCoreServices(services);
#endif
                });
            });

        _apiClient = _host.CreateClient();
        GuardAllConnectionsLocal(_host.Services);
        BindFurionRootServices(_host.Services);
        StartModuleTestServers();

        ClearAllData();
        ClearAllCache();
    }

    public async Task DisposeAsync()
    {
        _apiClient?.Dispose();
        _apiClient = null;

        _host?.Dispose();
        _host = null;

        StopModuleTestServers();
    }

    public void ClearAllData()
    {
        var db = _host?.Services.GetService<ISqlSugarClient>();
        if (db == null)
            return;

        GuardSqlConnectionIsLocal(db.CurrentConnectionConfig?.ConnectionString, "Database");

        var existingTables = db.DbMaintenance
            .GetTableInfoList(false)
            .Select(t => t.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var entityTableNames = GetSugarEntityTypes()
            .Select(t => db.EntityMaintenance.GetTableName(t))
            .Where(name => !string.IsNullOrWhiteSpace(name) && existingTables.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var tableName in entityTableNames)
            db.DbMaintenance.TruncateTable(tableName);
    }

    public void ClearAllCache()
    {
        var cacheProvider = _host?.Services.GetService<ICacheProvider>();
        cacheProvider?.Cache?.Clear();
    }

    partial void StartModuleTestServers();

    partial void StopModuleTestServers();

    private static void BindFurionRootServices(IServiceProvider services)
    {
        var rootServicesField = typeof(App).Assembly
            .GetType("Furion.InternalApp")
            ?.GetField("RootServices", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Không tìm thấy Furion.InternalApp.RootServices — Furion đã đổi API, cần cập nhật lại Test Host.");

        rootServicesField.SetValue(null, services);
    }

    private static void ApplyTestCulture()
    {
        var culture = new CultureInfo(TestCultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    private static void EnsureValidTestLicense()
    {
        string hwid;
        try
        {
            hwid = LicenseService.GetHardwareId();
        }
        catch
        {
            hwid = "TEST_MACHINE_FALLBACK_HWID";
        }

        if (string.IsNullOrWhiteSpace(hwid))
            hwid = "TEST_MACHINE_FALLBACK_HWID";

        using var rsa = RSA.Create(2048);
        var pubKeyBytes = rsa.ExportRSAPublicKey();
        var pubKeyBase64 = Convert.ToBase64String(pubKeyBytes);
        var pubKeyPem = $"-----BEGIN RSA PUBLIC KEY-----\r\n{pubKeyBase64}\r\n-----END RSA PUBLIC KEY-----";

        var headerJson = """{"alg":"RS256","typ":"JWT"}""";
        var payloadJson = $$"""{"hwid":"{{hwid}}","exp":"2099-12-31"}""";

        var headerBase64 = Base64UrlEncode(headerJson);
        var payloadBase64 = Base64UrlEncode(payloadJson);
        var dataToSign = $"{headerBase64}.{payloadBase64}";

        var sigBytes = rsa.SignData(Encoding.UTF8.GetBytes(dataToSign), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var sigBase64Url = Base64UrlEncodeBytes(sigBytes);
        var licenseKey = $"{dataToSign}.{sigBase64Url}";

        var dirs = new[]
        {
            AppDomain.CurrentDomain.BaseDirectory,
            Directory.GetCurrentDirectory(),
            Path.GetDirectoryName(typeof(LicenseService).Assembly.Location)
        }.Where(d => !string.IsNullOrWhiteSpace(d)).Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var dir in dirs)
        {
            if (Directory.Exists(dir))
            {
                File.WriteAllText(Path.Combine(dir, "TAKey.key"), pubKeyPem);
                File.WriteAllText(Path.Combine(dir, "TAKeyData.key"), licenseKey);
            }
        }

        var valType = typeof(LicenseService).Assembly.GetType("Shared.Core.Security.LicenseValidator");
        if (valType != null)
        {
            foreach (var f in valType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (!f.IsLiteral && !f.IsInitOnly)
                    f.SetValue(null, null);
            }

            var initMethod = valType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            initMethod?.Invoke(null, null);
        }

        static string Base64UrlEncodeBytes(byte[] input) =>
            Convert.ToBase64String(input).Replace("+", "-").Replace("/", "_").Replace("=", "");

        static string Base64UrlEncode(string input) =>
            Base64UrlEncodeBytes(Encoding.UTF8.GetBytes(input));
    }

    private static void GuardAllConnectionsLocal(IServiceProvider sp)
    {
        var sqlSugarClient = sp.GetService<ISqlSugarClient>();
        var configIds = sqlSugarClient?.AsTenant()?.GetCurrentConfigIds();
        if (configIds != null)
        {
            foreach (var cid in configIds)
            {
                var connStr = sqlSugarClient!.AsTenant().GetConnectionScope(cid)?.CurrentConnectionConfig?.ConnectionString;
                if (!string.IsNullOrWhiteSpace(connStr))
                    GuardSqlConnectionIsLocal(connStr, $"SqlSugar DB Config '{cid}'");
            }
        }

        var cacheProvider = sp.GetService<ICacheProvider>();
        if (cacheProvider?.Cache is FullRedis fullRedis && !string.IsNullOrWhiteSpace(fullRedis.Server))
            GuardNetworkHostIsLocal(fullRedis.Server, "DI FullRedis Server");

        var cacheOptions = sp.GetService<IOptions<CacheOptions>>()?.Value;
        if (!string.IsNullOrWhiteSpace(cacheOptions?.Redis?.Configuration))
            GuardNetworkHostIsLocal(cacheOptions.Redis.Configuration, "DI CacheOptions.Redis");

        var config = sp.GetRequiredService<IConfiguration>();
        foreach (var kv in config.AsEnumerable())
        {
            if (string.IsNullOrWhiteSpace(kv.Value))
                continue;

            if (IsSqlConnectionKey(kv.Key, kv.Value))
                GuardSqlConnectionIsLocal(kv.Value, $"IConfiguration Key '{kv.Key}'");
            else if (IsNetworkEndpointKey(kv.Key, kv.Value))
                GuardNetworkHostIsLocal(kv.Value, $"IConfiguration Key '{kv.Key}'");
        }
    }

    private static bool IsSqlConnectionKey(string key, string value) =>
        key.StartsWith("ConnectionStrings:", StringComparison.OrdinalIgnoreCase)
        || (key.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase)
            && (value.Contains("Server=", StringComparison.OrdinalIgnoreCase)
                || value.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)));

    private static bool IsNetworkEndpointKey(string key, string value) =>
        key.Contains("Url", StringComparison.OrdinalIgnoreCase)
        || (key.Contains("Redis", StringComparison.OrdinalIgnoreCase) && key.Contains("Configuration", StringComparison.OrdinalIgnoreCase))
        || value.StartsWith("nats://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("redis://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("ws://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("wss://", StringComparison.OrdinalIgnoreCase)
        ;

    private static void GuardSqlConnectionIsLocal(string? connectionString, string targetName)
    {
        EnsureNotEmpty(connectionString, targetName);

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"CHẶN NGUY HIỂM: Chuỗi kết nối {targetName} không đúng định dạng SQL Server hợp lệ! Lỗi: {ex.Message}. Raw: {connectionString}", ex);
        }

        var rawServer = builder.DataSource;
        var serverHost = rawServer.Split(',', ';', '\\', ':')[0].Trim();
        EnsureHostIsLocal(serverHost, connectionString!, targetName);

        if (!TestDatabaseName.Equals(builder.InitialCatalog, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"CHẶN NGUY HIỂM: Database '{builder.InitialCatalog}' trong chuỗi kết nối không phải '{TestDatabaseName}'. Dừng ngay lập tức! Raw: {connectionString}");
    }

    private static void GuardNetworkHostIsLocal(string? value, string targetName)
    {
        EnsureNotEmpty(value, targetName);

        var host = Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri.Host
            : value!.Split(':', '/', ',')[0].Trim();

        EnsureHostIsLocal(host, value!, targetName);
    }

    private static void EnsureNotEmpty(string? value, string targetName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"CHẶN NGUY HIỂM: Chuỗi kết nối {targetName} rỗng! Dừng ngay lập tức.");
    }

    private static void EnsureHostIsLocal(string host, string rawValue, string targetName)
    {
        if (!AllowedLocalHosts.Any(h => h.Equals(host, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"CHẶN NGUY HIỂM: Host '{host}' trong {targetName} không phải localhost/127.0.0.1! Dừng ngay lập tức! Raw: {rawValue}");
    }

    private static Type[] GetSugarEntityTypes() =>
        App.EffectiveTypes
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass && t.IsDefined(typeof(SugarTable), inherit: false))
            .ToArray();
}
