using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Services.Vds.Configuration;

namespace Services.Vds.Features.Events.Automation;

/// <summary>Kết quả một lần nhờ TMS lập lịch tự động.</summary>
/// <param name="Called">Đã gọi được API (không tính lần bỏ qua vì chưa cấu hình).</param>
/// <param name="Scheduled">TMS đã tạo lần chạy tự động.</param>
/// <param name="ReasonCode">Mã lý do của TMS (<c>Scheduled</c>, <c>NoConfig</c>, <c>SourceNotAllowed</c>…).</param>
/// <param name="ExecuteDueAt">Thời điểm TMS sẽ chạy kịch bản.</param>
/// <param name="Retryable">Lỗi tạm thời (mạng, 5xx, 401 sau khi đăng nhập lại) ⇒ đáng gọi lại.</param>
public readonly record struct ScheduleAutomationResult(
    bool Called, bool Scheduled, string? ReasonCode, DateTime? ExecuteDueAt, bool Retryable)
{
    public bool Rejected { get; init; }

    public bool IsTerminalFailure => Called && !Scheduled && !Retryable
        && (Rejected || ReasonCode == "NotSucceeded" || ReasonCode?.StartsWith("Http", StringComparison.Ordinal) == true);

    public static ScheduleAutomationResult Skipped(string reason) => new(false, false, reason, null, false);

    public static ScheduleAutomationResult Transient(string reason) => new(true, false, reason, null, true);
}

/// <summary>
/// Author: KienLT
/// Description: Client gọi <c>POST {BaseUrl}{SchedulePath}</c> của WebAPI ITS để nhờ TMS lập lịch kịch bản
/// tự động cho sự cố VDS VỪA GHI. VDS chỉ TẠO sự cố; toàn bộ việc xử lý tiếp (đếm ngược, áp kịch bản, điều
/// khiển VMS/LCS, tự kết thúc) do TMS lo — logic đó nằm trong module TMS và không nên nhân bản sang đây.
///
/// <para>
/// <b>Xác thực.</b> Có <c>Service:TmsApi:Token</c> thì dùng luôn; ngược lại đăng nhập bằng
/// <c>Username</c>/<c>Password</c>, giữ token trong RAM và đăng nhập lại đúng MỘT lần khi gặp 401/403.
/// </para>
///
/// <para>
/// Hợp đồng đăng nhập nằm trong <c>Modules.System.dll</c> (repo không có source) nên ở đây chọn cách chịu
/// được sai lệch: đường dẫn login lấy từ cấu hình; body gửi CẢ hai tên trường thường dùng (<c>account</c>
/// và <c>userName</c>) vì model binding bỏ qua trường lạ; token đọc bằng cách dò khoá
/// <c>token</c>/<c>accessToken</c>/<c>access_token</c> ở MỌI tầng của JSON trả về, không phụ thuộc lớp bọc
/// <c>WResult</c>.
/// </para>
///
/// KHÔNG BAO GIỜ ném lỗi ra ngoài: sự cố đã nằm trong DB, mất một lần lập lịch không được phép làm đứt
/// pipeline hay chết worker.
/// Created date: 26/08/2026
/// </summary>
public sealed class IncidentAutomationApiClient
{
    /// <summary>Tên HttpClient trong <see cref="IHttpClientFactory"/> — handler được dùng lại, không cạn socket.</summary>
    public const string HttpClientName = "vds-tms-api";

    /// <summary>HttpClient KHÔNG kiểm tra chứng chỉ máy chủ — chỉ dùng khi <c>TmsApi:AllowSelfSignedCert</c> bật.</summary>
    public const string InsecureHttpClientName = "vds-tms-api-insecure";

    /// <summary>Khoá JSON có thể chứa token trong phản hồi đăng nhập.</summary>
    private static readonly string[] TokenKeys = { "accessToken", "access_token", "token" };

    private readonly IHttpClientFactory? _factory;
    private readonly HttpClient? _fixedClient;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<IncidentAutomationApiClient> _logger;

    /// <summary>Chỉ một luồng được đăng nhập tại một thời điểm; các luồng khác dùng token vừa lấy được.</summary>
    private readonly SemaphoreSlim _loginGate = new(1, 1);

    private string? _token;
    private bool _warnedNotConfigured;

    public IncidentAutomationApiClient(
        IHttpClientFactory factory, IVdsConfigManager config, ILogger<IncidentAutomationApiClient> logger)
    {
        _factory = factory;
        _config = config;
        _logger = logger;
    }

    /// <summary>Dùng cho smoke/test: bơm trực tiếp <see cref="HttpClient"/> có handler giả.</summary>
    internal IncidentAutomationApiClient(
        HttpClient client, IVdsConfigManager config, ILogger<IncidentAutomationApiClient> logger)
    {
        _fixedClient = client;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Client cho một lượt gọi. Object này là singleton để giữ TOKEN, còn HttpClient lấy từ factory mỗi
    /// lượt — đúng khuyến nghị của .NET: giữ một HttpClient dài hạn thì mất DNS refresh, mà tạo handler mới
    /// mỗi lượt thì cạn socket.
    /// </summary>
    // Handler của IHttpClientFactory cố định theo TÊN client, còn cấu hình thì đổi lúc chạy ⇒ chọn tên theo cờ ở mỗi lượt gọi.
    private HttpClient HttpFor(TmsApiOptions options) => _fixedClient
        ?? _factory!.CreateClient(options.AllowSelfSignedCert ? InsecureHttpClientName : HttpClientName);

    /// <summary>
    /// Nhờ TMS lập lịch tự động cho một sự cố. Trả kết quả kèm cờ <c>Retryable</c> để worker biết có nên gọi
    /// lại hay không — lý do NGHIỆP VỤ (chưa bật tự động, nguồn không được phép) thì gọi lại chỉ tốn request
    /// và rác log.
    /// </summary>
    public async Task<ScheduleAutomationResult> ScheduleAsync(string incidentId, CancellationToken ct)
    {
        var options = _config.Current.TmsApi;

        if (!options.IsUsable) return NotConfigured(options);
        if (string.IsNullOrWhiteSpace(incidentId)) return ScheduleAutomationResult.Skipped("EmptyIncidentId");

        var uri = options.BuildUri(options.SchedulePath);
        if (uri is null)
        {
            _logger.LogError("[TmsApi] BaseUrl/SchedulePath không hợp lệ: '{Base}' + '{Path}'.",
                options.BaseUrl, options.SchedulePath);
            return ScheduleAutomationResult.Skipped("InvalidUrl");
        }

        try
        {
            var body = JsonConvert.SerializeObject(new
            {
                incidentId,
                triggerState = options.TriggerState,
                actor = options.Actor,
                oncePerIncident = true,
            });

            var response = await SendWithAuthAsync(uri, body, options, ct).ConfigureAwait(false);
            if (response is null) return ScheduleAutomationResult.Transient("NoToken");

            using (response)
            {
                var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    _logger.LogError(
                        "[TmsApi] Bị từ chối xác thực ({Status}) khi lập lịch sự cố {Incident}. Kiểm tra " +
                        "Service:TmsApi (Token hoặc Username/Password) và quyền của tài khoản.",
                        (int)response.StatusCode, incidentId);
                    return ScheduleAutomationResult.Transient("Unauthorized");
                }

                if (!response.IsSuccessStatusCode)
                {
                    // 5xx, 408 và 429 là lỗi tạm thời ⇒ đáng gọi lại. 4xx còn lại là yêu cầu của
                    // mình sai, gọi lại bao nhiêu lần cũng vẫn sai.
                    var retryable = (int)response.StatusCode >= 500
                                    || response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests;

                    _logger.LogWarning("[TmsApi] Lập lịch sự cố {Incident} trả về {Status}: {Body}",
                        incidentId, (int)response.StatusCode, Truncate(payload));

                    return new ScheduleAutomationResult(
                        true, false, $"Http{(int)response.StatusCode}", null, retryable);
                }

                return LogParsed(payload, incidentId);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Mạng chập, DNS, timeout: đáng gọi lại.
            _logger.LogWarning(ex, "[TmsApi] Gọi lập lịch cho sự cố {Incident} thất bại.", incidentId);
            return ScheduleAutomationResult.Transient("RequestFailed");
        }
    }

    /// <summary>Gửi yêu cầu kèm Bearer token; gặp 401/403 thì đăng nhập lại và thử ĐÚNG một lần.</summary>
    private async Task<HttpResponseMessage?> SendWithAuthAsync(
        Uri uri, string body, TmsApiOptions options, CancellationToken ct)
    {
        var token = await EnsureTokenAsync(options, staleToken: null, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token)) return null;

        var response = await PostAsync(uri, body, token!, options, ct).ConfigureAwait(false);
        if (response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden))
            return response;

        // Token hết hạn giữa hai lần gọi là chuyện bình thường của service chạy dài ngày. Chỉ đăng nhập lại
        // khi có tài khoản; token khai tay thì không có gì để làm mới.
        if (string.IsNullOrWhiteSpace(options.Username)) return response;

        response.Dispose();

        var renewed = await EnsureTokenAsync(options, staleToken: token, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(renewed)) return null;

        return await PostAsync(uri, body, renewed!, options, ct).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> PostAsync(
        Uri uri, string body, string token, TmsApiOptions options, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.EffectiveTimeoutSeconds));

        return await HttpFor(options).SendAsync(request, timeout.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Token đang dùng. <paramref name="staleToken"/> khác null nghĩa là token đó vừa bị từ chối ⇒ phải lấy
    /// token mới; nếu trong lúc chờ khoá có luồng khác đã làm mới rồi thì dùng luôn token của họ, không đăng
    /// nhập hai lần cho cùng một lần hết hạn.
    /// </summary>
    private async Task<string?> EnsureTokenAsync(
        TmsApiOptions options, string? staleToken, CancellationToken ct)
    {
        // Token khai tay: dùng nguyên, không bao giờ tự làm mới.
        if (!string.IsNullOrWhiteSpace(options.Token)) return options.Token.Trim();

        var current = Volatile.Read(ref _token);
        if (staleToken is null && !string.IsNullOrWhiteSpace(current)) return current;

        await _loginGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            current = Volatile.Read(ref _token);
            if (!string.IsNullOrWhiteSpace(current)
                && !string.Equals(current, staleToken, StringComparison.Ordinal))
            {
                return current;
            }

            var fresh = await LoginAsync(options, ct).ConfigureAwait(false);
            Volatile.Write(ref _token, fresh);
            return fresh;
        }
        finally
        {
            _loginGate.Release();
        }
    }

    private async Task<string?> LoginAsync(TmsApiOptions options, CancellationToken ct)
    {
        var uri = options.BuildUri(options.LoginPath);
        if (uri is null)
        {
            _logger.LogError("[TmsApi] LoginPath không hợp lệ: '{Path}'.", options.LoginPath);
            return null;
        }

        try
        {
            // Gửi cả hai tên trường tài khoản: hợp đồng login nằm trong DLL nên không chắc tên nào đúng, mà
            // trường lạ thì model binding bỏ qua — gửi cả hai rẻ hơn đoán sai rồi mất tính năng.
            var body = JsonConvert.SerializeObject(new
            {
                account = options.Username,
                userName = options.Username,
                password = options.Password,
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(options.EffectiveTimeoutSeconds));

            using var response = await HttpFor(options).SendAsync(request, timeout.Token).ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[TmsApi] Đăng nhập tài khoản dịch vụ '{User}' thất bại ({Status}): {Body}",
                    options.Username, (int)response.StatusCode, Truncate(payload));
                return null;
            }

            var token = ExtractToken(payload);
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogError(
                    "[TmsApi] Phản hồi đăng nhập không có khoá {Keys}. Kiểm tra Service:TmsApi:LoginPath. " +
                    "Body: {Body}",
                    string.Join('/', TokenKeys), Truncate(payload));
                return null;
            }

            _logger.LogInformation("[TmsApi] Đăng nhập tài khoản dịch vụ '{User}' thành công.", options.Username);
            return token;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TmsApi] Lỗi đăng nhập tài khoản dịch vụ '{User}'.", options.Username);
            return null;
        }
    }

    /// <summary>
    /// Dò token trong JSON bất kể lớp bọc: WebAPI trả <c>WResult</c> (<c>{ succeeded, result: {…} }</c>) nên
    /// token nằm sâu một tầng. Hàm THUẦN (public để kiểm được mà không cần mạng).
    /// </summary>
    public static string? ExtractToken(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            var root = JToken.Parse(json);

            // Duyệt theo THỨ TỰ KHOÁ, không theo thứ tự xuất hiện: 'accessToken' là tên chuẩn nhất, còn
            // 'token' hay bị dùng cho refresh token nên để cuối.
            foreach (var key in TokenKeys)
            {
                var found = PropertiesOf(root).FirstOrDefault(p =>
                    string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase)
                    && p.Value.Type == JTokenType.String
                    && !string.IsNullOrWhiteSpace(p.Value.Value<string>()));

                if (found is not null) return found.Value.Value<string>();
            }
        }
        catch (JsonException)
        {
            // Body không phải JSON ⇒ coi như không có token.
        }

        return null;
    }

    /// <summary>
    /// Đọc kết quả lập lịch. Chấp nhận cả dạng có lớp bọc <c>WResult</c> và dạng phẳng — hàm THUẦN (public
    /// để kiểm được mà không cần mạng).
    ///
    /// Không xác nhận được kết quả thì giữ pending để retry trong retention; không được coi là đã bàn giao.
    /// </summary>
    public static ScheduleAutomationResult ParseScheduleResponse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return ScheduleAutomationResult.Transient("EmptyResponse");

        try
        {
            var root = JToken.Parse(json);
            var scheduled = FindValue<bool?>(root, "scheduled");
            var reason = FindValue<string>(root, "reasonCode");
            var dueAt = FindValue<DateTime?>(root, "executeDueAt");
            if (reason is "InsertFailed" or "UnexpectedError" or "EventWindowPending")
                return new ScheduleAutomationResult(true, false, reason, null, true);

            // succeeded = false của lớp bọc nghĩa là WebAPI trả lỗi nghiệp vụ (validate, quyền…).
            var succeeded = FindValue<bool?>(root, "succeeded");
            if (succeeded == false)
            {
                var code = FindValue<int?>(root, "code");
                var retryable = code >= 500 || code is 408 or 429 or 401 or 403;
                return new ScheduleAutomationResult(true, false, reason ?? "NotSucceeded", null, retryable) { Rejected = true };
            }

            if (scheduled is null || (scheduled == false && string.IsNullOrWhiteSpace(reason)))
                return ScheduleAutomationResult.Transient("UnrecognizedResponse");

            return new ScheduleAutomationResult(true, scheduled == true, reason, dueAt, false);
        }
        catch (JsonException)
        {
            return ScheduleAutomationResult.Transient("InvalidResponse");
        }
    }

    private ScheduleAutomationResult LogParsed(string payload, string incidentId)
    {
        var result = ParseScheduleResponse(payload);

        if (result.Scheduled)
        {
            _logger.LogInformation("[TmsApi] Sự cố {Incident} đã được lập lịch tự động, chạy lúc {DueAt}.",
                incidentId, result.ExecuteDueAt);
        }
        else
        {
            _logger.LogInformation("[TmsApi] Sự cố {Incident} KHÔNG lập lịch tự động: {Reason}.",
                incidentId, result.ReasonCode ?? "(không rõ)");
        }

        return result;
    }

    /// <summary>Cảnh báo MỘT lần khi chưa khai đủ cấu hình, rồi im lặng bỏ qua các lần sau.</summary>
    private ScheduleAutomationResult NotConfigured(TmsApiOptions options)
    {
        if (!_warnedNotConfigured)
        {
            _warnedNotConfigured = true;

            if (!options.Enabled)
            {
                _logger.LogInformation(
                    "[TmsApi] Service:TmsApi:Enabled = false ⇒ sự cố VDS không được nhờ TMS lập lịch tự động.");
            }
            else
            {
                _logger.LogWarning(
                    "[TmsApi] Thiếu cấu hình (BaseUrl='{Base}', có credential={HasCred}) ⇒ bỏ qua việc lập " +
                    "lịch tự động.",
                    options.BaseUrl, options.HasCredential);
            }
        }

        return ScheduleAutomationResult.Skipped(options.Enabled ? "NotConfigured" : "Disabled");
    }

    private static T? FindValue<T>(JToken root, string name)
    {
        var property = PropertiesOf(root)
            .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        if (property is null || property.Value.Type is JTokenType.Null or JTokenType.Undefined) return default;

        try { return property.Value.Value<T>(); }
        catch (Exception) { return default; }
    }

    /// <summary>
    /// Mọi property ở MỌI tầng của JSON. Phải dò theo tầng vì WebAPI bọc kết quả trong <c>WResult</c>
    /// (<c>{ succeeded, code, result: { … } }</c>) và lớp bọc có thể đổi mà không báo trước.
    /// </summary>
    private static IEnumerable<JProperty> PropertiesOf(JToken? token)
        => token is JContainer container
            ? container.DescendantsAndSelf().OfType<JProperty>()
            : Enumerable.Empty<JProperty>();

    private static string Truncate(string? text, int max = 300)
        => string.IsNullOrEmpty(text) ? "" : text.Length <= max ? text : text[..max] + "…";
}
