using Services.Shared.Persistence;
using Services.Vds.Configuration;
using Shared.QuestHa;
using Shared.QuestHa.Enums;
using Shared.QuestHa.Exceptions;

namespace Services.Vds.Shared.Persistence;

/// <summary>
/// Author: KienLT
/// Description: Base của mọi repository đọc/ghi QuestDB.
///
/// Hai việc nó làm: chỉ cho <see cref="SqlSugarAccess"/> biết phải lấy connection nào
/// (<c>Service:QuestConnectionId</c>) và giữ sẵn hai giá trị đóng dấu bản ghi; và bọc mọi lượt gọi qua
/// cầu dao <see cref="QuestAvailabilityGate"/> bằng <see cref="GuardAsync{T}"/>.
///
/// <para><b>Mọi method chạm QuestDB PHẢI đi qua <c>GuardAsync</c>.</b> Gọi thẳng <c>Client</c> thì khi máy
/// chủ không tới được, lượt gọi giữ luồng vài phút và không huỷ được — lý do đầy đủ ghi ở
/// <see cref="QuestAvailabilityGate"/>.</para>
///
/// KHÔNG có <c>InsertIdempotentAsync</c> như <c>VdsRepositoryBase</c> của bản cũ. Lý do: hàm đó chống
/// trùng bằng cách insert rồi BẮT LỖI trùng khoá của SQL Server (mã 2601/2627). QuestDB không ném lỗi
/// trùng — nó GHI ĐÈ theo <c>DEDUP UPSERT KEYS</c> khai lúc <c>CREATE TABLE</c>. Nên ở đây không có gì
/// để bắt, và cũng không cần: chống trùng đã do tầng bảng lo.
/// Created date: 24/08/2026
/// </summary>
public abstract class QuestRepositoryBase : SqlSugarAccess
{
    private readonly IVdsConfigManager _config;
    private readonly QuestAvailabilityGate _gate;
    // KienLT - 31/10/2026: đọc/ghi HA qua QuestHaManager (cửa vào duy nhất của Shared.QuestHa); null = Direct/Single, giữ đường cũ.
    private readonly QuestHaManager? _questHa;

    protected QuestRepositoryBase(
        DatabaseManager databaseManager, IVdsConfigManager config, QuestAvailabilityGate gate, QuestHaManager? questHa = null)
        // Truyền LAMBDA chứ không truyền chuỗi: SqlSugarAccess gọi lại mỗi lần cần connection, nên đổi
        // QuestConnectionId lúc đang chạy là có hiệu lực ngay — không phải restart, không phải dựng lại
        // repository.
        : base(databaseManager, () => config.Current.QuestConnectionId)
    {
        _config = config;
        _gate = gate;
        _questHa = questHa;
    }

    /// <summary>
    /// Đọc từ snapshot mỗi lần dùng thay vì chốt lúc khởi tạo — repository là singleton, còn cấu hình
    /// đổi được lúc chạy; chốt lúc khởi tạo thì đổi <c>TenantId</c> sẽ không có tác dụng cho tới khi restart.
    /// </summary>
    protected string TenantId => _config.Current.TenantId;

    protected string ServiceUserId => _config.Current.ServiceUserId;

    /// <summary>
    /// Đóng dấu tenant/audit (ID nếu trống, TenantId, CreateTime UTC, CreateUId, IsDelete = null) — MỘT lần cho mỗi
    /// bản ghi. Ghi trực tiếp: <c>InsertAsync</c> tự gọi. Ghi qua journal QuestDB HA (Shared.QuestHa): gọi trước khi
    /// append để hai node nhận cùng một giá trị (replicator không đóng dấu lại).
    /// </summary>
    // KienLT - 27/10/2026: Chuẩn hoá chung cho đường ghi trực tiếp và journal để dữ liệu hai phía khớp nhau.
    public void Canonicalize(VdsQuestEntityTenant entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (string.IsNullOrEmpty(entity.ID)) entity.ID = Guid.NewGuid().ToString();
        entity.TenantId = TenantId;
        entity.CreateTime = DateTime.UtcNow;
        entity.CreateUId = ServiceUserId;
        entity.IsDelete = null;
    }

    /// <summary>
    /// Chạy một lượt gọi QuestDB dưới cầu dao.
    ///
    /// <para><paramref name="work"/> là LAMBDA chứ không phải <see cref="Task"/> đã khởi động: cầu dao
    /// phải quyết định trước khi câu lệnh được dựng, và SqlSugar mở kết nối ĐỒNG BỘ ngay trong thân
    /// method async nên "dựng task" đã là "chạm socket".</para>
    /// </summary>
    /// <exception cref="QuestDbUnavailableException">Cửa đang đóng — bỏ lượt này, thử lại sau.</exception>
    protected async Task<T> GuardAsync<T>(Func<Task<T>> work)
    {
        var runtime = _config.Current;

        if (!_gate.TryEnter(DateTime.UtcNow, out var retryAfter, out var failures))
            throw new QuestDbUnavailableException(runtime.QuestConnectionId, retryAfter, failures);

        try
        {
            // Bound SqlSugar's synchronous connection opening as well as commands. A cancelled
            // consumer must not remain blocked behind the driver's default multi-minute timeout.
            var connection = new Npgsql.NpgsqlConnectionStringBuilder(Client.CurrentConnectionConfig.ConnectionString);
            connection.Timeout = Math.Min(connection.Timeout > 0 ? connection.Timeout : 5, 5);
            connection.CommandTimeout = Math.Min(connection.CommandTimeout > 0 ? connection.CommandTimeout : 30, 30);
            Client.CurrentConnectionConfig.ConnectionString = connection.ConnectionString;
            Client.Ado.CommandTimeOut = 30;
            var result = await work().ConfigureAwait(false);
            _gate.ReportSuccess();
            return result;
        }
        catch (OperationCanceledException)
        {
            // Huỷ theo yêu cầu (host dừng, hết hạn xử lý) KHÔNG phải bằng chứng máy chủ chết.
            _gate.ReportUnrelatedFailure();
            throw;
        }
        catch (Exception ex)
        {
            if (QuestAvailabilityGate.IsConnectivityFailure(ex))
            {
                _gate.ReportConnectivityFailure(
                    DateTime.UtcNow,
                    TimeSpan.FromSeconds(runtime.EffectiveQuestUnavailableCooldownSeconds));
            }
            else
            {
                _gate.ReportUnrelatedFailure();
            }

            throw;
        }
    }

    // KienLT - 27/10/2026: Lease giữ đúng client của node đã được router chọn.
    /// <summary>Đọc qua lease khi bật Router; Single giữ cầu dao hiện có.</summary>
    protected Task<T> ReadAsync<T>(Func<ISqlSugarClient, Task<T>> work, CancellationToken ct) =>
        _questHa?.ReadMode == QuestReadModeEnum.Router ? ReadCoveredAsync(_questHa, work, ct) : GuardAsync(async () =>
        {
            ct.ThrowIfCancellationRequested();
            var db = Client;
            var previous = db.Ado.CancellationToken;
            db.Ado.CancellationToken = ct;
            try { return await work(db).ConfigureAwait(false); }
            finally { db.Ado.CancellationToken = previous; }
        });

    // KienLT - 31/10/2026: chốt head một lần, chờ coverage có giới hạn trên từng node; journal lỗi hoặc hết node phủ thì
    // QuestDbUnavailableException giữ đúng mã/RetryAfter/số lỗi Worker đã dùng (QuestHa / QuestHaNotCovered / QuestHaJournalUnavailable).
    private static async Task<T> ReadCoveredAsync<T>(QuestHaManager questHa, Func<ISqlSugarClient, Task<T>> work, CancellationToken ct)
    {
        try
        {
            return await questHa.ReadAsync(work, ct).ConfigureAwait(false);
        }
        catch (QuestHaUnavailableException ex)
        {
            throw new QuestDbUnavailableException(ex.Code, ex.RetryAfter, ex.ConsecutiveFailures);
        }
    }

    /// <summary>Bản không trả kết quả của <see cref="GuardAsync{T}"/>.</summary>
    protected Task GuardAsync(Func<Task> work) =>
        GuardAsync(async () =>
        {
            await work().ConfigureAwait(false);
            return true;
        });

    protected Task<int> GuardWriteAsync(Func<ISqlSugarClient, Task<int>> work, CancellationToken ct) => GuardAsync(async () =>
    {
        ct.ThrowIfCancellationRequested();
        var db = Client;
        var previous = db.Ado.CancellationToken;
        db.Ado.CancellationToken = ct;
        try { return await work(db).ConfigureAwait(false); }
        finally { db.Ado.CancellationToken = previous; }
    });
}
