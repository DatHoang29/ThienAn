using System.Collections.Concurrent;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Module.ShareData.Core.Helpers;
using Shared.DTO.Constants.Application;
using ShareDataWorker.Core.Models.DataOutbound;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Extraction;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Mapping;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Scheduling;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Transport;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;

namespace ShareDataWorker.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Description: Dịch vụ điều phối xuất bản dữ liệu (Outbound Orchestrator - ISO 14827 §5.4).
    ///              Thực thi pipeline trích xuất, ánh xạ dữ liệu và vận chuyển (lưu đĩa, gửi HTTP).
    /// Created date: 31/07/2026
    /// Modified date: 19/10/2026
    /// </summary>
    public partial class DataOutboundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DataOutboundService> logger,
        DataOutboundExtractionProcess extractionProcess,
        DataOutboundTransport transport,
        DataWorkerConfigService workerConfig,
        IConfiguration? configuration = null)
    {
        /// <summary>
        /// Description: Bộ nhớ chống ghi trùng cảnh báo cấu hình kết nối theo cặp đối tác × hash cấu hình, sống theo vòng đời tiến trình Worker.
        /// Created date: 17/10/2026
        /// </summary>
        private static readonly ConcurrentDictionary<string, byte> _loggedConfigWarnings = new();
        /// <summary>
        /// Author: DatHQ
        /// Description: Đăng ký × nhóm mốc × chữ ký công thức đã cảnh báo đổi công thức cột mốc, mỗi trường hợp chỉ cảnh báo một lần trong vòng đời tiến trình Worker.
        /// Created date: 31/07/2026
        /// </summary>
        private static readonly ConcurrentDictionary<string, byte> _warnedFormulaChanges = new();
        /// <summary>
        /// Author: DatHQ
        /// Description: Đăng ký × câu SQL đã ghi rủi ro nối đuôi vào Nhật ký cấu hình, mỗi trường hợp chỉ ghi một lần trong vòng đời tiến trình Worker.
        /// Created date: 31/07/2026
        /// </summary>
        private static readonly ConcurrentDictionary<string, byte> _loggedCursorRisks = new();

        /// <summary>
        /// Description: Bản ghi chứa các thông tin định danh và bối cảnh lặp lại phục vụ ghi nhật ký kết xuất từng trang dữ liệu.
        /// Created date: 16/10/2026
        /// </summary>
        private sealed record PageLog(
            string ParentId,
            int RecordCount,
            string? MappingId,
            string? PacketVersion,
            int? SerialNbr);

        /// <summary>
        /// Description: Dấu vết các dòng mốc (ID, UpdateTime) của đối tác × gói lần cuối Worker thấy hoặc tự ghi trong lượt: chụp đầu lượt, cập nhật sau mỗi lần
        ///              ghi mốc. Dòng mốc khác dấu vết là do Đặt lại / Đặt mốc từ màn hình (kể cả giữa hai trang): lượt này bỏ ghi mốc và dừng.
        /// Created date: 30/10/2026
        /// </summary>
        private sealed class CursorSnapshot
        {
            public (string ID, DateTime? UpdateTime)? Default { get; set; }
            public Dictionary<string, (string ID, DateTime? UpdateTime)> Groups { get; set; } = new(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Author: DatHQ
            /// Description: Hai ảnh chụp mốc giống nhau khi mốc mặc định và mốc của mọi nhóm đều trùng.
            /// Created date: 01/11/2026
            /// </summary>
            public bool SameAs(CursorSnapshot other) =>
                Default == other.Default
                && Groups.Count == other.Groups.Count
                && Groups.All(g => other.Groups.TryGetValue(g.Key, out var o) && o == g.Value);
        }

        /// <summary>Dựng dấu vết dòng mốc mặc định (CursorKey = null) và các nhóm mốc có tên (CursorKey = &lt;nhóm&gt;) của đối tác × gói từ danh sách dòng đã đọc.</summary>
        private static CursorSnapshot BuildCursorSnapshot(IEnumerable<ShareDataLastSend> rows)
        {
            var list = rows as IList<ShareDataLastSend> ?? rows.ToList();
            var main = list.FirstOrDefault(c => string.IsNullOrEmpty(c.CursorKey));
            return new CursorSnapshot
            {
                Default = main == null ? null : (main.ID, main.UpdateTime),
                Groups = list
                    .Where(k => !string.IsNullOrEmpty(k.CursorKey))
                    .ToDictionary(k => (string)k.CursorKey!, k => (k.ID, k.UpdateTime), StringComparer.OrdinalIgnoreCase)
            };
        }

        /// <summary>
        /// Description: Quét và xử lý hàng loạt Subscription đến hạn xuất bản dữ liệu định kỳ.
        ///              Tìm đăng ký hợp lệ, nạp trước cấu hình tĩnh một lần cho cả vòng, nhận lock và điều phối vòng lặp xuất bản phân trang.
        /// Created date: 16/09/2026
        /// Modified date: 17/11/2026
        /// </summary>
        public async Task ProcessSubscriptions(CancellationToken cancelToken = default)
        {
            var opt = await workerConfig.Get(cancelToken);
            await using var scope = scopeFactory.CreateAsyncScope();
            var baseClient = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            using var db = baseClient.CopyNew();
            var now = db.GetDate();
            var batchWatch = Stopwatch.StartNew();
            var selfPartnerCode = configuration?["ShareData:SelfPartnerCode"]?.Trim();
            var hasSelfPartnerCode = !string.IsNullOrEmpty(selfPartnerCode);

            var subs = await db.Queryable<ShareDataSubscription>()
                .Where(s => s.IsDelete == null)
                .Where(s => s.Direction == BaseEnums.Direction.Outbound)
                .Where(s => SqlFunc.Subqueryable<ShareDataPartner>()
                    .Where(p => p.ID == s.PartnerId)
                    .Where(p => p.IsDelete == null)
                    .Where(p => p.Status == BaseEnums.StatusEnum.Enable)
                    .Where(p => p.SessionState == BaseEnums.SessionState.Connected)
                    .WhereIF(hasSelfPartnerCode, p => p.Code == selfPartnerCode).Any())
                .Where(s => s.State == BaseEnums.SubSubscriptionState.Active)
                // Dữ liệu cũ còn hai đăng ký cùng đối tác, chiều, gói tin đang bật: chỉ đăng ký tạo trước được xử lý.
                .Where(s => !SqlFunc.Subqueryable<ShareDataSubscription>()
                    .Where(o => o.IsDelete == null)
                    .Where(o => o.State == BaseEnums.SubSubscriptionState.Active)
                    .Where(o => o.PartnerId == s.PartnerId && o.Direction == s.Direction && o.DatatypeId == s.DatatypeId)
                    .Where(o => o.CreateTime < s.CreateTime).Any())
                .Where(s => s.NextTimeRun == null || s.NextTimeRun <= now)
                .Where(s => s.ProcessingUntil == null || s.ProcessingUntil <= now)
                .ToListAsync(cancelToken);

            if (subs.Count == 0)
            {
                logger.LogInformation("ℹ️ Không có Subscription nào đến hạn cần xử lý.");
                return;
            }

            // Static config (partner, packet, mapping, SQL, variables, codeSets) is loaded once per round as local variables; cursors and locks stay per subscription
            Dictionary<string, ShareDataPartner> partners;
            List<ShareDataPacket> packets;
            List<ShareDataMapping> mappings;
            List<ShareDataSql> sqls;
            List<ShareDataCodeSet> codeSets;
            try
            {
                (partners, packets, mappings, sqls, codeSets) = await LoadOutboundConfig(
                    db,
                    subs,
                    cancelToken
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "❌ [Gửi] Lỗi nạp cấu hình gửi cho {Count} Subscription đến hạn, bỏ qua vòng quét này: {Message}", subs.Count, ex.Message);
                return;
            }

            foreach (var sub in subs)
            {
                if (cancelToken.IsCancellationRequested)
                    break;

                var subNow = now.Add(batchWatch.Elapsed);
                using var subDb = baseClient.CopyNew();
                await ExportSubscription(
                    subDb,
                    sub,
                    subNow,
                    null,
                    opt,
                    cancelToken,
                    partners,
                    packets,
                    mappings,
                    sqls,
                    codeSets
                );
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Nạp cấu hình tĩnh chiều gửi cho danh sách đăng ký đến hạn, mỗi bảng một câu: đối tác theo PartnerId, toàn bộ gói tin chưa xóa,
        ///              hồ sơ ánh xạ chiều gửi đang bật của các đối tác, dòng ShareDataSql của các gói tin mà đăng ký phân giải được và bộ mã chuẩn hóa.
        ///              Không nạp mốc gửi hay trạng thái khóa của đăng ký.
        /// Created date: 12/11/2026
        /// Modified date: 17/11/2026
        /// </summary>
        private static async Task<(Dictionary<string, ShareDataPartner> Partners, List<ShareDataPacket> Packets, List<ShareDataMapping> Mappings, List<ShareDataSql> Sqls, List<ShareDataCodeSet> CodeSets)> LoadOutboundConfig(
            SqlSugarClient db,
            List<ShareDataSubscription> subs,
            CancellationToken cancelToken,
            List<ShareDataPacket>? preloadedPackets = null
        )
        {
            var partnerIds = subs
                .Select(s => s.PartnerId)
                .OfType<string>()
                .Where(id => id.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var partners = new Dictionary<string, ShareDataPartner>(StringComparer.OrdinalIgnoreCase);
            var mappings = new List<ShareDataMapping>();
            if (partnerIds.Count > 0)
            {
                var partnerRows = await db.Queryable<ShareDataPartner>()
                    .In(p => p.ID, partnerIds)
                    .Select(p => new ShareDataPartner
                    {
                        ID = p.ID,
                        Code = p.Code,
                        Name = p.Name,
                        Status = p.Status,
                        SessionState = p.SessionState,
                        Address = p.Address,
                        Port = p.Port,
                        EndPointApiUrl = p.EndPointApiUrl,
                        ConnectionType = p.ConnectionType,
                        ConnectionConfig = p.ConnectionConfig
                    })
                    .ToListAsync(cancelToken);
                foreach (var partnerRow in partnerRows)
                {
                    partners.TryAdd(partnerRow.ID, partnerRow);
                }

                mappings = await db.Queryable<ShareDataMapping>()
                    .Where(m => m.IsDelete == null
                        && m.IsActive == true
                        && (m.Direction == BaseEnums.Direction.Outbound || m.Direction == BaseEnums.Direction.Both))
                    .In(m => m.PartnerId, partnerIds)
                    .ToListAsync(cancelToken);
            }

            var packets = preloadedPackets ?? await db.Queryable<ShareDataPacket>()
                .Where(p => p.IsDelete == null)
                .ToListAsync(cancelToken);

            var packetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sub in subs)
            {
                var subPacket = PickActivePacket(packets, sub.DatatypeId);
                if (!string.IsNullOrWhiteSpace(subPacket?.ID))
                    packetIds.Add(subPacket.ID);
            }

            var sqls = new List<ShareDataSql>();
            if (packetIds.Count > 0)
            {
                var packetIdList = packetIds.ToList();
                sqls = await db.Queryable<ShareDataSql>()
                    .Where(s => s.IsDelete == null)
                    .In(s => s.PacketId, packetIdList)
                    .ToListAsync(cancelToken);
            }

            var codeSets = mappings.Count > 0
                ? await db.Queryable<ShareDataCodeSet>()
                    .Where(c => c.IsDelete == null)
                    .ToListAsync(cancelToken)
                : [];

            return (partners, packets, mappings, sqls, codeSets);
        }

        /// <summary>
        /// Description: Xử lý tức thì các Subscription theo sự kiện có dữ liệu mới (SendOnNewData).
        ///              Nhận mã gói tin từ NATS, phân giải gói tin, nạp cấu hình tĩnh một lần cho cả lô, chiếm lock và kết xuất dữ liệu.
        /// Created date: 22/09/2026
        /// Modified date: 17/11/2026
        /// </summary>
        public async Task ProcessSubscriptions(string packetCode, CancellationToken cancelToken = default)
        {
            if (string.IsNullOrWhiteSpace(packetCode))
                return;

            var opt = await workerConfig.Get(cancelToken);
            await using var scope = scopeFactory.CreateAsyncScope();
            var baseClient = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            using var db = baseClient.CopyNew();

            var packets = await db.Queryable<ShareDataPacket>()
                .Where(p => p.IsDelete == null)
                .ToListAsync(cancelToken);
            var packet = PickActivePacket(packets, packetCode);
            if (packet == null)
            {
                logger.LogWarning("⚠️ Không phân giải được gói tin từ mã trigger [{PacketCode}] — bỏ qua lượt kích hoạt. Kiểm tra lại cấu hình bảng giám sát ShareDataSourceTable và bảng ShareDataPacket.", packetCode);
                return;
            }

            if (packet.Status == BaseEnums.StatusEnum.Disable)
            {
                logger.LogInformation("ℹ️ Gói tin [{PacketCode}] đang ở trạng thái Tắt (Disable), bỏ qua lượt kích hoạt SendOnNewData.", packetCode);
                return;
            }

            var packetId = packet.ID;
            var resolvedCode = packet.Code ?? string.Empty;
            var now = db.GetDate();
            var batchWatch = Stopwatch.StartNew();
            var selfPartnerCode = configuration?["ShareData:SelfPartnerCode"]?.Trim();
            var hasSelfPartnerCode = !string.IsNullOrEmpty(selfPartnerCode);

            var subs = await db.Queryable<ShareDataSubscription>()
                .Where(s => s.IsDelete == null)
                .Where(s => s.Direction == BaseEnums.Direction.Outbound)
                .Where(s => SqlFunc.Subqueryable<ShareDataPartner>()
                    .Where(p => p.ID == s.PartnerId)
                    .Where(p => p.IsDelete == null)
                    .Where(p => p.Status == BaseEnums.StatusEnum.Enable)
                    .Where(p => p.SessionState == BaseEnums.SessionState.Connected)
                    .WhereIF(hasSelfPartnerCode, p => p.Code == selfPartnerCode).Any())
                .Where(s => s.State == BaseEnums.SubSubscriptionState.Active)
                // Dữ liệu cũ còn hai đăng ký cùng đối tác, chiều, gói tin đang bật: chỉ đăng ký tạo trước được xử lý.
                .Where(s => !SqlFunc.Subqueryable<ShareDataSubscription>()
                    .Where(o => o.IsDelete == null)
                    .Where(o => o.State == BaseEnums.SubSubscriptionState.Active)
                    .Where(o => o.PartnerId == s.PartnerId && o.Direction == s.Direction && o.DatatypeId == s.DatatypeId)
                    .Where(o => o.CreateTime < s.CreateTime).Any())
                .Where(s => s.DatatypeId == packetId || s.DatatypeId == resolvedCode)
                .Where(s => s.SendOnNewData == true)
                .Where(s => s.ProcessingUntil == null || s.ProcessingUntil <= now)
                .ToListAsync(cancelToken);

            if (subs.Count == 0)
            {
                logger.LogInformation("ℹ️ Không có Subscription SendOnNewData nào đang active cho gói tin [{PacketCode}].", packetCode);
                return;
            }

            var dueSubs = subs.Where(sub => DataOutboundScheduler.IsValidTime(sub, now)).ToList();
            if (dueSubs.Count == 0)
            {
                return;
            }

            Dictionary<string, ShareDataPartner> partners;
            List<ShareDataMapping> mappings;
            List<ShareDataSql> sqls;
            List<ShareDataCodeSet> codeSets;
            try
            {
                (partners, packets, mappings, sqls, codeSets) = await LoadOutboundConfig(
                    db,
                    dueSubs,
                    cancelToken,
                    packets
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "❌ [Gửi] Lỗi nạp cấu hình gửi SendOnNewData cho {Count} Subscription gói [{PacketCode}], bỏ qua lượt này: {Message}", dueSubs.Count, packetCode, ex.Message);
                return;
            }

            logger.LogInformation("⚡ [SendOnNewData] Đang xử lý {Count} Subscription cho gói tin [{PacketCode}]", dueSubs.Count, packetCode);
            foreach (var sub in dueSubs)
            {
                if (cancelToken.IsCancellationRequested)
                    break;

                var subNow = now.Add(batchWatch.Elapsed);
                using var subDb = baseClient.CopyNew();
                await ExportSubscription(
                    subDb,
                    sub,
                    subNow,
                    packetCode,
                    opt,
                    cancelToken,
                    partners,
                    packets,
                    mappings,
                    sqls,
                    codeSets
                );
            }
        }

        /// <summary>
        /// Description: Kết xuất dữ liệu cho một Subscription: chiếm lock qua ProcessingUntil,
        ///              thực thi pipeline (phân giải gói, lọc, phân trang) và luôn nhả lock trong finally.
        ///              Có cấu hình nạp trước của vòng quét thì tra trong đó, không có (đường tín hiệu SendOnNewData) thì tra CSDL.
        ///              Nhận khóa lượt trước bỏ lại tới hết hạn (tiến trình bị tắt cứng) thì dùng số thứ tự kế tiếp.
        /// Created date: 16/09/2026
        /// Modified date: 15/11/2026
        /// </summary>
        private async Task ExportSubscription(
            SqlSugarClient db,
            ShareDataSubscription sub,
            DateTime now,
            string? packetCode,
            DataWorkerConfig config,
            CancellationToken cancelToken,
            Dictionary<string, ShareDataPartner>? partners = null,
            List<ShareDataPacket>? packets = null,
            List<ShareDataMapping>? mappings = null,
            List<ShareDataSql>? sqls = null,
            List<ShareDataCodeSet>? codeSets = null
        )
        {
            var settings = config;
            var runWatch = Stopwatch.StartNew();
            var interval = sub.IntervalSeconds is > 0
                ? sub.IntervalSeconds.Value
                : settings.OutboundDefaultIntervalSec;
            var lockDurationSeconds = Math.Max(
                settings.OutboundLockMinSec,
                interval * settings.OutboundLockIntervalMultiplier);
            var processingUntil = now.AddSeconds(lockDurationSeconds).TruncateToSeconds();
            var currentSerial = sub.SerialNbr;
            var isScheduled = packetCode == null;
            // Khóa lượt trước bỏ lại tới hết hạn (tiến trình bị tắt cứng, sập máy giữa lúc gửi và lúc lưu): gói số hiện tại có thể đã tới đối tác,
            // nên nhận khóa đó với số kế tiếp thay vì gửi lại số đối tác đã lưu rồi bị từ chối mãi. Chỉ nhận đúng trạng thái khóa lúc đọc đăng ký
            var takesLeftLock = sub.ProcessingUntil != null;
            var consumesLeftSerial = takesLeftLock && currentSerial != null;
            var nextSerial = currentSerial + 1;
            var lockedRows = await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.ProcessingUntil == processingUntil)
                .SetColumnsIF(consumesLeftSerial, s => s.SerialNbr == nextSerial)
                .Where(s => s.ID == sub.ID)
                .Where(s => s.State == BaseEnums.SubSubscriptionState.Active)
                .WhereIF(currentSerial == null, s => s.SerialNbr == null)
                .WhereIF(currentSerial != null, s => s.SerialNbr == currentSerial)
                .Where(s => s.ProcessingUntil == null || s.ProcessingUntil <= now)
                .WhereIF(takesLeftLock, s => s.ProcessingUntil != null)
                .WhereIF(!takesLeftLock, s => s.ProcessingUntil == null)
                .WhereIF(isScheduled, s => s.NextTimeRun == null || s.NextTimeRun <= now)
                .ExecuteCommandAsync(CancellationToken.None);

            if (lockedRows <= 0)
                return;

            var tag = ShareDataHelper.DescribeSubscription(sub, null);
            if (consumesLeftSerial)
            {
                sub.SerialNbr = nextSerial;
                logger.LogWarning("⚠️ [Gửi] {Tag}: lượt trước không nhả khóa (tiến trình bị tắt giữa chừng), lượt này gửi với số thứ tự kế tiếp {SerialNbr}.", tag, nextSerial);
            }

            ShareDataPartner? partner = null;
            (ShareDataPacket Packet, ShareDataMapping Mapping, ShareDataEnum.PacketPolicy Policy, string? PartnerSql, SqlVariableSet Variables)? packetConfig = null;
            var isRunSuccess = false;
            try
            {
                partner = partners != null
                    ? (string.IsNullOrEmpty(sub.PartnerId) ? null : partners.GetValueOrDefault(sub.PartnerId))
                    : string.IsNullOrEmpty(sub.PartnerId)
                    ? null
                    : await db.Queryable<ShareDataPartner>()
                        .Where(p => p.ID == sub.PartnerId)
                        .Select(p => new ShareDataPartner
                        {
                            ID = p.ID,
                            Code = p.Code,
                            Name = p.Name,
                            Status = p.Status,
                            SessionState = p.SessionState,
                            Address = p.Address,
                            Port = p.Port,
                            EndPointApiUrl = p.EndPointApiUrl,
                            ConnectionType = p.ConnectionType,
                            ConnectionConfig = p.ConnectionConfig
                        })
                        .FirstAsync(cancelToken);

                tag = ShareDataHelper.DescribeSubscription(sub, partner);
                var userTag = ShareDataHelper.DescribeSubscriptionForUser(sub, partner);

                packetConfig = await GetExportConfig(db, sub, tag, userTag, cancelToken, packets, mappings, sqls);
                if (packetConfig == null)
                    return;

                var (packet, mapping, policy, sql, variables) = packetConfig.Value;
                var isIncremental = policy == ShareDataEnum.PacketPolicy.AlwaysIncremental;
                var cursorSignature = isIncremental ? SqlValidatorHelper.GetCursorSignature(sql) : null;
                // Câu gộp nhiều nguồn: mỗi nhóm mốc có tên giữ mốc riêng ở ShareDataLastSend (CursorKey = <nhóm>)
                var groupSignatures = isIncremental
                    ? variables.CursorGroups.ToDictionary(g => g, g => SqlValidatorHelper.GetCursorSignature(sql, g), StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                var usesDefaultCursor = groupSignatures.Count == 0 || SqlValidatorHelper.IsIncrementalSql(sql, variables.DefaultCursorNames);
                string? currentLastKey = null;
                IReadOnlyDictionary<string, string?> currentGroups = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                var ctx = new DataOutboundContext(
                    sub,
                    partner,
                    packet,
                    mapping,
                    now,
                    cancelToken,
                    policy,
                    sql,
                    variables,
                    cursorSignature,
                    groupSignatures,
                    usesDefaultCursor,
                    codeSets
                );

                // Câu SQL có thể được nạp thẳng vào CSDL (seed, sửa tay) mà không qua bộ kiểm lúc lưu: kiểm lại cấu trúc nối đuôi trước khi đọc mốc.
                // Câu sai thì lượt này không chạy, nên không tạo dòng mốc và không báo lệch công thức
                var isStructureValid = !isIncremental || await ValidateIncrementalSqlAsync(db, ctx, sql, variables);

                List<ShareDataLastSend>? cursorRows = null;
                if (isIncremental && isStructureValid && !string.IsNullOrWhiteSpace(partner?.Code) && !string.IsNullOrWhiteSpace(packet.Code))
                {
                    var safePartner = partner.Code.Trim();
                    var safePacket = packet.Code.Trim();
                    cursorRows = await db.Queryable<ShareDataLastSend>()
                        .Where(c => c.PartnerCode == safePartner && c.PacketCode == safePacket)
                        .ToListAsync(cancelToken);
                }

                if (isIncremental && isStructureValid && usesDefaultCursor)
                {
                    var cursorNow = now.Add(runWatch.Elapsed);
                    var lastSend = await GetLastSend(
                        db,
                        tag,
                        partner,
                        packet.Code,
                        cancelToken,
                        cursorSignature,
                        sub,
                        cursorRows,
                        cursorNow
                    );

                    if (lastSend == null)
                        return;

                    currentLastKey = lastSend.Value.LastKey;
                }

                if (isIncremental && isStructureValid && groupSignatures.Count > 0)
                {
                    var groupCursors = await GetGroupCursors(
                        db,
                        tag,
                        partner,
                        packet.Code,
                        groupSignatures,
                        cancelToken,
                        sub,
                        cursorRows
                    );
                    if (groupCursors == null)
                        return;

                    currentGroups = groupCursors;
                }

                var totalRecords = 0;

                if (!isStructureValid)
                {
                    isRunSuccess = false;
                }
                else
                {
                    if (isIncremental)
                    {
                        await LogCursorRisks(db, ctx, sql, variables, tag);
                    }

                    // Chụp dòng mốc đầu lượt: Đặt lại / Đặt mốc từ màn hình trong lúc lượt đang chạy thì Worker không ghi đè (CommitSuccess)
                    var cursorSnapshot = cursorRows != null ? BuildCursorSnapshot(cursorRows) : null;

                    var page = await ExportPage(
                        db,
                        ctx,
                        isIncremental,
                        processingUntil,
                        tag,
                        0,
                        currentLastKey,
                        currentGroups,
                        cursorSnapshot,
                        runWatch
                    );

                    isRunSuccess = page.Success;
                    totalRecords = page.RecordCount;
                    currentLastKey = page.LastKey;
                    currentGroups = page.Groups;
                }
            }
            catch (ShareDataException ex)
            {
                logger.LogWarning(ex, "⚠️ [{AlertCode}] {Tag}: {Message}", ex.AlertCode, tag, ex.Message);
                await ShareDataTransferLog.WriteFailureLogs(
                    db,
                    sub,
                    partner,
                    ex.AlertCode,
                    ex.Severity,
                    ex.AlertSource,
                    ex.Message,
                    mappingId: packetConfig?.Mapping.ID,
                    packetVersion: packetConfig?.Packet.PacketVersion);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Lỗi gốc của máy chủ CSDL thường nêu tên bảng, cột: chỉ ghi ở nhật ký máy chủ, nhật ký giao diện dùng câu chung.
                logger.LogError(ex, "❌ [Gửi] Lỗi kết xuất {Tag}: {Message}", tag, ex.Message);
                await ShareDataTransferLog.WriteFailureLogs(
                    db,
                    sub,
                    partner,
                    ShareDataAlertCode.Outbound.QueryFailed,
                    BaseEnums.AlertSeverity.Error,
                    BaseEnums.AlertSource.Subscription,
                    SafeErrorMessage.ForLog(ex),
                    mappingId: packetConfig?.Mapping.ID,
                    packetVersion: packetConfig?.Packet.PacketVersion);
            }
            finally
            {
                var releaseNow = now.Add(runWatch.Elapsed);
                await ReleaseLock(
                    db,
                    sub,
                    processingUntil,
                    tag,
                    logger,
                    isRunSuccess,
                    settings.OutboundDefaultIntervalSec,
                    CancellationToken.None,
                    releaseNow
                );
            }
        }

        /// <summary>
        /// Description: Phân giải cấu hình gói tin active theo DatatypeId, kiểm tra chính sách OutboundPolicy
        ///              và lấy phễu lọc ShareDataMapping bắt buộc. Trả về null nếu bỏ qua xuất bản.
        ///              Có cấu hình nạp trước thì chọn dòng trong đó theo cặp (đối tác, gói tin), cùng hàm chọn với đường tra CSDL.
        /// Created date: 26/09/2026
        /// Modified date: 12/11/2026
        /// </summary>
        private async Task<(ShareDataPacket Packet, ShareDataMapping Mapping, ShareDataEnum.PacketPolicy Policy, string? PartnerSql, SqlVariableSet Variables)?> GetExportConfig(
            SqlSugarClient db,
            ShareDataSubscription sub,
            string tag,
            string userTag,
            CancellationToken cancelToken,
            List<ShareDataPacket>? packets = null,
            List<ShareDataMapping>? mappings = null,
            List<ShareDataSql>? sqls = null)
        {
            var packet = packets != null
                ? PickActivePacket(packets, sub.DatatypeId)
                : await HasActivePacket(db, sub.DatatypeId, cancelToken);
            if (packet == null)
            {
                var packetNotFoundMsg = $"Không tìm thấy gói tin đang hoạt động cho đăng ký của {userTag}.";
                throw new ShareDataException(
                    ShareDataAlertCode.Outbound.PacketNotFound,
                    BaseEnums.AlertSeverity.Error,
                    BaseEnums.AlertSource.Subscription,
                    packetNotFoundMsg);
            }

            if (packet.Status == BaseEnums.StatusEnum.Disable)
            {
                logger.LogInformation("ℹ️ Gói tin [{PacketCode}] đang ở trạng thái Tắt (Disable), bỏ qua xuất bản ({Tag}).", packet.Code, tag);
                return null;
            }

            ShareDataMapping? mapping = mappings != null
                ? SqlTextHelper.PickActiveOutboundMapping(mappings, sub.PartnerId, packet)
                : await SqlTextHelper.SelectActiveOutboundMapping(db, sub.PartnerId, packet, cancelToken);

            if (mapping == null)
            {
                var mappingNotFoundMsg = $"Không tìm thấy hồ sơ ánh xạ chiều gửi đang bật cho gói tin [{packet.Code}] của {userTag}. Hãy tạo hoặc bật hồ sơ ánh xạ chiều gửi cho đối tác này.";
                throw new ShareDataException(
                    ShareDataAlertCode.Outbound.MappingNotFound,
                    BaseEnums.AlertSeverity.Error,
                    BaseEnums.AlertSource.Subscription,
                    mappingNotFoundMsg);
            }

            // Câu SQL hiệu lực: dòng riêng của đối tác trong ShareDataSql, trống thì theo dòng mẫu chung
            var sqlConfig = sqls != null
                ? SqlTextHelper.PickSqlConfig(sqls, packet, sub.PartnerId)
                : await SqlTextHelper.ResolveSqlConfig(db, packet, sub.PartnerId, cancelToken);
            var sql = sqlConfig.ExtractSql;
            // Biến ở bảng tham số câu truy vấn: biến lấy từ mốc lần gửi trước cũng làm câu SQL thành truy vấn nối đuôi
            var variables = sqls != null
                ? SqlTextHelper.PickVariables(sqls, packet.ID, sub.PartnerId)
                : await SqlTextHelper.ResolveVariables(db, packet.ID, sub.PartnerId, cancelToken);
            var policy = PacketHelper.GetPolicyFromSql(sql, variables.CursorNames);

            return (packet, mapping, policy, sql, variables);
        }

        /// <summary>
        /// Description: Lấy hoặc khởi tạo mốc LastSend cho lượt xuất bản nối đuôi (incremental).
        ///              Trả về null nếu mã đối tác không hợp lệ để tránh tạo checkpoint rác.
        ///              Mốc (LastKey có giá trị) ghi theo công thức cột mốc khác công thức hiện tại (cursorSignature) thì trả mốc trống và ghi cảnh báo ESH-1308.
        ///              Dòng mới tạo để LastTime trống: cột này là giờ ghi mốc, chỉ có khi gửi thành công.
        /// Created date: 26/09/2026
        /// Modified date: 03/11/2026
        /// </summary>
        private async Task<(DateTime? LastTime, string? LastKey)?> GetLastSend(
            SqlSugarClient db,
            string tag,
            ShareDataPartner? partner,
            string? packetCode,
            CancellationToken cancelToken,
            string? cursorSignature = null,
            ShareDataSubscription? sub = null,
            List<ShareDataLastSend>? cursorRows = null,
            DateTime? dbNow = null
        )
        {
            if (string.IsNullOrWhiteSpace(partner?.Code))
            {
                logger.LogWarning("⚠️ Không xác định được mã đối tác (PartnerCode) hợp lệ cho {Tag}. Bỏ qua xuất bản incremental để tránh tạo checkpoint rác.", tag);
                return null;
            }

            if (string.IsNullOrWhiteSpace(packetCode))
                throw new ShareDataException(
                    ShareDataAlertCode.Outbound.RequiredFieldMissing,
                    BaseEnums.AlertSeverity.Error,
                    BaseEnums.AlertSource.Subscription,
                    "PacketCode không được để trống khi khởi tạo hoặc truy vấn ShareDataLastSend.");

            var safePartnerCode = partner.Code.Trim();
            var safePacketCode = packetCode.Trim();

            var existing = cursorRows != null
                ? cursorRows.FirstOrDefault(c => string.IsNullOrEmpty(c.CursorKey))
                : await db.Queryable<ShareDataLastSend>()
                    .Where(c => c.PartnerCode == safePartnerCode)
                    .Where(c => c.PacketCode == safePacketCode)
                    .Where(c => c.CursorKey == null)
                    .FirstAsync(cancelToken);

            if (existing != null)
            {
                // Công thức cột mốc đã đổi: mốc lưu theo công thức cũ không so được với công thức mới,
                // coi như chưa có mốc để gửi lại từ mốc khởi tạo thay vì bỏ sót hoặc đứng im; lần ghi mốc kế tiếp ghi đè cả chữ ký.
                // Dòng chưa có LastKey thì không có mốc để bỏ, không cảnh báo.
                if (!string.IsNullOrEmpty(existing.LastKey) && existing.CursorSignature != null && cursorSignature != null && existing.CursorSignature != cursorSignature)
                {
                    logger.LogWarning(
                        "⚠️ Công thức cột mốc của {Tag} đã đổi: bỏ mốc cũ ({LastKey}), gửi lại từ mốc khởi tạo.",
                        tag,
                        existing.LastKey);
                    await WarnCursorFormulaChanged(db, sub, partner, safePacketCode, null, cursorSignature, existing.LastKey);
                    return (null, null);
                }
                return (existing.LastTime, existing.LastKey);
            }

            var now = dbNow ?? db.GetDate();
            var newLastSend = new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = safePartnerCode,
                PacketCode = safePacketCode,
                CursorKey = null,
                LastKey = null,
                CursorSignature = cursorSignature,
                CreateTime = now,
                UpdateTime = now
            };

            try
            {
                await db.Insertable(newLastSend).ExecuteCommandAsync(cancelToken);
                if (cursorRows != null)
                {
                    var inserted = await db.Queryable<ShareDataLastSend>()
                        .Where(c => c.ID == newLastSend.ID)
                        .FirstAsync(cancelToken);
                    cursorRows.Add(inserted ?? newLastSend);
                }
                return (newLastSend.LastTime, newLastSend.LastKey);
            }
            catch (Exception ex)
            {
                var raceLastSend = await db.Queryable<ShareDataLastSend>()
                    .Where(c => c.PartnerCode == safePartnerCode)
                    .Where(c => c.PacketCode == safePacketCode)
                    .Where(c => c.CursorKey == null)
                    .FirstAsync(cancelToken);

                if (raceLastSend == null)
                    throw new ShareDataException(
                        ShareDataAlertCode.Outbound.QueryFailed,
                        BaseEnums.AlertSeverity.Error,
                        BaseEnums.AlertSource.Subscription,
                        $"Không thể khởi tạo hoặc truy xuất bản ghi ShareDataLastSend cho đối tác [{safePartnerCode}], gói [{safePacketCode}]: {SafeErrorMessage.ForLog(ex)}",
                        ex);

                cursorRows?.Add(raceLastSend);
                return (raceLastSend.LastTime, raceLastSend.LastKey);
            }
        }

        /// <summary>
        /// Description: Mốc của các nhóm mốc có tên (ShareDataLastSend có CursorKey) cho lượt xuất nối đuôi của câu gộp nhiều nguồn. Nhóm chưa có mốc,
        ///              hoặc mốc ghi theo công thức khác (chữ ký khác), thì để trống để câu dùng mốc khởi tạo của biến.
        ///              Trả về null nếu mã đối tác không hợp lệ.
        /// Created date: 29/10/2026
        /// Modified date: 17/11/2026
        /// </summary>
        private async Task<IReadOnlyDictionary<string, string?>?> GetGroupCursors(
            SqlSugarClient db,
            string tag,
            ShareDataPartner? partner,
            string? packetCode,
            IReadOnlyDictionary<string, string?> groupSignatures,
            CancellationToken cancelToken,
            ShareDataSubscription? sub = null,
            List<ShareDataLastSend>? cursorRows = null
        )
        {
            if (string.IsNullOrWhiteSpace(partner?.Code))
            {
                logger.LogWarning("⚠️ Không xác định được mã đối tác (PartnerCode) hợp lệ cho {Tag}. Bỏ qua xuất bản incremental để tránh tạo checkpoint rác.", tag);
                return null;
            }

            var safePartnerCode = partner.Code.Trim();
            var safePacketCode = packetCode?.Trim();
            var rows = cursorRows != null
                ? cursorRows.Where(k => !string.IsNullOrEmpty(k.CursorKey)).ToList()
                : await db.Queryable<ShareDataLastSend>()
                    .Where(k => k.PartnerCode == safePartnerCode && k.PacketCode == safePacketCode && k.CursorKey != null)
                    .ToListAsync(cancelToken);

            var cursors = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (group, signature) in groupSignatures)
            {
                var row = rows.FirstOrDefault(r => string.Equals(r.CursorKey, group, StringComparison.OrdinalIgnoreCase));
                if (row == null)
                    continue;

                if (!string.IsNullOrEmpty(row.LastKey) && row.CursorSignature != null && signature != null && row.CursorSignature != signature)
                {
                    logger.LogWarning(
                        "⚠️ Công thức cột mốc nhóm {Group} của {Tag} đã đổi: bỏ mốc cũ ({LastKey}), gửi lại từ mốc khởi tạo.",
                        group,
                        tag,
                        row.LastKey);
                    await WarnCursorFormulaChanged(db, sub, partner, safePacketCode, group, signature, row.LastKey);
                    continue;
                }

                cursors[group] = row.LastKey;
            }

            return cursors;
        }

        /// <summary>
        /// Description: Kết xuất dữ liệu cho một lượt: Extract -> Map -> Transport -> Commit
        ///              và tịnh tiến cursor (kể cả mốc của từng nhóm mốc có tên).
        ///              Gửi xong thì phần ghi sau khi gửi không dừng theo token dịch vụ; lưu số thứ tự, mốc lỗi thì vẫn tăng số thứ tự.
        /// Created date: 26/09/2026
        /// Modified date: 15/11/2026
        /// </summary>
        private async Task<(bool Success, int RecordCount, string? LastKey, IReadOnlyDictionary<string, string?> Groups)> ExportPage(
            SqlSugarClient db,
            DataOutboundContext ctx,
            bool isIncremental,
            DateTime processingUntil,
            string tag,
            int pageNumber,
            string? currentLastKey,
            IReadOnlyDictionary<string, string?> currentGroups,
            CursorSnapshot? cursorSnapshot = null,
            Stopwatch? runWatch = null
        )
        {
            var extraction = await extractionProcess.Extract(
                db,
                ctx.Subscription,
                ctx.Packet,
                isIncremental ? currentLastKey : null,
                (msg, args) => logger.LogWarning(msg, args),
                ctx.CancelToken,
                policy: ctx.Policy,
                partnerSql: ctx.PartnerSql,
                variables: ctx.Variables,
                groupCursors: currentGroups);

            if (extraction.RawRows.Count == 0)
            {
                if (pageNumber == 0)
                {
                    await ShareDataTransferLog.WriteActivityAsync(
                        db, ctx.Subscription, ctx.Partner,
                        BaseEnums.TransferDirection.SND,
                        recordCount: 0, byteSize: 0, filePath: null,
                        status: ShareDataEnum.ExportStatus.Success,
                        errorMessage: ShareDataEnum.ExportMessage.NoNewData,
                        mappingId: ctx.Mapping?.ID,
                        packetVersion: ctx.Packet.PacketVersion,
                        serialNbr: ctx.Subscription.SerialNbr);
                }
                return (true, 0, currentLastKey, currentGroups);
            }

            var pageLog = new PageLog(
                Guid.NewGuid().ToString("N"),
                extraction.RawRows.Count,
                ctx.Mapping?.ID,
                ctx.Packet.PacketVersion,
                ctx.Subscription.SerialNbr);

            await ShareDataTransferLog.WriteActivityAsync(
                db, ctx.Subscription, ctx.Partner, BaseEnums.TransferDirection.SND,
                recordCount: pageLog.RecordCount, byteSize: 0, filePath: null,
                status: ShareDataEnum.ExportStatus.Queued,
                mappingId: pageLog.MappingId, packetVersion: pageLog.PacketVersion,
                serialNbr: pageLog.SerialNbr,
                logId: pageLog.ParentId,
                description: $"Bắt đầu kết xuất {pageLog.RecordCount} bản ghi gói tin [{ctx.PacketCode}]");

            if (!await ValidateCursor(db, ctx, isIncremental, extraction, pageLog))
                return (false, 0, currentLastKey, currentGroups);

            var mapResult = await MapPage(db, extraction, ctx, pageNumber, pageLog);
            if (mapResult == null)
                return (false, 0, currentLastKey, currentGroups);

            var sendResult = await transport.Send(mapResult, ctx, ctx.CancelToken);
            // Từ đây gói có thể đã tới đối tác: nhật ký, số thứ tự, mốc phải ghi xong dù dịch vụ đang dừng, không thì lần chạy sau gửi lại số cũ và bị từ chối mãi.
            // SqlSugar dùng lại token của câu trước cho các câu không truyền token nên phải gỡ ở đây
            db.Ado.CancellationToken = CancellationToken.None;
            var sentByteSize = (int)sendResult.ByteSize;

            await TryLogConfigWarning(db, ctx, sendResult.Warnings);

            if (!sendResult.Success)
            {
                var vendorCode = ctx.Partner?.Code ?? ctx.Partner?.Name ?? ctx.Subscription.PartnerId ?? ShareDataEnum.SystemConstants.Unknown;
                var failureMessage = sendResult.ErrorMessage ?? $"Gửi dữ liệu tới đối tác [{vendorCode}] thất bại.";
                if (sendResult.IsPossiblyDelivered)
                {
                    var serialConsumed = await ConsumeSentSerial(db, ctx);
                    if (serialConsumed)
                    {
                        failureMessage = $"{failureMessage} Không rõ đối tác đã nhận gói hay chưa nên lượt sau gửi lại các dòng này với số thứ tự kế tiếp.";
                    }
                }

                await ShareDataTransferLog.WriteFailureLogs(
                    db,
                    ctx.Subscription,
                    ctx.Partner,
                    sendResult.AlertCode ?? ShareDataAlertCode.Outbound.HttpSendFailed,
                    BaseEnums.AlertSeverity.Warning,
                    BaseEnums.AlertSource.Protocol,
                    failureMessage,
                    detailJson: sendResult.DetailJson,
                    recordCount: pageLog.RecordCount,
                    byteSize: sentByteSize,
                    filePath: sendResult.RelativePath,
                    mappingId: pageLog.MappingId,
                    packetVersion: pageLog.PacketVersion,
                    parentId: pageLog.ParentId,
                    stepNbr: 3);
                return (false, 0, currentLastKey, currentGroups);
            }

            var newKey = isIncremental ? extraction.LastKey : null;
            var successDesc = $"Gửi thành công {pageLog.RecordCount} bản ghi cho đối tác";

            DateTime? commitNow = runWatch != null ? ctx.ExportedAt.Add(runWatch.Elapsed) : null;
            var commitTask = CommitPage(
                db,
                ctx,
                isIncremental,
                extraction,
                processingUntil,
                ctx.ExportedAt,
                newKey,
                tag,
                pageNumber,
                sentByteSize,
                sendResult.RelativePath,
                successDesc,
                pageLog,
                logger,
                cursorSnapshot,
                commitNow
            );

            bool commitOk;
            try
            {
                commitOk = await commitTask;
            }
            catch
            {
                // Đối tác đã lưu gói mà lưu số thứ tự, mốc lỗi (mất kết nối, hết giờ, deadlock): tăng riêng số thứ tự rồi báo lỗi như cũ, lượt sau gửi lại
                // các dòng này với số kế tiếp thay vì gửi lại số đối tác đã lưu rồi bị từ chối mãi. Kết nối của lượt có thể đã hỏng nên dùng kết nối mới
                using var serialDb = db.CopyNew();
                await ConsumeSentSerial(serialDb, ctx);
                throw;
            }

            if (!commitOk)
                return (false, 0, currentLastKey, currentGroups);

            var nextGroups = new Dictionary<string, string?>(currentGroups, StringComparer.OrdinalIgnoreCase);
            foreach (var (group, position) in extraction.Groups ?? new Dictionary<string, CursorGroupPosition>())
            {
                nextGroups[group] = position.LastKey;
            }

            return (true, extraction.RawRows.Count, newKey, nextGroups);
        }

        /// <summary>
        /// Description: Commit kết quả gửi thành công một trang: Cập nhật Subscription theo cơ chế OCC
        ///              qua ProcessingUntil và cập nhật mốc LastSend tăng dần trong cùng transaction.
        ///              Khóa số nguyên / GUID so đúng kiểu như câu SQL; công thức cột mốc đổi (chữ ký khác) thì ghi đè mốc cũ.
        ///              Mọi câu đọc mốc đứng trước câu ghi đầu tiên hoặc sau khi commit: đọc lúc đang giữ khóa ghi dễ deadlock với Worker khác ghi mốc cùng lúc.
        ///              Giao dịch bị máy chủ hủy (deadlock, mất kết nối) thì bỏ giao dịch hỏng để kết nối còn dùng tiếp, ném lại lỗi gốc.
        /// Created date: 21/09/2026
        /// Modified date: 15/11/2026
        /// </summary>
        private static async Task<(bool Committed, bool CursorAdvanced)> CommitSuccess(
            SqlSugarClient db,
            ShareDataSubscription sub,
            DateTime processingUntil,
            string partnerCode,
            string packetCode,
            bool isIncremental,
            DateTime newTime,
            string? newKey,
            object? newKeyValue,
            string? cursorSignature,
            bool usesDefaultCursor,
            IReadOnlyDictionary<string, CursorGroupPosition>? groups,
            IReadOnlyDictionary<string, string?>? groupSignatures,
            CancellationToken cancelToken,
            CursorSnapshot? cursorSnapshot = null,
            DateTime? commitTime = null
        )
        {
            var now = commitTime ?? db.GetDate();
            // Ghi mốc mới vào ShareDataLastSend, trả về số dòng đã cập nhật
            async Task<int> UpdateLastSend(ShareDataLastSend current)
            {
                var nt = newTime;
                var update = db.Updateable<ShareDataLastSend>()
                    .SetColumns(c => c.LastTime == nt)
                    .SetColumns(c => c.LastKey == newKey)
                    .SetColumns(c => c.CursorSignature == cursorSignature)
                    .SetColumns(c => c.UpdateTime == now)
                    .Where(c => c.ID == current.ID)
                    .Where(c => c.PartnerCode == partnerCode)
                    .Where(c => c.PacketCode == packetCode)
                    .Where(c => c.CursorKey == null);

                // Công thức cột mốc đã đổi: mốc cũ không so được với mốc mới (lượt này đã gửi lại từ mốc khởi tạo) nên ghi đè
                if (current.CursorSignature != null && cursorSignature != null && current.CursorSignature != cursorSignature)
                {
                    return await update.ExecuteCommandAsync(cancelToken);
                }

                // Khóa kiểu số nguyên hoặc GUID: câu SQL so theo kiểu đó, so chuỗi sẽ coi '18' nhỏ hơn '9' làm mốc đứng im
                if (IsTypedKey(newKeyValue))
                {
                    return IsKeyAfter(current.LastKey, newKeyValue!) ? await update.ExecuteCommandAsync(cancelToken) : 0;
                }

                update = newKey != null
                    ? update.Where(c => c.LastKey == null || c.LastKey.CompareTo(newKey) < 0)
                    : update.Where(c => c.LastKey == null);
                return await update.ExecuteCommandAsync(cancelToken);
            }

            var hasCursorRows = isIncremental && !string.IsNullOrWhiteSpace(partnerCode) && !string.IsNullOrWhiteSpace(packetCode);
            var cursorAdvanced = false;
            var tran = db.Ado;
            try
            {
                await tran.BeginTranAsync();

                // Dòng mốc khác dấu vết Worker giữ (Đặt lại / Đặt mốc từ màn hình trong lúc lượt đang chạy, kể cả giữa hai trang): không ghi đè
                List<ShareDataLastSend> storedCursors = [];
                if (hasCursorRows)
                {
                    storedCursors = await db.Queryable<ShareDataLastSend>()
                        .Where(c => c.PartnerCode == partnerCode && c.PacketCode == packetCode)
                        .ToListAsync(cancelToken);
                }

                var latestSnapshot = BuildCursorSnapshot(storedCursors);
                var cursorInterrupted = hasCursorRows && cursorSnapshot != null && !cursorSnapshot.SameAs(latestSnapshot);
                var current = !cursorInterrupted && hasCursorRows && usesDefaultCursor
                    ? storedCursors.FirstOrDefault(c => string.IsNullOrEmpty(c.CursorKey))
                    : null;
                var currentGroups = !cursorInterrupted && hasCursorRows && groups is { Count: > 0 }
                    ? storedCursors.Where(c => !string.IsNullOrEmpty(c.CursorKey)).ToList()
                    : [];

                if (cursorInterrupted)
                {
                    var subAffected = await db.Updateable<ShareDataSubscription>()
                        .SetColumns(s => s.SerialNbr == SqlFunc.IsNull(s.SerialNbr, 0) + 1)
                        .SetColumns(s => s.UpdateTime == now)
                        .Where(s => s.ID == sub.ID)
                        .Where(s => s.ProcessingUntil == processingUntil)
                        .ExecuteCommandAsync();
                    if (subAffected <= 0)
                    {
                        await tran.RollbackTranAsync();
                        return (false, false);
                    }

                    await tran.CommitTranAsync();
                    return (true, false);
                }

                var subAffectedNormal = await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.LastTimeRun == newTime)
                    .SetColumns(s => s.SerialNbr == SqlFunc.IsNull(s.SerialNbr, 0) + 1)
                    .SetColumns(s => s.UpdateTime == now)
                    .Where(s => s.ID == sub.ID)
                    .Where(s => s.ProcessingUntil == processingUntil)
                    .ExecuteCommandAsync();
                if (subAffectedNormal <= 0)
                {
                    await tran.RollbackTranAsync();
                    return (false, false);
                }

                if (isIncremental && usesDefaultCursor && !string.IsNullOrWhiteSpace(partnerCode) && !string.IsNullOrWhiteSpace(packetCode))
                {
                    if (current == null)
                    {
                        await db.Insertable(new ShareDataLastSend
                        {
                            ID = Guid.NewGuid().ToString("N"),
                            PartnerCode = partnerCode,
                            PacketCode = packetCode,
                            CursorKey = null,
                            LastTime = newTime,
                            LastKey = newKey,
                            CursorSignature = cursorSignature,
                            CreateTime = now,
                            UpdateTime = now
                        }).ExecuteCommandAsync(cancelToken);
                        cursorAdvanced = true;
                    }
                    else
                    {
                        var affected = await UpdateLastSend(current);
                        cursorAdvanced = affected > 0;
                    }
                }

                if (isIncremental && groups is { Count: > 0 } && !string.IsNullOrWhiteSpace(partnerCode) && !string.IsNullOrWhiteSpace(packetCode))
                {
                    foreach (var (group, position) in groups)
                    {
                        string? groupSignature = null;
                        groupSignatures?.TryGetValue(group, out groupSignature);
                        var currentGroup = currentGroups.FirstOrDefault(r => string.Equals(r.CursorKey, group, StringComparison.OrdinalIgnoreCase));
                        if (await CommitGroupCursor(db, partnerCode, packetCode, group, position, groupSignature, now, currentGroup, cancelToken))
                        {
                            cursorAdvanced = true;
                        }
                    }
                }

                await tran.CommitTranAsync();
            }
            catch
            {
                try
                {
                    await tran.RollbackTranAsync();
                }
                catch
                {
                    // Máy chủ đã tự hủy giao dịch (deadlock, mất kết nối) nên hoàn tác báo lỗi: bỏ giao dịch hỏng để kết nối của lượt còn ghi nhật ký,
                    // nhả khóa được, rồi ném lại lỗi gốc
                    tran.Transaction = null;
                    tran.Close();
                }

                throw;
            }

            return (true, cursorAdvanced);
        }

        /// <summary>
        /// Description: Ghi mốc của một nhóm mốc có tên (ShareDataLastSend có CursorKey) trong giao dịch ghi mốc của trang. Chốt chặn như nhóm mặc định:
        ///              mốc chỉ tiến, khóa số nguyên / GUID so đúng kiểu, công thức cột mốc đổi (chữ ký khác) thì ghi đè. Trả về true khi mốc tiến.
        /// Created date: 29/10/2026
        /// Modified date: 03/11/2026
        /// </summary>
        private static async Task<bool> CommitGroupCursor(
            SqlSugarClient db,
            string partnerCode,
            string packetCode,
            string group,
            CursorGroupPosition position,
            string? signature,
            DateTime now,
            ShareDataLastSend? current,
            CancellationToken cancelToken)
        {
            if (current == null)
            {
                await db.Insertable(new ShareDataLastSend
                {
                    ID = Guid.NewGuid().ToString("N"),
                    PartnerCode = partnerCode,
                    PacketCode = packetCode,
                    CursorKey = group,
                    LastKey = position.LastKey,
                    CursorSignature = signature,
                    CreateTime = now,
                    UpdateTime = now
                }).ExecuteCommandAsync(cancelToken);
                return true;
            }

            var currentId = current.ID;
            var newKey = position.LastKey;
            var update = db.Updateable<ShareDataLastSend>()
                .SetColumns(k => k.LastKey == newKey)
                .SetColumns(k => k.CursorSignature == signature)
                .SetColumns(k => k.UpdateTime == now)
                .Where(k => k.ID == currentId);

            if (current.CursorSignature != null && signature != null && current.CursorSignature != signature)
            {
                return await update.ExecuteCommandAsync(cancelToken) > 0;
            }

            if (IsTypedKey(position.LastKeyValue))
            {
                return IsKeyAfter(current.LastKey, position.LastKeyValue!) && await update.ExecuteCommandAsync(cancelToken) > 0;
            }

            return await update.Where(k => k.LastKey == null || k.LastKey.CompareTo(newKey) < 0).ExecuteCommandAsync(cancelToken) > 0;
        }

        /// <summary>
        /// Description: Nhả lock cho Subscription ở cuối phiên chạy: xóa ProcessingUntil,
        ///              cập nhật State và tính NextTimeRun tiếp theo trong khối try/catch an toàn.
        /// Created date: 21/09/2026
        /// </summary>
        private static async Task ReleaseLock(
            SqlSugarClient db,
            ShareDataSubscription sub,
            DateTime processingUntil,
            string tag,
            ILogger logger,
            bool isCompleted,
            int defaultIntervalSeconds,
            CancellationToken cancelToken = default,
            DateTime? releaseTime = null
        )
        {
            try
            {
                var isSingle = sub.Mode == BaseEnums.SubMode.Single;
                var dbNow = releaseTime ?? db.GetDate();
                var nextRun = DataOutboundScheduler.ComputeNextTimeRun(
                    sub,
                    dbNow,
                    defaultIntervalSeconds
                );
                // Chỉ đổi trạng thái khi đăng ký chạy một lần đã xong: người dùng tạm dừng trong lúc lượt đang chạy thì giữ nguyên, không bật lại
                var shouldClearNextRun = isSingle && isCompleted;
                var affected = await db.Updateable<ShareDataSubscription>()
                    .SetColumnsIF(shouldClearNextRun, x => x.State == BaseEnums.SubSubscriptionState.Expired)
                    .SetColumns(x => x.ProcessingUntil == null)
                    .SetColumnsIF(!shouldClearNextRun, x => x.NextTimeRun == nextRun)
                    .SetColumnsIF(shouldClearNextRun, x => x.NextTimeRun == null)
                    .Where(x => x.ID == sub.ID)
                    .Where(x => x.ProcessingUntil == processingUntil)
                    .ExecuteCommandAsync(cancelToken);

                if (affected <= 0)
                {
                    logger.LogWarning("⚠️ Mất lock khi nhả đăng ký cho {Tag} — một worker khác đã nhận lại đăng ký.", tag);
                }
            }
            catch (Exception releaseEx)
            {
                logger.LogError(releaseEx, "❌ Lỗi khi nhả lock cho {Tag} — Subscription có thể bị giữ lock tới khi ProcessingUntil tự hết hạn: {Message}", tag, releaseEx.Message);
            }
        }

        /// <summary>
        /// Description: Tìm kiếm cấu hình gói tin active tương ứng với DatatypeId theo thứ tự:
        ///              khớp chính xác Code/ID trước, sau đó fallback theo OrderNo hoặc tiền tố mã số.
        /// Created date: 16/09/2026
        /// Modified date: 12/11/2026
        /// </summary>
        private static async Task<ShareDataPacket?> HasActivePacket(SqlSugarClient db, string? datatypeId, CancellationToken cancelToken = default)
        {
            if (string.IsNullOrWhiteSpace(datatypeId))
                return null;

            var packets = await db.Queryable<ShareDataPacket>()
                .Where(p => p.IsDelete == null)
                .ToListAsync(cancelToken);

            return PickActivePacket(packets, datatypeId);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Chọn gói tin active tương ứng với DatatypeId từ danh sách gói đã nạp theo 3 tầng: khớp chính xác Code/ID,
        ///              tiền tố mã số, rồi OrderNo. Dùng chung cho đường tra CSDL và đường nạp trước.
        /// Created date: 12/11/2026
        /// </summary>
        private static ShareDataPacket? PickActivePacket(IEnumerable<ShareDataPacket> packets, string? datatypeId)
        {
            if (string.IsNullOrWhiteSpace(datatypeId))
                return null;

            var datatypeIdTrim = datatypeId.Trim();
            var activePackets = packets.Where(p => p.IsDelete == null).ToList();

            // 1. Tầng 1 (Exact Match): Tìm theo Code hoặc ID chính xác (chiếm phần lớn trường hợp thực tế)
            var packet = activePackets.FirstOrDefault(p =>
                string.Equals(p.Code, datatypeIdTrim, StringComparison.OrdinalIgnoreCase)
                || string.Equals(p.ID, datatypeIdTrim, StringComparison.OrdinalIgnoreCase));

            if (packet != null)
                return packet;

            // 2. Tầng 2 (Fallback): Chỉ khi không khớp chính xác, thử phân giải số và tìm theo tiền tố mã số trước, sau đó mới tới OrderNo
            int? targetNumber = PacketHelper.TryGetPacketNumber(datatypeIdTrim, out var resolvedNum)
                ? resolvedNum
                : PacketHelper.TryGetDatatypeEnum(datatypeIdTrim, out var dtEnum)
                    ? (int)dtEnum
                    : null;

            if (!targetNumber.HasValue)
                return null;

            var prefix = targetNumber.Value.ToString();
            var packetByPrefix = activePackets.FirstOrDefault(p => p.Code != null && p.Code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            if (packetByPrefix != null)
                return packetByPrefix;

            return activePackets.FirstOrDefault(p => p.OrderNo == targetNumber.Value);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Kiểm lại cấu trúc câu SQL nối đuôi ngay trước khi chạy (có cột __lastKey, ORDER BY tăng dần bắt đầu bằng __lastKey,
        ///              WHERE so cột mốc với biến mốc). Sai thì không gửi, giữ nguyên mốc và ghi cảnh báo ESH-1306.
        /// Created date: 28/10/2026
        /// Modified date: 03/11/2026
        /// </summary>
        private static async Task<bool> ValidateIncrementalSqlAsync(SqlSugarClient db, DataOutboundContext ctx, string sql, SqlVariableSet? variables)
        {
            var result = SqlValidatorHelper.ValidateIncrementalStructure(sql, SqlValidatorHelper.ExtractColumnsStatically(sql), variables);
            if (result.IsValid)
            {
                return true;
            }

            await ShareDataTransferLog.WriteFailureLogs(
                db,
                ctx.Subscription,
                ctx.Partner,
                ShareDataAlertCode.Outbound.CursorOrderInvalid,
                BaseEnums.AlertSeverity.Error,
                BaseEnums.AlertSource.Subscription,
                $"Câu truy vấn nối đuôi của gói tin [{ctx.PacketCode}] sai cấu trúc ({result.ErrorKey}): "
                    + SqlValidatorHelper.FormatVietnamese(result.ErrorKey, result.ErrorArgs)
                    + " Đã bỏ qua lượt gửi và giữ nguyên mốc.");
            return false;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu nối đuôi lưu được nhưng dễ lấy sai: ghi rủi ro vào nhật ký cấu hình (ShareDataActivityLog), không ghi cảnh báo, không chặn gửi.
        ///              Mỗi (đăng ký, câu SQL) một lần trong vòng đời tiến trình; hỏi kiểu cột lỗi thì bỏ qua, không ảnh hưởng lượt gửi.
        /// Created date: 01/11/2026
        /// </summary>
        private async Task LogCursorRisks(SqlSugarClient db, DataOutboundContext ctx, string sql, SqlVariableSet? variables, string tag)
        {
            if (!_loggedCursorRisks.TryAdd($"{ctx.Subscription.ID}:{ComputeConfigHash(sql)}", 0))
            {
                return;
            }

            try
            {
                var columns = SqlValidatorHelper.ValidateAndExtractColumns(db, sql, variables: variables);
                var risks = columns.IsValid ? SqlValidatorHelper.FindCursorRisks(sql, columns.ColumnInfos, variables) : [];
                if (risks.Count == 0)
                {
                    return;
                }

                await ShareDataTransferLog.WriteCursorRiskAsync(db, ctx.Subscription, ctx.Partner, string.Join(" ", risks), ctx.CancelToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogDebug(ex, "Không kiểm được rủi ro câu truy vấn cho {Tag}.", tag);
            }
        }

        /// <summary>
        /// Description: Kiểm tra tính hợp lệ của mốc con trỏ kết xuất nối đuôi và ghi nhật ký cảnh báo nếu không hợp lệ: mốc quá dài, nhiều dòng trùng giá trị cột mốc, không có mốc.
        /// Created date: 16/10/2026
        /// Modified date: 05/11/2026
        /// </summary>
        private static async Task<bool> ValidateCursor(
            SqlSugarClient db,
            DataOutboundContext ctx,
            bool isIncremental,
            DataOutboundExtractionResult extraction,
            PageLog pageLog)
        {
            var tooLongKey = extraction.LastKey?.Length > EntityConst.Length256
                ? extraction.LastKey
                : extraction.Groups?.Values.Select(g => g.LastKey).FirstOrDefault(k => k.Length > EntityConst.Length256);
            if (isIncremental && tooLongKey != null)
            {
                await ShareDataTransferLog.WriteFailureLogs(
                    db,
                    ctx.Subscription,
                    ctx.Partner,
                    ShareDataAlertCode.Outbound.CursorKeyTooLong,
                    BaseEnums.AlertSeverity.Error,
                    BaseEnums.AlertSource.Subscription,
                    $"Khoá bản ghi cuối trang (__lastKey) của gói tin [{ctx.PacketCode}] dài {tooLongKey.Length} ký tự, vượt giới hạn {EntityConst.Length256} của cột LastKey. Đã huỷ kết xuất trước khi gửi để tránh gửi trùng lặp vô hạn. Cần rà lại câu truy vấn trích xuất của gói tin.",
                    recordCount: pageLog.RecordCount,
                    mappingId: pageLog.MappingId,
                    packetVersion: pageLog.PacketVersion,
                    parentId: pageLog.ParentId,
                    stepNbr: 1);
                return false;
            }

            if (isIncremental && extraction.DuplicatedKey != null)
            {
                await ShareDataTransferLog.WriteFailureLogs(
                    db,
                    ctx.Subscription,
                    ctx.Partner,
                    ShareDataAlertCode.Outbound.CursorKeyDuplicated,
                    BaseEnums.AlertSeverity.Error,
                    BaseEnums.AlertSource.Subscription,
                    $"Câu truy vấn nối đuôi của gói tin [{ctx.PacketCode}] trả nhiều dòng cùng giá trị cột mốc [{extraction.DuplicatedKey}], thường do JOIN nhân dòng: trang kế tiếp sẽ bỏ sót các dòng trùng. Đã huỷ trước khi gửi và giữ nguyên mốc. Thêm khóa của bảng con vào công thức cột mốc để mỗi dòng có một giá trị riêng.",
                    recordCount: pageLog.RecordCount,
                    mappingId: pageLog.MappingId,
                    packetVersion: pageLog.PacketVersion,
                    parentId: pageLog.ParentId,
                    stepNbr: 1);
                return false;
            }

            var defaultCursorMissing = ctx.UsesDefaultCursor && string.IsNullOrEmpty(extraction.LastKey);
            // Câu chỉ có nhóm mốc có tên: trang phải có mốc của ít nhất một nhóm
            var groupCursorMissing = !ctx.UsesDefaultCursor && (extraction.Groups == null || extraction.Groups.Count == 0);
            if (isIncremental && (defaultCursorMissing || groupCursorMissing))
            {
                await ShareDataTransferLog.WriteFailureLogs(
                    db,
                    ctx.Subscription,
                    ctx.Partner,
                    ShareDataAlertCode.Outbound.RequiredFieldMissing,
                    BaseEnums.AlertSeverity.Error,
                    BaseEnums.AlertSource.Subscription,
                    $"Query nối đuôi của gói tin [{ctx.PacketCode}] không trả về cột mốc hợp lệ (__lastKey hoặc __lastKey_<nhóm>) cho bản ghi cuối trang. Đã huỷ trước khi gửi và giữ nguyên mốc.",
                    recordCount: pageLog.RecordCount,
                    mappingId: pageLog.MappingId,
                    packetVersion: pageLog.PacketVersion,
                    parentId: pageLog.ParentId,
                    stepNbr: 1);
                return false;
            }

            await ShareDataTransferLog.WriteActivityAsync(
                db,
                ctx.Subscription,
                ctx.Partner,
                BaseEnums.TransferDirection.SND,
                recordCount: pageLog.RecordCount,
                byteSize: 0,
                filePath: null,
                status: ShareDataEnum.ExportStatus.Success,
                serialNbr: pageLog.SerialNbr,
                description: $"Trích xuất {pageLog.RecordCount} bản ghi từ CSDL nguồn",
                parentId: pageLog.ParentId,
                stepNbr: 1);

            return true;
        }

        /// <summary>
        /// Description: Thực hiện ánh xạ dữ liệu kết xuất theo cấu hình mapping và ghi nhận nhật ký bước 2.
        /// Created date: 16/10/2026
        /// </summary>
        private async Task<DataMappingResult?> MapPage(
            SqlSugarClient db,
            DataOutboundExtractionResult extraction,
            DataOutboundContext ctx,
            int pageNumber,
            PageLog pageLog)
        {
            var mapResult = await DataMappingProcess.Map(
                db,
                extraction,
                ctx,
                (alertCode, severity, alertSource, msg, detailJson) => ShareDataTransferLog.WriteAlertAsync(
                    db,
                    ctx.Subscription,
                    alertCode,
                    severity,
                    alertSource,
                    msg,
                    detailJson),
                (msg, args) => logger.LogWarning(msg, args));

            if (!mapResult.Success)
            {
                await ShareDataTransferLog.WriteFailureLogs(
                    db,
                    ctx.Subscription,
                    ctx.Partner,
                    ShareDataAlertCode.Outbound.RequiredFieldMissing,
                    BaseEnums.AlertSeverity.Error,
                    BaseEnums.AlertSource.Funnel,
                    $"Gói tin [{ctx.PacketCode}] {mapResult.ErrorMessage} Đã huỷ kết xuất trang {pageNumber + 1}.",
                    recordCount: pageLog.RecordCount,
                    mappingId: pageLog.MappingId,
                    packetVersion: pageLog.PacketVersion,
                    parentId: pageLog.ParentId,
                    stepNbr: 2);
                return null;
            }

            // Ghi nhận Bước 2 (Ánh xạ dữ liệu) thành công
            await ShareDataTransferLog.WriteActivityAsync(
                db,
                ctx.Subscription,
                ctx.Partner,
                BaseEnums.TransferDirection.SND,
                recordCount: pageLog.RecordCount,
                byteSize: mapResult.FinalBytes?.Length ?? 0,
                filePath: null,
                status: ShareDataEnum.ExportStatus.Success,
                mappingId: pageLog.MappingId,
                packetVersion: pageLog.PacketVersion,
                serialNbr: pageLog.SerialNbr,
                description: $"Ánh xạ thành công {pageLog.RecordCount} bản ghi",
                parentId: pageLog.ParentId,
                stepNbr: 2);

            return mapResult;
        }



        /// <summary>
        /// Description: Ghi nhận nhật ký thành công, cập nhật mốc gửi và tính toán khả năng tiếp tục vòng lặp phân trang.
        ///              Đối tác đã lưu gói nên số thứ tự và mốc ghi không theo token dịch vụ: dừng lúc này vẫn ghi xong; mất khóa thì vẫn tăng số thứ tự.
        /// Created date: 16/10/2026
        /// Modified date: 15/11/2026
        /// </summary>
        private static async Task<bool> CommitPage(
            SqlSugarClient db,
            DataOutboundContext ctx,
            bool isIncremental,
            DataOutboundExtractionResult extraction,
            DateTime processingUntil,
            DateTime writeTime,
            string? newKey,
            string tag,
            int pageNumber,
            int sentByteSize,
            string? sentPath,
            string successDesc,
            PageLog pageLog,
            ILogger logger,
            CursorSnapshot? cursorSnapshot = null,
            DateTime? commitTime = null
        )
        {
            await ShareDataTransferLog.WriteActivityAsync(
                db,
                ctx.Subscription,
                ctx.Partner,
                BaseEnums.TransferDirection.SND,
                recordCount: pageLog.RecordCount,
                byteSize: sentByteSize,
                filePath: sentPath,
                status: ShareDataEnum.ExportStatus.Success,
                mappingId: pageLog.MappingId,
                packetVersion: pageLog.PacketVersion,
                serialNbr: pageLog.SerialNbr,
                description: successDesc,
                parentId: pageLog.ParentId,
                stepNbr: 3);

            var (committed, cursorAdvanced) = await CommitSuccess(
                db,
                ctx.Subscription,
                processingUntil,
                ctx.Partner?.Code ?? string.Empty,
                ctx.PacketCode ?? string.Empty,
                isIncremental,
                writeTime,
                newKey,
                extraction.LastKeyValue,
                ctx.CursorSignature,
                ctx.UsesDefaultCursor,
                extraction.Groups,
                ctx.GroupSignatures,
                CancellationToken.None,
                cursorSnapshot,
                commitTime
            );

            if (!committed)
            {
                // Lượt khác đã lấy khóa nhưng đối tác đã lưu gói: vẫn tăng số thứ tự để lượt sau gửi lại các dòng này với số kế tiếp,
                // không gửi lại số đối tác đã lưu rồi bị từ chối mãi
                var serialConsumed = await ConsumeSentSerial(db, ctx);
                var lostLockMessage = serialConsumed
                    ? "Mất lock khi commit dữ liệu. Đối tác đã nhận gói nên lượt sau gửi lại các dòng này với số thứ tự kế tiếp."
                    : "Mất lock khi commit dữ liệu.";
                await ShareDataTransferLog.WriteActivityAsync(
                    db,
                    ctx.Subscription,
                    ctx.Partner,
                    BaseEnums.TransferDirection.SND,
                    recordCount: pageLog.RecordCount,
                    byteSize: sentByteSize,
                    filePath: sentPath,
                    status: ShareDataEnum.ExportStatus.Failed,
                    errorMessage: lostLockMessage,
                    mappingId: pageLog.MappingId,
                    packetVersion: pageLog.PacketVersion,
                    serialNbr: pageLog.SerialNbr,
                    logId: pageLog.ParentId,
                    isUpdate: true);
                return false;
            }

            await ShareDataTransferLog.WriteActivityAsync(
                db,
                ctx.Subscription,
                ctx.Partner,
                BaseEnums.TransferDirection.SND,
                recordCount: pageLog.RecordCount,
                byteSize: sentByteSize,
                filePath: sentPath,
                status: ShareDataEnum.ExportStatus.Success,
                mappingId: pageLog.MappingId,
                packetVersion: pageLog.PacketVersion,
                serialNbr: pageLog.SerialNbr,
                description: successDesc,
                logId: pageLog.ParentId,
                isUpdate: true);

            ctx.Subscription.SerialNbr = (ctx.Subscription.SerialNbr ?? 0) + 1;
            return true;
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Công thức cột mốc của câu đang chạy khác công thức lúc ghi mốc: câu bị sửa ngoài màn hình cấu hình (màn hình đã hỏi xác nhận và xóa mốc trước
        ///              khi lưu). Ghi cảnh báo ESH-1308 để người vận hành biết đối tác nhận lại dữ liệu từ Mốc bắt đầu. Mốc còn lệch tới khi ghi mốc mới
        ///              (nguồn chưa có dòng mới), nên mỗi (đăng ký, nhóm mốc, công thức mới) chỉ báo một lần.
        /// Created date: 01/11/2026
        /// Modified date: 08/11/2026
        /// </summary>
        private static async Task WarnCursorFormulaChanged(
            ISqlSugarClient db,
            ShareDataSubscription? sub,
            ShareDataPartner? partner,
            string? packetCode,
            string? group,
            string? signature,
            string? oldLastKey)
        {
            if (sub == null || !_warnedFormulaChanges.TryAdd($"{sub.ID}:{group}:{signature}", 0))
                return;

            var groupText = group == null ? string.Empty : $" nhóm mốc [{group}]";
            await ShareDataTransferLog.WriteAlertAsync(
                db,
                sub,
                ShareDataAlertCode.Outbound.CursorFormulaChanged,
                BaseEnums.AlertSeverity.Warning,
                BaseEnums.AlertSource.Subscription,
                $"Công thức cột mốc{groupText} của gói [{packetCode}] đã đổi ngoài màn hình cấu hình: bỏ mốc cũ "
                    + $"({oldLastKey}), đối tác [{partner?.Code}] nhận lại dữ liệu từ Mốc bắt đầu.",
                partner: partner);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Ghi cảnh báo ESH-1403 về cấu hình kết nối bị bỏ qua, mỗi đối tác và mỗi nội dung cấu hình một lần.
        ///              Bộ nhớ chống trùng nằm trong tiến trình nên khởi động lại Worker sẽ ghi lại.
        /// Created date: 19/10/2026
        /// </summary>
        private static async Task TryLogConfigWarning(
            ISqlSugarClient db,
            DataOutboundContext ctx,
            IReadOnlyList<string>? warnings)
        {
            if (warnings == null || warnings.Count == 0)
                return;

            var partnerId = ctx.Partner?.ID ?? ctx.Subscription?.PartnerId ?? "Unknown";
            var configHash = ComputeConfigHash(ctx.Partner?.ConnectionConfig);
            var cacheKey = $"{partnerId}:{configHash}";

            if (!_loggedConfigWarnings.TryAdd(cacheKey, 0))
                return;

            var combinedMessage = string.Join("; ", warnings);
            await ShareDataTransferLog.WriteAlertAsync(
                db,
                ctx.Subscription,
                ShareDataAlertCode.Outbound.ConfigValueIgnored,
                BaseEnums.AlertSeverity.Warning,
                BaseEnums.AlertSource.Protocol,
                combinedMessage,
                partner: ctx.Partner);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Băm SHA-256 ra chuỗi hex, dùng làm khóa chống ghi trùng cho cấu hình kết nối và câu SQL. Chuỗi rỗng trả rỗng.
        /// Created date: 19/10/2026
        /// </summary>
        private static string ComputeConfigHash(string? config)
        {
            if (string.IsNullOrEmpty(config))
                return string.Empty;
            var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(config));
            return Convert.ToHexString(bytes);
        }

        /// <summary>Khóa __lastKey kiểu số nguyên hoặc GUID: câu SQL so theo kiểu đó nên chốt chặn ghi mốc cũng phải so theo kiểu đó.</summary>
        private static bool IsTypedKey(object? value) =>
            value is sbyte or byte or short or ushort or int or uint or long or ulong or Guid;

        /// <summary>
        /// Khóa mới có đứng sau khóa đã lưu không, so theo kiểu của khóa mới (GUID theo thứ tự của SQL Server).
        /// Khóa đã lưu trống hoặc không đọc được theo kiểu đó (ghi theo công thức cũ) thì coi là đứng sau.
        /// </summary>
        private static bool IsKeyAfter(string? stored, object value) => value is Guid guid
            ? !Guid.TryParse(stored, out var storedGuid) || new SqlGuid(storedGuid).CompareTo(new SqlGuid(guid)) < 0
            : !decimal.TryParse(stored, NumberStyles.Integer, CultureInfo.InvariantCulture, out var storedNumber)
                || storedNumber < Convert.ToDecimal(value, CultureInfo.InvariantCulture);

        /// <summary>
        /// Author: DatHQ
        /// Description: Gói đã rời Worker mà không lưu được kết quả (không nhận được trả lời, lưu số thứ tự và mốc lỗi, mất khóa): coi số thứ tự vừa gửi là đã dùng,
        ///              tăng SerialNbr để lượt sau gửi lại trang với số kế tiếp, tránh đối tác đã lưu gói từ chối mãi một số trùng.
        ///              Không đổi mốc nên không sót dòng; đối tác có thể nhận lại các dòng của trang dưới số mới.
        ///              Chỉ tăng khi số chưa bị lượt khác đổi, kể cả khi lượt này đã mất khóa; không theo token hủy để Worker đang dừng vẫn ghi được.
        /// Created date: 13/11/2026
        /// Modified date: 15/11/2026
        /// </summary>
        private static async Task<bool> ConsumeSentSerial(SqlSugarClient db, DataOutboundContext ctx)
        {
            var subscriptionId = ctx.Subscription.ID;
            var sentSerial = ctx.Subscription.SerialNbr;
            if (sentSerial == null)
            {
                return false;
            }

            var now = db.GetDate();
            var affected = await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => s.SerialNbr == SqlFunc.IsNull(s.SerialNbr, 0) + 1)
                .SetColumns(s => s.UpdateTime == now)
                .Where(s => s.ID == subscriptionId)
                .Where(s => s.SerialNbr == sentSerial)
                .ExecuteCommandAsync(CancellationToken.None);
            return affected > 0;
        }
    }
}