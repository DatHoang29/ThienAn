using Modules.CCTV.Core.Entities;
using Modules.CCTV.Core.Exceptions;
using Modules.TMS.Infrastructure;

namespace Module.CCTV.Infrastructure.Services.Device
{
    /// <summary>
    /// Điểm resolve tập trung: từ (DeviceId | Ip) do FE truyền lên → tra DB lấy CctvDevice.
    ///
    /// Bối cảnh:
    ///   - Toàn hệ thống ITS định danh thiết bị bằng IP → FE có thể chỉ có IP.
    ///   - Riêng NxVMS lại điều khiển bằng DeviceId (GUID).
    ///   → Vì vậy dù FE gửi IP, BE vẫn phải quy đổi ra DeviceId để gọi tầng NxVMS.
    ///
    /// Quy tắc ưu tiên:
    ///   1. Có IP        → tra theo IP (đúng yêu cầu "xử lý bằng IP nếu có").
    ///   2. Không có IP  → tra theo DeviceId.
    ///   3. Không có cả hai → throw.
    ///
    /// Mọi handler (Media, PTZ) chỉ cần gọi ResolveDeviceAsync(deviceId, ip)
    /// rồi dùng device.DeviceId cho tầng NxVMS bên dưới.
    ///
    /// Author: ChuongLP
    /// Date: 2026-07-28
    /// </summary>
    public class CctvDeviceResolveService : IScoped
    {
        private readonly BaseRepository<CctvDevice> _cctvDeviceRep;

        public CctvDeviceResolveService(BaseRepository<CctvDevice> cctvDeviceRep)
        {
            _cctvDeviceRep = cctvDeviceRep;
        }

        /// <summary>
        /// Resolve CctvDevice theo IP (ưu tiên) hoặc DeviceId.
        /// Không tìm thấy → throw. Đảm bảo device trả về luôn có DeviceId hợp lệ để gọi NxVMS.
        /// </summary>
        /// <param name="deviceId">DeviceId (GUID NxVMS) FE truyền lên (có thể null nếu dùng IP).</param>
        /// <param name="ip">IP thiết bị FE truyền lên (ưu tiên hơn DeviceId nếu có).</param>
        public async Task<CctvDevice> ResolveDeviceAsync(string? deviceId, string? ip, CancellationToken ct = default)
        {
            var device = await ResolveInternalAsync(deviceId, ip, throwIfNotFound: true, ct);

            // NxVMS bắt buộc phải có DeviceId để điều khiển tầng dưới.
            if (string.IsNullOrWhiteSpace(device!.DeviceId))
                throw Oops.Oh(BaseMsg.CctvDevice.Exception.MissingVmsDeviceId, ip ?? deviceId ?? string.Empty);

            return device;
        }

        /// <summary>
        /// Giống ResolveDeviceAsync nhưng KHÔNG throw khi không tìm thấy thiết bị (trả null).
        /// Dùng cho luồng cần xử lý mềm khi thiếu thiết bị (vd: WebRTC trả Url = null thay vì lỗi).
        /// Vẫn throw khi FE không truyền gì hoặc IP trùng nhiều thiết bị.
        /// </summary>
        public Task<CctvDevice?> TryResolveDeviceAsync(string? deviceId, string? ip, CancellationToken ct = default)
            => ResolveInternalAsync(deviceId, ip, throwIfNotFound: false, ct);

        private async Task<CctvDevice?> ResolveInternalAsync(string? deviceId, string? ip, bool throwIfNotFound, CancellationToken ct)
        {
            var hasIp = !string.IsNullOrWhiteSpace(ip);
            var hasDeviceId = !string.IsNullOrWhiteSpace(deviceId);

            // Guard: FE không truyền gì để định danh.
            if (!hasIp && !hasDeviceId)
                throw Oops.Oh(BaseMsg.CctvDevice.Exception.MissingDeviceIdOrIp);

            if (hasIp)
            {
                // Ưu tiên IP: toàn hệ thống dùng IP để định danh thiết bị.
                var matches = await _cctvDeviceRep.AsQueryable()
                    .Where(x => x.IsDelete == null && x.Ip == ip)
                    .ToListAsync(ct);

                if (matches.Count == 0)
                    return throwIfNotFound ? throw Oops.Oh(BaseMsg.CctvDevice.Exception.NotFoundByIp, ip!) : null;

                // Nhiều camera trùng IP: nếu FE có gửi kèm DeviceId thì lọc thêm cho chính xác,
                // ngược lại throw để tránh điều khiển nhầm thiết bị.
                if (matches.Count > 1)
                {
                    if (hasDeviceId)
                    {
                        var matched = matches.FirstOrDefault(x => x.DeviceId == deviceId);
                        if (matched == null)
                            return throwIfNotFound ? throw Oops.Oh(BaseMsg.CctvDevice.Exception.NotFoundByDeviceId, deviceId!) : null;
                        return matched;
                    }

                    throw Oops.Oh(BaseMsg.CctvDevice.Exception.MultipleByIp, ip!);
                }

                return matches[0];
            }

            // Không có IP → tra theo DeviceId.
            var device = await _cctvDeviceRep.GetFirstAsync(x => x.IsDelete == null && x.DeviceId == deviceId);

            if (device == null)
                return throwIfNotFound ? throw Oops.Oh(BaseMsg.CctvDevice.Exception.NotFoundByDeviceId, deviceId!) : null;

            return device;
        }
    }
}
