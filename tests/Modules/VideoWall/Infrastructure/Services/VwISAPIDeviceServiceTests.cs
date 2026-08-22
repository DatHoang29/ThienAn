namespace Tests.Modules.VideoWall
{
    /// <summary>
    /// Author: Đạt
    /// Description: Kiểm thử tích hợp VwISAPIDeviceService qua VwISAPIMockServerHikvision và SqlSugar Test DB.
    ///              Xác thực toàn bộ quy trình nghiệp vụ cấp cao: GetInputChannels, ActivateScene, SyncSceneWindows,
    ///              SwitchSource, SetWindowLayer và ResetCircuitBreaker.
    /// Created date: 18/08/2026
    /// </summary>
    [Collection("api")]
    public class VwISAPIDeviceServiceTests(Host host)
    {
        private readonly IVwISAPIDeviceService _service = host.Services.GetRequiredService<IVwISAPIDeviceService>();
        private readonly IVwISAPIDeviceClient _client = host.Services.GetRequiredService<IVwISAPIDeviceClient>();
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();

        private static VwController TestController => new()
        {
            ID = "ctrl-svc-1",
            Name = "TEST_Controller_Svc",
            Code = "TEST_CTRL_SVC",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable
        };

        #region GetInputChannels

        /// <summary>
        /// Description: Lấy danh sách kênh đầu vào thành công từ Mock Server qua VwISAPIDeviceService.
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_GetInputChannels_ReturnsChannels_Test()
        {
            // Arrange
            var controller = TestController;

            // Act
            var result = await _service.GetInputChannels(controller);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.VideoInputChannel);
            Assert.NotEmpty(result.VideoInputChannel);
            Assert.Equal(16842753, result.VideoInputChannel[0].Id);
        }

        #endregion

        #region ActivateScene

        /// <summary>
        /// Description: Kích hoạt kịch bản thành công trên controller khi thiết bị hỗ trợ scene.
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_ActivateScene_Success_Test()
        {
            // Arrange
            var controller = TestController;
            var scene = new VwScene
            {
                ID = "scene-svc-1",
                Code = "TEST_SCENE_SVC",
                OutputId = "1",
                ControllerId = controller.ID
            };

            // Act & Assert
            var exception = await Record.ExceptionAsync(() =>
                _service.ActivateScene(scene, [controller], [controller.ID]));

            Assert.Null(exception);
        }

        /// <summary>
        /// Description: Kích hoạt kịch bản khi scene.OutputId rỗng -> bỏ qua không gửi lệnh tới thiết bị.
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_ActivateScene_EmptyOutputId_DoesNothing_Test()
        {
            // Arrange
            var controller = TestController;
            var scene = new VwScene
            {
                ID = "scene-svc-2",
                Code = "TEST_SCENE_EMPTY_OUT",
                OutputId = null,
                ControllerId = controller.ID
            };

            // Act & Assert
            var exception = await Record.ExceptionAsync(() =>
                _service.ActivateScene(scene, [controller], [controller.ID]));

            Assert.Null(exception);
        }

        #endregion

        #region SyncSceneWindowsToDevice

        /// <summary>
        /// Description: Đồng bộ toàn bộ danh sách cửa sổ của Scene lên thiết bị qua Mock Server thành công.
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SyncSceneWindowsToDevice_Success_Test()
        {
            // Arrange
            var controller = TestController;
            await _db.Insertable(controller).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = "TEST_SCENE_SYNC_1",
                Code = "TEST_SCENE_SYNC",
                Name = "Test Scene Sync",
                OutputId = "1",
                ControllerId = controller.ID,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var source = new VwSource
            {
                ID = "TEST_SRC_SYNC_1",
                Code = "TEST_SRC_SYNC",
                Name = "Camera 01",
                SignalNo = 1,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(source).ExecuteCommandAsync();

            var window = new VwWindowScene
            {
                ID = "TEST_WIN_SYNC_1",
                Code = "TEST_WIN_SYNC",
                SceneId = scene.ID,
                SourceId = source.ID,
                X = 0,
                Y = 0,
                W = 1920,
                H = 1080
            };
            await _db.Insertable(window).ExecuteCommandAsync();

            // Act & Assert
            var exception = await Record.ExceptionAsync(() =>
                _service.SyncSceneWindowsToDevice(scene.ID));

            Assert.Null(exception);
        }

        #endregion

        #region SwitchWindowSource & SetWindowLayer

        /// <summary>
        /// Description: Chuyển nguồn tín hiệu SubWindow thành công qua Mock Server.
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SwitchWindowSource_Success_Test()
        {
            // Arrange
            var controller = TestController;

            // Act & Assert
            var exception = await Record.ExceptionAsync(() =>
                _service.SwitchWindowSource(controller, "1", "1", 2));

            Assert.Null(exception);
        }

        /// <summary>
        /// Description: Điều chỉnh thứ tự lớp Z-Order (Top / Bottom) thành công qua Mock Server.
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SetWindowLayer_TopAndBottom_Success_Test()
        {
            // Arrange
            var controller = TestController;

            // Act & Assert Top
            var exTop = await Record.ExceptionAsync(() =>
                _service.SetWindowLayer(controller, "1", VwWindowLayerAction.Top));
            Assert.Null(exTop);

            // Act & Assert Bottom
            var exBottom = await Record.ExceptionAsync(() =>
                _service.SetWindowLayer(controller, "1", VwWindowLayerAction.Bottom));
            Assert.Null(exBottom);
        }

        #endregion

        #region Circuit Breaker

        /// <summary>
        /// Description: Kiểm tra vòng đời Circuit Breaker trên VwISAPIDeviceService (Ghi nhận lỗi, Khóa, Khôi phục khi thành công).
        /// </summary>
        [Fact]
        public void VwISAPIDeviceService_CircuitBreaker_FullLifecycle_Test()
        {
            // Arrange
            var testIp = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}";

            // 1st failure -> not yet blocked
            _service.RecordCircuitBreakerFailure(testIp, 401);
            Assert.False(_service.IsCircuitBreakerBlocked(testIp, out _));

            // 2nd failure -> blocked
            _service.RecordCircuitBreakerFailure(testIp, 401);
            Assert.True(_service.IsCircuitBreakerBlocked(testIp, out var remaining));
            Assert.True(remaining > TimeSpan.Zero);

            // Record success -> clears block
            _service.RecordAuthSuccess(testIp);
            Assert.False(_service.IsCircuitBreakerBlocked(testIp, out _));
        }

        /// <summary>
        /// Description: Reset Circuit Breaker xóa bỏ trạng thái lỗi tạm khóa thành công cho từng Controller.
        /// </summary>
        [Fact]
        public void VwISAPIDeviceService_ResetCircuitBreaker_ClearsBlock_Test()
        {
            // Arrange
            var testIp = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}";
            _service.RecordCircuitBreakerFailure(testIp, 401);
            _service.RecordCircuitBreakerFailure(testIp, 401);

            Assert.True(_service.IsCircuitBreakerBlocked(testIp, out _));

            // Act
            _service.ResetCircuitBreaker(testIp);

            // Assert
            Assert.False(_service.IsCircuitBreakerBlocked(testIp, out _));
        }

        /// <summary>
        /// Description: Reset toàn bộ Circuit Breakers trên hệ thống qua VwISAPIDeviceService.
        /// </summary>
        [Fact]
        public void VwISAPIDeviceService_ResetAllCircuitBreakers_ClearsAll_Test()
        {
            // Arrange
            var testIp1 = "127.0.0.1:18080";
            var testIp2 = "127.0.0.1:18081";
            _service.RecordCircuitBreakerFailure(testIp1, 401);
            _service.RecordCircuitBreakerFailure(testIp1, 401);
            _service.RecordCircuitBreakerFailure(testIp2, 401);
            _service.RecordCircuitBreakerFailure(testIp2, 401);

            Assert.True(_service.IsCircuitBreakerBlocked(testIp1, out _));
            Assert.True(_service.IsCircuitBreakerBlocked(testIp2, out _));

            // Act
            _service.ResetAllCircuitBreakers();

            // Assert
            Assert.False(_service.IsCircuitBreakerBlocked(testIp1, out _));
            Assert.False(_service.IsCircuitBreakerBlocked(testIp2, out _));
        }

        #endregion
    }
}
