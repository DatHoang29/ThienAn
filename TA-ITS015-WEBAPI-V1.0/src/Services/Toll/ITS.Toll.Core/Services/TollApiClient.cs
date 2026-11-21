using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using ITS.Toll.Core.Configuration;
using ITS.Toll.Core.Logging;
using ITS.Toll.Core.Models;

namespace ITS.Toll.Core.Services;

/// <summary>
/// Gọi API thu phí (ETC) bằng HTTP GET và trả về <see cref="TollApiCallResult"/>.
/// Tự GHI LOG luồng xử lý (bắt đầu gọi → nhận dữ liệu/số bản ghi) qua <see cref="TollLogger"/>.
/// Chỉ lo việc GỌI + đo đạc + format phản hồi; KHÔNG lưu DB (phần đó xem <see cref="TollDataStore"/>).
/// Author: ChuongLP
/// Created date: 24/08/2026
/// </summary>
public sealed class TollApiClient
{
    private readonly HttpClient _http;
    private readonly TollLogger _logger;

    public TollApiClient(HttpClient http, TollLogger logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// FromDate/ToDate vào query string theo <paramref name="options"/>.DateFormat.
    /// Không ném exception ra ngoài — mọi lỗi gói vào <see cref="TollApiCallResult.Error"/>.
    /// Author: ChuongLP
    /// Date: 24/08/2026
    /// </summary>
    public async Task<TollApiCallResult> CallAsync(TollApiEndpoint endpoint, TollOptions options, DateTime? fromDate = null, DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = new TollApiCallResult
        {
            EndpointName = endpoint.Name,
            DataType = endpoint.DataType,
        };

        // --- Bắt đầu: ghi rõ đang gọi API nào, loại dữ liệu, khoảng ngày (nếu có), URL. ---
        _logger.Info(LogCategory.Sync, $"→ Gọi API '{endpoint.Name}' [{endpoint.DataType}]");
        if (endpoint.RequiresDateRange)
            _logger.Info(LogCategory.Sync, $"   Khoảng ngày: {fromDate:yyyy-MM-dd HH:mm:ss} … {toDate:yyyy-MM-dd HH:mm:ss}");

        string url;
        try
        {
            url = BuildUrl(endpoint, options, fromDate, toDate);
            result.RequestUrl = url;
            _logger.Info(LogCategory.Sync, $"   URL: {url}");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"URL không hợp lệ: {ex.Message}";
            _logger.Error(LogCategory.Sync, $"← Lỗi dựng URL '{endpoint.Name}': {ex.Message}");
            return result;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var seconds = options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 30;
            timeout.CancelAfter(TimeSpan.FromSeconds(seconds));

            using var response = await _http.GetAsync(url, timeout.Token).ConfigureAwait(false);
            result.StatusCode = (int)response.StatusCode;
            result.ContentType = response.Content.Headers.ContentType?.ToString();

            var content = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
            result.RawContent = content;
            result.Success = response.IsSuccessStatusCode;

            (result.PrettyContent, result.RecordCount) = TryFormatJson(content);

            if (!result.Success)
                result.Error = $"HTTP {result.StatusCode} {response.ReasonPhrase}";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            result.Success = false;
            result.Error = "Đã hủy bởi người dùng.";
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.Error = $"Quá thời gian chờ đồng bộ dữ liệu ({options.TimeoutSeconds}s).";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;
        }

        // --- Nhận: HTTP, thời gian, dung lượng payload, tổng số bản ghi. ---
        var size = FormatSize(result.RawContent?.Length ?? 0);
        var count = result.RecordCount?.ToString() ?? "?";
        if (result.Success)
            _logger.Success(LogCategory.Sync, $"← Nhận '{endpoint.Name}': HTTP {result.StatusCode} · {result.DurationMs} ms · {size} · ~{count} bản ghi");
        else
            _logger.Error(LogCategory.Sync, $"← Lỗi '{endpoint.Name}': {result.Error} (HTTP {result.StatusCode} · {result.DurationMs} ms)");

        return result;
    }

    /// <summary>Định dạng dung lượng payload cho dễ đọc (B/KB/MB) từ độ dài chuỗi trả về.</summary>
    private static string FormatSize(int length)
    {
        if (length < 1024) return $"{length} B";
        if (length < 1024 * 1024) return $"{length / 1024.0:0.#} KB";
        return $"{length / (1024.0 * 1024):0.#} MB";
    }

    /// <summary>
    /// Ghép URL đầy đủ (base + path) và thêm query FromDate/ToDate nếu cần.
    /// Author: ChuongLP
    /// Date: 24/08/2026
    /// </summary>
    public static string BuildUrl(TollApiEndpoint endpoint, TollOptions options, DateTime? fromDate, DateTime? toDate)
    {
        // 1) URL cơ sở: tuyệt đối dùng luôn, tương đối ghép với BaseUrl.
        var raw = endpoint.Url?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Url rỗng.");

        string baseUrl;
        if (Uri.TryCreate(raw, UriKind.Absolute, out var abs))
        {
            baseUrl = abs.ToString();
        }
        else
        {
            var root = (options.BaseUrl ?? string.Empty).TrimEnd('/');
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Url tương đối nhưng chưa cấu hình BaseUrl.");
            baseUrl = $"{root}/{raw.TrimStart('/')}";
        }

        // 2) Thêm FromDate/ToDate nếu endpoint yêu cầu.
        if (!endpoint.RequiresDateRange)
            return baseUrl;

        var format = string.IsNullOrWhiteSpace(options.DateFormat) ? "yyyy-MM-dd HH:mm:ss" : options.DateFormat;
        var separator = baseUrl.Contains('?') ? "&" : "?";
        var query = new List<string>();

        if (fromDate.HasValue)
            query.Add($"{Uri.EscapeDataString(endpoint.DateFromParam)}={Uri.EscapeDataString(fromDate.Value.ToString(format))}");
        if (toDate.HasValue)
            query.Add($"{Uri.EscapeDataString(endpoint.DateToParam)}={Uri.EscapeDataString(toDate.Value.ToString(format))}");

        return query.Count == 0 ? baseUrl : $"{baseUrl}{separator}{string.Join("&", query)}";
    }

    /// <summary>
    /// Thử format JSON cho đẹp và đếm số bản ghi. Nếu không phải JSON thì trả về nguyên văn, count = null.
    /// Đếm (xem <see cref="CountRecords"/>): mảng gốc → độ dài; envelope data/result/items là mảng → độ dài;
    /// envelope là OBJECT bọc nhiều mảng (vd masterdata: vehicleTypes/tcsRoads...) → TỔNG độ dài các mảng con.
    /// </summary>
    private static (string Pretty, int? Count) TryFormatJson(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return (string.Empty, null);

        try
        {
            using var doc = JsonDocument.Parse(content);
            var pretty = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true,
                // Hiển thị tiếng Việt có dấu đúng ký tự (không escape \uXXXX).
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            });

            return (pretty, CountRecords(doc.RootElement));
        }
        catch (JsonException)
        {
            // Không phải JSON — giữ nguyên nội dung thô.
            return (content, null);
        }
    }

    /// <summary>
    /// Đếm số bản ghi nhận về theo nhiều dạng payload:
    ///   1) Root là mảng → độ dài mảng.
    ///   2) Root là object có envelope data/result/items:
    ///        - envelope là mảng (transaction/outSummary) → độ dài mảng;
    ///        - envelope là object bọc nhiều mảng (masterdata) → TỔNG độ dài các mảng con.
    ///   3) Root là object bọc nhiều mảng (không có envelope) → TỔNG độ dài các mảng con.
    /// Trả về null nếu không xác định được (để hiển thị "?").
    /// </summary>
    private static int? CountRecords(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return root.GetArrayLength();

        if (root.ValueKind != JsonValueKind.Object)
            return null;

        // Có envelope data/result/items (không phân biệt hoa/thường)?
        foreach (var name in new[] { "data", "result", "items" })
        {
            if (TryGetPropIgnoreCase(root, name, out var inner))
            {
                if (inner.ValueKind == JsonValueKind.Array)
                    return inner.GetArrayLength();
                if (inner.ValueKind == JsonValueKind.Object)
                    return SumArrayLengths(inner);
            }
        }

        // Không có envelope: thử tổng các mảng con ngay tại root.
        var sum = SumArrayLengths(root);
        return sum > 0 ? sum : (int?)null;
    }

    /// <summary>Tổng độ dài của tất cả thuộc tính là MẢNG trong một object (bỏ qua thuộc tính khác).</summary>
    private static int SumArrayLengths(JsonElement obj)
    {
        var total = 0;
        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Array)
                total += prop.Value.GetArrayLength();
        }
        return total;
    }

    /// <summary>Lấy thuộc tính theo tên, không phân biệt hoa/thường.</summary>
    private static bool TryGetPropIgnoreCase(JsonElement obj, string name, out JsonElement value)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        value = default;
        return false;
    }
}
