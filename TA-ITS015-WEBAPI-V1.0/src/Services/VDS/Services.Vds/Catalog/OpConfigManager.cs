using System.Globalization;
using System.Text;
using System.Text.Json;
using Services.Vds.Shared.Domain;
using Services.Vds.Shared.Messaging;
using Services.Vds.Shared.Persistence;
using Services.Vds.Shared.State;
using Services.Vds.Configuration;
using Services.Vds.Features.ZoneStatus;
using Modules.VDS.Core.Constants;
using Shared.QuestHa;

namespace Services.Vds.Catalog;

/// <summary>
/// Author: KienLT
/// Description: Nạp cấu hình VẬN HÀNH từ <c>VdsConfig</c> nhóm <c>vds_config</c> — hiện là ngưỡng tốc
/// độ theo hướng tuyến:
/// <code>
/// vds_route_{DirectionId}_min_speed  →  SpeedRange.Min
/// vds_route_{DirectionId}_max_speed  →  SpeedRange.Max
/// </code>
/// Người vận hành sửa trên web, service đọc lên — KHÔNG khai ngưỡng trong appsettings.
///
/// <para><b>Ba nguồn, thứ tự rõ ràng:</b>
/// <list type="number">
/// <item><b>DB</b>. Đọc được ⇒ ghi lại <c>{StateDirectory}/opconfig-speed.json</c> làm bản
/// last-known-good.</item>
/// <item><b>File cache</b> khi DB lỗi hoặc không có dòng nào — service vẫn chạy đúng ngưỡng của lần
/// trước, thay vì mất tính năng chỉ vì DB chớp một nhịp.</item>
/// <item><b>Không có cả hai</b> ⇒ để TRỐNG và ghi Error. Khi đó không đồng bộ ngưỡng lên AID;
/// rule Speeding API v1.8 vẫn nhận phân loại do AID gửi.</item>
/// </list></para>
///
/// Ngưỡng đọc được đi vào snapshot qua <c>VdsConfigManager.Apply</c> — cùng một cửa với tham số nhận qua
/// NATS. Nhờ vậy nó cũng được lưu vào <c>config-overrides.json</c> và chỉ có MỘT chỗ kiểm tính hợp lệ.
/// Created date: 24/08/2026
/// </summary>
public sealed class OpConfigManager : IDisposable
{
    /// <summary><c>VdsConfig.GroupCode</c> của nhóm cấu hình VDS.</summary>
    public const string GroupCode = "vds_config";

    /// <summary>
    /// <c>VdsConfig.GroupCode</c> riêng cho các GIÁ TRỊ MẶC ĐỊNH của bộ lọc sự kiện
    /// (<c>vds_skip_default</c>, <c>vds_dedup_default</c>).
    ///
    /// Tách nhóm để phân biệt rõ "tham số vận hành của hệ" (<c>vds_config</c>) với "giá trị rơi về khi một
    /// loại không khai riêng trong danh mục" — hai thứ này được sửa vì lý do khác nhau.
    /// </summary>
    public const string GroupCodeDefault = "vds_config_default";

    /// <summary>Tên file cache trong thư mục state.</summary>
    public const string CacheFileName = "opconfig-speed.json";

    /// <summary>Khoá <c>VdsConfig</c> chứa chu kỳ thống kê (phút) mà người vận hành đặt trên web.</summary>
    public const string KeyStatisticPeriodMinutes = "vds_statistic_period_minutes";

    /// <summary>
    /// Khoá <c>VdsConfig</c> chứa THƯ MỤC GỐC lưu ảnh/clip.
    ///
    /// Đây là hợp đồng hai chiều: worker ghi dưới thư mục này, WebAPI (<c>VdsImageService</c>) đọc lại từ
    /// cùng chỗ. Khai một nơi để hai bên không trôi lệch.
    /// </summary>
    public const string KeyMediaStorageDirectory = "vds_media_storage_directory";

    /// <summary>
    /// Tiền tố khoá <c>VdsConfig</c> chứa NHÓM HƯỚNG đếm xe: <c>vds_count_direction_equipment_{key}</c>,
    /// giá trị là danh sách <c>TmsEquipment.ID</c> điểm vào của hướng, phân tách dấu phẩy. Ví dụ
    /// <c>vds_count_direction_equipment_2 = id1,id2</c> ⇒ nhóm hướng "2" gồm 2 camera điểm vào.
    /// </summary>
    public const string KeyCountDirectionEquipmentPrefix = "vds_count_direction_equipment_";

    /// <summary><c>SysConfigType.Code</c> danh mục cửa SKIP (ghi thô): con = Code(loại) + Value(thời lượng).</summary>
    public const string ConfigTypeEventSkip = "vds_event_skip";

    /// <summary><c>SysConfigType.Code</c> danh mục cửa DEDUP (tạo sự cố): con = Code(loại) + Value(thời lượng).</summary>
    public const string ConfigTypeEventDedup = "vds_event_dedup";

    /// <summary>
    /// <c>SysConfigType.Code</c> danh mục loại CÓ SO BIỂN SỐ: con chỉ cần Code (loại), KHÔNG có thời lượng.
    /// </summary>
    public const string ConfigTypeEventCheckPlate = "vds_event_check_plate";

    /// <summary>Khoá thời lượng SKIP mặc định (có hậu tố) trong nhóm <c>vds_config_default</c>.</summary>
    public const string KeySkipDefault = "vds_skip_default";

    /// <summary>Khoá thời lượng DEDUP mặc định (có hậu tố) trong nhóm <c>vds_config_default</c>.</summary>
    public const string KeyDedupDefault = "vds_dedup_default";

    /// <summary>Endpoint đặt chu kỳ thống kê trên VDS AID.</summary>
    private const string StatsConfigPath = "/api/v1/vision-traffic/stats-config";

    /// <summary>Timeout cho lượt gọi đẩy chu kỳ — chỉ là một lệnh nhỏ lúc khởi động.</summary>
    private const int PushTimeoutSeconds = 10;

    // ── Tham số sinh trạng thái CHẬM của vùng (nhóm vds_config). Thiếu khoá ⇒ giá trị appsettings. ──

    /// <summary>Bật đánh giá chậm theo tốc độ: <c>1/0</c>, <c>true/false</c>, <c>on/off</c>.</summary>
    public const string KeyZoneSlowSpeedEnabled = VdsConfigConst.Code.ZoneSlowSpeedEnabled;

    /// <summary>Số xe tối thiểu trong cửa sổ để kết luận vùng chậm (số nguyên &gt; 0).</summary>
    public const string KeyZoneSlowMinVehicles = VdsConfigConst.Code.ZoneSlowMinVehicles;

    /// <summary>Tỉ lệ xe dưới <c>V_min</c> để kết luận chậm: <c>0.6</c>, <c>60</c> hoặc <c>60%</c>.</summary>
    public const string KeyZoneSlowShare = VdsConfigConst.Code.ZoneSlowShare;

    /// <summary>Tốc độ TB phải vượt <c>V_min</c> thêm bấy nhiêu km/h mới hết chậm (≥ 0).</summary>
    public const string KeyZoneSlowClearMarginKmh = VdsConfigConst.Code.ZoneSlowClearMarginKmh;

    /// <summary>Cửa sổ đánh giá (cũng là cửa sổ tốc độ TB của vùng), có hậu tố: <c>300s</c>, <c>5m</c>.</summary>
    public const string KeyZoneSlowWindow = VdsConfigConst.Code.ZoneSlowWindow;

    /// <summary>Chốt chậm theo sự kiện <c>Slow Moving</c> tự tắt sau bấy lâu, có hậu tố: <c>60s</c>, <c>1m</c>.</summary>
    public const string KeyZoneSlowEventTtl = VdsConfigConst.Code.ZoneSlowEventTtl;

    /// <summary>Chốt nghẽn theo sự kiện <c>Congestion</c> tự tắt sau bấy lâu, có hậu tố: <c>60s</c>, <c>1m</c>.</summary>
    public const string KeyZoneCongestionEventTtl = VdsConfigConst.Code.ZoneCongestionEventTtl;

    /// <summary>
    /// Hạn quá hạn dòng trạng thái vùng, nhóm <c>vds_config_default</c>, có hậu tố: <c>30s</c>. CÙNG khoá API TMS
    /// đọc để hạ dòng cũ về thông thoáng; worker đọc để ghi giữ nhịp kịp trước hạn đó.
    /// </summary>
    public const string KeyZoneStaleSeconds = VdsConfigConst.Code.ZoneStaleSeconds;

    /// <summary>Khoảng nghỉ gom chuyến lọc xe, nhóm <c>vds_config_default</c>, có hậu tố: <c>10m</c>, <c>1h</c>.</summary>
    public const string KeyVehicleFilterTripGap = VdsConfigConst.Code.VehicleFilterTripGap;

    private const string KeyPrefix = "vds_route_";
    private const string MinSuffix = "_min_speed";
    private const string MaxSuffix = "_max_speed";

    private readonly VdsCatalogRepository _catalog;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<OpConfigManager> _logger;
    private readonly string _cachePath;
    // KienLT - 31/10/2026: null khi HA tắt (WriteMode=Direct, ReadMode=Single) ⇒ bỏ qua mã vds_questha_*.
    private readonly QuestHaManager? _questHa;
    private bool _questHaApplied;
    // KienLT - 31/10/2026: tenant sở hữu tham số QuestDB HA (QuestHa:SettingsTenantId; trống ⇒ Service:TenantId). Khoá giữ "áp + lưu
    // override" cùng thứ tự với vé đọc: hai lượt nạp song song (chu kỳ + NATS) không ghi override cũ đè bản mới.
    private readonly string? _questHaSettingsTenant;
    private readonly object _questHaGate = new();

    public OpConfigManager(
        VdsCatalogRepository catalog, IVdsConfigManager config, ILogger<OpConfigManager> logger,
        QuestHaManager? questHa = null, string? questHaSettingsTenant = null)
    {
        _catalog = catalog;
        _config = config;
        _logger = logger;
        _questHa = questHa;
        _questHaSettingsTenant = questHaSettingsTenant;
        _cachePath = StatePath.Resolve(config.Current.StateDirectory, CacheFileName);
    }

    /// <summary>
    /// Nạp ngưỡng tốc độ và đẩy vào snapshot cấu hình.
    ///
    /// KHÔNG BAO GIỜ ném lỗi ra ngoài: thiếu cấu hình chỉ làm GIẢM tính năng (không báo quá tốc độ),
    /// không phải lý do chặn khởi động.
    /// </summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        // KienLT - 31/10/2026: vé đọc QuestDB HA lấy TRƯỚC khi truy vấn ⇒ kết quả của lượt nạp cũ về muộn không đè bộ mới hơn.
        var questHaTicket = _questHa?.BeginSettingsRead() ?? 0;
        var ranges = await FromDatabaseAsync(questHaTicket, ct);

        if (ranges is null)
        {
            // KienLT - 31/10/2026: DB lỗi lúc khởi động ⇒ áp gói QuestDB HA đã lưu trong config-overrides.json (nếu có).
            ApplyQuestHaFromOverride(questHaTicket);
            ranges = LoadCache();
            if (ranges is null) return; // Only a read failure may use last-known-good.
        }
        else SaveCache(ranges);

        Apply(ranges);
    }

    /// <summary>Đọc và parse ngưỡng từ DB. Lỗi ⇒ null (phân biệt với "đọc được nhưng rỗng").</summary>
    private async Task<Dictionary<string, SpeedRange>?> FromDatabaseAsync(long questHaTicket, CancellationToken ct)
    {
        try
        {
            var rows = await _catalog.QueryOpConfigAsync(GroupCode, ct);
            var byCode = rows.ToDictionary(
                r => r.Code ?? "", r => r.Value, StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation(
                "[OpConfig] Đọc {Count} dòng từ VdsConfig nhóm '{Group}'.", byCode.Count, GroupCode);

            // Cùng một lượt đọc phục vụ hai tham số khác nhau: ngưỡng tốc độ (giá trị trả về) và chu kỳ
            // thống kê (áp ngay tại đây). Tách thành hai lượt truy vấn chỉ để "hàm thuần hơn" là đổi hai lần
            // đi DB lấy một chút gọn gàng.
            ApplyStatisticPeriod(byCode);
            ApplyMediaStorageDirectory(byCode);
            ApplyCountDirectionGroups(byCode);
            ApplyZoneSpeedSlow(byCode, await ReadZoneDefaultsAsync(ct).ConfigureAwait(false));
            // KienLT - 31/10/2026: tham số vận hành QuestDB HA (vds_questha_*) cùng lượt đọc nhóm vds_config.
            await ApplyQuestHaAsync(byCode, questHaTicket, ct).ConfigureAwait(false);
            await ApplyEventFilterAsync(ct).ConfigureAwait(false);
            await ApplyAutomationEventsAsync(ct).ConfigureAwait(false);

            return Parse(byCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("OpConfigManager");
            _logger.LogWarning(ex, "[OpConfig] Lỗi đọc VdsConfig nhóm '{Group}'.", GroupCode);
            return null;
        }
    }

    /// <summary>Đẩy ngưỡng vào snapshot qua cùng một cửa với tham số nhận từ NATS.</summary>
    private void Apply(Dictionary<string, SpeedRange> ranges)
    {
        var json = JsonSerializer.Serialize(ranges);

        if (_config.Apply(ConfigParamApplier.SpeedRangeByDirection, json))
            _logger.LogInformation("[OpConfig] Ngưỡng tốc độ đang hiệu lực: {Detail}.", Describe(ranges));
        else
            _logger.LogError("[OpConfig] Không áp được ngưỡng tốc độ; giữ giá trị đang có.");
    }

    /// <summary>
    /// Chu kỳ thống kê người vận hành đặt trên web → snapshot, qua cùng một cửa <c>Apply</c> với ngưỡng tốc
    /// độ (bản cũ gán thẳng vào snapshot đang chạy).
    ///
    /// Không khai hoặc khai sai ⇒ giữ giá trị trong appsettings và cảnh báo. KHÔNG tự đoán: chu kỳ sai sẽ
    /// được đẩy XUỐNG THIẾT BỊ, kéo theo mọi bản tin thống kê sau đó lệch theo.
    /// </summary>
    private void ApplyStatisticPeriod(IReadOnlyDictionary<string, string?> byCode)
    {
        if (!byCode.TryGetValue(KeyStatisticPeriodMinutes, out var raw))
            raw = ((_config as VdsConfigManager)?.ReadBaseline() ?? new ServiceConfig()).Statistic.ExpectedPeriodMinutes.ToString(CultureInfo.InvariantCulture);

        var text = (raw ?? "").Trim();
        if (text.Length == 0) return;

        if (!_config.Apply(ConfigParamApplier.StatisticPeriodMinutes, text))
        {
            _logger.LogWarning(
                "[OpConfig] '{Key}' = '{Value}' không dùng được (phải là số nguyên 1–60); giữ chu kỳ thống kê " +
                "{Current} phút.",
                KeyStatisticPeriodMinutes, raw, _config.Current.Statistic.ExpectedPeriodMinutes);
            return;
        }

        _logger.LogInformation("[OpConfig] Chu kỳ thống kê đang hiệu lực: {Period} phút.",
            _config.Current.Statistic.ExpectedPeriodMinutes);
    }

    /// <summary>
    /// Tham số sinh trạng thái CHẬM của vùng từ <c>VdsConfig</c> (khoá <c>vds_zone_slow_*</c>) → snapshot,
    /// qua cùng cửa <c>Apply</c> với các tham số khác ⇒ đổi trên web là có hiệu lực ở lượt nạp sau, không
    /// cần build/restart, và sống qua restart nhờ <c>config-overrides.json</c>.
    ///
    /// <para>Luôn dựng ĐỦ bộ tám giá trị (thêm chốt nghẽn và hạn quá hạn vùng — khoá này nằm ở nhóm
    /// <c>vds_config_default</c>, truyền qua <paramref name="defaults"/>):
    /// <list type="bullet">
    /// <item>Khoá có trên DB và đọc được ⇒ dùng giá trị DB.</item>
    /// <item>Khoá KHÔNG có trên DB ⇒ giá trị appsettings (baseline), để xoá dòng trên web là quay về mặc
    /// định chứ không kẹt ở giá trị DB cũ đã lưu trong file override.</item>
    /// <item>Khoá có nhưng rỗng hoặc sai định dạng ⇒ GIỮ giá trị đang chạy và cảnh báo — gõ sai một dòng không được
    /// lặng lẽ đổi hành vi.</item>
    /// </list></para>
    /// </summary>
    /// <param name="defaults">Nhóm <c>vds_config_default</c>; null = đọc lỗi ⇒ giữ hạn quá hạn đang chạy.</param>
    private void ApplyZoneSpeedSlow(
        IReadOnlyDictionary<string, string?> byCode, IReadOnlyDictionary<string, string?>? defaults)
    {
        var baseline = ((_config as VdsConfigManager)?.ReadBaseline() ?? new ServiceConfig()).ZoneStatus;
        var current = _config.Current.ZoneStatus;
        var fromDb = new List<string>();

        T Pick<T>(string key, Func<string, T?> parse, T baselineValue, T currentValue,
            IReadOnlyDictionary<string, string?>? source = null) where T : struct
        {
            if (!(source ?? byCode).TryGetValue(key, out var raw)) return baselineValue;

            if (!string.IsNullOrWhiteSpace(raw) && parse(raw.Trim()) is T value)
            {
                fromDb.Add(key);
                return value;
            }

            Services.Vds.Catalog.CatalogRefreshAudit.Failed(key);
            _logger.LogWarning(
                "[OpConfig] '{Key}' = '{Value}' không đọc được ⇒ giữ giá trị đang chạy ({Current}).",
                key, raw, currentValue);
            return currentValue;
        }

        var payload = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["enabled"] = Pick(KeyZoneSlowSpeedEnabled, ParseBool,
                baseline.SpeedSlow.Enabled, current.SpeedSlow.Enabled),
            ["minVehicles"] = Pick(KeyZoneSlowMinVehicles, ParsePositiveInt,
                baseline.SpeedSlow.MinVehicles, current.SpeedSlow.MinVehicles),
            ["slowShare"] = Pick(KeyZoneSlowShare, ParseShare,
                baseline.SpeedSlow.SlowShare, current.SpeedSlow.SlowShare),
            ["clearMarginKmh"] = Pick(KeyZoneSlowClearMarginKmh, ParseNonNegative,
                baseline.SpeedSlow.ClearMarginKmh, current.SpeedSlow.ClearMarginKmh),
            ["windowSeconds"] = Pick(KeyZoneSlowWindow, ParsePositiveSeconds,
                baseline.AvgWindowSeconds, current.AvgWindowSeconds),
            ["slowEventTtlSeconds"] = Pick(KeyZoneSlowEventTtl, ParsePositiveSeconds,
                baseline.SlowEventTtlSeconds, current.SlowEventTtlSeconds),
            ["congestionEventTtlSeconds"] = Pick(KeyZoneCongestionEventTtl, ParsePositiveSeconds,
                baseline.CongestionEventTtlSeconds, current.CongestionEventTtlSeconds),
            // Đọc nhóm mặc định lỗi ⇒ GIỮ hạn đang chạy, không rơi về appsettings: DB chớp một nhịp không được
            // làm nhịp ghi giữ lệch khỏi hạn API TMS đang dùng.
            ["staleSeconds"] = defaults is null
                ? current.StaleSeconds
                : Pick(KeyZoneStaleSeconds, ParseStaleSeconds,
                    baseline.StaleSeconds, current.StaleSeconds, defaults),
        };

        var json = JsonSerializer.Serialize(payload, ConfigParamApplier.JsonOptions);
        if (!_config.Apply(ConfigParamApplier.ZoneSpeedSlow, json))
        {
            _logger.LogWarning(
                "[OpConfig] Bộ tham số vùng chậm không hợp lệ ({Json}); giữ bộ đang chạy.", json);
            return;
        }

        var zone = _config.Current.ZoneStatus;
        _logger.LogInformation(
            "[OpConfig] Vùng chậm: theo tốc độ={Enabled}, ≥{MinVehicles} xe, ≥{Share:P0} xe dưới V_min, " +
            "hết chậm khi TB ≥ V_min+{Margin}km/h, cửa sổ {Window}s, chốt Slow Moving {Ttl}s, chốt Congestion " +
            "{CongestionTtl}s, quá hạn {Stale}s (ghi giữ nhịp sau {KeepAlive}s, quét {Sweep}s). Từ DB: {FromDb}.",
            zone.SpeedSlow.Enabled, zone.SpeedSlow.MinVehicles, zone.SpeedSlow.SlowShare,
            zone.SpeedSlow.ClearMarginKmh, zone.AvgWindowSeconds, zone.SlowEventTtlSeconds,
            zone.CongestionEventTtlSeconds, zone.StaleSeconds, zone.KeepAliveSeconds,
            zone.EffectiveSweepIntervalSeconds,
            fromDb.Count == 0 ? "(không có giá trị DB hợp lệ, dùng appsettings/giá trị đang chạy)" : string.Join(", ", fromDb));

        // Nhịp quét không xuống dưới 5s, cộng nhịp đọc DB 2s của SSE TMS ⇒ hạn ngắn hơn mức này thì ghi giữ nhịp
        // không kịp: vùng chậm/nghẽn vẫn chớp về thông thoáng trên bigscreen.
        if (zone.StaleSeconds < ZoneStatusOptions.MinKeepAliveStaleSeconds)
            _logger.LogWarning(
                "[OpConfig] '{Key}' = {Stale}s quá ngắn: ghi giữ nhịp cần tối thiểu {Min}s, vùng chậm/nghẽn sẽ bị coi là " +
                "quá hạn giữa hai lượt ghi.", KeyZoneStaleSeconds, zone.StaleSeconds, ZoneStatusOptions.MinKeepAliveStaleSeconds);
    }

    /// <summary>
    /// Hạn quá hạn vùng: cùng biên API TMS chấp nhận (1s..1 ngày). Ngoài biên thì TMS dùng mặc định, nên ở đây
    /// coi là "không đọc được" (giữ giá trị đang chạy, cảnh báo) thay vì để cả cụm tham số vùng bị từ chối.
    /// </summary>
    private static int? ParseStaleSeconds(string text)
        => DurationSpec.TryParseSeconds(text) is int s && s is >= 1 and <= 86400 ? s : null;

    // KienLT - 31/10/2026: tham số vận hành QuestDB HA đổi lúc chạy (CODING_RULES §11).
    /// <summary>
    /// Mã <c>vds_questha_*</c> → <see cref="QuestHaManager.ApplySettings"/>. Mã thiếu ⇒ giá trị appsettings (xoá dòng trên web là
    /// về mặc định). Một mã lạ/sai định dạng/ngoài biên, hoặc Manager từ chối gói ⇒ GIỮ bộ đang chạy và cảnh báo — không áp nửa gói.
    /// Gói được nhận mới lưu override (<c>config-overrides.json</c>) để dùng khi DB lỗi lúc khởi động lại.
    /// </summary>
    private async Task ApplyQuestHaAsync(IReadOnlyDictionary<string, string?> byCode, long ticket, CancellationToken ct)
    {
        if (_questHa is null) return;

        // Router/snapshot là singleton của host ⇒ chỉ dòng của tenant sở hữu được áp. Khác tenant của worker thì đọc riêng; lỗi đọc
        // chỉ giữ bộ đang chạy, không làm hỏng các tham số khác của cùng lượt nạp.
        var owner = Modules.VDS.Core.Helper.VdsBaseHelper.QuestHaConfigHp.SettingsTenant(_questHaSettingsTenant, _config.Current.TenantId);
        if (!string.Equals(owner, _config.Current.TenantId?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var rows = await _catalog.QueryOpConfigAsync(GroupCode, owner, ct).ConfigureAwait(false);
                byCode = rows.ToDictionary(r => r.Code ?? "", r => r.Value, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Services.Vds.Catalog.CatalogRefreshAudit.Failed(VdsConfigConst.QuestHa.Prefix);
                _logger.LogWarning("[OpConfig] Không đọc được tham số QuestDB HA của tenant sở hữu ({ErrorType}) ⇒ giữ bộ đang chạy.", ex.GetType().Name);
                return;
            }
        }

        var errors = new List<string>();
        var runtime = Modules.VDS.Core.Helper.VdsBaseHelper.QuestHaConfigHp.Build(byCode, errors);
        if (errors.Count > 0)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed(VdsConfigConst.QuestHa.Prefix);
            _logger.LogWarning("[OpConfig] Gói tham số QuestDB HA không hợp lệ ⇒ giữ bộ đang chạy: {Errors}.", string.Join("; ", errors));
            return;
        }

        lock (_questHaGate)
        {
            // Manager tự log Information các khoá đổi hoặc Warning lý do từ chối.
            if (!_questHa.ApplySettings(runtime, ticket, out var superseded))
            {
                // Vé cũ: một lượt nạp bắt đầu sau đã áp bộ mới hơn — không phải lỗi cấu hình.
                if (superseded) _logger.LogDebug("[OpConfig] Bỏ gói QuestDB HA của lượt nạp cũ; bộ mới hơn đã được áp.");
                else Services.Vds.Catalog.CatalogRefreshAudit.Failed(VdsConfigConst.QuestHa.Prefix);
                return;
            }
            _questHaApplied = true;

            var json = JsonSerializer.Serialize(runtime, QuestHaJsonOptions);
            var saved = _config.Current.QuestHaRuntime is { } current ? JsonSerializer.Serialize(current, QuestHaJsonOptions) : null;
            if (json != saved && !_config.Apply(ConfigParamApplier.QuestHaRuntime, json))
                _logger.LogWarning("[OpConfig] Đã áp tham số QuestDB HA nhưng không lưu được override; restart lúc DB lỗi sẽ dùng appsettings.");
        }
    }

    /// <summary>DB lỗi và chưa áp gói nào trong phiên này ⇒ áp gói đã lưu ở <c>config-overrides.json</c> (nếu có).</summary>
    private void ApplyQuestHaFromOverride(long ticket)
    {
        if (_questHa is null) return;
        lock (_questHaGate)
        {
            if (_questHaApplied || _config.Current.QuestHaRuntime is not { } saved) return;
            if (!_questHa.ApplySettings(saved, ticket, out _)) return;
            _questHaApplied = true;
        }
        _logger.LogInformation("[OpConfig] Không đọc được VdsConfig ⇒ áp tham số QuestDB HA đã lưu ở config-overrides.json.");
    }

    private static readonly JsonSerializerOptions QuestHaJsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Đọc nhóm <c>vds_config_default</c> cho tham số vùng. Lỗi ⇒ null (bên gọi giữ giá trị đang chạy) thay vì
    /// ném: lỗi ở đây không được chặn các tham số khác của cùng lượt nạp.
    /// </summary>
    private async Task<Dictionary<string, string?>?> ReadZoneDefaultsAsync(CancellationToken ct)
    {
        try
        {
            return await ReadDefaultsAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed(KeyZoneStaleSeconds);
            _logger.LogWarning(ex,
                "[OpConfig] Lỗi đọc VdsConfig nhóm '{Group}' cho trạng thái vùng; giữ hạn quá hạn đang chạy.",
                GroupCodeDefault);
            return null;
        }
    }

    private static bool? ParseBool(string text) => text.ToLowerInvariant() switch
    {
        "1" or "true" or "on" or "yes" => true,
        "0" or "false" or "off" or "no" => false,
        _ => null,
    };

    private static int? ParsePositiveInt(string text)
        => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v > 0 ? v : null;

    private static double? ParseNonNegative(string text)
        => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
           && double.IsFinite(v) && v >= 0 ? v : null;

    /// <summary>
    /// Tỉ lệ: <c>0.6</c> (0..1), hoặc phần trăm <c>60</c> / <c>60%</c>. Số trong (1, 100] coi là phần trăm vì
    /// tỉ lệ thật không thể lớn hơn 1.
    /// </summary>
    private static double? ParseShare(string text)
    {
        var percent = text.EndsWith('%');
        if (!double.TryParse(text.TrimEnd('%').Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            || !double.IsFinite(v))
            return null;
        if (percent || v > 1) v /= 100;
        return v is > 0 and <= 1 ? v : null;
    }

    /// <summary>Thời lượng có hậu tố (<see cref="DurationSpec"/>: không hậu tố = PHÚT); phải &gt; 0.</summary>
    private static int? ParsePositiveSeconds(string text)
        => DurationSpec.TryParseSeconds(text) is int s && s > 0 ? s : null;

    /// <summary>
    /// Thư mục gốc lưu ảnh/clip từ <c>VdsConfig</c> → snapshot, qua cùng cửa <c>Apply</c>.
    ///
    /// Không khai hoặc rỗng ⇒ GIỮ giá trị từ appsettings (fallback). Đây là hợp đồng hai chiều với
    /// <c>VdsImageService</c> phía WebAPI: hai bên đọc CÙNG khoá nên không thể trôi lệch thư mục.
    /// </summary>
    private void ApplyMediaStorageDirectory(IReadOnlyDictionary<string, string?> byCode)
    {
        if (!byCode.TryGetValue(KeyMediaStorageDirectory, out var raw))
            raw = ((_config as VdsConfigManager)?.ReadBaseline() ?? new ServiceConfig()).Media.StorageDirectory;

        var value = (raw ?? "").Trim();
        if (value.Length == 0) return;

        if (_config.Apply(ConfigParamApplier.MediaStorageDirectory, value))
            _logger.LogInformation("[OpConfig] Thư mục gốc lưu media: {Path}.", value);
        else
            _logger.LogWarning(
                "[OpConfig] '{Key}' = '{Value}' không dùng được; giữ thư mục đang có (fallback appsettings).",
                KeyMediaStorageDirectory, raw);
    }

    /// <summary>
    /// Nạp ba danh mục lọc sự kiện từ <c>SysConfig</c> và hai giá trị mặc định từ <c>VdsConfig</c>, rồi áp
    /// vào snapshot qua cùng cửa <c>Apply</c>:
    /// <list type="bullet">
    /// <item><c>vds_event_skip</c> → cửa ghi thô theo loại (Code=loại, Value=thời lượng có hậu tố)</item>
    /// <item><c>vds_event_dedup</c> → cửa tạo sự cố theo loại</item>
    /// <item><c>vds_event_check_plate</c> → danh sách loại có so biển (chỉ cần Code)</item>
    /// </list>
    ///
    /// <b>Data-driven hoàn toàn</b>: duyệt HẾT dòng con, không hard-code danh sách loại. Thêm loại mới chỉ
    /// cần thêm một dòng con trên DB, không sửa/không build lại code.
    ///
    /// TỰ BẮT MỌI LỖI: cấu hình lọc thiếu chỉ làm giảm độ mịn của bộ lọc, không được phép làm hỏng lượt nạp
    /// ngưỡng tốc độ hay chặn khởi động.
    /// </summary>
    private async Task ApplyEventFilterAsync(CancellationToken ct)
    {
        try
        {
            var skip = await ReadWindowsAsync(ConfigTypeEventSkip, ct).ConfigureAwait(false);
            var dedup = await ReadWindowsAsync(ConfigTypeEventDedup, ct).ConfigureAwait(false);

            // Giá trị mặc định nằm ở NHÓM RIÊNG vds_config_default, không lẫn với tham số vận hành.
            var defaults = await ReadDefaultsAsync(ct).ConfigureAwait(false);
            var baseline = (_config as VdsConfigManager)?.ReadBaseline() ?? new ServiceConfig();
            var skipDefault = ReadDefaultSeconds(defaults, KeySkipDefault) ?? baseline.Events.SkipWindowSecondsDefault;
            var dedupDefault = ReadDefaultSeconds(defaults, KeyDedupDefault) ?? baseline.Events.DedupWindowSecondsDefault;

            // DB values are authoritative; do not silently raise the dedup window.

            ApplyWindows(ConfigParamApplier.EventSkipWindows, "SKIP (ghi thô)", skip, skipDefault);
            ApplyWindows(ConfigParamApplier.EventDedupWindows, "DEDUP (tạo sự cố)", dedup, dedupDefault);
            ApplyVehicleFilterTripGap(defaults, baseline);


            await ApplyCheckPlateAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("OpConfigManager");
            _logger.LogWarning(ex,
                "[OpConfig] Lỗi nạp cấu hình lọc sự kiện (skip/dedup/checkPlate); giữ cấu hình đang chạy.");
        }
    }

    /// <summary>
    /// Khoảng nghỉ gom chuyến lọc xe từ <c>vds_config_default</c> → snapshot. Nguồn DUY NHẤT là DB (không còn khai
    /// trong appsettings): thiếu dòng hoặc sai định dạng ⇒ mặc định trong code (<see cref="Services.Vds.Features.VehicleFilter.VehicleFilterOptions.TripGapSeconds"/>).
    /// </summary>
    private void ApplyVehicleFilterTripGap(IReadOnlyDictionary<string, string?> defaults, ServiceConfig baseline)
    {
        var seconds = ReadDefaultSeconds(defaults, KeyVehicleFilterTripGap) ?? baseline.VehicleFilter.TripGapSeconds;
        if (seconds is < 60 or > 86_400)
        {
            _logger.LogWarning("[OpConfig] '{Key}' = {Seconds}s ngoài biên 1 phút..24 giờ ⇒ giữ {Current}s.",
                KeyVehicleFilterTripGap, seconds, _config.Current.VehicleFilter.TripGapSeconds);
            return;
        }

        if (_config.Current.VehicleFilter.TripGapSeconds == seconds) return;
        if (_config.Apply(ConfigParamApplier.VehicleFilterTripGap, seconds.ToString(CultureInfo.InvariantCulture)))
            _logger.LogInformation("[OpConfig] Khoảng nghỉ gom chuyến lọc xe: {Seconds}s ({Source}).", seconds,
                defaults.ContainsKey(KeyVehicleFilterTripGap) ? "VdsConfig" : "mặc định");
        else
            _logger.LogWarning("[OpConfig] Không áp được khoảng nghỉ gom chuyến lọc xe {Seconds}s; giữ giá trị đang chạy.", seconds);
    }

    /// <summary>Đọc nhóm <c>vds_config_default</c> thành dict; nhóm chưa tạo ⇒ dict rỗng.</summary>
    private async Task<Dictionary<string, string?>> ReadDefaultsAsync(CancellationToken ct)
    {
        var rows = await _catalog.QueryOpConfigAsync(GroupCodeDefault, ct).ConfigureAwait(false);
        return rows.ToDictionary(r => r.Code ?? "", r => r.Value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Đọc một danh mục thời lượng thành <c>{loại → giây}</c>. Dòng nào có thời lượng không đọc được thì BỎ
    /// dòng đó kèm cảnh báo (không bỏ cả danh mục) — khai sai một loại không được làm mất các loại còn lại.
    /// </summary>
    private async Task<Dictionary<string, int>> ReadWindowsAsync(string configTypeCode, CancellationToken ct)
    {
        var rows = await _catalog.QueryConfigDataAsync(configTypeCode, ct).ConfigureAwait(false);
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var type = (row.Code ?? "").Trim();
            if (type.Length == 0) continue;

            var seconds = DurationSpec.TryParseSeconds(row.Value);
            if (seconds is null)
            {
                CatalogRefreshAudit.Failed(configTypeCode);
                _logger.LogWarning(
                    "[OpConfig] '{Type}' trong danh mục '{Catalog}' có thời lượng '{Value}' không đọc được " +
                    "(dùng dạng 10s/5m/2h/1d; không hậu tố = PHÚT) ⇒ bỏ dòng này, loại sẽ dùng giá trị mặc định.",
                    type, configTypeCode, row.Value);
                continue;
            }

            result[type] = seconds.Value;
        }

        return result;
    }

    /// <summary>Đọc một khoá thời lượng mặc định từ nhóm <c>vds_config_default</c>; không khai/sai ⇒ null.</summary>
    private int? ReadDefaultSeconds(IReadOnlyDictionary<string, string?> defaults, string key)
    {
        if (!defaults.TryGetValue(key, out var raw)) return null;

        var seconds = DurationSpec.TryParseSeconds(raw);
        if (seconds is null && !string.IsNullOrWhiteSpace(raw))
            _logger.LogWarning(
                "[OpConfig] '{Key}' (nhóm {Group}) = '{Value}' không đọc được (dùng dạng 10s/5m/2h/1d) ⇒ giữ " +
                "mặc định đang có.",
                key, GroupCodeDefault, raw);

        return seconds;
    }

    /// <summary>Dựng payload <c>{"default":n,"byType":{...}}</c> và áp qua cửa <c>Apply</c>.</summary>
    private void ApplyWindows(string parameterName, string label, Dictionary<string, int> byType, int? defaultSeconds)
    {


        var payload = new Dictionary<string, object>(StringComparer.Ordinal);
        if (defaultSeconds is not null) payload["default"] = defaultSeconds.Value;
        payload["byType"] = byType;

        var json = JsonSerializer.Serialize(payload, ConfigParamApplier.JsonOptions);

        if (_config.Apply(parameterName, json))
            _logger.LogInformation(
                "[OpConfig] Cửa {Label} đang hiệu lực: mặc định {Default}, {Count} loại khai riêng ({Detail}).",
                label,
                defaultSeconds is null ? "(giữ nguyên)" : defaultSeconds.Value + "s",
                byType.Count,
                byType.Count == 0
                    ? "không có"
                    : string.Join(", ", byType
                        .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(kv => $"{kv.Key}={kv.Value}s")));
        else
            _logger.LogWarning("[OpConfig] Không áp được cửa {Label}; giữ cấu hình đang chạy.", label);
    }

    /// <summary>
    /// Áp danh sách loại có so biển. Danh mục RỖNG là hợp lệ (không loại nào so biển) nhưng chỉ áp khi danh
    /// mục THỰC SỰ tồn tại — chưa tạo danh mục thì giữ cấu hình đang chạy thay vì xoá trắng.
    /// </summary>
    private async Task ApplyCheckPlateAsync(CancellationToken ct)
    {
        var rows = await _catalog.QueryConfigDataAsync(ConfigTypeEventCheckPlate, ct).ConfigureAwait(false);


        var types = rows
            .Select(r => (r.Code ?? "").Trim())
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var json = JsonSerializer.Serialize(types, ConfigParamApplier.JsonOptions);

        if (_config.Apply(ConfigParamApplier.EventCheckPlateTypes, json))
            _logger.LogInformation(
                "[OpConfig] Loại có so biển số: {Detail}.",
                types.Count == 0 ? "(không loại nào)" : string.Join(", ", types));
        else
            _logger.LogWarning("[OpConfig] Không áp được danh sách loại so biển; giữ cấu hình đang chạy.");
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Áp danh sách loại sự kiện được gửi sang TMS (lập lịch tự động / tự kết thúc khi không ai nhận).
    ///              Từ 17/10/2026 danh sách SUY TỪ cấu hình tự động của TMS (<c>TmsIncidentAutomationConfig</c>), không
    ///              còn khai tay ở nhóm <c>vds_automation_event</c>. Danh sách rỗng cũng áp — nghĩa là không loại nào.
    ///              Lỗi đọc DB thì giữ cấu hình đang chạy, không chặn lượt nạp ngưỡng tốc độ.
    /// Created date: 14/10/2026
    /// </summary>
    private async Task ApplyAutomationEventsAsync(CancellationToken ct)
    {
        try
        {
            var types = await _catalog.QueryAutomationEventTypesAsync(ct).ConfigureAwait(false);

            if (_config.Apply(ConfigParamApplier.AutomationEventTypes, JsonSerializer.Serialize(types, ConfigParamApplier.JsonOptions)))
                _logger.LogInformation(
                    "[OpConfig] Loại sự kiện được nhờ TMS lập lịch tự động: {Detail}.",
                    types.Count == 0 ? "(không loại nào)" : string.Join(", ", types));
            else
                _logger.LogWarning("[OpConfig] Không áp được danh sách loại sự kiện tự động; giữ cấu hình đang chạy.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("OpConfigManager");
            _logger.LogWarning(ex, "[OpConfig] Lỗi đọc cấu hình tự động TMS; giữ danh sách loại sự kiện đang chạy.");
        }
    }

    /// <summary>
    /// Gom các khoá <c>vds_count_direction_equipment_{key}</c> (giá trị = danh sách EquipmentId phân tách
    /// phẩy) thành nhóm hướng đếm rồi áp vào snapshot qua cùng cửa <c>Apply</c>.
    ///
    /// Không có khoá nào ⇒ KHÔNG áp (giữ nhóm đang có = fallback appsettings). Khoá có nhưng rỗng/không
    /// hợp lệ thì <c>ConfigParamApplier</c> tự từ chối và giữ giá trị cũ.
    /// </summary>
    private void ApplyCountDirectionGroups(IReadOnlyDictionary<string, string?> byCode)
    {
        var groups = new List<CountDirectionGroup>();

        foreach (var (rawCode, rawValue) in byCode)
        {
            var code = (rawCode ?? "").Trim();
            if (!code.StartsWith(KeyCountDirectionEquipmentPrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var key = code[KeyCountDirectionEquipmentPrefix.Length..].Trim();
            if (key.Length == 0) continue;

            var ids = (rawValue ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ids.Count == 0) continue;

            groups.Add(new CountDirectionGroup { Key = key, EquipmentIds = ids });
        }



        // Ổn định thứ tự theo key để payload không đổi thứ tự nhóm giữa các lần nạp.
        groups = groups.OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase).ToList();

        var json = JsonSerializer.Serialize(groups, ConfigParamApplier.JsonOptions);
        if (_config.Apply(ConfigParamApplier.CountDirectionGroups, json))
            _logger.LogInformation(
                "[OpConfig] Nhóm hướng đếm đang hiệu lực: {Detail}.",
                string.Join("; ", groups.Select(g => $"{g.Key}={g.EquipmentIds.Count} cam")));
        else
            _logger.LogWarning("[OpConfig] Không áp được nhóm hướng đếm; giữ nhóm đang có (fallback appsettings).");
    }

    private readonly SemaphoreSlim _laneSyncLock = new(1, 1);

    /// <summary>
    /// Đồng bộ ngưỡng min/max xuống làn AID của từng camera; lỗi thì thử lại ở lượt refresh danh mục sau.
    /// Ngưỡng lấy qua <see cref="SpeedRangeResolver"/>: vùng chính của camera, giá trị vùng không khai thì
    /// rơi về mặc định theo hướng.
    /// </summary>
    public async Task PushLaneSpeedsAsync(
        IReadOnlyCollection<CameraInfo> cameras, SpeedRangeResolver speeds, CancellationToken ct = default)
    {
        await _laneSyncLock.WaitAsync(ct);
        try
        {
            var config = _config.Current;
            if (!Uri.TryCreate(config.Ingestion.ApiBaseUrl?.TrimEnd('/') + "/", UriKind.Absolute, out var root)
                || (root.Scheme != Uri.UriSchemeHttps && !(root.Scheme == Uri.UriSchemeHttp && config.Ingestion.AllowInsecureHttp)))
            {
                _logger.LogWarning("[AidSpeedSync] Invalid or disallowed AID base URL; skip sync.");
                return;
            }
            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            if (config.Ingestion.AllowSelfSignedCert)
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
            // Song song có giới hạn: máy chủ AID chậm/không trả lời thì mỗi camera chờ đủ timeout, gọi lần lượt
            // thì N camera mất N × 10s. Các camera độc lập nhau nên gửi đồng thời.
            var parallel = new ParallelOptions { MaxDegreeOfParallelism = LaneSyncParallelism, CancellationToken = ct };
            await Parallel.ForEachAsync(cameras, parallel, async (camera, token) =>
            {
                if (speeds.ForCamera(camera) is not ResolvedSpeedRange resolved)
                {
                    _logger.LogWarning(
                        "[AidSpeedSync] {Camera}: không có ngưỡng hợp lệ (vùng {Zone} + mặc định hướng {Direction}); bỏ qua.",
                        camera.CameraIp, speeds.ZoneOf(camera) ?? "-", camera.Direction ?? "-");
                    return;
                }
                var range = resolved.Range;
                try
                {
                    var uri = new Uri(root, "api/v1/cameras/by-ip/" + Uri.EscapeDataString(camera.CameraIp) + "/lanes");
                    var result = await AidLaneSpeedSync.SyncAsync(http, uri, range, request => config.Ingestion.ApplyAuth(request), token);
                    _logger.LogInformation("[AidSpeedSync] {Camera} direction={Direction} zone={Zone} min={Min} max={Max} source={Source}: {Outcome} — {Detail}; lanes=[{Lanes}].",
                        camera.CameraIp, camera.Direction, resolved.ZoneId ?? "-", range.Min, range.Max, resolved.Source,
                        result.Outcome, result.Detail, string.Join(",", result.Lanes));
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                catch (Exception ex) { _logger.LogWarning(ex, "[AidSpeedSync] {Camera}: sync not confirmed; retry next refresh.", camera.CameraIp); }
            }).ConfigureAwait(false);
        }
        finally { _laneSyncLock.Release(); }
    }

    /// <summary>Số camera gửi đồng thời khi đồng bộ ngưỡng xuống AID.</summary>
    private const int LaneSyncParallelism = 8;

    // 0 = rảnh, 1 = đang chạy, 2 = đang chạy và có yêu cầu mới chờ chạy thêm một lượt.
    private int _laneSyncState;
    private readonly CancellationTokenSource _laneSyncCts = new();
    private volatile bool _laneSyncDisposed;

    /// <summary>
    /// Đồng bộ ngưỡng xuống AID ở NỀN, không bắt bên gọi chờ HTTP. Nạp danh mục (khởi động, chu kỳ, lệnh refresh)
    /// gọi hàm này thay vì <see cref="PushLaneSpeedsAsync"/>: máy chủ AID chậm hoặc không tới được không được
    /// làm chậm việc mở ingestion hay hiện danh mục lên WPF.
    ///
    /// <para>Gộp yêu cầu: đang có lượt chạy thì chỉ đánh dấu và chạy thêm ĐÚNG một lượt khi lượt đó xong, với
    /// danh sách camera mới nhất (<paramref name="cameras"/> được gọi lại mỗi lượt) — nhiều lệnh refresh dồn dập
    /// không tạo hàng đợi dài.</para>
    /// </summary>
    public void RequestLaneSpeedPush(Func<IReadOnlyCollection<CameraInfo>> cameras, SpeedRangeResolver speeds)
    {
        if (_laneSyncDisposed) return;
        if (Interlocked.CompareExchange(ref _laneSyncState, 1, 0) != 0)
        {
            Interlocked.Exchange(ref _laneSyncState, 2);
            return;
        }

        _ = Task.Run(async () =>
        {
            var token = _laneSyncCts.Token;
            do
            {
                Interlocked.Exchange(ref _laneSyncState, 1);
                try { await PushLaneSpeedsAsync(cameras(), speeds, token).ConfigureAwait(false); }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
                catch (Exception ex) { _logger.LogWarning(ex, "[AidSpeedSync] Lượt đồng bộ nền lỗi; thử lại ở lượt refresh sau."); }
            }
            // Còn yêu cầu chờ (state = 2) thì CAS 1→0 thất bại ⇒ chạy thêm một lượt.
            while (!token.IsCancellationRequested && Interlocked.CompareExchange(ref _laneSyncState, 0, 1) != 1);
        });
    }

    /// <summary>Huỷ lượt đồng bộ nền đang chạy khi service dừng.</summary>
    public void Dispose()
    {
        if (_laneSyncDisposed) return;
        _laneSyncDisposed = true;
        _laneSyncCts.Cancel();
        _laneSyncCts.Dispose();
    }

    /// <summary>
    /// Đặt chu kỳ thống kê trên VDS AID: <c>PUT {ApiBaseUrl}/api/v1/vision-traffic/stats-config</c> với body
    /// <c>{ "period_minutes": n }</c>. Gọi SAU <see cref="LoadAsync"/> để đẩy đúng giá trị vừa nạp từ DB.
    ///
    /// <para>
    /// <b>Mặc định KHÔNG gọi</b> (<c>Service:Statistic:PushPeriodToDevice</c> = false). Đây là lệnh ghi vào
    /// thiết bị dùng chung: thiết bị chỉ có MỘT chu kỳ cho mọi bên nhận, nên khi còn chạy song song với
    /// worker cũ mà hai bên khai lệch nhau thì bên khởi động sau sẽ lặng lẽ đổi chu kỳ của bên kia.
    /// </para>
    ///
    /// Không chặn khởi động: mọi lỗi chỉ log: thiết bị giữ chu kỳ đang có, và tầng xử lý bản tin thống kê đã
    /// tự cảnh báo khi chu kỳ nhận được lệch với chu kỳ kỳ vọng.
    /// </summary>
    public async Task PushStatsConfigAsync(CancellationToken ct = default)
    {
        var config = _config.Current;
        var statistic = config.Statistic;

        if (!statistic.PushPeriodToDevice) return;

        var period = statistic.ExpectedPeriodMinutes;
        if (period is < 1 or > 60)
        {
            _logger.LogWarning(
                "[OpConfig] Chu kỳ thống kê {Period} ngoài khoảng 1–60 phút ⇒ không đẩy lên thiết bị.", period);
            return;
        }

        var baseUrl = (config.Ingestion.ApiBaseUrl ?? "").Trim();
        if (baseUrl.Length == 0)
        {
            _logger.LogWarning(
                "[OpConfig] Bật PushPeriodToDevice nhưng Service:Ingestion:ApiBaseUrl còn trống ⇒ không có " +
                "địa chỉ thiết bị để đẩy chu kỳ.");
            return;
        }

        if (!Uri.TryCreate($"{baseUrl.TrimEnd('/')}{StatsConfigPath}", UriKind.Absolute, out var uri))
        {
            _logger.LogError("[OpConfig] ApiBaseUrl không hợp lệ: '{Base}'.", baseUrl);
            return;
        }

        // Yêu cầu này mang theo API key ở header, nên không gửi qua HTTP trần trừ khi đã cố tình cho phép.
        if (uri.Scheme == Uri.UriSchemeHttp && !config.Ingestion.AllowInsecureHttp)
        {
            _logger.LogWarning(
                "[OpConfig] {Url} dùng HTTP trần mà Service:Ingestion:AllowInsecureHttp = false ⇒ không đẩy " +
                "chu kỳ (yêu cầu này mang API key).", uri);
            return;
        }

        try
        {
            using var handler = new HttpClientHandler();
            if (config.Ingestion.AllowSelfSignedCert)
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(PushTimeoutSeconds) };

            // Dictionary thay vì object khuyết danh: tên trường của thiết bị là snake_case, mà mọi tuỳ chọn
            // đổi tên tự động đều là một chỗ nữa có thể lệch âm thầm với hợp đồng của thiết bị.
            var body = JsonSerializer.Serialize(new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["period_minutes"] = period,
            });

            using var request = new HttpRequestMessage(HttpMethod.Put, uri)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            config.Ingestion.ApplyAuth(request);

            using var response = await http.SendAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[OpConfig] PUT stats-config trả {Status} ⇒ thiết bị giữ chu kỳ đang có; bản tin thống kê " +
                    "có thể lệch với chu kỳ kỳ vọng {Period} phút.",
                    (int)response.StatusCode, period);
                return;
            }

            // Đọc period_minutes TRONG PHẢN HỒI, không coi 2xx là đã lưu đúng số gửi lên: mục 4.10 của API
            // v1.8 nói máy chủ KẸP giá trị ngoài khoảng 1–60 về mặc định 3 phút rồi vẫn trả 2xx. Ở đây đầu
            // vào đã được chặn 1–60 nên nhánh kẹp không nên xảy ra — chính vì vậy nếu nó xảy ra thì đó là
            // dấu hiệu hợp đồng đã đổi, phải thấy trong log thay vì bị ghi là thành công.
            var applied = await ReadAppliedPeriodAsync(response, ct).ConfigureAwait(false);

            if (applied == period)
            {
                _logger.LogInformation(
                    "[OpConfig] Đã đặt chu kỳ thống kê = {Period} phút trên thiết bị ({Url}); " +
                    "phản hồi xác nhận đúng giá trị.", period, uri);
            }
            else if (applied is int actual)
            {
                // KHÔNG tự đổi ExpectedPeriodMinutes theo số này: làm vậy là che mất việc cấu hình ITS và
                // thiết bị đang lệch nhau. Số thật được ghi ra để người vận hành quyết định.
                _logger.LogWarning(
                    "[OpConfig] PUT stats-config thành công nhưng thiết bị lưu {Actual} phút, không phải " +
                    "{Period} phút đã yêu cầu ({Url}) ⇒ bản tin thống kê sẽ theo {Actual} phút. Cấu hình ITS " +
                    "giữ nguyên; sửa chu kỳ kỳ vọng hoặc kiểm tra thiết bị.",
                    actual, period, uri, actual);
            }
            else
            {
                _logger.LogWarning(
                    "[OpConfig] PUT stats-config trả {Status} nhưng không đọc được period_minutes trong phản " +
                    "hồi ⇒ CHƯA xác nhận được thiết bị đã lưu {Period} phút ({Url}).",
                    (int)response.StatusCode, period, uri);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("OpConfigManager");
            _logger.LogWarning(ex,
                "[OpConfig] Không gọi được PUT stats-config ⇒ thiết bị giữ chu kỳ đang có.");
        }
    }

    /// <summary>
    /// <c>period_minutes</c> mà thiết bị BÁO ĐÃ LƯU, đọc từ phản hồi của PUT stats-config.
    ///
    /// <para>Trả <c>null</c> khi không có gì để tin: body rỗng, JSON sai, thiếu trường, hoặc trường không
    /// phải số. Bên gọi phân biệt "lệch giá trị" với "chưa xác nhận được" — hai tình huống cần hành động
    /// khác nhau nên không được gộp làm một.</para>
    /// </summary>
    private static async Task<int?> ReadAppliedPeriodAsync(
        HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body)) return null;

            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
            if (!document.RootElement.TryGetProperty("period_minutes", out var value)) return null;

            return value.ValueKind switch
            {
                JsonValueKind.Number => value.TryGetInt32(out var number) ? number : null,

                // Nhận cả số dạng chuỗi để không báo "chưa xác nhận được" chỉ vì kiểu JSON, nhưng KHÔNG
                // nhận giá trị không phải số.
                JsonValueKind.String =>
                    int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture,
                        out var parsed) ? parsed : null,

                _ => null,
            };
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("OpConfigManager");
            return null;
        }
    }

    /// <summary>
    /// <c>vds_route_1_min_speed = "50"</c> → <c>{"1": (50, …)}</c>.
    ///
    /// BỎ hướng nào khai thiếu một nửa hoặc có giá trị vô lý, kèm cảnh báo. Hướng không có ngưỡng thì
    /// fail-closed — an toàn hơn là đoán nốt nửa còn lại.
    /// </summary>
    internal Dictionary<string, SpeedRange> Parse(IReadOnlyDictionary<string, string?> byCode)
    {
        var min = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var max = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (rawCode, rawValue) in byCode)
        {
            var code = rawCode.Trim().ToLowerInvariant();
            if (!code.StartsWith(KeyPrefix, StringComparison.Ordinal)) continue;

            var isMin = code.EndsWith(MinSuffix, StringComparison.Ordinal);
            var isMax = code.EndsWith(MaxSuffix, StringComparison.Ordinal);
            if (!isMin && !isMax) continue;

            var direction = code[KeyPrefix.Length..^(isMin ? MinSuffix.Length : MaxSuffix.Length)];
            if (direction.Length == 0) continue;

            if (!int.TryParse((rawValue ?? "").Trim(), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var value))
            {
                _logger.LogWarning(
                    "[OpConfig] '{Code}' = '{Value}' không phải số nguyên; bỏ qua.", rawCode, rawValue);
                continue;
            }

            (isMin ? min : max)[direction] = value;
        }

        var result = new Dictionary<string, SpeedRange>(StringComparer.OrdinalIgnoreCase);

        foreach (var direction in min.Keys.Union(max.Keys, StringComparer.OrdinalIgnoreCase))
        {
            if (!min.TryGetValue(direction, out var lo) || !max.TryGetValue(direction, out var hi))
            {
                _logger.LogWarning(
                    "[OpConfig] Hướng '{Dir}' chỉ khai MỘT NỬA ngưỡng (min={Min}, max={Max}) ⇒ bỏ hướng này.",
                    direction,
                    min.TryGetValue(direction, out var m) ? m : null,
                    max.TryGetValue(direction, out var x) ? x : null);
                continue;
            }

            var range = new SpeedRange(lo, hi);
            if (!range.IsValid)
            {
                _logger.LogWarning(
                    "[OpConfig] Hướng '{Dir}' có ngưỡng không hợp lệ ({Range}) ⇒ bỏ hướng này.",
                    direction, range);
                continue;
            }

            result[direction] = range;
        }

        return result;
    }

    private void SaveCache(Dictionary<string, SpeedRange> ranges)
    {
        try
        {
            var json = JsonSerializer.Serialize(ranges, ConfigParamApplier.FileWriteOptions);
            StatePath.WriteAtomic(_cachePath, json);
        }
        catch (Exception ex)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("OpConfigManager");
            // Mất file cache chỉ ảnh hưởng lần khởi động sau khi DB lỗi; lượt này vẫn có ngưỡng từ DB.
            _logger.LogWarning(ex, "[OpConfig] Không ghi được file cache {Path}.", _cachePath);
        }
    }

    private Dictionary<string, SpeedRange>? LoadCache()
    {
        try
        {
            if (!File.Exists(_cachePath)) return null;

            return JsonSerializer.Deserialize<Dictionary<string, SpeedRange>>(
                File.ReadAllText(_cachePath), ConfigParamApplier.JsonOptions);
        }
        catch (Exception ex)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("OpConfigManager");
            _logger.LogWarning(ex, "[OpConfig] File cache {Path} hỏng; bỏ qua.", _cachePath);
            return null;
        }
    }

    private static string Describe(Dictionary<string, SpeedRange> ranges)
        => ranges.Count == 0
            ? "(không có hướng nào)"
            : string.Join(", ", ranges
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kv => $"hướng {kv.Key} {kv.Value}"));
}
