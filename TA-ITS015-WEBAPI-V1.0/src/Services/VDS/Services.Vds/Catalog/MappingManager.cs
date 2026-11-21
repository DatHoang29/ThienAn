using System.Text.Json;
using Services.Vds.Configuration;

namespace Services.Vds.Catalog;

/// <summary>
/// Author: KienLT
/// Description: Nạp các MAPPING cấu hình từ <c>VdsConfig</c> (danh mục cha–con) và đẩy vào snapshot
/// VDS qua cùng cửa <c>VdsConfigManager.Apply</c>.
///
/// <para>Hai type cha trong <c>VdsConfig.GroupCode</c>:</para>
/// <list type="bullet">
/// <item><b><c>vds_mapping_event_type</c></b>: mỗi dòng con là một cặp mapping sự kiện —
/// <c>Code</c> = loại VDS (ví dụ <c>Speeding</c>), <c>Value</c> = mã ITS <c>TmsEventType.Code</c>
/// (ví dụ <c>speeding</c>).</item>
/// <item><b><c>vds_mapping_vehicle_size</c></b>: mỗi dòng con là một tham số cỡ xe —
/// <c>Code</c> ∈ {<c>mode</c>, <c>shortMax</c>, <c>mediumMax</c>, <c>unit</c>}, <c>Value</c> là giá trị.</item>
/// </list>
///
/// <para><b>Nguồn chính là DB; appsettings chỉ để FALLBACK.</b> Type/con không có hoặc rỗng ⇒ giữ nguyên
/// giá trị đã bind từ <c>appsettings.json</c> (không xoá trắng). Khai sai ⇒ cửa <c>Apply</c> từ chối và
/// giữ giá trị đang có.</para>
///
/// KHÔNG BAO GIỜ ném lỗi ra ngoài: thiếu mapping chỉ làm dùng fallback, không phải lý do chặn khởi động.
/// Created date: 04/09/2026
/// </summary>
public sealed class MappingManager
{
    /// <summary><c>VdsConfig.GroupCode.Code</c> cha của mapping sự kiện VDS ↔ ITS.</summary>
    public const string EventTypeCode = "vds_mapping_event_type";

    /// <summary><c>VdsConfig.GroupCode.Code</c> cha của mapping cỡ xe.</summary>
    public const string VehicleSizeCode = "vds_mapping_vehicle_size";

    private readonly VdsCatalogRepository _catalog;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<MappingManager> _logger;

    public MappingManager(
        VdsCatalogRepository catalog, IVdsConfigManager config, ILogger<MappingManager> logger)
    {
        _catalog = catalog;
        _config = config;
        _logger = logger;
    }

    /// <summary>Nạp cả hai mapping. Lỗi từng phần được nuốt và log; giữ fallback appsettings.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        await LoadEventTypeMappingAsync(ct).ConfigureAwait(false);
        await LoadVehicleSizeMappingAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Mapping sự kiện: mỗi dòng con <c>Code</c> (loại VDS) → <c>Value</c> (mã ITS). Không có dòng nào ⇒
    /// giữ map appsettings.
    /// </summary>
    public async Task LoadEventTypeMappingAsync(CancellationToken ct = default)
    {
        List<VdsCatalogRepository.ConfigDataRow> rows;
        try
        {
            rows = await _catalog.QueryConfigDataAsync(EventTypeCode, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("MappingManager");
            _logger.LogWarning(ex,
                "[Mapping] Lỗi đọc VdsConfig type '{Type}'; giữ mapping sự kiện đang dùng.", EventTypeCode);
            return;
        }

        // DB definitions extend the configured mapping; an empty group must not erase it.
        var baseline = ((_config as VdsConfigManager)?.ReadBaseline() ?? _config.Current).Events.EventTypeCodes;
        var map = new Dictionary<string, string>(baseline, StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.Code) || string.IsNullOrWhiteSpace(r.Value)) continue;
            map[r.Code.Trim()] = r.Value.Trim();
        }

        var json = JsonSerializer.Serialize(map);
        if (_config.Apply(ConfigParamApplier.EventTypeCodes, json))
            _logger.LogInformation("[Mapping] Đã áp {Count} cặp mapping sự kiện VDS↔ITS từ VdsConfig.", map.Count);
        else
            _logger.LogWarning("[Mapping] Mapping sự kiện từ VdsConfig không dùng được; giữ giá trị đang có.");
    }

    /// <summary>
    /// Mapping cỡ xe: các dòng con <c>Code</c> ∈ {mode, shortMax, mediumMax, unit} → <c>Value</c>.
    /// Không có dòng nào ⇒ giữ cấu hình appsettings.
    /// </summary>
    public async Task LoadVehicleSizeMappingAsync(CancellationToken ct = default)
    {
        List<VdsCatalogRepository.ConfigDataRow> rows;
        try
        {
            rows = await _catalog.QueryConfigDataAsync(VehicleSizeCode, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("MappingManager");
            _logger.LogWarning(ex,
                "[Mapping] Lỗi đọc VdsConfig type '{Type}'; giữ cấu hình cỡ xe đang dùng.", VehicleSizeCode);
            return;
        }

        var byCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            if (string.IsNullOrWhiteSpace(r.Code) || string.IsNullOrWhiteSpace(r.Value)) continue;
            byCode[r.Code.Trim()] = r.Value.Trim();
        }

        // Dựng đúng JSON mà ConfigParamApplier.ApplyVehicleSizeMapping nhận. Trường thiếu thì bỏ khỏi JSON
        // (applier sẽ giữ giá trị đang có cho trường đó).
        var baseline = ((_config as VdsConfigManager)?.ReadBaseline() ?? new ServiceConfig()).VehicleSize;
        var payload = new Dictionary<string, object>(StringComparer.Ordinal) { ["mode"] = baseline.VehicleSizeMode, ["unit"] = baseline.VehicleDimensionUnit, ["shortMax"] = baseline.VehicleLengthShortMax, ["mediumMax"] = baseline.VehicleLengthMediumMax };
        if (byCode.TryGetValue("mode", out var mode)) payload["mode"] = mode;
        if (byCode.TryGetValue("unit", out var unit)) payload["unit"] = unit;
        if (byCode.TryGetValue("shortMax", out var shortMax)) payload["shortMax"] = shortMax;
        if (byCode.TryGetValue("mediumMax", out var mediumMax)) payload["mediumMax"] = mediumMax;

        if (payload.Count == 0)
        {
            _logger.LogWarning(
                "[Mapping] VdsConfig type '{Type}' có dòng nhưng không dòng nào là mode/unit/shortMax/" +
                "mediumMax ⇒ giữ cấu hình cỡ xe đang dùng.", VehicleSizeCode);
            return;
        }

        var json = JsonSerializer.Serialize(payload);
        if (_config.Apply(ConfigParamApplier.VehicleSizeMapping, json))
            _logger.LogInformation("[Mapping] Đã áp cấu hình cỡ xe từ VdsConfig ({Count} trường).", payload.Count);
        else
            _logger.LogWarning(
                "[Mapping] Cấu hình cỡ xe từ VdsConfig không dùng được (giá trị vô lý); giữ giá trị đang có.");
    }
}
