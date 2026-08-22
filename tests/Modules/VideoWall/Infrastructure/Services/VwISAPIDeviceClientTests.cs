namespace Tests.Modules.VideoWall
{
    /// <summary>
    /// Author: Đạt
    /// Description: Kiểm thử VwISAPIDeviceClient thật qua Socket HTTP và Digest Auth với VwISAPIMockServerHikvision.
    ///              Toàn bộ dữ liệu XML phản hồi dựa trên dữ liệu ĐO THẬT từ thiết bị Hikvision DS-C66S-H88-CL.
    /// Created date: 17/08/2026
    /// </summary>
    [Collection("api")]
    public class VwISAPIDeviceClientTests(Host host)
    {
        private readonly IVwISAPIDeviceClient _client = host.Services.GetRequiredService<IVwISAPIDeviceClient>();

        /// <summary>
        /// Author: Đạt
        /// Description: Controller mẫu kết nối tới MockServer port 18080 với thông tin xác thực mặc định
        /// Created date: 15/08/2026
        /// </summary>
        private static VwController TestController => new()
        {
            ID = "ctrl-1",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword
        };

        #region Registration & Credentials

        /// <summary>
        /// Author: Đạt
        /// Description: Controller thiếu IP phải throw InvalidOperationException.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public void VwISAPIDeviceClient_EnsureRegistered_MissingIp_ThrowsInvalidOperationException_Test()
        {
            var controller = new VwController { ID = "ctrl-no-ip", IP = null, Account = "admin", PassWord = "12345" };

            Assert.Throws<InvalidOperationException>(() => _client.EnsureRegistered(controller));
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Base URI phải đúng quy ước cố định của toàn fleet: scheme http, port 80, kết thúc bằng "/".
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public void VwISAPIDeviceClient_EnsureRegistered_BuildsHttpBaseUriOnPort80_Test()
        {
            var controller = new VwController { ID = "ctrl-1", IP = "10.10.9.236", Account = "admin", PassWord = "12345" };

            var baseUri = _client.EnsureRegistered(controller);

            Assert.Equal("http", baseUri.Scheme);
            Assert.Equal(80, baseUri.Port);
            Assert.Equal("10.10.9.236", baseUri.Host);
            Assert.Equal("/", baseUri.AbsolutePath);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Sau khi EnsureRegistered, CredentialCache phải trả đúng NetworkCredential cho base URI + Digest.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public void VwISAPIDeviceClient_EnsureRegistered_RegistersDigestCredentialForController_Test()
        {
            var controller = new VwController { ID = "ctrl-1", IP = "10.10.9.236", Account = "admin", PassWord = "abc123" };

            var baseUri = _client.EnsureRegistered(controller);
            var credential = _client.Credentials.GetCredential(baseUri, "Digest");

            Assert.NotNull(credential);
            Assert.Equal("admin", credential.UserName);
            Assert.Equal("abc123", credential.Password);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Gọi lại EnsureRegistered cho cùng 1 controller với mật khẩu mới phải cập nhật credential ngay.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public void VwISAPIDeviceClient_EnsureRegistered_CalledAgainWithNewPassword_RefreshesCredential_Test()
        {
            var controller = new VwController { ID = "ctrl-1", IP = "10.10.9.236", Account = "admin", PassWord = "old-pass" };
            _client.EnsureRegistered(controller);

            controller.PassWord = "new-pass";
            var baseUri = _client.EnsureRegistered(controller);
            var credential = _client.Credentials.GetCredential(baseUri, "Digest");

            Assert.NotNull(credential);
            Assert.Equal("new-pass", credential.Password);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Controller có cấu hình IP kèm Port tùy chỉnh (ví dụ 127.0.0.1:18080) phải giữ đúng port đó.
        /// Created date: 17/08/2026
        /// </summary>
        [Fact]
        public void VwISAPIDeviceClient_EnsureRegistered_BuildsHttpBaseUriWithCustomPort_Test()
        {
            var controller = new VwController { ID = "ctrl-custom-port", IP = "127.0.0.1:18080", Account = "admin", PassWord = "123" };

            var baseUri = _client.EnsureRegistered(controller);

            Assert.Equal("http", baseUri.Scheme);
            Assert.Equal(18080, baseUri.Port);
            Assert.Equal("127.0.0.1", baseUri.Host);
            Assert.Equal("/", baseUri.AbsolutePath);
        }

        #endregion

        #region ISAPI Core Operations

        /// <summary>
        /// Author: Đạt
        /// Description: GET .../outputs — VwISAPIDeviceClient gửi request HTTP thật tới Mock Server,
        ///              đọc đúng outputID và toạ độ Y=1920 của output thứ 2 (màn dưới).
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_GetOutputs_ParsesRealWallOutputListResponse_Test()
        {
            var result = await _client.GetOutputsAsync(TestController);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.WallOutput.Count);
            Assert.Equal(17235971, result.Data.WallOutput[0].OutputId);
            Assert.Equal(1920, result.Data.WallOutput[1].Rect!.Coordinate!.Y);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: PUT .../scene/{SID}/activate — gửi lệnh kích hoạt scene qua HTTP thật tới Mock Server.
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_ActivateScene_ReturnsSuccess_WithRealResponseStatusEnvelope_Test()
        {
            var result = await _client.ActivateSceneAsync(TestController, "1");

            Assert.True(result.Success);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Mock Server trả ResponseStatus.statusCode = 4 (Invalid Operation) — VwISAPIDeviceClient
        ///              phải đọc statusCode bên trong body và trả về Fail an toàn (never-throw).
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_ActivateScene_ReturnsFailure_WithRealInvalidOperationResponse_Test()
        {
            host.MockServer.SimulateDeviceFailure = true;

            var result = await _client.ActivateSceneAsync(TestController, "1");

            Assert.False(result.Success);
            host.MockServer.SimulateDeviceFailure = false;
        }

        /// <summary>
        /// Author: Đạt
        /// Description: PUT .../windows/{VWMWID} — cập nhật 1 window qua HTTP thật tới Mock Server.
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_UpdateWindow_ReturnsSuccess_WithRealResponseStatusEnvelope_Test()
        {
            var windowRequest = new VwISAPIWindowRequest
            {
                Id = 33554433,
                IdSpecified = true,
                Rect = new VwISAPIRect
                {
                    Coordinate = new VwISAPICoordinate { X = 0, Y = 0 },
                    Width = 1920,
                    Height = 1080
                }
            };

            var result = await _client.UpdateWindowAsync(TestController, "33554433", windowRequest);

            Assert.True(result.Success);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: POST .../windows — tạo window mới qua HTTP thật tới Mock Server.
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_AddWindow_ReturnsSuccess_WithRealCreatedWindowId_Test()
        {

            var windowRequest = new VwISAPIWindowRequest
            {
                Rect = new VwISAPIRect
                {
                    Coordinate = new VwISAPICoordinate { X = 1920, Y = 0 },
                    Width = 1920,
                    Height = 1080
                },
                WindowMode = 1,
                WindowModeSpecified = true,
                SubWindowList = new VwISAPISubWindowList
                {
                    SubWindow =
                    [
                        new()
                        {
                            Id = 1,
                            SubWindowParam = new() { SignalMode = "video input", VideoInputChannelId = "1" }
                        }
                    ]
                }
            };

            var result = await _client.AddWindowAsync(TestController, windowRequest);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(33554435, result.Data.Id);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: DELETE .../windows/{VWMWID} — xóa 1 window qua HTTP thật tới Mock Server.
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_DeleteWindow_ReturnsSuccess_Test()
        {
            var result = await _client.DeleteWindowAsync(TestController, "33554433");

            Assert.True(result.Success);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: DELETE .../windows — xóa toàn bộ window qua HTTP thật tới Mock Server.
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_DeleteAllWindows_ReturnsSuccess_Test()
        {
            var result = await _client.DeleteAllWindowsAsync(TestController);

            Assert.True(result.Success);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: PUT .../scene/{SID}/saveData — lưu snapshot canvas hiện tại vào scene qua HTTP thật.
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_SaveSceneData_ReturnsSuccess_Test()
        {
            var result = await _client.SaveSceneDataAsync(TestController, "1");

            Assert.True(result.Success);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: GET /ISAPI/Security/userCheck — kiểm tra thông tin user và trạng thái kích hoạt.
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_UserCheck_ParsesRealUserCheckResponse_Test()
        {
            var result = await _client.UserCheckAsync(TestController);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.IsSuccess);
            Assert.True(result.Data.IsActivated);
            Assert.False(result.Data.IsRiskPassword);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: GET .../capabilities — đọc đúng năng lực thiết bị (baseOutputSize = 1920).
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_GetCapabilities_ParsesRealVideoWallCapResponse_Test()
        {
            var result = await _client.GetCapabilitiesAsync(TestController);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.IsSupportScene);
            Assert.Equal(1920, result.Data.BaseOutputSize);
            Assert.Equal(512, result.Data.MaxWindowNums);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: GET .../scene/isRunning — đọc đúng Scene ID đang chạy.
        /// Created date: 15/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_GetActiveScene_ParsesRealRunningSceneResponse_Test()
        {
            var result = await _client.GetActiveSceneAsync(TestController);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data.SceneId);
        }

        #endregion

        #region Digest Auth & Circuit Breaker Tests

        /// <summary>
        /// Author: Đạt
        /// Description: Sai mật khẩu Digest Auth — Mock Server trả 401 Unauthorized -> Client trả Fail an toàn.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_UserCheck_WithWrongCredentials_ReturnsUnauthorizedFailure_Test()
        {
            var wrongController = new VwController
            {
                ID = "ctrl-wrong",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = "wrong_user",
                PassWord = "wrong_password"
            };

            var result = await _client.UserCheckAsync(wrongController);

            Assert.False(result.Success);
            Assert.Equal(401, result.HttpStatusCode);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kết nối tới IP/Port không tồn tại — HttpClient timeout / connection refused -> Client trả Fail an toàn.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_UserCheck_WithUnreachableHost_ReturnsConnectionFailure_Test()
        {
            var unreachableController = new VwController
            {
                ID = "ctrl-unreachable",
                IP = "127.0.0.1:19999",
                Account = "admin",
                PassWord = "password"
            };

            var result = await _client.UserCheckAsync(unreachableController);

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Nhiều controller với thông tin xác thực khác nhau hoạt động độc lập qua cùng 1 VwISAPIDeviceClient.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_MultipleControllers_HandleIndependentCredentials_Test()
        {

            var ctrl1 = new VwController
            {
                ID = "ctrl-1",
                IP = "127.0.0.1:18080",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword
            };

            var ctrl2 = new VwController
            {
                ID = "ctrl-2",
                IP = "127.0.0.1:18081",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword
            };

            var ctrl3 = new VwController
            {
                ID = "ctrl-3",
                IP = "127.0.0.1:18082",
                Account = "wrong_user",
                PassWord = "bad_password"
            };

            var ctrl4 = new VwController
            {
                ID = "ctrl-4",
                IP = "127.0.0.1:18083",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword
            };

            var r1 = await _client.UserCheckAsync(ctrl1);
            var r2 = await _client.UserCheckAsync(ctrl2);
            var r3 = await _client.UserCheckAsync(ctrl3);
            var r4 = await _client.UserCheckAsync(ctrl4);

            Assert.True(r1.Success);
            Assert.True(r2.Success);
            Assert.False(r3.Success);
            Assert.True(r4.Success);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Circuit Breaker — sau 2 lần sai mật khẩu liên tiếp (401), request thứ 3 bị
        ///              ngắt mạch Fast-Fail (429) ngay tại máy chủ, ngăn thiết bị khóa IP.
        /// Created date: 17/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_CircuitBreaker_BlocksSubsequentRequests_AfterConsecutiveAuthFailures_Test()
        {
            var testIp = "127.0.0.1:18083";
            _client.ResetCircuitBreaker(testIp);
            host.MockServer.ResetDefaults();

            var wrongCtrl = new VwController
            {
                ID = "ctrl-cb-test",
                IP = testIp,
                Account = "wrong_user",
                PassWord = "wrong_pass"
            };

            // Lần 1: Sai pass -> 401
            var r1 = await _client.UserCheckAsync(wrongCtrl);
            Assert.False(r1.Success);
            Assert.Equal(401, r1.HttpStatusCode);

            // Lần 2: Sai pass -> 401 (đạt ngưỡng threshold = 2 -> Trip Circuit OPEN)
            var r2 = await _client.UserCheckAsync(wrongCtrl);
            Assert.False(r2.Success);
            Assert.Equal(401, r2.HttpStatusCode);

            // Lần 3: Dù gửi đúng pass, Circuit Breaker vẫn chặn ngay tại client với mã 429
            var rightCtrl = new VwController
            {
                ID = "ctrl-cb-test",
                IP = testIp,
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword
            };

            var r3 = await _client.UserCheckAsync(rightCtrl);
            Assert.False(r3.Success);
            Assert.Equal(429, r3.HttpStatusCode);
            Assert.Contains("Circuit breaker", r3.ErrorMessage ?? string.Empty);

            // Dọn dẹp reset cho các test khác
            _client.ResetCircuitBreaker(testIp);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Circuit Breaker — gọi thành công sẽ reset bộ đếm lỗi xác thực.
        /// Created date: 17/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_CircuitBreaker_ResetsOnSuccess_Test()
        {
            var testIp = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}";
            _client.ResetCircuitBreaker(testIp);
            host.MockServer.ResetDefaults();

            var wrongCtrl = new VwController
            {
                ID = "ctrl-reset-test",
                IP = testIp,
                Account = "wrong_user",
                PassWord = "wrong_pass"
            };

            // Lần 1: Sai pass -> 401 (Fail count = 1)
            var r1 = await _client.UserCheckAsync(wrongCtrl);
            Assert.False(r1.Success);

            // Lần 2: Đúng pass -> 200 (Success -> Fail count reset về 0)
            var r2 = await _client.UserCheckAsync(TestController);
            Assert.True(r2.Success);

            // Lần 3: Sai pass lại -> 401 nhưng CHƯA bị block 429 (vì fail count mới = 1)
            var r3 = await _client.UserCheckAsync(wrongCtrl);
            Assert.False(r3.Success);
            Assert.Equal(401, r3.HttpStatusCode);

            _client.ResetCircuitBreaker(testIp);
        }

        #endregion

        #region Extended Input Channels & Window Controls

        /// <summary>
        /// Author: Đạt
        /// Description: GET .../Video/inputs/channels — VwISAPIDeviceClient gửi request HTTP thật tới Mock Server,
        ///              đọc đúng danh sách kênh đầu vào (§9.4 HANDOV_1.MD, §B.2 / §H.1).
        /// Created date: 17/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_GetInputChannels_ParsesRealInputChannelList_Test()
        {
            var result = await _client.GetInputChannelsAsync(TestController);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.VideoInputChannel.Count);
            Assert.Equal(16842753, result.Data.VideoInputChannel[0].Id);
            Assert.Equal("HDMI", result.Data.VideoInputChannel[0].InputPortType);
            Assert.Equal("normal", result.Data.VideoInputChannel[0].VideoInputChannelAccessStatus);
            Assert.True(host.MockServer.GetInputChannelsCallCount >= 1);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: PUT .../windows/<id>/sub/<subId> — Đổi nguồn tín hiệu SubWindow trực tiếp thành công (§9.4 HANDOV_1.MD).
        /// Created date: 17/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_SwitchWindowSource_ReturnsSuccess_Test()
        {
            var result = await _client.SwitchWindowSourceAsync(TestController, "33554433", "1", 16842753);

            Assert.True(result.Success);
            Assert.Equal(200, result.HttpStatusCode);
            Assert.True(host.MockServer.SwitchSourceCallCount >= 1);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: PUT .../windows/<id>/top — Đưa cửa sổ lên trên cùng của Z-Order thành công (§9.4 HANDOV_1.MD).
        /// Created date: 17/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_BringWindowToTop_ReturnsSuccess_Test()
        {
            var result = await _client.BringWindowToTopAsync(TestController, "33554433");

            Assert.True(result.Success);
            Assert.Equal(200, result.HttpStatusCode);
            Assert.True(host.MockServer.WindowTopCallCount >= 1);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: PUT .../windows/<id>/bottom — Đưa cửa sổ xuống dưới cùng của Z-Order thành công (§9.4 HANDOV_1.MD).
        /// Created date: 17/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceClient_SendWindowToBottom_ReturnsSuccess_Test()
        {
            var result = await _client.SendWindowToBottomAsync(TestController, "33554433");

            Assert.True(result.Success);
            Assert.Equal(200, result.HttpStatusCode);
            Assert.True(host.MockServer.WindowBottomCallCount >= 1);
        }

        #endregion
    }
}
