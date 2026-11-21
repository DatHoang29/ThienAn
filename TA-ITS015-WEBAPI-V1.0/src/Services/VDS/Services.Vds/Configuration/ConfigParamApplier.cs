using System.Globalization;
using System.Text.Json;
using Services.Vds.Pipeline.Mapping;
using Services.Vds.Shared.Domain;
using Services.Vds.Shared.Messaging;

namespace Services.Vds.Configuration;

/// <summary>
/// Author: KienLT
/// Description: Áp MỘT tham số (tên + giá trị JSON) lên snapshot cấu hình, trả về snapshot MỚI.
///
/// Đây là danh sách trắng: chỉ tham số có tên trong <see cref="SupportedNames"/> mới đổi được lúc chạy.
/// Tên lạ hoặc giá trị không hợp lệ ⇒ trả <c>null</c>, bên gọi giữ nguyên snapshot đang có. Làm vậy để
/// một bản tin cấu hình gõ sai không thể phá cấu hình đang chạy.
///
/// Hàm THUẦN, tất định — không đọc file, không đụng DB, không log. Nhờ vậy test được mà không cần DI.
/// Created date: 24/08/2026
/// </summary>
public static class ConfigParamApplier
{
    /// <summary>Ngưỡng tốc độ theo hướng. Giá trị: <c>{"1":{"min":50,"max":90},"2":{...}}</c></summary>
    public const string SpeedRangeByDirection = nameof(SpeedRangeByDirection);

    /// <summary><c>ConfigId</c> kết nối QuestDB. Giá trị: <c>"VdsDefault"</c> hoặc <c>""</c> để tắt.</summary>
    public const string QuestConnectionId = nameof(QuestConnectionId);

    /// <summary>
    /// Chu kỳ thống kê của thiết bị (phút), 1–60. Giá trị: <c>5</c>.
    ///
    /// Đi qua đây thay vì gán thẳng vào snapshot (bản cũ gán thẳng) để giữ đúng một cửa kiểm tính hợp lệ và
    /// để giá trị người vận hành đặt trên web sống qua restart nhờ file override.
    /// </summary>
    public const string StatisticPeriodMinutes = nameof(StatisticPeriodMinutes);

    /// <summary>
    /// Mapping loại sự kiện VDS ↔ mã sự cố ITS (<c>TmsEventType.Code</c>). Giá trị:
    /// <c>{"Speeding":"VDS_SPEEDING","Congestion":"VDS_CONGESTION",...}</c>. Rỗng/không hợp lệ ⇒ giữ map
    /// đang có (fallback appsettings). Key rỗng bị bỏ; map rỗng hoàn toàn ⇒ từ chối.
    /// </summary>
    public const string EventTypeCodes = nameof(EventTypeCodes);

    /// <summary>
    /// Mapping cỡ xe: ngưỡng chiều dài + đơn vị + chế độ. Giá trị:
    /// <c>{"mode":"length","shortMax":6.0,"mediumMax":12.0,"unit":"meter"}</c>. Trường thiếu ⇒ giữ giá trị
    /// đang có; ngưỡng vô lý (shortMax &gt;= mediumMax, không dương) ⇒ từ chối cả cụm.
    /// </summary>
    public const string VehicleSizeMapping = nameof(VehicleSizeMapping);

    /// <summary>
    /// Thư mục GỐC lưu ảnh/clip. Giá trị: đường dẫn tuyệt đối (<c>"D:\\wwwroot\\VdsImages"</c>) hoặc tương
    /// đối (neo vào thư mục chứa exe). Chuỗi rỗng ⇒ từ chối, giữ giá trị đang chạy.
    ///
    /// Đây là <b>hợp đồng hai chiều</b>: worker ghi ảnh dưới thư mục này, WebAPI đọc lại từ cùng một chỗ.
    /// Vì vậy nó thuộc <b>cấu hình vận hành</b> (<c>VdsConfig</c>) — một nơi khai, hai bên cùng đọc — thay
    /// vì mỗi bên khai riêng trong appsettings rồi trôi lệch.
    /// </summary>
    public const string MediaStorageDirectory = nameof(MediaStorageDirectory);

    /// <summary>
    /// Nhóm hướng của payload đếm xe. Giá trị JSON: mảng
    /// <c>[{"key":"1","name":"CL → HN","equipmentIds":["id1"]},{"key":"2","equipmentIds":["id1","id2"]}]</c>.
    /// Nguồn: <c>VdsConfig</c> các khoá <c>vds_count_direction_equipment_{key}</c> (ID phân tách phẩy),
    /// do <c>OpConfigManager</c> gom lại. Mảng rỗng / không hợp lệ ⇒ giữ nhóm đang có (fallback appsettings).
    /// </summary>
    public const string CountDirectionGroups = nameof(CountDirectionGroups);

    /// <summary>
    /// Cửa SKIP (ghi thô) theo loại + mặc định, đã quy về GIÂY. Giá trị JSON:
    /// <c>{"default":10,"byType":{"Normal":10,"Conflagration":60}}</c>.
    /// Nguồn: <c>SysConfig</c> danh mục <c>vds_event_skip</c> + <c>VdsConfig</c> khoá
    /// <c>vds_skip_default</c>; <c>OpConfigManager</c> parse hậu tố (s/m/h/d) trước khi áp.
    /// Không có mục nào hợp lệ ⇒ giữ cấu hình đang có.
    /// </summary>
    public const string EventSkipWindows = nameof(EventSkipWindows);

    /// <summary>
    /// Cửa DEDUP (tạo sự cố) theo loại + mặc định, đã quy về GIÂY. Cùng dạng JSON với
    /// <see cref="EventSkipWindows"/>. Nguồn: danh mục <c>vds_event_dedup</c> + <c>vds_dedup_default</c>.
    /// </summary>
    public const string EventDedupWindows = nameof(EventDedupWindows);

    /// <summary>
    /// Danh sách loại CÓ so biển số. Giá trị JSON: <c>["Normal","Speeding"]</c>.
    /// Nguồn: <c>SysConfig</c> danh mục <c>vds_event_check_plate</c> (con chỉ có Code, không thời lượng).
    /// Mảng rỗng là HỢP LỆ và có nghĩa "không loại nào so biển".
    /// </summary>
    public const string EventCheckPlateTypes = nameof(EventCheckPlateTypes);

    /// <summary>
    /// Tham số trạng thái vùng. Giá trị JSON (mọi trường tuỳ chọn, thiếu ⇒ giữ giá trị đang có):
    /// <c>{"enabled":true,"minVehicles":5,"slowShare":0.6,"clearMarginKmh":5,"windowSeconds":300,
    /// "slowEventTtlSeconds":60,"congestionEventTtlSeconds":60,"staleSeconds":30}</c>. Nguồn: <c>VdsConfig</c>
    /// nhóm <c>vds_config</c> các khoá <c>vds_zone_slow_*</c> và <c>vds_zone_congestion_event_ttl</c>, nhóm
    /// <c>vds_config_default</c> khoá <c>vds_zone_stale_seconds</c>; <c>OpConfigManager</c> đọc và quy đổi đơn vị
    /// trước khi áp.
    /// Sau khi áp mà bộ tham số không hợp lệ ⇒ từ chối cả cụm.
    /// </summary>
    public const string ZoneSpeedSlow = nameof(ZoneSpeedSlow);

    /// <summary>
    /// Loại sự kiện được nhờ TMS lập lịch tự động. Giá trị JSON: <c>["speeding","fire"]</c> (mã
    /// <c>TmsEventType.Code</c>); mảng rỗng = không loại nào. Nguồn: suy từ cấu hình tự động TMS
    /// (<c>VdsCatalogRepository.QueryAutomationEventTypesAsync</c>). Author: KienLT - 14/10/2026, đổi nguồn 17/10/2026
    /// </summary>
    public const string AutomationEventTypes = nameof(AutomationEventTypes);

    /// <summary>
    /// Khoảng nghỉ gom chuyến lọc xe (giây). Giá trị JSON: số nguyên <c>600</c>, 60..86400. Nguồn: <c>VdsConfig</c>
    /// nhóm <c>vds_config_default</c> khoá <c>vds_vehicle_filter_trip_gap</c> (dạng 10m/1h, <c>OpConfigManager</c> quy đổi).
    /// Author: KienLT - 23/10/2026
    /// </summary>
    public const string VehicleFilterTripGap = nameof(VehicleFilterTripGap);

    // KienLT - 31/10/2026: tham số vận hành QuestDB HA đổi lúc chạy (CODING_RULES §11).
    /// <summary>
    /// Gói tham số vận hành QuestDB HA. Giá trị JSON: <see cref="global::Shared.QuestHa.Options.QuestHaRuntimeOptions"/> (thời lượng dạng
    /// <c>"00:00:30"</c>, trường thiếu/null = dùng appsettings). Nguồn: <c>VdsConfig</c> nhóm <c>vds_config</c> mã <c>vds_questha_*</c>;
    /// <c>OpConfigManager</c> quy đổi hậu tố và chỉ lưu gói khi <c>QuestHaManager.ApplySettings</c> đã nhận. Ở đây chỉ kiểm từng giá
    /// trị đã khai (biên, Warn &lt; Critical); kiểm cả gói sau khi gộp nằm ở QuestHaManager.
    /// </summary>
    public const string QuestHaRuntime = nameof(QuestHaRuntime);

    /// <summary>Các tham số đổi được lúc chạy. Thêm tham số mới thì khai ở đây và xử lý trong <see cref="BuildNext"/>.</summary>
    public static readonly IReadOnlySet<string> SupportedNames =
        new HashSet<string>(StringComparer.Ordinal)
        {
            SpeedRangeByDirection, QuestConnectionId, StatisticPeriodMinutes,
            EventTypeCodes, VehicleSizeMapping, MediaStorageDirectory, CountDirectionGroups,
            EventSkipWindows, EventDedupWindows, EventCheckPlateTypes, ZoneSpeedSlow,
            AutomationEventTypes, VehicleFilterTripGap, QuestHaRuntime,
        };

    /// <summary>Dựng snapshot mới từ <paramref name="current"/> với tham số đã đổi.</summary>
    /// <returns>Snapshot mới, hoặc <c>null</c> nếu tên không hỗ trợ / giá trị không hợp lệ.</returns>
    public static ServiceConfig? BuildNext(ServiceConfig current, string parameterName, string json)
    {
        var name = parameterName?.Trim() ?? "";
        if (!SupportedNames.Contains(name)) return null;

        return name switch
        {
            SpeedRangeByDirection => ApplySpeedRanges(current, json),
            QuestConnectionId => ApplyQuestConnectionId(current, json),
            StatisticPeriodMinutes => ApplyStatisticPeriod(current, json),
            EventTypeCodes => ApplyEventTypeCodes(current, json),
            VehicleSizeMapping => ApplyVehicleSizeMapping(current, json),
            MediaStorageDirectory => ApplyMediaStorageDirectory(current, json),
            CountDirectionGroups => ApplyCountDirectionGroups(current, json),
            EventSkipWindows => ApplyEventWindows(current, json, skip: true),
            EventDedupWindows => ApplyEventWindows(current, json, skip: false),
            EventCheckPlateTypes => ApplyEventCheckPlateTypes(current, json),
            ZoneSpeedSlow => ApplyZoneSpeedSlow(current, json),
            AutomationEventTypes => ApplyAutomationEventTypes(current, json),
            VehicleFilterTripGap => ApplyVehicleFilterTripGap(current, json),
            QuestHaRuntime => ApplyQuestHaRuntime(current, json),
            _ => null,
        };
    }

    // KienLT - 31/10/2026: gói sai một giá trị ⇒ từ chối cả gói, không lưu override nửa vời.
    /// <summary>Payload <see cref="QuestHaRuntime"/>: JSON đọc được và mọi giá trị đã khai hợp lệ, không thì <c>null</c>.</summary>
    private static ServiceConfig? ApplyQuestHaRuntime(ServiceConfig current, string json)
    {
        global::Shared.QuestHa.Options.QuestHaRuntimeOptions? runtime;
        try
        {
            runtime = JsonSerializer.Deserialize<global::Shared.QuestHa.Options.QuestHaRuntimeOptions>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (runtime is null || runtime.Validate().Count > 0) return null;

        var next = current.Clone();
        next.QuestHaRuntime = runtime;
        return next;
    }

    /// <summary>
    /// Payload <see cref="ZoneSpeedSlow"/>. Số đã ở đơn vị chuẩn (giây, km/h, tỉ lệ 0..1) — bên gọi quy đổi
    /// hậu tố/phần trăm trước. Trường thiếu ⇒ giữ giá trị đang có; kết quả cuối không hợp lệ ⇒ từ chối cả cụm,
    /// để không bao giờ chạy với nửa bộ tham số mới nửa cũ mà sai.
    /// </summary>
    private static ServiceConfig? ApplyZoneSpeedSlow(ServiceConfig current, string json)
    {
        JsonElement root;
        try
        {
            root = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (root.ValueKind != JsonValueKind.Object) return null;

        var next = current.Clone();
        var zone = next.ZoneStatus;
        var slow = zone.SpeedSlow;

        if (root.TryGetProperty("enabled", out var enabledEl))
        {
            if (enabledEl.ValueKind is JsonValueKind.True or JsonValueKind.False) slow.Enabled = enabledEl.GetBoolean();
            else return null;
        }

        if (TryGetDouble(root, "minVehicles", out var minVehicles))
        {
            if (minVehicles != Math.Floor(minVehicles)) return null;
            slow.MinVehicles = (int)minVehicles;
        }

        if (TryGetDouble(root, "slowShare", out var share)) slow.SlowShare = share;
        if (TryGetDouble(root, "clearMarginKmh", out var margin)) slow.ClearMarginKmh = margin;

        if (TryGetDouble(root, "windowSeconds", out var window))
        {
            if (window != Math.Floor(window)) return null;
            zone.AvgWindowSeconds = (int)window;
        }

        if (TryGetDouble(root, "slowEventTtlSeconds", out var ttl))
        {
            if (ttl != Math.Floor(ttl)) return null;
            zone.SlowEventTtlSeconds = (int)ttl;
        }

        if (TryGetDouble(root, "congestionEventTtlSeconds", out var congestionTtl))
        {
            if (congestionTtl != Math.Floor(congestionTtl)) return null;
            zone.CongestionEventTtlSeconds = (int)congestionTtl;
        }

        if (TryGetDouble(root, "staleSeconds", out var stale))
        {
            // Cùng biên API TMS chấp nhận (1s..1 ngày); ngoài biên TMS dùng mặc định nên worker cũng không nhận.
            if (stale != Math.Floor(stale) || stale is < 1 or > 86400) return null;
            zone.StaleSeconds = (int)stale;
        }

        // MinVehicles vượt trần mẫu thì không thể vào chậm; cửa sổ/TTL 0 làm mất mẫu/chốt sự kiện.
        if (!slow.IsUsableFor(zone.AvgMaxSamples) || zone.AvgWindowSeconds <= 0 || zone.SlowEventTtlSeconds <= 0
            || zone.CongestionEventTtlSeconds <= 0 || zone.StaleSeconds <= 0)
            return null;

        return next;
    }

    /// <summary>
    /// Payload cửa Skip/Dedup: <c>{"default":10,"byType":{"Normal":10}}</c>. Số đã là GIÂY (bên gọi parse
    /// hậu tố trước). Bỏ mục có key rỗng hoặc số âm; <c>0</c> giữ lại vì đó là cách TẮT cửa cho một loại.
    ///
    /// Ràng buộc an toàn: nếu cùng một loại có Dedup &lt; 2×Skip thì cửa dedup sẽ không bao giờ "đóng" được
    /// đúng nhịp (bản tin chỉ tới sau mỗi Skip giây) ⇒ một sự việc bị tách thành nhiều sự cố. Việc NÂNG giá
    /// trị không làm ở đây (hàm thuần, không log); tầng nạp cấu hình chịu trách nhiệm cảnh báo.
    /// </summary>

    private static ServiceConfig? ApplyEventWindows(ServiceConfig current, string json, bool skip)
    {
        JsonElement root;
        try
        {
            root = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (root.ValueKind != JsonValueKind.Object) return null;

        var byType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("byType", out var byTypeEl) && byTypeEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in byTypeEl.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(prop.Name)) continue;
                if (prop.Value.ValueKind != JsonValueKind.Number || !prop.Value.TryGetInt32(out var sec)) continue;
                if (sec < 0) continue;
                byType[prop.Name.Trim()] = sec;
            }
        }

        int? defaultSeconds = null;
        if (root.TryGetProperty("default", out var defEl)
            && defEl.ValueKind == JsonValueKind.Number
            && defEl.TryGetInt32(out var d)
            && d >= 0)
        {
            defaultSeconds = d;
        }

        // Không có gì dùng được ⇒ giữ cấu hình đang chạy.
        var clearMap = root.TryGetProperty("byType", out var suppliedMap) && suppliedMap.ValueKind == JsonValueKind.Object && !suppliedMap.EnumerateObject().Any();
        if (byType.Count == 0 && defaultSeconds is null && !clearMap) return null;

        var next = current.Clone();
        if (skip)
        {
            if (byType.Count > 0 || clearMap) next.Events.SkipWindowSeconds = byType;
            if (defaultSeconds is not null) next.Events.SkipWindowSecondsDefault = defaultSeconds.Value;
        }
        else
        {
            if (byType.Count > 0 || clearMap) next.Events.DedupWindowSeconds = byType;
            if (defaultSeconds is not null) next.Events.DedupWindowSecondsDefault = defaultSeconds.Value;
        }

        return next;
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Danh sách loại sự kiện được nhờ TMS lập lịch tự động. Mảng rỗng HỢP LỆ (không loại nào được
    /// nhờ). Không phải mảng ⇒ <c>null</c> (giữ cấu hình đang chạy).
    /// Created date: 14/10/2026
    /// </summary>
    /// <summary>Payload <see cref="VehicleFilterTripGap"/>: số giây nguyên 60..86400; ngoài biên ⇒ <c>null</c>.</summary>
    private static ServiceConfig? ApplyVehicleFilterTripGap(ServiceConfig current, string json)
    {
        int seconds;
        try
        {
            seconds = JsonSerializer.Deserialize<int>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (seconds is < 60 or > 86_400) return null;

        var next = current.Clone();
        next.VehicleFilter.TripGapSeconds = seconds;
        return next;
    }

    private static ServiceConfig? ApplyAutomationEventTypes(ServiceConfig current, string json)
    {
        List<string>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (parsed is null) return null;

        var next = current.Clone();
        next.TmsApi.AutomationEventTypes = parsed
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return next;
    }

    /// <summary>
    /// Danh sách loại có so biển: <c>["Normal","Speeding"]</c>. MẢNG RỖNG LÀ HỢP LỆ (nghĩa là không loại nào
    /// so biển) — khác với các tham số khác, ở đây "rỗng" là một lựa chọn vận hành thật, không phải lỗi.
    /// Không phải mảng ⇒ <c>null</c> (giữ cấu hình đang chạy).
    /// </summary>
    private static ServiceConfig? ApplyEventCheckPlateTypes(ServiceConfig current, string json)
    {
        List<string>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (parsed is null) return null;

        var valid = parsed
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var next = current.Clone();
        next.Events.CheckPlateTypes = valid;
        return next;
    }

    /// <summary>
    /// Nhóm hướng đếm xe. Bỏ nhóm có key rỗng hoặc không có equipmentId nào; không còn nhóm nào hợp lệ ⇒
    /// <c>null</c> (giữ nhóm đang có). Trùng lặp EquipmentId trong một nhóm được loại.
    /// </summary>
    private static ServiceConfig? ApplyCountDirectionGroups(ServiceConfig current, string json)
    {
        List<CountDirectionGroup>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<List<CountDirectionGroup>>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (parsed is null) return null;

        var valid = new List<CountDirectionGroup>();
        foreach (var g in parsed)
        {
            if (g is null || string.IsNullOrWhiteSpace(g.Key)) continue;
            var eqIds = (g.EquipmentIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (eqIds.Count == 0) continue;
            valid.Add(new CountDirectionGroup
            {
                Key = g.Key.Trim(),
                Name = string.IsNullOrWhiteSpace(g.Name) ? null : g.Name.Trim(),
                EquipmentIds = eqIds,
            });
        }

        if (valid.Count == 0 && parsed.Count > 0) return null;

        var next = current.Clone();
        next.Publish.CountDirectionGroups = valid;
        return next;
    }

    /// <summary>
    /// Thư mục gốc lưu media. Chuỗi rỗng/khoảng trắng ⇒ <c>null</c> (giữ giá trị đang chạy): thư mục rỗng
    /// sẽ khiến ảnh rơi vào thư mục chứa exe và WebAPI không tìm thấy.
    /// </summary>
    private static ServiceConfig? ApplyMediaStorageDirectory(ServiceConfig current, string json)
    {
        var value = TryReadString(json)?.Trim();
        if (string.IsNullOrEmpty(value)) return null;

        var next = current.Clone();
        next.Media.StorageDirectory = value;
        return next;
    }

    /// <summary>
    /// Chu kỳ thống kê. Ngoài khoảng 1–60 phút ⇒ <c>null</c> (giữ giá trị đang chạy): chu kỳ 0 làm phép
    /// kiểm lệch chu kỳ chia cho 0 giá trị vô nghĩa, còn chu kỳ quá dài thì số liệu tổng hợp không còn dùng
    /// được cho điều hành.
    /// </summary>
    private static ServiceConfig? ApplyStatisticPeriod(ServiceConfig current, string json)
    {
        var raw = TryReadString(json);
        if (raw is null) return null;

        if (!int.TryParse(raw.Trim().Trim('"'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
            return null;

        if (minutes is < 1 or > 60) return null;

        var next = current.Clone();
        next.Statistic.ExpectedPeriodMinutes = minutes;
        return next;
    }

    /// <summary>
    /// Mapping loại sự kiện VDS → mã sự cố ITS. <c>{"Speeding":"VDS_SPEEDING",...}</c>.
    /// Bỏ cặp có key/value rỗng; không còn cặp nào ⇒ <c>null</c> (giữ map đang có = fallback appsettings).
    /// </summary>
    private static ServiceConfig? ApplyEventTypeCodes(ServiceConfig current, string json)
    {
        Dictionary<string, string>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (parsed is null) return null;

        var valid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (vdsType, itsCode) in parsed)
        {
            if (string.IsNullOrWhiteSpace(vdsType) || string.IsNullOrWhiteSpace(itsCode)) continue;
            valid[vdsType.Trim()] = itsCode.Trim();
        }

        if (valid.Count == 0 && parsed.Count > 0) return null;

        var next = current.Clone();
        next.Events.EventTypeCodes = valid;
        return next;
    }

    /// <summary>
    /// Mapping cỡ xe: <c>{"mode":"length","shortMax":6.0,"mediumMax":12.0,"unit":"meter"}</c>.
    /// Trường nào thiếu thì GIỮ giá trị đang có; ngưỡng vô lý (không dương, shortMax &gt;= mediumMax) hoặc
    /// đơn vị lạ ⇒ <c>null</c> (từ chối cả cụm, giữ cấu hình đang chạy).
    /// </summary>
    private static ServiceConfig? ApplyVehicleSizeMapping(ServiceConfig current, string json)
    {
        JsonElement root;
        try
        {
            root = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (root.ValueKind != JsonValueKind.Object) return null;

        var next = current.Clone();
        var size = next.VehicleSize;

        if (root.TryGetProperty("mode", out var modeEl) && modeEl.ValueKind == JsonValueKind.String)
        {
            var mode = modeEl.GetString()?.Trim();
            if (!string.IsNullOrEmpty(mode)) size.VehicleSizeMode = mode;
        }

        if (root.TryGetProperty("unit", out var unitEl) && unitEl.ValueKind == JsonValueKind.String)
        {
            var unit = unitEl.GetString()?.Trim();
            if (string.IsNullOrEmpty(unit) || !DimensionUnits.IsSupported(unit)) return null;
            size.VehicleDimensionUnit = unit;
        }

        if (TryGetDouble(root, "shortMax", out var shortMax)) size.VehicleLengthShortMax = shortMax;
        if (TryGetDouble(root, "mediumMax", out var mediumMax)) size.VehicleLengthMediumMax = mediumMax;

        // Sau khi áp: ngưỡng phải dương và short < medium, nếu không thì từ chối cả cụm.
        if (size.VehicleLengthShortMax <= 0 || size.VehicleLengthMediumMax <= 0
            || size.VehicleLengthShortMax >= size.VehicleLengthMediumMax)
            return null;

        return next;
    }

    /// <summary>Đọc một số double từ property JSON (chấp nhận số hoặc chuỗi số).</summary>
    private static bool TryGetDouble(JsonElement root, string name, out double value)
    {
        value = 0;
        if (!root.TryGetProperty(name, out var el)) return false;

        switch (el.ValueKind)
        {
            case JsonValueKind.Number when el.TryGetDouble(out var n):
                value = n;
                return true;
            case JsonValueKind.String when double.TryParse(
                el.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var s):
                value = s;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Ngưỡng tốc độ. Bỏ hướng nào có ngưỡng không hợp lệ thay vì bỏ cả bản tin — người vận hành khai sai một hướng thì các hướng còn lại vẫn phải có hiệu lực.
    /// Không hướng nào hợp lệ ⇒ <c>null</c> (giữ ngưỡng đang chạy, không xoá trắng).
    /// </summary>
    private static ServiceConfig? ApplySpeedRanges(ServiceConfig current, string json)
    {
        Dictionary<string, SpeedRange>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<Dictionary<string, SpeedRange>>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (parsed is null) return null;

        var valid = new Dictionary<string, SpeedRange>(StringComparer.OrdinalIgnoreCase);
        foreach (var (direction, range) in parsed)
        {
            if (string.IsNullOrWhiteSpace(direction)) continue;
            if (range is null || !range.IsValid) continue;
            valid[direction.Trim()] = range;
        }

        if (valid.Count == 0 && parsed.Count > 0) return null;

        var next = current.Clone();
        next.SpeedRangeByDirection = valid;
        return next;
    }

    /// <summary>
    /// Đổi kết nối QuestDB lúc chạy. Chuỗi rỗng là giá trị hợp lệ — đó là cách tắt QuestDB.
    /// Giá trị JSON có thể là chuỗi có nháy hoặc chuỗi trần.
    /// </summary>
    private static ServiceConfig? ApplyQuestConnectionId(ServiceConfig current, string json)
    {
        var value = TryReadString(json);
        if (value is null) return null;

        var next = current.Clone();
        next.QuestConnectionId = value.Trim();
        return next;
    }

    /// <summary>Đọc một chuỗi từ JSON, chấp nhận cả dạng có nháy và không nháy.</summary>
    private static string? TryReadString(string json)
    {
        if (json is null) return null;

        var raw = json.Trim();
        if (raw.Length == 0) return "";

        if (raw.StartsWith('"'))
        {
            try { return JsonSerializer.Deserialize<string>(raw); }
            catch (JsonException) { return null; }
        }

        return raw;
    }

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    internal static readonly JsonSerializerOptions FileWriteOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
