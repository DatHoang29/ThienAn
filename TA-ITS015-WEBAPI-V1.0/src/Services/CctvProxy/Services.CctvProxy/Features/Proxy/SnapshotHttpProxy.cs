using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Services.CctvProxy.Configuration;
using Services.CctvProxy.Features.Session;
using Services.Shared.Logging;
using Shared.Utility.Utilities;

namespace Services.CctvProxy.Features.Proxy;

/// <summary>
/// Author: kien.le
/// Description: Chuyển tiếp ảnh snapshot NxVMS (/rest/v4/devices/{id}/image) — chuyển từ luồng snapshot của
/// WebAPI (NxVmsApiService.GetSnapshotAsync) sang proxy để FE ở mạng public xem camera dạng ảnh định kỳ
/// qua cổng public mà không cần video stream.
/// - Chỉ chuyển các tham số NxVMS hỗ trợ: timestampMs, size, format, crop, rotation.
/// - Ảnh live (không có timestampMs) được dùng chung trong SnapshotCacheSeconds cho cùng camera + tham số:
///   nhiều người xem cùng camera chỉ gọi NxVMS 1 lần mỗi chu kỳ (single-flight).
/// Created date: 06/10/2026
/// </summary>
public sealed class SnapshotHttpProxy(
    IHttpClientFactory httpClientFactory,
    NxVmsSessionManager sessionManager,
    IConfigManager configManager)
{
    private static readonly string[] AllowedQueryKeys = ["timestampMs", "size", "format", "crop", "rotation"];

    /// <summary>Giới hạn số ảnh cache, quá thì dọn mục hết hạn.</summary>
    private const int CacheCleanupThreshold = 512;

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);

    /// <summary>
    /// Trả ảnh snapshot cho request có ticket: ảnh live dùng chung cache SnapshotCacheSeconds, ảnh theo thời điểm gọi thẳng NxVMS.
    /// </summary>
    /// <param name="context">Request của FE (đã kiểm tra ticket).</param>
    /// <param name="deviceId">DeviceId NxVMS (đã khớp với ticket).</param>
    public async Task HandleAsync(HttpContext context, string deviceId)
    {
        var current = configManager.Current;
        var uri = UpstreamUrlBuilder.Build(current.Cctv.BaseUrl, context.Request, webSocket: false, AllowedQueryKeys);

        var cacheSeconds = Math.Max(0, current.Proxy.SnapshotCacheSeconds);
        var isLive = !context.Request.Query.ContainsKey("timestampMs");

        SnapshotResult result;
        if (isLive && cacheSeconds > 0)
            result = await GetOrFetchAsync(uri, TimeSpan.FromSeconds(cacheSeconds), context.RequestAborted);
        else
            result = await FetchAsync(uri, retryOnUnauthorized: true, context.RequestAborted);

        context.Response.StatusCode = result.StatusCode;
        context.Response.Headers.CacheControl = "no-store";
        if (result.Data is null)
            return;

        context.Response.ContentType = result.ContentType;
        context.Response.ContentLength = result.Data.Length;
        await context.Response.Body.WriteAsync(result.Data, context.RequestAborted);
    }

    /// <summary>
    /// Ảnh live của một camera (định dạng jpg, kích thước cố định), dùng chung cache với endpoint có ticket.
    /// Dùng cho endpoint public của web người dân.
    /// </summary>
    /// <param name="deviceId">DeviceId NxVMS.</param>
    /// <param name="size">Kích thước ảnh, vd 640x360.</param>
    /// <param name="ttl">Thời gian dùng chung ảnh; &lt;= 0 thì không cache.</param>
    /// <param name="cancellationToken">Huỷ khi người xem ngắt kết nối (không huỷ lần gọi NxVMS dùng chung).</param>
    /// <returns>Ảnh (Data != null) hoặc mã lỗi HTTP tương ứng.</returns>
    public Task<SnapshotResult> GetLiveAsync(string deviceId, string size, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var baseUrl = configManager.Current.Cctv.BaseUrl.TrimEnd('/');
        // Thứ tự query giống UpstreamUrlBuilder (sắp theo key) để dùng chung mục cache với request có ticket cùng tham số.
        var uri = new Uri($"{baseUrl}/rest/v4/devices/{Uri.EscapeDataString(deviceId)}/image?format=jpg&size={Uri.EscapeDataString(size)}");
        return ttl > TimeSpan.Zero
            ? GetOrFetchAsync(uri, ttl, cancellationToken)
            : FetchAsync(uri, retryOnUnauthorized: true, cancellationToken);
    }

    private async Task<SnapshotResult> GetOrFetchAsync(Uri uri, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var key = uri.ToString();
        var now = DateTimeOffset.UtcNow;

        while (true)
        {
            if (_cache.TryGetValue(key, out var existing) && !IsStale(existing, now, ttl))
                return await existing.Task.WaitAsync(cancellationToken);

            // Lần gọi NxVMS dùng chung cho nhiều request — không gắn với RequestAborted của request nào.
            var created = new CacheEntry(new Lazy<Task<SnapshotResult>>(() => FetchAsync(uri, retryOnUnauthorized: true, CancellationToken.None)), now);
            var added = existing is null
                ? _cache.TryAdd(key, created)
                : _cache.TryUpdate(key, created, existing);

            // Request khác vừa thay mục cache → đọc lại.
            if (!added)
                continue;

            CleanupIfNeeded(now, ttl);
            return await created.Task.WaitAsync(cancellationToken);
        }
    }

    /// <summary>Hết hạn, hoặc lần gọi trước đã xong mà lỗi (không cache lỗi).</summary>
    private static bool IsStale(CacheEntry entry, DateTimeOffset now, TimeSpan ttl) =>
        now - entry.CreatedAt >= ttl
        || (entry.Task.IsCompleted && (entry.Task.IsFaulted || entry.Task.Result.Data is null));

    private void CleanupIfNeeded(DateTimeOffset now, TimeSpan ttl)
    {
        if (_cache.Count <= CacheCleanupThreshold)
            return;

        foreach (var (key, entry) in _cache)
        {
            if (entry.Task.IsCompleted && now - entry.CreatedAt >= ttl)
                _cache.TryRemove(new KeyValuePair<string, CacheEntry>(key, entry));
        }
    }

    private async Task<SnapshotResult> FetchAsync(Uri uri, bool retryOnUnauthorized, CancellationToken cancellationToken)
    {
        var current = configManager.Current;
        var token = await sessionManager.GetTokenAsync(cancellationToken);
        if (token is null)
            return SnapshotResult.Failed(StatusCodes.Status503ServiceUnavailable);

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var client = httpClientFactory.CreateClient(NxVmsSessionManager.HttpClientName);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, current.Proxy.ConnectTimeoutSeconds)));

        try
        {
            using var response = await client.SendAsync(request, timeout.Token);

            if (retryOnUnauthorized && response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Token NxVMS bị thu hồi/hết hạn sớm → login lại và thử đúng 1 lần.
                sessionManager.Invalidate(token);
                return await FetchAsync(uri, retryOnUnauthorized: false, cancellationToken);
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return SnapshotResult.Failed(StatusCodes.Status502BadGateway);

            if (!response.IsSuccessStatusCode)
            {
                LogManager.Log(Microsoft.Extensions.Logging.LogLevel.Warning,
                    $"Snapshot NxVMS thất bại {uri.GetLeftPart(UriPartial.Path)}: HTTP {(int)response.StatusCode}",
                    kind: LogKind.Data);
                return SnapshotResult.Failed((int)response.StatusCode);
            }

            var data = await response.Content.ReadAsByteArrayAsync(timeout.Token);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            return new SnapshotResult(StatusCodes.Status200OK, data, contentType, new DateTimeOffset(BaseUtil.TimeServer.GetTimeServer()));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
            LogManager.Error(exception, $"Không lấy được snapshot NxVMS {uri.GetLeftPart(UriPartial.Path)}", kind: LogKind.Data);
            return SnapshotResult.Failed(exception is OperationCanceledException
                ? StatusCodes.Status504GatewayTimeout
                : StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>Kết quả lấy ảnh. <see cref="FetchedAt"/> = thời điểm NxVMS trả ảnh (giờ server — BaseUtil.TimeServer), dùng để nhận biết ảnh mới.</summary>
    public sealed record SnapshotResult(int StatusCode, byte[]? Data, string ContentType, DateTimeOffset FetchedAt)
    {
        /// <summary>Kết quả lỗi (không có ảnh) kèm mã HTTP trả cho người gọi.</summary>
        /// <param name="statusCode">Mã HTTP lỗi.</param>
        public static SnapshotResult Failed(int statusCode) => new(statusCode, null, string.Empty, DateTimeOffset.MinValue);
    }

    private sealed class CacheEntry(Lazy<Task<SnapshotResult>> task, DateTimeOffset createdAt)
    {
        public Task<SnapshotResult> Task => task.Value;

        public DateTimeOffset CreatedAt { get; } = createdAt;
    }
}
