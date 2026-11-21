using Microsoft.Extensions.Configuration;
using Modules.System.Controllers.Config.Service;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Shared.Utility.Utilities;

namespace Module.Integration.CCTV.Infrastructure.Services
{
    public class NxVmsAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly NxVmsConfigService _configService;

        // Service đăng ký Scoped (mỗi request một instance) nên cache token phải static,
        // nếu không gần như request nào cũng login lại và sinh rất nhiều session trên NxVMS.
        // Cache gắn với (BaseUrl, Username, Password): đổi cấu hình cctv_config thì token cũ bị bỏ, login lại.
        // Author: kien.le - Date: 2026-10-06
        private static readonly SemaphoreSlim _loginLock = new(1, 1);
        private static CachedToken? _cached;

        // Update 21/10/2026 (giống NxVmsSessionManager của Cctv Proxy):
        // - Hạn token theo expiresInS NxVMS trả về (không có → 55 phút), làm mới trước 2 phút.
        // - Login thất bại → trong LoginRetryDelay trả null ngay, không login lại;
        //   tránh mọi request CCTV xếp hàng chờ login khi NxVMS sập.
        private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(55);
        private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan LoginRetryDelay = TimeSpan.FromSeconds(15);
        private static long _lastFailureTicksUtc;

        private sealed record LoginConfig(string BaseUrl, string Username, string Password);

        private sealed record CachedToken(string Token, LoginConfig Config, DateTime ExpiresAtUtc);

        public NxVmsAuthService(HttpClient httpClient,  NxVmsConfigService configService)
        {
            _httpClient = httpClient;
            _configService = configService;
        }

        /// <summary>
        /// Lấy token hiện tại. Nếu còn hạn và cùng cấu hình đăng nhập thì trả về luôn,
        /// ngược lại tự động login để lấy token mới.
        /// Login vừa thất bại (trong 15 giây) → trả null ngay, không thử lại.
        /// Author: ChuongLP
        /// Date: 2026-05-29
        /// </summary>
        public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            var config = await GetLoginConfigAsync();
            if (TryGetCachedToken(config, out var cached))
                return cached;

            // Single-flight: nhiều request cùng lúc chỉ login một lần.
            await _loginLock.WaitAsync(cancellationToken);
            try
            {
                if (TryGetCachedToken(config, out cached))
                    return cached;

                if (DateTime.UtcNow - new DateTime(Interlocked.Read(ref _lastFailureTicksUtc), DateTimeKind.Utc) < LoginRetryDelay)
                    return null;

                return await LoginAsync(config, cancellationToken);
            }
            finally
            {
                _loginLock.Release();
            }
        }

        private static bool TryGetCachedToken(LoginConfig config, out string? token)
        {
            var cached = Volatile.Read(ref _cached);
            token = cached?.Token;
            return cached is not null
                && !string.IsNullOrWhiteSpace(cached.Token)
                && cached.Config == config
                && cached.ExpiresAtUtc - RefreshMargin > DateTime.UtcNow;
        }

        private async Task<LoginConfig> GetLoginConfigAsync()
        {
            // Đọc từ DB thay vì appsettings
            var (baseUrl, username, password) = await _configService.GetLoginConfigAsync();
            return new LoginConfig(baseUrl.TrimEnd('/'), username, password);
        }

        /// <summary>
        /// Đăng nhập vào NxVMS và lưu token mới vào cache.
        /// Author: ChuongLP
        /// Date: 2026-05-29
        /// </summary>
        public async Task<string?> LoginAsync(CancellationToken cancellationToken = default)
        {
            return await LoginAsync(await GetLoginConfigAsync(), cancellationToken);
        }

        private async Task<string?> LoginAsync(LoginConfig config, CancellationToken cancellationToken)
        {
            try
            {
                var jsonBody = JsonSerializer.Serialize(new { username = config.Username, password = config.Password, setCookie = true });
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var loginUrl = $"{config.BaseUrl}/rest/v4/login/sessions";
                using var response = await _httpClient.PostAsync(loginUrl, content, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                using var document = JsonDocument.Parse(responseBody);
                var root = document.RootElement;

                // NxVMS có thể trả về "token" hoặc "accessToken" tuỳ phiên bản
                string? token = null;
                if (root.TryGetProperty("token", out var tokenEl))
                    token = tokenEl.GetString();

                else if (root.TryGetProperty("accessToken", out var accessTokenEl))
                    token = accessTokenEl.GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    // Sai tài khoản / NxVMS trả lỗi → coi như login thất bại.
                    MarkLoginFailed();
                    return null;
                }

                var lifetime = root.TryGetProperty("expiresInS", out var expiresEl)
                               && expiresEl.TryGetInt64(out var expiresInS) && expiresInS > 0
                    ? TimeSpan.FromSeconds(expiresInS)
                    : DefaultLifetime;

                Volatile.Write(ref _cached, new CachedToken(token, config, DateTime.UtcNow.Add(lifetime)));
                Interlocked.Exchange(ref _lastFailureTicksUtc, 0);

                return token;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Caller chủ động hủy request → giữ nguyên hành vi hủy, không nuốt.
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException
                                       or TaskCanceledException
                                       or System.Net.Sockets.SocketException
                                       or JsonException)
            {
                // Không kết nối được tới NxVMS (host unreachable / timeout / từ chối kết nối) hoặc body không phải JSON
                // → KHÔNG quăng lỗi. Xóa token cache và trả về null để tầng trên tự xử lý.
                MarkLoginFailed();
                return null;
            }
        }

        private void MarkLoginFailed()
        {
            ClearToken();
            Interlocked.Exchange(ref _lastFailureTicksUtc, DateTime.UtcNow.Ticks);
        }

        /// <summary>
        /// Xóa token đang cache. Gọi khi nhận được 401 để buộc login lại ở lần tiếp theo.
        /// Author: ChuongLP
        /// Date: 2026-05-29
        /// </summary>
        public void ClearToken()
        {
            Volatile.Write(ref _cached, null);
        }
    }
}
