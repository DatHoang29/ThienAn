using Module.CCTV.Controllers.CctvMedia.Dto;
using Module.CCTV.Infrastructure.Services.Device;
using Module.CCTV.Infrastructure.Services.Media;
using Module.CCTV.Infrastructure.Services.Proxy;
using Module.CCTV.Infrastructure.Services.Vendor;
using Module.Integration.CCTV.Core.Dto;
using Modules.CCTV.Core.Entities;
using Modules.CCTV.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.Utility.Helpers;
using Wolverine;

// Alias tường minh để tránh xung đột tên với DTO tầng này

namespace Module.CCTV.Controllers.CctvMedia.Queries
{
    /// <summary>
    /// Xử lý logic lấy URL stream:
    /// 1. Tra IP → lấy DeviceId từ DB.
    /// 2. Map sang VmsXxxInput (DTO chung tầng Core).
    /// 3. Gọi BaseCctvService.BuildMediaUrl / BuildWebRtcUrl.
    /// Không import trực tiếp bất cứ gì từ NxVms.Infrastructure.
    /// </summary>
    public class CctvMediaQueryHandler : IWolverineHandler
    {
        private readonly VendorService _vendorService;
        private readonly CctvDeviceResolveService _resolveService;

        private readonly CctvProxyUrlService _proxyUrlService;
        private readonly CctvLiveSourceService _liveSourceService;

        public CctvMediaQueryHandler(VendorService vendorService, CctvDeviceResolveService resolveService, CctvProxyUrlService proxyUrlService, CctvLiveSourceService liveSourceService)
        {
            _vendorService = vendorService;
            _resolveService = resolveService;
            _proxyUrlService = proxyUrlService;
            _liveSourceService = liveSourceService;
        }

        // Đã xóa API GetAuthToken (21/10/2026): trả token tài khoản dịch vụ NxVMS cho FE là lộ thông tin xác thực.
        // URL WebRTC/tải video/snapshot đi qua Cctv Proxy kèm ticket, FE không cần token NxVMS.

        /// <summary>
        /// Trả WSS URL cho FE kết nối WebRTC.
        /// PositionMs null = live, có giá trị = playback.
        /// </summary>
        /// Author: ChuongLP
        /// Date: 2026-05-30
        public async Task<CctvWebRtcStreamOutput> HandleAsync(CctvWebRtcUrlInput input, CancellationToken cancellationToken = default)
        {
            if (!input.FromDate.HasValue)
                return await _liveSourceService.ResolveAsync(input, cancellationToken);

            var nxvms = await _vendorService.GetAsync(input.Vendor);
            // Ưu tiên IP nếu FE truyền, fallback DeviceId. Không tìm thấy → trả Url null (không throw).
            var device = await _resolveService.TryResolveDeviceAsync(input.DeviceId, input.Ip, cancellationToken);

            // Kiểm tra tiếp nếu DB không tìm thấy thiết bị này (device bị null)
            if (device == null)
            {
                return new CctvWebRtcStreamOutput
                {
                    DeviceId = input.DeviceId,
                    Url = null,
                    Mode = input.FromDate.HasValue ? "Playback" : "Live"
                };
            }

            var wssUrl = await nxvms.BuildWebRtcStreamUrlAsync(
                new VmsWebRtcUrlInput
                {
                    DeviceId = device.DeviceId,
                    Stream = input.Stream,
                    Resolution = input.Resolution,
                    FromDate = input.FromDate
                });

            // Không trả URL NxVMS trực tiếp: đổi sang URL Cctv Proxy kèm ticket (kien.le 2026-10-06).
            var proxyUrl = await _proxyUrlService.BuildAsync(wssUrl, device.DeviceId, BaseHelper.CctvStreamTicketHp.KindWebRtc);

            return new CctvWebRtcStreamOutput
            {
                DeviceId = device.DeviceId,
                Url = proxyUrl.Url,
                Mode = input.FromDate.HasValue ? "Playback" : "Live",
                ExpiresAt = proxyUrl.ExpiresAt
            };
        }

        /// <summary>
        /// Trả HTTPS URL để FE tải file video archive.
        /// </summary>
        /// Author: ChuongLP
        /// Date: 2026-05-30
        public async Task<CctvMediaDownloadOutput> HandleAsync(CctvMediaDownloadInput input, CancellationToken cancellationToken = default)
        {
            var nxvms = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, cancellationToken);

            var downloadUrl = await nxvms.BuildMediaDownloadUrlAsync(
                new VmsMediaDownloadInput
                {
                    DeviceId = device.DeviceId,
                    Stream = input.Stream,
                    Resolution = input.Resolution,
                    FromDate = input.FromDate,
                    ToDate = input.ToDate,
                    VideoCodec = input.VideoCodec
                }, cancellationToken);

            // Không trả URL NxVMS trực tiếp: đổi sang URL Cctv Proxy kèm ticket (kien.le 2026-10-06).
            var proxyUrl = await _proxyUrlService.BuildAsync(downloadUrl, device.DeviceId, BaseHelper.CctvStreamTicketHp.KindMedia);

            return new CctvMediaDownloadOutput
            {
                DeviceId = device.DeviceId,
                DownloadUrl = proxyUrl.Url,
                ExpiresAt = proxyUrl.ExpiresAt
            };
        }

        /// <summary>
        /// Chụp ảnh snapshot tại thời điểm chỉ định hoặc frame mới nhất.
        /// Author: ChuongLP
        /// Date: 2026-05-30
        /// </summary>  
        public async Task<CctvSnapshotOutput> HandleAsync(CctvSnapshotInput input, CancellationToken cancellationToken = default)
        {
            var nxvms = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, cancellationToken);

            var (bytes, contentType) = await nxvms.GetSnapshotAsync(new VmsSnapshotInput
            {
                DeviceId = device.DeviceId,
                FromDate = input.FromDate,
                Size = input.Size,
                Format = input.Format,
                Crop = input.Crop,
                Rotation = input.Rotation
            }, cancellationToken);

            var fileTimestamp = input.FromDate.HasValue
                ? input.FromDate.Value.ToString("yyyyMMdd_HHmmss")
                : "latest";

            return new CctvSnapshotOutput
            {
                Data = bytes,
                ContentType = contentType,
                FileName = $"snapshot_{fileTimestamp}.{input.Format.ToLower()}"
            };
        }

        /// <summary>
        /// Trả URL ảnh snapshot qua Cctv Proxy kèm ticket — FE ở mạng public xem camera dạng ảnh định kỳ
        /// (gắn thẳng vào &lt;img src&gt;), proxy dùng chung ảnh vài giây cho nhiều người xem.
        /// Author: kien.le
        /// Date: 2026-10-06
        /// </summary>
        public async Task<CctvSnapshotUrlOutput> HandleAsync(CctvSnapshotUrlInput input, CancellationToken cancellationToken = default)
        {
            var nxvms = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, cancellationToken);

            var snapshotUrl = await nxvms.BuildSnapshotUrlAsync(new VmsSnapshotInput
            {
                DeviceId = device.DeviceId,
                FromDate = input.FromDate,
                Size = input.Size,
                Format = input.Format
            });

            var proxyUrl = await _proxyUrlService.BuildAsync(snapshotUrl, device.DeviceId, BaseHelper.CctvStreamTicketHp.KindImage);

            return new CctvSnapshotUrlOutput
            {
                DeviceId = device.DeviceId,
                Url = proxyUrl.Url,
                ExpiresAt = proxyUrl.ExpiresAt
            };
        }

        /// <summary>
        /// Lấy danh sách footage đã ghi của camera trong khoảng thời gian.
        /// FE truyền DateTime giờ VN → convert sang Unix ms khi gọi NxVMS,
        /// rồi convert ms trả về ngược lại DateTime cho FE dễ hiển thị.
        /// Author: ChuongLP
        /// Date: 2026-06-17
        /// </summary>
        public async Task<CctvFootageOutput> HandleAsync(CctvFootageInput input, CancellationToken cancellationToken = default)
        {
            var nxvms = await _vendorService.GetAsync(input.Vendor);
            var device = await _resolveService.ResolveDeviceAsync(input.DeviceId, input.Ip, cancellationToken);

            var footage = await nxvms.GetFootageAsync(new VmsFootageInput
            {
                DeviceId = device.DeviceId,
                FromDate = input.FromDate,          // ← truyền thẳng
                ToDate = input.ToDate,            // ← truyền thẳng
                DetailLevelMs = input.DetailLevelMs,
                PeriodType = input.PeriodType
            }, cancellationToken);

            var items = footage.Select(x => new CctvFootageItemOutput
            {
                DeviceId = device.DeviceId,
                StartTimeMs = x.StartTimeMs,
                DurationMs = x.DurationMs,
                StartTime = DateTimeOffset.FromUnixTimeMilliseconds(x.StartTimeMs).LocalDateTime,
                EndTime = DateTimeOffset.FromUnixTimeMilliseconds(x.StartTimeMs + x.DurationMs).LocalDateTime
            }).ToList();

            return new CctvFootageOutput
            {
                DeviceId = device.DeviceId,
                Count = items.Count,
                Items = items
            };
        }
    }
}
