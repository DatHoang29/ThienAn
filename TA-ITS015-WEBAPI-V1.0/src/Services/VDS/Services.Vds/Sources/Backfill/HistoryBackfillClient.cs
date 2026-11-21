using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Services.Vds.Configuration;
using Services.Vds.Pipeline;

namespace Services.Vds.Sources.Backfill;

public sealed record HistoryBackfillPage(
    int PageNumber,
    int Offset,
    IReadOnlyList<string> Frames,
    bool IsLastPage);

public sealed class BackfillConfigurationException(string message) : Exception(message);

public sealed class BackfillWindowTruncatedException(string message) : Exception(message);

/// <summary>
/// Author: KienLT
/// Description: HTTP client cho GET history API; chịu trách nhiệm URL, auth, status, giới hạn body và
/// phân trang. Không biết Wolverine, camera hay QuestDB.
/// Created date: 26/08/2026
/// </summary>
public sealed class HistoryBackfillClient
{
    private readonly ILogger<HistoryBackfillClient> _logger;
    private int _insecureHttpLogged;
    private int _selfSignedLogged;

    public HistoryBackfillClient(ILogger<HistoryBackfillClient> logger) => _logger = logger;

    public async IAsyncEnumerable<HistoryBackfillPage> QueryPagesAsync(
        IngestionOptions ingestion, BackfillOptions backfill, DateTime fromUtc, DateTime toUtc,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        for (var from = fromUtc; from < toUtc;)
        {
            var to = from.AddHours(1) < toUtc ? from.AddHours(1) : toUtc;
            await foreach (var page in QueryWindowAsync(ingestion, backfill, from, to, ct)) yield return page;
            from = to;
        }
    }

    private async IAsyncEnumerable<HistoryBackfillPage> QueryWindowAsync(
        IngestionOptions ingestion, BackfillOptions backfill, DateTime from, DateTime to,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var truncated = false;
        await using (var reader = QueryCoreAsync(ingestion, backfill, from, to, ct).GetAsyncEnumerator(ct))
        {
            while (true)
            {
                bool more;
                try { more = await reader.MoveNextAsync(); }
                catch (BackfillWindowTruncatedException) { truncated = true; break; }
                if (!more) break;
                yield return reader.Current;
            }
        }
        if (!truncated) yield break;
        if (to - from <= TimeSpan.FromSeconds(1))
            throw new BackfillWindowTruncatedException("History saturated even at one second; increase page limits. Coverage remains pending.");
        var mid = from.AddTicks((to - from).Ticks / 2);
        await foreach (var page in QueryWindowAsync(ingestion, backfill, from, mid, ct)) yield return page;
        await foreach (var page in QueryWindowAsync(ingestion, backfill, mid, to, ct)) yield return page;
    }

    private async IAsyncEnumerable<HistoryBackfillPage> QueryCoreAsync(
        IngestionOptions ingestion,
        BackfillOptions backfill,
        DateTime fromUtc,
        DateTime toUtc,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        fromUtc = EnsureUtc(fromUtc);
        toUtc = EnsureUtc(toUtc);
        if (fromUtc >= toUtc) yield break;

        using var handler = new HttpClientHandler();
        if (ingestion.AllowSelfSignedCert)
        {
            if (Interlocked.Exchange(ref _selfSignedLogged, 1) == 0)
                _logger.LogWarning("[Backfill] Đang bỏ kiểm tra chứng thư HTTPS theo cấu hình.");

            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        // Mỗi page tự có linked timeout bao trùm cả headers và body streaming.
        using var http = new HttpClient(handler)
        {
            Timeout = System.Threading.Timeout.InfiniteTimeSpan,
        };

        var limit = backfill.EffectivePageLimit;
        for (var pageIndex = 0; pageIndex < backfill.EffectiveMaxPages; pageIndex++)
        {
            ct.ThrowIfCancellationRequested();
            var offset = checked(pageIndex * limit);
            var url = BuildHistoryUrl(ingestion, backfill, fromUtc, toUtc, limit, offset);

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && Interlocked.Exchange(ref _insecureHttpLogged, 1) == 0)
            {
                _logger.LogWarning(
                    "[Backfill] Đang dùng HTTP không mã hóa theo Ingestion:AllowInsecureHttp=true.");
            }

            var frames = await FetchPageAsync(
                http,
                ingestion,
                backfill,
                url,
                fromUtc,
                toUtc,
                pageIndex + 1,
                offset,
                ct).ConfigureAwait(false);
            var isLast = frames.Count < limit;

            yield return new HistoryBackfillPage(
                pageIndex + 1, offset, frames, isLast);

            if (isLast) yield break;
        }

        throw new BackfillWindowTruncatedException(
            $"API history vẫn trả đủ trang sau trần {backfill.EffectiveMaxPages} x " +
            $"{backfill.EffectivePageLimit}; chưa bù hết cửa sổ {fromUtc:O}..{toUtc:O}.");
    }

    internal static string BuildHistoryUrl(
        IngestionOptions ingestion,
        BackfillOptions backfill,
        DateTime fromUtc,
        DateTime toUtc,
        int limit,
        int offset)
    {
        if (!Uri.TryCreate(ingestion.ApiBaseUrl?.Trim(), UriKind.Absolute, out var baseUri)
            || baseUri.Scheme is not ("http" or "https")
            || (baseUri.Scheme == "http" && !ingestion.AllowInsecureHttp))
        {
            throw new BackfillConfigurationException(
                "Ingestion:ApiBaseUrl không hợp lệ hoặc dùng HTTP khi AllowInsecureHttp=false.");
        }

        var baseUrl = ingestion.ApiBaseUrl.TrimEnd('/');
        var path = "/" + (string.IsNullOrWhiteSpace(backfill.HistoryPath)
            ? "api/v1/events"
            : backfill.HistoryPath.TrimStart('/'));

        if (!Uri.TryCreate(baseUrl + path, UriKind.Absolute, out var endpoint))
            throw new BackfillConfigurationException("Không dựng được URL history từ cấu hình.");

        var builder = new UriBuilder(endpoint);
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(builder.Query))
            query.Add(builder.Query.TrimStart('?'));

        query.Add("from=" + Uri.EscapeDataString(EnsureUtc(fromUtc).ToString("O", CultureInfo.InvariantCulture)));
        query.Add("to=" + Uri.EscapeDataString(EnsureUtc(toUtc).ToString("O", CultureInfo.InvariantCulture)));
        query.Add("limit=" + limit.ToString(CultureInfo.InvariantCulture));
        query.Add("offset=" + offset.ToString(CultureInfo.InvariantCulture));

        if (backfill.IncludeMedia) query.Add("include=media");
        if (!string.IsNullOrWhiteSpace(ingestion.Sources))
            query.Add("sources=" + Uri.EscapeDataString(ingestion.Sources.Trim()));

        builder.Query = string.Join("&", query);
        return builder.Uri.AbsoluteUri;
    }

    private static async Task<IReadOnlyList<string>> FetchPageAsync(
        HttpClient http,
        IngestionOptions ingestion,
        BackfillOptions backfill,
        string url,
        DateTime fromUtc,
        DateTime toUtc,
        int pageNumber,
        int offset,
        CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(backfill.EffectiveRequestTimeoutSeconds));
        var requestCt = timeoutCts.Token;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.ParseAdd("application/json");
            ingestion.ApplyAuth(request);

            using var response = await http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                requestCt).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new BackfillConfigurationException(
                    $"API history trả 404 cho {fromUtc:O}..{toUtc:O}; " +
                    "camera/scope/path không khớp nên không thể xác nhận coverage.");
            }

            ThrowConfigurationError(response);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var status = (int)response.StatusCode;
                if (status is >= 200 and <= 299)
                {
                    throw new InvalidDataException(
                        $"API history phải trả 200 với JSON array; nhận {(int)response.StatusCode} " +
                        $"({response.StatusCode}) ở page={pageNumber}, offset={offset}.");
                }

                response.EnsureSuccessStatusCode();
            }

            var body = await ReadLimitedAsync(
                response.Content,
                backfill.EffectiveMaxResponseCharacters,
                requestCt).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body))
            {
                throw new InvalidDataException(
                    $"API history trả body rỗng ở page={pageNumber}, offset={offset}; " +
                    "empty range hợp lệ phải là JSON array [].");
            }

            return ParseFrames(body);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"API history quá hạn {backfill.EffectiveRequestTimeoutSeconds}s ở " +
                $"page={pageNumber}, offset={offset}, window={fromUtc:O}..{toUtc:O}.", ex);
        }
    }

    private static void ThrowConfigurationError(HttpResponseMessage response)
    {
        var message = response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized =>
                "API history trả 401: API key sai hoặc đã bị thu hồi.",
            System.Net.HttpStatusCode.Forbidden =>
                "API history trả 403: API key thiếu scope events:read hoặc bị giới hạn nguồn/IP.",
            System.Net.HttpStatusCode.MethodNotAllowed =>
                "API history trả 405: kiểm tra Backfill:HistoryPath.",
            _ => null,
        };

        if (message is not null) throw new BackfillConfigurationException(message);
    }

    private static async Task<string> ReadLimitedAsync(
        HttpContent content, int maxCharacters, CancellationToken ct)
    {
        if (content.Headers.ContentLength is long length && length > maxCharacters * 4L)
        {
            throw new InvalidDataException(
                $"Response history Content-Length={length} vượt giới hạn an toàn.");
        }

        await using var stream = await content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var buffer = new char[8_192];
        var body = new StringBuilder(Math.Min(maxCharacters, 64 * 1_024));

        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), ct).ConfigureAwait(false);
            if (read == 0) break;
            if (body.Length + read > maxCharacters)
            {
                throw new InvalidDataException(
                    $"Response history vượt giới hạn {maxCharacters} ký tự.");
            }

            body.Append(buffer, 0, read);
        }

        return body.ToString();
    }

    private static IReadOnlyList<string> ParseFrames(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return Array.Empty<string>();

        JToken root;
        try
        {
            root = JToken.Parse(body);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Response history không phải JSON hợp lệ.", ex);
        }

        var array = root as JArray ?? root["events"] as JArray ?? root["data"] as JArray;
        if (array is null)
        {
            throw new InvalidDataException(
                "Response history phải là JSON array hoặc object chứa mảng events/data.");
        }

        return array.Select(item => item.ToString(Formatting.None)).ToList();
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
