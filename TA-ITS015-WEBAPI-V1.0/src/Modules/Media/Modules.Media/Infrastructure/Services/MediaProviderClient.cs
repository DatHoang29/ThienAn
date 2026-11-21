using System.Reflection;
using Modules.Media.Infrastructure.Config;
using Newtonsoft.Json.Linq;

namespace Modules.Media.Infrastructure.Services;

/// <summary>
/// Description: [WP] Gọi sang ITS-Media.
/// Created date: 08/10/2026
/// </summary>
public sealed class MediaProviderClient
{
    // Một HttpClient dùng chung cho cả app (tránh cạn socket); timeout áp theo từng request.
    private static readonly HttpClient Http = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    })
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    public async Task<T?> GetAsync<T>(MediaConfig config, string subsystem, object query, CancellationToken ct)
    {
        var baseUrl = config.ProviderBaseUrl;
        var endpoint = config.Endpoint(subsystem);
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException(
                $"Chưa cấu hình media_provider_base_url / media_{subsystem}_endpoint trong SysOpConfig.");

        var url = $"{baseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}{BuildQuery(query)}";

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(config.TimeoutSeconds));

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation(MediaConst.ApiKeyHeader, config.ApiKey);

        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"ITS-Media trả về {(int)response.StatusCode} cho {endpoint}.");

        var json = await response.Content.ReadAsStringAsync(timeout.Token);
        if (string.IsNullOrWhiteSpace(json)) return default;

        var token = JToken.Parse(json);

        // Phòng trường hợp phía ITS vẫn bọc kết quả theo chuẩn Unify { code, result, ... }.
        if (token is JObject obj
            && obj.TryGetValue("result", StringComparison.OrdinalIgnoreCase, out var inner)
            && (obj.ContainsKey("code") || obj.ContainsKey("statusCode")))
        {
            token = inner;
        }

        return token.ToObject<T>();
    }

    private static string BuildQuery(object query)
    {
        var parts = query.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => (p.Name, Value: p.GetValue(query)))
            .Where(x => x.Value != null && !string.IsNullOrWhiteSpace(x.Value.ToString()))
            .Select(x => $"{Uri.EscapeDataString(x.Name)}={Uri.EscapeDataString(x.Value!.ToString()!)}")
            .ToList();

        return parts.Count == 0 ? string.Empty : "?" + string.Join('&', parts);
    }
}
