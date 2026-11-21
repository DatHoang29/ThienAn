using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Services.CctvProxy.Configuration;
using Services.CctvProxy.Features.Proxy;
using Services.CctvProxy.Features.PublicSnapshot;
using Services.Shared.Hosting;
using Shared.Utility.Helpers;

namespace Services.CctvProxy.Endpoints;

/// <summary>
/// Author: kien.le
/// Description: Map các endpoint proxy. Path GIỐNG HỆT NxVMS (chỉ mở theo allowlist, không pass-through
/// toàn bộ /rest/*) để WebAPI chỉ cần thay origin NxVMS bằng origin proxy + gắn ticket.
/// - WS  /rest/v4/devices/{deviceId}/webrtc : WebRTC signaling (live + playback)
/// - GET /rest/v4/devices/{deviceId}/media  : tải video archive
/// - GET /rest/v4/devices/{deviceId}/image  : ảnh snapshot (xem dạng ảnh định kỳ ở mạng public)
/// - GET /public/equipments/{equipmentId}/snapshot.jpg : ảnh công khai cho web người dân (không ticket)
/// - GET /health                            : kiểm tra sống
/// Created date: 06/10/2026
/// </summary>
public static class CctvProxyEndpoints
{
    /// <summary>CORS mở cho mọi origin — ảnh công khai, web người dân có thể ở domain bất kỳ.</summary>
    public const string PublicCorsPolicy = "public-snapshot";

    /// <summary>Giới hạn số request theo IP cho endpoint công khai (Service:Public:RateLimitPerMinute).</summary>
    public const string PublicRateLimitPolicy = "public-snapshot";

    /// <summary>Map toàn bộ endpoint proxy (xem danh sách ở summary của class).</summary>
    /// <param name="app">Route builder của host Worker.</param>
    /// <returns>Chính <paramref name="app"/> để gọi nối tiếp.</returns>
    public static IEndpointRouteBuilder MapCctvProxyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", (IConfigManager config, IServiceManager manager) =>
            Results.Ok(new { ok = manager.State == ServiceManagerState.Running, service = config.Current.Name, state = manager.State.ToString() }));

        app.Map("/rest/v4/devices/{deviceId}/webrtc", async (
            HttpContext context,
            string deviceId,
            StreamTicketValidator validator,
            WebRtcSignalingProxy proxy,
            IConfigManager config) =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (!IsOriginAllowed(context, config))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            if (!validator.TryValidate(context, deviceId, BaseHelper.CctvStreamTicketHp.KindWebRtc, out _))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await proxy.HandleAsync(context, deviceId);
        });

        app.MapGet("/rest/v4/devices/{deviceId}/media", async (
            HttpContext context,
            string deviceId,
            StreamTicketValidator validator,
            MediaHttpProxy proxy) =>
        {
            if (!validator.TryValidate(context, deviceId, BaseHelper.CctvStreamTicketHp.KindMedia, out var error))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync(error ?? "Unauthorized");
                return;
            }

            await proxy.HandleAsync(context, deviceId);
        });

        app.MapGet("/rest/v4/devices/{deviceId}/image", async (
            HttpContext context,
            string deviceId,
            StreamTicketValidator validator,
            SnapshotHttpProxy proxy) =>
        {
            if (!validator.TryValidate(context, deviceId, BaseHelper.CctvStreamTicketHp.KindImage, out var error))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync(error ?? "Unauthorized");
                return;
            }

            await proxy.HandleAsync(context, deviceId);
        });

        // Web người dân: không cần đăng nhập/ticket, chỉ equipment công khai (SysOpConfig cctv_public_equipment_ids).
        app.MapGet("/public/equipments/{equipmentId}/snapshot.jpg", (
                HttpContext context,
                string equipmentId,
                PublicSnapshotHandler handler) => handler.HandleAsync(context, equipmentId))
            .RequireCors(PublicCorsPolicy)
            .RequireRateLimiting(PublicRateLimitPolicy);

        return app;
    }

    /// <summary>
    /// WebSocket không chịu CORS nên tự kiểm tra Origin. AllowedOrigins rỗng = cho phép mọi origin.
    /// </summary>
    private static bool IsOriginAllowed(HttpContext context, IConfigManager config)
    {
        var allowed = config.Current.Proxy.AllowedOrigins;
        if (allowed.Count == 0 || allowed.Contains("*"))
            return true;

        var origin = context.Request.Headers.Origin.ToString();
        return string.IsNullOrEmpty(origin)
            || allowed.Any(x => string.Equals(x.TrimEnd('/'), origin, StringComparison.OrdinalIgnoreCase));
    }
}
