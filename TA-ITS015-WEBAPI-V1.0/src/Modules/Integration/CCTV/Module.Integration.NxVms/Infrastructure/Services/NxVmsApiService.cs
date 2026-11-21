using Azure.Core;
using Furion.FriendlyException;
using Microsoft.Extensions.Configuration;
using Module.Integration.NxVms.Infrastructure.Dto;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Module.Integration.CCTV.Infrastructure.Services;

/// <summary>
/// Gọi trực tiếp các endpoint REST của NxVMS.
/// Xử lý Bearer token và tự động retry một lần khi nhận 401 Unauthorized.
/// Không biết gì về logic nghiệp vụ — chỉ gửi HTTP và trả raw data.
///
/// ─────────────────────────────────────────────────────────────────────────
/// QUY HOẠCH MÃ LỖI NXVMS — format thông điệp: "{mã}, {mô tả}"
///   101  Xác thực NxVMS thất bại (401 vẫn lỗi sau khi làm mới token)
///   102  NxVMS trả về lỗi khi gọi API (upstream error, kèm errorString của hãng)
///   103  Snapshot từ NxVMS thất bại
///   104  Không lấy được vị trí PTZ (position)
///   105  Không lấy được giới hạn PTZ (limits)
///   106  Mất kết nối NxVMS khi gửi lệnh điều khiển (POST/PATCH/DELETE)
/// Lưu ý: lỗi mất kết nối (host down/timeout) với lệnh ĐỌC (GET/snapshot) KHÔNG throw — trả null/empty
///        để tầng trên xử lý mềm (giữ dữ liệu cũ, không xóa nhầm).
///        Lệnh GHI (POST/PATCH/DELETE: PTZ, preset, tour) THROW 106 để FE biết lệnh chưa tới camera
///        (tránh báo "dừng thành công" khi camera vẫn đang quay).
/// ─────────────────────────────────────────────────────────────────────────
/// </summary>
public class NxVmsApiService
{
    private readonly HttpClient _httpClient;
    private readonly NxVmsAuthService _authService;
    private readonly NxVmsConfigService _configService;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string CommandConnectionLostMessage = "106, Không kết nối được tới NxVMS, lệnh chưa được gửi tới camera. Vui lòng thử lại.";

    public NxVmsApiService(HttpClient httpClient, NxVmsAuthService authService, NxVmsConfigService configService)
    {
        _httpClient = httpClient;
        _authService = authService;
        _configService = configService;
    }

    #region Phương Thức API Gọi HTTP 
    /// <summary>
    /// GET endpoint và deserialize kết quả về kiểu T.
    /// Nếu nhận 401, xóa token cũ và thử lại một lần.
    /// </summary>
    /// Author: ChuongLP
    /// Date: 2026-05-29
    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        var token = await _authService.GetTokenAsync(cancellationToken);
        var fullEndpoint = await BuildEndpointAsync(endpoint);
        var response = await SendGetAsync(fullEndpoint, token, cancellationToken);

        // Không kết nối được tới NxVMS → không throw, trả default.
        if (response == null)
            return default;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _authService.ClearToken();
            token = await _authService.GetTokenAsync(cancellationToken);

            var retryResponse = await SendGetAsync(fullEndpoint, token, cancellationToken);
            if (retryResponse == null)
                return default;

            var retryBody = await retryResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!retryResponse.IsSuccessStatusCode)
            {
                var err = JsonSerializer.Deserialize<NxVmsErrorResponse>(retryBody, _jsonOptions);
                throw Oops.Oh($"101, Xác thực NxVMS thất bại (sau khi làm mới token): {err?.ErrorString ?? retryBody}");
            }

            return string.IsNullOrWhiteSpace(retryBody)
                ? default
                : JsonSerializer.Deserialize<T>(retryBody, _jsonOptions);
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = JsonSerializer.Deserialize<NxVmsErrorResponse>(body, _jsonOptions);
            throw Oops.Oh($"102, NxVMS trả về lỗi: {err?.ErrorString ?? body}");
        }

        return string.IsNullOrWhiteSpace(body)
            ? default
            : JsonSerializer.Deserialize<T>(body, _jsonOptions);
    }

    /// <summary>
    /// POST endpoint với body JSON.
    /// Nếu nhận 401, xóa token cũ và thử lại một lần.
    /// </summary>
    public async Task<string?> PostAsync(string endpoint, object bodyData, CancellationToken cancellationToken = default)
    {
        var token = await _authService.GetTokenAsync(cancellationToken);
        var fullEndpoint = await BuildEndpointAsync(endpoint);
        var response = await SendPostAsync(fullEndpoint, token, bodyData, cancellationToken);

        // Không kết nối được tới NxVMS → lệnh ghi phải báo lỗi, không trả null.
        if (response == null)
            throw Oops.Oh(CommandConnectionLostMessage);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _authService.ClearToken();
            token = await _authService.GetTokenAsync(cancellationToken);

            var retryResponse = await SendPostAsync(fullEndpoint, token, bodyData, cancellationToken);
            if (retryResponse == null)
                throw Oops.Oh(CommandConnectionLostMessage);

            var retryBody = await retryResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!retryResponse.IsSuccessStatusCode)
            {
                var err = JsonSerializer.Deserialize<NxVmsErrorResponse>(retryBody, _jsonOptions);
                throw Oops.Oh($"101, Xác thực NxVMS thất bại (sau khi làm mới token): {err?.ErrorString ?? retryBody}");
            }

            return retryBody;
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = JsonSerializer.Deserialize<NxVmsErrorResponse>(body, _jsonOptions);
            throw Oops.Oh($"102, NxVMS trả về lỗi: {err?.ErrorString ?? body}");
        }

        return body;
    }

    /// <summary>
    /// DELETE endpoint. Tự động retry 401 giống GetAsync/PostAsync.
    /// </summary>
    /// Author: ChuongLP
    /// Date: 2026-05-29
    public async Task<string?> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        var token = await _authService.GetTokenAsync(cancellationToken);
        var fullEndpoint = await BuildEndpointAsync(endpoint);
        var response = await SendDeleteAsync(fullEndpoint, token, cancellationToken);

        // Không kết nối được tới NxVMS → lệnh ghi phải báo lỗi, không trả null.
        if (response == null)
            throw Oops.Oh(CommandConnectionLostMessage);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _authService.ClearToken();
            token = await _authService.GetTokenAsync(cancellationToken);

            var retry = await SendDeleteAsync(fullEndpoint, token, cancellationToken);
            if (retry == null)
                throw Oops.Oh(CommandConnectionLostMessage);

            var retryBody = await retry.Content.ReadAsStringAsync(cancellationToken);

            if (!retry.IsSuccessStatusCode)
            {
                var err = JsonSerializer.Deserialize<NxVmsErrorResponse>(retryBody, _jsonOptions);
                throw Oops.Oh($"101, Xác thực NxVMS thất bại (sau khi làm mới token): {err?.ErrorString ?? retryBody}");
            }

            return retryBody;
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = JsonSerializer.Deserialize<NxVmsErrorResponse>(body, _jsonOptions);
            throw Oops.Oh($"102, NxVMS trả về lỗi: {err?.ErrorString ?? body}");
        }

        return body;
    }

    /// <summary>
    /// PATCH endpoint với body JSON. Retry 401 giống PostAsync.
    /// Author: ChuongLP
    /// Date: 2026-06-13
    /// </summary>
    public async Task<string?> PatchAsync(string endpoint, object bodyData, CancellationToken cancellationToken = default)
    {
        var token = await _authService.GetTokenAsync(cancellationToken);
        var fullEndpoint = await BuildEndpointAsync(endpoint);
        var response = await SendPatchAsync(fullEndpoint, token, bodyData, cancellationToken);

        // Không kết nối được tới NxVMS → lệnh ghi phải báo lỗi, không trả null.
        if (response == null)
            throw Oops.Oh(CommandConnectionLostMessage);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _authService.ClearToken();
            token = await _authService.GetTokenAsync(cancellationToken);

            var retry = await SendPatchAsync(fullEndpoint, token, bodyData, cancellationToken);
            if (retry == null)
                throw Oops.Oh(CommandConnectionLostMessage);

            var retryBody = await retry.Content.ReadAsStringAsync(cancellationToken);

            if (!retry.IsSuccessStatusCode)
            {
                var err = JsonSerializer.Deserialize<NxVmsErrorResponse>(retryBody, _jsonOptions);
                throw Oops.Oh($"101, Xác thực NxVMS thất bại (sau khi làm mới token): {err?.ErrorString ?? retryBody}");
            }

            return retryBody;
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = JsonSerializer.Deserialize<NxVmsErrorResponse>(body, _jsonOptions);
            throw Oops.Oh($"102, NxVMS trả về lỗi: {err?.ErrorString ?? body}");
        }

        return body;
    }

    /// <summary>
    /// Lấy ảnh thumbnail/snapshot từ camera.
    /// Author: ChuongLP
    /// Date: 2026-05-29
    /// </summary>
    public async Task<(byte[] Data, string ContentType)> GetSnapshotAsync(NxVmsSnapshotInput input, CancellationToken ct = default)
    {
        var token = await _authService.GetTokenAsync(ct);

        var endpoint = BuildSnapshotEndpoint(input);
        var fullEndpoint = await BuildEndpointAsync(endpoint);
        var response = await SendGetRawAsync(fullEndpoint, token, ct);

        // Không kết nối được tới NxVMS → không throw, trả rỗng để tầng trên tự xử lý (giữ ảnh cũ).
        if (response == null)
            return (Array.Empty<byte>(), string.Empty);

        // Retry 1 lần nếu 401
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _authService.ClearToken();
            token = await _authService.GetTokenAsync(ct);
            response = await SendGetRawAsync(fullEndpoint, token, ct);

            if (response == null)
                return (Array.Empty<byte>(), string.Empty);

            if (!response.IsSuccessStatusCode)
                throw Oops.Oh($"103, Snapshot NxVMS thất bại sau khi làm mới token (HTTP {(int)response.StatusCode}).");
        }
        else if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var err = JsonSerializer.Deserialize<NxVmsErrorResponse>(body, _jsonOptions);
            throw Oops.Oh($"103, Snapshot NxVMS thất bại: {err?.ErrorString ?? body}");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var contentType = response.Content.Headers.ContentType?.MediaType ?? $"image/{input.Format}";

        return (bytes, contentType);
    }

    /// <summary>
    /// Hàm gửi DELETE đến NxVMS. Không deserialize JSON, chỉ trả raw body.
    /// Nếu không kết nối được tới NxVMS (host down/timeout) → trả null thay vì throw.
    /// </summary>
    /// Author: ChuongLP
    /// Date: 2026-05-29
    private async Task<HttpResponseMessage?> SendDeleteAsync(string endpoint, string? token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await SendWithConnectionGuardAsync(request, ct);
    }

    /// <summary>
    /// Hàm gửi GET đến NxVMS.
    /// Nếu không kết nối được tới NxVMS (host down/timeout) → trả null thay vì throw.
    /// </summary>
    /// Author: ChuongLP
    /// Date: 2026-05-29
    private async Task<HttpResponseMessage?> SendGetAsync(string endpoint, string? token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await SendWithConnectionGuardAsync(request, ct);
    }

    /// <summary>
    /// Gửi POST đến NxVMS với body JSON.
    /// Nếu không kết nối được tới NxVMS (host down/timeout) → trả null thay vì throw.
    /// </summary>
    /// Author: ChuongLP
    /// Date: 2026-05-29
    private async Task<HttpResponseMessage?> SendPostAsync(string endpoint, string? token, object bodyData, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var json = JsonSerializer.Serialize(bodyData);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        return await SendWithConnectionGuardAsync(request, ct);
    }

    /// <summary>
    /// Gửi PATCH đến NxVMS với body JSON.
    /// Nếu không kết nối được tới NxVMS (host down/timeout) → trả null thay vì throw.
    /// Author: ChuongLP
    /// Date: 2026-06-13
    /// </summary>
    private async Task<HttpResponseMessage?> SendPatchAsync(string endpoint, string? token, object bodyData, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var json = JsonSerializer.Serialize(bodyData);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return await SendWithConnectionGuardAsync(request, ct);
    }

    /// <summary>
    /// Wrapper dùng chung cho mọi request: bắt lỗi không kết nối được tới NxVMS
    /// (host down, timeout, refused...) và trả null thay vì để exception rơi ra ngoài.
    /// Nếu cancellationToken bị hủy chủ động thì vẫn ném lại như bình thường.
    /// Author: ChuongLP
    /// Date: 2026-07-30
    /// </summary>
    private async Task<HttpResponseMessage?> SendWithConnectionGuardAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            return await _httpClient.SendAsync(request, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException
                                   or TaskCanceledException
                                   or System.Net.Sockets.SocketException)
        {
            _authService.ClearToken();
            return null;
        }
    }

    public Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
        => _authService.GetTokenAsync(cancellationToken);

    #endregion

    // ── Device ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Lấy danh sách thiết bị từ NxVMS.
    /// Sau khi nhận response, tự động trích IP từ field Url và gán vào Ip.
    /// </summary>
    /// Author: ChuongLP
    /// Date: 2026-05-29
    public async Task<List<NxVmsDeviceOutput>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<List<NxVmsDeviceOutput>>("/rest/v4/devices", cancellationToken);

        if (result == null)
            throw new HttpRequestException("Không nhận được danh sách thiết bị NxVMS.");

        foreach (var item in result)
            item.Ip = ExtractIpFromUrl(item.Url);

        return result;
    }

    #region Media
    // ── Media URL (build chuỗi, không gọi HTTP) ───────────────────────────────
    public async Task<string> BuildMediaUrl(NxVmsMediaUrlInput input)
    {
        var baseUrl = await _configService.GetBaseUrlAsync();

        var url = $"{baseUrl}/media/{input.DeviceId}.{input.Format}" +
                  $"?stream={input.Stream}&resolution={input.Resolution}";

        if (input.DurationMs.HasValue) url += $"&duration={input.DurationMs.Value}";
        if (input.PositionMs.HasValue) url += $"&pos={input.PositionMs.Value}";

        if (input.Download)
            url += "&download=true";

        return url;
    }

    /// <summary>
    /// Build WSS URL cho WebRTC theo endpoint REST v4 chuẩn của NxVMS.
    /// PositionMs null = live, có giá trị = playback.
    /// Auth token được đính kèm vào URL để FE kết nối trực tiếp.
    /// </summary>
    /// Author: ChuongLP
    /// Date: 2026-05-29
    public async Task<string> BuildWebRtcStreamUrlAsync(NxVmsWebRtcUrlInput input)
    {
        var baseUrl = (await _configService.GetBaseUrlAsync()).TrimEnd('/')
           .Replace("https://", "wss://")
           .Replace("http://", "ws://");


        var url = $"{baseUrl}/rest/v4/devices/{input.DeviceId}/webrtc" +
                  $"?stream={input.Stream}" +
                  $"&resolution={input.Resolution}";

        // Thêm positionMs nếu là playback
        if (input.PositionMs.HasValue) url += $"&positionMs={input.PositionMs.Value}";

        return url;
    }

    /// <summary>
    /// Build HTTPS URL để FE tải file video archive từ NxVMS.
    /// Bắt buộc: positionMs, durationMs, videoCodec=h264, download=true.
    /// </summary>
    /// Author: ChuongLP
    /// Date: 2026-05-29
    public async Task<string> BuildMediaDownloadUrlAsync(NxVmsMediaDownloadInput input, CancellationToken cancellationToken = default)
    {
        var baseUrl = (await _configService.GetBaseUrlAsync()).TrimEnd('/') ?? string.Empty;

        return $"{baseUrl}/rest/v4/devices/{input.DeviceId}/media" +
               $"?positionMs={input.PositionMs}" +
               $"&durationMs={input.DurationMs}" +
               $"&stream={input.Stream}" +
               $"&resolution={input.Resolution}" +
               $"&videoCodec={input.VideoCodec}" +
               $"&download=true";
    }

    /// <summary>
    /// URL đầy đủ ảnh snapshot (không gọi HTTP, không kèm token) — dùng để đổi sang URL Cctv Proxy.
    /// Author: kien.le
    /// Date: 2026-10-06
    /// </summary>
    public Task<string> BuildSnapshotUrlAsync(NxVmsSnapshotInput input)
    {
        return BuildEndpointAsync(BuildSnapshotEndpoint(input));
    }

    /// <summary>
    /// Xây dựng endpoint URL cho snapshot với các query parameters dựa trên input.
    /// Author: ChuongLP
    /// Date: 2026-05-29
    /// </summary>
    private string BuildSnapshotEndpoint(NxVmsSnapshotInput input)
    {
        var query = new List<string>();

        // Null → không truyền → NxVMS lấy frame mới nhất
        // -1   → truyền tường minh "latest"
        if (input.TimestampMs.HasValue)
            query.Add($"timestampMs={input.TimestampMs.Value}");

        if (!string.IsNullOrWhiteSpace(input.Size))
            query.Add($"size={input.Size}");

        if (!string.IsNullOrWhiteSpace(input.Format))
            query.Add($"format={input.Format}");

        if (!string.IsNullOrWhiteSpace(input.Crop))
            query.Add($"crop={input.Crop}");

        if (input.Rotation.HasValue)
            query.Add($"rotation={input.Rotation.Value}");

        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return $"/rest/v4/devices/{input.DeviceId}/image{queryString}";
    }

    /// <summary>
    /// Gửi GET và trả về raw HttpResponseMessage để đọc binary.
    /// Không dùng "using" vì cần đọc Content sau khi return.
    /// Nếu không kết nối được tới NxVMS (host down/timeout) → trả null thay vì throw.
    /// </summary>
    private async Task<HttpResponseMessage?> SendGetRawAsync(string endpoint, string? token, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await SendWithConnectionGuardAsync(request, ct);
    }

    /// <summary>
    /// GET danh sách footage (các chunk đã ghi) của một device.
    /// Chỉ truyền query khi có giá trị — NxVMS bỏ qua param lạ nên phải build cẩn thận.
    /// Author: ChuongLP
    /// Date: 2026-06-17
    /// </summary>
    public async Task<List<NxVmsFootageResponse>?> GetFootageAsync(NxVmsFootageInput input, CancellationToken ct = default)
    {
        var query = new List<string>();

        if (input.StartTimeMs.HasValue) query.Add($"startTimeMs={input.StartTimeMs.Value}");
        if (input.EndTimeMs.HasValue) query.Add($"endTimeMs={input.EndTimeMs.Value}");
        if (input.DetailLevelMs.HasValue) query.Add($"detailLevelMs={input.DetailLevelMs.Value}");
        if (!string.IsNullOrWhiteSpace(input.PeriodType)) query.Add($"periodType={input.PeriodType}");

        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;

        return await GetAsync<List<NxVmsFootageResponse>>(
            $"/rest/v4/devices/{input.DeviceId}/footage{queryString}", ct);
    }

    #endregion

    #region PTZ
    // ── PTZ — đọc trạng thái ──────────────────────────────────────────────────
    /// <summary>
    /// Gọi endpoint lấy vị trí PTZ hiện tại của camera.
    /// </summary>
    /// Author: ChuongLP
    /// Date: 2026-05-29
    public async Task<NxVmsPtzPositionResponse?> GetPtzPositionAsync(string deviceId, CancellationToken ct = default)
    {
        return await GetAsync<NxVmsPtzPositionResponse>($"/rest/v4/devices/{deviceId}/ptz/position", ct);
    }

    /// <summary>
    /// Lấy giới hạn PTZ của camera, bao gồm min/max pan/tilt/zoom/focus.
    /// </summary>
    /// Author: ChuongLP
    /// Date: 2026-05-29
    public async Task<NxVmsPtzLimitsResponse?> GetPtzLimitsAsync(string deviceId, CancellationToken ct = default)
    {
        return await GetAsync<NxVmsPtzLimitsResponse>($"/rest/v4/devices/{deviceId}/ptz/limits", ct);
    }

    // ── PTZ — Preset ──────────────────────────────────────────────────────────
    /// <summary>
    /// Lấy danh sách preset PTZ của camera. Mỗi preset gồm presetId và name.
    /// </summary>
    /// Author: ChuongLP
    /// Date: 2026-05-29
    public async Task<List<NxVmsPtzPresetResponse>?> GetPresetsAsync(string deviceId, CancellationToken ct = default)
    {
        return await GetAsync<List<NxVmsPtzPresetResponse>>($"/rest/v4/devices/{deviceId}/ptz/presets", ct);
    }

    /// <summary>
    /// Xóa một preset PTZ của camera.
    /// </summary>
    /// Author: ChuongLP
    /// Date: 2026-05-29
    public async Task<string?> DeletePresetAsync(string deviceId, string presetId, CancellationToken ct = default)
    {
        return await DeleteAsync($"/rest/v4/devices/{deviceId}/ptz/presets/{Uri.EscapeDataString(presetId ?? string.Empty)}", ct);
    }

    /// <summary>
    /// POST tạo preset mới. Body: { id?, name }.
    /// Author: ChuongLP
    /// Date: 2026-06-13
    /// </summary>
    public async Task AddPresetAsync(NxVmsAddPresetInput input, CancellationToken ct = default)
    {
        var body = new Dictionary<string, object> { ["name"] = input.PresetName };
        await PostAsync($"/rest/v4/devices/{input.DeviceId}/ptz/presets", body, ct);
    }

    /// <summary>
    /// PATCH đổi tên preset. Body: { name }.
    /// Author: ChuongLP
    /// Date: 2026-06-13
    /// </summary>
    public async Task<string?> UpdatePresetAsync(NxVmsUpdatePresetInput input, CancellationToken ct = default)
    {
        return await PatchAsync(
            $"/rest/v4/devices/{input.DeviceId}/ptz/presets/{Uri.EscapeDataString(input.PresetId ?? string.Empty)}",
            new { name = input.Name },
            ct);
    }

    // ── PTZ — Tour ────────────────────────────────────────────────────────────
    /// <summary>
    /// Lấy danh sách tour PTZ của camera. Mỗi tour gồm tourId và name.
    /// </summary>
    /// Author: ChuongLP
    /// Date: 2026-05-29
    public async Task<List<NxVmsPtzTourResponse>?> GetToursAsync(string deviceId, CancellationToken ct = default)
    {
        return await GetAsync<List<NxVmsPtzTourResponse>>($"/rest/v4/devices/{deviceId}/ptz/tours", ct);
    }

    #endregion

    /// <summary>
    /// Hàm hỗ trợ: từ URL (thường là URL media) trích xuất IP để gán vào field Ip của NxVmsDeviceOutput.
    /// </summary>
    /// Author: ChuongLP
    /// Date: 2026-05-29
    private static string? ExtractIpFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : null;
    }

    // Helper dùng chung — đọc BaseUrl từ DB/cache rồi ghép với path
    private async Task<string> BuildEndpointAsync(string path)
    {
        var baseUrl = await _configService.GetBaseUrlAsync();
        return $"{baseUrl.TrimEnd('/')}{path}";
    }
}
