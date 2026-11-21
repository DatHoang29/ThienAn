using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Services.Shared.Events;
using Module.VideoWall.WPF.Api.Dto;

namespace Module.VideoWall.WPF.Api;

/// <summary>
/// Author: Đạt
/// Description: Nơi duy nhất gửi HTTP tới TAC_WebAPI: bóc vỏ WResult, quy lỗi về ApiException và
/// phát một dòng hoạt động qua ActivityPublisher cho mỗi lượt gọi.
/// Lượt THẤT BẠI luôn được log. Lượt THÀNH CÔNG có thể bỏ log qua suppressSuccessActivity — dành cho
/// device-setup, nơi phía gọi đã log từng bước ISAPI nên dòng chung chỉ làm trùng.
/// CỐ Ý không log request body: body đăng nhập có mật khẩu.
/// Created date: 25/08/2026
/// </summary>
public sealed class ApiInvoker(IHttpClientFactory httpClientFactory, ActivityPublisher activityPublisher)
{
    /// <summary>Client đã gắn JwtAuthHandler — dùng cho mọi endpoint nghiệp vụ.</summary>
    public const string VideoWallClient = "videowall";

    /// <summary>Client thô, KHÔNG gắn JwtAuthHandler — chỉ dùng cho login/refresh để tránh đệ quy.</summary>
    public const string AuthClient = "auth";

    private const int MaxLoggedBodyLength = 400;

    public Task<T?> GetAsync<T>(
        string clientName,
        string path,
        object? query,
        string stage,
        CancellationToken cancellationToken)
        => SendAsync<T>(clientName, HttpMethod.Get, path + BuildQuery(query), body: null, stage, cancellationToken);

    /// <param name="suppressSuccessActivity">
    /// Bỏ dòng log thành công chung của lượt gọi. Dùng cho các endpoint device-setup: phía gọi tự
    /// phát một dòng cho TỪNG bước ISAPI (có kèm request/response), nên dòng chung chỉ là bản trùng.
    /// Dòng log THẤT BẠI vẫn luôn được phát: khi lỗi thì không có bước nào để phát, bỏ luôn là mất dấu.
    /// </param>
    public Task<T?> PostAsync<T>(
        string clientName,
        string path,
        object? body,
        string stage,
        CancellationToken cancellationToken,
        bool suppressSuccessActivity = false)
        => SendAsync<T>(clientName, HttpMethod.Post, path, body, stage, cancellationToken, suppressSuccessActivity);

    /// <summary>Cho các endpoint ghi trả về void (Add/Update/Delete) — chỉ cần biết thành công hay không.</summary>
    public Task PostAsync(
        string clientName,
        string path,
        object? body,
        string stage,
        CancellationToken cancellationToken)
        => SendAsync<object>(clientName, HttpMethod.Post, path, body, stage, cancellationToken);

    /// <summary>
    /// Dựng query string từ một object lọc: phẳng hoá các thuộc tính vô hướng, bỏ qua null,
    /// enum ra số (theo ApiJson.Options).
    /// </summary>
    public static string BuildQuery(object? query)
    {
        if (query is null)
            return string.Empty;

        if (JsonSerializer.SerializeToNode(query, query.GetType(), ApiJson.Options) is not JsonObject node)
            return string.Empty;

        var pairs = new List<string>();
        foreach (var property in node)
        {
            if (property.Value is not JsonValue value)
                continue;

            var raw = value.ToString();
            if (string.IsNullOrEmpty(raw))
                continue;

            pairs.Add($"{Uri.EscapeDataString(property.Key)}={Uri.EscapeDataString(raw)}");
        }

        return pairs.Count == 0 ? string.Empty : "?" + string.Join("&", pairs);
    }

    private async Task<T?> SendAsync<T>(
        string clientName,
        HttpMethod method,
        string path,
        object? body,
        string stage,
        CancellationToken cancellationToken,
        bool suppressSuccessActivity = false)
    {
        var client = httpClientFactory.CreateClient(clientName);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null)
                request.Content = JsonContent.Create(body, body.GetType(), options: ApiJson.Options);

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            var envelope = TryReadEnvelope<T>(payload, response, method, path);
            if (envelope.Code != WResult<T>.SuccessCode)
                throw Fail(stage, method, path, stopwatch, envelope.Code, (int)response.StatusCode, envelope.Message);

            if (!suppressSuccessActivity)
            {
                activityPublisher.Publish(
                    stage,
                    $"{method} {path} → {envelope.Code} ({stopwatch.ElapsedMilliseconds} ms)",
                    ActivityLevel.Success);
            }

            return envelope.Result;
        }
        catch (ApiException)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            throw Fail(stage, method, path, stopwatch, envelopeCode: null, httpStatus: null, exception.Message);
        }
    }

    private ApiException Fail(
        string stage,
        HttpMethod method,
        string path,
        Stopwatch stopwatch,
        int? envelopeCode,
        int? httpStatus,
        string? message)
    {
        var reason = string.IsNullOrWhiteSpace(message) ? "không rõ nguyên nhân" : message.Trim();
        activityPublisher.Publish(
            stage,
            $"{method} {path} → Fail {reason}"
                + (envelopeCode is null ? string.Empty : $" (code {envelopeCode})")
                + $" ({stopwatch.ElapsedMilliseconds} ms)",
            ActivityLevel.Error);

        return new ApiException(reason, envelopeCode, httpStatus);
    }

    /// <summary>
    /// Bóc vỏ WResult. Khi phản hồi không phải JSON hợp lệ (HTML lỗi, 500 thô, backend chưa chạy)
    /// thì dựng envelope lỗi từ HTTP status + đoạn đầu payload để người vận hành còn thấy nguyên nhân.
    /// </summary>
    private static WResult<T> TryReadEnvelope<T>(
        string payload,
        HttpResponseMessage response,
        HttpMethod method,
        string path)
    {
        if (!string.IsNullOrWhiteSpace(payload))
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<WResult<T>>(payload, ApiJson.Options);
                if (envelope is not null && envelope.Code != 0)
                    return envelope;
            }
            catch (JsonException)
            {
                // Rơi xuống nhánh dựng envelope lỗi bên dưới.
            }
        }

        var snippet = payload.Length > MaxLoggedBodyLength
            ? payload[..MaxLoggedBodyLength] + "..."
            : payload;

        return new WResult<T>
        {
            Code = (int)response.StatusCode,
            Succeeded = false,
            Message = string.IsNullOrWhiteSpace(snippet)
                ? $"{method} {path} trả về HTTP {(int)response.StatusCode} {response.ReasonPhrase} với nội dung rỗng"
                : snippet,
        };
    }
}
