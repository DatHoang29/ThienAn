using SqlSugar;

namespace Shared.QuestHa;

/// <summary>
/// Author: KienLT
/// Description: Cửa vào duy nhất của thư viện QuestDB HA. Host đăng ký bằng <c>services.AddQuestHa(configuration, tables, role)</c>
/// rồi inject <see cref="QuestHaManager"/>; mọi thành phần còn lại (journal, replicator, router, backup…) là nội bộ.
/// Manager chỉ uỷ quyền cho service sẵn có — không chứa luật HA. Method không dùng được ở vai/chế độ hiện tại ném
/// <see cref="InvalidOperationException"/>. Khi WriteMode=Direct và ReadMode=Single, AddQuestHa không đăng ký Manager
/// (consumer nhận null và giữ nguyên đường cũ).
/// Created date: 31/10/2026
/// </summary>
public sealed class QuestHaManager
{
    private readonly QuestHaOptions _options;
    private readonly Func<IJournalStore>? _journal;
    private readonly Func<QuestShadowCopyWorker>? _shadow;
    private readonly Func<QuestCoveredReadService>? _coveredRead;
    private readonly Func<QuestJournalMaintenanceService>? _maintenance;
    private readonly Func<QuestHaHealthService>? _health;
    private readonly Func<QuestNodeCompareService>? _compare;
    private readonly Func<QuestNodeOptions, IQuestNodeClient>? _nodeClient;
    private readonly Func<QuestBackupAdminClientService>? _adminPipe;
    private readonly Func<QuestMaintenanceLockService>? _maintenanceLock;
    private readonly Func<QuestHaSettingsService>? _settings;
    private readonly Func<QuestExportService>? _export;

    // Thành phần truyền dạng Func để dựng lười như đăng ký DI cũ: chưa gọi method nào thì không mở SQL/pool node.
    internal QuestHaManager(QuestHaHostRoleEnum role, QuestHaOptions options,
        Func<IJournalStore>? journal = null,
        Func<QuestShadowCopyWorker>? shadow = null,
        Func<QuestCoveredReadService>? coveredRead = null,
        Func<QuestJournalMaintenanceService>? maintenance = null,
        Func<QuestHaHealthService>? health = null,
        Func<QuestNodeCompareService>? compare = null,
        Func<QuestNodeOptions, IQuestNodeClient>? nodeClient = null,
        Func<QuestBackupAdminClientService>? adminPipe = null,
        Func<QuestMaintenanceLockService>? maintenanceLock = null,
        Func<QuestHaSettingsService>? settings = null,
        Func<QuestExportService>? export = null)
    {
        Role = role;
        _options = options;
        _journal = journal;
        _shadow = shadow;
        _coveredRead = coveredRead;
        _maintenance = maintenance;
        _health = health;
        _compare = compare;
        _nodeClient = nodeClient;
        _adminPipe = adminPipe;
        _maintenanceLock = maintenanceLock;
        _settings = settings;
        _export = export;
    }

    /// <summary>Vai host đã đăng ký.</summary>
    public QuestHaHostRoleEnum Role { get; }

    /// <summary>Chế độ ghi từ cấu hình: Journal ⇒ consumer gọi <see cref="AppendAsync"/>; Shadow ⇒ ghi node chính rồi <see cref="CopyToShadow"/>.</summary>
    public QuestWriteModeEnum WriteMode => _options.WriteMode;

    /// <summary>Chế độ đọc từ cấu hình: Router ⇒ consumer đọc qua <see cref="ReadAsync{T}"/>; Single ⇒ giữ đường đọc cũ của host.</summary>
    public QuestReadModeEnum ReadMode => _options.ReadMode;

    /// <summary>
    /// Ghi một event vào journal (WriteMode=Journal, vai Writer). Chỉ hoàn tất khi SQL đã COMMIT (điểm ACK); lỗi SQL/huỷ lan ra
    /// cho caller retry, không ghi trực tiếp QuestDB. Huỷ trước khi gọi ⇒ <see cref="OperationCanceledException"/>, event chưa nhận.
    /// Vai khác Writer (Reader/Admin) hoặc WriteMode khác Journal ⇒ <see cref="InvalidOperationException"/> trước khi chạm journal.
    /// </summary>
    public async Task AppendAsync(QuestTableSpec table, object entity, CancellationToken ct)
    {
        // Kiểm vai trước: Reader/Admin cũng giữ delegate journal (đọc trạng thái/vận hành) nhưng không được ghi.
        if (Role != QuestHaHostRoleEnum.Writer || WriteMode != QuestWriteModeEnum.Journal) throw Unavailable(nameof(AppendAsync));
        var journal = Require(_journal, nameof(AppendAsync));
        ct.ThrowIfCancellationRequested();
        await journal.AppendAsync(table, entity, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Sao chép bản ghi đã ghi thành công node chính vào journal (WriteMode=Shadow, vai Writer). Chụp JSON ngay, không chờ SQL,
    /// không ném lỗi sao chép ra pipeline (đầy hàng đợi/lỗi journal chỉ đếm và cảnh báo có throttle).
    /// Vai khác Writer hoặc WriteMode khác Shadow ⇒ <see cref="InvalidOperationException"/>.
    /// </summary>
    public void CopyToShadow(QuestTableSpec table, object entity)
    {
        if (Role != QuestHaHostRoleEnum.Writer || WriteMode != QuestWriteModeEnum.Shadow) throw Unavailable(nameof(CopyToShadow));
        Require(_shadow, nameof(CopyToShadow)).Append(table, entity);
    }

    /// <summary>
    /// Đọc có chứng nhận coverage (ReadMode=Router, vai Writer/Reader): chốt head journal một lần, chạy <paramref name="query"/>
    /// trên lease của node đã phủ head. Journal lỗi / không node phủ / hết node ⇒ <see cref="QuestHaUnavailableException"/>
    /// (Code, RetryAfter); huỷ ⇒ <see cref="OperationCanceledException"/>; lỗi khác của query ném nguyên.
    /// </summary>
    public Task<T> ReadAsync<T>(Func<ISqlSugarClient, Task<T>> query, CancellationToken ct) =>
        Require(_coveredRead, nameof(ReadAsync)).ReadAsync(query, ct);

    /// <summary>Trạng thái HA: backlog journal (cursor/pending từng node) rồi sức khoẻ (node, lag, quarantine, backup). Lỗi SQL của backlog ném ra.</summary>
    public async Task<QuestHaStatusOutput> GetStatusAsync(CancellationToken ct)
    {
        var maintenance = Require(_maintenance, nameof(GetStatusAsync));
        var health = Require(_health, nameof(GetStatusAsync));
        var backlog = await maintenance.GetBacklogAsync(ct).ConfigureAwait(false);
        var check = await health.CheckAsync(ct).ConfigureAwait(false);
        return new QuestHaStatusOutput(backlog, check);
    }

    /// <summary>
    /// So sánh một bảng giữa hai node (chỉ đọc, vai Admin). Tên node sai/trùng hoặc nguồn = đích ⇒ <see cref="ArgumentException"/>.
    /// </summary>
    public Task<NodeCompareOutput> CompareAsync(NodeCompareInput input, CancellationToken ct)
    {
        RequireAdmin(nameof(CompareAsync));
        var compare = Require(_compare, nameof(CompareAsync));
        var (source, target) = ResolvePair(input.Source, input.Target);
        return compare.CompareAsync(input.Table, source, target, input.FromUtc, input.ToUtc, input.Settle, maxWindows: input.MaxWindows, ct: ct);
    }

    /// <summary>
    /// Vá node đích từ node nguồn theo từng bảng (vai Admin). Xem trước khi <c>Apply</c> = false. Apply chỉ ở WriteMode=Shadow với nguồn
    /// là SourceNode (sai ⇒ <see cref="ArgumentException"/>), chạy dưới khoá vận hành journal (đang có người giữ ⇒
    /// <see cref="InvalidOperationException"/>), chờ đích hết backlog và WAL apply; quá hạn ⇒ mục cuối có <c>DeferredReason</c> và dừng.
    /// </summary>
    public async Task<IReadOnlyList<NodePatchOutput>> PatchAsync(NodePatchInput input, CancellationToken ct)
    {
        RequireAdmin(nameof(PatchAsync));
        var compare = Require(_compare, nameof(PatchAsync));
        var journal = Require(_journal, nameof(PatchAsync));
        var (source, target) = ResolvePair(input.Source, input.Target);
        // Chỉ nguồn Direct của Shadow là nguồn sự thật để vá node phụ. Journal phải phát lại từ journal,
        // không đưa snapshot node cũ vào journal rồi ghi đè lên cả nguồn đang nhận dữ liệu mới.
        if (input.Apply && (_options.WriteMode != QuestWriteModeEnum.Shadow || _options.SourceNode is null ||
            !_options.SourceNode.Name.Equals(input.Source, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Vá thật chỉ hợp lệ ở WriteMode=Shadow với nguồn là SourceNode.", nameof(input));

        async Task<IReadOnlyList<NodePatchOutput>> Execute(CancellationToken token)
        {
            var results = new List<NodePatchOutput>();
            foreach (var table in input.Tables)
            {
                if (input.Apply)
                {
                    // Không tạo cursor giả: thiếu cursor nghĩa là Worker chưa sẵn sàng, chờ trong ngân sách.
                    var started = System.Diagnostics.Stopwatch.StartNew();
                    while (true)
                    {
                        var stats = await journal.GetStatsAsync(token).ConfigureAwait(false);
                        if (stats.PendingByNode.TryGetValue(input.Target, out var pending) && pending <= input.MaxPending) break;
                        if (started.Elapsed >= input.Wait) { results.Add(new(table.Name, null, "TargetPendingOrNoCursor")); return results; }
                        await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
                    }
                    // pending của target đã chốt ở trên, TRƯỚC probe đầu; row đã giao lúc đó nằm
                    // trong WAL nên mốc sequencer chốt ở probe đầu bao chúng, append mới sau đó không làm lệnh chờ mãi.
                    if (!await target.WaitWalAppliedAsync(table, input.Wait, token).ConfigureAwait(false))
                    { results.Add(new(table.Name, null, "TargetWalNotApplied")); return results; }
                }
                var repair = await compare.RepairAsync(table, source, target, input.FromUtc, input.ToUtc, dryRun: !input.Apply,
                    maxRows: input.MaxRows, settle: input.Settle, maxTargetPending: input.MaxPending, maxWindows: input.MaxWindows, ct: token).ConfigureAwait(false);
                results.Add(new(table.Name, repair, null));
            }
            return results;
        }

        return input.Apply
            ? await Require(_maintenanceLock, nameof(PatchAsync)).RunAsync(Execute, ct).ConfigureAwait(false)
            : await Execute(ct).ConfigureAwait(false);
    }

    /// <summary>Dọn journal đã giao có trần theo <c>QuestHa:JournalMaintenance</c> (vai Admin); <paramref name="apply"/> = false chỉ đếm.</summary>
    public Task<JournalPurgeOutput> PurgeAsync(bool apply, CancellationToken ct)
    {
        RequireAdmin(nameof(PurgeAsync));
        return Require(_maintenance, nameof(PurgeAsync)).PurgeAsync(dryRun: !apply, ct: ct);
    }

    /// <summary>Gửi lệnh backup nguội node qua IPC tới host Writer (vai Admin); <paramref name="apply"/> = false chỉ tiền kiểm.</summary>
    public Task<QuestHaAdminReplyOutput> BackupAsync(string node, bool apply, CancellationToken ct) =>
        SendAdminAsync(nameof(BackupAsync), node, (apply ? "apply|" : "preview|") + node, ct);

    /// <summary>
    /// Gửi lệnh restore node từ bản <paramref name="backupId"/> qua IPC (vai Admin); <paramref name="apply"/> = false chỉ xem trước
    /// (băm lại nội dung theo manifest). <paramref name="allowJournalGap"/> = chấp nhận journal thiếu: bắt buộc backfill, node không tự Ready.
    /// </summary>
    public Task<QuestHaAdminReplyOutput> RestoreAsync(string node, Guid backupId, bool apply, bool allowJournalGap, CancellationToken ct) =>
        SendAdminAsync(nameof(RestoreAsync), node, (apply ? "restore|" : "restore-preview|") + node + "|" + backupId + "|" +
            (allowJournalGap ? "allow-journal-gap" : "strict"), ct);

    /// <summary>Gửi lệnh gỡ rào thủ công của node qua IPC (vai Admin); <paramref name="apply"/> = false chỉ xem trước.</summary>
    public Task<QuestHaAdminReplyOutput> UnfenceNodeAsync(string node, bool apply, CancellationToken ct) =>
        SendAdminAsync(nameof(UnfenceNodeAsync), node, (apply ? "unfence|" : "unfence-preview|") + node, ct);

    /// <summary>Danh sách bản backup trong catalog (lọc theo node nếu có), mới nhất trước (vai Admin).</summary>
    public Task<IReadOnlyList<BackupRecordOutput>> ListBackupsAsync(string? node, CancellationToken ct)
    {
        RequireAdmin(nameof(ListBackupsAsync));
        return Require(_journal, nameof(ListBackupsAsync)).GetBackupsAsync(node, ct);
    }

    /// <summary>
    /// Retire (<paramref name="retired"/> = true) hoặc unretire bản backup (vai Admin); <paramref name="apply"/> = false chỉ xem trước.
    /// Trả true khi đã ghi. Không cho retire bản Verified mới nhất.
    /// </summary>
    public Task<bool> RetireBackupAsync(Guid backupId, bool retired, bool apply, CancellationToken ct)
    {
        RequireAdmin(nameof(RetireBackupAsync));
        return Require(_journal, nameof(RetireBackupAsync)).SetBackupRetiredAsync(backupId, retired, !apply, ct);
    }

    /// <summary>
    /// Gỡ cursor journal của node đã bỏ khỏi <c>QuestHa:Nodes</c> (vai Admin); heartbeat mới hơn <paramref name="staleMinutes"/> chặn,
    /// trừ <paramref name="forceLive"/>. <paramref name="apply"/> = false chỉ xem trước; không xoá journal/status/backup/quarantine.
    /// </summary>
    public Task<JournalCursorRetireOutput> RetireCursorAsync(string node, int staleMinutes, bool forceLive, bool apply, CancellationToken ct)
    {
        RequireAdmin(nameof(RetireCursorAsync));
        return Require(_journal, nameof(RetireCursorAsync)).RetireJournalCursorAsync(node, _options.Nodes.Select(n => n.Name).ToArray(),
            staleMinutes, forceLive, !apply, ct);
    }

    /// <summary>
    /// Áp NGUYÊN TỬ tham số vận hành đổi lúc chạy (ngưỡng health, bảo trì journal, lịch/giữ bản backup, router/đọc có coverage)
    /// cho mọi service của host. Thuộc tính null ⇒ giá trị khởi động (appsettings/mặc định). Gói có một giá trị sai (ngoài biên,
    /// Warn ≥ Critical, giờ không phải HH:mm, cửa sổ rỗng, slot lịch ngoài cửa sổ…) ⇒ từ chối CẢ gói, giữ bộ đang chạy, log Warning,
    /// trả false. Thành công ⇒ log Information các khoá đổi, trả true; vòng đang chạy (purge, backup đang chép, lượt chờ coverage)
    /// giữ bộ đã chụp, áp từ vòng sau. <paramref name="settings"/> null ⇒ <see cref="ArgumentNullException"/>.
    /// </summary>
    public bool ApplySettings(QuestHaRuntimeOptions settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return Require(_settings, nameof(ApplySettings)).Apply(settings);
    }

    /// <summary>
    /// Lấy vé đọc tham số vận hành (tăng dần trong host). Gọi NGAY TRƯỚC khi truy vấn nguồn cấu hình (DB) rồi truyền vé cho
    /// <see cref="ApplySettings(QuestHaRuntimeOptions, long, out bool)"/>: nhờ vậy kết quả đọc cũ về muộn không đè bộ mới hơn đã áp.
    /// </summary>
    public long BeginSettingsRead() => Require(_settings, nameof(BeginSettingsRead)).BeginRead();

    /// <summary>
    /// Như <see cref="ApplySettings(QuestHaRuntimeOptions)"/> nhưng gắn vé đọc từ <see cref="BeginSettingsRead"/>. Vé cũ hơn bộ đang chạy
    /// (một lượt đọc bắt đầu sau đã áp) ⇒ bỏ, <paramref name="superseded"/> = true, trả false — không phải lỗi cấu hình.
    /// Vé chưa phát ⇒ <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    public bool ApplySettings(QuestHaRuntimeOptions settings, long readTicket, out bool superseded)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return Require(_settings, nameof(ApplySettings)).Apply(settings, readTicket, out superseded);
    }

    /// <summary>
    /// Xuất (hoặc kiểm lại nếu đã có) trọn ngày giờ server <paramref name="dayLocal"/> ra Parquet zstd theo giờ cho mọi bảng đã đăng ký
    /// (vai Writer/Admin). <paramref name="apply"/> = false chỉ xem trước: node chọn, số dòng dự kiến từng giờ, dung lượng ước, trống đĩa
    /// (phải ≥ ước × 2). Hoãn (không node phụ Ready, chưa phủ head, thiếu đĩa, ngày chưa kết thúc, lượt khác đang chạy) ⇒
    /// <see cref="QuestExportOutput.DeferredReason"/>. Vai Reader ⇒ <see cref="InvalidOperationException"/>.
    /// </summary>
    public Task<QuestExportOutput> ExportAsync(DateOnly dayLocal, bool apply, CancellationToken ct)
    {
        RequireExportRole(nameof(ExportAsync));
        return Require(_export, nameof(ExportAsync)).ExportAsync(dayLocal, apply, ct);
    }

    /// <summary>Catalog bản xuất Parquet (lọc bảng, khoảng ngày giờ server), ngày mới nhất trước (vai Writer/Admin).</summary>
    public Task<IReadOnlyList<QuestExportRecordOutput>> ListExportsAsync(string? table, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        RequireExportRole(nameof(ListExportsAsync));
        return Require(_export, nameof(ListExportsAsync)).ListAsync(table, from, to, ct);
    }

    /// <summary>
    /// Nạp ngược một bản xuất vào node <paramref name="node"/> (vai Writer/Admin). <paramref name="dayFolder"/> = thư mục phiên bản
    /// <c>vNNNN</c> có manifest, hoặc thư mục ngày (⇒ phiên bản đã công bố mới nhất). Kiểm SHA-256 theo manifest TRƯỚC, file phải nằm
    /// trong <c>ParquetCopyRoot</c> của node (chép vào thư mục phiên riêng nếu chưa, cây đích không qua link), <c>INSERT INTO … SELECT * FROM
    /// read_parquet(…)</c> từng giờ, DEDUP chống trùng; báo số dòng trước/sau; WAL chưa áp xong ⇒ <c>Error = WalNotApplied</c>.
    /// <paramref name="intoTable"/> = bảng đích khác (đã tạo bằng schema.sql), null = bảng của manifest. <paramref name="apply"/> = false chỉ kiểm.
    /// File lệch/thiếu ⇒ <see cref="InvalidDataException"/>; cây đích qua link ⇒ <see cref="IOException"/>; node/bảng sai ⇒ <see cref="ArgumentException"/>.
    /// </summary>
    public Task<QuestImportOutput> ImportAsync(string dayFolder, string node, bool apply, CancellationToken ct) =>
        ImportAsync(dayFolder, node, apply, null, ct);

    /// <summary>Như <see cref="ImportAsync(string, string, bool, CancellationToken)"/>, nạp vào <paramref name="intoTable"/> (null = bảng của manifest).</summary>
    public Task<QuestImportOutput> ImportAsync(string dayFolder, string node, bool apply, string? intoTable, CancellationToken ct)
    {
        RequireExportRole(nameof(ImportAsync));
        ArgumentException.ThrowIfNullOrWhiteSpace(dayFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(node);
        return Require(_export, nameof(ImportAsync)).ImportAsync(dayFolder, node, apply, intoTable, ct);
    }

    private void RequireExportRole(string operation)
    {
        if (Role == QuestHaHostRoleEnum.Reader) throw Unavailable(operation);
    }

    private async Task<QuestHaAdminReplyOutput> SendAdminAsync(string operation, string node, string request, CancellationToken ct)
    {
        RequireAdmin(operation);
        var reply = await Require(_adminPipe, operation).SendAsync(node, request, ct).ConfigureAwait(false);
        return new QuestHaAdminReplyOutput(reply);
    }

    private (IQuestNodeClient Source, IQuestNodeClient Target) ResolvePair(string sourceName, string targetName)
    {
        var nodeClient = Require(_nodeClient, "node client");
        if (string.Equals(sourceName, targetName, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Node nguồn và node đích phải khác nhau.", nameof(targetName));
        var nodes = _options.Nodes.ToList();
        if (_options.SourceNode is { } sourceNode) nodes.Add(sourceNode);
        if (nodes.Any(n => string.IsNullOrWhiteSpace(n.Name)) || nodes.GroupBy(n => n.Name, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            throw new ArgumentException("QuestHa:Nodes/SourceNode có tên trống hoặc trùng.", nameof(sourceName));
        var sourceConfig = nodes.SingleOrDefault(n => n.Name.Equals(sourceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("Node nguồn không thuộc Nodes/SourceNode.", nameof(sourceName));
        // Đích bắt buộc thuộc Nodes: SourceNode không có replicator nhận dữ liệu.
        var targetConfig = _options.Nodes.SingleOrDefault(n => n.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException("Node đích không thuộc Nodes.", nameof(targetName));
        return (nodeClient(sourceConfig), nodeClient(targetConfig));
    }

    private void RequireAdmin(string operation)
    {
        if (Role != QuestHaHostRoleEnum.Admin) throw Unavailable(operation);
    }

    private T Require<T>(Func<T>? part, string operation) => part is not null ? part() : throw Unavailable(operation);

    private Func<QuestNodeOptions, IQuestNodeClient> Require(Func<QuestNodeOptions, IQuestNodeClient>? part, string operation) =>
        part ?? throw Unavailable(operation);

    private InvalidOperationException Unavailable(string operation) =>
        new($"QuestHaManager.{operation} không dùng được ở vai {Role} (WriteMode={WriteMode}, ReadMode={ReadMode}).");
}
