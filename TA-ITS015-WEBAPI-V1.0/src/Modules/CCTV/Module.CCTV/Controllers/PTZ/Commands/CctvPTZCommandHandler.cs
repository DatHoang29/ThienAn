using Microsoft.Extensions.Logging;
using Module.CCTV.Controllers.CctvGetDevice;          // PtzDirection, ZoomDirection
using Module.CCTV.Controllers.CctvPTZ.Dto;            // CctvXxx Input/Output
using Module.CCTV.Infrastructure.Services.Device;     // DeviceService
using Module.CCTV.Infrastructure.Services.Ptz;        // CctvPtzAutoStopService
using Module.CCTV.Infrastructure.Services.Vendor;     // VendorService
using Module.Integration.CCTV.Core.Dto;               // VmsActionOutput
using Module.Integration.NxVms.Infrastructure.Dto;
using Modules.CCTV.Core.Entities;
using Modules.CCTV.Core.Exceptions;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Wolverine;
namespace Module.CCTV.Controllers.PTZ.Commands
{
    /// <summary>
    /// Xử lý toàn bộ lệnh WRITE của PTZ (Commands).
    /// Các truy vấn READ-ONLY (GetPosition, GetLimits, GetPresets, GetTours)
    /// Luồng chuẩn mỗi handle:
    ///   1. GetAsync(vendor) → resolve đúng VMS service.
    ///   2. ResolveDeviceAsync(deviceId, ip) → tra DB lấy CctvDevice (ưu tiên IP nếu FE truyền).
    ///   3. GetPtzConfigAsync(vendor) → đọc speed config từ SysOpConfig.
    ///   4. Gọi BaseCctvService — không phụ thuộc hãng cụ thể.
    /// Lưu ý: FE có thể truyền IP thay cho DeviceId. Dù vậy, tầng NxVMS luôn được
    /// gọi bằng device.DeviceId đã resolve từ DB.
    /// Author: ChuongLP
    /// Date: 2026-06-13
    /// </summary>
    public class CctvPTZCommandHandler : IWolverineHandler
    {
        private readonly VendorService _vendorService;
        private readonly DeviceService _deviceService;
        private readonly CctvDeviceConfigService _configService;
        private readonly CctvDeviceResolveService _resolveService;
        private readonly CctvPtzAutoStopService _autoStopService;
        private readonly ILogger<CctvPTZCommandHandler> _logger;
        public CctvPTZCommandHandler(
            VendorService vendorService,
            DeviceService deviceService,
            CctvDeviceConfigService configService,
            CctvDeviceResolveService resolveService,
            CctvPtzAutoStopService autoStopService,
            ILogger<CctvPTZCommandHandler> logger)
        {
            _logger = logger;
            _vendorService = vendorService;
            _deviceService = deviceService;
            _configService = configService;
            _resolveService = resolveService;
            _autoStopService = autoStopService;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // DI CHUYỂN LIÊN TỤC
        // ═══════════════════════════════════════════════════════════════════════════

       
        /// <summary>
        /// Nhích PTZ một bước ngắn — dùng cho click/tap, không dùng cho nhấn giữ.
        /// BE tự xử lý chuỗi: Stop phiên cũ → Move → Delay(NudgeDurationMs) → Stop.
        /// Đảm bảo luôn có lệnh dừng kể cả FE mất timer hay browser bị lag.
        /// Author: ChuongLP
        /// Date: 2026-06-11
        /// </summary>
        public async Task<CctvActionOutput> HandleAsync(CctvPtzNudgeInput input, CancellationToken ct = default)
        {
            var service = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, ct);
            var cfg = await _deviceService.GetPtzConfigAsync(input.Vendor,
                        () => _configService.GetDevicePtzConfigAsync(device.DeviceId, ct));

            var generation = _autoStopService.Begin(device.DeviceId);

            try
            {
                var moveCfg = OverridePanTilt(cfg, input.Speed);
                var (pan, tilt) = MapDirectionToSpeed(input.Direction, moveCfg);

                var moveResult = await service.MovePtzAsync(new VmsPtzMoveInput
                { DeviceId = device.DeviceId, Pan = pan, Tilt = tilt, Zoom = 0 }, ct);
                if (!moveResult.Success) return ToOutput(moveResult);

                // ── Đè giữ: chạy rồi trả về ngay. FE thả tay gọi StopPtz; FE mất kết nối thì BE tự Stop sau ContinuousMoveAutoStopMs. ──
                if (input.IsHold)
                {
                    _autoStopService.ScheduleAutoStop(device.DeviceId, generation, input.Vendor,
                        CctvPtzAutoStopService.ResolveAutoStopMs(cfg.ContinuousMoveAutoStopMs));
                    return new CctvActionOutput { Success = true, Message = $"PTZ đè giữ hướng [{input.Direction}]." };
                }

                // ── Nhích 1 phát: Move → Delay → Stop (bỏ Stop nếu đã có lệnh mới hơn, vd FE chuyển sang đè giữ) ──
                await Task.Delay(cfg.NudgeDurationMs, CancellationToken.None);
                if (_autoStopService.IsLatest(device.DeviceId, generation))
                    await service.StopPtzAsync(device.DeviceId, CancellationToken.None);

                return new CctvActionOutput
                { Success = true, Message = $"PTZ đã nhích hướng [{input.Direction}] trong {cfg.NudgeDurationMs}ms." };
            }
            catch (Exception ex) when (ex is not AppFriendlyException) // Lỗi nghiệp vụ NxVMS (101/102/106) giữ nguyên thông điệp
            {
                throw Oops.Oh(BaseMsg.CctvPtz.Exception.MoveFailed, device.DeviceId);
            }
        }


        /// <summary>
        /// Zoom liên tục (In = phóng to, Out = thu nhỏ).
        /// FE gọi 1 lần khi nhấn giữ, gọi StopPtz khi thả.
        /// Author: ChuongLP
        /// Date: 2026-06-11
        /// </summary>
        public async Task<CctvActionOutput> HandleAsync(CctvPtzZoomInput input, CancellationToken ct = default)
        {
            var service = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, ct);
            var cfg = await _deviceService.GetPtzConfigAsync(input.Vendor,
                        () => _configService.GetDevicePtzConfigAsync(device.DeviceId, ct));

            var zoomCfg = OverrideZoom(cfg, input.Speed);
            var zoomSpeed = input.Direction == ZoomDirection.In ? zoomCfg.Zoom : -zoomCfg.Zoom;
            var generation = _autoStopService.Begin(device.DeviceId);

            try
            {
                var moveResult = await service.MovePtzAsync(new VmsPtzMoveInput
                { DeviceId = device.DeviceId, Pan = 0, Tilt = 0, Zoom = zoomSpeed, Focus = 0 }, ct);
                if (!moveResult.Success) return ToOutput(moveResult);

                // ── Đè giữ: chạy zoom liên tục rồi trả về ngay. FE thả tay gọi StopPtz; mất kết nối thì BE tự Stop. ──
                if (input.IsHold)
                {
                    _autoStopService.ScheduleAutoStop(device.DeviceId, generation, input.Vendor,
                        CctvPtzAutoStopService.ResolveAutoStopMs(cfg.ContinuousMoveAutoStopMs));
                    return new CctvActionOutput { Success = true, Message = $"Zoom đè giữ [{input.Direction}]." };
                }

                // ── Nhích 1 nấc: Move → Delay(ZoomNudgeMs) → Stop (bỏ Stop nếu đã có lệnh mới hơn) ──
                await Task.Delay(cfg.ZoomNudgeMs, CancellationToken.None);
                if (_autoStopService.IsLatest(device.DeviceId, generation))
                    await service.StopPtzAsync(device.DeviceId, CancellationToken.None);

                return new CctvActionOutput
                { Success = true, Message = $"Zoom [{input.Direction}] {cfg.ZoomNudgeMs}ms." };
            }
            catch (Exception ex) when (ex is not AppFriendlyException) // Lỗi nghiệp vụ NxVMS (101/102/106) giữ nguyên thông điệp
            {
                throw Oops.Oh(BaseMsg.CctvPtz.Exception.MoveFailed, device.DeviceId);
            }
        }


        /// <summary>
        /// Dừng mọi di chuyển PTZ. Hủy auto-stop timer nếu đang có.
        /// Author: ChuongLP
        /// Date: 2026-06-10
        /// </summary>
        public async Task<CctvActionOutput> HandleAsync(CctvPtzStopInput input, CancellationToken ct = default)
        {
            var service = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, ct);
            _autoStopService.Begin(device.DeviceId);   // hủy hẹn tự dừng đang chờ

            try
            {
                var result = await service.StopPtzAsync(device.DeviceId, ct);
                return ToOutput(result);
            }
            catch (Exception ex) when (ex is not AppFriendlyException) // Lỗi nghiệp vụ NxVMS (101/102/106) giữ nguyên thông điệp
            {
                throw Oops.Oh(BaseMsg.CctvPtz.Exception.MoveFailed, device.DeviceId);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // VỊ TRÍ TUYỆT ĐỐI
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Di chuyển PTZ đến tọa độ tuyệt đối (pan, tilt, fov).
        /// Gọi /ptz/positions — khác /ptz/move (continuous).
        /// Tự dừng phiên continuous đang chạy trước khi thực hiện.
        /// FIX: Dùng input.Fov (không phải input.Zoom — Fov là field đúng của CctvPtzPositionInput).
        /// Author: ChuongLP
        /// Date: 2026-06-10
        /// </summary>
        public async Task<CctvActionOutput> HandleAsync(CctvPtzPositionInput input, CancellationToken ct = default)
        {
            var service = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, ct);
            _autoStopService.Begin(device.DeviceId);   // lệnh vị trí tuyệt đối thay thế phiên đè giữ

            try
            {
                var result = await service.MovePtzPositionAsync(new VmsPtzPositionInput
                {
                    DeviceId = device.DeviceId,
                    Pan = Math.Clamp(input.Pan, -1m, 1m),
                    Tilt = Math.Clamp(input.Tilt, -1m, 1m),
                    Zoom = Math.Clamp(input.Zoom, 0m, 1m)
                }, ct);

                return ToOutput(result);
            }
            catch (Exception ex) when (ex is not AppFriendlyException) // Lỗi nghiệp vụ NxVMS (101/102/106) giữ nguyên thông điệp
            {
                throw Oops.Oh(BaseMsg.CctvPtz.Exception.MoveFailed, device.DeviceId);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // PRESET
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Kích hoạt preset — camera di chuyển về góc đã lưu.
        /// Author: ChuongLP
        /// Date: 2026-06-10
        /// </summary>
        public async Task<CctvActionOutput> HandleAsync(CctvActivatePresetInput input, CancellationToken ct = default)
        {
            var service = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, ct);
            var cfg = await _deviceService.GetPtzConfigAsync(input.Vendor);

            // FE truyền hợp lệ (> 0) → dùng; ngược lại → lấy Speed từ config.
            var speed = (input.Speed.HasValue && input.Speed.Value > 0)
                        ? Math.Clamp(input.Speed.Value, 0.05m, 1m)
                        : Math.Clamp(cfg.Speed, 0.05m, 1m);

            _autoStopService.Begin(device.DeviceId);   // kích hoạt preset thay thế phiên đè giữ
            var result = await service.ActivatePresetAsync(
                new VmsActivatePresetInput
                { 
                    DeviceId = device.DeviceId,
                    PresetId = input.PresetId,
                    Speed    = speed
                }, ct);
            return ToOutput(result);
        }

        /// <summary>
        /// Lưu vị trí PTZ hiện tại thành Preset mới.
        /// PresetId null → NxVMS tự sinh ID.
        /// Author: ChuongLP
        /// Date: 2026-06-13
        /// </summary>
        public async Task<CctvActionOutput> HandleAsync(CctvAddPresetInput input, CancellationToken ct = default)
        {
            var service = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, ct);

            // Service: add lên camera + xác định đúng presetId vừa tạo.
            var saved = await service.AddPresetAndResolveIdAsync(new NxVmsAddPresetInput
            {
                DeviceId = device.DeviceId,
                PresetName = input.PresetName
            }, ct);

            // Không xác định được id → KHÔNG ghi Config, trả lỗi để FE biết.
            if (!saved.Success || string.IsNullOrWhiteSpace(saved.PresetId))
                return new CctvActionOutput { Success = false, Message = saved.Message };

            // Có presetId chính xác → giờ mới ghi vào Config TmsEquipment.
            var equipment = await _configService.ResolveEquipmentAsync(device.DeviceId, ct);
            await _configService.AddPresetAsync(equipment, saved.PresetId, saved.Name, ct);

            return new CctvActionOutput { Success = true, Message = saved.Message };
        }

        /// <summary>
        /// Đổi tên một Preset đã tồn tại.
        /// Author: ChuongLP
        /// Date: 2026-06-13
        /// </summary>
        /// Update 21/10/2026: NxVMS là nguồn chuẩn — đổi tên trên camera trước, thành công mới đồng bộ Config.
        public async Task<CctvActionOutput> HandleAsync(CctvUpdatePresetInput input, CancellationToken ct = default)
        {
            var service = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, ct);

            var result = await service.UpdatePresetAsync(new NxVmsUpdatePresetInput
            {
                DeviceId = device.DeviceId,
                PresetId = input.PresetId,
                Name = input.PresetName
            }, ct);
            if (!result.Success)
                return ToOutput(result);

            await TrySyncPresetConfigAsync(device.DeviceId,
                equipment => _configService.UpdatePresetNameAsync(equipment, input.PresetId!, input.PresetName, ct), ct);

            return new CctvActionOutput
            {
                Success = true,
                Message = $"Đã đổi tên preset [{input.PresetId}] thành [{input.PresetName}]."
            };
        }

        /// <summary>
        /// Xóa một Preset.
        /// Author: ChuongLP
        /// Date: 2026-06-10
        /// </summary>
        public async Task<CctvActionOutput> HandleAsync(CctvDeletePresetInput input, CancellationToken ct = default)
        {
            var service = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, ct);
            var result = await service.DeletePresetAsync(new VmsDeletePresetInput
            {
                DeviceId = device.DeviceId,
                PresetId = input.PresetId
            }, ct);

            // Update 21/10/2026: xóa trên NxVMS thành công → xóa luôn trong Config để không còn preset "mồ côi".
            if (result.Success)
                await TrySyncPresetConfigAsync(device.DeviceId,
                    equipment => _configService.RemovePresetAsync(equipment, input.PresetId!, ct), ct);

            return ToOutput(result);
        }

        /// <summary>
        /// Đồng bộ danh sách preset trong Config TmsEquipment sau khi thao tác trên NxVMS đã thành công.
        /// Lỗi ở bước này (không tìm thấy/trùng IP thiết bị TMS, Config sai JSON) chỉ ghi log —
        /// không báo lỗi cho FE vì thao tác trên camera (nguồn chuẩn) đã hoàn tất.
        /// Author: ChuongLP
        /// Date: 21/10/2026
        /// </summary>
        private async Task TrySyncPresetConfigAsync(string deviceId, Func<TmsEquipment, Task> sync, CancellationToken ct)
        {
            try
            {
                var equipment = await _configService.ResolveEquipmentAsync(deviceId, ct);
                await sync(equipment);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "CCTV preset: đã cập nhật trên NxVMS nhưng không đồng bộ được Config thiết bị TMS. DeviceId={DeviceId}", deviceId);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // TOUR
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Kích hoạt tour — camera bắt đầu hành trình tuần tra tự động.
        /// Author: ChuongLP
        /// Date: 2026-06-10
        /// </summary>
        public async Task<CctvActionOutput> HandleAsync(CctvActivateTourInput input, CancellationToken ct = default)
        {
            var service = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, ct);

            try
            {
                var result = await service.ActivateTourAsync(
                    new VmsActivateTourInput { DeviceId = device.DeviceId, TourId = input.TourId }, ct);
                return ToOutput(result);
            }
            catch (Exception ex) when (ex is not AppFriendlyException) // Lỗi nghiệp vụ NxVMS (101/102/106) giữ nguyên thông điệp
            {
                throw Oops.Oh(BaseMsg.CctvPtz.Exception.ActivateTourFailed, input.TourId);
            }
        }


        public async Task<CctvActionOutput> HandleAsync(CctvPtzFocusInput input, CancellationToken ct = default)
        {
            var service = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, ct);
            var cfg = await _deviceService.GetPtzConfigAsync(input.Vendor,
                        () => _configService.GetDevicePtzConfigAsync(device.DeviceId, ct));

            var focusCfg = OverrideFocus(cfg, input.Speed);
            var focusSpeed = input.Direction == FocusDirection.In ? focusCfg.Focus : -focusCfg.Focus;
            var generation = _autoStopService.Begin(device.DeviceId);

            try
            {
                var moveResult = await service.MovePtzAsync(new VmsPtzMoveInput
                {
                    DeviceId = device.DeviceId,
                    Pan = 0,
                    Tilt = 0,
                    Zoom = 0,
                    Focus = focusSpeed
                }, ct);
                if (!moveResult.Success) return ToOutput(moveResult);

                // Xung ngắn rồi dừng — chỉnh từng chút (FE giữ nút sẽ gửi lặp; xung mới hơn tự lo Stop).
                await Task.Delay(100, CancellationToken.None);
                if (_autoStopService.IsLatest(device.DeviceId, generation))
                    await service.StopPtzAsync(device.DeviceId, CancellationToken.None);

                return new CctvActionOutput { Success = true, Message = $"Focus [{input.Direction}] 100ms." };
            }
            catch (Exception ex) when (ex is not AppFriendlyException) // Lỗi nghiệp vụ NxVMS (101/102/106) giữ nguyên thông điệp
            {
                throw Oops.Oh(BaseMsg.CctvPtz.Exception.MoveFailed, device.DeviceId);
            }
        }


        // ═══════════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Map 8 hướng → (pan, tilt).
        /// Quy ước NxVMS: Pan dương = phải, Tilt dương = lên.
        /// Hướng chéo giữ nguyên magnitude 2 trục, chỉ đổi dấu.
        /// </summary>
        private static (decimal pan, decimal tilt ) MapDirectionToSpeed(
            PtzDirection direction, PtzSpeedConfig cfg)
        {
            var pan = cfg.Pan;  
            var tilt = cfg.Tilt;

            return direction switch
            {
                PtzDirection.Up => (0, +tilt),
                PtzDirection.Down => (0, -tilt),
                PtzDirection.Left => (-pan, 0),
                PtzDirection.Right => (+pan, 0),
                PtzDirection.UpLeft => (-pan, +tilt),
                PtzDirection.UpRight => (+pan, +tilt),
                PtzDirection.DownLeft => (-pan, -tilt),
                PtzDirection.DownRight => (+pan, -tilt),
                _ => (0, 0)
            };
        }

        /// <summary>Override Pan + Tilt nếu FE truyền speed tùy chỉnh.</summary>
        private static PtzSpeedConfig OverridePanTilt(PtzSpeedConfig cfg, decimal? speed)
        {
            if (!speed.HasValue) return cfg;
            var s = Math.Clamp(speed.Value, 0.05m, 1m);
            return CopyConfig(cfg, pan: s, tilt: s);
        }

        /// <summary>Override Zoom   nếu FE truyền speed tùy chỉnh.</summary>
        private static PtzSpeedConfig OverrideZoom(PtzSpeedConfig cfg, decimal? speed)
        {
            if (!speed.HasValue) return cfg;
            var s = Math.Clamp(speed.Value, 0.05m, 1m);
            return CopyConfig(cfg, zoom: s);
        }

        /// <summary>Tạo bản copy config với tùy chọn ghi đè từng trục riêng biệt.</summary>
        private static PtzSpeedConfig CopyConfig(PtzSpeedConfig cfg,decimal? pan = null, decimal? tilt = null, decimal? zoom = null,
            decimal? focus = null) => new()
        {
            Pan = pan ?? cfg.Pan,
            Tilt = tilt ?? cfg.Tilt,
            Zoom = zoom ?? cfg.Zoom,
            Focus = focus ?? cfg.Focus,     // ← thêm
            Speed = cfg.Speed,              // ← nên carry luôn kẻo mất khi override
            ContinuousMoveAutoStopMs = cfg.ContinuousMoveAutoStopMs,
            NudgeDurationMs = cfg.NudgeDurationMs
        };



        private static CctvActionOutput ToOutput(VmsActionOutput result) =>
            new() { Success = result.Success, Message = result.Message };

        private static PtzSpeedConfig OverrideFocus(PtzSpeedConfig cfg, decimal? speed)
        {
            if (!speed.HasValue) return cfg;
            var s = Math.Clamp(speed.Value, 0.05m, 1m);
            return CopyConfig(cfg, focus: s);
        }
    }
}
