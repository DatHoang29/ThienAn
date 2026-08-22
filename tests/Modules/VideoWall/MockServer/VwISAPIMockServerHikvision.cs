using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Tests.Modules.VideoWall.MockServer
{
    /// <summary>
    /// Author: Đạt
    /// Description: Mock Server HTTP độc lập giả lập thiết bị Hikvision Video Wall Controller DS-C66S-H88-CL.
    ///              Mở Socket HTTP thật trên localhost (mặc định port 18080), hỗ trợ Digest Authentication (RFC 7616)
    ///              và trả về toàn bộ các mẫu XML & JSON đo thật (100% Ground Truth từ API_Postman_Videowall.md).
    /// Created date: 17/08/2026
    /// </summary>
    public class VwISAPIMockServerHikvision : IDisposable
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

        public string BaseUrl => $"http://{DefaultHost}:{DefaultPort}/";

        // ─── Cờ điều khiển kịch bản giả lập (Dynamic Behavior Flags) ───
        public bool RequireDigestAuth { get; set; } = true;
        public bool SimulateDeviceFailure { get; set; }
        public string? SimulateFailureControllerId { get; set; }
        public HashSet<int> SimulateFailurePorts { get; } = [];
        public int FailedAuthLockoutThreshold { get; set; } = 0;
        public int ConsecutiveFailedAuthCount { get; private set; }
        public bool IsLockedOut { get; set; }
        public bool SimulateNonceExpiry { get; set; }
        public int NonceExpiryTriggerCount { get; set; } = 0;
        public bool IsSupportScene { get; set; } = true;
        public int ActiveSceneId { get; set; } = 1;
        public bool SimulateSaveDataFailure { get; set; }
        public bool SimulateMethodNotAllowed { get; set; }
        public bool SimulateBadXmlFormat { get; set; }
        public bool SimulateBadParameters { get; set; }
        public bool SimulateInvalidOperation { get; set; }
        public bool ScreenCtrlCloseAllThrowsInvalidOperation { get; set; } = true;

        // ─── Bộ đếm số lần gọi API ───
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
            SimulateDeviceFailure = false;
            SimulateFailureControllerId = null;
            SimulateFailurePorts.Clear();
            FailedAuthLockoutThreshold = 0;
            ConsecutiveFailedAuthCount = 0;
            IsLockedOut = false;
            SimulateNonceExpiry = false;
            NonceExpiryTriggerCount = 0;
            IsSupportScene = true;
            ActiveSceneId = 1;
            SimulateSaveDataFailure = false;
            SimulateMethodNotAllowed = false;
            SimulateBadXmlFormat = false;
            SimulateBadParameters = false;
            SimulateInvalidOperation = false;
            ScreenCtrlCloseAllThrowsInvalidOperation = true;

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

                // ─── 2. Xử lý Digest Authentication & Nonce Expiry (stale="true") ───
                if (RequireDigestAuth)
                {
                    var authHeader = req.Headers["Authorization"];
                    var hasValidAuth = !string.IsNullOrWhiteSpace(authHeader)
                        && authHeader.StartsWith("Digest", StringComparison.OrdinalIgnoreCase)
                        && authHeader.Contains($"username=\"{DefaultUser}\"", StringComparison.OrdinalIgnoreCase);

                    // Kịch bản Nonce Expiry: lần đầu trả stale="true" để bắt client tái cấp nonce mới
                    if (SimulateNonceExpiry && hasValidAuth && NonceExpiryTriggerCount == 0)
                    {
                        NonceExpiryTriggerCount++;
                        res.StatusCode = (int)HttpStatusCode.Unauthorized;
                        res.Headers.Add("WWW-Authenticate", $"Digest realm=\"{Realm}\", qop=\"auth\", nonce=\"{Guid.NewGuid():N}\", opaque=\"{Guid.NewGuid():N}\", algorithm=MD5, stale=\"true\"");
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

                        res.StatusCode = (int)HttpStatusCode.Unauthorized;
                        res.Headers.Add("WWW-Authenticate", $"Digest realm=\"{Realm}\", qop=\"auth\", nonce=\"{Guid.NewGuid():N}\", opaque=\"{Guid.NewGuid():N}\", algorithm=MD5, stale=\"false\"");
                        res.Close();
                        return;
                    }

                    // Xác thực thành công: reset chuỗi sai liên tiếp
                    ConsecutiveFailedAuthCount = 0;
                }

                // ─── 2. Giả lập các loại lỗi ISAPI chuẩn theo Mục G trong tài liệu ───
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

                // ─── 3. Router xử lý từng Endpoint ISAPI chuẩn theo Tài liệu đo thật ───

                // A. GET /ISAPI/Security/userCheck (Đo thật §A.1)
                if (path.Contains("/ISAPI/Security/userCheck", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <userCheck version="2.0" xmlns="{{Ns}}">
                          <statusValue>200</statusValue>
                          <statusString>OK</statusString>
                          <isRiskPassword>false</isRiskPassword>
                          <isActivated>true</isActivated>
                        </userCheck>
                        """);
                    return;
                }

                // B.1. GET /ISAPI/DisplayDev/capabilities (Toàn bộ năng lực thiết bị - §B.1 DisplayCap)
                if (path.Equals("/ISAPI/DisplayDev/capabilities", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <DisplayCap version="2.0" xmlns="{{Ns}}">
                          <isSupportScreenCtrl>true</isSupportScreenCtrl>
                          <isSupportVideoWallOperate>true</isSupportVideoWallOperate>
                          <isSupportVideoWall>true</isSupportVideoWall>
                          <VideoCap>
                            <VideoInputsCap>
                              <videoInputPortNums>24</videoInputPortNums>
                              <isSupportColorSetting>true</isSupportColorSetting>
                              <isSupportCutOffSetting>true</isSupportCutOffSetting>
                              <isSupportPictureCapture>true</isSupportPictureCapture>
                            </VideoInputsCap>
                            <VideoOutputsCap>
                              <videoOutputPortNums>24</videoOutputPortNums>
                              <isSupportMultiOutputType>true</isSupportMultiOutputType>
                              <isSupportMultiResolution>true</isSupportMultiResolution>
                              <resolutionCoordinateX min="0" max="65535"/>
                              <resolutionCoordinateY min="0" max="65535"/>
                            </VideoOutputsCap>
                            <VideoStreamingCap>
                              <streamingNums>2048</streamingNums>
                              <isSupportURL>true</isSupportURL>
                              <isSupportIPAddress>true</isSupportIPAddress>
                            </VideoStreamingCap>
                          </VideoCap>
                          <VideoWallCap>
                            <maxWallNums>8</maxWallNums>
                            <maxWindowNums>512</maxWindowNums>
                            <baseOutputSize>1920</baseOutputSize>
                            <isSupportScene>{{(IsSupportScene ? "true" : "false")}}</isSupportScene>
                            <isSupportPlan>true</isSupportPlan>
                            <isSupportRoam>true</isSupportRoam>
                            <isSupportBaseMap>true</isSupportBaseMap>
                            <isSupportVirtualLED>true</isSupportVirtualLED>
                          </VideoWallCap>
                        </DisplayCap>
                        """);
                    return;
                }

                // B.2. GET .../VideoWall/capabilities (Năng lực Video Wall - §9.7.5.6 VideoWallCap)
                if (path.Contains("/capabilities", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <VideoWallCap version="2.0" xmlns="{{Ns}}">
                          <maxWallNums>8</maxWallNums>
                          <maxWindowNums>512</maxWindowNums>
                          <baseOutputSize>1920</baseOutputSize>
                          <isSupportScene>{{(IsSupportScene ? "true" : "false")}}</isSupportScene>
                        </VideoWallCap>
                        """);
                    return;
                }

                // B.3. GET /ISAPI/DisplayDev/decoingDevice/status?format=json (Đo thật §B.2 JSON)
                if (path.Contains("/decoingDevice/status", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteJsonResponseAsync(res, HttpStatusCode.OK, """
                        {
                          "DevCaseStatus": {
                            "height": "4.5U",
                            "row": 8,
                            "col": 2
                          },
                          "MainBoardStatusList": [
                            {
                              "ID": 1,
                              "row": 1,
                              "col": 2,
                              "runTime": 12444,
                              "CPUUtilization": 0,
                              "memoryUtilization": 36,
                              "status": "normal"
                            }
                          ],
                          "BackplaneStatusList": [
                            {
                              "ID": 1,
                              "backplaneTemperature": 60
                            }
                          ],
                          "SubBoardStatusList": [
                            { "ID": 1, "row": 3, "col": 1, "status": "normal", "subBoardType": "input" },
                            { "ID": 2, "row": 4, "col": 1, "status": "normal", "subBoardType": "input" },
                            { "ID": 7, "row": 3, "col": 2, "status": "normal", "subBoardType": "output" },
                            { "ID": 8, "row": 4, "col": 2, "status": "normal", "subBoardType": "output" }
                          ],
                          "SubBoardInterfaceList": [
                            { "ID": 1, "subBoardInterfaceType": "HDMI", "outputPortLinkStatus": "notconnect" },
                            { "ID": 2, "subBoardInterfaceType": "HDMI", "outputPortLinkStatus": "notconnect" },
                            { "ID": 3, "subBoardInterfaceType": "HDMI", "outputPortLinkStatus": "connected" },
                            { "ID": 4, "subBoardInterfaceType": "HDMI", "outputPortLinkStatus": "connected" }
                          ]
                        }
                        """);
                    return;
                }

                // C.1. GET /ISAPI/DisplayDev/Video/outputs/channels (Đo thật §C.1)
                if (path.Equals("/ISAPI/DisplayDev/Video/outputs/channels", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <VideoOutputChannelList version="2.0" xmlns="{{Ns}}">
                          <VideoOutputChannel>
                            <id>17235971</id>
                            <portType>HDMI</portType>
                            <timeSequenceMode>standard</timeSequenceMode>
                            <name>Output 7-3</name>
                            <OutputResolution>
                              <resolution>1920*1080@60HZ</resolution>
                              <imageWidth>0</imageWidth>
                              <imageHeight>0</imageHeight>
                            </OutputResolution>
                            <PortInBoard>
                              <boardID>7</boardID>
                              <portID>3</portID>
                              <ipAddress>10.10.9.236</ipAddress>
                              <port>13191</port>
                            </PortInBoard>
                            <outputPortAccessStatus>normal</outputPortAccessStatus>
                          </VideoOutputChannel>
                          <VideoOutputChannel>
                            <id>17235972</id>
                            <portType>HDMI</portType>
                            <timeSequenceMode>standard</timeSequenceMode>
                            <name>Output 7-4</name>
                            <OutputResolution>
                              <resolution>1920*1080@60HZ</resolution>
                              <imageWidth>0</imageWidth>
                              <imageHeight>0</imageHeight>
                            </OutputResolution>
                            <PortInBoard>
                              <boardID>7</boardID>
                              <portID>4</portID>
                              <ipAddress>10.10.9.236</ipAddress>
                              <port>13191</port>
                            </PortInBoard>
                            <outputPortAccessStatus>normal</outputPortAccessStatus>
                          </VideoOutputChannel>
                        </VideoOutputChannelList>
                        """);
                    return;
                }

                // C.2. GET /ISAPI/DisplayDev/Video/outputs/channels/<channelID> (Đo thật §C.2)
                if (path.StartsWith("/ISAPI/DisplayDev/Video/outputs/channels/", StringComparison.OrdinalIgnoreCase))
                {
                    var chanId = path.Substring("/ISAPI/DisplayDev/Video/outputs/channels/".Length);
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <VideoOutputChannel version="2.0" xmlns="{{Ns}}">
                          <id>{{chanId}}</id>
                          <portType>HDMI</portType>
                          <timeSequenceMode>standard</timeSequenceMode>
                          <name>Output Channel {{chanId}}</name>
                          <outputPortAccessStatus>normal</outputPortAccessStatus>
                        </VideoOutputChannel>
                        """);
                    return;
                }

                // D.1. GET /ISAPI/DisplayDev/VideoWall (Đo thật §D.1 - Danh sách Videowall)
                if (method == "GET" && path.Equals("/ISAPI/DisplayDev/VideoWall", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <VideoWallList version="2.0" xmlns="{{Ns}}">
                          <VideoWall>
                            <id>1</id>
                            <name>VideoWall1</name>
                            <wndStaticMode>blackScreen</wndStaticMode>
                            <streamFailedMode>lastFrame</streamFailedMode>
                            <wallBindOutputStatus>unbound</wallBindOutputStatus>
                          </VideoWall>
                          <VideoWall>
                            <id>2</id>
                            <name>HoangNhu</name>
                            <wndStaticMode>blackScreen</wndStaticMode>
                            <streamFailedMode>lastFrame</streamFailedMode>
                            <wallBindOutputStatus>bound</wallBindOutputStatus>
                          </VideoWall>
                        </VideoWallList>
                        """);
                    return;
                }

                // D.2. PUT /ISAPI/DisplayDev/VideoWall/<id> (Đo thật §D.2)
                if (method == "PUT" && (path.Equals("/ISAPI/DisplayDev/VideoWall/1", StringComparison.OrdinalIgnoreCase)
                                     || path.Equals("/ISAPI/DisplayDev/VideoWall/2", StringComparison.OrdinalIgnoreCase)))
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL></requestURL>
                          <statusCode>1</statusCode>
                          <statusString>OK</statusString>
                          <subStatusCode>ok</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // D.3. GET .../outputs (Đo thật §D.3 - toạ độ ảo 1920x1920)
                if (path.Contains("/outputs", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <WallOutputList version="2.0" xmlns="{{Ns}}">
                          <WallOutput>
                            <id>2</id>
                            <outputID>17235971</outputID>
                            <Rect><Coordinate><x>0</x><y>0</y></Coordinate><width>1920</width><height>1920</height></Rect>
                            <outputWinNum>1</outputWinNum>
                            <coordinateMode>uniformCoordinate</coordinateMode>
                          </WallOutput>
                          <WallOutput>
                            <id>3</id>
                            <outputID>17235972</outputID>
                            <Rect><Coordinate><x>0</x><y>1920</y></Coordinate><width>1920</width><height>1920</height></Rect>
                            <outputWinNum>1</outputWinNum>
                            <coordinateMode>uniformCoordinate</coordinateMode>
                          </WallOutput>
                        </WallOutputList>
                        """);
                    return;
                }

                // E.1. GET .../windows (Đo thật §E.1 - Danh sách Windows trên Videowall)
                if (method == "GET" && path.Contains("/windows", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <WallWindowList version="2.0" xmlns="{{Ns}}">
                          <WallWindow>
                            <id>33554433</id>
                            <wndOperateMode>uniformCoordinate</wndOperateMode>
                            <Rect>
                              <Coordinate><x>0</x><y>0</y></Coordinate>
                              <width>1920</width>
                              <height>1920</height>
                            </Rect>
                            <layerIdx>67108865</layerIdx>
                            <windowMode>1</windowMode>
                            <wndShowMode>subWndMode</wndShowMode>
                            <SubWindowList>
                              <SubWindow>
                                <id>1</id>
                                <SubWindowParam>
                                  <signalMode>video input</signalMode>
                                  <videoInputChannelID>16842753</videoInputChannelID>
                                </SubWindowParam>
                              </SubWindow>
                            </SubWindowList>
                            <wndLockKeep>false</wndLockKeep>
                          </WallWindow>
                          <WallWindow>
                            <id>33554434</id>
                            <wndOperateMode>uniformCoordinate</wndOperateMode>
                            <Rect>
                              <Coordinate><x>0</x><y>1920</y></Coordinate>
                              <width>1920</width>
                              <height>1920</height>
                            </Rect>
                            <layerIdx>67108866</layerIdx>
                            <windowMode>1</windowMode>
                            <wndShowMode>subWndMode</wndShowMode>
                            <SubWindowList>
                              <SubWindow>
                                <id>1</id>
                                <SubWindowParam>
                                  <signalMode>video input</signalMode>
                                  <videoInputChannelID>16842753</videoInputChannelID>
                                </SubWindowParam>
                              </SubWindow>
                            </SubWindowList>
                            <wndLockKeep>false</wndLockKeep>
                          </WallWindow>
                        </WallWindowList>
                        """);
                    return;
                }

                // E.2. POST .../windows (Add Window - Trả về ID 33554435)
                if (method == "POST" && path.Contains("/windows", StringComparison.OrdinalIgnoreCase))
                {
                    AddWindowCallCount++;
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>1</statusCode>
                          <statusString>OK</statusString>
                          <subStatusCode>ok</subStatusCode>
                          <ID>33554435</ID>
                        </ResponseStatus>
                        """);
                    return;
                }

                // C.0. GET /ISAPI/DisplayDev/Video/inputs/channels (Kênh đầu vào video - §B.2 / §H.1)
                if (method == "GET" && path.Contains("/Video/inputs/channels", StringComparison.OrdinalIgnoreCase))
                {
                    GetInputChannelsCallCount++;
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <VideoInputChannelList version="2.0" xmlns="{{Ns}}">
                          <VideoInputChannel>
                            <id>16842753</id>
                            <inputPortType>HDMI</inputPortType>
                            <name>Input 1-1</name>
                            <videoInputChannelAccessStatus>normal</videoInputChannelAccessStatus>
                            <PortInBoard>
                              <boardID>1</boardID>
                              <portID>1</portID>
                              <ipAddress>127.0.0.1</ipAddress>
                              <port>13191</port>
                            </PortInBoard>
                          </VideoInputChannel>
                          <VideoInputChannel>
                            <id>16842754</id>
                            <inputPortType>HDMI</inputPortType>
                            <name>Input 1-2</name>
                            <videoInputChannelAccessStatus>notConnected</videoInputChannelAccessStatus>
                            <PortInBoard>
                              <boardID>1</boardID>
                              <portID>2</portID>
                              <ipAddress>127.0.0.1</ipAddress>
                              <port>13191</port>
                            </PortInBoard>
                          </VideoInputChannel>
                        </VideoInputChannelList>
                        """);
                    return;
                }

                // E.3a. PUT .../windows/<winId>/sub/<subId> (Switch Window Source - §9.4 HANDOV_1.MD)
                if (method == "PUT" && path.Contains("/windows/", StringComparison.OrdinalIgnoreCase) && path.Contains("/sub/", StringComparison.OrdinalIgnoreCase))
                {
                    SwitchSourceCallCount++;
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>1</statusCode>
                          <statusString>OK</statusString>
                          <subStatusCode>ok</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // E.3b. PUT .../windows/<winId>/top (Bring Window To Top - §9.4 HANDOV_1.MD)
                if (method == "PUT" && path.Contains("/windows/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/top", StringComparison.OrdinalIgnoreCase))
                {
                    WindowTopCallCount++;
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>1</statusCode>
                          <statusString>OK</statusString>
                          <subStatusCode>ok</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // E.3c. PUT .../windows/<winId>/bottom (Send Window To Bottom - §9.4 HANDOV_1.MD)
                if (method == "PUT" && path.Contains("/windows/", StringComparison.OrdinalIgnoreCase) && path.EndsWith("/bottom", StringComparison.OrdinalIgnoreCase))
                {
                    WindowBottomCallCount++;
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>1</statusCode>
                          <statusString>OK</statusString>
                          <subStatusCode>ok</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // E.3. PUT .../windows/<winId> (Update Window - Đo thật §E.2)
                if (method == "PUT" && path.Contains("/windows/", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateWindowCallCount++;
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>1</statusCode>
                          <statusString>OK</statusString>
                          <subStatusCode>ok</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // E.4. DELETE .../windows/<winId> (Delete Single Window)
                if (method == "DELETE" && path.Contains("/windows/", StringComparison.OrdinalIgnoreCase))
                {
                    DeleteWindowCallCount++;
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>1</statusCode>
                          <statusString>OK</statusString>
                          <subStatusCode>ok</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // E.5. DELETE .../windows (Delete All Windows)
                if (method == "DELETE" && path.EndsWith("/windows", StringComparison.OrdinalIgnoreCase))
                {
                    DeleteAllWindowsCallCount++;
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>1</statusCode>
                          <statusString>OK</statusString>
                          <subStatusCode>ok</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // F.1. GET .../scene/isRunning (Lấy kịch bản đang chạy - §9.7.7.6 RunningScene)
                if (method == "GET" && path.Contains("/scene/isRunning", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <RunningScene version="2.0" xmlns="{{Ns}}">
                          <sceneID>{{ActiveSceneId}}</sceneID>
                        </RunningScene>
                        """);
                    return;
                }

                // F.2. PUT .../scene/.../activate (Activate Scene - Đo thật §D.2)
                if (method == "PUT" && path.Contains("/activate", StringComparison.OrdinalIgnoreCase))
                {
                    ActivateSceneCallCount++;
                    // Cập nhật ActiveSceneId nếu đường dẫn có ID
                    var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    var actIndex = Array.FindIndex(segments, s => s.Equals("activate", StringComparison.OrdinalIgnoreCase));
                    if (actIndex > 0 && int.TryParse(segments[actIndex - 1], out var parsedSceneId))
                    {
                        ActiveSceneId = parsedSceneId;
                    }

                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>1</statusCode>
                          <statusString>OK</statusString>
                          <subStatusCode>ok</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // F.3. PUT .../scene/.../saveData (SaveSceneData)
                if (method == "PUT" && (path.Contains("saveData", StringComparison.OrdinalIgnoreCase) || path.Contains("/scene/", StringComparison.OrdinalIgnoreCase) && path.Contains("data", StringComparison.OrdinalIgnoreCase)))
                {
                    SaveSceneDataCallCount++;
                    if (SimulateSaveDataFailure)
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

                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>1</statusCode>
                          <statusString>OK</statusString>
                          <subStatusCode>ok</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

                // G. PUT /ISAPI/DisplayDev/ScreenCtrl/closeAll (Đo thật §F.1 - Tắt nguồn màn hình qua RS-232)
                if (path.Contains("/ScreenCtrl/closeAll", StringComparison.OrdinalIgnoreCase))
                {
                    if (ScreenCtrlCloseAllThrowsInvalidOperation)
                    {
                        // Phản hồi thật trên thiết bị khi chưa cắm/cấu hình dây RS-232 serial
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

                    await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                        <?xml version="1.0" encoding="UTF-8"?>
                        <ResponseStatus version="1.0" xmlns="{{Ns}}">
                          <requestURL>{{path}}</requestURL>
                          <statusCode>1</statusCode>
                          <statusString>OK</statusString>
                          <subStatusCode>ok</subStatusCode>
                        </ResponseStatus>
                        """);
                    return;
                }

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
        /// Description: Kiểm tra tính hợp lệ của Authorization header Digest cho người dùng mặc định
        /// Created date: 17/08/2026
        /// </summary>
        private static bool ValidateDigestAuth(HttpListenerRequest req)
        {
            var authHeader = req.Headers["Authorization"];
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Digest", StringComparison.OrdinalIgnoreCase))
                return false;

            // Kiểm tra username có khớp admin không
            return authHeader.Contains($"username=\"{DefaultUser}\"", StringComparison.OrdinalIgnoreCase);
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
