using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ITS.PA.Core.Configuration;
using ITS.PA.Core.Models;

namespace ITS.PA.Core.Integration;

/// <summary>
/// Gọi REST API máy chủ PA (ITC). Port ITC_PAControl_API của ITS008 sang async, giữ nguyên action code và
/// đường dẫn. Khác bản cũ:
///  - Token được giữ và tự đăng nhập lại một lần khi máy chủ trả 401/403 (cũ: lấy token mỗi lần Init).
///  - Mật khẩu được mã hóa URL khi đăng nhập (cũ: ghép thẳng vào query string).
///  - Lỗi được ghi log và trả null như cũ, không nuốt im lặng.
/// Author: ChuongLP
/// Created date: 31/10/2026
/// </summary>
public sealed class PaApiClient
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true,
        // Bản cũ dùng Newtonsoft, tự đổi qua lại số và chuỗi. Giữ độ dễ tính đó vì không có tài liệu API chính thức.
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new PaLenientStringConverter() },
    };

    private readonly HttpClient _http;
    private readonly PaOptions _options;
    private readonly ILogger<PaApiClient> _logger;
    private readonly SemaphoreSlim _loginLock = new(1, 1);
    private string? _token;

    public PaApiClient(HttpClient http, IOptions<PaOptions> options, ILogger<PaApiClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        // Chưa khai IP thì dịch vụ vẫn phải khởi động được; các lệnh sẽ trả lỗi và ghi log.
        if (!string.IsNullOrWhiteSpace(_options.Ip))
            _http.BaseAddress = new Uri($"http://{_options.Ip}:{_options.Port}/");
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "C# App");
    }

    /// <summary>Cũ: fn_GetLoginToken_API. GET {api}/auth?name=&amp;password= — thành công khi result = 200.</summary>
    public async Task<string?> LoginAsync(CancellationToken ct = default)
    {
        try
        {
            var url = $"{_options.ApiPath}/auth?name={Uri.EscapeDataString(_options.Username)}&password={Uri.EscapeDataString(_options.Password)}";
            using var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<PaApiResponse<string>>(body, Json);
            if (result?.Result == (int)HttpStatusCode.OK)
            {
                _token = result.Token;
                return _token;
            }

            _logger.LogWarning("Đăng nhập PA không thành công: result={Result} message={Message}", result?.Result, result?.ReturnMessage);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Đăng nhập PA lỗi.");
        }

        return null;
    }

    /// <summary>Cũ: fn_TerminalStatus_API (c2ls_get_server_terminals_status).</summary>
    public Task<PaTerminalStatusData?> GetTerminalStatusAsync(CancellationToken ct = default) =>
        ForwardAsync<string, PaTerminalStatusData>("c2ls_get_server_terminals_status", "", ct);

    /// <summary>Cũ: fn_GetLocalMusicList_API (c2ls_get_server_music_list).</summary>
    public Task<PaMusicListData?> GetLocalMusicListAsync(CancellationToken ct = default) =>
        ForwardAsync<string, PaMusicListData>("c2ls_get_server_music_list", "", ct);

    /// <summary>Cũ: fn_StartSongLocal_API (c2ls_mobile_terminal_damand_music).</summary>
    public Task<PaMusicResponse?> StartSongLocalAsync(PaDemandMusicRequest request, CancellationToken ct = default) =>
        ForwardAsync<PaDemandMusicRequest, PaMusicResponse>("c2ls_mobile_terminal_damand_music", request, ct);

    /// <summary>Cũ: fn_StartSongURL_API (c2ls_damand_media_stream).</summary>
    public Task<PaMusicResponse?> StartSongUrlAsync(PaUrlMusicRequest request, CancellationToken ct = default) =>
        ForwardAsync<PaUrlMusicRequest, PaMusicResponse>("c2ls_damand_media_stream", request, ct);

    /// <summary>Cũ: fn_GetTaskStatus_API (c2ls_get_task_status).</summary>
    public Task<PaTaskStatusData?> GetTaskStatusAsync(CancellationToken ct = default) =>
        ForwardAsync<string, PaTaskStatusData>("c2ls_get_task_status", "", ct);

    /// <summary>Cũ: fn_StopTask_API (c2ls_stop_task).</summary>
    public Task<PaStopTaskRequest?> StopTaskAsync(PaStopTaskRequest request, CancellationToken ct = default) =>
        ForwardAsync<PaStopTaskRequest, PaStopTaskRequest>("c2ls_stop_task", request, ct);

    /// <summary>
    /// Cũ: fn_TerminalGroups_API. GET {api}/terminals-groups/all/?withTerminals=true&amp;withGroups=false,
    /// token gửi qua header Authorization.
    /// </summary>
    public async Task<List<PaTerminalGroup>?> GetTerminalGroupsAsync(CancellationToken ct = default)
    {
        try
        {
            var url = $"{_options.ApiPath}/terminals-groups/all/?withTerminals=true&withGroups=false";
            var body = await SendAsync(token =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("Authorization", token);
                return request;
            }, ct).ConfigureAwait(false);

            return JsonSerializer.Deserialize<PaApiResponse<List<PaTerminalGroup>>>(body, Json)?.Data;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Lấy danh sách nhóm thiết bị PA lỗi.");
            return null;
        }
    }

    /// <summary>PUT {api}/ws/forwarder với gói { actioncode, token, data, company }.</summary>
    private async Task<TResponse?> ForwardAsync<TRequest, TResponse>(string actionCode, TRequest data, CancellationToken ct)
        where TRequest : class
        where TResponse : class
    {
        try
        {
            var body = await SendAsync(token =>
            {
                var envelope = new PaApiResponse<TRequest>
                {
                    ActionCode = actionCode,
                    Token = token,
                    Company = _options.Company,
                    Data = data,
                };
                return new HttpRequestMessage(HttpMethod.Put, $"{_options.ApiPath}/ws/forwarder")
                {
                    Content = new StringContent(JsonSerializer.Serialize(envelope, Json), Encoding.UTF8, "application/json"),
                };
            }, ct).ConfigureAwait(false);

            return JsonSerializer.Deserialize<PaApiResponse<TResponse>>(body, Json)?.Data;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Gọi PA {ActionCode} lỗi.", actionCode);
            return null;
        }
    }

    /// <summary>Gửi request; nếu máy chủ trả 401/403 thì đăng nhập lại một lần rồi gửi lại.</summary>
    private async Task<string> SendAsync(Func<string, HttpRequestMessage> build, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            var token = await EnsureTokenAsync(ct).ConfigureAwait(false);
            using var request = build(token);
            using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);

            if (attempt == 0 && response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                _token = null;
                continue;
            }

            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }
    }

    private async Task<string> EnsureTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_token)) return _token;

        await _loginLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return !string.IsNullOrEmpty(_token)
                ? _token
                : await LoginAsync(ct).ConfigureAwait(false)
                  ?? throw new InvalidOperationException("Không đăng nhập được máy chủ PA.");
        }
        finally
        {
            _loginLock.Release();
        }
    }
}
