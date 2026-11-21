using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Modules.VDS.Core.Media;
using Modules.VDS.Core.Entities;
using Modules.VDS.Core.Constants;
using Services.Shared.Configuration;
using Services.Shared.Persistence;
using Services.Vds.Configuration;
using Services.Vds.Features.Media;

// A targeted live check: SELECT the existing configuration, then exercise private state IO.
// Never start a host/pipeline, modify SQL incidents, print secrets, or consume pending frames.
if (args.Length is < 1 or > 2 || args.Length == 2 && args[1] is not ("config-only" or "process-identity" or "configured-state"))
{ Console.WriteLine("Supply a VDS host directory and optional config-only, process-identity or configured-state mode."); return 2; }
var phase = "configuration";
try
{
    var hostDirectory = Path.GetFullPath(args[0]);
    var hostEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
    var configuration = new ConfigurationBuilder().SetBasePath(hostDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile("appsettings." + hostEnvironment + ".json", optional: true)
        .AddJsonFile("Configuration/Database.json", optional: false)
        .AddJsonFile("Configuration/Database." + hostEnvironment + ".json", optional: true)
        .AddEnvironmentVariables().Build();
    var dbOptions = configuration.GetSection("DbConnection").Get<DbConnectionOptions>()
        ?? throw new InvalidOperationException("Missing database configuration.");
    var service = configuration.GetSection(ServiceConfig.SectionName).Get<ServiceConfig>()
        ?? throw new InvalidOperationException("Missing service configuration.");
    var imageOptions = configuration.GetSection(VdsRegistrationImageOptions.Section).Get<VdsRegistrationImageOptions>()
        ?? throw new InvalidOperationException("Missing image-store configuration.");
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(25));
    using var databases = new DatabaseManager(Options.Create(dbOptions));
    using var manager = new VdsConfigManager(service, NullLogger<VdsConfigManager>.Instance);
    phase = "SQL configuration read";
    var settings = await new VdsImageShareSettingsRepository(databases, manager).ReadAsync(timeout.Token);
    Console.WriteLine("Configuration loaded; usernameConfigured=" + !string.IsNullOrWhiteSpace(settings.UserName)
        + "; separateDomainConfigured=" + !string.IsNullOrWhiteSpace(settings.Domain)
        + "; sameVdsShare=" + string.Equals(VdsImageShareAccess.UncShare(settings.BasePath),
            VdsImageShareAccess.UncShare(imageOptions.StateDirectory), StringComparison.OrdinalIgnoreCase));
    var configuredShare = VdsImageShareAccess.UncShare(settings.BasePath);
    Console.WriteLine("VDS image root kind=" + (configuredShare is not null
        ? "UNC " + configuredShare : "non-UNC") + "; state root share=" + VdsImageShareAccess.UncShare(imageOptions.StateDirectory)
        + "; baseline media share=" + VdsImageShareAccess.UncShare(service.Media.StorageDirectory));
    var mediaSetting = await databases.Get(service.DataConnectionId).Queryable<VdsConfig>()
        .Where(x => x.TenantId == service.TenantId && x.IsDelete == null
            && x.Status == Shared.DTO.Enums.BaseEnums.StatusEnum.Enable
            && x.GroupCode == VdsConfigKeys.Group.Config && x.Code == VdsConfigKeys.Code.MediaStorageDirectory)
        .Select(x => x.Value).FirstAsync(timeout.Token);
    Console.WriteLine("Configured image base path=" + (configuredShare is not null ? settings.BasePath : "non-UNC")
        + "; tenant media share=" + VdsImageShareAccess.UncShare(mediaSetting));
    if (args.Length == 2 && args[1] == "config-only") return 0;
    // Unique child only. No locks/state belonging to actual incidents are touched.
    var stateRoot = args.Length == 2 && args[1] == "configured-state"
        ? Path.Combine(settings.BasePath ?? throw new InvalidOperationException("Missing configured image root."), "IncidentImageState")
        : imageOptions.StateDirectory;
    var probeRoot = Path.Combine(Path.GetFullPath(stateRoot), "probe-" + Guid.NewGuid().ToString("N"));
    var processIdentity = args.Length == 2 && args[1] == "process-identity";
    var access = new VdsImageShareAccess(_ => Task.FromResult(processIdentity
        ? new VdsImageShareSettings(settings.BasePath, null, null, null) : settings));
    var store = new VdsRegistrationImageStore(new() { StateDirectory = probeRoot }, access);
    var created = false;
    try
    {
        phase = "SMB register/lock/write/read";
        await access.RunAsync(probeRoot, () =>
        {
            Directory.CreateDirectory(probeRoot);
            created = true;
            return Task.FromResult(true);
        }, timeout.Token);
        var now = DateTime.UtcNow;
        await store.RegisterAsync("probe", "probe", now, now,
            new VdsRegistrationImageSource(0, now, "probe", null, null), timeout.Token);
        var state = await store.ReadAsync("probe", "probe", timeout.Token);
        if (state?.Status != "WaitingForSource") throw new IOException("Probe state mismatch.");
        await using (await store.LockAsync("probe", "probe", timeout.Token))
            await store.SaveAsync(state with { Status = "MissingSource" }, timeout.Token);
        if ((await store.ReadAsync("probe", "probe", timeout.Token))?.Status != "MissingSource")
            throw new IOException("Probe completion mismatch.");
        Console.WriteLine("PASS: live SMB registration, lock, read, write and pending-to-completed state.");
    }
    finally
    {
        if (created)
        {
            phase = "SMB probe cleanup";
            // Checked construction: only this run's unique child underneath StateDirectory may be deleted.
            await access.RunAsync(probeRoot, () =>
            {
                if (Directory.Exists(probeRoot)) Directory.Delete(probeRoot, recursive: true);
                return Task.FromResult(true);
            }, CancellationToken.None);
        }
    }
    Console.WriteLine("PASS: unique probe state removed; actual incidents and recovery queue untouched.");
    return 0;
}
catch (Exception ex)
{
    // Exception messages/stack traces from SQL libraries can contain connection values. Emit codes only.
    var cause = ex.GetBaseException();
    Console.WriteLine("FAIL: " + phase + "; type=" + ex.GetType().Name + "; hresult=" + ex.HResult
        + "; causeHresult=" + cause.HResult
        + (cause is System.ComponentModel.Win32Exception win32 ? "; windowsError=" + win32.NativeErrorCode : ""));
    return 1;
}
