using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ITS.VideoWall.Services.ISAPIDevice
{
    /// <summary>
    /// Tự tính header Authorization theo Digest (RFC 7616) thay vì để HttpClientHandler lo.
    ///
    /// VÌ SAO KHÔNG DÙNG HttpClientHandler.Credentials: firmware của bộ điều khiển trả challenge
    /// thiếu trường algorithm (RFC quy định mặc định MD5), và ở chế độ MD5/SHA-256 nó nhét HAI
    /// challenge vào cùng một header WWW-Authenticate — client cắt chuỗi theo dấu phẩy sẽ trộn
    /// nonce của cái này với algorithm của cái kia rồi xác thực hỏng. Đây đúng là lỗi đã làm
    /// Postman không gọi được thiết bị.
    ///
    /// Trường stale quyết định có được thử lại hay không, và đây là thứ chặn nguy cơ bị thiết bị
    /// khoá IP: stale=TRUE nghĩa mật khẩu ĐÚNG chỉ nonce hết hạn (thử lại an toàn), stale=FALSE
    /// nghĩa SAI tài khoản (đẩy 401 lên ngay cho circuit breaker, tuyệt đối không thử lại).
    ///
    /// ITransient là bắt buộc: IHttpClientFactory gán InnerHandler cho từng chuỗi handler nên một
    /// instance không được chia sẻ giữa các chuỗi.
    /// </summary>
    public class VwISAPIDigestHandler(
        VwISAPICredentialResolver credentialResolver,
        ILogger<VwISAPIDigestHandler> logger) : DelegatingHandler
    {
        private const string NonceCount = VwDeviceProfile.DigestNonceCount;

        private static readonly Regex ChallengeSplitter = new(@"(?:^|,)\s*Digest\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly VwISAPICredentialResolver _credentialResolver = credentialResolver;
        private readonly ILogger<VwISAPIDigestHandler> _logger = logger;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = await BufferContent(request, cancellationToken);

            var first = await base.SendAsync(request, cancellationToken);
            if (first.StatusCode != HttpStatusCode.Unauthorized)
                return first;

            // Không có bộ điều khiển trong Options thì rơi về tài khoản cấu hình. Ghi log vì đây gần
            // như luôn là dấu hiệu ai đó gọi HttpClient này mà quên gắn controller, và hệ quả là gõ
            // sai tài khoản xuống thiết bị — đúng thứ làm bộ đếm ngắt mạch chạy tới ngưỡng khoá IP.
            if (!request.Options.TryGetValue(VwISAPIDeviceClient.ControllerOptionKey, out var controller))
            {
                _logger.LogWarning(
                    "VwISAPIDigestHandler: request tới {Host} không gắn VwController, dùng tài khoản trong cấu hình.",
                    request.RequestUri?.Host);
            }

            var (account, password) = _credentialResolver.Resolve(controller);
            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("VwISAPIDigestHandler: chưa cấu hình tài khoản Digest cho {Host}", request.RequestUri?.Host);
                return first;
            }

            var challenge = ParseChallenge(first);
            if (challenge == null)
            {
                _logger.LogWarning(
                    "VwISAPIDigestHandler: không đọc được challenge Digest dùng được từ {Host}. Header: {Header}",
                    request.RequestUri?.Host,
                    first.Headers.TryGetValues("WWW-Authenticate", out var raw) ? string.Join(" | ", raw) : "(rỗng)");

                return first;
            }

            first.Dispose();

            var authorized = await SendAuthorized(request, body, challenge, account!, password!, cancellationToken);
            if (authorized.StatusCode != HttpStatusCode.Unauthorized)
                return authorized;

            // Nonce của thiết bị hết hạn giữa hai chặng — challenge mới kèm stale=TRUE xác nhận
            // mật khẩu vẫn đúng nên thử lại đúng MỘT lần, không rơi vào vòng lặp gõ sai mật khẩu.
            var renewed = ParseChallenge(authorized);
            if (renewed is not { Stale: true })
                return authorized;

            authorized.Dispose();
            return await SendAuthorized(request, body, renewed, account!, password!, cancellationToken);
        }

        #region Gửi lại request kèm Authorization

        private async Task<HttpResponseMessage> SendAuthorized(
            HttpRequestMessage original,
            byte[]? body,
            VwDigestChallenge challenge,
            string account,
            string password,
            CancellationToken ct)
        {
            using var retry = CloneRequest(original, body);
            retry.Headers.TryAddWithoutValidation("Authorization", BuildAuthorizationHeader(retry, challenge, account, password));

            return await base.SendAsync(retry, ct);
        }

        /// <summary>
        /// Đọc body ra mảng byte và gắn lại vào request: nội dung stream chỉ đọc được một lần,
        /// trong khi Digest luôn cần gửi lại nguyên request ở chặng thứ hai.
        /// </summary>
        private static async Task<byte[]?> BufferContent(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Content == null)
                return null;

            var body = await request.Content.ReadAsByteArrayAsync(ct);
            var contentType = request.Content.Headers.ContentType;

            var buffered = new ByteArrayContent(body);
            if (contentType != null)
                buffered.Headers.ContentType = contentType;

            request.Content = buffered;
            return body;
        }

        private static HttpRequestMessage CloneRequest(HttpRequestMessage original, byte[]? body)
        {
            var clone = new HttpRequestMessage(original.Method, original.RequestUri);

            foreach (var header in original.Headers)
            {
                if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
                    continue;

                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            foreach (var option in original.Options)
                ((IDictionary<string, object?>)clone.Options)[option.Key] = option.Value;

            if (body == null)
                return clone;

            clone.Content = new ByteArrayContent(body);
            var contentType = original.Content?.Headers.ContentType;
            if (contentType != null)
                clone.Content.Headers.ContentType = contentType;

            return clone;
        }

        #endregion

        #region Đọc challenge từ header WWW-Authenticate

        /// <summary>
        /// Chọn challenge dùng để tính hash. Header có thể chứa nhiều challenge nối nhau nên phải
        /// tách theo từ khoá "Digest" thay vì theo dấu phẩy, rồi ưu tiên MD5 vì đó là thuật toán
        /// được chốt cấu hình trên toàn bộ bộ điều khiển của hệ thống.
        /// </summary>
        private static VwDigestChallenge? ParseChallenge(HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues("WWW-Authenticate", out var values))
                return null;

            var raw = string.Join(", ", values);
            var parts = ChallengeSplitter.Split(raw).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            if (parts.Count == 0)
                return null;

            var candidates = parts.Select(Build).Where(c => c != null).Select(c => c!).ToList();
            if (candidates.Count == 0)
                return null;

            // Chỉ nhận thuật toán tự tính đúng được. Không fallback về candidates[0]: gặp biến thể
            // *-sess mà hash như MD5 thường thì thiết bị chỉ trả 401 mãi, và mỗi lần 401 lại cộng
            // vào bộ đếm ngắt mạch — hai lần là thiết bị khoá IP.
            return candidates.FirstOrDefault(c => c.Algorithm.Equals("MD5", StringComparison.OrdinalIgnoreCase))
                ?? candidates.FirstOrDefault(c => c.Algorithm.Equals("SHA-256", StringComparison.OrdinalIgnoreCase));
        }

        private static VwDigestChallenge? Build(string part)
        {
            var realm = ReadDirective(part, "realm");
            var nonce = ReadDirective(part, "nonce");
            if (string.IsNullOrWhiteSpace(realm) || string.IsNullOrWhiteSpace(nonce))
                return null;

            var stale = ReadDirective(part, "stale");

            return new VwDigestChallenge
            {
                Realm = realm!,
                Nonce = nonce!,
                Qop = ReadDirective(part, "qop"),
                Opaque = ReadDirective(part, "opaque"),
                // Thiếu algorithm thì theo RFC 7616 hiểu là MD5 — firmware thường không gửi trường này.
                Algorithm = ReadDirective(part, "algorithm") ?? "MD5",
                Stale = string.Equals(stale, "true", StringComparison.OrdinalIgnoreCase)
            };
        }

        /// <summary>Đọc một directive, chấp nhận cả dạng có nháy kép lẫn không nháy.</summary>
        private static string? ReadDirective(string challenge, string key)
        {
            var quoted = Regex.Match(challenge, key + @"\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase);
            if (quoted.Success)
                return quoted.Groups[1].Value;

            var bare = Regex.Match(challenge, key + @"\s*=\s*([^\s,""]+)", RegexOptions.IgnoreCase);
            return bare.Success ? bare.Groups[1].Value : null;
        }

        #endregion

        #region Tính header Authorization (HA1 / HA2 / response)

        private static string BuildAuthorizationHeader(
            HttpRequestMessage request,
            VwDigestChallenge challenge,
            string account,
            string password)
        {
            // PHẢI kèm query string: HA2 tính trên đúng chuỗi uri đã gửi, thiếu "?format=json"
            // là thiết bị trả 401 dù mật khẩu đúng.
            var uri = request.RequestUri!.PathAndQuery;
            var method = request.Method.Method;

            var ha1 = Hash(challenge.Algorithm, $"{account}:{challenge.Realm}:{password}");
            var ha2 = Hash(challenge.Algorithm, $"{method}:{uri}");

            var builder = new StringBuilder("Digest ");
            builder.Append($"username=\"{account}\", ");
            builder.Append($"realm=\"{challenge.Realm}\", ");
            builder.Append($"nonce=\"{challenge.Nonce}\", ");
            builder.Append($"uri=\"{uri}\", ");
            builder.Append($"algorithm={challenge.Algorithm}");

            string response;
            if (string.IsNullOrWhiteSpace(challenge.Qop))
            {
                response = Hash(challenge.Algorithm, $"{ha1}:{challenge.Nonce}:{ha2}");
            }
            else
            {
                var qop = SelectQop(challenge.Qop!);
                var cnonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
                response = Hash(challenge.Algorithm, $"{ha1}:{challenge.Nonce}:{NonceCount}:{cnonce}:{qop}:{ha2}");
                builder.Append($", qop={qop}, nc={NonceCount}, cnonce=\"{cnonce}\"");
            }

            builder.Append($", response=\"{response}\"");

            if (!string.IsNullOrWhiteSpace(challenge.Opaque))
                builder.Append($", opaque=\"{challenge.Opaque}\"");

            return builder.ToString();
        }

        /// <summary>
        /// Ưu tiên "auth". Thiết bị có thể chào cả "auth-int,auth"; lấy phần tử đầu danh sách là
        /// rơi vào auth-int — chế độ đó bắt buộc băm cả body vào HA2, không làm là response sai và
        /// chỉ biểu hiện thành 401 không rõ nguyên nhân.
        /// </summary>
        private static string SelectQop(string qop)
        {
            var options = qop.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return options.FirstOrDefault(o => o.Equals("auth", StringComparison.OrdinalIgnoreCase))
                ?? options[0];
        }

        private static string Hash(string algorithm, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var hash = algorithm.Equals("SHA-256", StringComparison.OrdinalIgnoreCase)
                ? SHA256.HashData(bytes)
                : MD5.HashData(bytes);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private sealed class VwDigestChallenge
        {
            public string Realm { get; init; } = string.Empty;

            public string Nonce { get; init; } = string.Empty;

            public string? Qop { get; init; }

            public string? Opaque { get; init; }

            public string Algorithm { get; init; } = "MD5";

            /// <summary>TRUE = mật khẩu đúng, chỉ nonce hết hạn ⇒ được phép thử lại.</summary>
            public bool Stale { get; init; }
        }

        #endregion
    }
}
