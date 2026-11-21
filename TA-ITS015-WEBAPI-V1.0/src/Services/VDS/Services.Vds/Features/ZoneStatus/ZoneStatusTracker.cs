using System.Collections.Concurrent;
using System.Globalization;
using Services.Vds.Configuration;

namespace Services.Vds.Features.ZoneStatus;

/// <summary>
/// Trạng thái giao thông của một vùng.
///
/// <b>GIÁ TRỊ SỐ TĂNG THEO ĐỘ NẶNG</b> — máy trạng thái lấy MAX của các nguồn, nên thứ tự này là phần của
/// hợp đồng, đổi số là đổi logic.
/// </summary>
public enum ZoneCondition
{
    /// <summary>Thông thoáng.</summary>
    Normal = 0,

    /// <summary>Chậm — do sự kiện <c>Slow Moving</c>, hoặc do đánh giá theo tốc độ (nếu bật).</summary>
    Slow = 1,

    /// <summary>Nghẽn — CHỈ do sự kiện <c>Congestion</c>.</summary>
    Congested = 2,
}

/// <summary>Loại sự kiện của một bản tin, theo góc nhìn trạng thái vùng.</summary>
public enum ZoneEventKind
{
    /// <summary>Không phải sự kiện liên quan trạng thái vùng (lượt xe thường, sự cố khác).</summary>
    None = 0,

    /// <summary>Bản tin <c>Slow Moving</c> ⇒ bật chốt chậm.</summary>
    Slow = 1,

    /// <summary>Bản tin <c>Congestion</c> ⇒ bật chốt nghẽn.</summary>
    Congestion = 2,
}

/// <summary>Số liệu một vùng cần ghi xuống bảng trạng thái.</summary>
/// <param name="AverageSpeed">Trung bình cửa sổ trượt; null = không có mẫu nào trong cửa sổ.</param>
/// <param name="KeepAlive">
/// Lượt ghi CHỈ để giữ nhịp (trạng thái và số liệu không đổi, cập nhật lại <c>UpdateTime</c> cho dòng khỏi
/// quá hạn) — bên ghi dùng để hạ mức log.
/// </param>
public readonly record struct ZoneStatusSnapshot(
    string ZoneId, ZoneCondition Condition, double? AverageSpeed, DateTime UpdateTime, bool KeepAlive = false)
{
    /// <summary>
    /// Chuỗi ghi vào cột tốc độ trung bình (cột kiểu chuỗi): một chữ số thập phân, không đơn vị.
    ///
    /// Định dạng theo <c>InvariantCulture</c> — máy chủ đặt locale Việt Nam sẽ ghi dấu phẩy thập phân và
    /// bên đọc số sẽ hiểu sai đơn vị.
    /// </summary>
    public string? AverageSpeedText =>
        AverageSpeed is double v ? v.ToString("F1", CultureInfo.InvariantCulture) : null;
}

/// <summary>
/// Author: KienLT
/// Description: Máy trạng thái tình trạng vùng — THUẦN RAM: không chạm DB, không nhận DI, mọi tham số đi
/// qua đối số. Nhờ vậy kiểm được toàn bộ hành vi mà không cần cơ sở dữ liệu.
///
/// <para><b>BA NGUỒN ĐỘC LẬP</b>, trạng thái cuối là bậc NẶNG NHẤT (đổi 07/10/2026):
/// <list type="bullet">
/// <item><b>Chốt nghẽn (sự kiện)</b>: bản tin <c>Congestion</c> ⇒ nghẽn, tự tắt sau
/// <c>CongestionEventTtlSeconds</c> kể từ bản tin nghẽn GẦN NHẤT. Đây là đường DUY NHẤT ra nghẽn.</item>
/// <item><b>Chốt chậm (sự kiện)</b>: bản tin <c>Slow Moving</c> ⇒ chậm, tự tắt sau <c>SlowEventTtlSeconds</c>.</item>
/// <item><b>Bậc theo tốc độ</b> (tuỳ chọn, <c>SpeedSlow:Enabled</c>): chỉ ra được thông hoặc chậm, theo
/// công thức ở <see cref="ZoneSpeedSlowOptions"/>, so với ngưỡng tối thiểu của vùng.</item>
/// </list></para>
///
/// <para><b>Vì sao bỏ nghẽn theo tốc độ</b>: bản trước so tốc độ với ba ngưỡng cố định 30/50/60 km/h, không
/// liên quan ngưỡng min/max của tuyến — tuyến khai 30–50 km/h thì xe chạy đúng luật vẫn bị coi là chậm và vùng
/// không bao giờ về thông thoáng. Thiết bị đã tự phát hiện nghẽn và xe chạy chậm theo ngưỡng làn mà service
/// đẩy xuống, nên sự kiện của nó là nguồn chính.</para>
///
/// <para><b>MỐC THỜI GIAN</b>: mọi phép tính cửa sổ/chốt/nhịp ghi dùng GIỜ HỆ THỐNG (tham số <c>now</c>),
/// không dùng thời điểm thiết bị ghi nhận — camera lệch giờ sẽ làm cửa sổ trượt vô nghĩa. Thời điểm thiết
/// bị chỉ dùng để chặn mẫu đến muộn và mẫu quá cũ.</para>
/// Created date: 26/08/2026
/// </summary>
public sealed class ZoneStatusTracker
{
    private readonly ConcurrentDictionary<string, ZoneState> _zones = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Số vùng đang theo dõi trong RAM.</summary>
    public int TrackedZones => _zones.Count;

    /// <summary>
    /// Nạp một quan sát vào vùng. Trả snapshot khi ĐẾN LÚC ghi DB (đổi trạng thái, hoặc số liệu đổi và đã
    /// qua nhịp ghi tối thiểu), ngược lại trả null.
    /// </summary>
    /// <param name="detectTime">Thời điểm thiết bị ghi nhận; chỉ dùng để chặn mẫu đến muộn/quá cũ.</param>
    /// <param name="speedKmh">Tốc độ lượt xe; null hoặc ≤ 0 ⇒ không tính là mẫu tốc độ.</param>
    /// <param name="eventKind">Bản tin này có phải sự kiện nghẽn/chậm không.</param>
    /// <param name="minSpeedKmh">
    /// Ngưỡng tối thiểu của vùng (vùng → mặc định hướng); null ⇒ không đánh giá chậm theo tốc độ được.
    /// </param>
    /// <param name="now">Giờ hệ thống.</param>
    /// <param name="fallbackDirection">
    /// Hướng camera của bản tin — lưu lại để <see cref="Sweep"/> tra lại ngưỡng của vùng theo danh mục hiện
    /// tại (vùng không khai hướng thì rơi về hướng này) mà không cần bản tin mới.
    /// </param>
    public ZoneStatusSnapshot? Observe(
        string zoneId,
        DateTime? detectTime,
        double? speedKmh,
        ZoneEventKind eventKind,
        int? minSpeedKmh,
        DateTime now,
        ZoneStatusOptions cfg,
        string? fallbackDirection = null)
    {
        if (string.IsNullOrWhiteSpace(zoneId)) return null;

        // Mẫu quá cũ so với giờ hệ thống (frame bù lọt lưới, camera lệch giờ) ⇒ bỏ hẳn. Xét TRƯỚC khi tạo
        // state để bản tin cũ không làm phát sinh một vùng theo dõi rỗng trong RAM.
        if (detectTime is DateTime old && (now - old).TotalSeconds > cfg.SampleMaxAgeSeconds) return null;

        var key = zoneId.Trim();
        var state = _zones.GetOrAdd(key, _ => new ZoneState());

        lock (state.Sync)
        {
            // Mẫu đến muộn (cũ hơn mẫu gần nhất đã xử lý) ⇒ bỏ. Thứ tự mẫu là phần của bằng chứng.
            if (detectTime is DateTime current && state.LastDetectAt is DateTime previous && current < previous)
                return null;
            if (detectTime is DateTime stamp) state.LastDetectAt = stamp;

            switch (eventKind)
            {
                case ZoneEventKind.Congestion: state.LastCongestionAt = now; break;
                case ZoneEventKind.Slow: state.LastSlowAt = now; break;
            }

            // Luôn thay ngưỡng, kể cả null, để xoá ngưỡng cũ khi danh mục không còn min hợp lệ.
            // Khi tắt SpeedSlow, updater truyền null; xoá luôn để không giữ ngưỡng cũ qua lần bật lại.
            state.MinSpeedKmh = minSpeedKmh is > 0 ? minSpeedKmh : null;
            state.FallbackDirection = string.IsNullOrWhiteSpace(fallbackDirection) ? null : fallbackDirection.Trim();

            if (speedKmh is double speed && speed > 0)
            {
                state.Samples.Enqueue((now, speed));
                PruneLocked(state, now, cfg);
            }

            return EvaluateLocked(key, state, now, cfg);
        }
    }

    /// <summary>
    /// Quét mọi vùng theo GIỜ: hạ chốt sự kiện hết hạn, trượt cửa sổ tốc độ.
    ///
    /// BẮT BUỘC phải có: các điều kiện đó theo thời gian, không có bản tin nào tới để kích hoạt việc hạ
    /// trạng thái.
    ///
    /// <para><paramref name="minSpeedOf"/> (khi bật <c>SpeedSlow</c>): tra LẠI V_min của từng vùng theo danh
    /// mục HIỆN TẠI trước khi đánh giá, với hướng camera của lần Observe gần nhất làm hướng dự phòng. Không
    /// có nó thì sau khi nạp lại danh mục, vùng không còn bản tin mới cứ đánh giá theo ngưỡng cũ tới khi cửa
    /// sổ rỗng. Hàm được gọi NGOÀI lock của vùng; trả null/không dương ⇒ xoá ngưỡng (như Observe). Tắt
    /// <c>SpeedSlow</c> hoặc không truyền ⇒ giữ ngưỡng đang lưu (không dùng tới khi tắt).</para>
    /// </summary>
    public IReadOnlyList<ZoneStatusSnapshot> Sweep(
        DateTime now, ZoneStatusOptions cfg, Func<string, string?, int?>? minSpeedOf = null)
    {
        var result = new List<ZoneStatusSnapshot>();
        var refresh = minSpeedOf is not null && cfg.SpeedSlow.Enabled;

        foreach (var (zoneId, state) in _zones)
        {
            // Tra ngưỡng NGOÀI lock: hàm của bên gọi không được giữ lock vùng (và có lỗi thì không kẹt vùng).
            // Observe chen giữa lúc tra có thể đổi hướng fallback ⇒ lượt này ghi min của hướng cũ; lượt quét
            // sau tra lại và sửa. Chấp nhận lệch một nhịp quét, không phải sai vĩnh viễn.
            var apply = false;
            int? freshMin = null;
            if (refresh)
            {
                string? fallback;
                lock (state.Sync) fallback = state.FallbackDirection;
                try
                {
                    freshMin = minSpeedOf!(zoneId, fallback);
                    apply = true;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // KHÔNG để lỗi tra ngưỡng của MỘT vùng làm hỏng cả lượt quét: Evaluate của các vùng đã xét
                    // đã đánh dấu "đã ghi" (MarkWritten) nên nếu Sweep ném, bên gọi không nhận danh sách và các
                    // thay đổi đó mất hẳn (vd chốt nghẽn hết hạn mà DB vẫn giữ conges). Lỗi ⇒ giữ ngưỡng đang có
                    // cho vùng này, lượt sau tra lại.
                }
            }

            lock (state.Sync)
            {
                if (apply) state.MinSpeedKmh = freshMin is > 0 ? freshMin : null;
                if (EvaluateLocked(zoneId, state, now, cfg) is ZoneStatusSnapshot due) result.Add(due);
            }
        }

        return result;
    }

    /// <summary>
    /// ÁP trạng thái một vùng — dùng cho harness và thao tác vận hành đặt tay.
    ///
    /// Đặt như thể vừa nhận sự kiện tương ứng, nên trạng thái áp tay cũng TỰ HẾT HẠN theo TTL của chốt đó:
    /// <list type="bullet">
    /// <item><see cref="ZoneCondition.Congested"/> ⇒ bật chốt nghẽn.</item>
    /// <item><see cref="ZoneCondition.Slow"/> ⇒ bật chốt chậm, hạ chốt nghẽn.</item>
    /// <item><see cref="ZoneCondition.Normal"/> ⇒ hạ cả hai chốt và XOÁ mẫu tốc độ, để đánh giá theo tốc độ
    /// (nếu bật) làm lại từ đầu thay vì kéo ngay về bậc chậm cũ.</item>
    /// </list>
    /// </summary>
    public ZoneStatusSnapshot ForceCondition(
        string zoneId, ZoneCondition condition, DateTime now, ZoneStatusOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var key = zoneId?.Trim() ?? string.Empty;
        var state = _zones.GetOrAdd(key, _ => new ZoneState());

        lock (state.Sync)
        {
            switch (condition)
            {
                case ZoneCondition.Congested:
                    state.LastCongestionAt = now;
                    break;

                case ZoneCondition.Slow:
                    state.LastCongestionAt = null;
                    state.LastSlowAt = now;
                    break;

                default:
                    state.LastCongestionAt = null;
                    state.LastSlowAt = null;
                    state.Samples.Clear();
                    state.SpeedLevel = ZoneCondition.Normal;
                    break;
            }

            PruneLocked(state, now, cfg);
            state.SpeedLevel = SpeedLevelLocked(state, cfg);
            var snapshot = new ZoneStatusSnapshot(
                key, ConditionLocked(state, now, cfg), AverageLocked(state), now);

            // Đánh dấu ĐÃ ghi ngay: người dùng vừa áp tay thì lượt quan sát kế tiếp không được coi đây là
            // "trạng thái mới" rồi ghi DB lần nữa với cùng giá trị.
            MarkWritten(state, snapshot, now);
            return snapshot;
        }
    }

    /// <summary>
    /// HẠ một chốt sự kiện của vùng ngay lập tức, không chờ hết TTL — tương ứng thao tác "huỷ sự kiện".
    ///
    /// KHÔNG đụng bậc theo tốc độ: xe vẫn chạy chậm thì vùng vẫn phải chậm. Huỷ sự kiện không có nghĩa là
    /// đường đã thông.
    /// </summary>
    /// <returns>
    /// null khi vùng chưa được theo dõi, hoặc khi hạ chốt mà trạng thái không đổi và chưa tới hạn ghi giữ nhịp
    /// (vùng còn chậm/nghẽn do nguồn khác thì vẫn có thể trả snapshot giữ nhịp).
    /// </returns>
    public ZoneStatusSnapshot? ClearEvent(
        string zoneId, ZoneEventKind kind, DateTime now, ZoneStatusOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        if (string.IsNullOrWhiteSpace(zoneId)) return null;
        if (!_zones.TryGetValue(zoneId.Trim(), out var state)) return null;

        lock (state.Sync)
        {
            switch (kind)
            {
                case ZoneEventKind.Congestion: state.LastCongestionAt = null; break;
                case ZoneEventKind.Slow: state.LastSlowAt = null; break;
                default: return null;
            }

            return EvaluateLocked(zoneId.Trim(), state, now, cfg);
        }
    }

    /// <summary>
    /// XOÁ dấu "đã ghi" của một vùng để lượt đánh giá sau ghi lại.
    ///
    /// Cần vì <see cref="Observe"/> và <see cref="Sweep"/> đánh dấu đã ghi NGAY khi trả snapshot, trước khi
    /// bên gọi kịp ghi DB. Ghi DB lỗi mà không xoá dấu thì lần đổi trạng thái đó mất hẳn: các lượt sau thấy
    /// "trạng thái không đổi so với lần ghi trước" nên im lặng, và DB đứng ở giá trị cũ cho tới lần đổi tiếp
    /// theo — bản cũ mắc đúng lỗi này.
    /// </summary>
    public void InvalidateWriteMark(string? zoneId)
    {
        if (string.IsNullOrWhiteSpace(zoneId)) return;
        if (!_zones.TryGetValue(zoneId.Trim(), out var state)) return;

        lock (state.Sync)
        {
            state.LastWrittenCondition = null;
            state.LastWrittenAvgText = null;
            state.LastWrittenAt = null;
        }
    }

    // ==================== Bên trong ====================

    /// <summary>Loại mẫu đã ra khỏi cửa sổ và chặn trần số mẫu. Gọi TRONG lock.</summary>
    private static void PruneLocked(ZoneState state, DateTime now, ZoneStatusOptions cfg)
    {
        var from = now.AddSeconds(-cfg.AvgWindowSeconds);
        while (state.Samples.Count > 0 && state.Samples.Peek().At < from) state.Samples.Dequeue();
        while (state.Samples.Count > cfg.AvgMaxSamples) state.Samples.Dequeue();
    }

    /// <summary>
    /// Bậc theo tốc độ (chỉ <see cref="ZoneCondition.Normal"/> hoặc <see cref="ZoneCondition.Slow"/>) theo
    /// công thức ở <see cref="ZoneSpeedSlowOptions"/>. Gọi TRONG lock, SAU khi đã prune cửa sổ.
    /// </summary>
    private static ZoneCondition SpeedLevelLocked(ZoneState state, ZoneStatusOptions cfg)
    {
        var slow = cfg.SpeedSlow;
        if (!slow.Enabled || !slow.IsUsableFor(cfg.AvgMaxSamples) || state.MinSpeedKmh is not int vMin)
            return ZoneCondition.Normal;

        var n = state.Samples.Count;
        if (n == 0) return ZoneCondition.Normal;

        var avg = state.Samples.Average(s => s.Speed);

        // Đang chậm ⇒ chỉ thoát khi trung bình vượt HẲN V_min. Không đòi đủ N: đường vắng phải về được thông.
        if (state.SpeedLevel == ZoneCondition.Slow)
            return avg >= vMin + slow.ClearMarginKmh ? ZoneCondition.Normal : ZoneCondition.Slow;

        if (n < slow.MinVehicles || avg >= vMin) return ZoneCondition.Normal;

        var slowShare = (double)state.Samples.Count(s => s.Speed < vMin) / n;
        return slowShare >= slow.SlowShare ? ZoneCondition.Slow : ZoneCondition.Normal;
    }

    /// <summary>Trạng thái = bậc NẶNG NHẤT giữa hai chốt sự kiện và bậc theo tốc độ. Gọi TRONG lock.</summary>
    private static ZoneCondition ConditionLocked(ZoneState state, DateTime now, ZoneStatusOptions cfg)
    {
        if (IsLatched(state.LastCongestionAt, now, cfg.CongestionEventTtlSeconds)) return ZoneCondition.Congested;

        var eventLevel = IsLatched(state.LastSlowAt, now, cfg.SlowEventTtlSeconds)
            ? ZoneCondition.Slow
            : ZoneCondition.Normal;
        return state.SpeedLevel > eventLevel ? state.SpeedLevel : eventLevel;
    }

    private static bool IsLatched(DateTime? at, DateTime now, int ttlSeconds)
        => at is DateTime t && (now - t).TotalSeconds <= ttlSeconds;

    private static double? AverageLocked(ZoneState state)
        => state.Samples.Count == 0 ? null : state.Samples.Average(s => s.Speed);

    /// <summary>
    /// Chốt số hiện tại và quyết định có ghi DB hay không. Gọi TRONG lock.
    ///
    /// Đổi trạng thái ⇒ ghi NGAY (người trực phải thấy ngay). Chỉ số liệu đổi ⇒ chờ đủ nhịp tối thiểu, vì
    /// tốc độ trung bình đổi theo từng lượt xe và ghi hết sẽ thành hàng nghìn lượt UPDATE mỗi phút.
    ///
    /// <para><b>Ghi giữ nhịp</b>: vùng đang chậm/nghẽn mà không có gì đổi thì vẫn ghi lại sau
    /// <see cref="ZoneStatusOptions.KeepAliveSeconds"/>. Không ghi thì sau <c>StaleSeconds</c> API TMS coi dòng
    /// là quá hạn và hạ về thông thoáng, trong khi chốt sự kiện ở đây vẫn còn — bản đồ và tiêu đề bigscreen
    /// nhảy về thông thoáng rồi bật lại khi có xe mới. Vùng thông thoáng không cần: quá hạn cũng là thông.</para>
    /// </summary>
    private static ZoneStatusSnapshot? EvaluateLocked(
        string zoneId, ZoneState state, DateTime now, ZoneStatusOptions cfg)
    {
        PruneLocked(state, now, cfg);
        state.SpeedLevel = SpeedLevelLocked(state, cfg);

        var condition = ConditionLocked(state, now, cfg);
        var snapshot = new ZoneStatusSnapshot(zoneId, condition, AverageLocked(state), now);

        var conditionChanged = state.LastWrittenCondition != condition;
        var valueChanged = !string.Equals(
            state.LastWrittenAvgText, snapshot.AverageSpeedText, StringComparison.Ordinal);
        var intervalElapsed = state.LastWrittenAt is null
                              || (now - state.LastWrittenAt.Value).TotalSeconds >= cfg.MinWriteIntervalSeconds;

        if (!conditionChanged && !(valueChanged && intervalElapsed))
        {
            var keepAliveDue = condition != ZoneCondition.Normal
                               && state.LastWrittenAt is DateTime lastWrite
                               && (now - lastWrite).TotalSeconds >= cfg.KeepAliveSeconds;
            if (!keepAliveDue) return null;
            snapshot = snapshot with { KeepAlive = !valueChanged };
        }

        MarkWritten(state, snapshot, now);
        return snapshot;
    }

    private static void MarkWritten(ZoneState state, ZoneStatusSnapshot snapshot, DateTime now)
    {
        state.LastWrittenCondition = snapshot.Condition;
        state.LastWrittenAvgText = snapshot.AverageSpeedText;
        state.LastWrittenAt = now;
    }

    private sealed class ZoneState
    {
        public readonly object Sync = new();

        /// <summary>Mẫu tốc độ trong cửa sổ trượt, mốc theo GIỜ HỆ THỐNG.</summary>
        public readonly Queue<(DateTime At, double Speed)> Samples = new();

        /// <summary>Bậc theo tốc độ — chỉ Normal/Slow; luôn Normal khi tắt <c>SpeedSlow</c>.</summary>
        public ZoneCondition SpeedLevel = ZoneCondition.Normal;

        /// <summary>Ngưỡng tối thiểu của vùng (km/h) ở lượt quan sát/quét gần nhất.</summary>
        public int? MinSpeedKmh;

        /// <summary>Hướng camera của lượt Observe gần nhất — hướng dự phòng khi Sweep tra lại ngưỡng.</summary>
        public string? FallbackDirection;

        /// <summary>Giờ hệ thống của bản tin <c>Congestion</c> gần nhất.</summary>
        public DateTime? LastCongestionAt;

        /// <summary>Giờ hệ thống của bản tin <c>Slow Moving</c> gần nhất.</summary>
        public DateTime? LastSlowAt;

        /// <summary>Thời điểm thiết bị ghi nhận của mẫu gần nhất; dùng để bỏ mẫu đến muộn.</summary>
        public DateTime? LastDetectAt;

        public ZoneCondition? LastWrittenCondition;
        public string? LastWrittenAvgText;
        public DateTime? LastWrittenAt;
    }
}
