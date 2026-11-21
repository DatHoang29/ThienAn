namespace Shared.QuestHa.Interfaces;

/// <summary>
/// Author: KienLT
/// Description: Đọc/ghi QuestDB của 1 node (PG wire qua SqlSugar). Dashboard dùng để đếm/xem dòng.
/// Created date: 09/10/2026
/// </summary>
internal interface IQuestNodeClient
{
    string Node { get; }

    // vô hiệu kết nối tới tiến trình node cũ cho cả reader và replicator trong từng host.
    /// <summary>Xoá riêng pool của node, tối đa một lần/giây; client giả không có pool.</summary>
    void ResetConnectionPool() { }

    // giữ tương thích client cũ/test, truyền lý do cho log của client PG thật.
    /// <summary>Xoá pool riêng node, kèm lý do chẩn đoán không chứa chuỗi kết nối.</summary>
    void ResetConnectionPool(string reason) => ResetConnectionPool();

    /// <summary>
    /// SqlSugar client mới (connection pool của Npgsql dùng chung), SqlSugar tự mở kết nối khi chạy lệnh — node đã dừng
    /// mất ~30 s mới báo lỗi (xem <c>QuestNodeClientService.OpenAsync</c>). Đường đọc qua router dùng <see cref="OpenLeaseAsync"/>.
    /// </summary>
    SqlSugar.ISqlSugarClient CreateDb();

    /// <summary>
    /// lease cho router — kết nối mở sẵn bằng OpenAsync (lỗi trong Timeout của chuỗi kết nối, không chặn
    /// thread). Mặc định bọc <see cref="CreateDb"/> cho client giả trong test; client thật PHẢI override.
    /// </summary>
    Task<QuestNodeLease> OpenLeaseAsync(CancellationToken ct = default) => Task.FromResult(new QuestNodeLease(Node, CreateDb()));

    /// <summary>SELECT 1 + kiểm tra bảng tồn tại và WAL không suspend. Ném lỗi kết nối nếu không tới được.</summary>
    Task<NodeProbeOutput> ProbeAsync(QuestTableSpec table, CancellationToken ct = default);

    /// <summary>Insert nhiều dòng trong 1 câu lệnh (1 commit).</summary>
    Task WriteBatchAsync(QuestTableSpec table, IReadOnlyList<QuestRow> rows, CancellationToken ct = default);

    /// <summary>Số dòng của bảng trên node.</summary>
    Task<long> CountAsync(QuestTableSpec table, CancellationToken ct = default);

    /// <summary>N dòng mới nhất theo designated timestamp (cột → giá trị).</summary>
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> LatestAsync(
        QuestTableSpec table, int take = 10, CancellationToken ct = default);

    /// <summary>Đọc mọi dòng có designated ts trong [from, to) — dùng cho reconciler.</summary>
    Task<IReadOnlyList<QuestRow>> ReadRangeAsync(
        QuestTableSpec table, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    // hợp đồng mốc cố định, không đuổi sequencer di động.
    /// <summary>
    /// Chờ WAL đã apply tới MỐC CHỐT Ở PROBE ĐẦU: target = sequencerTxn của probe đầu, true khi writerTxn ≥ target trong
    /// <paramref name="timeout"/>. Không đòi WAL của append sau target cũng rỗng. Bảng mất/WAL suspended/txn lùi so với
    /// probe đầu (node restart/restore) ⇒ false. Caller phải chốt head/cursor TRƯỚC khi gọi: row ≤ head đã ở WAL trước
    /// probe đầu nên target bao phủ chúng.
    /// </summary>
    Task<bool> WaitWalAppliedAsync(QuestTableSpec table, TimeSpan timeout, CancellationToken ct = default);
}
