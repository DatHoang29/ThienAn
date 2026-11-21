using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;
using Module.VideoWall.Core.Options;
using System.Net;
using System.Xml;
using System.Xml.Serialization;

namespace ITS.VideoWall.Services.ISAPIDevice
{
    /// <summary>
    /// Tầng "dây" nói chuyện với bộ điều khiển Hikvision Video Wall qua ISAPI.
    ///
    ///   1. <see cref="VwISAPIDeviceClient"/>       — dựng URL, gửi lệnh, đọc ResponseStatus.
    ///   (Xác thực Digest do <see cref="VwISAPIDigestHandler"/> và <see cref="VwISAPICredentialResolver"/>
    ///   ở file riêng đảm nhiệm).
    ///
    /// Client giao tiếp với bộ điều khiển Hikvision Video Wall qua ISAPI.
    ///
    /// Xác thực Digest do <see cref="VwISAPIDigestHandler"/> đảm nhiệm; client chỉ gắn bộ điều
    /// khiển đang gọi vào HttpRequestMessage.Options để handler biết lấy tài khoản nào.
    ///
    /// Tham số wallNo do tầng gọi quyết định (xem <see cref="IVwISAPIDeviceService.ResolveWall"/>) — client không tự
    /// đoán, vì tường 1 trên thiết bị thật là sandbox chưa gắn màn hình. Bỏ trống wallNo ở lệnh cần
    /// tường thì trả về kết quả thất bại, KHÔNG ném ngoại lệ và KHÔNG mặc định về 1.
    /// </summary>
    public class VwISAPIDeviceClient(
        HttpClient httpClient,
        IServiceProvider serviceProvider,
        IOptions<VwDeviceOptions> deviceOptions,
        ILogger<VwISAPIDeviceClient> logger) : IVwISAPIDeviceClient
    {
        public static readonly HttpRequestOptionsKey<VwController> ControllerOptionKey = new("VwController");

        private const string EndpointSecurityUserCheck = "ISAPI/Security/userCheck";
        private const string EndpointCapabilities = "ISAPI/DisplayDev/VideoWall/capabilities";
        private const string EndpointInputChannels = "ISAPI/DisplayDev/Video/inputs/channels";
        private const string EndpointOutputChannels = "ISAPI/DisplayDev/Video/outputs/channels";
        private const string EndpointWallScreenInfo = "ISAPI/DisplayDev/ScreenCtrl/GetWallScreenInfo?format=json";
        /// <summary>openAll / closeAll: thiết bị trả lời sau khi xử lý xong từng màn (đo .19: 12 s / 8 màn ⇒ .18 16 màn ~25 s).</summary>
        private static readonly TimeSpan ScreenPowerTimeout = TimeSpan.FromSeconds(90);
        /// <summary>Lệnh màn (đo 11/11/2026): GetWallScreenInfo 12-15 s / bộ; screenStatus ~3,5 s / màn; control (độ sáng) chưa đo.</summary>
        private static readonly TimeSpan WallScreenInfoTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan ScreenStatusTimeout = TimeSpan.FromSeconds(20);
        private static readonly TimeSpan ScreenControlTimeout = TimeSpan.FromSeconds(30);
        private const string EndpointScreenOpenAll = "ISAPI/DisplayDev/ScreenCtrl/openAll";
        private const string EndpointScreenCloseAll = "ISAPI/DisplayDev/ScreenCtrl/closeAll";
        private const string EndpointScreenStatus = "ISAPI/DisplayDev/ScreenCtrl/screenStatus?format=json";
        private const string EndpointScreenControl = "ISAPI/DisplayDev/ScreenCtrl/control";
        private const string EndpointStreamChannels = "ISAPI/DisplayDev/Video/streaming/channels";
        private const string EndpointVideoWalls = "ISAPI/DisplayDev/VideoWall";
        private const string WallNotResolvedMessage = "Chưa xác định được số hiệu tường — gọi IVwISAPIDeviceService.ResolveWall trước khi gửi lệnh ISAPI.";

        private readonly HttpClient _httpClient = httpClient;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IOptions<VwDeviceOptions> _deviceOptions = deviceOptions;
        private readonly ILogger<VwISAPIDeviceClient> _logger = logger;

        private IVwISAPIDeviceService DeviceService => _serviceProvider.GetRequiredService<IVwISAPIDeviceService>();

        #region Registration & Device Auth Failure

        public Uri EnsureRegistered(VwController controller)
        {
            var config = _deviceOptions?.Value?.Device ?? new VwDeviceConnectionOptions();
            var configuration = _serviceProvider.GetService<IConfiguration>();
            var isLocal = configuration != null && string.Equals(configuration["VideoWall:Device:IsLocal"], "true", StringComparison.OrdinalIgnoreCase);

            return ResolveDeviceUri(controller, config, isLocal);
        }

        public static Uri ResolveDeviceUri(VwController? controller, VwDeviceConnectionOptions config, bool isLocal = false)
        {
            ArgumentNullException.ThrowIfNull(config);

            string? rawIp;
            if (isLocal && !string.IsNullOrWhiteSpace(config.Ip))
            {
                rawIp = config.Ip;
            }
            else
            {
                var controllerIp = controller?.IP?.Trim();
                rawIp = !string.IsNullOrWhiteSpace(controllerIp) ? controllerIp : config.Ip;
            }

            if (string.IsNullOrWhiteSpace(rawIp))
            {
                throw new InvalidOperationException("Chưa cấu hình IP thiết bị. Khai VideoWall:Device:Ip trong appsettings.json (Worker ITS.VideoWall), hoặc điền IP cho bản ghi VwController.");
            }

            var ip = rawIp.Trim();
            int? portFromIp = null;
            if (ip.Contains(':'))
            {
                var parts = ip.Split(':');
                ip = parts[0];
                if (int.TryParse(parts[1], out var parsedPort))
                    portFromIp = parsedPort;
            }

            // Cổng: cột VwController.Port (từ 26/10/2026 tách khỏi IP) → "ip:port" trong cấu hình → cấu hình → mặc định
            var controllerPort = controller?.Port is > 0 && !(isLocal && !string.IsNullOrWhiteSpace(config.Ip)) ? controller.Port : null;
            var port = (isLocal && config.Port.HasValue)
                ? config.Port.Value
                : (controllerPort ?? portFromIp ?? config.Port ?? VwDeviceProfile.DefaultPort);

            var scheme = !string.IsNullOrWhiteSpace(config.Scheme)
                ? config.Scheme.Trim().ToLowerInvariant()
                : VwDeviceProfile.Scheme;

            return new Uri($"{scheme}://{ip}:{port}/");
        }

        /// <summary>
        /// Quên mọi thứ đã ghi nhớ về bộ điều khiển. Phải xoá cả tường đã dò được, không chỉ ngắt
        /// mạch: lý do hay gặp nhất khi phải làm mới là vừa đổi IP hoặc đổi cấu hình tường, mà
        /// wallNo cũ được cache tới 30 phút — giữ lại là ghi thẳng vào tường sai.
        /// </summary>
        public Uri Refresh(VwController controller)
        {
            var uri = EnsureRegistered(controller);
            var key = !string.IsNullOrWhiteSpace(controller?.IP) ? controller.IP : uri.Host;
            ResetDeviceAuthFailure(key);
            if (controller != null)
                DeviceService.ForgetWall(controller.ID);

            return uri;
        }

        public bool IsDeviceAuthFailureBlocked(string? ipOrKey, out TimeSpan remaining)
            => DeviceService.IsDeviceAuthFailureBlocked(ipOrKey, out remaining);

        public void RecordDeviceAuthFailure(string? ipOrKey, int statusCode)
            => DeviceService.RecordDeviceAuthFailure(ipOrKey, statusCode);

        public void ResetDeviceAuthFailure(string? ipOrKey)
        {
            if (string.IsNullOrWhiteSpace(ipOrKey))
                return;

            DeviceService.ResetDeviceAuthFailure(ipOrKey);
        }

        public void ResetAllDeviceAuthFailures() => DeviceService.ResetAllDeviceAuthFailures();

        #endregion

        #region ISAPI Operations

        public Task<VwISAPIResult<VwISAPIUserCheckResponse>> UserCheckAsync(VwController controller, CancellationToken ct = default) =>
            SendGetXmlAsync<VwISAPIUserCheckResponse>(controller, EndpointSecurityUserCheck, ct);

        public Task<VwISAPIResult<VwISAPICapabilitiesResponse>> GetCapabilitiesAsync(VwController controller, CancellationToken ct = default) =>
            SendGetXmlAsync<VwISAPICapabilitiesResponse>(controller, EndpointCapabilities, ct);

        public Task<VwISAPIResult<VwISAPIVideoWallList>> GetVideoWallsAsync(VwController controller, CancellationToken ct = default) =>
            SendGetXmlAsync<VwISAPIVideoWallList>(controller, EndpointVideoWalls, ct);

        public Task<VwISAPIResult<VwISAPIOutputsResponse>> GetOutputsAsync(VwController controller, int? wallNo = null, CancellationToken ct = default) =>
            WithWall<VwISAPIOutputsResponse>(wallNo, wall => SendGetXmlAsync<VwISAPIOutputsResponse>(controller, EndpointOutputs(wall), ct));

        public Task<VwISAPIResult<VwISAPIWallWindowList>> GetWindowsAsync(VwController controller, int? wallNo = null, CancellationToken ct = default) =>
            WithWall<VwISAPIWallWindowList>(wallNo, wall => SendGetXmlAsync<VwISAPIWallWindowList>(controller, EndpointWindows(wall), ct));

        public Task<VwISAPIResult<VwISAPISceneStatusResponse>> GetActiveSceneAsync(VwController controller, int? wallNo = null, CancellationToken ct = default) =>
            WithWall<VwISAPISceneStatusResponse>(wallNo, wall => SendGetXmlAsync<VwISAPISceneStatusResponse>(controller, EndpointSceneRunning(wall), ct));

        public Task<VwISAPIResult> ActivateSceneAsync(VwController controller, string isapiSceneId, int? wallNo = null, CancellationToken ct = default) =>
            WithWall(wallNo, wall => SendPutStatusAsync(controller, EndpointSceneActivate(isapiSceneId, wall), null, ct));

        public Task<VwISAPIResult> SaveSceneDataAsync(VwController controller, string isapiSceneId, int? wallNo = null, CancellationToken ct = default) =>
            WithWall(wallNo, wall => SendPutStatusAsync(controller, EndpointSceneSaveData(isapiSceneId, wall), null, ct));

        public Task<VwISAPIResult<VwISAPIResponseStatus>> AddWindowAsync(VwController controller, VwISAPIWindowRequest window, int? wallNo = null, CancellationToken ct = default) =>
            WithWall<VwISAPIResponseStatus>(wallNo, async wall =>
                RequireIsapiOk(await SendPostXmlAsync<VwISAPIResponseStatus>(controller, EndpointWindows(wall), window, ct)));

        public Task<VwISAPIResult> UpdateWindowAsync(VwController controller, string windowId, VwISAPIWindowRequest window, int? wallNo = null, CancellationToken ct = default) =>
            WithWall(wallNo, wall => SendPutStatusAsync(controller, EndpointWindow(windowId, wall), window, ct));

        public Task<VwISAPIResult> DeleteWindowAsync(VwController controller, string windowId, int? wallNo = null, CancellationToken ct = default) =>
            WithWall(wallNo, wall => SendDeleteStatusAsync(controller, EndpointWindow(windowId, wall), ct));

        public Task<VwISAPIResult> DeleteAllWindowsAsync(VwController controller, int? wallNo = null, CancellationToken ct = default) =>
            WithWall(wallNo, wall => SendDeleteStatusAsync(controller, EndpointWindows(wall), ct));

        public Task<VwISAPIResult<VwISAPIInputChannelsResponse>> GetInputChannelsAsync(VwController controller, CancellationToken ct = default) =>
            SendGetXmlAsync<VwISAPIInputChannelsResponse>(controller, EndpointInputChannels, ct);

        public Task<VwISAPIResult<VwISAPIOutputChannelsResponse>> GetOutputChannelsAsync(VwController controller, CancellationToken ct = default) =>
            SendGetXmlAsync<VwISAPIOutputChannelsResponse>(controller, EndpointOutputChannels, ct);

        public async Task<VwISAPIResult<VwISAPIWallScreenInfoResponse>> GetWallScreenInfoAsync(VwController controller, int wallNo,
            IEnumerable<VwISAPIWallScreenRect> rects, CancellationToken ct = default)
        {
            // Body giống web hãng: 1 phần tử / màn cần hỏi
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                screenPosList = rects.Select(r => new { wallID = wallNo, rect = new { x = r.X, y = r.Y, width = r.Width, height = r.Height } }),
            });
            var core = await SendCoreAsync(controller, HttpMethod.Post, EndpointWallScreenInfo,
                () => (new StringContent(payload, Encoding.UTF8, "application/json"), payload), ct, WallScreenInfoTimeout);
            if (!core.Success)
                return VwISAPIResult<VwISAPIWallScreenInfoResponse>.Fail(core.ErrorMessage!, core.HttpStatusCode, core.RawRequest, core.RawResponse);

            try
            {
                return VwISAPIResult<VwISAPIWallScreenInfoResponse>.Ok(
                    System.Text.Json.JsonSerializer.Deserialize<VwISAPIWallScreenInfoResponse>(core.Data!)!, core.HttpStatusCode, core.RawRequest, core.RawResponse);
            }
            catch (Exception ex)
            {
                // Lỗi nghiệp vụ trả JSON {statusCode, errorMsg} ⇒ không có screenPosList
                _logger.LogError(ex, "Error calling ISAPI POST (JSON) {Uri} on controller {Ip}", EndpointWallScreenInfo, controller.IP);
                return VwISAPIResult<VwISAPIWallScreenInfoResponse>.Fail(ex.Message, core.HttpStatusCode, core.RawRequest, core.RawResponse);
            }
        }

        public async Task<VwISAPIResult> SetScreenPowerAsync(VwController controller, int wallNo, bool on, CancellationToken ct = default)
        {
            var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                + $"<ScreenCtrl version=\"2.0\" xmlns=\"http://www.isapi.org/ver20/XMLSchema\"><VideoWallID>{wallNo}</VideoWallID></ScreenCtrl>";
            var core = await SendCoreAsync(controller, HttpMethod.Put, on ? EndpointScreenOpenAll : EndpointScreenCloseAll, () => XmlBody(xml), ct, ScreenPowerTimeout);
            return core.Success
                ? ReadStatus(core.Data!, core.HttpStatusCode ?? 0, core.RawRequest)
                : core;
        }

        public async Task<VwISAPIResult<VwISAPIScreenStatusResponse>> GetScreenStatusAsync(VwController controller, int wallNo,
            VwISAPIWallScreenRect rect, CancellationToken ct = default)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                ScreenStatusCond = new
                {
                    statusType = "all",
                    ScreenPos = new { wallNo, Rect = new { Coordinate = new { x = rect.X, y = rect.Y }, width = rect.Width, height = rect.Height } },
                },
            });
            var core = await SendCoreAsync(controller, HttpMethod.Post, EndpointScreenStatus,
                () => (new StringContent(payload, Encoding.UTF8, "application/json"), payload), ct, ScreenStatusTimeout);
            if (!core.Success)
                return VwISAPIResult<VwISAPIScreenStatusResponse>.Fail(core.ErrorMessage!, core.HttpStatusCode, core.RawRequest, core.RawResponse);

            VwISAPIScreenStatusResponse? data;
            try { data = System.Text.Json.JsonSerializer.Deserialize<VwISAPIScreenStatusResponse>(core.Data!); }
            catch (Exception ex) { return VwISAPIResult<VwISAPIScreenStatusResponse>.Fail(ex.Message, core.HttpStatusCode, core.RawRequest, core.RawResponse); }
            // Lỗi nghiệp vụ (vd màn đã tháo: gettingScreenStatusFailed) trả JSON {statusCode, errorMsg} ⇒ không có ScreenStatus
            return data?.ScreenStatus == null
                ? VwISAPIResult<VwISAPIScreenStatusResponse>.Fail(core.Data!, core.HttpStatusCode, core.RawRequest, core.RawResponse)
                : VwISAPIResult<VwISAPIScreenStatusResponse>.Ok(data, core.HttpStatusCode, core.RawRequest, core.RawResponse);
        }

        public async Task<VwISAPIResult> SetScreenBacklightAsync(VwController controller, int wallNo,
            IEnumerable<VwISAPIWallScreenRect> rects, int value, CancellationToken ct = default)
        {
            var positions = string.Concat(rects.Select(r =>
                $"<ScreenPos><wallNo>{wallNo}</wallNo><Rect><Coordinate><x>{r.X}</x><y>{r.Y}</y></Coordinate><width>{r.Width}</width><height>{r.Height}</height></Rect></ScreenPos>"));
            var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ScreenCtrlEx><command>effect</command>"
                + $"<EffectCtrl><type>backlight</type><valueType>Number</valueType><Number><backlight>{value}</backlight></Number></EffectCtrl>"
                + $"<ScreenPosList>{positions}</ScreenPosList></ScreenCtrlEx>";
            var core = await SendCoreAsync(controller, HttpMethod.Put, EndpointScreenControl, () => XmlBody(xml), ct, ScreenControlTimeout);
            return core.Success
                ? ReadStatus(core.Data!, core.HttpStatusCode ?? 0, core.RawRequest)
                : core;
        }

        public Task<VwISAPIResult> BringWindowToTopAsync(VwController controller, string windowId, int? wallNo = null, CancellationToken ct = default) =>
            WithWall(wallNo, wall => SendPutStatusAsync(controller, EndpointWindowTop(windowId, wall), null, ct));

        public Task<VwISAPIResult> SendWindowToBottomAsync(VwController controller, string windowId, int? wallNo = null, CancellationToken ct = default) =>
            WithWall(wallNo, wall => SendPutStatusAsync(controller, EndpointWindowBottom(windowId, wall), null, ct));

        public Task<VwISAPIResult<VwISAPIStreamChannelList>> GetStreamChannelsAsync(VwController controller, CancellationToken ct = default) =>
            SendGetXmlAsync<VwISAPIStreamChannelList>(controller, EndpointStreamChannels, ct);

        public Task<VwISAPIResult<VwISAPIWallSceneList>> GetScenesAsync(VwController controller, int? wallNo = null, CancellationToken ct = default) =>
            WithWall<VwISAPIWallSceneList>(wallNo, wall => SendGetXmlAsync<VwISAPIWallSceneList>(controller, EndpointScenes(wall), ct));

        public Task<VwISAPIResult<VwISAPISceneInfoResponse>> GetSceneInfoAsync(VwController controller, string sceneId, int? wallNo = null, CancellationToken ct = default) =>
            WithWall<VwISAPISceneInfoResponse>(wallNo, wall => SendGetJsonAsync<VwISAPISceneInfoResponse>(controller, EndpointSceneInfo(sceneId, wall), ct));

        public Task<VwISAPIResult<VwISAPIResponseStatus>> CreateSceneAsync(VwController controller, string sceneName, int? wallNo = null, CancellationToken ct = default) =>
            WithWall<VwISAPIResponseStatus>(wallNo, async wall =>
                RequireIsapiOk(await SendPostXmlAsync<VwISAPIResponseStatus>(controller, EndpointScenes(wall), new VwISAPIWallSceneRequest { Name = sceneName }, ct)));

        /// <summary>
        /// POST trả ResponseStatus kèm HTTP 200 cả khi thiết bị từ chối (statusCode 4/5/6) ⇒ phải đọc statusCode,
        /// không thì lỗi bị coi là thành công (11/11/2026: Áp dụng có thể thiếu cửa sổ mà vẫn lưu ô kịch bản).
        /// </summary>
        private static VwISAPIResult<VwISAPIResponseStatus> RequireIsapiOk(VwISAPIResult<VwISAPIResponseStatus> result)
            => result is { Success: true, Data: { IsSuccess: false } status }
                ? VwISAPIResult<VwISAPIResponseStatus>.Fail($"{status.StatusString} ({status.SubStatusCode})", result.HttpStatusCode, result.RawRequest, result.RawResponse)
                : result;

        public Task<VwISAPIResult> RenameSceneAsync(VwController controller, string sceneId, string sceneName, int? wallNo = null, CancellationToken ct = default) =>
            WithWall(wallNo, wall => SendPutStatusAsync(controller, EndpointScene(sceneId, wall), new VwISAPIWallSceneRequest { Id = int.Parse(sceneId), IdSpecified = true, Name = sceneName }, ct));

        public Task<VwISAPIResult> StartSubWindowDecodeAsync(VwController controller, string windowId, int subId, int? wallNo = null, CancellationToken ct = default) =>
            WithWall(wallNo, wall => SendPutStatusAsync(controller, EndpointWindowSubStart(windowId, subId, wall), null, ct));

        public Task<VwISAPIResult<VwISAPIWallWindowItem>> GetWindowAsync(VwController controller, string windowId, int? wallNo = null, CancellationToken ct = default) =>
            WithWall<VwISAPIWallWindowItem>(wallNo, wall => SendGetXmlAsync<VwISAPIWallWindowItem>(controller, EndpointWindow(windowId, wall), ct));

        public async Task<VwISAPIResult> SendRawAsync(VwController controller, HttpMethod method, string relativeUri,
            string? body, string? contentType, CancellationToken ct = default)
        {
            if (body is null)
                return await SendCoreAsync(controller, method, relativeUri, null, ct);

            var payload = body;
            var mediaType = contentType ?? "application/xml";

            return await SendCoreAsync(controller, method, relativeUri,
                () => (new StringContent(payload, Encoding.UTF8, mediaType), payload), ct);
        }

        public string SerializeToXml(object payload) => Serialize(payload);

        #endregion

        #region Wall Guard & Endpoint Builders

        /// <summary>
        /// Chặn mọi lệnh chưa xác định được số hiệu tường. Trả về lỗi thay vì mặc định tường 1:
        /// tường 1 trên thiết bị thật là sandbox chưa gắn màn hình nên lệnh sẽ trả HTTP 200 mà màn
        /// hình không đổi gì. Trả VwISAPIResult.Fail chứ không ném để giữ đúng hợp đồng never-throw.
        /// </summary>
        private static Task<VwISAPIResult<T>> WithWall<T>(int? wallNo, Func<int, Task<VwISAPIResult<T>>> call) where T : class
            => wallNo is > 0
                ? call(wallNo.Value)
                : Task.FromResult(VwISAPIResult<T>.Fail(WallNotResolvedMessage));

        private static Task<VwISAPIResult> WithWall(int? wallNo, Func<int, Task<VwISAPIResult>> call)
            => wallNo is > 0
                ? call(wallNo.Value)
                : Task.FromResult(VwISAPIResult.Fail(WallNotResolvedMessage));

        private static string EndpointOutputs(int wallNo) => $"ISAPI/DisplayDev/VideoWall/{wallNo}/outputs";
        private static string EndpointWindows(int wallNo) => $"ISAPI/DisplayDev/VideoWall/{wallNo}/windows";
        private static string EndpointWindow(string windowId, int wallNo) => $"ISAPI/DisplayDev/VideoWall/{wallNo}/windows/{windowId}";
        private static string EndpointWindowTop(string windowId, int wallNo) => $"ISAPI/DisplayDev/VideoWall/{wallNo}/windows/{windowId}/top";
        private static string EndpointWindowBottom(string windowId, int wallNo) => $"ISAPI/DisplayDev/VideoWall/{wallNo}/windows/{windowId}/bottom";
        private static string EndpointSceneRunning(int wallNo) => $"ISAPI/DisplayDev/VideoWall/{wallNo}/scene/isRunning";
        private static string EndpointScenes(int wallNo) => $"ISAPI/DisplayDev/VideoWall/{wallNo}/scene";
        private static string EndpointScene(string sceneId, int wallNo) => $"ISAPI/DisplayDev/VideoWall/{wallNo}/scene/{sceneId}";
        private static string EndpointSceneInfo(string sceneId, int wallNo) => $"ISAPI/DisplayDev/VideoWall/{wallNo}/scene/{sceneId}/sceneInfo?format=json";
        private static string EndpointWindowSubStart(string windowId, int subId, int wallNo) => $"ISAPI/DisplayDev/VideoWall/{wallNo}/windows/{windowId}/sub/{subId}/start";
        private static string EndpointSceneActivate(string sceneId, int wallNo) => $"ISAPI/DisplayDev/VideoWall/{wallNo}/scene/{sceneId}/activate";
        private static string EndpointSceneSaveData(string sceneId, int wallNo) => $"ISAPI/DisplayDev/VideoWall/{wallNo}/scene/{sceneId}/saveData";

        #endregion

        #region Transport (gửi request & đọc kết quả)

        private void RecordSuccess(string? ipOrKey)
        {
            if (string.IsNullOrWhiteSpace(ipOrKey))
                return;

            DeviceService.RecordAuthSuccess(ipOrKey);
        }

        /// <summary>Gắn bộ điều khiển vào request để VwISAPIDigestHandler biết dùng tài khoản nào.</summary>
        private static HttpRequestMessage CreateRequest(HttpMethod method, VwController controller, string relativeUri, Uri baseUri)
        {
            var requestUri = new Uri(baseUri, relativeUri);
            var request = new HttpRequestMessage(method, requestUri);
            request.Options.Set(ControllerOptionKey, controller);

            return request;
        }

        /// <summary>
        /// Khung chung cho mọi lệnh ISAPI: chặn khi lỗi xác thực (Device Auth Failure), dựng &amp; gửi request, đọc body,
        /// soi HTTP status (nuôi RecordDeviceAuthFailure), ghi nhận thành công và log ngoại lệ.
        /// Thành công thì Data giữ nguyên body thô để tầng gọi tự giải mã (XML / JSON / ResponseStatus).
        /// bodyFactory (nếu có) chạy TRONG try, sau khi qua cửa kiểm tra lỗi xác thực — lỗi serialize được xử như
        /// lỗi transport: log + trả Fail, không ném ra ngoài.
        /// </summary>
        private async Task<VwISAPIResult<string>> SendCoreAsync(
            VwController controller,
            HttpMethod method,
            string relativeUri,
            Func<(HttpContent Content, string RawRequest)>? bodyFactory,
            CancellationToken ct,
            TimeSpan? timeout = null)
        {
            // Hạn chờ theo từng request (HttpClient.Timeout = vô hạn): mặc định Device.TimeoutSeconds;
            // lệnh chậm (bật/tắt màn: thiết bị xử lý lần lượt từng màn, đo 12 s / 8 màn) truyền hạn dài hơn.
            var requestTimeout = timeout ?? TimeSpan.FromSeconds(_deviceOptions.Value?.Device?.TimeoutSeconds is int sec and > 0 ? sec : 8);

            Uri baseUri;
            try
            {
                baseUri = EnsureRegistered(controller);
            }
            catch (Exception ex)
            {
                return VwISAPIResult<string>.Fail(ex.Message);
            }

            var configuration = _serviceProvider.GetService<IConfiguration>();
            var isLocal = configuration != null && string.Equals(configuration["VideoWall:Device:IsLocal"], "true", StringComparison.OrdinalIgnoreCase);

            var ip = isLocal
                ? $"{baseUri.Host}:{baseUri.Port}"
                : (!string.IsNullOrWhiteSpace(controller?.IP) ? controller.IP : baseUri.Host);

            var maxAttempts = Math.Max(1, _deviceOptions.Value?.Device?.MaxRetryAttempts ?? 3);
            var baseDelayMs = Math.Max(50, _deviceOptions.Value?.Device?.RetryBaseDelayMs ?? 500);

            if (!IsMethodRetriable(method))
                maxAttempts = 1;

            string? rawRequest = null;
            string? lastContent = null;
            int? lastStatus = null;
            string? lastErrorMessage = null;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                if (IsDeviceAuthFailureBlocked(ip, out var remaining))
                    return VwISAPIResult<string>.Fail(DeviceAuthFailureBlockedMessage(controller, remaining), 429);

                try
                {
                    HttpContent? requestContent = null;
                    if (bodyFactory != null)
                        (requestContent, rawRequest) = bodyFactory();

                    using var request = CreateRequest(method, controller, relativeUri, baseUri);
                    request.Content = requestContent;

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeoutCts.CancelAfter(requestTimeout);
                    using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
                    var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                    var status = (int)response.StatusCode;

                    if (!response.IsSuccessStatusCode)
                    {
                        lastContent = content;
                        lastStatus = status;
                        lastErrorMessage = $"HTTP {status} {response.ReasonPhrase}";

                        if (attempt < maxAttempts && IsTransientStatusCode(status))
                        {
                            var delay = CalculateBackoff(attempt, baseDelayMs);
                            _logger.LogWarning(
                                "ISAPI {Method} {Uri} on controller {Ip} returned {StatusCode}. Retrying ({Attempt}/{MaxAttempts}) after {DelayMs}ms...",
                                method.Method, relativeUri, ip, status, attempt, maxAttempts, delay.TotalMilliseconds);

                            try { await Task.Delay(delay, ct); }
                            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                            continue;
                        }

                        RecordDeviceAuthFailure(ip, status);
                        return VwISAPIResult<string>.Fail(lastErrorMessage, status, rawRequest, content);
                    }

                    RecordSuccess(ip);
                    return VwISAPIResult<string>.Ok(content, status, rawRequest, content);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastErrorMessage = ex.Message;

                    if (attempt < maxAttempts && IsTransientException(ex, ct))
                    {
                        var delay = CalculateBackoff(attempt, baseDelayMs);
                        _logger.LogWarning(
                            ex,
                            "Transient error calling ISAPI {Method} {Uri} on controller {Ip}. Retrying ({Attempt}/{MaxAttempts}) after {DelayMs}ms...",
                            method.Method, relativeUri, ip, attempt, maxAttempts, delay.TotalMilliseconds);

                        try { await Task.Delay(delay, ct); }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                        continue;
                    }

                    _logger.LogError(
                        ex,
                        "Error calling ISAPI {Method} {Uri} on controller {Ip} (Attempt {Attempt}/{MaxAttempts})",
                        method.Method, relativeUri, ip, attempt, maxAttempts);

                    RecordDeviceAuthFailure(ip, (int)HttpStatusCode.ServiceUnavailable);
                    return VwISAPIResult<string>.Fail(ex.Message, null, rawRequest);
                }
            }

            RecordDeviceAuthFailure(ip, lastStatus ?? (int)HttpStatusCode.ServiceUnavailable);
            return VwISAPIResult<string>.Fail(lastErrorMessage ?? "Request failed after maximum retry attempts", lastStatus, rawRequest, lastContent);
        }

        private static (HttpContent Content, string RawRequest) XmlBody(string xml) =>
            (new StringContent(xml, Encoding.UTF8, "application/xml"), xml);

        private async Task<VwISAPIResult<T>> SendGetXmlAsync<T>(VwController controller, string relativeUri, CancellationToken ct = default) where T : class
        {
            var core = await SendCoreAsync(controller, HttpMethod.Get, relativeUri, null, ct);
            if (!core.Success)
                return VwISAPIResult<T>.Fail(core.ErrorMessage!, core.HttpStatusCode, core.RawRequest, core.RawResponse);

            try
            {
                return VwISAPIResult<T>.Ok(Deserialize<T>(core.Data!), core.HttpStatusCode, core.RawRequest, core.RawResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ISAPI GET {Uri} on controller {Ip}", relativeUri, controller.IP);
                return VwISAPIResult<T>.Fail(ex.Message);
            }
        }

        private async Task<VwISAPIResult<T>> SendGetJsonAsync<T>(VwController controller, string relativeUri, CancellationToken ct = default) where T : class
        {
            var core = await SendCoreAsync(controller, HttpMethod.Get, relativeUri, null, ct);
            if (!core.Success)
                return VwISAPIResult<T>.Fail(core.ErrorMessage!, core.HttpStatusCode, core.RawRequest, core.RawResponse);

            try
            {
                return VwISAPIResult<T>.Ok(System.Text.Json.JsonSerializer.Deserialize<T>(core.Data!)!, core.HttpStatusCode, core.RawRequest, core.RawResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ISAPI GET (JSON) {Uri} on controller {Ip}", relativeUri, controller.IP);
                return VwISAPIResult<T>.Fail(ex.Message);
            }
        }

        private async Task<VwISAPIResult<T>> SendPostXmlAsync<T>(VwController controller, string relativeUri, object body, CancellationToken ct = default) where T : class
        {
            var core = await SendCoreAsync(controller, HttpMethod.Post, relativeUri, () => XmlBody(Serialize(body)), ct);
            if (!core.Success)
                return VwISAPIResult<T>.Fail(core.ErrorMessage!, core.HttpStatusCode, core.RawRequest, core.RawResponse);

            try
            {
                return VwISAPIResult<T>.Ok(Deserialize<T>(core.Data!), core.HttpStatusCode, core.RawRequest, core.RawResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ISAPI POST {Uri} on controller {Ip}", relativeUri, controller.IP);
                return VwISAPIResult<T>.Fail(ex.Message, null, core.RawRequest);
            }
        }

        private async Task<VwISAPIResult> SendPutStatusAsync(VwController controller, string relativeUri, object? body = null, CancellationToken ct = default)
        {
            var core = await SendCoreAsync(controller, HttpMethod.Put, relativeUri,
                body != null ? () => XmlBody(Serialize(body)) : null, ct);

            return core.Success
                ? ReadStatus(core.Data!, core.HttpStatusCode ?? 0, core.RawRequest)
                : core;
        }

        private async Task<VwISAPIResult> SendDeleteStatusAsync(VwController controller, string relativeUri, CancellationToken ct = default)
        {
            var core = await SendCoreAsync(controller, HttpMethod.Delete, relativeUri, null, ct);
            return core.Success
                ? ReadStatus(core.Data!, core.HttpStatusCode ?? 0, null)
                : core;
        }

        /// <summary>
        /// Thiết bị trả HTTP 200 kèm ResponseStatus mô tả lỗi nghiệp vụ, nên phải đọc statusCode
        /// trong body mới biết lệnh có thật sự thành công không.
        /// </summary>
        private static VwISAPIResult ReadStatus(string content, int httpStatusCode, string? rawRequest)
        {
            if (string.IsNullOrWhiteSpace(content))
                return VwISAPIResult.Ok(httpStatusCode, rawRequest, content);

            try
            {
                var status = Deserialize<VwISAPIResponseStatus>(content);
                if (status != null && !status.IsSuccess)
                    return VwISAPIResult.Fail($"{status.StatusString} ({status.SubStatusCode})", httpStatusCode, rawRequest, content);
            }
            catch
            {
                // Body không phải ResponseStatus (một số lệnh trả rỗng hoặc trả DTO khác) — coi HTTP 2xx là thành công.
            }

            return VwISAPIResult.Ok(httpStatusCode, rawRequest, content);
        }

        private static string DeviceAuthFailureBlockedMessage(VwController controller, TimeSpan remaining)
            => $"Device auth failure block is ACTIVE for controller '{controller.Name}' ({controller.IP}). Remaining cooldown: {remaining.TotalSeconds:F0}s";

        #region XML Serialization

        private static readonly XmlWriterSettings WriterSettings = new()
        {
            // Bỏ hẳn XML declaration. StringWriter mặc định khai encoding="utf-16" trong khi request
            // gửi charset=utf-8 — thiết bị trả badXmlFormat (mục G.2 tài liệu đo thật 09B). Cũng
            // không được để BOM lọt vào body vì đó là nguyên nhân thứ hai của cùng lỗi này.
            OmitXmlDeclaration = true,
            Encoding = new UTF8Encoding(false),
            Indent = false
        };

        private static readonly XmlReaderSettings ReaderSettings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        private static string Serialize(object obj)
        {
            var serializer = new XmlSerializer(obj.GetType());
            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, VwDeviceProfile.XmlNamespace);

            using var stringWriter = new StringWriter();
            using (var xmlWriter = XmlWriter.Create(stringWriter, WriterSettings))
                serializer.Serialize(xmlWriter, obj, ns);

            return stringWriter.ToString().TrimStart('\uFEFF');
        }

        private static T Deserialize<T>(string xml) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));

            using var stringReader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(stringReader, ReaderSettings);

            return (T)serializer.Deserialize(xmlReader)!;
        }

        private static bool IsMethodRetriable(HttpMethod method)
        {
            // GET và DELETE là idempotent / safe để retry.
            // PUT (activate scene, set layer, switch) là idempotent để retry có kiểm soát.
            // POST (AddWindow, CreateScene) KHÔNG retry tự động để tránh nhân đôi tài nguyên trên phần cứng.
            return method == HttpMethod.Get || method == HttpMethod.Put || method == HttpMethod.Delete;
        }

        private static bool IsTransientStatusCode(int statusCode)
        {
            return statusCode == 408 || statusCode == 429 || statusCode >= 500;
        }

        private static bool IsTransientException(Exception ex, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return false;

            return ex is HttpRequestException or TimeoutException or TaskCanceledException;
        }

        private static TimeSpan CalculateBackoff(int attempt, int baseDelayMs)
        {
            var exp = Math.Min(5, Math.Max(0, attempt - 1));
            var delayMs = baseDelayMs * Math.Pow(2, exp);
            var jitter = Random.Shared.Next(0, Math.Max(10, (int)(delayMs * 0.2)));
            return TimeSpan.FromMilliseconds(delayMs + jitter);
        }

        #endregion

        #endregion
    }
}

