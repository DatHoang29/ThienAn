using Module.CCTV.Controllers.CctvPTZ.Dto;
using Module.CCTV.Infrastructure.Services.Device;
using Module.CCTV.Infrastructure.Services.Vendor;
using Module.Integration.CCTV.Core.Dto;
using Modules.CCTV.Core.Entities;
using Modules.TMS.Infrastructure;
using Wolverine;

namespace Module.CCTV.Controllers.CctvPtz.Queries
{
    /// <summary>
    /// Chỉ xử lý các truy vấn READ-ONLY của PTZ.
    /// Không có side-effect, không gọi Stop/Move/Zoom.
    ///
    /// Các lệnh điều khiển (Move, Nudge, Zoom, Stop, Preset, Tour)
    /// đã được chuyển sang CctvPTZCommandHandler.
    ///
    /// FE có thể truyền IP thay cho DeviceId. Resolver quy đổi ra DeviceId
    /// để gọi tầng NxVMS bên dưới.
    ///
    /// Author: ChuongLP
    /// Date: 2026-06-13
    /// </summary>
    public class CctvPtzQueryHandler : IWolverineHandler
    {
        private readonly VendorService _vendorService;
        private readonly DeviceService _deviceService;
        private readonly CctvDeviceResolveService _resolveService;

        public CctvPtzQueryHandler(
            VendorService vendorService,
            DeviceService deviceService,
            CctvDeviceResolveService resolveService)
        {
            _vendorService = vendorService;
            _deviceService = deviceService;
            _resolveService = resolveService;
        }

        // ── Lấy vị trí PTZ hiện tại ──────────────────────────────────────────────
        /// <summary>
        /// GET — đọc pan/tilt/zoom/focus hiện tại của camera.
        /// Author: ChuongLP
        /// Date: 2026-06-10
        /// </summary>
        public async Task<CctvPtzPositionOutput> HandleAsync(CctvGetPtzPositionInput input, CancellationToken ct = default)
        {
            var service = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, ct);
            var result = await service.GetPtzPositionAsync(device.DeviceId, ct);

            return new CctvPtzPositionOutput
            {
                Pan = result.Pan,
                Tilt = result.Tilt,
                Zoom = result.Zoom,
                Focus = result.Focus,
            };
        }

        // ── Lấy giới hạn PTZ ─────────────────────────────────────────────────────
        /// <summary>
        /// GET — đọc min/max của pan, tilt, fov, focus, panSpeed, tiltSpeed, zoomSpeed.
        /// Author: ChuongLP
        /// Date: 2026-06-10
        /// </summary>
        public async Task<CctvPtzLimitsOutput> HandleAsync(CctvGetPtzLimitsInput input, CancellationToken ct = default)
        {
            var service = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, ct);
            var result = await service.GetPtzLimitsAsync(device.DeviceId, ct);

            return new CctvPtzLimitsOutput
            {
                MinPan = result.MinPan,
                MaxPan = result.MaxPan,
                MinTilt = result.MinTilt,
                MaxTilt = result.MaxTilt,
                MinFov = result.MinFov,
                MaxFov = result.MaxFov,
                MinFocus = result.MinFocus,
                MaxFocus = result.MaxFocus,
                MinPanSpeed = result.MinPanSpeed,
                MaxPanSpeed = result.MaxPanSpeed,
                MinTiltSpeed = result.MinTiltSpeed,
                MaxTiltSpeed = result.MaxTiltSpeed,
                MinZoomSpeed = result.MinZoomSpeed,
                MaxZoomSpeed = result.MaxZoomSpeed,
            };
        }

        // ── Lấy danh sách Preset ─────────────────────────────────────────────────
        /// <summary>
        /// GET — danh sách preset đã lưu trên camera.
        /// FIX: tên DTO sửa từ CctvGetPresetInput → CctvGetPresetsInput cho khớp controller.
        /// Author: ChuongLP
        /// Date: 2026-06-10
        /// </summary>
        public async Task<List<CctvPtzPresetOutput>> HandleAsync(CctvGetPresetInput input, CancellationToken ct = default)
        {
            var service = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, ct);
            var result = await service.GetPresetsAsync(device.DeviceId, ct);

            return result.Select(x => new CctvPtzPresetOutput
            {
                PresetId = x.PresetId,
                Name = x.Name
            }).ToList();
        }

        // ── Lấy danh sách Tour ───────────────────────────────────────────────────
        /// <summary>
        /// GET — danh sách tour đã cấu hình trên camera.
        /// Author: ChuongLP
        /// Date: 2026-06-10
        /// </summary>
        public async Task<List<CctvPtzTourOutput>> HandleAsync(CctvGetToursInput input, CancellationToken ct = default)
        {
            var service = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, ct);
            var result = await service.GetToursAsync(device.DeviceId, ct);

            return result.Select(x => new CctvPtzTourOutput
            {
                TourId = x.TourId,
                Name = x.Name
            }).ToList();
        }
    }
}
