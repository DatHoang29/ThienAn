using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Shared.Utility.Resilience;

/// <summary>
/// Description: "Cổng" báo CSDL đã sẵn sàng. Mở MỘT lần sau khi app khởi động xong
///              (ApplicationStarted) và ping được các kết nối SqlServer trong Database.json.
///
///              VÌ SAO CẦN: lúc khởi động, Hangfire server + các hosted service + worker cùng
///              chạm DB một lúc. SqlSugarScope giữ connection theo AsyncLocal; luồng nào đã
///              chạm DB rồi mới tách task con thì các task con DÙNG CHUNG một SqlConnection
///              -> "The connection was not closed. The connection's current state is connecting".
///              Gom mọi việc nền về sau cổng này giúp tránh cơn dồn DB lúc khởi động.
///
///              CÁCH DÙNG: trong BackgroundService.ExecuteAsync -> <c>await gate.WaitAsync(ct);</c>
///              KHÔNG await trong IHostedService.StartAsync (cổng chỉ mở sau khi mọi StartAsync
///              chạy xong -> sẽ deadlock).
/// Created date: 06/10/2026
/// </summary>
public sealed class DbReadyGate
{
    private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsReady => _tcs.Task.IsCompleted;

    public Task WaitAsync(CancellationToken cancellationToken = default) => _tcs.Task.WaitAsync(cancellationToken);

    public void MarkReady() => _tcs.TrySetResult();
}

/// <summary>
/// Description: Chờ app khởi động xong, ping lần lượt các kết nối SqlServer (mỗi kết nối thử tối đa
///              <see cref="MaxAttempts"/> lần, cách nhau <see cref="RetryDelay"/>), rồi mở <see cref="DbReadyGate"/>.
///              Kết nối không phải SqlServer (vd QuestDB "VdsDefault") bỏ qua — đã có QuestAvailabilityGate riêng.
///              Hết lượt thử vẫn mở cổng (chỉ log lỗi) để không treo các job/worker vĩnh viễn.
/// Created date: 06/10/2026
/// </summary>
public sealed class DbStartupGateService : BackgroundService
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    private readonly DbReadyGate _gate;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbStartupGateService> _logger;

    public DbStartupGateService(DbReadyGate gate, IHostApplicationLifetime lifetime,
        IConfiguration configuration, ILogger<DbStartupGateService> logger)
    {
        _gate = gate;
        _lifetime = lifetime;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // 1. Chờ Kestrel + mọi IHostedService.StartAsync chạy xong
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using (_lifetime.ApplicationStarted.Register(() => started.TrySetResult()))
            {
                await started.Task.WaitAsync(stoppingToken);
            }

            // 2. Ping từng kết nối SqlServer
            foreach (var configId in GetSqlServerConfigIds())
            {
                await PingWithRetryAsync(configId, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DbReadyGate] Lỗi không mong muốn khi kiểm tra CSDL — vẫn mở cổng.");
        }

        _gate.MarkReady();
        _logger.LogInformation("[DbReadyGate] CSDL sẵn sàng — mở cổng cho job/worker.");
    }

    private List<string> GetSqlServerConfigIds()
    {
        return _configuration.GetSection("DbConnection:ConnectionConfigs").GetChildren()
            .Where(c => string.Equals(c["DbType"], "SqlServer", StringComparison.OrdinalIgnoreCase))
            .Select(c => c["ConfigId"])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToList();
    }

    private async Task PingWithRetryAsync(string configId, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                Task ping;
                // Chạy ping trong flow RIÊNG + client RIÊNG (CopyNew) để không để lại connection
                // trong AsyncLocal của luồng đang gọi.
                using (ExecutionContext.SuppressFlow())
                {
                    ping = Task.Run(async () =>
                    {
                        using var db = SqlSugarSetup.ITenant.GetConnectionScope(configId).CopyNew();
                        await db.Ado.GetScalarAsync("SELECT 1");
                    }, ct);
                }
                await ping;
                _logger.LogInformation("[DbReadyGate] Kết nối {ConfigId} OK.", configId);
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (attempt == MaxAttempts)
                {
                    _logger.LogError(ex, "[DbReadyGate] Kết nối {ConfigId} vẫn lỗi sau {N} lần thử — bỏ qua.", configId, MaxAttempts);
                    return;
                }
                _logger.LogWarning("[DbReadyGate] Kết nối {ConfigId} chưa sẵn sàng (lần {Attempt}/{Max}): {Msg}",
                    configId, attempt, MaxAttempts, ex.Message);
                await Task.Delay(RetryDelay, ct);
            }
        }
    }
}
