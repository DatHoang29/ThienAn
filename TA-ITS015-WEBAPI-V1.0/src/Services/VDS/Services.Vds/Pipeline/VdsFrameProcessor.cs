using System.Collections.Concurrent;
using Newtonsoft.Json;
using Services.Vds.Shared.Domain;
using Services.Vds.Shared.Messaging;
using Services.Vds.Shared.Parsing;
using Services.Vds.Configuration;
using Services.Vds.Catalog;
using Services.Vds.Features.Counting;
using Services.Vds.Features.Events;
using Services.Vds.Features.Journey;
using Services.Vds.Pipeline.Contracts;
using Services.Vds.Pipeline.Mapping;
using Services.Vds.Pipeline.State;
using Services.Vds.Features.Media;
using Services.Vds.Features.Statistic;
using Services.Vds.Pipeline.Persistence;
using Services.Vds.Features.VehicleFilter;
using Services.Vds.Features.ZoneStatus;
using Services.Vds.Features.ZoneTraffic;

namespace Services.Vds.Pipeline;

/// <summary>
/// Author: KienLT
/// Description: <b>ĐÂY LÀ TOÀN BỘ ĐƯỜNG DỮ LIỆU THỜI GIAN THỰC.</b> Đọc file này là hiểu luồng.
///
/// Đường tới đây: nguồn (SSE / injector) → <c>VdsFrameDispatcher</c> → Wolverine local queue
/// <c>"vds-data"</c> (bounded, TUẦN TỰ) → <c>ProcessVdsFrameCommandHandler</c> → <see cref="ProcessAsync"/>.
///
/// <para><see cref="ProcessAsync"/> làm đúng 14 việc, theo thứ tự này:</para>
/// <list type="number">
/// <item>Parse JSON (<c>VdsEnvelopeCodec</c>) — JSON sai ⇒ bỏ frame.</item>
/// <item>Quy đổi tên loại bản tin cũ sang tên chuẩn (<c>VdsEnvelopeNormalizer</c>).</item>
/// <item>Tra camera theo <c>cameraIp</c> (<c>CameraManager</c>) — không có trong danh mục ⇒ BỎ frame,
/// vì bản tin chỉ mang IP, không tra được thì không biết ghi vào đâu.</item>
/// <item>Tra vùng của camera (<c>ZoneManager</c>) ⇒ <c>zoneId</c>. Thiếu vùng KHÔNG chặn ghi.</item>
/// <item>Nếu là bản tin thống kê chu kỳ ⇒ rẽ sang <see cref="ProcessStatisticAsync"/> (bảng khác,
/// metadata schema khác) rồi kết thúc.</item>
/// <item>Map sang entity <c>VdsTrafficData</c> (<c>VdsTrafficDataMapper</c> + <c>VehicleSizeResolver</c>).</item>
/// <item>Cửa chống trùng lượt xe (<see cref="VehicleDedupGate"/>) — cùng camera + cùng loại + cùng biển
/// trong cửa sổ thì BỎ frame, không ghi dòng nào.</item>
/// <item>GHI QuestDB (<c>QuestTrafficDataRepository</c>).</item>
/// <item>Giữ nhật ký live trước raw; chỉ tiến checkpoint và xóa nhật ký sau xử lý hồ sơ.</item>
/// <item>Dựng cờ camera/vùng vừa có dữ liệu (<c>CountPublishTrigger</c>, <c>ZoneTrafficPublishTrigger</c>).</item>
/// <item>Lấy mẫu trạng thái vùng (<c>ZoneStatusUpdater</c>).</item>
/// <item>Đối chiếu danh sách biển số (<c>VehicleFilterNotifier</c>).</item>
/// <item>Đẩy media vào hàng đợi tải (<c>VdsMediaQueue</c>) — chỉ đẩy việc, không tải ở đây.</item>
/// <item>Xét luật sự kiện và nâng sự cố nếu cần (<see cref="EvaluateIncidentAsync"/>).</item>
/// </list>
///
/// <b>Frame BÙ LỊCH SỬ (<c>isBackfill</c>) — bốn chỗ đối xử khác:</b> replay quá khứ KHÔNG được lái trạng
/// thái hiện tại và không được làm bên nhận tưởng có việc vừa xảy ra.
/// <list type="bullet">
/// <item><c>ZoneStatusUpdater</c> — bỏ hẳn: trạng thái vùng là "bây giờ".</item>
/// <item><c>VehicleFilterNotifier</c> — bỏ hẳn: cảnh báo biển số là việc thời gian thực.</item>
/// <item><c>StatisticHandler</c> — VẪN GHI nhưng không publish: bù dữ liệu là mục đích, phát lại thì bảng
/// điều khiển nhảy về chu kỳ cũ.</item>
/// <item><c>EvaluateIncidentAsync</c> — bỏ hẳn: sự cố ghi ra là đang MỞ, nâng từ replay là mở lại việc đã
/// qua và còn kích kịch bản tự động.</item>
/// </list>
/// Ngược lại, hai trigger số liệu GỘP (<c>count</c>, <c>traffic</c>) VẪN được đánh dấu: một lượt xe đến
/// muộn thực sự làm con số của cửa sổ hiện tại đổi, nên tính lại là đúng.
///
/// Thứ tự 8 → 9 → phần còn lại là CỐ Ý: dữ liệu thô phải nằm trong DB trước khi bất kỳ thứ gì dẫn xuất từ
/// nó được phát ra, để bên nhận không bao giờ thấy bản tin mà DB chưa có.
///
/// Mọi việc KHÔNG nằm trong file này thì hoặc là job định kỳ (<c>VdsJobScheduler</c>: quét trạng thái
/// vùng, nạp lại danh sách biển, ghép hành trình, nạp lại danh mục), hoặc là worker chạy theo hàng đợi
/// (tải media, gọi API lập lịch sự cố, phát số đếm).
/// Created date: 25/08/2026
/// </summary>
public sealed class VdsFrameProcessor
{
    private const int MaxDistinctLogKeys = 1_024;

    private readonly CameraManager _cameras;
    private readonly ZoneManager _zones;
    private readonly VehicleSizeResolver _sizeResolver;
    private readonly QuestTrafficDataRepository _trafficData;
    private readonly IngestionCheckpointStore _checkpoint;
    private readonly StatisticHandler _statistic;
    private readonly IncidentPromoter _incidents;
    private readonly ZoneStatusUpdater _zoneStatus;
    private readonly VehicleFilterNotifier _vehicleFilter;
    private readonly VdsMediaQueue _media;
    private readonly CountPublishTrigger _countTrigger;
    private readonly ZoneTrafficPublishTrigger _zoneTrafficTrigger;
    private readonly VehicleDedupGate _vehicleDedup;
    private readonly JourneyArrivalQueue _journeyArrivals;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<VdsFrameProcessor> _logger;

    private readonly ConcurrentDictionary<string, byte> _loggedLegacyTypes =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, byte> _loggedUnknownTypes =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, byte> _loggedUnknownCameras =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, byte> _loggedZonelessCameras =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly SemaphoreSlim _processing = new(1, 1);
    private readonly IncidentWorkJournal _incidentWork;
    private readonly SpeedRangeResolver _speeds;

    public VdsFrameProcessor(
        CameraManager cameras,
        ZoneManager zones,
        VehicleSizeResolver sizeResolver,
        QuestTrafficDataRepository trafficData,
        IngestionCheckpointStore checkpoint,
        StatisticHandler statistic,
        IncidentPromoter incidents,
        ZoneStatusUpdater zoneStatus,
        VehicleFilterNotifier vehicleFilter,
        VdsMediaQueue media,
        CountPublishTrigger countTrigger,
        ZoneTrafficPublishTrigger zoneTrafficTrigger,
        VehicleDedupGate vehicleDedup,
        IVdsConfigManager config,
        ILogger<VdsFrameProcessor> logger,
        JourneyArrivalQueue journeyArrivals,
        SpeedRangeResolver speeds)
    {
        _journeyArrivals = journeyArrivals;
        _speeds = speeds;
        _cameras = cameras;
        _zones = zones;
        _sizeResolver = sizeResolver;
        _trafficData = trafficData;
        _checkpoint = checkpoint;
        _statistic = statistic;
        _incidents = incidents;
        _zoneStatus = zoneStatus;
        _vehicleFilter = vehicleFilter;
        _media = media;
        _countTrigger = countTrigger;
        _zoneTrafficTrigger = zoneTrafficTrigger;
        _vehicleDedup = vehicleDedup;
        _config = config;
        _incidentWork = new IncidentWorkJournal(config);
        _logger = logger;
    }

    public async Task<VdsFrameProcessResult> ProcessAsync(string rawJson, CancellationToken ct = default, bool isBackfill = false)
    {
        await _processing.WaitAsync(ct).ConfigureAwait(false);
        try { return await ProcessCoreAsync(rawJson, ct, isBackfill, false).ConfigureAwait(false); }
        finally { _processing.Release(); }
    }

    public async Task RecoverPendingAsync(CancellationToken ct)
    {
        await _processing.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            foreach (var json in _incidentWork.ReadPending().Take(100))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var result = await ProcessCoreAsync(json, ct, false, true).ConfigureAwait(false);
                    if (result.Status != VdsFrameProcessStatus.Persisted)
                        throw new InvalidOperationException("Pending incident work could not be recovered: " + result.Reason);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    _incidentWork.Defer(json);
                    _logger.LogError(ex, "[Recovery] Retained failed frame for a later attempt; continuing other pending work.");
                }
            }
        }
        finally { _processing.Release(); }
    }

    private async Task<VdsFrameProcessResult> ProcessCoreAsync(
        string rawJson, CancellationToken ct, bool isBackfill, bool recovery)
    {
        ct.ThrowIfCancellationRequested();

        var envelope = VdsEnvelopeCodec.TryParse(rawJson, out var parseError);
        if (envelope is null)
        {
            _logger.LogWarning("[Ingestion] Bỏ frame JSON không hợp lệ: {Error}.", parseError);
            return VdsFrameProcessResult.Rejected(
                $"JSON không hợp lệ: {parseError ?? "không rõ lỗi"}.");
        }

        if (envelope.Id <= 0)
        {
            _logger.LogWarning("[Ingestion] Bỏ frame có id không hợp lệ: {EventId}.", envelope.Id);
            return VdsFrameProcessResult.Rejected(
                "Event id phải lớn hơn 0.", envelope.Id);
        }

        var originalType = envelope.Type?.Trim() ?? string.Empty;
        if (VdsEnvelopeNormalizer.Normalize(envelope)
            && TryLogOnce(_loggedLegacyTypes, originalType))
        {
            _logger.LogInformation(
                "[Ingestion] Quy đổi event type legacy '{Original}' → '{Canonical}'.",
                originalType, envelope.Type);
        }

        if (!VdsEventTypes.IsKnown(envelope.Type)
            && TryLogOnce(_loggedUnknownTypes, envelope.Type ?? string.Empty))
        {
            _logger.LogWarning(
                "[Ingestion] Gặp event type chưa biết '{EventType}'; vẫn lưu raw để không mất dữ liệu.",
                envelope.Type);
        }

        var cameraKey = string.IsNullOrWhiteSpace(envelope.CameraIp)
            ? "(rỗng)"
            : envelope.CameraIp.Trim();

        if (!_cameras.TryGet(envelope.CameraIp, out var camera) || camera is null)
        {
            if (TryLogOnce(_loggedUnknownCameras, cameraKey))
            {
                _logger.LogWarning(
                    "[Ingestion] Bỏ frame id={EventId}: cameraIp '{CameraIp}' không có trong cache.",
                    envelope.Id, cameraKey);
            }
            else
            {
                _logger.LogDebug(
                    "[Ingestion] Bỏ frame id={EventId}: cameraIp '{CameraIp}' không có trong cache.",
                    envelope.Id, cameraKey);
            }

            // Camera chưa có trong danh mục là thiếu MAPPING, không phải dữ liệu sai: khai thêm thiết bị vào
            // vds_list_equipment (Catalog:ReloadSeconds nạp lại) là bản tin này xử lý được. Vì vậy trả
            // Unmapped để lượt bù giữ lại chờ replay, thay vì coi như dữ liệu hỏng và bỏ hẳn.
            return VdsFrameProcessResult.Unmapped(
                $"cameraIp '{cameraKey}' không có trong cache.", envelope.Id);
        }

        // Vùng chỉ ảnh hưởng cột zoneId (phân tích theo vùng), không phải điều kiện để ghi: camera chưa
        // gán vùng vẫn phải lưu được dữ liệu thô, nhưng phải cảnh báo vì truy vấn theo vùng sẽ hụt.
        var zoneId = _zones.PrimaryZoneOf(camera.EquipmentId);
        if (string.IsNullOrWhiteSpace(zoneId)
            && TryLogOnce(_loggedZonelessCameras, camera.EquipmentId))
        {
            _logger.LogWarning(
                "[Ingestion] Camera {Camera} chưa gán vùng trong TmsZoneEquipment ⇒ zoneId để rỗng.",
                camera.Label);
        }

        // Bản tin thống kê có schema metadata RIÊNG và đi sang bảng khác. Tách nhánh SAU khi đã tra camera
        // vì nó cũng cần equipment/hướng/vùng/km để định vị.
        if (string.Equals(envelope.Type, VdsEventTypes.Statistic, StringComparison.OrdinalIgnoreCase))
        {
            return await ProcessStatisticAsync(envelope, camera, zoneId, isBackfill, ct)
                .ConfigureAwait(false);
        }

        VdsVehicleMetadata? metadata;
        try
        {
            metadata = envelope.Metadata?.ToObject<VdsVehicleMetadata>(
                JsonSerializer.Create(VdsParse.JsonSettings));
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "[Ingestion] Bỏ frame id={EventId}: metadata không đúng định dạng.", envelope.Id);
            return VdsFrameProcessResult.Rejected(
                "Metadata không đúng định dạng.", envelope.Id);
        }

        // Vùng chỉ ảnh hưởng cột zoneId... (đã tra ở trên, dùng lại cho bản tin phương tiện)
        // Lưu ngưỡng min/max TẠI THỜI ĐIỂM phát hiện (đúng ngưỡng đã đẩy xuống AID).
        var entity = VdsTrafficDataMapper.Map(
            envelope, metadata, camera, zoneId, _sizeResolver, out var mapError, _speeds.ForCamera(camera)?.Range,
            _config.Current.Events.MappedEventTypeCode(envelope.Type));
        if (entity is null)
        {
            _logger.LogWarning(
                "[Ingestion] Bỏ frame id={EventId}: {Error}.", envelope.Id, mapError);
            return VdsFrameProcessResult.Rejected(
                mapError ?? "Không ánh xạ được traffic data.", envelope.Id);
        }

        // CỬA CHỐNG TRÙNG LƯỢT XE — đặt ngay trước lệnh ghi, sau khi đã map để có equipment/biển/mốc thời
        // gian. Thiết bị bắn lại cùng một xe với vendorEventId khác thì QuestDB coi là hai dòng thật (khoá
        // chống trùng của nó là (vendorEventId, detectTime)), và số đếm tính lại từ bảng thô sẽ phồng.
        //
        // ÁP CHO CẢ frame bù lịch sử: bản tin lặp cũng nằm trong dữ liệu quá khứ, bỏ qua cửa khi bù thì bảng
        // thô lại có đúng những dòng trùng mà cửa vừa loại lúc chạy live.
        var ingestion = _config.Current.Ingestion;
        var events = _config.Current.Events;

        // Cửa SKIP data-driven: thời lượng theo LOẠI (danh mục vds_event_skip, thiếu ⇒ vds_skip_default) và
        // khoá có biển hay không theo danh mục vds_event_check_plate. Thêm loại mới chỉ cần thêm dòng trên DB.
        var skipWindow = TimeSpan.FromSeconds(events.SkipWindowFor(entity.AidEventType ?? entity.EventType));
        var checkPlate = events.IsCheckPlateType(entity.AidEventType ?? entity.EventType);

        if (!recovery && !_incidentWork.Contains(rawJson) && !_vehicleDedup.TryEnter(
                entity.EquipmentId, entity.AidEventType ?? entity.EventType, entity.LicensePlate, entity.VendorEventId,
                entity.DetectTime, skipWindow,
                ingestion.EffectiveVehicleDedupMinPlateLength, ingestion.EffectiveVehicleDedupMaxKeys,
                checkPlate))
        {
            _logger.LogDebug(
                "[Ingestion] Bỏ lượt trùng event={EventId}, camera={Camera}, type={EventType}, " +
                "detect={DetectTime:O}: trùng khoá ({KeyKind}) trong {Window}s (đã chặn {Blocked} lượt).",
                entity.VendorEventId, entity.CameraIp, entity.AidEventType ?? entity.EventType, entity.DetectTime,
                checkPlate ? "camera+loại+biển" : "camera+loại",
                skipWindow.TotalSeconds, _vehicleDedup.BlockedCount);

            return VdsFrameProcessResult.SkippedDuplicate(
                entity.VendorEventId,
                $"Trùng trong {skipWindow.TotalSeconds:0}s tại cùng camera.");
        }

        // Không bắt lỗi DB/checkpoint ở đây: Wolverine phải thấy lỗi để retry. Nếu insert đã thành công
        // nhưng checkpoint lỗi, lần retry chỉ UPSERT cùng DEDUP key nên không sinh dòng thứ hai.
        if (!isBackfill) _incidentWork.Begin(rawJson);
        await _trafficData.InsertAsync(entity, ct).ConfigureAwait(false);

        _logger.LogDebug(
            "[Ingestion] Đã ghi event={EventId}, camera={Camera}, detect={DetectTime:O}, backfill={Backfill}.",
            entity.VendorEventId, entity.CameraIp, entity.DetectTime, isBackfill);

        // Chỉ ĐÁNH DẤU phạm vi vừa có dữ liệu; truy vấn gộp chạy ở worker riêng. Đặt truy vấn ở đây thì mỗi
        // lượt xe phải chờ một truy vấn gộp 24h và giờ cao điểm sẽ nghẽn queue tuần tự.
        //
        // Hai đích khác nhau, cố ý:
        //   count   — theo CAMERA, và chỉ camera đếm (điểm vào của hướng) mới đánh thức.
        //   traffic — theo VÙNG, mọi vùng có xe đi qua đều đánh thức.
        //
        // Frame bù lịch sử VẪN đánh dấu: hai bản tin này là số liệu GỘP tính lại từ bảng thô, nên một lượt
        // xe đến muộn thực sự làm con số của cửa sổ hiện tại đổi. Khác với bản tin từng lượt xe (đã bỏ) —
        // cái đó mà phát lại thì bên nhận tưởng xe cũ vừa đi qua.
        // Count SSE queries persisted data; no count publish signal.
        _zoneTrafficTrigger.Notify(entity.ZoneId);

        // Ghép hành trình A→B THEO BẢN TIN: lượt có biển này có thể là camera cuối của một đoạn. Chỉ ĐẨY tín hiệu
        // sang hàng đợi riêng (không truy vấn ở đây — queue vds-data chạy tuần tự). Đặt SAU lệnh ghi thô để hành
        // trình luôn truy được về dòng dữ liệu sinh ra nó; frame bù/phục hồi vẫn đẩy vì ghi hành trình luỹ đẳng
        // và sự cố của chuyến cũ tự đi chế độ phục hồi (JourneyWriter.RecoveryAge).
        var journey = _config.Current.Journey;
        if (journey.Enabled && journey.IsArrivalTrigger)
            _journeyArrivals.Enqueue(entity.EquipmentId, entity.LicensePlate, entity.DetectTime, entity.VehicleSize);

        // Trạng thái vùng lấy mẫu ở ĐÂY — tầng dữ liệu thô, không phải tầng đếm: tầng đếm lọc theo biển số
        // nên ban đêm sẽ mất phần lớn mẫu, mà tình trạng vùng là thứ cần liên tục nhất.
        await _zoneStatus.ObserveAsync(entity, camera, isBackfill || recovery, ct).ConfigureAwait(false);

        // Cảnh báo biển số cũng lấy từ dòng thô. Bản cũ gọi sau khi ghi bảng phân tích (bảng đó đã bỏ), nên
        // hai việc bảng đó từng lo — lọc biển rác và chống dội theo biển — giờ nằm trong chính notifier.
        await _vehicleFilter.NotifyAsync(entity, camera, isBackfill || recovery, ct).ConfigureAwait(false);

        // Media chỉ ĐẨY VÀO HÀNG ĐỢI, không tải ở đây. KHÔNG bỏ qua frame bù lịch sử như hai tính năng trên:
        // ảnh không có ngữ nghĩa thời gian thực, tải được ảnh của lượt xe cũ là bù được vào kho ảnh.
        _media.Enqueue(entity.VendorEventId, entity.CameraIp, entity.DetectTime, envelope.Type, envelope.Media);

        // Sự kiện/sự cố xét SAU khi dữ liệu thô đã ghi: sự cố phải luôn truy được về dòng dữ liệu sinh ra nó.
        await EvaluateIncidentAsync(envelope, metadata, entity, camera, isBackfill, ct, recovery).ConfigureAwait(false);

        await _checkpoint.AdvanceAsync(entity.VendorEventId, entity.DetectTime, entity.CameraIp, ct).ConfigureAwait(false);
        if (!isBackfill) _incidentWork.Complete(rawJson);
        return VdsFrameProcessResult.Persisted(entity.VendorEventId);
    }

    /// <summary>
    /// Nhánh bản tin thống kê: metadata schema riêng, ghi sang bảng thống kê rồi vẫn tiến checkpoint.
    ///
    /// Vẫn tiến checkpoint dù ghi bảng khác: checkpoint là mốc "đã xử lý xong tới đâu" của cả luồng, để
    /// backfill không đọc lại. Không tiến thì mỗi lần bù sẽ kéo lại toàn bộ bản tin thống kê cũ.
    /// </summary>
    private async Task<VdsFrameProcessResult> ProcessStatisticAsync(
        VdsSseEnvelope envelope,
        CameraInfo camera,
        string? zoneId,
        bool isBackfill,
        CancellationToken ct)
    {
        if (!_config.Current.Statistic.Enabled)
        {
            _logger.LogDebug(
                "[Statistic] Bỏ qua event={EventId} vì Service:Statistic:Enabled=false.", envelope.Id);
            return VdsFrameProcessResult.SkippedStatistic(envelope.Id);
        }

        var occurredUtc = VdsTrafficDataMapper.ResolveDetectTime(envelope);
        if (occurredUtc is null)
        {
            _logger.LogWarning(
                "[Statistic] Bỏ event={EventId}: thiếu occurred_utc/created_at nên không có mốc chu kỳ.",
                envelope.Id);
            return VdsFrameProcessResult.Rejected(
                "thiếu occurred_utc/created_at", envelope.Id);
        }

        VdsStatisticMetadata? metadata;
        try
        {
            metadata = envelope.Metadata?.ToObject<VdsStatisticMetadata>(
                JsonSerializer.Create(VdsParse.JsonSettings));
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "[Statistic] Bỏ event={EventId}: metadata không đúng định dạng bản tin thống kê.",
                envelope.Id);
            return VdsFrameProcessResult.Rejected(
                "Metadata thống kê không đúng định dạng.", envelope.Id);
        }

        var entity = await _statistic
            .HandleAsync(envelope, metadata, camera, zoneId, occurredUtc.Value, isBackfill, ct)
            .ConfigureAwait(false);

        await _checkpoint.AdvanceAsync(
            entity.VendorEventId, occurredUtc.Value, camera.CameraIp, ct).ConfigureAwait(false);

        _logger.LogDebug(
            "[Statistic] Đã ghi event={EventId}, camera={Camera}, from={From:O}, backfill={Backfill}.",
            entity.VendorEventId, entity.CameraIp, entity.FromTime, isBackfill);

        return VdsFrameProcessResult.Persisted(entity.VendorEventId);
    }

    /// <summary>
    /// Xét luật sự kiện rồi nâng sự cố nếu cần.
    ///
    /// Chạy SAU khi ghi dữ liệu thô nên sự cố luôn truy được về dòng đã lưu. Lỗi DB không bị bắt ở đây:
    /// Wolverine phải thấy để retry, và cổng chống trùng theo id bản tin làm lần ghi lại an toàn.
    ///
    /// <b>Bỏ HOÀN TOÀN frame bù lịch sử</b> — không chỉ bỏ publish mà bỏ cả việc ghi. Sự cố được ghi với
    /// <c>State = "1"</c> (đang mở) và <c>StartDate</c> theo mốc bản tin, nên nâng từ dữ liệu replay là mở
    /// một sự cố của vài giờ/vài ngày trước ngay trên bảng điều khiển, rồi còn kích cả kịch bản tự động
    /// (đổi nội dung VMS, đóng làn) cho một việc đã qua.
    /// </summary>
    private async Task EvaluateIncidentAsync(
        VdsSseEnvelope envelope,
        VdsVehicleMetadata? metadata,
        VdsTrafficData row,
        CameraInfo camera,
        bool isBackfill,
        CancellationToken ct, bool recovery = false)
    {
        if (isBackfill) return;

        var runtime = _config.Current;
        if (!runtime.Events.Enabled) return;

        var context = new VdsEventContext(
            envelope, metadata, row, camera, runtime.Events, runtime.SpeedRangeByDirection);

        var decision = VdsEventRules.Decide(context);
        if (decision.IsSkip || !decision.Promote) return;

        await _incidents
            .PromoteAsync(context, decision, row.DetectTime, ct, recovery)
            .ConfigureAwait(false);
    }

    /// <summary>Giới hạn cardinality của các khóa log do payload ngoài hệ thống điều khiển.</summary>
    private static bool TryLogOnce(
        ConcurrentDictionary<string, byte> keys, string key)
    {
        if (keys.Count >= MaxDistinctLogKeys) return false;
        return keys.TryAdd(key, 0);
    }
}

public enum VdsFrameProcessStatus
{
    Persisted,
    SkippedStatistic,

    /// <summary>
    /// Bản tin bị cửa <see cref="VehicleDedupGate"/> nhận ra là lượt trùng của một xe đã ghi ⇒ bỏ có Ý ĐỒ.
    ///
    /// Tách khỏi <see cref="Rejected"/> vì hai bên được đối xử khác nhau: <c>Rejected</c> là dữ liệu SAI nên
    /// backfill phải dừng và live phải ghi nhận lỗ hổng, còn đây là dữ liệu ĐÚNG mà ta chủ động không ghi.
    /// </summary>
    SkippedDuplicate,

    Rejected,
}

/// <summary>
/// Author: KienLT
/// Description: Phân loại một bản tin bị <see cref="VdsFrameProcessStatus.Rejected"/> theo khả năng
/// THỬ LẠI ĐƯỢC HAY KHÔNG. Hai nhóm được đối xử khác nhau ở cả hai đường nhận, nên không gộp:
///
/// <list type="bullet">
/// <item><see cref="Permanent"/> — dữ liệu tự nó sai (JSON đứt, id ≤ 0, metadata lệch schema, không map
/// được). Thử lại bao nhiêu lần cũng cùng kết quả, nên đường live KHÔNG đánh dấu gap (đánh dấu thì backfill
/// quay lại gặp đúng bản tin đó và lượt bù nào cũng vướng), và replay cũng vô nghĩa.</item>
/// <item><see cref="Unmapped"/> — dữ liệu có thể đúng nhưng hệ thống CHƯA biết chỗ của nó (camera_ip không
/// có trong danh mục). Bổ sung danh mục là xử lý được, nên đường live VẪN đánh dấu gap và bản tin được giữ
/// để replay.</item>
/// </list>
///
/// <para>Ví dụ có thật (23/09/2026): một bản tin camera <c>10.10.12.15</c> chưa khai trong
/// <c>vds_list_equipment</c> làm lượt bù lịch sử ném <c>InvalidDataException</c>; lỗi thoát ra tới
/// <c>SseEventSource</c> nên suốt 2 ngày worker không đọc được một bản tin live nào
/// (<c>pendingBackfillRevision</c> lên tới 296). Phân loại này là điều kiện để lượt bù đi tiếp thay vì đổ.</para>
/// Created date: 23/09/2026
/// </summary>
public enum VdsRejectKind
{
    /// <summary>Dữ liệu sai vĩnh viễn — thử lại không đổi kết quả.</summary>
    Permanent,

    /// <summary>Thiếu danh mục — bổ sung camera rồi replay là được.</summary>
    Unmapped,
}

/// <summary>Kết quả xử lý một raw frame; backfill dùng để chỉ mở live sau durable success.</summary>
public sealed record VdsFrameProcessResult(
    VdsFrameProcessStatus Status,
    long? VendorEventId = null,
    string? Reason = null,
    VdsRejectKind? RejectKind = null)
{
    public static VdsFrameProcessResult Persisted(long eventId) =>
        new(VdsFrameProcessStatus.Persisted, eventId);

    public static VdsFrameProcessResult SkippedStatistic(long eventId) =>
        new(VdsFrameProcessStatus.SkippedStatistic, eventId, "Statistic được để phase counting.");

    public static VdsFrameProcessResult SkippedDuplicate(long eventId, string reason) =>
        new(VdsFrameProcessStatus.SkippedDuplicate, eventId, reason);

    /// <summary>
    /// Bản tin sai vĩnh viễn. Mặc định của <c>Rejected</c> là <see cref="VdsRejectKind.Permanent"/> vì đó là
    /// trường hợp an toàn hơn: nhận nhầm một bản tin sai thành "thử lại được" sẽ tạo lại đúng vòng lặp bù
    /// không bao giờ xong.
    /// </summary>
    public static VdsFrameProcessResult Rejected(string reason, long? eventId = null) =>
        new(VdsFrameProcessStatus.Rejected, eventId, reason, VdsRejectKind.Permanent);

    /// <summary>Bản tin chưa tra được danh mục — giữ lại để replay sau khi danh mục có thêm thiết bị.</summary>
    public static VdsFrameProcessResult Unmapped(string reason, long? eventId = null) =>
        new(VdsFrameProcessStatus.Rejected, eventId, reason, VdsRejectKind.Unmapped);
}
