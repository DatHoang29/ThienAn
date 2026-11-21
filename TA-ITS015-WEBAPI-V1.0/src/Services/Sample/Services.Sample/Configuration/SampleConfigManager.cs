using Microsoft.Extensions.Configuration;
using Services.Sample.Configuration.Options;
using Services.Shared.Configuration;
using Services.Shared.Logging;
using Services.Shared.Persistence;

namespace Services.Sample.Configuration;

/// <summary>
/// Author: TAC Team
/// Description: Singleton facade cấu hình Sample. Pipeline load:
/// 1. LoadFromFile — bind appsettings.json (base)
/// 2. Typed loads — mỗi section (Queue/Api/Devices) do một IConfigLoader&lt;T&gt; đọc từ file
///    và trả đúng typed class qua IConfigLoaderRegistry.
/// 3. Fallback — loader nào lỗi tự trả DefaultValue (từ file) cho section đó.
/// Validate tối thiểu: check Name + DataConnectionId. Service tự guard runtime khi dùng config.
/// Created date: 16/07/2026
/// </summary>
public sealed class SampleConfigManager : IConfigManager, IDisposable
{
    private const string SectionName = "Service";

    private readonly IConfiguration _configuration;
    private readonly DatabaseManager _dbFactory;
    private readonly IConfigLoaderRegistry _configLoaders;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    private ServiceConfig _current;

    public SampleConfigManager(
        IConfiguration configuration,
        DatabaseManager dbFactory,
        IConfigLoaderRegistry configLoaders)
    {
        _configuration = configuration;
        _dbFactory = dbFactory;
        _configLoaders = configLoaders;
        _current = LoadFromFile();
        Validate(_current);
    }

    public ServiceConfig Current => Volatile.Read(ref _current);

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await _reloadLock.WaitAsync(cancellationToken);
        try
        {
            // 1. Base: bind từ appsettings.json
            var next = LoadFromFile();

            // 2. Typed loads — mỗi section do loader riêng đọc từ file.
            next.Queue = await _configLoaders.LoadAsync<QueueOptions>(cancellationToken);
            next.Api = await _configLoaders.LoadAsync<ApiOptions>(cancellationToken);
            next.Devices = await _configLoaders.LoadAsync<DevicesOptions>(cancellationToken);

            Validate(next);
            Volatile.Write(ref _current, next);

            LogManager.Info(
                $"Configuration reloaded for {next.Name} (typed loaders applied)",
                kind: LogKind.Config);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogManager.Error(
                exception,
                "Configuration reload failed; keeping last-known-good snapshot",
                kind: LogKind.Config);
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    public void Dispose() => _reloadLock.Dispose();

    // === Pipeline steps ===

    private ServiceConfig LoadFromFile() =>
        _configuration
            .GetRequiredSection(SectionName)
            .Get<ServiceConfig>()
        ?? throw new InvalidOperationException("Service configuration section is required.");

    // === Validate tối thiểu ===

    private void Validate(ServiceConfig options)
    {
        if (string.IsNullOrWhiteSpace(options.Name))
            throw new InvalidOperationException("Service:Name is required.");

        if (!_dbFactory.Contains(options.DataConnectionId))
            throw new InvalidOperationException(
                $"Data connection '{options.DataConnectionId}' is not configured.");
    }
}
