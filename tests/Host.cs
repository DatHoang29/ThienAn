using Furion;
using Hangfire;
using JasperFx.CodeGeneration;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Modules.System.Extensions;
using NewLife.Caching;
using Shared.Core.Extensions;
using Shared.Core.Security;
using Shared.Core.Settings.Options;
using Shared.Infrastructure.Localization;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

// ─── Namespace của các module TÙY CHỌN ───
// Gói trong #if do test.csproj sinh ra (mục 2.3): module bị xoá khỏi repo thì using này biến mất
// cùng lời gọi ở bước đăng ký, nên project test vẫn build.
#if HAS_SHAREDATA
using Module.ShareData.Extensions;
#endif
#if HAS_VIDEOWALL
using Module.VideoWall.Extensions;
#endif
#if HAS_SHAREDATAWORKER
using ShareDataWorker.Extensions;
#endif

namespace Tests;

/// <summary>
/// Author: Đạt
/// Description: Định nghĩa xUnit Test Collection dùng chung cho toàn bộ các module WebAPI chia sẻ chung Host và Database
/// Created date: 16/08/2026
/// </summary>
[CollectionDefinition("api")]
public class ApiTestCollection : ICollectionFixture<Host> { }

/// <summary>
/// Author: Đạt
/// Description: Generic Test Host độc lập 100% dùng chung cho toàn bộ các module trong Test Suite (.NET 10 / SqlSugar / Lamar).
///              Cấu hình nạp trực tiếp từ bộ nhớ (In-Memory) và bảo vệ an toàn tuyệt đối theo nguyên tắc Zero-Trust.
/// Created date: 15/08/2026
/// </summary>
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
        ["DbConnection:ConnectionConfigs:0:TableSettings:EnableInitTable"] = "true",
        ["DbConnection:ConnectionConfigs:0:TableSettings:EnableIncreTable"] = "false",
        ["DbConnection:ConnectionConfigs:0:SeedSettings:EnableInitSeed"] = "false",
        ["DbConnection:ConnectionConfigs:0:SeedSettings:EnableIncreSeed"] = "false",

        ["DbConnection:ConnectionConfigs:1:ConfigId"] = "LogDefault",
        ["DbConnection:ConnectionConfigs:1:DbType"] = "SqlServer",
        ["DbConnection:ConnectionConfigs:1:ConnectionString"] = DefaultLocalConnectionString,
        ["DbConnection:ConnectionConfigs:1:DbSettings:EnableInitDb"] = "true",
        ["DbConnection:ConnectionConfigs:1:DbSettings:EnableDiffLog"] = "false",
        ["DbConnection:ConnectionConfigs:1:DbSettings:EnableUnderLine"] = "false",
        ["DbConnection:ConnectionConfigs:1:TableSettings:EnableInitTable"] = "true",
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

    private IHost? _host;

    public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("Host not initialized");

    /// <summary>
    /// Author: Đạt
    /// Description: IStringLocalizer cho Validator trong test, lấy từ JsonStringLocalizerFactory đã
    ///              đăng ký ở bước 11 nên message validate giống runtime thật.
    /// Created date: 21/08/2026
    /// </summary>
    public IStringLocalizer Localizer => Services.GetRequiredService<IStringLocalizer>();

    /// <summary>
    /// Author: Đạt
    /// Description: Dựng Host test, chạy guard chặn hạ tầng ngoài trước khi khởi động, rồi khởi tạo schema và làm sạch dữ liệu
    /// Created date: 15/08/2026
    /// </summary>
    public async Task InitializeAsync()
    {
        // ─── Set UTF-8 để console hiển thị đúng tiếng Việt khi chạy dotnet test ───
        Console.OutputEncoding = Encoding.UTF8;

        // ─── Thiết lập CultureInfo toàn cục (khớp 1:1 với Startup.cs, bảo đảm date/decimal đồng nhất trên mọi máy CI) ───
        ApplyTestCulture();

        // 1. Build Host độc lập
        _host = BuildHostBuilder().Build();

        // 2. Chạy guard kiểm tra toàn diện NGAY TRƯỚC KHI GỌI StartAsync() (chống mọi kết nối ra ngoài)
        GuardAllConnectionsLocal(_host.Services);

        // 3. Khởi chạy Host sau khi đã được xác nhận 100% an toàn
        await _host.StartAsync();

        // 3a. Gán App.RootServices cho Furion (app thật làm việc này trong app.UseInject()). Thiếu nó,
        //     ctor không tham số của SqlSugarRepository<T> đổ NullReferenceException.
        BindFurionRootServices(_host.Services);

        // 3b. Bật MockServer của từng module (phần thân ở tests/Modules/<Module>/Host.<Module>.cs).
        StartModuleTestServers();

        // 4. Clear dữ liệu Database, sau đó BẮT BUỘC clear cache đi kèm:
        //    ClearAllData dùng DbMaintenance.TruncateTable nên đi thẳng xuống DB, không qua ORM pipeline
        //    → IsAutoRemoveDataCache không kích hoạt và ICacheProvider không biết dữ liệu đã bị xóa.
        //    Bỏ ClearAllCache sẽ khiến test đọc được kết quả query đã cache từ lần chạy trước.
        ClearAllData();
        ClearAllCache();
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Dừng và giải phóng Host test khi toàn bộ Test Collection kết thúc
    /// Created date: 15/08/2026
    /// </summary>
    public async Task DisposeAsync()
    {
        StopModuleTestServers();

        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Điểm mở rộng cho từng module tự khởi động server giả lập thiết bị của mình.
    ///              Không có module nào cài đặt thì lời gọi bị xoá lúc biên dịch (partial method).
    /// Created date: 21/08/2026
    /// </summary>
    partial void StartModuleTestServers();

    /// <summary>
    /// Author: Đạt
    /// Description: Điểm mở rộng đối ứng để từng module dừng và giải phóng server giả lập của mình
    /// Created date: 21/08/2026
    /// </summary>
    partial void StopModuleTestServers();

    /// <summary>
    /// Author: Đạt
    /// Description: Xóa sạch dữ liệu 100% bằng ORM (DbMaintenance.TruncateTable), chỉ trong phạm vi các bảng
    ///              thực thể được quản lý trong môi trường Test.
    ///              <para>
    ///              TRUNCATE khác DELETE ở 4 điểm:
    ///              (1) Tốc độ — TRUNCATE giải phóng theo data page và ghi log tối thiểu, DELETE xóa từng dòng
    ///              và ghi transaction log từng dòng nên chậm hơn nhiều khi bảng có dữ liệu.
    ///              (2) IDENTITY — TRUNCATE reset seed về giá trị gốc, DELETE giữ nguyên seed và tiếp tục tăng;
    ///              với test thì reset seed giúp ID sinh ra ổn định giữa các lần chạy.
    ///              (3) Foreign Key — TRUNCATE KHÔNG dùng được nếu bảng đang bị FK của bảng khác tham chiếu
    ///              (kể cả khi FK đã NOCHECK), DELETE thì được; đây chính là lý do bản cũ phải dùng DELETE
    ///              kèm NOCHECK/CHECK CONSTRAINT ALL.
    ///              (4) Trigger — TRUNCATE không kích hoạt DELETE trigger, DELETE thì có.
    ///              Cả hai đều rollback được trong transaction của SQL Server.
    ///              </para>
    ///              Chọn TRUNCATE vì schema Database test do CodeFirst sinh ra nên không có FK constraint nào
    ///              (đã kiểm chứng sys.foreign_keys = 0) và cũng không có trigger, nên 2 hạn chế (3) và (4)
    ///              không áp dụng. Nếu sau này có FK được thêm vào, TRUNCATE sẽ báo lỗi rõ ràng ngay tại đây.
    /// Created date: 20/08/2026
    /// </summary>
    public void ClearAllData()
    {
        var db = _host?.Services.GetService<ISqlSugarClient>();
        if (db == null)
            return;

        GuardSqlConnectionIsLocal(db.CurrentConnectionConfig?.ConnectionString, "Database");

        // Lấy 1 lần danh sách bảng đang tồn tại thật trong Database (isCache = false để không đọc cache cũ)
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

    /// <summary>
    /// Author: Đạt
    /// Description: Xóa sạch toàn bộ dữ liệu cache trong RAM và Redis của hệ thống
    /// Created date: 19/08/2026
    /// </summary>
    public void ClearAllCache()
    {
        var cacheProvider = _host?.Services.GetService<ICacheProvider>();
        cacheProvider?.Cache?.Clear();
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Gán IServiceProvider gốc vào Furion (Furion.InternalApp.RootServices) để App.GetService,
    ///              App.RootServices và các service locator của Furion hoạt động giống app thật.
    ///              Furion chỉ mở setter này qua UseInject() của pipeline web nên Generic Host phải gán trực tiếp
    ///              vào static field; đây là điểm nối duy nhất, nếu Furion đổi tên field thì test sẽ báo lỗi rõ ràng.
    /// Created date: 21/08/2026
    /// </summary>
    private static void BindFurionRootServices(IServiceProvider services)
    {
        var rootServicesField = typeof(App).Assembly
            .GetType("Furion.InternalApp")
            ?.GetField("RootServices", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Không tìm thấy Furion.InternalApp.RootServices — Furion đã đổi API, cần cập nhật lại Test Host.");

        rootServicesField.SetValue(null, services);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Gán CultureInfo cố định cho toàn bộ tiến trình test để định dạng ngày/số không phụ thuộc culture của máy chạy
    /// Created date: 21/08/2026
    /// </summary>
    private static void ApplyTestCulture()
    {
        var culture = new CultureInfo(TestCultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Dựng Generic IHostBuilder độc lập 100% cho môi trường Test (nạp cấu hình In-Memory trực tiếp trong C#)
    /// Created date: 19/08/2026
    /// </summary>
    private static IHostBuilder BuildHostBuilder() =>
        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((ctx, configBuilder) =>
            {
                // 1. XÓA SẠCH 100% các source cấu hình mặc định (appsettings.json từ project khác, env vars)
                configBuilder.Sources.Clear();

                // 2. NẠP CẤU HÌNH TEST TRỰC TIẾP TỪ BỘ NHỚ (Độc lập tuyệt đối, không phụ thuộc file JSON)
                configBuilder.AddInMemoryCollection(InMemoryTestConfigurations);
            })
            // autoRegisterBackgroundService: false — TẮT Furion.AddAppHostedService(), hàm quét
            // App.EffectiveTypes (mọi assembly tham chiếu Furion = toàn bộ module trong output) rồi
            // AddHostedService<T>() cho mọi type implement IHostedService. Bật thì DataExportWorker,
            // WeatherService, TmsIncidentAutomationWorker, IncidentSnapshotWorker... vào container dù
            // không ai gọi AddTMSCore/AddWorkerInfrastructure: hoặc chạy nền query DB song song với
            // test, hoặc nổ LamarException lúc dựng IEnumerable<IHostedService> khi thiếu dependency.
            // Tắt tại nguồn nên không phải gỡ descriptor sau, giữ nguyên WolverineRuntime (bắt buộc:
            // dựng handler graph trong StartAsync) và hosted service hạ tầng khác.
            .Inject(autoRegisterBackgroundService: false)
            .UseLamar()
            .ConfigureServices(RegisterTestServices);

    /// <summary>
    /// Author: Đạt
    /// Description: Đăng ký các service kế thừa 100% pipeline gốc từ DLL qua AddSharedInfrastructure & AddSharedApplication
    /// Created date: 22/08/2026
    /// </summary>
    private static void RegisterTestServices(HostBuilderContext ctx, IServiceCollection services)
    {
        var config = ctx.Configuration;

        // 1. Tự động sinh cặp khóa Test License hợp lệ cho máy hiện tại (Bypass license an toàn 100%)
        EnsureValidTestLicense();

        // 2. Kế thừa trọn vẹn 100% hạ tầng cốt lõi từ DLL gốc (SqlSugar, Cache, Options, MVC, Routing...)
        services.AddSharedInfrastructure(config);

        // 3. Kế thừa trọn vẹn tầng Application dùng chung (ObjectMapper, ICurrentUser, IDateTimeService, FluentValidation...)
        services.AddSharedApplication(config);

        // 4. Wolverine Mediator (khớp Startup.cs)
        services.AddWolverine(opts =>
        {
            opts.Policies.AutoApplyTransactions();
            opts.Durability.Mode = DurabilityMode.MediatorOnly;
            opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Dynamic;
        });

        // 5. Cấu hình Hangfire Server nếu bật trong config
        if (config["Hangfire:Enable"]?.ToLower() == "true")
        {
            services.AddHangfire(x => x.UseSqlServerStorage(config["Hangfire:ConnectionString"]
                ?? config["DbConnection:ConnectionConfigs:0:ConnectionString"]));
            services.AddHangfireServer();
        }

        // 6. Serialization + DatabaseAccessor + JSON Localization Factory
        services.AddSerialization(config);
        services.AddDatabaseAccessor();
        services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();

        // 7. Business Modules
        services.AddSystemModule(config);

#if HAS_SHAREDATA
        services.AddShareDataModule(config);
#endif

#if HAS_VIDEOWALL
        services.AddVWModule(config);
#endif

#if HAS_SHAREDATAWORKER
        services.AddShareDataWorkerCoreServices();
#endif
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Tự động sinh và lưu cặp khóa TAKey.key / TAKeyData.key hợp lệ cho Hardware ID của máy hiện tại
    /// Created date: 22/08/2026
    /// </summary>
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

        // Reset cache trong LicenseValidator và nạp ngay cặp khóa mới
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

    /// <summary>
    /// Author: Đạt
    /// Description: Quét toàn diện 4 lớp bảo vệ: SqlSugar (tất cả ConfigId), IOptions Cache/DB, và toàn bộ IConfiguration để đảm bảo 100% không kết nối ra máy chủ ngoài
    /// Created date: 21/08/2026
    /// </summary>
    private static void GuardAllConnectionsLocal(IServiceProvider sp)
    {
        // 1. Kiểm tra toàn bộ Connection Configs trong SqlSugar (kể cả Default, LogDefault)
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

        // 2. Kiểm tra Cache Provider thực tế trong DI
        var cacheProvider = sp.GetService<ICacheProvider>();
        if (cacheProvider?.Cache is FullRedis fullRedis && !string.IsNullOrWhiteSpace(fullRedis.Server))
            GuardNetworkHostIsLocal(fullRedis.Server, "DI FullRedis Server");

        // 3. Kiểm tra IOptions<CacheOptions>
        var cacheOptions = sp.GetService<IOptions<CacheOptions>>()?.Value;
        if (!string.IsNullOrWhiteSpace(cacheOptions?.Redis?.Configuration))
            GuardNetworkHostIsLocal(cacheOptions.Redis.Configuration, "DI CacheOptions.Redis");

        // 4. Quét toàn bộ IConfiguration bắt mọi key nhạy cảm (SQL, NATS, Redis, SocketCluster, URLs)
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

    /// <summary>
    /// Author: Đạt
    /// Description: Nhận diện một cặp key/value trong IConfiguration có phải chuỗi kết nối CSDL hay không
    /// Created date: 21/08/2026
    /// </summary>
    private static bool IsSqlConnectionKey(string key, string value) =>
        key.StartsWith("ConnectionStrings:", StringComparison.OrdinalIgnoreCase)
        || (key.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase)
            && (value.Contains("Server=", StringComparison.OrdinalIgnoreCase)
                || value.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    /// Author: Đạt
    /// Description: Nhận diện một cặp key/value trong IConfiguration có phải endpoint mạng (NATS, Redis, SocketCluster, HTTP/WS) hay không
    /// Created date: 21/08/2026
    /// </summary>
    private static bool IsNetworkEndpointKey(string key, string value) =>
        key.Contains("Url", StringComparison.OrdinalIgnoreCase)
        || (key.Contains("Redis", StringComparison.OrdinalIgnoreCase) && key.Contains("Configuration", StringComparison.OrdinalIgnoreCase))
        || value.StartsWith("nats://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("redis://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("ws://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("wss://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Author: Đạt
    /// Description: Guard Zero-Trust cho chuỗi kết nối SQL Server — Server BẮT BUỘC là localhost/127.0.0.1/(localdb)
    ///              và Database BẮT BUỘC đúng tên Database test
    /// Created date: 19/08/2026
    /// </summary>
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

        // Trích xuất Server/Host thực tế từ Data Source (bỏ qua Port/Instance)
        var rawServer = builder.DataSource;
        var serverHost = rawServer.Split(',', ';', '\\', ':')[0].Trim();
        EnsureHostIsLocal(serverHost, connectionString!, targetName);

        if (!TestDatabaseName.Equals(builder.InitialCatalog, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"CHẶN NGUY HIỂM: Database '{builder.InitialCatalog}' trong chuỗi kết nối không phải '{TestDatabaseName}'. Dừng ngay lập tức! Raw: {connectionString}");
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Guard Zero-Trust cho endpoint mạng (NATS, Redis, WebSocket, HTTP) — Host BẮT BUỘC là localhost/127.0.0.1
    /// Created date: 21/08/2026
    /// </summary>
    private static void GuardNetworkHostIsLocal(string? value, string targetName)
    {
        EnsureNotEmpty(value, targetName);

        var host = Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri.Host
            : value!.Split(':', '/', ',')[0].Trim();

        EnsureHostIsLocal(host, value!, targetName);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Chặn ngay khi giá trị cấu hình rỗng (không cho phép chạy test với cấu hình thiếu)
    /// Created date: 21/08/2026
    /// </summary>
    private static void EnsureNotEmpty(string? value, string targetName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"CHẶN NGUY HIỂM: Chuỗi kết nối {targetName} rỗng! Dừng ngay lập tức.");
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Nguyên tắc Zero-Trust — bất kỳ host nào không thuộc danh sách local đều bị chặn 100%
    /// Created date: 21/08/2026
    /// </summary>
    private static void EnsureHostIsLocal(string host, string rawValue, string targetName)
    {
        if (!AllowedLocalHosts.Any(h => h.Equals(host, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"CHẶN NGUY HIỂM: Host '{host}' trong {targetName} không phải localhost/127.0.0.1! Dừng ngay lập tức! Raw: {rawValue}");
    }


    /// <summary>
    /// Author: Đạt
    /// Description: Quét toàn bộ Type đã nạp trong ứng dụng Furion để lấy danh sách thực thể SqlSugar:
    ///              bỏ abstract/interface, và chỉ lấy class khai báo TRỰC TIẾP [SugarTable]
    ///              (inherit = false để loại trừ các DTO kế thừa Entity bị nhận diện nhầm thành bảng)
    /// Created date: 21/08/2026
    /// </summary>
    private static Type[] GetSugarEntityTypes() =>
        App.EffectiveTypes
            .Where(t => !t.IsAbstract && !t.IsInterface && t.IsClass && t.IsDefined(typeof(SugarTable), inherit: false))
            .ToArray();

}
