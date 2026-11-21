using Services.Vds.Shared.Domain;

namespace Services.Vds.Pipeline;

/// <summary>
/// Author: KienLT
/// Description: Cửa chống trùng LƯỢT XE ở biên nhận: cùng camera + cùng loại bản tin + cùng biển số, đến
/// hai lần trong cửa sổ thì chỉ lượt ĐẦU được ghi.
///
/// <para><b>Vì sao cần.</b> Thiết bị có lúc bắn một chiếc xe hai lần với <c>vendorEventId</c> KHÁC nhau.
/// Khoá chống trùng của QuestDB là <c>(vendorEventId, detectTime)</c> nên hai id khác nhau tạo hai dòng
/// thật, và số đếm — vốn được TÍNH LẠI từ bảng thô — phồng lên đúng bằng số lần bắn lặp. Bản cũ chặn việc
/// này ở tầng phân tích (<c>AnalysisProjector</c> + <c>FindRecentByPlateAsync</c>); tầng đó đã bỏ nên phải
/// dựng lại cửa, nhưng lần này trong RAM thay vì một truy vấn DB cho mỗi lượt xe.</para>
///
/// <para><b>Khoá có loại bản tin</b>: một lượt xe có thể sinh nhiều bản tin khác loại (đi qua + quá tốc độ).
/// Bỏ loại ra khỏi khoá thì bản tin quá tốc độ bị coi là bản trùng của lượt đi qua và MẤT hẳn sự kiện.</para>
///
/// <para><b>Xe không đọc được biển thì KHÔNG chặn.</b> Không có biển là không định danh được, mà chặn theo
/// phỏng đoán (giờ gần nhau + tốc độ/cỡ giống) thì rủi ro bỏ mất xe thật. Biển quá ngắn cũng coi như không
/// đọc được: hai xe khác nhau cùng bị nhận dạng nhầm ra một chuỗi rác ngắn sẽ khiến một xe bị bỏ, và HỤT số
/// đếm là loại sai tệ hơn phồng số đếm vì nó âm thầm.</para>
///
/// <para><b>An toàn với retry</b> — đây là điểm dễ sai nhất. Wolverine retry frame khi ghi DB lỗi, và lần
/// retry đi lại đúng đường này. Nếu cửa chỉ nhớ mốc thời gian thì lần retry sẽ gặp DẤU CỦA CHÍNH NÓ và bị
/// chặn ⇒ dòng dữ liệu mất luôn. Vì vậy cửa nhớ kèm <c>vendorEventId</c>: id đến trùng id đã nhớ nghĩa là
/// chính bản tin đó quay lại, cho qua.</para>
///
/// <para><b>Bộ nhớ bị chặn hai lớp.</b> Lớp một là <b>dọn theo nhịp</b>: cứ mỗi quãng bằng đúng một cửa sổ
/// thì quét bỏ khoá đã hết cửa sổ, nên số khoá giữ lại xấp xỉ lượng xe đi qua TRONG một cửa sổ chứ không
/// leo dần tới trần. Lớp hai là trần <c>maxKeys</c> làm chốt an toàn cho dữ liệu bất thường. Trước đây chỉ
/// có lớp hai: khoá chết nằm lại tới khi chạm trần rồi mới bị dọn bằng một lượt sắp xếp toàn bộ từ điển —
/// vừa giữ bộ nhớ cao vô ích, vừa tạo gai chậm ngay trên đường nhận (queue chạy tuần tự).</para>
///
/// <para><b>Vẫn thuần RAM, không I/O, không nhận DI</b> ⇒ kiểm được trực tiếp bằng đồng hồ truyền vào.
/// Việc lưu ra đĩa để sống qua restart do <c>VehicleDedupStateStore</c> làm, qua
/// <see cref="Export"/>/<see cref="Import"/> — tách ra để cửa không phải biết đến file, và để thay cách lưu
/// mà không chạm vào luật chặn.</para>
/// Created date: 28/08/2026
/// </summary>
public sealed class VehicleDedupGate
{
    private readonly Dictionary<string, Seen> _lastSeen = new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private long _blocked;

    /// <summary>Mốc của lượt quét dọn gần nhất; <c>null</c> = chưa quét lần nào.</summary>
    private DateTime? _lastSweepAt;

    /// <summary>Mốc của lượt đã cho qua gần nhất trên một khoá.</summary>
    /// <param name="EventType">
    /// Tên loại đã quy chuẩn. Giữ RIÊNG chứ không tách lại từ <c>Key</c>: khoá có hai dạng
    /// (<c>camera|loại</c> và <c>camera|loại|biển</c>), tách chuỗi để suy ra loại là dựng thêm một chỗ có thể
    /// lệch với hàm dựng khoá. Chuỗi này lấy từ <see cref="VdsEventTypes.Canonical"/> nên các mục cùng loại
    /// dùng chung một tham chiếu, không tốn thêm bộ nhớ đáng kể.
    /// </param>
    private readonly record struct Seen(DateTime At, long VendorEventId, string EventType, TimeSpan Window);

    /// <summary>
    /// Lượt xe này có được ghi không. Trả true đồng thời GHI NHẬN mốc — gọi đúng MỘT lần cho mỗi frame,
    /// ngay trước khi ghi bảng dữ liệu thô.
    /// </summary>
    /// <param name="equipmentId">Khoá camera trong danh mục; rỗng ⇒ không chặn (chưa định danh được chỗ).</param>
    /// <param name="eventType">Loại bản tin; được quy về tên chuẩn để tên legacy không tách thành khoá riêng.</param>
    /// <param name="licensePlate">Biển số NGUYÊN VĂN của bản tin; hàm tự chuẩn hoá.</param>
    /// <param name="vendorEventId">Id bản tin phía thiết bị — dùng để nhận ra lượt retry của chính nó.</param>
    /// <param name="at">Thời điểm xe đi qua (<c>detectTime</c>).</param>
    /// <param name="window">Cửa sổ chặn; <see cref="TimeSpan.Zero"/> hoặc âm = tắt cửa.</param>
    /// <param name="minPlateLength">Độ dài tối thiểu của biển đã chuẩn hoá để được coi là định danh được.</param>
    /// <param name="maxKeys">Trần số khoá giữ trong bộ nhớ.</param>
    /// <param name="checkPlate">
    /// Loại này có SO BIỂN không (danh mục <c>vds_event_check_plate</c>).
    /// <list type="bullet">
    /// <item><c>true</c> (Normal/Speeding): khoá = camera + loại + BIỂN. Khác biển ⇒ hai xe ⇒ cho qua cả hai.
    /// Không đọc được biển ⇒ CHO QUA (không coi "không nhận dạng được" là "cùng xe").</item>
    /// <item><c>false</c> (cháy, tắc…): khoá = camera + loại, KHÔNG xét biển — các loại này không gắn với một
    /// chiếc xe cụ thể, nên bản tin lặp của cùng hiện tượng tại cùng camera là trùng.</item>
    /// </list>
    /// </param>
    public bool TryEnter(
        string? equipmentId,
        string? eventType,
        string? licensePlate,
        long vendorEventId,
        DateTime at,
        TimeSpan window,
        int minPlateLength,
        int maxKeys,
        bool checkPlate = true)
    {
        if (window <= TimeSpan.Zero) return true;
        if (string.IsNullOrWhiteSpace(equipmentId)) return true;

        var canonicalType = VdsEventTypes.Canonical(eventType);
        string key;

        if (checkPlate)
        {
            var plate = PlateRules.Normalize(licensePlate);
            if (plate.Length < Math.Max(1, minPlateLength)) return true;
            key = $"{equipmentId.Trim()}|{canonicalType}|{plate}";
        }
        else
        {
            // Không so biển: mọi bản tin cùng loại tại cùng camera dùng CHUNG một khoá.
            key = $"{equipmentId.Trim()}|{canonicalType}";
        }

        lock (_sync)
        {
            if (_lastSeen.TryGetValue(key, out var last))
            {
                // Chính bản tin đó quay lại (Wolverine retry sau lỗi ghi DB) ⇒ phải cho qua, nếu không thì
                // frame bị chính dấu vết của nó chặn và mất dòng dữ liệu.
                if (last.VendorEventId == vendorEventId) return true;

                // So TRỊ TUYỆT ĐỐI để bản tin đến lệch thứ tự thời gian không mở lại cửa.
                if ((at - last.At).Duration() < window)
                {
                    _blocked++;

                    // KHÔNG cập nhật mốc khi chặn: giữ nguyên id của lượt đã cho qua để lượt retry của
                    // chính lượt đó vẫn nhận ra được, và để một chuỗi bản tin trùng liên tiếp không kéo dài
                    // cửa sổ vô hạn (làm mất xe thật đi sau đó).
                    return false;
                }
            }

            _lastSeen[key] = new Seen(at, vendorEventId, canonicalType, window);

            // Dọn theo NHỊP, lấy chính detectTime làm đồng hồ nên không cần phụ thuộc thời gian hệ thống
            // (giữ được tính kiểm thử được của cửa). Nhịp = đúng một cửa sổ: quét dày hơn thì tốn công vô
            // ích vì chưa có khoá nào kịp hết hạn, thưa hơn thì khoá chết nằm lại lâu.
            if (_lastSweepAt is null)
            {
                _lastSweepAt = at;
            }
            else if ((at - _lastSweepAt.Value).Duration() >= window)
            {
                SweepExpired(at);
                _lastSweepAt = at;
            }

            if (_lastSeen.Count > Math.Max(1, maxKeys)) Trim(at, maxKeys);
            return true;
        }
    }

    /// <summary>Số khoá đang giữ trong bộ nhớ — để chẩn đoán và kiểm thử.</summary>
    public int TrackedKeys
    {
        get { lock (_sync) return _lastSeen.Count; }
    }

    /// <summary>Tổng số lượt đã chặn từ lúc khởi động — để đối chiếu mức phồng số đếm trước/sau.</summary>
    public long BlockedCount
    {
        get { lock (_sync) return _blocked; }
    }

    /// <summary>
    /// Ảnh chụp trạng thái để ghi ra đĩa. Trả bản SAO nên bên gọi ghi file thoải mái mà không giữ lock.
    /// </summary>
    public IReadOnlyList<VehicleDedupSnapshot> Export()
    {
        lock (_sync)
        {
            var items = new List<VehicleDedupSnapshot>(_lastSeen.Count);
            foreach (var (key, seen) in _lastSeen)
                items.Add(new VehicleDedupSnapshot(key, seen.EventType, seen.At, seen.VendorEventId, seen.Window.TotalSeconds));
            return items;
        }
    }

    /// <summary>
    /// Nạp lại trạng thái đã ghi ra đĩa. Gọi MỘT lần lúc khởi động, TRƯỚC bản tin đầu tiên.
    ///
    /// <para>Khoá đang có trong RAM KHÔNG bị ghi đè: nếu vì lý do nào đó bản tin đã chạy trước lượt nạp thì
    /// mốc mới (đúng hơn) phải thắng mốc cũ đọc từ file.</para>
    /// </summary>
    /// <param name="entries">Các mục đọc từ file.</param>
    /// <param name="cutoffUtc">Bỏ mục có mốc cũ hơn giá trị này; <c>null</c> = nhận hết.</param>
    /// <returns>Số mục thực sự được nạp.</returns>
    public int Import(IEnumerable<VehicleDedupSnapshot> entries, DateTime? cutoffUtc = null)
    {
        var loaded = 0;

        lock (_sync)
        {
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Key)) continue;
                if (cutoffUtc is not null && entry.At < cutoffUtc.Value) continue;
                if (_lastSeen.ContainsKey(entry.Key)) continue;
                if (entry.WindowSeconds is {} seconds && (!double.IsFinite(seconds) || seconds < 0 || seconds > 2592000)) continue;

                _lastSeen[entry.Key] = new Seen(
                    entry.At, entry.VendorEventId, VdsEventTypes.Canonical(entry.EventType), TimeSpan.FromSeconds(entry.WindowSeconds ?? 30 * 86400));
                loaded++;
            }
        }

        return loaded;
    }

    /// <summary>Bỏ mọi khoá đã hết cửa sổ. Gọi TRONG lock.</summary>
    private void SweepExpired(DateTime now)
    {
        List<string>? expired = null;

        foreach (var (key, seen) in _lastSeen)
        {
            if (now < seen.At || now - seen.At < seen.Window) continue;
            (expired ??= new List<string>()).Add(key);
        }

        if (expired is null) return;
        foreach (var key in expired) _lastSeen.Remove(key);
    }

    /// <summary>Dọn khoá đã hết cửa sổ; còn vượt trần thì bỏ khoá cũ nhất. Gọi TRONG lock.</summary>
    private void Trim(DateTime now, int maxKeys)
    {
        SweepExpired(now);

        var overflow = _lastSeen.Count - Math.Max(1, maxKeys);
        if (overflow <= 0) return;

        foreach (var key in _lastSeen
                     .OrderBy(kv => kv.Value.At)
                     .Take(overflow)
                     .Select(kv => kv.Key)
                     .ToList())
        {
            _lastSeen.Remove(key);
        }
    }
}

/// <summary>
/// Author: KienLT
/// Description: Một mục trạng thái của <see cref="VehicleDedupGate"/> dùng để ghi/đọc đĩa.
///
/// Cố ý KHÔNG mang thuộc tính JSON: cửa chống trùng không được biết nó đang được lưu bằng định dạng gì.
/// Việc ánh xạ sang JSON là của <c>VehicleDedupStateStore</c>.
/// Created date: 09/09/2026
/// </summary>
/// <param name="Key">Khoá chặn đã dựng sẵn (<c>camera|loại</c> hoặc <c>camera|loại|biển</c>).</param>
/// <param name="EventType">Tên loại đã quy chuẩn — dùng để tách file theo loại mà không phải phân tích khoá.</param>
/// <param name="At">Thời điểm của lượt đã cho qua (<c>detectTime</c>, UTC).</param>
/// <param name="VendorEventId">Id bản tin phía thiết bị của lượt đã cho qua.</param>
public readonly record struct VehicleDedupSnapshot(
    string Key, string EventType, DateTime At, long VendorEventId, double? WindowSeconds = null);
