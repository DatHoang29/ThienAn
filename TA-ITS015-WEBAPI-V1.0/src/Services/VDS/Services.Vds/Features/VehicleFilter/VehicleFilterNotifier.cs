using Services.Vds.Shared.Domain;
using Services.Vds.Shared.Messaging;
using Services.Vds.Configuration;
using Services.Vds.Catalog;
using Services.Vds.Features.Journey;
using Shared.Utility.Utilities;

namespace Services.Vds.Features.VehicleFilter;

/// <summary>
/// Author: KienLT
/// Description: Đối chiếu lượt xe vừa ghi với danh sách đen/trắng rồi lưu vết và cảnh báo.
///
/// <para><b>Bốn bước, đúng thứ tự:</b>
/// <list type="number">
/// <item>Đối chiếu biển với danh sách trong RAM. Không khớp ⇒ thôi.</item>
/// <item>Xếp lượt vào CHUYẾN (<see cref="PlateTripTracker"/>): cùng biển, đi tiến theo tuyến, cách nhau không quá
/// khoảng nghỉ <c>vds_vehicle_filter_trip_gap</c> ⇒ cùng <c>TripId</c>.</item>
/// <item>Ghi lịch sử — MỌI lượt khớp, kèm camera và mã chuyến. Đây là vết di chuyển của xe trên tuyến.</item>
/// <item>Phát bản tin cho MỌI lượt đã ghi lịch sử, kèm <c>tripId</c> và <c>notify</c>: true chỉ ở lượt MỞ chuyến
/// khi cờ cảnh báo BẬT — mỗi chuyến A → B → C → D một thông báo.</item>
/// </list></para>
///
/// <para>
/// <b>Điểm gọi ở tầng dữ liệu THÔ</b>, khác bản cũ gọi sau khi ghi bảng phân tích (bảng đó đã bỏ). Hệ quả:
/// hai việc mà bảng phân tích từng lo, giờ class này phải tự làm — (a) LỌC biển rác nhận dạng, (b) gộp lượt
/// theo biển. Việc (b) do <see cref="PlateTripTracker"/> đảm nhiệm.
/// </para>
///
/// <para>
/// <b>BỎ QUA bản tin bù lịch sử</b>: thông báo biển số là cảnh báo THỜI GIAN THỰC, phát lại một lượt xe đã đi
/// qua vài giờ trước sẽ khiến người trực đi tìm một chiếc xe không còn ở đó. Lượt xe đó vẫn nằm trong bảng dữ
/// liệu thô nên bằng chứng không mất.
/// </para>
///
/// KHÔNG BAO GIỜ ném lỗi ra pipeline: dữ liệu lượt xe đã ghi DB rồi, mất một cảnh báo không được phép làm
/// mất bản ghi.
/// Created date: 26/08/2026
/// </summary>
public sealed class VehicleFilterNotifier
{
    private readonly VehicleFilterManager _filters;
    private readonly VehicleFilterRepository _repo;
    private readonly VdsPublisher _publisher;
    private readonly PlateTripTracker _trips;
    private readonly SegmentManager _segments;
    private readonly AutoSegmentManager _autoSegments;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<VehicleFilterNotifier> _logger;

    public VehicleFilterNotifier(
        VehicleFilterManager filters,
        VehicleFilterRepository repo,
        VdsPublisher publisher,
        PlateTripTracker trips,
        SegmentManager segments,
        AutoSegmentManager autoSegments,
        IVdsConfigManager config,
        ILogger<VehicleFilterNotifier> logger)
    {
        _filters = filters;
        _repo = repo;
        _publisher = publisher;
        _trips = trips;
        _segments = segments;
        _autoSegments = autoSegments;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Camera <paramref name="to"/> nằm PHÍA TRƯỚC camera <paramref name="from"/> theo hướng đi: có đoạn A→B (khai
    /// tay hoặc tự sinh, kể cả đoạn bắc cầu qua nút giao) từ <paramref name="from"/> tới <paramref name="to"/>.
    /// </summary>
    private bool IsForward(string from, string to)
        => _autoSegments.StartingAt(from).Any(s => string.Equals(s.ToEquipmentId, to, StringComparison.OrdinalIgnoreCase))
           || _segments.StartingAt(from).Any(s => string.Equals(s.ToEquipmentId, to, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Đối chiếu một lượt xe đã ghi dữ liệu thô. <paramref name="isBackfill"/> = true ⇒ bỏ qua hoàn toàn.
    /// </summary>
    public async Task NotifyAsync(
        VdsTrafficData row, CameraInfo? camera, bool isBackfill, CancellationToken ct)
    {
        if (isBackfill) return;

        try
        {
            var options = _config.Current.VehicleFilter;
            if (!options.Enabled) return;

            // Lọc biển rác nhận dạng TRƯỚC khi tra danh sách: bản tin không đọc được biển thường mang chuỗi
            // rỗng hoặc một hai ký tự, tra cũng không khớp mà vẫn tốn một lượt chuẩn hoá cho mỗi lượt xe.
            var normalized = PlateRules.Normalize(row.LicensePlate);
            if (normalized.Length < options.EffectiveMinPlateLength) return;

            var at = row.DetectTime;

            // at là UTC (VdsTrafficDataMapper.ResolveDetectTime luôn trả UTC), còn StartDate/EndDate của
            // TmsVehicleFilter là NGÀY người vận hành khai trên web, tức GIỜ SERVER. So thẳng hai thứ này lệch
            // đúng số giờ của múi giờ máy chủ: với UTC+7, biển khai hiệu lực từ hôm nay 00:00 sẽ không khớp xe
            // đi qua từ 00:00 đến 07:00, còn biển hết hạn hôm nay lại khớp thêm tới 07:00 sáng mai.
            // Cùng loại bẫy mà ZoneStatusUpdater và IncidentPromoter đã ghi chú.
            //
            // Chỉ việc XÉT HIỆU LỰC đổi sang giờ server. Lịch sử, payload và cửa chống dội vẫn dùng at (UTC):
            // chúng so mốc UTC với mốc UTC nên tự nhất quán, và TmsVehicleFilterHistory.DetectTime theo hợp
            // đồng là UTC.
            var atServer = BaseUtil.TimeServer.ToServerTime(at);
            if (!_filters.TryMatch(normalized, atServer, out var entry) || entry is null) return;

            // Xếp vào chuyến TRƯỚC khi ghi lịch sử (lịch sử lưu mã chuyến). Biển chưa có trong RAM (service vừa
            // khởi động lại) ⇒ nạp chuyến gần nhất từ lịch sử, để xe đang đi dở không mở chuyến mới và báo lặp.
            var gap = options.TripGap;
            PlateTripState? seed = null;
            if (!_trips.IsTracked(normalized))
                seed = await TryLoadTripAsync(normalized, at - gap, ct).ConfigureAwait(false);
            var trip = _trips.Track(normalized, at, row.EquipmentId, gap, options.EffectiveTripMaxPlates, IsForward, seed);

            // Lịch sử ghi MỌI lượt khớp (vết di chuyển trên tuyến), chuyến chỉ gác THÔNG BÁO.
            var alarmOn = options.IsAlarmOn(entry.Alarm);
            var historyId = await InsertHistoryAsync(row, camera, entry, at, trip.TripId, ct).ConfigureAwait(false);

            _logger.Log(trip.IsNewTrip ? LogLevel.Information : LogLevel.Debug,
                "[VehicleFilter] {Plate} khớp danh sách '{Type}' tại {Equipment}; chuyến {Trip} ({Reason}); " +
                "cảnh báo={Alarm}; lịch sử={History}.",
                PlateRules.Mask(entry.Plate), entry.Type, row.EquipmentId, trip.TripId, trip.Reason,
                alarmOn ? "bật" : "tắt", historyId ?? "(không ghi được)");

            // Chỉ lượt MỞ chuyến mới được báo (notify = true). Lượt nối chuyến vẫn phát (notify = false) để bảng
            // "hoạt động gần đây" nối lộ trình A → B → C. Không ghi được lịch sử mà cũng không cần báo thì thôi.
            var notify = alarmOn && trip.IsNewTrip;
            if (!_config.Current.Publish.PublishFilter) return;
            if (!notify && historyId is null) return;

            await _publisher
                .SendAsync(VdsSubjects.Filter, BuildPayload(row, camera, entry, at, historyId, notify, trip.TripId), ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "[VehicleFilter] Lỗi xử lý cảnh báo biển số; bỏ qua lượt này.");
        }
    }

    /// <summary>Chuyến gần nhất của biển trong khoảng nghỉ (sau khởi động lại); lỗi DB ⇒ null (mở chuyến mới).</summary>
    private async Task<PlateTripState?> TryLoadTripAsync(string normalizedPlate, DateTime sinceUtc, CancellationToken ct)
    {
        try
        {
            return await _repo.QueryLastTripAsync(normalizedPlate, sinceUtc, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "[VehicleFilter] Không đọc được chuyến gần nhất của {Plate}; coi như chuyến mới.",
                PlateRules.Mask(normalizedPlate));
            return null;
        }
    }

    /// <summary>
    /// Ghi lịch sử; lỗi ⇒ null và VẪN cảnh báo. Người trực cần biết ngay, còn vết thì ghi bù sau — đảo lại
    /// thứ tự ưu tiên này nghĩa là một lỗi ghi bảng phụ làm mất luôn cảnh báo.
    /// </summary>
    private async Task<string?> InsertHistoryAsync(
        VdsTrafficData row, CameraInfo? camera, VehicleFilterEntry entry, DateTime at, string tripId, CancellationToken ct)
    {
        try
        {
            return await _repo
                .InsertHistoryAsync(BuildHistory(row, camera, entry, at, tripId), ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[VehicleFilter] Không ghi được lịch sử khớp danh sách cho {Plate}.",
                PlateRules.Mask(row.LicensePlate));
            return null;
        }
    }

    /// <summary>
    /// Dựng bản ghi lịch sử của một lượt khớp — hàm THUẦN, tách riêng để kiểm được mà không cần DB.
    ///
    /// <para>Ba cột dễ gán sai, ghi rõ để không lặp lại:</para>
    /// <list type="bullet">
    /// <item><c>LicensePlate</c> = biển NGUYÊN VĂN của lượt xe, không phải bản chuẩn hoá — lịch sử là bằng
    /// chứng hiện trường nên phải khớp đúng dòng dữ liệu thô cùng lượt.</item>
    /// <item><c>LocationId</c> = MÃ TUYẾN của camera trong danh mục (cột chỉ dài 16 ký tự).</item>
    /// <item><c>LaneId</c> = khoá danh mục làn của CAMERA, không phải số làn thiết bị gửi trong bản tin —
    /// hai thứ khác miền giá trị.</item>
    /// </list>
    /// </summary>
    public static TmsVehicleFilterHistory BuildHistory(
        VdsTrafficData row, CameraInfo? camera, VehicleFilterEntry entry, DateTime at, string? tripId = null) => new()
    {
        // Camera bắt được xe + chuyến: lý trình và lộ trình A → B → C khi tra lại lịch sử.
        EquipmentId = row.EquipmentId,
        TripId = tripId,

        LicensePlate = row.LicensePlate,
        Type = entry.Type,
        DetectTime = at,

        // Hạn hiệu lực của dòng danh sách LÚC KHỚP: người vận hành sửa hạn sau đó thì vết cũ vẫn đúng.
        EndDate = entry.EndDate,

        Direction = row.Direction,
        LocationId = camera?.Location,
        ZoneId = row.ZoneId,
        LaneId = camera?.Lane,
    };

    /// <summary>
    /// Bản tin cảnh báo — hàm THUẦN. Đây là HỢP ĐỒNG với giao diện nên tên trường không được đổi lỡ tay.
    /// </summary>
    public static VdsFilterPayload BuildPayload(
        VdsTrafficData row, CameraInfo? camera, VehicleFilterEntry entry, DateTime at, string? historyId,
        bool notify = true, string? tripId = null)
        => new()
        {
            HistoryId = historyId,
            TripId = tripId,
            FilterId = entry.FilterId,
            Plate = row.LicensePlate,
            FilterType = entry.Type,
            Alarm = entry.Alarm,
            Notify = notify,
            Remark = entry.Remark,
            EquipmentId = row.EquipmentId,
            EquipmentName = camera?.Name,
            Km = camera?.Km,
            Met = camera?.Met,
            Zone = row.ZoneId,
            Direction = row.Direction,
            Lane = row.Lane,
            LaneId = camera?.Lane,
            Location = camera?.Location,
            Speed = row.Speed,
            Size = row.VehicleSize,
            DetectTime = at,
        };
}
