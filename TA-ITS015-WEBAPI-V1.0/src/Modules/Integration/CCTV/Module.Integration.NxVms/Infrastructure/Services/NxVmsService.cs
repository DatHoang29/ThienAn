using Furion.FriendlyException;
using Module.Integration.CCTV.Core.Abstracts;
using Module.Integration.CCTV.Core.Dto;
using Module.Integration.NxVms.Infrastructure.Dto;
using Shared.Utility.Utilities;

namespace Module.Integration.CCTV.Infrastructure.Services
{
    /// <summary>
    /// Triển khai cụ thể cho hãng NxVMS.
    /// Nhận DTO chung (VmsXxx) từ BaseCctvService, map sang DTO nội bộ (NxVmsXxx),
    /// rồi gọi NxVmsApiService để thực hiện HTTP call.
    /// Quy tắc: class này KHÔNG được import bất cứ gì từ tầng CCTV controller/handler.
    /// Author: ChuongLP
    /// Date: 2026-05-29
    /// </summary>
    public class NxVmsService : BaseCctvService
    {
        private readonly NxVmsApiService _apiService;

        public NxVmsService(NxVmsApiService apiService)
        {
            _apiService = apiService;
        }

        // ── Device ────────────────────────────────────────────────────────────────
        /// <summary>
        /// Lấy danh sách thiết bị từ NxVMS. Nếu onlyOnline=true thì chỉ lấy thiết bị có trạng thái "Online".
        /// </summary>
        /// Author: ChuongLP
        /// Date: 2026-05-29
        public override async Task<List<VmsDeviceOutput>> GetDevicesAsync(bool onlyOnline = false, CancellationToken cancellationToken = default)
        {
            var devices = await _apiService.GetDevicesAsync(cancellationToken);

            return devices.Select(x => new VmsDeviceOutput
            {
                DeviceId = x.DeviceId,
                Name = x.Name,
                Ip = x.Ip,
                Url = x.Url,
                DeviceState = x.DeviceState.ToString()
            }).ToList();
        }

        /// <summary>
        /// Lấy token để xác thực khi gọi API NxVMS. Token này do NxVmsAuthService quản lý, có cơ chế cache và tự động refresh khi gần hết hạn.
        /// Author: ChuongLP
        /// Date: 2026-05-29
        /// </summary>
        public override Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            return _apiService.GetTokenAsync(cancellationToken);
        }

        #region Media
        // ── Media URL ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Lây URL stream/download video từ NxVMS. Nếu input.Download=true thì trả về URL download, ngược lại trả về URL xem trực tiếp.
        /// Author: ChuongLP
        /// Date: 2026-05-29
        /// </summary>
        public override async Task<string> BuildMediaUrlAsync(VmsMediaUrlInput input, CancellationToken cancellationToken = default)
        {
            var positionMs = ToUnixMs(input.FromDate.Value);
            var durationMs = ToUnixMs(input.ToDate.Value) - positionMs;
            return await _apiService.BuildMediaUrl(new NxVmsMediaUrlInput
            {
                DeviceId = input.DeviceId,
                Stream = input.Stream,
                Resolution = input.Resolution,
                PositionMs = positionMs,
                DurationMs = durationMs,
                Download = input.Download
            });
        }

        /// <summary>
        /// Xây dựng URL WebRTC stream từ NxVMS. Trả về URL xem trực tiếp.
        /// Author: ChuongLP
        /// Date: 2026-05-29
        /// </summary>
        public override async Task<string> BuildWebRtcStreamUrlAsync(VmsWebRtcUrlInput input)
        {
            return await _apiService.BuildWebRtcStreamUrlAsync(
                new NxVmsWebRtcUrlInput
                {
                    DeviceId = input.DeviceId,
                    Stream = input.Stream,
                    Resolution = input.Resolution,
                    PositionMs = input.FromDate.HasValue ? ToUnixMs(input.FromDate.Value) : null
                });
        }

        /// <summary>
        /// Xây dựng URL download video từ NxVMS. Trả về URL download.
        /// Author: ChuongLP
        /// Date: 2026-05-29
        /// </summary>
        public override async Task<string> BuildMediaDownloadUrlAsync(VmsMediaDownloadInput input, CancellationToken cancellationToken = default)
        {

            var positionMs = ToUnixMs(input.FromDate);
            var durationMs = ToUnixMs(input.ToDate) - positionMs;

            return await _apiService.BuildMediaDownloadUrlAsync(
                new NxVmsMediaDownloadInput
                {
                    DeviceId = input.DeviceId,
                    Stream = input.Stream,
                    Resolution = input.Resolution,
                    PositionMs = positionMs,
                    DurationMs = durationMs,
                    VideoCodec = input.VideoCodec
                }, cancellationToken);
        }

        /// <summary>
        /// Lấy snapshot ảnh từ NxVMS. Trả về byte[] data và content-type của ảnh.
        /// Author: ChuongLP
        /// Date: 2026-05-29
        /// </summary>
        public override async Task<(byte[] Data, string ContentType)> GetSnapshotAsync(VmsSnapshotInput input, CancellationToken cancellationToken = default)
        {
            return await _apiService.GetSnapshotAsync(new NxVmsSnapshotInput
            {
                DeviceId = input.DeviceId,
                TimestampMs = input.FromDate.HasValue ? ToUnixMs(input.FromDate.Value) : null,
                Size = input.Size,
                Format = input.Format,
                Crop = input.Crop,
                Rotation = input.Rotation
            }, cancellationToken);
        }

        /// <summary>
        /// Build URL ảnh snapshot NxVMS (/rest/v4/devices/{id}/image), cùng tham số với GetSnapshotAsync.
        /// Author: kien.le
        /// Date: 2026-10-06
        /// </summary>
        public override Task<string> BuildSnapshotUrlAsync(VmsSnapshotInput input)
        {
            return _apiService.BuildSnapshotUrlAsync(new NxVmsSnapshotInput
            {
                DeviceId = input.DeviceId,
                TimestampMs = input.FromDate.HasValue ? ToUnixMs(input.FromDate.Value) : null,
                Size = input.Size,
                Format = input.Format,
                Crop = input.Crop,
                Rotation = input.Rotation
            });
        }


        /// <summary>
        /// Lấy footage từ NxVMS rồi map sang DTO chung.
        /// Author: ChuongLP
        /// Date: 2026-06-17
        /// </summary>
        public override async Task<List<VmsFootageOutput>> GetFootageAsync(VmsFootageInput input, CancellationToken cancellationToken = default)
        {
            var result = await _apiService.GetFootageAsync(new NxVmsFootageInput
            {
                DeviceId = input.DeviceId,
                StartTimeMs = input.FromDate.HasValue ? ToUnixMs(input.FromDate.Value) : null,
                EndTimeMs = input.ToDate.HasValue ? ToUnixMs(input.ToDate.Value) : null,
                DetailLevelMs = input.DetailLevelMs,
                PeriodType = input.PeriodType
            }, cancellationToken);

            return (result ?? new())
                .Select(x => new VmsFootageOutput
                {
                    DeviceId = x.DeviceId,
                    StartTimeMs = x.StartTimeMs,
                    DurationMs = x.DurationMs
                }).ToList();
        }
        #endregion

        #region PTZ
        // ── PTZ ───────────────────────────────────────────────────────────────────
        /// <summary>
        /// Di chuyển PTZ đến vị trí mới (pan, tilt, fov) từ NxVMS. Author: ChuongLP. Date: 2026-05-29
        /// </summary>
        public override async Task<VmsActionOutput> MovePtzPositionAsync(VmsPtzPositionInput input, CancellationToken cancellationToken = default)
        {
            await _apiService.PostAsync($"/rest/v4/devices/{input.DeviceId}/ptz/positions",
                new { pan = input.Pan, tilt = input.Tilt, fov = input.Zoom },
                cancellationToken);

            return Ok("");
        }

        /// <summary>
        /// Di chuyển PTZ với tốc độ (panSpeed, tiltSpeed, zoomSpeed) từ NxVMS.
        /// Author: ChuongLP. 
        /// Date: 2026-05-29
        /// </summary>
        public override async Task<VmsActionOutput> MovePtzAsync(VmsPtzMoveInput input, CancellationToken cancellationToken = default) 
        {
            var body = new Dictionary<string, object>();

            if (input.Pan != 0) body["pan"] = input.Pan;
            if (input.Tilt != 0) body["tilt"] = input.Tilt;
            if (input.Zoom != 0) body["zoom"] = input.Zoom;
            if (input.Focus != 0) body["focus"] = input.Focus;

            if (body.Count == 0)
                return Ok("Không có trục nào cần di chuyển (tất cả tốc độ = 0).");

            await _apiService.PostAsync($"/rest/v4/devices/{input.DeviceId}/ptz/move", body, cancellationToken);
            //await _apiService.PostAsync($"/rest/v4/devices/{input.DeviceId}/ptz/move", new { pan = input.PanSpeed, tilt = input.TiltSpeed, zoom = input.ZoomSpeed }, cancellationToken);
            return Ok("PTZ bắt đầu di chuyển.");
        }

        /// <summary>
        /// Dừng di chuyển PTZ từ NxVMS.
        /// Author: ChuongLP. 
        /// Date: 2026-05-29
        /// </summary>
        public override async Task<VmsActionOutput> StopPtzAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            await _apiService.PostAsync($"/rest/v4/devices/{deviceId}/ptz/move", new { pan = 0, tilt = 0, zoom = 0, focus = 0 }, cancellationToken);
            return Ok("PTZ dừng thành công.");
        }

        /// <summary>
        /// Kích hoạt preset PTZ từ NxVMS. 
        /// Author: ChuongLP. 
        /// Date: 2026-05-29
        /// </summary>
        public override async Task<VmsActionOutput> ActivatePresetAsync(VmsActivatePresetInput input, CancellationToken cancellationToken = default)
        {
            // PresetId do FE truyền → escape trước khi ghép vào path NxVMS (chặn "../", "?", "#"...).
            await _apiService.PostAsync($"/rest/v4/devices/{input.DeviceId}/ptz/presets/{Uri.EscapeDataString(input.PresetId ?? string.Empty)}/activate", new { speed = input.Speed }, cancellationToken);
            return Ok($"Kích hoạt preset [{input.PresetId}] thành công.");
        }

        /// <summary>
        /// Kích hoạt tour PTZ từ NxVMS. 
        /// Author: ChuongLP.
        /// Date: 2026-05-29
        /// </summary>
        public override async Task<VmsActionOutput> ActivateTourAsync(VmsActivateTourInput input, CancellationToken cancellationToken = default)
        {
            await _apiService.PostAsync($"/rest/v4/devices/{input.DeviceId}/ptz/tours/{Uri.EscapeDataString(input.TourId ?? string.Empty)}/activate", new { }, cancellationToken);
            return Ok($"Kích hoạt tour [{input.TourId}] thành công.");
        }

        /// <summary>
        /// Tạo preset PTZ mới lưu vị trí hiện tại từ NxVMS.
        /// Lấy presetId từ response để handler ghi vào Config TmsEquipment.
        /// Author: ChuongLP
        /// Date: 2026-06-13
        /// </summary>
        public override async Task<VmsPresetAddOutput> AddPresetAndResolveIdAsync(NxVmsAddPresetInput input, CancellationToken cancellationToken = default)
        {
            // 1. Chụp TRƯỚC: tập id đang có.
            var before = await _apiService.GetPresetsAsync(input.DeviceId, cancellationToken);
            var idsBefore = (before ?? new())
                .Select(x => x.PresetId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet();

            // 2. Add preset (chỉ ra lệnh tạo).
            await _apiService.AddPresetAsync(new NxVmsAddPresetInput
            {
                DeviceId = input.DeviceId,
                PresetName = input.PresetName
            }, cancellationToken);

            // 3. Chụp SAU.
            var after = await _apiService.GetPresetsAsync(input.DeviceId, cancellationToken);
            var idsAfter = (after ?? new())
                .Select(x => x.PresetId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();

            // 4. Lấy id mới xuất hiện.
            var newIds = idsAfter.Where(id => !idsBefore.Contains(id)).ToList();

            // Chặn biên: phải đúng 1 id mới.
            if (newIds.Count == 0)
                return new VmsPresetAddOutput
                {
                    Success = false,
                    Message = "Đã gọi tạo preset nhưng danh sách không có preset mới " +
                              "(camera có thể đã đầy slot hoặc ghi đè). Vui lòng kiểm tra lại."
                };

            if (newIds.Count > 1)
                return new VmsPresetAddOutput
                {
                    Success = false,
                    Message = $"preset vừa tạo (có thể do thao tác đồng thời). Vui lòng thử lại."
                };

            // OK: đúng 1 id mới.
            return new VmsPresetAddOutput
            {
                Success = true,
                PresetId = newIds[0],
                Name = input.PresetName,   // tên user nhập, không lấy từ camera
                Message = $"Đã lưu preset [{input.PresetName}]"
                //Message = $"Đã lưu preset [{input.PresetName}] (ID: {newIds[0]})."
            };
        }

        /// <summary>
        /// Đổi tên preset PTZ từ NxVMS.
        /// Author: ChuongLP
        /// Date: 2026-06-13
        /// </summary>
        public override async Task<VmsActionOutput> UpdatePresetAsync(NxVmsUpdatePresetInput input, CancellationToken cancellationToken = default)
        {
            await _apiService.UpdatePresetAsync(new NxVmsUpdatePresetInput
            {
                DeviceId = input.DeviceId,
                PresetId = input.PresetId,
                Name = input.Name
            }, cancellationToken);

            return Ok($"Đã đổi tên preset [{input.PresetId}] → [{input.Name}].");
        }

        /// <summary>
        /// Lấy thông số PTZ hiện tại từ NxVMS. 
        /// Author: ChuongLP.
        /// Date: 2026-05-29
        /// </summary>
        public override async Task<VmsPtzPositionOutput> GetPtzPositionAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            var result = await _apiService.GetPtzPositionAsync(deviceId, cancellationToken);
            if (result == null)
                throw Oops.Oh($"104, Không lấy được vị trí PTZ của thiết bị [{deviceId}].");

            return new VmsPtzPositionOutput
            {
                Pan = result.Pan,
                Tilt = result.Tilt,
                Focus = result.Focus,
                Zoom = result.Zoom
            };
        }

        /// <summary>
        /// Lấy thông số giới hạn PTZ (pan, tilt, zoom, focus + speed) từ NxVMS.
        /// NxVMS trả về object lồng nhau { "pan": { "min": -1, "max": 1 } }
        /// nên phải đọc qua .Min / .Max thay vì flat field.
        /// Author: ChuongLP.
        /// Date: 2026-06-10
        /// </summary>
        public override async Task<VmsPtzLimitsOutput> GetPtzLimitsAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            var result = await _apiService.GetPtzLimitsAsync(deviceId, cancellationToken);
            if (result == null)
                throw Oops.Oh($"105, Không lấy được giới hạn PTZ của thiết bị [{deviceId}].");

            return new VmsPtzLimitsOutput
            {
                // ── Absolute position ──────────────────────────────────────────
                MinPan = result.Pan?.Min ?? -1m,
                MaxPan = result.Pan?.Max ?? 1m,
                MinTilt = result.Tilt?.Min ?? -1m,
                MaxTilt = result.Tilt?.Max ?? 1m,
                MinFov = result.Fov?.Min ?? 0m,
                MaxFov = result.Fov?.Max ?? 1m,
                MinFocus = result.Focus?.Min ?? 0m,
                MaxFocus = result.Focus?.Max ?? 1m,

                // ── Continuous speed ───────────────────────────────────────────
                MinPanSpeed = result.PanSpeed?.Min ?? -1m,
                MaxPanSpeed = result.PanSpeed?.Max ?? 1m,
                MinTiltSpeed = result.TiltSpeed?.Min ?? -1m,
                MaxTiltSpeed = result.TiltSpeed?.Max ?? 1m,
                MinZoomSpeed = result.ZoomSpeed?.Min ?? -1m,
                MaxZoomSpeed = result.ZoomSpeed?.Max ?? 1m,
            };
        }

        /// <summary>
        /// Lấy thông số preset PTZ từ NxVMS.
        /// Author: ChuongLP.
        /// Date: 2026-05-29
        /// </summary>
        public override async Task<List<VmsPtzPresetOutput>> GetPresetsAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            var result = await _apiService.GetPresetsAsync(deviceId, cancellationToken);
            return (result ?? new())
                .Select(x => new VmsPtzPresetOutput
                {
                    PresetId = x.PresetId,
                    Name = x.Name
                }).ToList();
        }


        /// <summary>
        /// Xóa thông số preset PTZ từ NxVMS.
        /// Author: ChuongLP.
        /// Date: 2026-05-29
        /// </summary>
        public override async Task<VmsActionOutput> DeletePresetAsync(VmsDeletePresetInput input, CancellationToken cancellationToken = default)
        {
            await _apiService.DeletePresetAsync(input.DeviceId, input.PresetId, cancellationToken);
            return Ok($"Đã xóa preset [{input.PresetId}].");
        }

        /// <summary>
        /// Lấy thông số tour PTZ từ NxVMS.
        /// Author: ChuongLP.
        /// Date: 2026-05-29
        /// </summary>
        public override async Task<List<VmsPtzTourOutput>> GetToursAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            var result = await _apiService.GetToursAsync(deviceId, cancellationToken);
            return (result ?? new())
                .Select(x => new VmsPtzTourOutput
                {
                    TourId = x.TourId,
                    Name = x.Name
                }).ToList();
        }

        #endregion

        // ── Helper ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Convert DateTime → Unix timestamp ms để truyền vào NxVMS API.
        /// Dùng TimeServerUtil để đảm bảo đúng timezone VN.
        /// </summary>
        private static long ToUnixMs(DateTime dt)
        {
            var local = TimeServerUtil.GetTimeServer(dt) ?? dt;
            return new DateTimeOffset(local).ToUnixTimeMilliseconds();
        }


        private static VmsActionOutput Ok(string message)
        {
            return new() { Success = true, Message = message };
        }
    }
}
