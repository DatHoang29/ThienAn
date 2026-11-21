using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Shared.Utility.Helpers;

namespace Services.CctvProxy.Features.Proxy;

/// <summary>
/// Author: kien.le
/// Description: Ghép URL NxVMS từ request vào proxy: giữ nguyên path + query (bỏ ticket),
/// thay origin bằng BaseUrl NxVMS. Proxy mở các path giống hệt NxVMS nên WebAPI chỉ cần đổi origin.
/// Created date: 06/10/2026
/// </summary>
internal static class UpstreamUrlBuilder
{
    /// <param name="allowedKeys">Chỉ chuyển các query key này sang NxVMS (null = chuyển tất cả trừ ticket).</param>
    public static Uri Build(string baseUrl, HttpRequest request, bool webSocket, IReadOnlyCollection<string>? allowedKeys = null)
    {
        var query = new QueryBuilder(
            request.Query
                .Where(x => !string.Equals(x.Key, BaseHelper.CctvStreamTicketHp.QueryName, StringComparison.OrdinalIgnoreCase))
                .Where(x => allowedKeys is null || allowedKeys.Contains(x.Key, StringComparer.OrdinalIgnoreCase))
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .SelectMany(x => x.Value.Select(v => new KeyValuePair<string, string>(x.Key, v ?? string.Empty))));

        var builder = new UriBuilder(baseUrl.TrimEnd('/'));
        builder.Path = builder.Path.TrimEnd('/') + request.Path.Value;
        builder.Query = query.ToQueryString().Value?.TrimStart('?') ?? string.Empty;

        if (webSocket)
            builder.Scheme = builder.Scheme == Uri.UriSchemeHttps ? "wss" : "ws";

        return builder.Uri;
    }
}
