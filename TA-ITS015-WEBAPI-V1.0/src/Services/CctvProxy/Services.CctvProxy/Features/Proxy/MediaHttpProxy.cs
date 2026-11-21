using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Services.CctvProxy.Configuration;
using Services.CctvProxy.Features.Session;
using Services.Shared.Logging;
using Shared.Utility.Helpers;

namespace Services.CctvProxy.Features.Proxy;

/// <summary>
/// Author: kien.le
/// Description: Chuyển tiếp HTTP media NxVMS (/rest/v4/devices/{id}/media — tải video archive) về FE.
/// Stream thẳng body (không buffer cả file), giữ Range để trình duyệt tải tiếp/tua được.
/// Created date: 06/10/2026
/// </summary>
public sealed class MediaHttpProxy(
    IHttpClientFactory httpClientFactory,
    NxVmsSessionManager sessionManager,
    IConfigManager configManager)
{
    /// <summary>Header response NxVMS được chép về FE.</summary>
    private static readonly string[] ForwardedResponseHeaders =
    [
        "Content-Type",
        "Content-Length",
        "Content-Range",
        "Content-Disposition",
        "Accept-Ranges",
        "Last-Modified",
        "ETag",
    ];

    /// <summary>
    /// Gọi NxVMS /media (kèm Bearer token phía BE) rồi stream body về FE, chép các header cần cho tải file/tua (Range).
    /// </summary>
    /// <param name="context">Request của FE (đã kiểm tra ticket).</param>
    /// <param name="deviceId">DeviceId NxVMS, dùng đặt tên file tải về khi NxVMS không trả Content-Disposition.</param>
    public async Task HandleAsync(HttpContext context, string deviceId)
    {
        var aborted = context.RequestAborted;

        // Ticket không ký query → tự sửa durationMs trên URL vẫn phải bị chặn theo giới hạn chung với WebAPI.
        if (!TryValidateDuration(context.Request, out var error))
        {
            LogManager.Log(Microsoft.Extensions.Logging.LogLevel.Warning,
                $"Từ chối {context.Request.Path} (device {deviceId}) từ {context.Connection.RemoteIpAddress}: {error}",
                kind: LogKind.Main);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(error!, aborted);
            return;
        }

        using var response = await SendUpstreamAsync(context, retryOnUnauthorized: true, aborted);
        if (response is null)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            // Lỗi xác thực phía NxVMS là lỗi cấu hình BE — không để trình duyệt hiện hộp thoại đăng nhập.
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            return;
        }

        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var name in ForwardedResponseHeaders)
        {
            if (response.Headers.TryGetValues(name, out var values) || response.Content.Headers.TryGetValues(name, out values))
                context.Response.Headers[name] = values.ToArray();
        }

        if (response.IsSuccessStatusCode && !context.Response.Headers.ContainsKey("Content-Disposition")
            && context.Request.Query.ContainsKey("download"))
        {
            context.Response.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = $"camera-{deviceId}-archive.mp4",
            }.ToString();
        }

        try
        {
            await using var body = await response.Content.ReadAsStreamAsync(aborted);
            await body.CopyToAsync(context.Response.Body, aborted);
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException or HttpRequestException)
        {
            // FE huỷ tải / mất kết nối giữa chừng — bình thường, không log lỗi.
        }
    }

    /// <summary>
    /// Author: ChuongLP
    /// Description: <c>durationMs</c> bắt buộc, &gt; 0 và không vượt <see cref="BaseHelper.CctvStreamTicketHp.MaxMediaDownloadDurationMs"/>
    /// (thiếu durationMs NxVMS sẽ stream không giới hạn).
    /// Created date: 21/10/2026
    /// </summary>
    private static bool TryValidateDuration(HttpRequest request, out string? error)
    {
        error = null;
        var values = request.Query["durationMs"];
        if (values.Count != 1 || !long.TryParse(values[0], out var durationMs) || durationMs <= 0)
        {
            error = "Thiếu hoặc sai durationMs";
            return false;
        }

        if (durationMs > BaseHelper.CctvStreamTicketHp.MaxMediaDownloadDurationMs)
        {
            error = $"Thời lượng tải tối đa {BaseHelper.CctvStreamTicketHp.MaxMediaDownloadMinutes} phút";
            return false;
        }

        return true;
    }

    private async Task<HttpResponseMessage?> SendUpstreamAsync(
        HttpContext context, bool retryOnUnauthorized, CancellationToken cancellationToken)
    {
        var current = configManager.Current;
        var token = await sessionManager.GetTokenAsync(cancellationToken);
        if (token is null)
            return null;

        var uri = UpstreamUrlBuilder.Build(current.Cctv.BaseUrl, context.Request, webSocket: false);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (context.Request.Headers.Range.Count > 0)
            request.Headers.TryAddWithoutValidation("Range", context.Request.Headers.Range.ToArray());

        var client = httpClientFactory.CreateClient(NxVmsSessionManager.HttpClientName);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, current.Proxy.ConnectTimeoutSeconds)));

        HttpResponseMessage response;
        try
        {
            // ResponseHeadersRead: timeout chỉ áp cho lúc chờ header; body stream theo RequestAborted.
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
            LogManager.Error(exception, $"Không gọi được NxVMS {uri.GetLeftPart(UriPartial.Path)}", kind: LogKind.Data);
            return null;
        }

        if (retryOnUnauthorized && response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Token NxVMS bị thu hồi/hết hạn sớm → login lại và thử đúng 1 lần.
            response.Dispose();
            sessionManager.Invalidate(token);
            return await SendUpstreamAsync(context, retryOnUnauthorized: false, cancellationToken);
        }

        return response;
    }
}
