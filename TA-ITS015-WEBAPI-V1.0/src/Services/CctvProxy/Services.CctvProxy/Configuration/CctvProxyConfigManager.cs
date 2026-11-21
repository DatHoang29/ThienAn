using Microsoft.Extensions.Configuration;
using Services.CctvProxy.Configuration.Options;
using Services.Shared.Configuration;
using Services.Shared.Logging;
using Services.Shared.Persistence;

namespace Services.CctvProxy.Configuration;

/// <summary>
/// Author: kien.le
/// Description: Singleton facade cấu hình Cctv Proxy. Pipeline load:
/// 1. LoadFromFile — bind appsettings.json (section "Service").
/// 2. Typed load — section Cctv đọc từ SysOpConfig qua IConfigLoader&lt;CctvOptions&gt;.
///    Gọi loader TRỰC TIẾP (không qua IConfigLoaderRegistry vì registry nuốt lỗi và trả DefaultValue):
///    DB lỗi thì giữ Cctv đang dùng (last-known-good); chỉ dùng giá trị trong file khi chưa từng nạp được từ DB.
/// Reload lỗi giữ nguyên snapshot cũ (last-known-good).
/// Created date: 06/10/2026
/// </summary>
public sealed class CctvProxyConfigManager : IConfigManager, IDisposable
{
    private const string SectionName = "Service";

    private readonly IConfiguration _configuration;
    private readonly DatabaseManager _dbFactory;
    private readonly IConfigLoader<CctvOptions> _cctvLoader;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    private ServiceConfig _current;
    private bool _cctvLoadedFromDb;

    public CctvProxyConfigManager(
        IConfiguration configuration,
        DatabaseManager dbFactory,
        IConfigLoader<CctvOptions> cctvLoader)
    {
        _configuration = configuration;
        _dbFactory = dbFactory;
        _cctvLoader = cctvLoader;
        _current = LoadFromFile();
        Validate(_current);
    }

    public ServiceConfig Current => Volatile.Read(ref _current);

    /// <inheritdoc/>
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        await _reloadLock.WaitAsync(cancellationToken);
        try
        {
            var next = LoadFromFile();
            next.Cctv = await LoadCctvAsync(cancellationToken);

            Validate(next);
            Volatile.Write(ref _current, next);

            LogManager.Info(
                $"Configuration reloaded for {next.Name} (NxVMS: {next.Cctv.BaseUrl})",
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

    /// <summary>
    /// Nạp Cctv từ SysOpConfig. Lỗi DB (mất kết nối, timeout...) không được làm mất BaseUrl/TicketSecret
    /// đang chạy — nếu không, mọi kết nối mới bị từ chối cho tới lần reload thành công.
    /// </summary>
    private async Task<CctvOptions> LoadCctvAsync(CancellationToken cancellationToken)
    {
        try
        {
            var loaded = await _cctvLoader.LoadAsync(cancellationToken);
            _cctvLoadedFromDb = true;
            return loaded;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var keepCurrent = _cctvLoadedFromDb;
            LogManager.Error(
                exception,
                keepCurrent
                    ? "Không đọc được SysOpConfig (cctv_config); giữ cấu hình NxVMS đang dùng"
                    : "Không đọc được SysOpConfig (cctv_config); dùng cấu hình Service:Cctv trong file",
                kind: LogKind.Config);
            return keepCurrent ? Current.Cctv : _cctvLoader.DefaultValue;
        }
    }

    private ServiceConfig LoadFromFile() =>
        _configuration
            .GetRequiredSection(SectionName)
            .Get<ServiceConfig>()
        ?? throw new InvalidOperationException("Service configuration section is required.");

    private void Validate(ServiceConfig options)
    {
        if (string.IsNullOrWhiteSpace(options.Name))
            throw new InvalidOperationException("Service:Name is required.");

        if (!_dbFactory.Contains(options.DataConnectionId))
            throw new InvalidOperationException(
                $"Data connection '{options.DataConnectionId}' is not configured.");
    }
}
