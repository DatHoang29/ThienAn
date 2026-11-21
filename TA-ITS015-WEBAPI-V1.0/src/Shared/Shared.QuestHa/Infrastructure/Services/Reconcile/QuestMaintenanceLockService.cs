using System.Data;
using Microsoft.Data.SqlClient;

namespace Shared.QuestHa.Infrastructure.Services.Reconcile;

/// <summary>
/// Author: KienLT
/// Description: Khoá vận hành theo DB journal giữa các tiến trình/máy; mất phiên SQL thì huỷ lượt đang chạy.
/// Created date: 27/10/2026
/// </summary>
internal sealed class QuestMaintenanceLockService(string connectionString, string resource = "QuestHa:NodeRepair")
{
    // Tên applock: vá node dùng mặc định; bản xuất Parquet dùng khoá riêng để Worker và CLI không xuất trùng.
    private string Resource => resource;

    /// <summary>Chạy một lượt vá độc quyền; không chờ khi đã có người giữ khoá. Không khởi chạy replicator.</summary>
    public Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct) =>
        RunAsync(work, ct, () => throw new InvalidOperationException("QuestHa maintenance is already running or the lock could not be acquired."));

    /// <summary>Như <see cref="RunAsync{T}(Func{CancellationToken, Task{T}}, CancellationToken)"/>; khoá đang bị giữ ⇒ trả <paramref name="whenBusy"/> thay vì ném.</summary>
    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct, Func<T> whenBusy)
    {
        // Không pooling/reconnect: khoá Session phải mất cùng đúng phiên kết nối này.
        var builder = new SqlConnectionStringBuilder(connectionString) { Pooling = false, ConnectRetryCount = 0 };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);
        await using var acquire = connection.CreateCommand();
        acquire.CommandText = "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@name, @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=0; SELECT @r;";
        acquire.Parameters.AddWithValue("@name", Resource);
        var code = Convert.ToInt32(await acquire.ExecuteScalarAsync(ct).ConfigureAwait(false));
        if (code < 0) return whenBusy();

        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connection.StateChange += (_, e) =>
        {
            if (e.CurrentState is ConnectionState.Broken or ConnectionState.Closed) lifetime.Cancel();
        };
        var monitor = MonitorAsync(connection, lifetime);
        try
        {
            var result = await work(lifetime.Token).ConfigureAwait(false);
            lifetime.Token.ThrowIfCancellationRequested();
            return result;
        }
        finally
        {
            lifetime.Cancel();
            await monitor.ConfigureAwait(false);
            // Đóng phiên giải phóng applock, kể cả khi command/caller lỗi hoặc bị huỷ.
            await connection.CloseAsync().ConfigureAwait(false);
        }
    }

    private async Task MonitorAsync(SqlConnection connection, CancellationTokenSource lifetime)
    {
        try
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), lifetime.Token).ConfigureAwait(false);
                await using var command = connection.CreateCommand();
                command.CommandTimeout = 2;
                command.CommandText = "SELECT APPLOCK_MODE('public', @name, 'Session');";
                command.Parameters.AddWithValue("@name", Resource);
                if (!Equals(await command.ExecuteScalarAsync(lifetime.Token).ConfigureAwait(false), "Exclusive"))
                    throw new InvalidOperationException("Maintenance lock was lost.");
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch { lifetime.Cancel(); }
    }
}
