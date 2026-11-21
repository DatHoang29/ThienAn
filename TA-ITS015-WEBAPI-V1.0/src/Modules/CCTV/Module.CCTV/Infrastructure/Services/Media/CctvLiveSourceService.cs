using Microsoft.Extensions.Logging;
using Module.CCTV.Controllers.CctvMedia.Dto;
using Module.CCTV.Controllers.CctvGetDevice;
using Module.CCTV.Infrastructure.Services.Device;
using Module.CCTV.Infrastructure.Services.Proxy;
using Module.CCTV.Infrastructure.Services.Vendor;
using Module.Integration.CCTV.Core.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.Utility.Helpers;

namespace Module.CCTV.Infrastructure.Services.Media;

/// <summary>Chọn nguồn live tại BE. Danh sách VMS thực tế quyết định hỗ trợ CCTV;
/// URL dự phòng luôn lấy từ TmsEquipment, không lấy URL proxy/NxVMS đã lưu trong CctvDevice.</summary>
public sealed class CctvLiveSourceService(
    BaseRepository<TmsEquipment> equipmentRepository,
    CctvDeviceResolveService deviceResolver,
    VendorService vendorService,
    CctvProxyUrlService proxyUrlService,
    CctvVmsDeviceCache vmsDeviceCache,
    ILogger<CctvLiveSourceService> logger) : IScoped
{
    public async Task<CctvWebRtcStreamOutput> ResolveAsync(CctvWebRtcUrlInput input, CancellationToken ct = default)
    {
        var ip = NormalizeIp(input.Ip);
        if (string.IsNullOrEmpty(ip) && !string.IsNullOrWhiteSpace(input.DeviceId))
            ip = NormalizeIp((await deviceResolver.TryResolveDeviceAsync(input.DeviceId, null, ct))?.Ip);

        var fallbackUrl = await GetFallbackUrlAsync(ip, ct);
        var result = new CctvWebRtcStreamOutput
        {
            Source = "rtsp",
            Mode = "Live",
            Url = fallbackUrl,
            FallbackUrl = fallbackUrl,
            Reason = "not-registered"
        };

        try
        {
            var vms = await vendorService.GetAsync(input.Vendor);
            // Cache 20s (21/10/2026): không tải toàn bộ danh sách thiết bị NxVMS cho mỗi lượt mở camera.
            var devices = await vmsDeviceCache.GetOrLoadAsync(input.Vendor,
                () => vms.GetDevicesAsync(onlyOnline: false, cancellationToken: ct), ct);
            // Có IP thì không được dùng một DeviceId thuộc IP khác. Không chọn ngẫu nhiên khi trùng IP.
            var requestedIp = NormalizeIp(input.Ip);
            var matches = devices.Where(device => !string.IsNullOrWhiteSpace(device.DeviceId)
                && (requestedIp.Length > 0
                    ? NormalizeIp(device.Ip) == requestedIp
                    : SameDeviceId(device.DeviceId, input.DeviceId))).ToList();
            var device = matches.Count == 1 ? matches[0]
                : matches.FirstOrDefault(candidate => SameDeviceId(candidate.DeviceId, input.DeviceId));
            if (device is null)
            {
                if (matches.Count > 1) result.Reason = "probe-error";
                return result;
            }

            result.DeviceId = device.DeviceId;
            result.DeviceState = DeviceEnum.MapToItsStatus(device.DeviceState);
            if (requestedIp.Length == 0)
            {
                result.FallbackUrl = await GetFallbackUrlAsync(NormalizeIp(device.Ip), ct);
                result.Url = result.FallbackUrl;
            }

            var upstreamUrl = await vms.BuildWebRtcStreamUrlAsync(new VmsWebRtcUrlInput
            {
                DeviceId = device.DeviceId,
                Stream = input.Stream,
                Resolution = input.Resolution,
                FromDate = null
            });
            var proxy = await proxyUrlService.BuildAsync(upstreamUrl, device.DeviceId!, BaseHelper.CctvStreamTicketHp.KindWebRtc);
            result.Source = "cctv";
            result.Url = proxy.Url;
            result.ExpiresAt = proxy.ExpiresAt;
            result.Reason = null;
            return result;
        }
        catch (Exception exception) when (!ct.IsCancellationRequested)
        {
            // Không ghi RTSP, mật khẩu, token hoặc ticket vào log.
            logger.LogWarning("CCTV live source failed: Ip={Ip}, ErrorType={ErrorType}", ip, exception.GetType().Name);
            result.Reason = "stream-unavailable";
            return result;
        }
    }

    private async Task<string?> GetFallbackUrlAsync(string ip, CancellationToken ct)
    {
        if (ip.Length == 0) return null;
        var urls = await equipmentRepository.AsQueryable()
            .Where(equipment => equipment.IsDelete == null && equipment.Ip == ip)
            .Select(equipment => equipment.Url)
            .ToListAsync(ct);
        var fallbacks = urls.Select(url => url?.Trim()).Where(url =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "rtsp" or "rtsps")
            .Distinct(StringComparer.Ordinal).ToArray();
        // Trùng IP nhưng khác kênh/URL: không trả luồng của một thiết bị tùy ý.
        return fallbacks.Length == 1 ? fallbacks[0] : null;
    }

    private static bool SameDeviceId(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right)
        && string.Equals(left.Trim().Trim('{', '}'), right.Trim().Trim('{', '}'), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeIp(string? value)
    {
        var text = value?.Trim() ?? string.Empty;
        if (text.Length == 0) return text;
        return Uri.TryCreate(text.Contains("://") ? text : $"rtsp://{text}", UriKind.Absolute, out var uri)
            ? uri.Host.ToLowerInvariant() : text.ToLowerInvariant();
    }
}
