using Npgsql;

namespace Modules.VDS.Infrastructure.Services.Persistence.Quest;

/// <summary>Use the cancellable PgWire handshake before SqlSugar's synchronous connection check.</summary>
internal static class QuestConnectionProbe
{
    private static readonly SemaphoreSlim Mutex = new(1, 1);
    private static DateTime _verifiedUntil;
    private static string? _verifiedConnection;

    public static async Task EnsureAsync(string connectionString)
    {
        if (!await Mutex.WaitAsync(TimeSpan.FromSeconds(6)))
            throw new QuestDbUnavailableException("VdsDefault", TimeSpan.FromSeconds(5), 0);
        try
        {
            if (_verifiedConnection == connectionString && DateTime.UtcNow < _verifiedUntil) return;
            var builder = new NpgsqlConnectionStringBuilder(connectionString) { Timeout = 5, Pooling = false };
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            try { await connection.OpenAsync(deadline.Token); }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested)
            { throw new TimeoutException("QuestDB connection probe timed out."); }
            _verifiedConnection = connectionString;
            _verifiedUntil = DateTime.UtcNow.AddSeconds(2);
        }
        finally { Mutex.Release(); }
    }
}
