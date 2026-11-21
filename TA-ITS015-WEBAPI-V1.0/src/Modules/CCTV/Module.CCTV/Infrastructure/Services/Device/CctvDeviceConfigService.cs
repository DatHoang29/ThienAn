using Module.CCTV.Controllers.CctvPTZ.Dto;
using Module.CCTV.Infrastructure.Dto;     // CctvDeviceConfig, CctvPresetItem
using Modules.CCTV.Core.Entities;          // CctvDevice
using Modules.TMS.Core.Entities;           // TmsEquipment
using Modules.TMS.Infrastructure;          // BaseRepository
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
// Module có global using Newtonsoft.Json → chỉ định rõ System.Text.Json.
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Module.CCTV.Infrastructure.Services.Device
{
    /// <summary>
    /// Quản lý đọc/ghi danh sách Preset trong cột Config của TmsEquipment.
    ///
    /// Bối cảnh dữ liệu:
    ///   - CctvDevice: có DeviceId (NxVMS) + Ip, KHÔNG có Config.
    ///   - TmsEquipment: có Ip + Config, KHÔNG có DeviceId.
    ///   → Nối nhau qua Ip. Mọi thao tác Config phải đi đường:
    ///       DeviceId → CctvDevice (lấy Ip) → TmsEquipment (đọc/ghi Config).
    ///
    /// Service này gom logic 2-bước đó lại để handler PTZ gọn gàng.
    /// Author: ChuongLP
    /// Date: 2026-06-13
    /// </summary>
    public class CctvDeviceConfigService
    {
        private readonly BaseRepository<CctvDevice> _cctvDeviceRep;
        private readonly BaseRepository<TmsEquipment> _equipmentRep;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public CctvDeviceConfigService(
            BaseRepository<CctvDevice> cctvDeviceRep,
            BaseRepository<TmsEquipment> equipmentRep)
        {
            _cctvDeviceRep = cctvDeviceRep;
            _equipmentRep = equipmentRep;
        }


        // ── PTZ default config theo thiết bị ───────────────────────────────────────

        /// <summary>
        /// Đọc cấu hình PTZ lưu trong cột Config (field "ptz") của thiết bị.
        /// Dùng khi OpConfig {vendor}_ptz_config KHÔNG có key.
        ///   - Thiết bị / IP / TmsEquipment không resolve được → ResolveEquipmentAsync tự throw.
        ///   - Config rỗng hoặc field "ptz" null → throw: thiết bị chưa cấu hình PTZ.
        /// Tầng trên không cần check null — gọi là chắc chắn có cfg hợp lệ hoặc đã throw.
        /// Author: ChuongLP
        /// </summary>
        public async Task<PtzSpeedConfig> GetDevicePtzConfigAsync(string? deviceId, CancellationToken ct = default)
        {
            var equipment = await ResolveEquipmentAsync(deviceId, ct);
            var ptz = ParseConfig(equipment.Config).Ptz
                ?? throw Oops.Oh($"Thiết bị [{deviceId}] chưa được cấu hình PTZ (cả OpConfig lẫn Config thiết bị đều trống).");

            return ptz;
        }


        // ── Resolve: DeviceId → TmsEquipment qua Ip ───────────────────────────────

        /// <summary>
        /// Từ DeviceId (NxVMS) tìm ra TmsEquipment để đọc/ghi Config.
        ///   - CctvDevice không tồn tại → chưa sync.
        ///   - CctvDevice thiếu Ip → không nối được sang TmsEquipment.
        ///   - >1 TmsEquipment trùng Ip → KHÔNG đoán, throw rõ ràng để tránh
        ///     ghi preset vào nhầm thiết bị (bug âm thầm rất khó debug).
        /// </summary>
        public async Task<TmsEquipment> ResolveEquipmentAsync(string? deviceId, CancellationToken ct = default)
        {
            var cctv = await _cctvDeviceRep.GetFirstAsync(x => x.DeviceId == deviceId)
                ?? throw Oops.Oh($"Không tìm thấy camera [{deviceId}]. Vui lòng đồng bộ thiết bị trước.");

            if (string.IsNullOrWhiteSpace(cctv.Ip))
                throw Oops.Oh($"Camera [{deviceId}] chưa có IP để liên kết với thiết bị TMS.");

            var matches = await _equipmentRep.AsQueryable()
                .Where(x => x.Ip == cctv.Ip)
                .ToListAsync();

            if (matches.Count == 0)
                throw Oops.Oh($"Không tìm thấy thiết bị TMS nào có IP [{cctv.Ip}].");

            if (matches.Count > 1)
                throw Oops.Oh($"Có {matches.Count} thiết bị TMS trùng IP [{cctv.Ip}]. " +
                              $"Cần làm rõ dữ liệu trước khi lưu preset để tránh ghi nhầm thiết bị.");

            return matches[0];
        }

        // ── Parse / Serialize Config ──────────────────────────────────────────────
        //
        // Cột TmsEquipment.Config dùng chung với màn hình Thiết bị TMS: FE đọc/ghi dạng key → chuỗi phẳng
        // ({"k":"v"}, editEquipment.vue). Vì vậy CCTV lưu dữ liệu của mình thành CHUỖI JSON dưới 2 key riêng
        // và giữ nguyên mọi key khác:
        //   "cctv_presets": "[{\"presetId\":\"1\",\"name\":\"Cổng\"}]"
        //   "cctv_ptz":     "{\"Pan\":0.5,...}"
        // Dữ liệu cũ dạng lồng ("presets": [...], "ptz": {...}) vẫn đọc được; lần ghi preset kế tiếp tự chuyển sang dạng mới.
        // Updated: 21/10/2026

        public const string PresetsKey = "cctv_presets";
        public const string PtzKey = "cctv_ptz";
        private const string LegacyPresetsKey = "presets";
        private const string LegacyPtzKey = "ptz";

        // Giữ nguyên tiếng Việt (không escape \uXXXX) để tab Cấu hình bên TMS đọc được.
        private static readonly JsonSerializerOptions _writeOpts = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>Đọc phần cấu hình CCTV trong cột Config. Null/hỏng → object rỗng (chỉ dùng cho luồng đọc).</summary>
        public CctvDeviceConfig ParseConfig(string? configJson)
        {
            var root = ParseRootOrNull(configJson);
            return root == null ? new CctvDeviceConfig() : ReadCctvConfig(root);
        }

        /// <summary>Parse cột Config thành JsonObject. Rỗng → object mới; không phải JSON object → null.</summary>
        private static JsonObject? ParseRootOrNull(string? configJson)
        {
            if (string.IsNullOrWhiteSpace(configJson))
                return new JsonObject();

            try
            {
                return JsonNode.Parse(configJson) as JsonObject;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static CctvDeviceConfig ReadCctvConfig(JsonObject root) => new()
        {
            Presets = ReadNode<List<CctvPresetItem>>(root, PresetsKey, LegacyPresetsKey) ?? new List<CctvPresetItem>(),
            Ptz = ReadNode<PtzSpeedConfig>(root, PtzKey, LegacyPtzKey)
        };

        /// <summary>
        /// Đọc giá trị theo key mới (chuỗi JSON), không có thì theo key cũ (node lồng).
        /// Giá trị hỏng (vd "[object Object]" do màn hình TMS lưu đè dữ liệu lồng cũ) → bỏ qua.
        /// </summary>
        private static T? ReadNode<T>(JsonObject root, params string[] keys) where T : class
        {
            foreach (var key in keys)
            {
                var node = root[key];
                if (node == null)
                    continue;

                try
                {
                    if (node is JsonValue value && value.TryGetValue<string>(out var text))
                    {
                        if (string.IsNullOrWhiteSpace(text))
                            continue;
                        return JsonSerializer.Deserialize<T>(text, _jsonOpts);
                    }

                    return node.Deserialize<T>(_jsonOpts);
                }
                catch (JsonException)
                {
                    // Thử key kế tiếp.
                }
            }

            return null;
        }

        // ── Preset operations ─────────────────────────────────────────────────────

        /// <summary>Đọc danh sách preset từ Config của thiết bị (cho FE render).</summary>
        public List<CctvPresetItem> GetPresets(TmsEquipment equipment)
            => ParseConfig(equipment.Config).Presets;

        /// <summary>
        /// Thêm 1 preset vào Config rồi ghi xuống DB.
        /// Nếu presetId đã tồn tại → cập nhật name (idempotent, tránh trùng).
        /// </summary>
        public Task AddPresetAsync(TmsEquipment equipment, string presetId, string? name, CancellationToken ct = default)
            => UpdatePresetsAsync(equipment, presets =>
            {
                var existing = presets.FirstOrDefault(p => p.PresetId == presetId);
                if (existing != null)
                    existing.Name = name ?? existing.Name;
                else
                    presets.Add(new CctvPresetItem { PresetId = presetId, Name = name ?? string.Empty });
                return true;
            }, ct);

        /// <summary>Đổi tên 1 preset trong Config theo presetId.</summary>
        public Task UpdatePresetNameAsync(TmsEquipment equipment, string presetId, string? newName, CancellationToken ct = default)
            => UpdatePresetsAsync(equipment, presets =>
            {
                var item = presets.FirstOrDefault(p => p.PresetId == presetId);
                if (item == null)
                    return false;
                item.Name = newName ?? item.Name;
                return true;
            }, ct);

        /// <summary>Xóa 1 preset khỏi Config theo presetId.</summary>
        public Task RemovePresetAsync(TmsEquipment equipment, string presetId, CancellationToken ct = default)
            => UpdatePresetsAsync(equipment, presets => presets.RemoveAll(p => p.PresetId == presetId) > 0, ct);

        /// <summary>
        /// Tra presetId từ Name (dùng khi FE chỉ gửi Name, không gửi PresetId).
        /// Trùng tên → lấy match đầu tiên (Name không phải khóa định danh chuẩn).
        /// Không tìm thấy → null.
        /// </summary>
        public string? ResolvePresetIdByName(TmsEquipment equipment, string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return ParseConfig(equipment.Config).Presets
                .FirstOrDefault(p => p.Name == name)?.PresetId;
        }

        // ── Persist ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Đọc lại Config mới nhất từ DB, áp thay đổi lên danh sách preset rồi chỉ ghi lại key của CCTV,
        /// giữ nguyên các key khác (cấu hình nhập ở màn hình Thiết bị TMS).
        /// Config hiện tại không phải JSON object → throw, KHÔNG ghi đè.
        /// </summary>
        /// <param name="mutate">Sửa danh sách preset; trả false nếu không có gì thay đổi (bỏ qua ghi DB).</param>
        private async Task UpdatePresetsAsync(TmsEquipment equipment, Func<List<CctvPresetItem>, bool> mutate, CancellationToken ct)
        {
            var currentJson = (await _equipmentRep.AsQueryable()
                    .Where(x => x.ID == equipment.ID)
                    .Select(x => x.Config)
                    .ToListAsync(ct))
                .FirstOrDefault();

            var root = ParseRootOrNull(currentJson)
                ?? throw Oops.Oh($"Cấu hình (Config) của thiết bị [{equipment.Name}] không phải JSON hợp lệ. " +
                                 "Vui lòng kiểm tra lại tại màn hình Thiết bị trước khi lưu preset.");

            var cctv = ReadCctvConfig(root);
            if (!mutate(cctv.Presets))
                return;

            root[PresetsKey] = JsonSerializer.Serialize(cctv.Presets, _writeOpts);
            root.Remove(LegacyPresetsKey);

            // Chuyển luôn ptz dạng cũ (object lồng) sang chuỗi để màn hình TMS lưu lại không làm hỏng.
            if (root.ContainsKey(LegacyPtzKey) && cctv.Ptz != null)
            {
                root[PtzKey] = JsonSerializer.Serialize(cctv.Ptz, _writeOpts);
                root.Remove(LegacyPtzKey);
            }

            equipment.Config = root.ToJsonString(_writeOpts);
            await _equipmentRep.AsUpdateable(equipment)
                .UpdateColumns(x => new { x.Config })   // chỉ update cột Config, tránh đụng field khác
                .ExecuteCommandAsync();
        }
    }
}
