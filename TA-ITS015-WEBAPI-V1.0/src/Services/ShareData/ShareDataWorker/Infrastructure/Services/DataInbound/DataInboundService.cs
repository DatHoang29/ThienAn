using ShareDataWorker.Core.Dto;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Mapping;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;

namespace ShareDataWorker.Infrastructure.Services.DataInbound
{
    /// <summary>
    /// Dịch vụ Thu nhận dữ liệu (luồng NHẬN VỀ — ISO 14827, chiều ngược của Publication).
    ///
    /// Từ 15/09/2026 luồng nhận chạy THEO GÓI TIN thay vì theo lịch đăng ký:
    ///  - WebAPI (ShareDataInboundController) lưu gói thô vào ShareDataInboundPacket (CSDL Inbound), trạng thái Pending.
    ///  - Service này quét gói Pending theo nhóm (PartnerCode + DatatypeId), gỡ bộ khung phễu lọc,
    ///    rồi chạy các câu ghi khai ở ShareDataSql.WriteSql (MERGE theo tập, OPENJSON(@records))
    ///    trên CSDL Inbound trong một transaction, cập nhật Done / Failed.
    ///  - Chiều nhận KHÔNG đọc ShareDataTable: gói tin (ShareDataPacket + ShareDataSql.WriteSql
    ///    + phễu lọc chiều nhận) là nguồn cấu hình duy nhất.
    ///
    /// Chốt 20/09/2026 — hợp đồng mới với luồng gửi:
    ///  - Đối tác gửi body NGUYÊN VĂN (không phong bì), mọi thông tin định danh nằm ở HEADER:
    ///    PartnerCode, PacketCode, SerialNbr. ShareDataInboundPacket.DatatypeId lưu PacketCode.
    ///  - Khoá bên trong body KHÔNG cố định. Vị trí mảng bản ghi và đường dẫn từng fieldKey do
    ///    bộ khung (ShareDataMapping.TargetShapeJson) quyết định — xem ParsePayloadRecords.
    ///    Tuyệt đối không giả định tên khoá nào trong gói tin đối tác.
    ///
    /// Nguyên tắc thiết kế:
    ///  - Coi dữ liệu đến như do bên thứ ba gửi
    ///  - Dùng lại các hàm quy ước chung của luồng gửi (ParseFields, ReadShape) thay vì chép thành
    ///    bản thứ hai — hai bản sẽ trôi khỏi nhau. Riêng việc phân giải gói tin thì tách ra
    ///    (ResolvePacketByCodeAsync) vì chiều nhận tra theo Code, chiều gửi tra theo ID.
    ///  - Xử lý gói tin theo SerialNbr TĂNG DẦN, nếu không gói cũ sẽ ghi đè gói mới.
    ///  - Gói lỗi đánh dấu Failed rồi xử lý tiếp gói sau (không chặn cả nhóm).
    ///
    /// Author: HieuNV
    /// Created date: 07/09/2026
    /// Modified date: 19/10/2026
    /// </summary>
    public partial class DataInboundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DataInboundService> logger,
        DataWorkerConfigService workerConfig)
    {
        private const int ErrorMessageMaxLength = 512;

        /// <summary>Gói gặp lỗi CSDL tạm thời còn được trả về chờ để ghi lại nếu nhận chưa quá khoảng này.</summary>
        private static readonly TimeSpan TransientRetryWindow = TimeSpan.FromMinutes(30);
        private DateTime _lastBothWarnAt = DateTime.MinValue;

        /// <summary>
        /// Author: HieuNV
        /// Description: Quét và xử lý các gói tin Pending trong ShareDataInboundPacket.
        ///              Đầu mỗi lượt trả các gói kẹt ở Processing về Pending.
        /// Created date: 07/09/2026
        /// </summary>
        public async Task ProcessPendingPackets(CancellationToken stoppingToken = default)
        {
            var settings = await workerConfig.Get(stoppingToken);

            await WarnBothDirectionSubscriptions(TimeSpan.FromMinutes(settings.InboundWarnIntervalMin));

            await using var scope = scopeFactory.CreateAsyncScope();
            var baseClient = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var tenant = baseClient.AsTenant();
            var inboundBase = tenant.IsAnyConnection(ShareDataWorkerExtensions.InboundConnectionKey)
                ? tenant.GetConnectionScope(ShareDataWorkerExtensions.InboundConnectionKey)
                : null;

            if (inboundBase == null)
            {
                LogErrorMsg(null, "❌ [Nhận] Chưa cấu hình ConnectionStrings:InboundConnection — bỏ qua toàn bộ luồng nhận.");
                return;
            }

            using var db = baseClient.CopyNew();
            using var inboundDb = inboundBase.CopyNew();

            await ReleaseStuckPacketsAsync(
                inboundDb,
                TimeSpan.FromMinutes(settings.InboundStuckTimeoutMin));

            // 1. Gom nhóm gói chờ theo (đối tác, loại gói) — mỗi nhóm dùng chung một bộ cấu hình.
            var groups = await inboundDb.Queryable<ShareDataInboundPacket>()
                .Where(p => p.IsDelete == null)
                .Where(p => p.ProcessState == BaseEnums.InboundProcessState.Pending)
                .GroupBy(p => new { p.PartnerCode, p.PacketCode })
                .Select(p => new { p.PartnerCode, p.PacketCode })
                .ToListAsync();

            if (groups.Count == 0)
            {
                LogInformationMsg("ℹ️ [Nhận] Không có gói tin nào chờ xử lý.");
                return;
            }

            LogInformationMsg("▶️ [Nhận] Có {Count} nhóm gói tin chờ xử lý.", groups.Count);

            foreach (var group in groups)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    await ProcessGroupAsync(
                        db,
                        inboundDb,
                        group.PartnerCode,
                        group.PacketCode,
                        settings,
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    LogErrorMsg(ex, "❌ [Nhận] Lỗi xử lý nhóm {PartnerCode}/{PacketCode}: {Message}",
                        group.PartnerCode, group.PacketCode, ex.Message);
                }
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xử lý một nhóm gói (cùng đối tác + loại gói) theo SerialNbr tăng dần.
        ///              Chỉ nhận gói khi không còn gói số nhỏ hơn cùng nhóm đang chờ hoặc đang xử lý, để gói cũ không ghi đè gói mới.
        /// Created date: 15/09/2026
        /// Modified date: 16/11/2026
        /// </summary>
        private async Task ProcessGroupAsync(
            ISqlSugarClient db,
            ISqlSugarClient inboundDb,
            string? partnerCode,
            string? packetCode,
            DataWorkerConfig settings,
            CancellationToken cancellationToken)
        {
            var ctx = await ResolveContextAsync(db, partnerCode, packetCode);

            if (ctx.Skip)
            {
                LogInformationMsg("⏸️ [Nhận] Bỏ qua nhóm {PartnerCode}/{PacketCode}: {Reason}",
                    partnerCode, packetCode, ctx.ErrorMessage);
                return;
            }

            var packets = await inboundDb.Queryable<ShareDataInboundPacket>()
                .Where(p => p.IsDelete == null)
                .Where(p => p.ProcessState == BaseEnums.InboundProcessState.Pending)
                .Where(p => p.PartnerCode == partnerCode && p.PacketCode == packetCode)
                .OrderBy(p => p.SerialNbr)
                .OrderBy(p => p.CreateTime)
                .Take(settings.InboundMaxPacketsPerRun)
                .ToListAsync();

            int? lastSerial = null;

            foreach (var packet in packets)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Đánh dấu nhận xử lý: chỉ một tiến trình được chuyển Pending → Processing.
                // ProcessedAt lúc này là mốc bắt đầu xử lý (để phát hiện gói kẹt); xử lý xong sẽ ghi đè.
                // Còn gói số nhỏ hơn cùng nhóm đang chờ hoặc đang xử lý (dịch vụ khác đang ghi, gói lỗi tạm thời chờ ghi lại) thì chưa nhận gói này:
                // mỗi nhóm chỉ một gói được ghi tại một thời điểm, gói cũ không ghi sau gói mới. Gói lỗi không chặn; gói không số giữ như cũ
                var markedAt = DateTime.Now;
                var packetSerial = packet.SerialNbr;
                var markedCount = await inboundDb.Updateable<ShareDataInboundPacket>()
                    .SetColumns(p => p.ProcessState == BaseEnums.InboundProcessState.Processing)
                    .SetColumns(p => p.ProcessedAt == markedAt)
                    .Where(p => p.ID == packet.ID && p.ProcessState == BaseEnums.InboundProcessState.Pending)
                    .WhereIF(packetSerial != null, p => !SqlFunc.Subqueryable<ShareDataInboundPacket>()
                        .Where(o => o.PartnerCode == partnerCode && o.PacketCode == packetCode && o.IsDelete == null && o.SerialNbr < packetSerial)
                        .Where(o => o.ProcessState == BaseEnums.InboundProcessState.Pending || o.ProcessState == BaseEnums.InboundProcessState.Processing)
                        .Any())
                    .ExecuteCommandAsync(CancellationToken.None);

                if (markedCount <= 0)
                    continue;

                if (ctx.ErrorState.HasValue)
                {
                    await FailPacketAsync(db, inboundDb, ctx, packet, ctx.ErrorState.Value, ctx.AlertCode, ctx.ErrorMessage!);
                    continue;
                }

                if (await ProcessPacketAsync(
                    db,
                    inboundDb,
                    ctx,
                    packet,
                    settings.InboundWriteTimeoutSec,
                    cancellationToken))
                {
                    if (!lastSerial.HasValue || packet.SerialNbr > lastSerial)
                        lastSerial = packet.SerialNbr;
                }
            }

            // Với đăng ký chiều NHẬN, SerialNbr mang nghĩa "số thứ tự gói đã nhận đến" — chỉ tăng, không lùi.
            if (ctx.Subscription != null && lastSerial.HasValue)
            {
                var serial = lastSerial.Value;
                var subId = ctx.Subscription.ID;
                var now = DateTime.Now;
                await db.Updateable<ShareDataSubscription>()
                    .SetColumns(s => s.LastTimeRun == now)
                    .SetColumns(s => s.SerialNbr == serial)
                    .Where(s => s.ID == subId && (s.SerialNbr == null || s.SerialNbr < serial))
                    .ExecuteCommandAsync(CancellationToken.None);
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xử lý một gói: bóc PDU, mapping ngược, ghi CSDL Inbound, cập nhật trạng thái và ghi log.
        ///              Trả về true nếu gói được ghi (Done). Dịch vụ dừng giữa lúc ghi thì trả gói về Pending.
        ///              Lỗi CSDL tạm thời (deadlock, hết giờ, phiên bị cắt) với gói nhận chưa quá 30 phút cũng trả gói về Pending để lượt sau ghi lại.
        /// Created date: 15/09/2026
        /// Modified date: 16/11/2026
        /// </summary>
        private async Task<bool> ProcessPacketAsync(
            ISqlSugarClient db,
            ISqlSugarClient inboundDb,
            InboundContext ctx,
            ShareDataInboundPacket packet,
            int writeCommandTimeoutSeconds,
            CancellationToken cancellationToken)
        {
            var sub = ctx.Subscription!;
            var tag = $"{ctx.Tag} gói #{packet.SerialNbr}";

            try
            {
                // Bộ mã tra ngược chỉ lấy từ phễu lọc ($extend.codeSet) — không truyền bộ mã theo trường.
                var records = ParsePayloadRecords(
                    packet.RawContent ?? string.Empty, ctx.Mapping?.TargetShapeJson,
                    null, ctx.CodeSets, out var parseError);

                if (parseError != null)
                {
                    await FailPacketAsync(db, inboundDb, ctx, packet, BaseEnums.InboundProcessState.Failed,
                        ShareDataAlertCode.Inbound.ParseFailed, $"Không đọc được gói tin: {parseError}");
                    return false;
                }

                // Ghi theo tập: mỗi câu ghi chạy 1 lần cho cả gói, chung một transaction.
                // Lỗi bất kỳ câu nào → rollback cả gói, ném ra catch bên dưới → Failed.
                var result = await WriteRecordsBySqlAsync(
                    inboundDb,
                    ctx,
                    packet,
                    records,
                    cancellationToken,
                    writeCommandTimeoutSeconds);
                var summary = result.Describe(records.Count);

                await MarkPacketAsync(inboundDb, packet.ID, BaseEnums.InboundProcessState.Done, null);

                await LogInboundResult(db, sub, ctx.Partner, packet, result.Inserted + result.Updated,
                    ShareDataEnum.ExportStatus.Success, null,
                    $"Xử lý gói tin loại {packet.PacketCode} từ đối tác \"{ctx.PartnerName}\": {summary}",
                    ctx.Mapping?.ID, parentSummary: summary);

                LogInformationMsg("✅ [Nhận] {Tag}: {Summary}.", tag, summary);
                return true;
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested)
            {
                // Câu ghi chạy trong giao dịch nên đã hoàn tác: trả gói về chờ để lần chạy sau ghi lại, gói lỗi thì không bao giờ được xử lý lại
                LogInformationMsg("⏸️ [Nhận] {Tag}: dịch vụ dừng giữa lúc ghi, trả gói về chờ ({Message}).", tag, ex.Message);
                await MarkPacketAsync(inboundDb, packet.ID, BaseEnums.InboundProcessState.Pending, null);
                return false;
            }
            catch (InboundWriteException ex) when (ex.IsTransient && CanRetryTransientWrite(packet))
            {
                // Câu ghi đã hoàn tác: trả gói về chờ để lượt sau ghi lại; gói số lớn hơn cùng nhóm chưa được nhận cho tới khi gói này ghi xong
                LogWarningMsg("⚠️ [Nhận] {Tag}: lỗi CSDL tạm thời, trả gói về chờ để lượt sau ghi lại ({Message}).", tag, ex.Message);
                await MarkPacketAsync(inboundDb, packet.ID, BaseEnums.InboundProcessState.Pending, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                LogErrorMsg(ex, "❌ [Nhận] Lỗi xử lý {Tag}: {Message}", tag, ex.Message);
                await FailPacketAsync(db, inboundDb, ctx, packet, BaseEnums.InboundProcessState.Failed,
                    ShareDataAlertCode.Inbound.WriteFailed, SafeErrorMessage.ForLog(ex));
                return false;
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Gói gặp lỗi CSDL tạm thời được trả về chờ để ghi lại khi nhận chưa quá 30 phút (giờ nhận gói, cùng giờ ứng dụng như khi trả gói kẹt);
        ///              quá thời gian đó thì chuyển lỗi như lỗi ghi thường để không thử lại mãi.
        /// Created date: 16/11/2026
        /// </summary>
        private static bool CanRetryTransientWrite(ShareDataInboundPacket packet) =>
            DateTime.Now - packet.CreateTime < TransientRetryWindow;

        /// <summary>
        /// Author: HieuNV
        /// Description: Đánh dấu gói lỗi và ghi cảnh báo + nhật ký (nếu xác định được đăng ký).
        /// Created date: 15/09/2026
        /// </summary>
        private async Task FailPacketAsync(
            ISqlSugarClient db,
            ISqlSugarClient inboundDb,
            InboundContext ctx,
            ShareDataInboundPacket packet,
            BaseEnums.InboundProcessState state,
            string? alertCode,
            string message)
        {
            await MarkPacketAsync(inboundDb, packet.ID, state, message);

            if (!string.IsNullOrWhiteSpace(alertCode))
            {
                await LogAlertInbound(db, ctx.Subscription, ctx.Partner, alertCode!, BaseEnums.AlertSeverity.Error,
                    $"{ctx.UserTag} gói #{packet.SerialNbr}: {message}");
            }

            // Nhật ký truyền nhận gắn với đăng ký — không có đăng ký thì chỉ còn cảnh báo ở trên.
            if (ctx.Subscription != null)
            {
                await LogInboundResult(db, ctx.Subscription, ctx.Partner, packet, 0,
                    ShareDataEnum.ExportStatus.Failed, message,
                    $"Lỗi xử lý gói tin loại {packet.PacketCode} từ đối tác \"{ctx.PartnerName}\": {message}",
                    ctx.Mapping?.ID);
            }
            else if (!string.IsNullOrWhiteSpace(packet.ReceiveLogId))
            {
                await ShareDataTransferLog.WriteActivityAsync(
                    db,
                    sub: null,
                    partner: null,
                    BaseEnums.TransferDirection.RCV,
                    recordCount: 0,
                    status: ShareDataEnum.ExportStatus.Failed,
                    errorMessage: message,
                    description: $"Lỗi nhận gói tin loại {packet.PacketCode} từ đối tác \"{ctx.PartnerName ?? packet.PartnerCode}\": {message}",
                    logId: packet.ReceiveLogId,
                    isUpdate: true);
            }

            LogWarningMsg("⚠️ [Nhận] {Tag} gói #{Serial} → {State}: {Message}", ctx.Tag, packet.SerialNbr, state, message);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Trả các gói kẹt ở Processing (bắt đầu xử lý quá StuckProcessingTimeout) về Pending
        ///              để lượt sau xử lý lại. Gói ghi CSDL trong transaction nên chạy lại không bị ghi dở.
        /// Created date: 15/09/2026
        /// </summary>
        private async Task ReleaseStuckPacketsAsync(
            ISqlSugarClient inboundDb,
            TimeSpan stuckProcessingTimeout)
        {
            var threshold = DateTime.Now - stuckProcessingTimeout;

            var released = await inboundDb.Updateable<ShareDataInboundPacket>()
                .SetColumns(p => p.ProcessState == BaseEnums.InboundProcessState.Pending)
                .Where(p => p.ProcessState == BaseEnums.InboundProcessState.Processing
                    && (p.ProcessedAt == null || p.ProcessedAt < threshold))
                .ExecuteCommandAsync(CancellationToken.None);

            if (released > 0)
                LogWarningMsg("⚠️ [Nhận] Trả {Count} gói kẹt ở Processing về Pending.", released);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật trạng thái xử lý của một gói trong CSDL Inbound.
        /// Created date: 15/09/2026
        /// </summary>
        private static Task<int> MarkPacketAsync(
            ISqlSugarClient inboundDb, string? packetId, BaseEnums.InboundProcessState state, string? errorMessage)
        {
            var now = DateTime.Now;
            var error = string.IsNullOrEmpty(errorMessage) || errorMessage.Length <= ErrorMessageMaxLength
                ? errorMessage
                : errorMessage[..ErrorMessageMaxLength];

            return inboundDb.Updateable<ShareDataInboundPacket>()
                .SetColumns(p => p.ProcessState == state)
                .SetColumns(p => p.ProcessedAt == now)
                .SetColumns(p => p.ErrorMessage == error)
                .Where(p => p.ID == packetId)
                .ExecuteCommandAsync(CancellationToken.None);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Phân giải cấu hình dùng chung cho một nhóm gói: đối tác, đăng ký nhận,
        ///              gói tin, câu ghi (ShareDataSql.WriteSql), phễu lọc chiều nhận và bộ mã.
        ///              Không đọc ShareDataTable — gói tin là nguồn cấu hình duy nhất.
        ///              Gói chỉ lưu PartnerCode nên phải tra đối tác theo mã để có ID
        ///              (Subscription / Mapping trỏ tới đối tác bằng ID, và nằm ở CSDL khác nên không join được).
        /// Created date: 15/09/2026
        /// </summary>
        private static async Task<InboundContext> ResolveContextAsync(
            ISqlSugarClient db, string? partnerCode, string? packetcode)
        {
            var ctx = new InboundContext { Tag = $"gói {packetcode} | đối tác {partnerCode}" };

            var partner = await db.Queryable<ShareDataPartner>()
                .Where(p => p.IsDelete == null && p.Code == partnerCode)
                .FirstAsync();

            if (partner == null)
                return ctx.Fail(BaseEnums.InboundProcessState.Failed, ShareDataAlertCode.Inbound.PartnerInvalid,
                    $"Không tìm thấy đối tác mã [{partnerCode}].");

            ctx.Partner = partner;
            ctx.PartnerName = partner.Name ?? partner.Code;

            if (partner.Status != BaseEnums.StatusEnum.Enable)
                return ctx.Fail(BaseEnums.InboundProcessState.Failed, ShareDataAlertCode.Inbound.PartnerInvalid,
                    $"Đối tác [{partnerCode}] đang ở trạng thái không sử dụng.");

            // Gói tin — phân giải TRƯỚC đăng ký, vì chiều nhận chỉ cầm PacketCode lấy từ header.
            // ShareDataInboundPacket.DatatypeId lưu PacketCode, còn Subscription/Mapping lại trỏ gói tin
            // bằng ShareDataPacket.ID — nên phải có packet trong tay mới tra được hai bảng kia.
            // PacketVersion không đối chiếu (giai đoạn vòng lặp nội bộ).
            var packet = await ResolvePacketByCodeAsync(db, packetcode);
            if (packet == null)
            {
                return ctx.Fail(BaseEnums.InboundProcessState.Failed, ShareDataAlertCode.Inbound.PacketNotFound,
                    $"Không tìm thấy cấu hình gói tin cho PacketCode={packetcode}.");
            }

            var packetId = packet.ID;
            var packetCode = packet.Code;

            // Đăng ký chiều nhận — lấy bản mới nhất nếu có nhiều.
            // Nhận cả hai cách khai DatatypeId (ID của gói tin hoặc Code) để không kén dữ liệu cũ/mới.
            var sub = await db.Queryable<ShareDataSubscription>()
                .Where(s => s.IsDelete == null)
                .Where(s => s.PartnerId == partner.ID)
                .Where(s => s.DatatypeId == packetId || s.DatatypeId == packetCode)
                .Where(s => s.Direction == BaseEnums.Direction.Inbound)
                .OrderBy(s => s.CreateTime, OrderByType.Desc)
                .FirstAsync();

            if (sub == null || !IsUsableSubscription(sub.State))
            {
                return ctx.Fail(BaseEnums.InboundProcessState.NoSubscription, ShareDataAlertCode.Inbound.NoSubscription,
                    sub == null
                        ? "Chưa có đăng ký nhận cho loại gói tin này."
                        : $"Đăng ký nhận đang ở trạng thái {sub.State} — không còn hiệu lực.");
            }

            ctx.Subscription = sub;
            ctx.Tag = ShareDataHelper.DescribeSubscription(sub, partner);
            ctx.UserTag = ShareDataHelper.DescribeSubscriptionForUser(sub, partner);

            if (sub.State != BaseEnums.SubSubscriptionState.Active)
            {
                ctx.Skip = true;
                ctx.ErrorMessage = $"đăng ký nhận đang ở trạng thái {sub.State}, giữ gói ở Pending.";
                return ctx;
            }

            // Câu ghi chiều nhận: ưu tiên câu ghi riêng của đối tác (dòng ShareDataSql PartnerId = đối tác),
            // đối tác chưa khai riêng thì dùng câu ghi ở dòng mẫu chung (PartnerId trống).
            var writeRows = await db.Queryable<ShareDataSql>()
                .Where(s => s.IsDelete == null && s.PacketId == packetId && (s.PartnerId == null || s.PartnerId == partner.ID))
                .Select(s => new { s.PartnerId, s.WriteSql })
                .ToListAsync();
            var writeSql = writeRows.FirstOrDefault(s => s.PartnerId == partner.ID && !string.IsNullOrWhiteSpace(s.WriteSql))?.WriteSql
                ?? writeRows.FirstOrDefault(s => s.PartnerId == null)?.WriteSql;
            ctx.Writes = string.IsNullOrWhiteSpace(writeSql) ? [] : [writeSql];
            if (ctx.Writes.Count == 0)
            {
                return ctx.Fail(BaseEnums.InboundProcessState.Failed, ShareDataAlertCode.Inbound.WriteSqlMissing,
                    $"Gói tin [{packet.Code}] chưa khai câu ghi chiều nhận.");
            }

            // Phễu lọc chiều NHẬN — mô tả vị trí từng fieldKey trong JSON đối tác gửi tới.
            ctx.Mapping = await db.Queryable<ShareDataMapping>()
                .Where(m => m.IsDelete == null)
                .Where(m => m.IsActive == true)
                .Where(m => m.PartnerId == partner.ID)
                // Phễu lọc do WebAPI/FE tạo lưu DatatypeId bằng ShareDataPacket.ID;
                // vẫn nhận Code để không bỏ sót hồ sơ cũ.
                .Where(m => m.DatatypeId == packet.ID || m.DatatypeId == packet.Code)
                // Chấp nhận cả phễu lọc khai Both — một bộ khung dùng cho hai chiều.
                .Where(m => m.Direction == BaseEnums.Direction.Inbound
                         || m.Direction == BaseEnums.Direction.Both)
                .OrderBy(m => m.UpdateTime, OrderByType.Desc)
                .FirstAsync();

            // Bộ mã tra ngược partnerValue → sourceValue: chỉ lấy từ $extend.codeSet của phễu lọc.
            var neededCodeSets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(ctx.Mapping?.TargetShapeJson))
            {
                var (_, shapeCodeSets) = DataMappingProcess.ReadShape(ctx.Mapping!.TargetShapeJson);
                foreach (var code in shapeCodeSets.Where(c => !string.IsNullOrWhiteSpace(c)))
                    neededCodeSets.Add(code.Trim());
            }

            ctx.CodeSets = await LoadCodeSetsAsync(db, neededCodeSets);
            return ctx;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Phân giải gói tin cho chiều NHẬN theo PacketCode lấy ở header.
        ///              Chiều nhận chỉ biết Code — đó là toàn bộ thứ đối tác gửi sang, nên tra đúng Code,
        ///              không đoán sang ID. Không dùng chung DataOutboundService.ResolveActivePacket vì
        ///              hàm đó tra theo ID và đang phục vụ luồng gửi — sửa vào đó là đụng cả hai chiều.
        /// Created date: 20/09/2026
        /// </summary>
        private static async Task<ShareDataPacket?> ResolvePacketByCodeAsync(ISqlSugarClient db, string? packetCode)
        {
            if (string.IsNullOrWhiteSpace(packetCode))
                return null;

            var clean = packetCode.Trim();

            return await db.Queryable<ShareDataPacket>()
                .Where(p => p.IsDelete == null && p.Code == clean)
                .FirstAsync();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Đăng ký còn hiệu lực để nhận gói (Active, hoặc tạm dừng / chờ duyệt — giữ gói chờ).
        ///              Hủy / Từ chối / Hết hạn coi như không có đăng ký.
        ///              Giữ đồng bộ với ShareDataInboundCommandHandler bên WebAPI.
        /// Created date: 15/09/2026
        /// </summary>
        private static bool IsUsableSubscription(BaseEnums.SubSubscriptionState? state)
            => state is BaseEnums.SubSubscriptionState.Active
                or BaseEnums.SubSubscriptionState.Paused
                or BaseEnums.SubSubscriptionState.Pending;

        /// <summary>
        /// Author: HieuNV
        /// Description: Nạp các bộ mã cần dùng cho việc tra ngược.
        ///              Bộ mã khai trong cấu hình nhưng không có trong CSDL thì bỏ qua lặng lẽ ở đây —
        ///              chiều gửi đã cảnh báo ESH-1201 cho đúng tình huống đó rồi, báo lại chỉ gây nhiễu.
        /// Created date: 07/09/2026
        /// </summary>
        private static async Task<Dictionary<string, CodeSetDto>> LoadCodeSetsAsync(
            ISqlSugarClient db, HashSet<string> neededCodeSets)
        {
            var result = new Dictionary<string, CodeSetDto>(StringComparer.OrdinalIgnoreCase);
            if (neededCodeSets.Count == 0)
                return result;

            var rows = await db.Queryable<ShareDataCodeSet>()
                .Where(c => c.IsDelete == null)
                .In(c => c.Code, neededCodeSets.ToList())
                .ToListAsync();

            foreach (var row in rows.Where(r => !string.IsNullOrWhiteSpace(r.Code)))
                // ParseCodeSet chứ không phải ParseCodeValues: giữ lại defaultSourceValue /
                // defaultPartnerValue của bộ mã, chiều nhận cần defaultSourceValue để lấp mã lạ (§3.4).
                result[row.Code!] = PacketJsonParser.ParseCodeSet(row.ValuesJson);

            return result;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cảnh báo các Đăng ký bị khai Direction = Both — xem chú thích cùng tên
        ///              bên DataOutboundService. Cả hai worker đều báo, vì đăng ký kiểu này
        ///              không worker nào nhận: không báo thì nó nằm im và rất khó lần ra.
        /// Created date: 07/09/2026
        /// </summary>
        private async Task WarnBothDirectionSubscriptions(TimeSpan warnInterval)
        {
            if (DateTime.Now - _lastBothWarnAt < warnInterval)
                return;

            _lastBothWarnAt = DateTime.Now;

            await using var scope = scopeFactory.CreateAsyncScope();
            var baseClient = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            using var db = baseClient.CopyNew();

            var invalid = await db.Queryable<ShareDataSubscription>()
                .Where(s => s.IsDelete == null)
                .Where(s => s.Direction == BaseEnums.Direction.Both)
                .Where(s => s.State == BaseEnums.SubSubscriptionState.Active)
                .Select(s => s.ID)
                .ToListAsync();

            if (invalid.Count == 0)
                return;

            LogWarningMsg(
                "⚠️ [Nhận] Có {Count} đăng ký đang khai Direction = Both — giá trị này chỉ dùng cho PHỄU LỌC, "
                + "không dùng cho ĐĂNG KÝ. Những đăng ký này sẽ KHÔNG chạy ở cả hai chiều. "
                + "Hãy sửa về Gửi đi hoặc Nhận về. ID: {Ids}",
                invalid.Count, string.Join(", ", invalid));
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cấu hình đã phân giải cho một nhóm gói (đối tác + loại gói).
        /// Created date: 15/09/2026
        /// </summary>
        private sealed class InboundContext
        {
            public string Tag { get; set; } = string.Empty;

            /// <summary>Nhãn dễ hiểu cho thông báo hiển thị trên giao diện (không chứa ID nội bộ).</summary>
            public string UserTag { get; set; } = string.Empty;
            public ShareDataPartner? Partner { get; set; }
            public string? PartnerName { get; set; }
            public ShareDataSubscription? Subscription { get; set; }
            /// <summary>Khối câu ghi chiều nhận của gói (ShareDataSql.WriteSql ở dòng mẫu chung).</summary>
            public List<string> Writes { get; set; } = [];
            public ShareDataMapping? Mapping { get; set; }
            public Dictionary<string, CodeSetDto> CodeSets { get; set; } = new(StringComparer.OrdinalIgnoreCase);

            /// <summary>true: để nguyên gói ở Pending, lần chạy sau xét lại.</summary>
            public bool Skip { get; set; }

            /// <summary>Có giá trị: mọi gói trong nhóm chuyển sang trạng thái này.</summary>
            public BaseEnums.InboundProcessState? ErrorState { get; private set; }
            public string? AlertCode { get; private set; }
            public string? ErrorMessage { get; set; }

            /// <summary>
            /// Author: HieuNV
            /// Description: Đánh dấu nhóm gói không xử lý được: ghi trạng thái, mã cảnh báo, câu lỗi rồi trả lại chính đối tượng này.
            /// Created date: 15/09/2026
            /// </summary>
            public InboundContext Fail(BaseEnums.InboundProcessState state, string? alertCode, string message)
            {
                ErrorState = state;
                AlertCode = alertCode;
                ErrorMessage = message;
                return this;
            }
        }
    }
}

