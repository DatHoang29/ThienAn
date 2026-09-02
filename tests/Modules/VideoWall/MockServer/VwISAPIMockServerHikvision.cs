using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Tests.Modules.VideoWall.MockServer
{
    /// <summary>
    /// Author: Đạt
    /// Description: Mock Server HTTP độc lập giả lập thiết bị Hikvision Video Wall Controller DS-C66S-H88-CL.
    ///              Mở Socket HTTP thật trên localhost (mặc định port 18080), hỗ trợ Digest Authentication (RFC 7616)
    ///              và trả về toàn bộ các mẫu XML & JSON đo thật (100% Ground Truth từ API_Postman_Videowall.md).
    /// Created date: 17/08/2026
    /// </summary>
    public partial class VwISAPIMockServerHikvision : IDisposable
    {
        private const string Ns = "http://www.isapi.org/ver20/XMLSchema";
        private readonly HttpListener _listener = new();
        private CancellationTokenSource? _cts;
        private Task? _listenTask;

        public const string DefaultHost = "127.0.0.1";
        public const int DefaultPort = 18080;
        public static readonly int[] DefaultPorts = [18080, 18081, 18082, 18083];
        public const string DefaultUser = "admin";
        public const string DefaultPassword = "Password123!";
        public const string Realm = "DS-C66S-H88-CL";

        /// <summary>
        /// Trần SID kịch bản đo thật trên DS-C66S-H88-CL (09B-practical-guide §203).
        /// Test nào cần kiểm nhánh chặn SID thì hạ <see cref="MaxSceneNums"/> xuống, đừng đổi hằng này.
        /// </summary>
        public const int DefaultMaxSceneNums = 128;

        public string BaseUrl => $"http://{DefaultHost}:{DefaultPort}/";

        // ─── Cờ điều khiển kịch bản giả lập (Dynamic Behavior Flags) ───
        public bool RequireDigestAuth { get; set; } = true;
        public bool SimulateChallengeWithoutAlgorithm { get; set; }
        public bool SimulateDualChallengeHeader { get; set; }
        public bool VerifyDigestResponseHash { get; set; }
        public bool SimulateAddWindowWithoutId { get; set; }
        public string? LastIssuedMd5Nonce { get; private set; }
        public string? LastReceivedAuthNonce { get; private set; }
        public bool SimulateDeviceFailure { get; set; }
        public string? SimulateFailureControllerId { get; set; }
        public HashSet<int> SimulateFailurePorts { get; } = [];
        public int FailedAuthLockoutThreshold { get; set; } = 0;
        public int ConsecutiveFailedAuthCount { get; private set; }
        public bool IsLockedOut { get; set; }
        public bool SimulateNonceExpiry { get; set; }
        public int NonceExpiryTriggerCount { get; set; } = 0;
        public bool IsSupportScene { get; set; } = true;

        /// <summary>
        /// Trần SID kịch bản thiết bị nhận, trả về trong &lt;maxSceneNums&gt; của VideoWallCap.
        ///
        /// Mặc định 128 = số ĐO THẬT trên DS-C66S-H88-CL (09B-practical-guide §202-203:
        /// "isSupportScene: true (maxSceneNums: 128)"). Giữ đúng số thật để mọi test hiện có không
        /// đổi hành vi; bài nào cần kiểm nhánh chặn SID thì tự hạ giá trị này xuống.
        ///
        /// Trước khi có field này, mock KHÔNG trả maxSceneNums nên nó luôn deserialize ra 0, mà
        /// VwISAPIDeviceService.DeviceSetup kiểm SID bằng `if (output.MaxSceneNums > 0 && ...)` —
        /// tức nhánh chặn SID sai khoảng KHÔNG THỂ chạm tới được qua mock.
        /// </summary>
        public int MaxSceneNums { get; set; } = DefaultMaxSceneNums;

        public Dictionary<int, Dictionary<int, string>> WallSceneStores { get; } = new()
        {
            [1] = new()
            {
                [1] = "Kịch bản 1: Giám sát Tuyến Trên - Dưới (2 Ô dọc)",
                [2] = "Kịch bản 2: Toàn cảnh Tuyến Dọc (Toàn tường Wall 1)",
            },
            [2] = new()
            {
                [1] = "Kịch bản 1: Giám sát 4 Ô Độc lập (Lưới 2×2)",
                [2] = "Kịch bản 2: Sự cố Trọng điểm (Khối lớn 2×1 + 2 Ô phụ)",
                [3] = "Kịch bản 3: Bản đồ GIS / Dashboard (Toàn tường 2×2)",
            }
        };

        public Dictionary<int, string> SceneStore => GetSceneStore(1);
        public int NextSceneId { get; set; } = 4;

        public Dictionary<int, string> GetSceneStore(int wallNo)
        {
            if (!WallSceneStores.TryGetValue(wallNo, out var store))
            {
                store = new Dictionary<int, string> { [1] = "Default Scene" };
                WallSceneStores[wallNo] = store;
            }
            return store;
        }
        public Dictionary<int, string> PlanStore { get; } = new() { [1] = "Default Plan" };
        public int NextPlanId { get; set; } = 2;
        public int? ActivePlanId { get; set; } = 1;

        public HashSet<int> ValidOutputChannelIds { get; } = [17235971, 17235972, 17235973, 17235974];

        public bool IsValidOutputChannel(string? channelIdStr, out int channelId)
        {
            if (int.TryParse(channelIdStr, out channelId) && ValidOutputChannelIds.Contains(channelId))
                return true;
            channelId = 0;
            return false;
        }

        public int ActiveSceneId { get; set; } = 1;
        public bool SimulateSaveDataFailure { get; set; }
        public bool SimulateMethodNotAllowed { get; set; }
        public bool SimulateBadXmlFormat { get; set; }
        public bool SimulateBadParameters { get; set; }
        public bool SimulateInvalidOperation { get; set; }
        public bool ScreenCtrlCloseAllThrowsInvalidOperation { get; set; } = true;
        public bool IsSupportSerialTransparent { get; set; } = true;
        public bool SimulateSerialOpenFailure { get; set; }
        public bool SimulateSerialSendFailure { get; set; }
        public bool SimulateNoBoundWall { get; set; }
        public bool SimulateMultipleBoundWalls { get; set; }
        public bool SimulateUnreachable { get; set; }
        public HashSet<int> SimulateUnreachablePorts { get; } = [];
        public bool SimulateMalformedXmlResponse { get; set; }

        // ─── Bộ đếm số lần gọi API ───
        public int UserCheckCallCount { get; private set; }
        public int GetCapabilitiesCallCount { get; private set; }
        public int GetSerialCapabilitiesCallCount { get; private set; }
        public int GetOutputsCallCount { get; private set; }
        public int GetWindowsCallCount { get; private set; }
        public int GetActiveSceneCallCount { get; private set; }
        public int SaveSceneDataCallCount { get; private set; }
        public int ActivateSceneCallCount { get; private set; }
        public int AddWindowCallCount { get; private set; }
        public int UpdateWindowCallCount { get; private set; }
        public int DeleteWindowCallCount { get; private set; }
        public int DeleteAllWindowsCallCount { get; private set; }
        public int SwitchSourceCallCount { get; private set; }
        public int WindowTopCallCount { get; private set; }
        public int WindowBottomCallCount { get; private set; }
        public int GetInputChannelsCallCount { get; private set; }
        public int GetOutputChannelsCallCount { get; private set; }
        public int GetVideoWallsCallCount { get; private set; }
        public HashSet<int> NotConnectedOutputChannels { get; } = [];
        public int SerialOpenCallCount { get; private set; }
        public int SerialSendCallCount { get; private set; }
        public int SerialReceiveCallCount { get; private set; }
        public int SerialCloseCallCount { get; private set; }
        public byte[]? LastReceivedSerialData { get; private set; }
        public string? LastReceivedContentType { get; private set; }

        /// <summary>Body thô của request gần nhất (trừ transData vì route đó tự đọc InputStream).</summary>
        public byte[]? LastReceivedBodyBytes { get; private set; }

        /// <summary>Body gần nhất đã giải mã UTF-8, tiện cho assert nội dung.</summary>
        public string? LastReceivedBody { get; private set; }
        public byte[]? SerialDataToReturn { get; set; }
        public List<string> ReceivedRequests { get; } = [];

        /// <summary>
        /// Author: Đạt
        /// Description: Khởi động HttpListener lắng nghe trên các port (mặc định 18080, 18081, 18082, 18083)
        /// Created date: 17/08/2026
        /// </summary>
        public void Start(params int[] ports)
        {
            if (_listener.IsListening)
                return;

            var targetPorts = ports != null && ports.Length > 0 ? ports : DefaultPorts;

            _listener.Prefixes.Clear();
            foreach (var port in targetPorts)
            {
                _listener.Prefixes.Add($"http://{DefaultHost}:{port}/");
            }

            try
            {
                _listener.Start();
            }
            catch (HttpListenerException ex)
            {
                throw new InvalidOperationException(
                    $"MockServer không thể lắng nghe trên các port [{string.Join(", ", targetPorts)}]. " +
                    $"Có thể tiến trình testhost cũ đang chạy ngầm chiếm cổng. Hãy chạy 'Stop-Process -Name testhost -Force' để giải phóng. Chi tiết: {ex.Message}", ex);
            }

            _cts = new CancellationTokenSource();
            _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Đưa toàn bộ cấu hình giả lập, cờ điều khiển và bộ đếm về trạng thái mặc định ban đầu
        /// Created date: 17/08/2026
        /// </summary>
        public void ResetDefaults()
        {
            RequireDigestAuth = true;
            SimulateChallengeWithoutAlgorithm = false;
            SimulateDualChallengeHeader = false;
            VerifyDigestResponseHash = false;
            SimulateAddWindowWithoutId = false;
            LastIssuedMd5Nonce = null;
            LastReceivedAuthNonce = null;
            SimulateDeviceFailure = false;
            SimulateFailureControllerId = null;
            SimulateFailurePorts.Clear();
            FailedAuthLockoutThreshold = 0;
            ConsecutiveFailedAuthCount = 0;
            IsLockedOut = false;
            SimulateNonceExpiry = false;
            NonceExpiryTriggerCount = 0;
            IsSupportScene = true;
            MaxSceneNums = DefaultMaxSceneNums;
            ActiveSceneId = 1;
            WallSceneStores.Clear();
            WallSceneStores[1] = new()
            {
                [1] = "Kịch bản 1: Giám sát Tuyến Trên - Dưới (2 Ô dọc)",
                [2] = "Kịch bản 2: Toàn cảnh Tuyến Dọc (Toàn tường Wall 1)",
            };
            WallSceneStores[2] = new()
            {
                [1] = "Kịch bản 1: Giám sát 4 Ô Độc lập (Lưới 2×2)",
                [2] = "Kịch bản 2: Sự cố Trọng điểm (Khối lớn 2×1 + 2 Ô phụ)",
                [3] = "Kịch bản 3: Bản đồ GIS / Dashboard (Toàn tường 2×2)",
            };
            NextSceneId = 4;
            PlanStore.Clear();
            PlanStore[1] = "Default Plan";
            NextPlanId = 2;
            ActivePlanId = 1;
            SimulateSaveDataFailure = false;
            SimulateMethodNotAllowed = false;
            SimulateBadXmlFormat = false;
            SimulateBadParameters = false;
            SimulateInvalidOperation = false;
            ScreenCtrlCloseAllThrowsInvalidOperation = true;
            IsSupportSerialTransparent = true;
            SimulateSerialOpenFailure = false;
            SimulateSerialSendFailure = false;
            SimulateNoBoundWall = false;
            SimulateMultipleBoundWalls = false;
            SimulateUnreachable = false;
            SimulateUnreachablePorts.Clear();
            SimulateMalformedXmlResponse = false;

            UserCheckCallCount = 0;
            GetCapabilitiesCallCount = 0;
            GetSerialCapabilitiesCallCount = 0;
            GetOutputsCallCount = 0;
            GetWindowsCallCount = 0;
            GetActiveSceneCallCount = 0;
            SaveSceneDataCallCount = 0;
            ActivateSceneCallCount = 0;
            AddWindowCallCount = 0;
            UpdateWindowCallCount = 0;
            DeleteWindowCallCount = 0;
            DeleteAllWindowsCallCount = 0;
            SwitchSourceCallCount = 0;
            WindowTopCallCount = 0;
            WindowBottomCallCount = 0;
            GetInputChannelsCallCount = 0;
            GetOutputChannelsCallCount = 0;
            GetVideoWallsCallCount = 0;
            NotConnectedOutputChannels.Clear();
            SerialOpenCallCount = 0;
            SerialSendCallCount = 0;
            SerialReceiveCallCount = 0;
            SerialCloseCallCount = 0;
            LastReceivedSerialData = null;
            LastReceivedContentType = null;
            LastReceivedBodyBytes = null;
            LastReceivedBody = null;
            SerialDataToReturn = null;
            ReceivedRequests.Clear();
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Vòng lặp nhận HTTP connection từ socket và điều phối xử lý request bất đồng bộ
        /// Created date: 17/08/2026
        /// </summary>
        private async Task ListenLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = ProcessRequestAsync(context);
                }
                catch (HttpListenerException) when (ct.IsCancellationRequested || !_listener.IsListening)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Tiếp tục vòng lặp cho các request khác
                }
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Xử lý request ISAPI theo đường dẫn URL, phương thức HTTP và các cờ kịch bản giả lập
        /// Created date: 17/08/2026
        /// </summary>
        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            try
            {
                var reqPort = req.Url?.Port ?? DefaultPort;
                var path = req.Url?.AbsolutePath ?? "/";
                var method = req.HttpMethod;
                var rawUrl = req.RawUrl ?? path;
                ReceivedRequests.Add($"[{reqPort}] {method} {rawUrl}");

                // Chụp body THÔ dạng byte trước khi bất kỳ route nào đọc InputStream. Phải là byte
                // chứ không phải string: chốt "không có BOM" chỉ kiểm được ở mức byte, mà BOM lọt
                // vào là thiết bị thật trả badXmlFormat (mục G.2 tài liệu 09B).
                if (req.HasEntityBody && !path.Contains("/transData", StringComparison.OrdinalIgnoreCase))
                {
                    using var bodyBuffer = new MemoryStream();
                    await req.InputStream.CopyToAsync(bodyBuffer);
                    LastReceivedBodyBytes = bodyBuffer.ToArray();
                    LastReceivedBody = Encoding.UTF8.GetString(LastReceivedBodyBytes);
                    LastReceivedContentType = req.ContentType;
                }

                // ─── 0.0. Giả lập thiết bị không phản hồi / rớt mạng / timeout ───
                if (SimulateUnreachable || SimulateUnreachablePorts.Contains(reqPort))
                {
                    context.Response.Abort();
                    return;
                }

                // ─── 0. Giả lập lỗi trên từng Controller/Port cụ thể ───
                if (SimulateFailurePorts.Contains(reqPort))
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.InternalServerError, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>4</statusCode>
                          <statusString>Invalid Operation</statusString>
                          <subStatusCode>devicePortFailure</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // ─── 1. Giả lập Khóa IP do sai mật khẩu liên tiếp (§A.2 & HANDOV_1.MD) ───
                if (IsLockedOut || (FailedAuthLockoutThreshold > 0 && ConsecutiveFailedAuthCount >= FailedAuthLockoutThreshold))
                {
                    IsLockedOut = true;
                    await WriteXmlResponseAsync(res, HttpStatusCode.Forbidden, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>3</statusCode>
                          <statusString>Device Locked</statusString>
                          <subStatusCode>ipAddressLocked</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // M1: /SDK/activateStatus — liveness, KHÔNG yêu cầu digest (KB-01 #1)
                if (method == "GET" && path.TrimStart('/').Equals("SDK/activateStatus", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
                        <Activated xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
                          <Activated>true</Activated>
                        </Activated>
                        """);
                    return;
                }

                // ─── 2. Xử lý Digest Authentication & Nonce Expiry (stale="true") ───
                if (RequireDigestAuth)
                {
                    var authHeader = req.Headers["Authorization"];
                    var clientUsername = ReadAuthDirective(authHeader, "username");
                    var clientRealm = ReadAuthDirective(authHeader, "realm") ?? Realm;
                    var clientNonce = ReadAuthDirective(authHeader, "nonce");
                    var clientUri = ReadAuthDirective(authHeader, "uri") ?? path;
                    var clientQop = ReadAuthDirective(authHeader, "qop");
                    var clientNc = ReadAuthDirective(authHeader, "nc");
                    var clientCnonce = ReadAuthDirective(authHeader, "cnonce");
                    var clientResponse = ReadAuthDirective(authHeader, "response");

                    LastReceivedAuthNonce = clientNonce;

                    bool hasValidAuth;
                    if (VerifyDigestResponseHash)
                    {
                        if (clientUsername == DefaultUser && !string.IsNullOrWhiteSpace(clientNonce) && !string.IsNullOrWhiteSpace(clientResponse))
                        {
                            var ha1 = ComputeMd5Hex($"{DefaultUser}:{clientRealm}:{DefaultPassword}");
                            var ha2 = ComputeMd5Hex($"{method}:{clientUri}");
                            string expectedResponse;
                            if (!string.IsNullOrWhiteSpace(clientQop) && !string.IsNullOrWhiteSpace(clientNc) && !string.IsNullOrWhiteSpace(clientCnonce))
                            {
                                expectedResponse = ComputeMd5Hex($"{ha1}:{clientNonce}:{clientNc}:{clientCnonce}:{clientQop}:{ha2}");
                            }
                            else
                            {
                                expectedResponse = ComputeMd5Hex($"{ha1}:{clientNonce}:{ha2}");
                            }

                            hasValidAuth = string.Equals(clientResponse, expectedResponse, StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            hasValidAuth = false;
                        }
                    }
                    else
                    {
                        hasValidAuth = !string.IsNullOrWhiteSpace(authHeader)
                            && authHeader.StartsWith("Digest", StringComparison.OrdinalIgnoreCase)
                            && authHeader.Contains($"username=\"{DefaultUser}\"", StringComparison.OrdinalIgnoreCase);
                    }

                    // Kịch bản Nonce Expiry: lần đầu trả stale="true" để bắt client tái cấp nonce mới
                    if (SimulateNonceExpiry && hasValidAuth && NonceExpiryTriggerCount == 0)
                    {
                        NonceExpiryTriggerCount++;
                        var staleNonce = Guid.NewGuid().ToString("N");
                        LastIssuedMd5Nonce = staleNonce;
                        res.StatusCode = (int)HttpStatusCode.Unauthorized;
                        res.Headers.Add("WWW-Authenticate", $"Digest realm=\"{Realm}\", qop=\"auth\", nonce=\"{staleNonce}\", opaque=\"{Guid.NewGuid():N}\", algorithm=MD5, stale=\"true\"");
                        res.Close();
                        return;
                    }

                    if (!hasValidAuth)
                    {
                        if (!string.IsNullOrWhiteSpace(authHeader))
                        {
                            ConsecutiveFailedAuthCount++;
                            if (FailedAuthLockoutThreshold > 0 && ConsecutiveFailedAuthCount >= FailedAuthLockoutThreshold)
                            {
                                IsLockedOut = true;
                            }
                        }

                        var nonceMd5 = Guid.NewGuid().ToString("N");
                        LastIssuedMd5Nonce = nonceMd5;
                        var opaque = Guid.NewGuid().ToString("N");

                        res.StatusCode = (int)HttpStatusCode.Unauthorized;
                        if (SimulateDualChallengeHeader)
                        {
                            var nonceSha = Guid.NewGuid().ToString("N");
                            res.Headers.Add("WWW-Authenticate", $"Digest realm=\"{Realm}\", qop=\"auth\", nonce=\"{nonceSha}\", algorithm=SHA-256, stale=\"false\", Digest realm=\"{Realm}\", qop=\"auth\", nonce=\"{nonceMd5}\", algorithm=MD5, stale=\"false\"");
                        }
                        else if (SimulateChallengeWithoutAlgorithm)
                        {
                            res.Headers.Add("WWW-Authenticate", $"Digest realm=\"{Realm}\", qop=\"auth\", nonce=\"{nonceMd5}\", opaque=\"{opaque}\", stale=\"false\"");
                        }
                        else
                        {
                            res.Headers.Add("WWW-Authenticate", $"Digest realm=\"{Realm}\", qop=\"auth\", nonce=\"{nonceMd5}\", opaque=\"{opaque}\", algorithm=MD5, stale=\"false\"");
                        }
                        res.Close();
                        return;
                    }

                    // Xác thực thành công: reset chuỗi sai liên tiếp
                    ConsecutiveFailedAuthCount = 0;
                }

                // ─── 2. Giả lập các loại lỗi ISAPI chuẩn theo Mục G trong tài liệu ───
                if (SimulateMalformedXmlResponse)
                {
                    res.StatusCode = (int)HttpStatusCode.OK;
                    res.ContentType = "application/xml; charset=utf-8";
                    var bytes = Encoding.UTF8.GetBytes("INVALID_XML_PAYLOAD_UNCLOSED_TAG_<ResponseStatus");
                    res.ContentLength64 = bytes.Length;
                    await res.OutputStream.WriteAsync(bytes);
                    res.Close();
                    return;
                }

                if (SimulateMethodNotAllowed)
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>4</statusCode>
                          <statusString>Invalid Operation</statusString>
                          <subStatusCode>methodNotAllowed</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                if (SimulateBadXmlFormat)
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>5</statusCode>
                          <statusString>Invalid XML Format</statusString>
                          <subStatusCode>badXmlFormat</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                if (SimulateBadParameters)
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>6</statusCode>
                          <statusString>Invalid XML Content</statusString>
                          <subStatusCode>badParameters</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                if (SimulateInvalidOperation || SimulateDeviceFailure)
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>4</statusCode>
                          <statusString>Invalid Operation</statusString>
                          <subStatusCode>invalidOperation</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // ─── 3. Router xử lý từng Endpoint ISAPI chuẩn theo Template Matcher ───
                if (await DispatchIsapiRouteAsync(context, method, path))
                    return;

                // H. Default ResponseStatus OK cho các API ghi khác
                await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <ResponseStatus version="1.0" xmlns="{{Ns}}">
                      <requestURL>{{path}}</requestURL>
                      <statusCode>1</statusCode>
                      <statusString>OK</statusString>
                      <subStatusCode>ok</subStatusCode>
                    </ResponseStatus>
                    """);
            }
            catch (Exception)
            {
                res.StatusCode = (int)HttpStatusCode.InternalServerError;
                res.Close();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Đọc một directive từ header Authorization Digest
        /// Created date: 24/08/2026
        /// </summary>
        private static string? ReadAuthDirective(string? header, string key)
        {
            if (string.IsNullOrWhiteSpace(header))
                return null;

            var quoted = Regex.Match(header, key + @"\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase);
            if (quoted.Success)
                return quoted.Groups[1].Value;

            var bare = Regex.Match(header, key + @"\s*=\s*([^\s,""]+)", RegexOptions.IgnoreCase);
            return bare.Success ? bare.Groups[1].Value : null;
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Tính mã hash MD5 trả về chuỗi hex chữ thường
        /// Created date: 24/08/2026
        /// </summary>
        private static string ComputeMd5Hex(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant();
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Ghi dữ liệu XML UTF-8 vào OutputStream phản hồi của HttpListenerResponse
        /// Created date: 17/08/2026
        /// </summary>
        private static async Task WriteXmlResponseAsync(HttpListenerResponse res, HttpStatusCode status, string xml)
        {
            res.StatusCode = (int)status;
            res.ContentType = "application/xml; charset=utf-8";
            var bytes = Encoding.UTF8.GetBytes(xml);
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes);
            res.Close();
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Ghi dữ liệu JSON UTF-8 vào OutputStream phản hồi của HttpListenerResponse
        /// Created date: 17/08/2026
        /// </summary>
        private static async Task WriteJsonResponseAsync(HttpListenerResponse res, HttpStatusCode status, string json)
        {
            res.StatusCode = (int)status;
            res.ContentType = "application/json; charset=utf-8";
            var bytes = Encoding.UTF8.GetBytes(json);
            res.ContentLength64 = bytes.Length;
            await res.OutputStream.WriteAsync(bytes);
            res.Close();
        }

        private static async Task WriteBinaryResponseAsync(HttpListenerResponse res, HttpStatusCode status, string contentType, byte[] data)
        {
            res.StatusCode = (int)status;
            res.ContentType = contentType;
            res.ContentLength64 = data.Length;
            await res.OutputStream.WriteAsync(data);
            res.Close();
        }

        private bool _disposed;

        /// <summary>
        /// Author: Đạt
        /// Description: Giải phóng socket HttpListener và tài nguyên bất đồng bộ của MockServer
        /// Created date: 17/08/2026
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _cts?.Cancel();
            }
            catch { }

            try
            {
                if (_listener.IsListening)
                {
                    _listener.Stop();
                }
                _listener.Prefixes.Clear();
                _listener.Close();
            }
            catch { }

            try
            {
                _cts?.Dispose();
            }
            catch { }

            GC.SuppressFinalize(this);
        }
    }
}
