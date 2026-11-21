namespace Services.Vds.Features.VehicleFilter;

/// <summary>Trạng thái chuyến đang mở của một biển: mã chuyến, lượt gần nhất và camera của lượt đó.</summary>
public sealed record PlateTripState(string TripId, DateTime LastAt, string? LastEquipmentId);

/// <summary>Kết quả xếp một lượt khớp vào chuyến.</summary>
/// <param name="TripId">Mã chuyến của lượt (mới hoặc nối tiếp).</param>
/// <param name="IsNewTrip">true ⇒ lượt mở chuyến mới — chỉ lượt này được thông báo.</param>
/// <param name="Reason">Lý do (log/kiểm thử): first, gap, backward, continue, same-camera, out-of-order.</param>
public readonly record struct TripDecision(string TripId, bool IsNewTrip, string Reason);

/// <summary>
/// Author: KienLT
/// Description: Gộp các lượt khớp danh sách lọc của CÙNG một biển thành CHUYẾN (A → B → C → D). Thay cho cửa
/// chống dội cũ (<c>PlateNotifyGate</c>) — cửa đó tính cửa sổ từ LẦN THÔNG BÁO trước nên chuyến dài hơn cửa sổ bị
/// báo lại giữa đường; dự án ~2 km một camera thì gần như mỗi camera một thông báo.
///
/// <para><b>Luật chuyến</b> (<see cref="Decide"/>), lượt mới mở chuyến mới khi:
/// <list type="bullet">
/// <item>biển chưa có chuyến đang mở;</item>
/// <item>cách lượt gần nhất quá <c>gap</c> (khoảng nghỉ, cấu hình <c>vds_vehicle_filter_trip_gap</c>);</item>
/// <item>camera mới KHÔNG nằm phía trước camera lần trước theo hướng đi (xe quay đầu, sang hướng ngược lại, hoặc
/// xuất hiện lại ở camera phía sau) — "phía trước" tra bằng đoạn A→B của topology.</item>
/// </list>
/// Cùng camera lặp lại (camera bắn lại cùng lượt) hoặc frame đến lệch thứ tự trong khoảng nghỉ ⇒ nối chuyến.</para>
///
/// <para>Thuần RAM; trạng thái mất khi service khởi động lại thì bên gọi nạp lại từ lịch sử
/// (<c>seed</c>) để không mở chuyến mới — tức không thông báo lặp — cho xe đang đi dở.</para>
/// Created date: 23/10/2026
/// </summary>
public sealed class PlateTripTracker
{
    private readonly object _sync = new();
    private readonly Dictionary<string, PlateTripState> _trips = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Biển đã có chuyến trong RAM chưa — chưa có thì bên gọi tra lịch sử để nạp <c>seed</c>.</summary>
    public bool IsTracked(string plate)
    {
        lock (_sync) return _trips.ContainsKey(plate);
    }

    /// <summary>Số biển đang theo dõi — để chẩn đoán và kiểm thử.</summary>
    public int TrackedPlates
    {
        get { lock (_sync) return _trips.Count; }
    }

    /// <summary>
    /// Xếp một lượt vào chuyến và GHI NHẬN trạng thái. Gọi đúng MỘT lần cho mỗi lượt khớp, TRƯỚC khi ghi lịch sử
    /// (lịch sử lưu mã chuyến).
    /// </summary>
    /// <param name="plate">Biển đã chuẩn hoá.</param>
    /// <param name="at">Thời điểm xe đi qua (UTC).</param>
    /// <param name="equipmentId">Camera bắt được xe; null ⇒ không xét hướng.</param>
    /// <param name="gap">Khoảng nghỉ tối đa trong một chuyến.</param>
    /// <param name="maxPlates">Trần số biển giữ trong bộ nhớ.</param>
    /// <param name="isForward">(cameraTrước, cameraSau) ⇒ camera sau nằm phía trước theo hướng đi.</param>
    /// <param name="seed">Chuyến đọc từ lịch sử khi biển chưa có trong RAM (sau khởi động lại); null nếu không có.</param>
    public TripDecision Track(string plate, DateTime at, string? equipmentId, TimeSpan gap, int maxPlates,
        Func<string, string, bool> isForward, PlateTripState? seed = null)
    {
        lock (_sync)
        {
            var previous = _trips.TryGetValue(plate, out var state) ? state : seed;
            var decision = Decide(previous, at, equipmentId, gap, isForward, () => Guid.NewGuid().ToString());

            if (decision.IsNewTrip)
                _trips[plate] = new PlateTripState(decision.TripId, at, equipmentId);
            else if (previous is not null && at >= previous.LastAt)
                _trips[plate] = previous with { LastAt = at, LastEquipmentId = equipmentId ?? previous.LastEquipmentId };
            else if (previous is not null)
                _trips[plate] = previous; // frame lệch thứ tự: giữ mốc mới nhất, chỉ đảm bảo seed được nhớ lại

            if (_trips.Count > Math.Max(1, maxPlates)) Trim(at, gap, maxPlates);
            return decision;
        }
    }

    /// <summary>Luật chuyến THUẦN (không trạng thái) — kiểm thử trực tiếp.</summary>
    public static TripDecision Decide(PlateTripState? previous, DateTime at, string? equipmentId, TimeSpan gap,
        Func<string, string, bool> isForward, Func<string> newTripId)
    {
        if (previous is null) return new(newTripId(), true, "first");
        if ((at - previous.LastAt).Duration() > gap) return new(newTripId(), true, "gap");

        // Frame đến muộn hơn lượt đã thấy (replay, camera lệch giờ) mà còn trong khoảng nghỉ: thuộc chuyến đang mở.
        if (at < previous.LastAt) return new(previous.TripId, false, "out-of-order");

        if (string.IsNullOrEmpty(equipmentId) || string.IsNullOrEmpty(previous.LastEquipmentId))
            return new(previous.TripId, false, "continue");
        if (string.Equals(equipmentId, previous.LastEquipmentId, StringComparison.OrdinalIgnoreCase))
            return new(previous.TripId, false, "same-camera");

        return isForward(previous.LastEquipmentId, equipmentId)
            ? new(previous.TripId, false, "continue")
            : new(newTripId(), true, "backward");
    }

    /// <summary>Dọn biển đã hết khoảng nghỉ; còn vượt trần thì bỏ biển cũ nhất. Gọi TRONG lock.</summary>
    private void Trim(DateTime now, TimeSpan gap, int maxPlates)
    {
        foreach (var key in _trips.Where(kv => (now - kv.Value.LastAt).Duration() > gap).Select(kv => kv.Key).ToList())
            _trips.Remove(key);

        var overflow = _trips.Count - Math.Max(1, maxPlates);
        if (overflow <= 0) return;

        foreach (var key in _trips.OrderBy(kv => kv.Value.LastAt).Take(overflow).Select(kv => kv.Key).ToList())
            _trips.Remove(key);
    }
}
