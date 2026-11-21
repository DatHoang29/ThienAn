namespace Shared.QuestHa.Interfaces;

/// <summary>
/// Author: KienLT
/// Description: Journal SQL — điểm ACK duy nhất (append chỉ trả về khi SQL đã COMMIT; lỗi SQL ⇒ <see cref="JournalAppendException"/>,
/// caller coi event chưa nhận) và mọi thao tác cursor/trạng thái/quarantine/backup mà replicator/router/reconciler/backup dùng.
/// Hiện thực: <see cref="Shared.QuestHa.Infrastructure.Services.Journal.QuestJournalService"/>; test thay bằng fake.
/// Created date: 09/10/2026
/// </summary>
internal interface IJournalStore
{
    // Nhóm ghi/xem journal; mặc định báo thiếu hiện thực để fake chỉ cần cài phần mình dùng.

    /// <summary>
    /// Ghi 1 event đã chuẩn hoá. <paramref name="payload"/> được serialize JSON theo tên cột của
    /// <paramref name="table"/>; EventKey/DesignatedTs/Version lấy từ payload theo spec.
    /// Nếu spec có VersionColumn và journal đã có bản cùng khoá với version LỚN HƠN ⇒ dòng mới vẫn được
    /// ghi (ACK) nhưng đánh dấu Superseded=1 và không được giao (version cao nhất thắng).
    /// </summary>
    Task<JournalAppendOutput> AppendAsync(QuestTableSpec table, object payload, CancellationToken ct = default)
        => throw new NotSupportedException("Journal append is not implemented.");

    /// <summary>Ghi thô, KHÔNG kiểm tra kiểu payload (dùng để thử dữ liệu hỏng).</summary>
    Task<JournalAppendOutput> AppendRawAsync(JournalAppendInput entry, CancellationToken ct = default)
        => throw new NotSupportedException("Journal raw append is not implemented.");

    /// <summary>
    /// Ghi thô cả lô trong 1 transaction (caller tự gom, vd worker). ACK theo thứ tự đầu vào; lỗi ⇒ cả lô CHƯA ACK.
    /// Không kiểm tra kiểu payload — caller dùng <see cref="AppendAsync"/> nếu cần kiểm.
    /// </summary>
    Task<IReadOnlyList<JournalAppendOutput>> AppendRawBatchAsync(IReadOnlyList<JournalAppendInput> entries, CancellationToken ct = default)
        => throw new NotSupportedException("Journal batch append is not implemented.");

    /// <summary>Tổng quan journal: Seq lớn nhất, tổng dòng, số quarantine, con trỏ và số dòng chờ từng node.</summary>
    Task<JournalStatsOutput> GetStatsAsync(CancellationToken ct = default)
        => throw new NotSupportedException("Journal stats are not implemented.");

    /// <summary>N dòng mới nhất (Seq giảm dần).</summary>
    Task<IReadOnlyList<JournalRowOutput>> GetLatestAsync(int take = 10, CancellationToken ct = default)
        => throw new NotSupportedException("Journal latest rows are not implemented.");

    /// <summary>N dòng quarantine gần nhất.</summary>
    Task<IReadOnlyList<QuarantineRowOutput>> GetQuarantineAsync(int take = 50, CancellationToken ct = default)
        => throw new NotSupportedException("Journal quarantine listing is not implemented.");

    /// <summary>Danh sách bản backup (lọc theo node nếu có), mới nhất trước.</summary>
    Task<IReadOnlyList<BackupRecordOutput>> GetBackupsAsync(string? node = null, CancellationToken ct = default)
        => throw new NotSupportedException("Backup listing is not implemented.");

    // Nhóm cursor/trạng thái/phục hồi/backup.

    // restore phải kiểm journal, thiếu hiện thực thì từ chối thay vì giả vờ đủ dữ liệu.
    /// <summary>True nếu đã purge sau watermark hoặc phần journal còn lại bắt đầu quá watermark+1.</summary>
    Task<bool> HasJournalGapAsync(long watermark, CancellationToken ct = default)
        => throw new NotSupportedException("Journal replay continuity check is not implemented.");
    /// <summary>Kiểm journal còn đủ và đặt cursor restore nguyên tử với purge; thiếu hiện thực phải từ chối.</summary>
    Task SetRestoreCursorAsync(string node, long watermark, CancellationToken ct = default)
        => throw new NotSupportedException("Atomic restore cursor pin is not implemented.");
    // catalog/retire dùng cho tiền kiểm và chính sách bảo vệ backup.
    /// <summary>Catalog phục vụ lịch/retention; thiếu hiện thực phải báo lỗi.</summary>
    Task<IReadOnlyList<BackupRecordOutput>> ListBackupsAsync(CancellationToken ct = default)
        => throw new NotSupportedException("Backup catalog listing is not implemented.");
    /// <summary>Retire/unretire tường minh, không cho retire bản Verified mới nhất.</summary>
    Task<bool> SetBackupRetiredAsync(Guid id, bool retired, bool dryRun = true, CancellationToken ct = default)
        => throw new NotSupportedException("Backup retirement is not implemented.");
    // mặc định báo thiếu hiện thực để các fake cũ không giả vờ đã dọn an toàn.
    /// <summary>Đo pending có trần mặc định theo node; tổng dòng/dung lượng lấy metadata.</summary>
    Task<JournalBacklogOutput> ReadJournalBacklogAsync(IReadOnlyList<string> nodes, CancellationToken ct = default)
        => throw new NotSupportedException("Journal maintenance measurement is not implemented.");

    /// <summary>Đếm tối đa pendingCountLimit dòng mỗi node; chạm trần trả cận dưới để vẫn phân mức chính xác.</summary>
    Task<JournalBacklogOutput> ReadJournalBacklogAsync(IReadOnlyList<string> nodes, long pendingCountLimit, CancellationToken ct = default)
        => ReadJournalBacklogAsync(nodes, ct);

    /// <summary>Gỡ cursor ngoài cấu hình có chủ đích; mặc định chỉ xem trước, không xoá journal/status/backup/quarantine.</summary>
    Task<JournalCursorRetireOutput> RetireJournalCursorAsync(string node, IReadOnlyList<string> configuredNodes,
        bool dryRun = true, CancellationToken ct = default)
        => throw new NotSupportedException("Journal cursor retirement is not implemented.");

    /// <summary>Gỡ cursor ngoài cấu hình; heartbeat mới hơn <paramref name="staleMinutes"/> chặn cả xem trước/apply, trừ <paramref name="forceLive"/>.</summary>
    Task<JournalCursorRetireOutput> RetireJournalCursorAsync(string node, IReadOnlyList<string> configuredNodes,
        int staleMinutes, bool forceLive = false, bool dryRun = true, CancellationToken ct = default)
        => throw new NotSupportedException("Journal cursor retirement is not implemented.");

    /// <summary>Dọn/đếm có trần; một phiên SQL giữ khoá liên tiến trình, mỗi lô kiểm lại cursor/backup/quarantine nguyên tử.</summary>
    Task<JournalPurgeOutput> PurgeJournalAsync(IReadOnlyList<string> nodes, JournalMaintenanceOptions options,
        bool dryRun = true, CancellationToken ct = default)
        => throw new NotSupportedException("Journal maintenance purge is not implemented.");

    /// <summary>Lô kế tiếp: <c>Seq &gt; afterSeq AND RowVer &lt; MIN_ACTIVE_ROWVERSION()</c>, theo Seq tăng dần.</summary>
    Task<IReadOnlyList<JournalRowOutput>> ReadBatchAsync(long afterSeq, int max, CancellationToken ct = default);

    /// <summary>Đọc cursor; tạo dòng (0,0) nếu chưa có.</summary>
    Task<NodeCursorOutput> GetCursorAsync(string node, CancellationToken ct = default);

    /// <summary>Đọc recovery/cursor/quarantine chung; hiện thực SQL dùng một lệnh không MERGE, thiếu metadata thì fail-closed.</summary>
    async Task<NodeReadCoverageOutput> ReadCoverageAsync(string node, CancellationToken ct = default)
    {
        var recovery = await GetRecoveryAsync(node, ct);
        var cursor = await GetCursorAsync(node, ct);
        var quarantined = await GetQuarantinedSeqsAsync(node, 0, long.MaxValue, ct);
        return new(recovery.Generation, recovery.Phase, recovery.ManualFence, cursor.LastSeq,
            quarantined.Count == 0 ? null : quarantined.Min());
    }

    /// <summary>
    /// Tiến cursor có điều kiện <c>LastSeq = expected</c> (optimistic). false = cursor đã bị người khác đổi
    /// (reconciler/restore) ⇒ caller phải đọc lại, KHÔNG ghi đè.
    /// </summary>
    Task<bool> TryAdvanceCursorAsync(string node, long expected, long newSeq, long safeSeq, CancellationToken ct = default);

    /// <summary>Nâng SafeSeq (không vượt LastSeq, không hạ).</summary>
    Task TryRaiseSafeSeqAsync(string node, long safeSeq, CancellationToken ct = default);

    /// <summary>Lùi LastSeq về SafeSeq (nếu nhỏ hơn). Trả về (cũ, mới); null nếu không lùi.</summary>
    Task<(long From, long To)?> RewindToSafeAsync(string node, CancellationToken ct = default);

    /// <summary>Hạ cursor về <c>min(LastSeq, seq)</c> (phát lại). Trả về cursor sau khi hạ.</summary>
    Task<long> LowerCursorAsync(string node, long seq, string by, CancellationToken ct = default);

    /// <summary>Đặt cứng cursor (restore = watermark). SafeSeq = seq.</summary>
    Task SetCursorAsync(string node, long seq, string by, CancellationToken ct = default);

    /// <summary>Độ trễ so với journal tính từ <paramref name="cursor"/> (chỉ dòng đã commit).</summary>
    Task<NodeLagOutput> GetLagAsync(long cursor, CancellationToken ct = default);

    /// <summary>Ghi phần trạng thái do replicator quản lý (không đụng cột đối soát).</summary>
    Task WriteStatusAsync(NodeStatusOutput status, CancellationToken ct = default);

    /// <summary>Đọc QuestNodeStatus; mọi mốc thời gian đã quy đổi về đồng hồ LOCAL (now − tuổi đo bằng đồng hồ SQL).</summary>
    Task<IReadOnlyList<NodeStatusOutput>> ReadStatusesAsync(CancellationToken ct = default);

    /// <summary>Ghi kết quả đối soát gần nhất của node (đạt/không) vào QuestNodeStatus.</summary>
    Task WriteReconcileResultAsync(string node, bool ok, CancellationToken ct = default);

    /// <summary>Upsert quarantine (Attempts++ nếu đã có). Dòng đã resolved bị quarantine lại ⇒ mở lại.</summary>
    Task QuarantineAsync(long seq, string node, string error, CancellationToken ct = default);

    /// <summary>Seq đang quarantine (chưa resolved) của 1 node trong [fromSeq, toSeq].</summary>
    Task<IReadOnlySet<long>> GetQuarantinedSeqsAsync(string node, long fromSeq, long toSeq, CancellationToken ct = default);

    /// <summary>
    /// quarantine chưa resolved của MỌI node trong [fromSeq, toSeq], kèm thế hệ (<see cref="OpenQuarantineOutput.Attempts"/>)
    /// và cờ Superseded của dòng journal. Reconciler dùng để dựng tập kỳ vọng chung (chỉ loại Seq quarantine ở TẤT CẢ node)
    /// và, gọi với toàn dải, để không chứng nhận node còn quarantine riêng ở cửa sổ khác.
    /// </summary>
    Task<IReadOnlyList<OpenQuarantineOutput>> GetOpenQuarantineAsync(long fromSeq, long toSeq, CancellationToken ct = default)
        => throw new NotSupportedException("Cần hiện thực quản lý quarantine theo thế hệ.");

    /// <summary>
    /// đánh dấu resolved CHỈ khi dòng vẫn mở và đúng thế hệ đã kiểm chứng (<c>Attempts = expectedAttempts</c>).
    /// false = đã bị quarantine lại / đã resolved / không tồn tại ⇒ caller không được chứng nhận node bằng bằng chứng cũ.
    /// </summary>
    Task<bool> ResolveQuarantineAsync(long seq, string node, int expectedAttempts, string by, CancellationToken ct = default)
        => throw new NotSupportedException("Cần hiện thực quản lý quarantine theo thế hệ.");

    // Phục hồi lưu bền và chứng nhận theo thế hệ.

    /// <summary>Đọc trạng thái phục hồi; tạo dòng (thế hệ 0, None) nếu chưa có. Mốc thời gian theo đồng hồ SQL.</summary>
    Task<NodeRecoveryOutput> GetRecoveryAsync(string node, CancellationToken ct = default)
        => throw new NotSupportedException("Cần hiện thực phục hồi node lưu bền theo thế hệ.");

    /// <summary>
    /// Bắt đầu restore: Generation++, Phase=Restoring, xoá CoveredUtc/ReconciledGeneration. PHẢI gọi và thành công TRƯỚC khi
    /// thay root. Trả thế hệ mới.
    /// </summary>
    Task<long> BeginRecoveryAsync(string node, string by, CancellationToken ct = default)
        => throw new NotSupportedException("Cần hiện thực phục hồi node lưu bền theo thế hệ.");

    /// <summary>CAS (Generation = generation AND Phase = Restoring) → Phase=AwaitReconcile, CoveredUtc = giờ SQL. false = thế hệ/pha đã đổi.</summary>
    Task<bool> MarkRecoveryCoveredAsync(string node, long generation, CancellationToken ct = default)
        => throw new NotSupportedException("Cần hiện thực phục hồi node lưu bền theo thế hệ.");

    /// <summary>
    /// CAS kết thúc phục hồi: Generation = generation AND Phase = AwaitReconcile AND ReconciledGeneration = generation AND
    /// LastReconcileOk = 1 AND LastReconcileUtc &gt; CoveredUtc → Phase=None. false = chưa đủ điều kiện.
    /// </summary>
    Task<bool> CompleteRecoveryAsync(string node, long generation, CancellationToken ct = default)
        => throw new NotSupportedException("Cần hiện thực phục hồi node lưu bền theo thế hệ.");

    /// <summary>Rào thủ công lưu bền (sống qua restart process).</summary>
    Task SetManualFenceAsync(string node, bool fenced, string by, CancellationToken ct = default)
        => throw new NotSupportedException("Cần hiện thực phục hồi node lưu bền theo thế hệ.");

    /// <summary>Gỡ rào CAS theo thế hệ; Restoring chỉ cho phép khi host có chứng cứ strict restore đã bàn giao an toàn.</summary>
    Task<bool> TryClearManualFenceAsync(string node, long generation, bool replayReady, CancellationToken ct = default)
        => throw new NotSupportedException("Conditional manual fence clear is not implemented.");

    /// <summary>
    /// Ghi kết quả đối soát CÓ ĐIỀU KIỆN, nguyên tử trong 1 câu lệnh SQL:
    /// <list type="bullet">
    /// <item>Chỉ ghi khi Generation hiện tại = <paramref name="observedGeneration"/> (thế hệ đọc bằng <see cref="GetRecoveryAsync"/>
    /// TRƯỚC khi đọc node) và, nếu Phase = AwaitReconcile, <paramref name="readStartedSqlUtc"/> &gt; CoveredUtc.</item>
    /// <item><paramref name="ok"/> = true chỉ được lưu thành 1 khi KHÔNG còn quarantine mở của node đó mà Seq không quarantine ở
    /// mọi node và không Superseded (kiểm tại thời điểm ghi); ngược lại lưu 0.</item>
    /// <item>Ghi ReconciledGeneration = observedGeneration, LastReconcileUtc = giờ SQL.</item>
    /// </list>
    /// Trả giá trị đã lưu (true/false), hoặc null nếu không ghi do thế hệ/mốc không khớp.
    /// </summary>
    Task<bool?> WriteReconcileResultAsync(string node, bool ok, long observedGeneration, DateTime readStartedSqlUtc, CancellationToken ct = default)
        => throw new NotSupportedException("Cần hiện thực phục hồi node lưu bền theo thế hệ.");

    /// <summary>Dòng chưa Superseded của bảng có DesignatedTs trong [from, to).</summary>
    Task<IReadOnlyList<JournalRowOutput>> ReadWindowAsync(string table, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    // overload mặc định giữ tương thích fake/adapter, SQL lọc tại nguồn.
    /// <summary>Đọc cửa sổ journal tại mốc Seq đã chốt; overload cũ vẫn giữ tương thích.</summary>
    async Task<IReadOnlyList<JournalRowOutput>> ReadWindowAsync(string table, DateTime fromUtc, DateTime toUtc, long maxSeq, CancellationToken ct = default)
        => (await ReadWindowAsync(table, fromUtc, toUtc, ct)).Where(r => r.Seq <= maxSeq).ToArray();

    /// <summary>Thời điểm tạo (UTC) của dòng journal <paramref name="seq"/>; null nếu không có.</summary>
    Task<DateTime?> GetCreatedUtcAsync(long seq, CancellationToken ct = default);

    /// <summary>Ghi một bản backup vào QuestBackupCatalog.</summary>
    Task AddBackupAsync(BackupRecordOutput record, CancellationToken ct = default);

    /// <summary>Đọc một bản backup theo id; null nếu không có.</summary>
    Task<BackupRecordOutput?> GetBackupAsync(Guid backupId, CancellationToken ct = default);

    // Catalog bản xuất Parquet; mặc định báo thiếu hiện thực để fake cũ không giả vờ đã có bản xuất.

    /// <summary>Danh sách bản xuất Parquet (lọc bảng/khoảng ngày giờ server nếu có), ngày mới nhất trước.</summary>
    Task<IReadOnlyList<QuestExportRecordOutput>> ListExportsAsync(string? table = null, DateOnly? fromDay = null, DateOnly? toDay = null,
        CancellationToken ct = default)
        => throw new NotSupportedException("Export catalog listing is not implemented.");

    /// <summary>Ghi một bản xuất; trong cùng transaction retire mọi bản còn hiệu lực của cùng bảng/ngày.</summary>
    Task AddExportAsync(QuestExportRecordOutput record, CancellationToken ct = default)
        => throw new NotSupportedException("Export catalog write is not implemented.");

    /// <summary>Retire các bản xuất theo id (bỏ qua bản đã retire); trả số dòng đã đổi.</summary>
    Task<int> RetireExportsAsync(IReadOnlyList<Guid> exportIds, CancellationToken ct = default)
        => throw new NotSupportedException("Export catalog retirement is not implemented.");

    /// <summary>Chỉ số giờ (tính từ <paramref name="fromUtc"/>) có dòng journal của bảng với Seq trong (afterSeq, maxSeq] — dữ liệu đến muộn.</summary>
    Task<IReadOnlyList<int>> ReadExportLateHoursAsync(string table, DateTime fromUtc, DateTime toUtc, long afterSeq, long maxSeq,
        CancellationToken ct = default)
        => throw new NotSupportedException("Journal late-hour scan is not implemented.");
}
