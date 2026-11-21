using Module.CCTV.Infrastructure.Services.Device;
using Modules.CCTV.Core.Exceptions;
using Shared.Core.Utilities.Constants;
using Shared.Utility.Helpers;
using Shared.Utility.Utilities;
using Microsoft.Extensions.Logging;

namespace Module.CCTV.Infrastructure.Services.Proxy
{
    /// <summary>
    /// Author: kien.le
    /// Description: Đổi URL NxVMS (vendor build) sang URL Cctv Proxy Service kèm ticket có hạn,
    /// để FE xem/tải video, xem ảnh snapshot qua proxy mà không cần token/cookie NxVMS.
    /// Proxy mở path giống hệt NxVMS nên chỉ cần thay origin + gắn ticket.
    /// Cấu hình trong SysOpConfig (nhóm cctv_config):
    /// - cctv_proxy_base_url          : origin proxy, vd https://its-host:7101
    /// - cctv_proxy_ticket_secret     : secret ký ticket (trùng với proxy — proxy đọc cùng bảng)
    /// - cctv_proxy_ticket_ttl_seconds: hạn ticket (giây), mặc định 14400. Ticket chỉ kiểm tra lúc mở kết nối.
    /// Created date: 06/10/2026
    /// </summary>
    public class CctvProxyUrlService : IScoped
    {
        public const string CodeProxyBaseUrl = "cctv_proxy_base_url";
        public const string CodeTicketSecret = "cctv_proxy_ticket_secret";
        public const string CodeTicketTtlSeconds = "cctv_proxy_ticket_ttl_seconds";

        private const int DefaultTicketTtlSeconds = 4 * 60 * 60;

        private readonly DeviceService _configService;
        private readonly ILogger<CctvProxyUrlService> _logger;

        public CctvProxyUrlService(DeviceService configService, ILogger<CctvProxyUrlService> logger)
        {
            _configService = configService;
            _logger = logger;
        }

        /// <summary>
        /// Author: kien.le
        /// Description: Đổi <paramref name="upstreamUrl"/> (URL NxVMS) sang URL proxy kèm ticket.
        /// Created date: 06/10/2026
        /// </summary>
        /// <param name="upstreamUrl">URL NxVMS do vendor build (chỉ dùng path + query).</param>
        /// <param name="deviceId">DeviceId NxVMS mà ticket được phép truy cập.</param>
        /// <param name="kind"><see cref="BaseHelper.CctvStreamTicketHp.KindWebRtc"/> → wss/ws; <see cref="BaseHelper.CctvStreamTicketHp.KindMedia"/> / <see cref="BaseHelper.CctvStreamTicketHp.KindImage"/> → https/http.</param>
        /// <returns>URL proxy kèm ticket và hạn ticket (giờ server).</returns>
        public async Task<CctvProxyUrl> BuildAsync(string upstreamUrl, string deviceId, string kind)
        {
            var configs = await _configService.GetGroupAsync();

            configs.TryGetValue(CodeProxyBaseUrl, out var proxyBaseUrl);
            configs.TryGetValue(CodeTicketSecret, out var secret);
            var missing = new[] { CodeProxyBaseUrl, CodeTicketSecret }
                .Where(code => !configs.TryGetValue(code, out var value) || string.IsNullOrWhiteSpace(value))
                .ToArray();
            if (missing.Length > 0)
            {
                _logger.LogError("CCTV URL denied: DeviceId={DeviceId} Kind={Kind} Group=cctv_config MissingKeys={MissingKeys}. No proxy connection attempted. TraceId={TraceId}",
                    deviceId, kind, string.Join(",", missing), System.Diagnostics.Activity.Current?.TraceId.ToString());
                throw Oops.Oh(BaseMsg.CctvMedia.Exception.ProxyNotConfigured, string.Join(", ", missing));
            }

            var ttlSeconds = configs.TryGetValue(CodeTicketTtlSeconds, out var ttlValue)
                             && int.TryParse(ttlValue, out var ttl) && ttl > 0
                ? ttl
                : DefaultTicketTtlSeconds;

            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(ttlSeconds);
            var ticket = BaseHelper.CctvStreamTicketHp.Create(secret!.Trim(), new BaseHelper.CctvStreamTicketHp.CctvStreamTicketPayload
            {
                DeviceId = deviceId,
                Kind = kind,
                UserId = App.User?.FindFirst(ClaimConst.UserId)?.Value,
                ExpiresAtUnix = expiresAt.ToUnixTimeSeconds()
            });

            var upstream = new Uri(upstreamUrl);
            var builder = new UriBuilder(proxyBaseUrl!.Trim().TrimEnd('/'));
            builder.Path = builder.Path.TrimEnd('/') + upstream.AbsolutePath;

            var query = upstream.Query.TrimStart('?');
            var ticketParam = $"{BaseHelper.CctvStreamTicketHp.QueryName}={Uri.EscapeDataString(ticket)}";
            builder.Query = string.IsNullOrEmpty(query) ? ticketParam : $"{query}&{ticketParam}";

            if (kind == BaseHelper.CctvStreamTicketHp.KindWebRtc)
                builder.Scheme = builder.Scheme == Uri.UriSchemeHttps ? "wss" : "ws";

            // Hạn ticket trả FE theo giờ server (quy ước BaseUtil.TimeServer của dự án).
            var expiresAtServer = BaseUtil.TimeServer.GetTimeServer(expiresAt.UtcDateTime) ?? expiresAt.LocalDateTime;
            _logger.LogInformation("CCTV URL issued: DeviceId={DeviceId} Kind={Kind} Endpoint={Endpoint} ExpiresAtUtc={ExpiresAtUtc} TraceId={TraceId}",
                deviceId, kind, builder.Uri.GetLeftPart(UriPartial.Path), expiresAt, System.Diagnostics.Activity.Current?.TraceId.ToString());
            return new CctvProxyUrl(builder.Uri.ToString(), expiresAtServer);
        }
    }

    public record CctvProxyUrl(string Url, DateTime ExpiresAt);
}
