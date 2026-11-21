using Module.CCTV.Controllers.CctvPTZ.Dto;
using Module.CCTV.Infrastructure.Services.Ptz;
using Module.Integration.CCTV.Core.Abstracts;
using Modules.CfgSystem.Core.Dto.OpConfig;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Wolverine;

namespace Module.CCTV.Infrastructure.Services.Device
{
    public class DeviceService : INxVmsConfigProvider, IScoped
    {
        private readonly IMessageBus _bus;
        private const string GroupCode = "cctv_config";

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public DeviceService(IMessageBus bus)
        {
            _bus = bus;
        }

        // ── Public helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Lấy toàn bộ cấu hình của GroupCode = "cctv_config" dạng Dictionary.
        /// Key = Code, Value = Value.
        /// Dùng khi cần đọc nhiều key cùng lúc — chỉ gọi bus 1 lần.
        /// </summary>
        public async Task<Dictionary<string, string>> GetGroupAsync()
        {
            var result = await _bus.InvokeAsync<List<SysOpConfigOutput>>(
                new SysOpConfigInput { GroupCode = GroupCode });

            return result?.GroupBy(x => x.Code).ToDictionary(g => g.Key, g => g.Last().Value) ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Lấy Value theo Code trong GroupCode = "cctv_config".
        /// Throw nếu không tìm thấy hoặc chưa cấu hình.
        /// </summary>
        public async Task<string> GetValueAsync(string code)
        {
            var configs = await GetGroupAsync();

            if (!configs.TryGetValue(code, out var value) || string.IsNullOrWhiteSpace(value))
                throw Oops.Oh($"Chưa cấu hình key [{code}] trong SysOpConfig (GroupCode: {GroupCode}).");

            return value;
        }

        // ── Shortcut properties ───────────────────────────────────────────────────

        public Task<string> GetBaseUrlAsync() => GetValueAsync("cctv_base_url");
        public Task<string> GetUsernameAsync() => GetValueAsync("cctv_username");
        public Task<string> GetPasswordAsync() => GetValueAsync("cctv_password");
        public Task<string> GetDeviceTypeAsync() => GetValueAsync("cctv_device_type");
        public Task<string> GetSyncDeviceAsync() => GetValueAsync("cctv_timer_hangfire_syncdevice");


        /// <summary>
        /// Đọc PTZ config theo vendor, fallback 3 tầng:
        /// 1. {vendor}_ptz_config
        /// 2. cctv_ptz_fallback
        /// 3. PtzSpeedConfig.Default
        /// </summary>
        public async Task<PtzSpeedConfig> GetPtzConfigAsync(string? vendor, Func<Task<PtzSpeedConfig>>? deviceConfigFactory = null)
        {
            var v = (vendor ?? "nxvms").Trim().ToLower();

            // ── Tầng 1 & 2: OpConfig (sai/null đều nuốt, nhảy tiếp) ──────────────────
            try
            {
                var configs = await GetGroupAsync();

                // Tầng 1: cấu hình chính theo vendor
                if (configs.TryGetValue($"{v}_ptz_config", out var json)
                    && TryParseStandard(json, out var cfg))
                    return cfg;

                // Tầng 2: default backup dùng chung
                if (configs.TryGetValue("cctv_ptz_default", out var defJson)
                    && TryParseStandard(defJson, out var def))
                    return def;
            }
            catch { /* lỗi đọc OpConfig → nhảy xuống tầng 3 */ }

            // ── Tầng 3: cột Config thiết bị ─────────────────────────────────────────
            if (deviceConfigFactory != null)
                return await deviceConfigFactory();   // trả cfg hợp lệ hoặc tự ném lỗi

            // Không có deviceId để tra tầng 3.
            throw Oops.Oh($"Chưa cấu hình PTZ: thiếu [{v}_ptz_config] và [cctv_ptz_default]");
        }


        // Trong DeviceService.cs — thêm method này
        /// <summary>
        /// Lấy 3 thông tin cần cho login trong 1 lần query duy nhất.
        /// Tránh gọi GetGroupAsync() 3 lần riêng lẻ.
        /// </summary>
        public async Task<(string BaseUrl, string Username, string Password)> GetLoginConfigAsync()
        {
            var configs = await GetGroupAsync();

            if (!configs.TryGetValue("cctv_base_url", out var baseUrl) || string.IsNullOrWhiteSpace(baseUrl))
                throw Oops.Oh("Chưa cấu hình [cctv_base_url].");
            if (!configs.TryGetValue("cctv_username", out var username) || string.IsNullOrWhiteSpace(username))
                throw Oops.Oh("Chưa cấu hình [cctv_username].");
            if (!configs.TryGetValue("cctv_password", out var password) || string.IsNullOrWhiteSpace(password))
                throw Oops.Oh("Chưa cấu hình [cctv_password].");

            return (baseUrl, username, password);
        }


        /// <summary>
        /// Parse JSON OpConfig sang PtzSpeedConfig.
        /// Parse lỗi / null → THROW (theo yêu cầu: OpConfig có data mà sai thì báo lỗi).
        /// CHƯA validate range (Validator làm sau).
        /// </summary>
        private static PtzSpeedConfig ParseOpConfig(string json, string vendor)
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<PtzSpeedConfig>(json, _jsonOpts);
                if (parsed == null)
                    throw Oops.Oh($"Cấu hình [{vendor}_ptz_config] không hợp lệ (parse ra null).");
                return parsed;
            }
            catch (Exception ex) when (ex is System.Text.Json.JsonException)
            {
                throw Oops.Oh($"Cấu hình [{vendor}_ptz_config] sai định dạng JSON. Vui lòng kiểm tra lại.");
            }
        }


        /// <summary>
        /// Parse JSON OpConfig → PtzSpeedConfig, clamp giá trị về khoảng hợp lệ.
        /// Trả false (KHÔNG throw) khi value rỗng / parse lỗi / null
        /// → để tầng trên nuốt và nhảy xuống tầng fallback kế tiếp.
        /// Author: ChuongLP
        /// Date: 2026-06-15
        /// </summary>
        private static bool TryParseStandard(string? json, out PtzSpeedConfig cfg)
        {
            cfg = new PtzSpeedConfig();

            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<PtzSpeedConfig>(json, _jsonOpts);
                if (parsed == null) return false;

                cfg = new PtzSpeedConfig
                {
                    Pan = Math.Clamp(parsed.Pan, 0.05m, 1m),
                    Tilt = Math.Clamp(parsed.Tilt, 0.05m, 1m),
                    Zoom = Math.Clamp(parsed.Zoom, 0.05m, 1m),
                    Speed = Math.Clamp(parsed.Speed, 0.05m, 1m),
                    Focus = Math.Clamp(parsed.Focus, 0.05m, 1m),
                    ZoomNudgeMs = Math.Clamp(parsed.ZoomNudgeMs, 80, 1000),
                    // Không cấu hình (0) → mặc định 5s thay vì bị kẹp về 1s.
                    ContinuousMoveAutoStopMs = CctvPtzAutoStopService.ResolveAutoStopMs(parsed.ContinuousMoveAutoStopMs),
                    NudgeDurationMs = Math.Clamp(parsed.NudgeDurationMs, 80, 1000),
                };
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}