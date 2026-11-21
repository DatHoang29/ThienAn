using System.Text;
using System.Text.Json;
using Services.CctvProxy.Configuration;
using Services.Shared.Logging;

namespace Services.CctvProxy.Features.Session;

/// <summary>
/// Author: kien.le
/// Description: Giữ phiên đăng nhập NxVMS phía BE (singleton) — thay cho việc FE tự login + set cookie.
/// - Cache token theo (BaseUrl, Username): đổi cấu hình thì tự login lại.
/// - Single-flight: nhiều request cùng lúc chỉ login một lần.
/// - Gia hạn trước khi hết hạn (dựa vào expiresInS NxVMS trả về; không có thì coi 55 phút).
/// - Login lỗi thì chờ LoginRetryDelaySeconds mới thử lại, tránh spam NxVMS khi host down.
/// Created date: 06/10/2026
/// </summary>
public sealed class NxVmsSessionManager(IHttpClientFactory httpClientFactory, IConfigManager configManager) : IDisposable
{
    public const string HttpClientName = "NxVms";

    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(55);
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(2);

    private readonly SemaphoreSlim _loginLock = new(1, 1);

    private Session? _session;
    private DateTimeOffset _lastFailureAt = DateTimeOffset.MinValue;

    /// <summary>Lấy token hợp lệ (cache, tự login lại khi gần hết hạn hoặc đổi cấu hình).</summary>
    /// <param name="cancellationToken">Huỷ khi request bị huỷ.</param>
    /// <returns>Token Bearer; null nếu không login được (NxVMS down / sai tài khoản / chưa cấu hình).</returns>
    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {
        var key = CurrentKey();
        var session = Volatile.Read(ref _session);
        if (IsUsable(session, key))
            return session!.Token;

        await _loginLock.WaitAsync(cancellationToken);
        try
        {
            // Request khác có thể đã login xong trong lúc chờ lock.
            session = Volatile.Read(ref _session);
            if (IsUsable(session, key))
                return session!.Token;

            var retryDelay = TimeSpan.FromSeconds(Math.Max(0, configManager.Current.Proxy.LoginRetryDelaySeconds));
            if (DateTimeOffset.UtcNow - _lastFailureAt < retryDelay)
                return null;

            var created = await LoginAsync(key, cancellationToken);
            if (created is null)
            {
                _lastFailureAt = DateTimeOffset.UtcNow;
                return null;
            }

            Volatile.Write(ref _session, created);
            return created.Token;
        }
        finally
        {
            _loginLock.Release();
        }
    }

    /// <summary>
    /// Bỏ token khi NxVMS trả 401. Chỉ bỏ nếu đúng token đang cache để nhiều request lỗi cùng lúc
    /// không xoá mất token mới vừa được request khác login lại.
    /// </summary>
    /// <param name="token">Token vừa bị NxVMS từ chối.</param>
    public void Invalidate(string? token)
    {
        var session = Volatile.Read(ref _session);
        if (session is not null && session.Token == token)
            Interlocked.CompareExchange(ref _session, null, session);
    }

    public void Dispose() => _loginLock.Dispose();

    private async Task<Session?> LoginAsync(string key, CancellationToken cancellationToken)
    {
        var cctv = configManager.Current.Cctv;
        if (string.IsNullOrWhiteSpace(cctv.BaseUrl) || string.IsNullOrWhiteSpace(cctv.Username))
        {
            LogManager.Log(Microsoft.Extensions.Logging.LogLevel.Warning,
                "Chưa cấu hình cctv_base_url / cctv_username trong SysOpConfig (nhóm cctv_config)", kind: LogKind.Device);
            return null;
        }

        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, configManager.Current.Proxy.ConnectTimeoutSeconds)));

            // NxVMS rejects chunked login bodies with HTTP 400 "Invalid JSON content".
            // StringContent supplies Content-Length, unlike streaming JsonContent/PostAsJsonAsync.
            using var content = new StringContent(
                JsonSerializer.Serialize(new { username = cctv.Username, password = cctv.Password, setCookie = false }),
                Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(
                $"{cctv.BaseUrl.TrimEnd('/')}/rest/v4/login/sessions",
                content,
                timeout.Token);

            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                LogManager.Log(Microsoft.Extensions.Logging.LogLevel.Warning,
                    $"Login NxVMS thất bại: HTTP {(int)response.StatusCode} {Truncate(body)}", kind: LogKind.Device);
                return null;
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            // NxVMS có thể trả "token" hoặc "accessToken" tuỳ phiên bản.
            var token = root.TryGetProperty("token", out var tokenEl) ? tokenEl.GetString()
                : root.TryGetProperty("accessToken", out var accessEl) ? accessEl.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(token))
            {
                LogManager.Log(Microsoft.Extensions.Logging.LogLevel.Warning,
                    $"Login NxVMS không trả token: {Truncate(body)}", kind: LogKind.Device);
                return null;
            }

            var lifetime = DefaultLifetime;
            if (root.TryGetProperty("expiresInS", out var expiresEl) && expiresEl.TryGetInt64(out var expiresInS) && expiresInS > 0)
                lifetime = TimeSpan.FromSeconds(expiresInS);

            LogManager.Info($"Login NxVMS thành công ({cctv.BaseUrl}), hạn {lifetime.TotalMinutes:0} phút", kind: LogKind.Device);
            return new Session(token, key, DateTimeOffset.UtcNow.Add(lifetime));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            LogManager.Error(exception, $"Không login được NxVMS ({cctv.BaseUrl})", kind: LogKind.Device);
            return null;
        }
    }

    private string CurrentKey()
    {
        var cctv = configManager.Current.Cctv;
        return $"{cctv.BaseUrl.TrimEnd('/')}|{cctv.Username}|{cctv.Password}";
    }

    private static bool IsUsable(Session? session, string key) =>
        session is not null
        && session.Key == key
        && session.ExpiresAt - RefreshMargin > DateTimeOffset.UtcNow;

    private static string Truncate(string value) => value.Length <= 300 ? value : value[..300];

    private sealed record Session(string Token, string Key, DateTimeOffset ExpiresAt);
}
