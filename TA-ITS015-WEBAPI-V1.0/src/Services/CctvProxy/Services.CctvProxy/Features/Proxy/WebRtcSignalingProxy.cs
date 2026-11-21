using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using Services.CctvProxy.Configuration;
using Services.CctvProxy.Features.Session;
using Services.Shared.Logging;

namespace Services.CctvProxy.Features.Proxy;

/// <summary>
/// Author: kien.le
/// Description: Chuyển tiếp WebSocket WebRTC signaling (SDP/ICE) giữa FE và NxVMS
/// (/rest/v4/devices/{id}/webrtc — live và playback). Proxy tự gắn Bearer token NxVMS,
/// FE không cần cookie/token. Lưu ý: chỉ signaling đi qua proxy; luồng video WebRTC vẫn đi
/// trực tiếp NxVMS -> trình duyệt theo ICE candidate.
/// Created date: 06/10/2026
/// </summary>
public sealed class WebRtcSignalingProxy(NxVmsSessionManager sessionManager, IConfigManager configManager)
{
    private const int BufferSize = 16 * 1024;

    /// <summary>
    /// Mở WebSocket tới NxVMS (kèm Bearer token phía BE) rồi mới accept WebSocket của FE và chuyển tiếp message
    /// hai chiều tới khi một phía đóng. Không kết nối được NxVMS thì trả HTTP lỗi (503/502/504/404) thay vì mở rồi đóng.
    /// </summary>
    /// <param name="context">Request WebSocket của FE (đã kiểm tra ticket).</param>
    /// <param name="deviceId">DeviceId NxVMS.</param>
    public async Task HandleAsync(HttpContext context, string deviceId)
    {
        var aborted = context.RequestAborted;
        var proxy = configManager.Current.Proxy;

        // Kết nối NxVMS TRƯỚC khi accept FE: lỗi thì trả HTTP status rõ ràng thay vì mở rồi đóng ngay.
        var (upstream, statusCode) = await ConnectUpstreamAsync(context, retryOnUnauthorized: true, aborted);
        if (upstream is null)
        {
            context.Response.StatusCode = statusCode;
            return;
        }

        using (upstream)
        {
            using var downstream = await context.WebSockets.AcceptWebSocketAsync(new WebSocketAcceptContext
            {
                KeepAliveInterval = TimeSpan.FromSeconds(Math.Max(1, proxy.WebSocketKeepAliveSeconds)),
            });

            LogManager.Info($"Mở WebRTC signaling device {deviceId} cho {context.Connection.RemoteIpAddress}", kind: LogKind.Data);

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(aborted);
            var toUpstream = PumpAsync(downstream, upstream, linked.Token);
            var toDownstream = PumpAsync(upstream, downstream, linked.Token);

            // Một phía đóng/lỗi thì dừng phía còn lại.
            await Task.WhenAny(toUpstream, toDownstream);
            await linked.CancelAsync();
            await Task.WhenAll(Suppress(toUpstream), Suppress(toDownstream));

            await CloseQuietlyAsync(downstream, upstream);
            await CloseQuietlyAsync(upstream, downstream);

            LogManager.Info($"Đóng WebRTC signaling device {deviceId}", kind: LogKind.Data);
        }
    }

    private async Task<(ClientWebSocket? Socket, int StatusCode)> ConnectUpstreamAsync(
        HttpContext context, bool retryOnUnauthorized, CancellationToken cancellationToken)
    {
        var current = configManager.Current;
        var token = await sessionManager.GetTokenAsync(cancellationToken);
        if (token is null)
            return (null, StatusCodes.Status503ServiceUnavailable);

        var uri = UpstreamUrlBuilder.Build(current.Cctv.BaseUrl, context.Request, webSocket: true);
        var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
        socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(Math.Max(1, current.Proxy.WebSocketKeepAliveSeconds));
        socket.Options.CollectHttpResponseDetails = true;
        if (current.Cctv.AcceptAnyCertificate)
            socket.Options.RemoteCertificateValidationCallback = static (_, _, _, _) => true;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, current.Proxy.ConnectTimeoutSeconds)));

        try
        {
            await socket.ConnectAsync(uri, timeout.Token);
            return (socket, StatusCodes.Status101SwitchingProtocols);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            socket.Dispose();
            throw;
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or HttpRequestException)
        {
            var status = socket.HttpStatusCode;
            socket.Dispose();

            if (retryOnUnauthorized && status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                // Token NxVMS bị thu hồi/hết hạn sớm → login lại và thử đúng 1 lần.
                sessionManager.Invalidate(token);
                return await ConnectUpstreamAsync(context, retryOnUnauthorized: false, cancellationToken);
            }

            LogManager.Error(exception,
                $"Không mở được WebSocket NxVMS {uri.GetLeftPart(UriPartial.Path)} (HTTP {(int)status})",
                kind: LogKind.Data);

            return (null, status switch
            {
                HttpStatusCode.NotFound => StatusCodes.Status404NotFound,
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => StatusCodes.Status502BadGateway,
                _ => exception is OperationCanceledException
                    ? StatusCodes.Status504GatewayTimeout
                    : StatusCodes.Status502BadGateway,
            });
        }
    }

    private static async Task PumpAsync(WebSocket source, WebSocket destination, CancellationToken cancellationToken)
    {
        var buffer = new byte[BufferSize];
        while (source.State == WebSocketState.Open && destination.State == WebSocketState.Open)
        {
            var result = await source.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
                return;

            await destination.SendAsync(
                new ArraySegment<byte>(buffer, 0, result.Count),
                result.MessageType,
                result.EndOfMessage,
                cancellationToken);
        }
    }

    /// <summary>Đóng <paramref name="socket"/>, mang theo close status của phía bên kia nếu có.</summary>
    private static async Task CloseQuietlyAsync(WebSocket socket, WebSocket other)
    {
        if (socket.State is not (WebSocketState.Open or WebSocketState.CloseReceived))
            return;

        var status = other.CloseStatus ?? WebSocketCloseStatus.NormalClosure;
        // 1005/1006 không được gửi trong frame close — quy về 1000/1011.
        if (status == WebSocketCloseStatus.Empty)
            status = WebSocketCloseStatus.NormalClosure;

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            await socket.CloseOutputAsync(status, other.CloseStatusDescription, timeout.Token);
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or ObjectDisposedException)
        {
            socket.Abort();
        }
    }

    private static async Task Suppress(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception exception) when (exception is WebSocketException or OperationCanceledException or IOException)
        {
            // Mất kết nối một phía là tình huống bình thường (đóng tab, mạng chập chờn).
        }
    }
}
