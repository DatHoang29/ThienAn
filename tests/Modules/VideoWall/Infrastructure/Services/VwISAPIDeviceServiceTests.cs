using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Module.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Dto.ISAPI;
using Module.VideoWall.Core.Options;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Access;
using Module.VideoWall.Infrastructure.Services.ISAPIDevice;

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
        private const string TestPrefix = "TEST_SVC_";
        private readonly IVwISAPIDeviceService _service = host.Services.GetRequiredService<IVwISAPIDeviceService>();
        private readonly IVwISAPIDeviceClient _client = host.Services.GetRequiredService<IVwISAPIDeviceClient>();
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly IMessageBus _bus = host.Services.GetRequiredService<IMessageBus>();
        private readonly VwISAPIMockServerHikvision _mock = host.MockServer;

        private static VwController TestController => new()
        {
            ID = $"ctrl-svc-{Guid.NewGuid():N}",
            Name = "TEST_Controller_Svc",
            Code = $"{TestPrefix}CTRL_{Guid.NewGuid():N}",
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
            host.MockServer.ResetDefaults();

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
            Assert.Equal(1, host.MockServer.ActivateSceneCallCount);
        }

        /// <summary>
        /// Description: Kích hoạt kịch bản khi scene.OutputId rỗng -> ném lỗi yêu cầu khai báo SID kịch bản trên thiết bị.
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_ActivateScene_EmptyOutputId_ThrowsSceneSidRequired_Test()
        {
            host.MockServer.ResetDefaults();

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
            await Assert.ThrowsAnyAsync<Exception>(() =>
                _service.ActivateScene(scene, [controller], [controller.ID]));
        }

        #endregion

        #region SyncSceneWindowsToDevice

        /// <summary>
        /// Description: Đồng bộ toàn bộ danh sách cửa sổ của Scene lên thiết bị qua Mock Server thành công.
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SyncSceneWindowsToDevice_Success_Test()
        {
            host.MockServer.ResetDefaults();

            // Arrange
            var controller = TestController;
            await _db.Insertable(controller).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCENE_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}SCENE_{Guid.NewGuid():N}",
                Name = "Test Scene Sync",
                OutputId = "1",
                ControllerId = controller.ID,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var source = new VwSource
            {
                ID = $"{TestPrefix}SRC_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}SRC_{Guid.NewGuid():N}",
                Name = "Camera 01",
                SignalNo = 1,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(source).ExecuteCommandAsync();

            var window = new VwWindowScene
            {
                ID = $"{TestPrefix}WIN_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}WIN_{Guid.NewGuid():N}",
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
            Assert.True(host.MockServer.AddWindowCallCount >= 1);
            Assert.Equal(1, host.MockServer.SaveSceneDataCallCount);
            Assert.Equal(1, host.MockServer.DeleteAllWindowsCallCount);
        }

        #endregion

        #region SetWindowLayer

        /// <summary>
        /// Author: Đạt
        /// Description: Điều chỉnh thứ tự lớp Z-Order (Top / Bottom) gửi đúng lệnh xuống Mock Server.
        /// Created date: 24/08/2026
        /// </summary>
        [Theory]
        [InlineData(VwWindowLayerAction.Top)]
        [InlineData(VwWindowLayerAction.Bottom)]
        public async Task VwISAPIDeviceService_SetWindowLayer_SendsLayerCommand_Test(VwWindowLayerAction action)
        {
            host.MockServer.ResetDefaults();

            // Arrange
            var controller = TestController;

            // Act & Assert
            var exception = await Record.ExceptionAsync(() =>
                _service.SetWindowLayer(controller, "1", action));
            Assert.Null(exception);

            if (action == VwWindowLayerAction.Top)
            {
                Assert.Equal(1, host.MockServer.WindowTopCallCount);
                Assert.Equal(0, host.MockServer.WindowBottomCallCount);
            }
            else
            {
                Assert.Equal(0, host.MockServer.WindowTopCallCount);
                Assert.Equal(1, host.MockServer.WindowBottomCallCount);
            }
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
            var testIp = "127.0.0.1:18091";
            _service.ResetCircuitBreaker(testIp);

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
            var testIp = "127.0.0.1:18092";
            _service.ResetCircuitBreaker(testIp);
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
            var testIp1 = "127.0.0.1:18093";
            var testIp2 = "127.0.0.1:18094";
            _service.ResetCircuitBreaker(testIp1);
            _service.ResetCircuitBreaker(testIp2);
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

        /// <summary>
        /// Description: Circuit Breaker tuân thủ ngưỡng MaxConsecutiveFailures từ profile (chưa chạm ngưỡng thì chưa chặn, chạm ngưỡng mới chặn).
        /// </summary>
        [Fact]
        public void VwISAPIDeviceService_CircuitBreaker_RespectsProfileThreshold_Test()
        {
            var testIp = "127.0.0.1:18090";
            _service.ResetCircuitBreaker(testIp);

            var threshold = VwWallProfile.MaxConsecutiveFailures;
            for (var i = 0; i < threshold - 1; i++)
            {
                _service.RecordCircuitBreakerFailure(testIp, 401);
                Assert.False(_service.IsCircuitBreakerBlocked(testIp, out _));
            }

            _service.RecordCircuitBreakerFailure(testIp, 401);
            Assert.True(_service.IsCircuitBreakerBlocked(testIp, out var remaining));
            Assert.True(remaining > TimeSpan.Zero);

            _service.ResetCircuitBreaker(testIp);
        }

        /// <summary>
        /// Description: Cấu hình profile override từ options (DeviceIntegration.json) có độ ưu tiên cao hơn giá trị mặc định của code.
        /// </summary>
        [Fact]
        public void VwISAPIDeviceService_ProfileOverride_FromOptions_TakesPrecedence_Test()
        {
            var customOptions = Microsoft.Extensions.Options.Options.Create(new VwDeviceOptions
            {
                Device = new VwDeviceConnectionOptions
                {
                    Profile = new VwDeviceProfileOptions
                    {
                        MaxConsecutiveFailures = 3,
                        BlockMinutes = 10
                    }
                }
            });

            using var scope = host.Services.CreateScope();
            var service = new VwISAPIDeviceService(
                _client,
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwController>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwScene>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwWindowScene>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwSource>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwScreen>>(),
                scope.ServiceProvider.GetRequiredService<VwSceneRegionService>(),
                scope.ServiceProvider.GetRequiredService<VwISAPICredentialResolver>(),
                scope.ServiceProvider.GetRequiredService<VwOrgAccessService>(),
                scope.ServiceProvider.GetRequiredService<BaseCacheService>(),
                scope.ServiceProvider.GetRequiredService<IVwEventTriggerLogWriter>(),
                customOptions,
                scope.ServiceProvider.GetRequiredService<ILogger<VwISAPIDeviceService>>());

            var testIp = "127.0.0.1:18091";
            service.ResetCircuitBreaker(testIp);

            // 1st & 2nd failure -> not blocked (because threshold is 3)
            service.RecordCircuitBreakerFailure(testIp, 401);
            Assert.False(service.IsCircuitBreakerBlocked(testIp, out _));

            service.RecordCircuitBreakerFailure(testIp, 401);
            Assert.False(service.IsCircuitBreakerBlocked(testIp, out _));

            // 3rd failure -> blocked
            service.RecordCircuitBreakerFailure(testIp, 401);
            Assert.True(service.IsCircuitBreakerBlocked(testIp, out var remaining));
            Assert.True(remaining > TimeSpan.FromMinutes(5));

            service.ResetCircuitBreaker(testIp);
        }

        #endregion

        #region Setup

        /// <summary>
        /// Author: Đạt
        /// Description: Ping xác thực Digest vào bộ điều khiển thành công và trả về bước Success với GET userCheck.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_Ping_ReturnsSuccessStep_Test()
        {
            var controller = TestController;
            await _db.Insertable(controller).ExecuteCommandAsync();

            var step = await _service.Ping(controller.ID);

            Assert.NotNull(step);
            Assert.True(step.Success);
            Assert.Equal("GET", step.Method);
            Assert.Contains("userCheck", step.Endpoint);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Ping controller không tồn tại trong CSDL phải ném ngoại lệ NotExist (chặn trước khi phát HTTP).
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_Ping_ControllerNotFound_Throws_Test()
        {
            var nonExistentId = $"{TestPrefix}CTRL_NOTFOUND_{Guid.NewGuid():N}";

            await Assert.ThrowsAnyAsync<Exception>(() => _service.Ping(nonExistentId));
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Ping controller thiếu IP phải ném ngoại lệ và không phát bất kỳ request nào tới Mock Server.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_Ping_ControllerWithoutIp_Throws_Test()
        {
            host.MockServer.ResetDefaults();
            var controller = new VwController
            {
                ID = $"{TestPrefix}CTRL_NOIP_{Guid.NewGuid():N}",
                Name = "Controller Without IP",
                Code = $"{TestPrefix}CTRL_NOIP_{Guid.NewGuid():N}",
                IP = null,
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(controller).ExecuteCommandAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => _service.Ping(controller.ID));
            Assert.Empty(host.MockServer.ReceivedRequests);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Ping controller thiếu Account/PassWord phải ném ngoại lệ và không phát bất kỳ request nào tới Mock Server.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_Ping_ControllerWithoutCredential_Throws_Test()
        {
            host.MockServer.ResetDefaults();
            var controller = new VwController
            {
                ID = $"{TestPrefix}CTRL_NOCRED_{Guid.NewGuid():N}",
                Name = "Controller Without Credential",
                Code = $"{TestPrefix}CTRL_NOCRED_{Guid.NewGuid():N}",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = null,
                PassWord = null,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(controller).ExecuteCommandAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => _service.Ping(controller.ID));
            Assert.Empty(host.MockServer.ReceivedRequests);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Ping controller với PassWord sai không được ném ngoại lệ mà trả về VwSetupSceneStep thất bại (Success=false, HttpStatus=401).
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_Ping_WrongPassword_ReturnsFailedStepWithoutThrowing_Test()
        {
            host.MockServer.ResetDefaults();
            host.MockServer.VerifyDigestResponseHash = true;
            var testIp = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPorts[1]}";
            _service.ResetCircuitBreaker(testIp);

            try
            {
                var controller = new VwController
                {
                    ID = $"{TestPrefix}CTRL_WRONGPWD_{Guid.NewGuid():N}",
                    Name = "Controller Wrong Password",
                    Code = $"{TestPrefix}CTRL_WRONGPWD_{Guid.NewGuid():N}",
                    IP = testIp,
                    Account = VwISAPIMockServerHikvision.DefaultUser,
                    PassWord = "IncorrectPassword!",
                    Status = BaseEnums.StatusEnum.Enable
                };
                await _db.Insertable(controller).ExecuteCommandAsync();

                var step = await _service.Ping(controller.ID);

                Assert.NotNull(step);
                Assert.False(step.Success);
                Assert.Equal(401, step.HttpStatus);
            }
            finally
            {
                _service.ResetCircuitBreaker(testIp);
                host.MockServer.ResetDefaults();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Khảo sát thiết bị chỉ phát lệnh đọc, trả về thông tin năng lực/tường/kênh và không phát bất kỳ lệnh ghi nào.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_Probe_ReturnsReadOnlyStepsAndMismatchTables_Test()
        {
            var controller = TestController;
            await _db.Insertable(controller).ExecuteCommandAsync();

            var output = await _service.Probe(new VwProbeDeviceInput { ID = controller.ID });

            Assert.NotNull(output);
            Assert.True(output.Reachable);
            Assert.NotEmpty(output.Walls);
            Assert.True(output.Steps.Count > 0);
            Assert.All(output.Steps, s => Assert.Equal("GET", s.Method));
        }

        /// <summary>
        /// Author: Đạt
        /// Description: SetupScene mặc định DryRun=true chỉ tạo payload thử nghiệm và đánh dấu Skipped=true cho các bước ghi, không phát lệnh ghi xuống thiết bị.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SetupScene_DryRunDefault_EmitsNoWriteCommand_Test()
        {
            var controller = TestController;
            await _db.Insertable(controller).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCENE_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}SCENE_{Guid.NewGuid():N}",
                OutputId = "1",
                ControllerId = controller.ID,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var window = new VwWindowScene
            {
                ID = $"{TestPrefix}WIN_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}WIN_{Guid.NewGuid():N}",
                SceneId = scene.ID,
                X = 0,
                Y = 0,
                W = 1920,
                H = 1080
            };
            await _db.Insertable(window).ExecuteCommandAsync();

            var output = await _service.SetupScene(new VwSetupSceneInput
            {
                ControllerId = controller.ID,
                SceneId = scene.ID,
                DryRun = true,
                ResetWindows = true
            });

            Assert.NotNull(output);
            Assert.True(output.Success);
            Assert.True(output.DryRun);

            var writeSteps = output.Steps.Where(s => s.Method is "POST" or "PUT" or "DELETE").ToList();
            Assert.NotEmpty(writeSteps);
            Assert.All(writeSteps, s => Assert.True(s.Skipped));
        }

        /// <summary>
        /// Author: Đạt
        /// Description: SetupScene tự động chọn tường bound (Wall 2) thay vì mặc định Wall 1 chưa gắn màn hình.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SetupScene_SelectsBoundWallNotWallOne_Test()
        {
            _mock.ResetDefaults();
            _mock.SimulateWall1Unbound = true;

            var controller = TestController;
            await _db.Insertable(controller).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCENE_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}SCENE_{Guid.NewGuid():N}",
                OutputId = "1",
                ControllerId = controller.ID,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var output = await _service.SetupScene(new VwSetupSceneInput
            {
                ControllerId = controller.ID,
                SceneId = scene.ID
            });

            Assert.NotNull(output);
            Assert.Equal(2, output.WallNo);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Khi scene không có SID (OutputId rỗng), bước Lưu kịch bản bị đánh dấu thất bại (Success=false, không phải Skipped) và kịch bản tổng thể Success=false.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SetupScene_SceneWithoutSid_MarksStepFailed_Test()
        {
            var controller = TestController;
            await _db.Insertable(controller).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCENE_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}SCENE_{Guid.NewGuid():N}",
                OutputId = null,
                ControllerId = controller.ID,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var output = await _service.SetupScene(new VwSetupSceneInput
            {
                ControllerId = controller.ID,
                SceneId = scene.ID
            });

            Assert.NotNull(output);
            Assert.False(output.Success);

            var saveStep = output.Steps.FirstOrDefault(s => s.Name == "Lưu kịch bản");
            Assert.NotNull(saveStep);
            Assert.False(saveStep.Success);
            Assert.False(saveStep.Skipped);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Mọi SID nằm ngoài dải thiết bị nhận (1..maxSceneNums) đều phải bị chặn TRƯỚC KHI
        ///              phát lệnh, với thông báo nêu rõ khoảng hợp lệ: vượt trần, bằng 0, âm, và không
        ///              phải số (nhánh int.TryParse thất bại — không được để lọt xuống thiết bị rồi
        ///              nhận badParameters khó hiểu).
        ///
        ///              Vì sao bài này từng KHÔNG THỂ viết: mock server trước đây không trả
        ///              &lt;maxSceneNums&gt; nên nó luôn deserialize ra 0, mà điều kiện chặn là
        ///              `if (output.MaxSceneNums > 0 && ...)` — nhánh này không bao giờ chạm tới được.
        ///              Nay mock có MaxSceneNums cấu hình được nên kiểm được thật.
        /// Created date: 26/08/2026
        /// </summary>
        [Theory]
        [InlineData("10")]  // vượt maxSceneNums
        [InlineData("0")]   // dưới dải 1..max, dễ lọt nhất vì "0" vẫn parse được thành số
        [InlineData("-1")]  // âm
        [InlineData("abc")] // không phải số (int.TryParse thất bại)
        public async Task VwISAPIDeviceService_SetupScene_SidOutsideDeviceRange_MarksStepFailed_Test(string sid)
        {
            // Arrange
            var output = await RunSetupSceneWithSid(sid, maxSceneNums: 5);

            // Assert
            AssertSidRejected(output, sid, maxSceneNums: 5);
        }

        /// <summary>
        /// Description: Dựng fixture chuẩn rồi ghi đè SID của kịch bản và trần maxSceneNums của thiết
        ///              bị, sau đó chạy SetupScene ở chế độ GHI THẬT.
        ///
        ///              DryRun = false có chủ ý: nếu nhánh chặn SID không hoạt động thì lệnh sẽ thật
        ///              sự bắn xuống mock, và phép đếm SaveSceneDataCallCount == 0 ở
        ///              <see cref="AssertSidRejected"/> sẽ đỏ. Chạy DryRun thì mọi lệnh đều bị bỏ qua
        ///              nên phép đếm đó không chứng minh được gì.
        /// </summary>
        private async Task<VwSetupSceneOutput> RunSetupSceneWithSid(string sid, int maxSceneNums)
        {
            host.MockServer.ResetDefaults();
            host.MockServer.MaxSceneNums = maxSceneNums;

            var (controller, scene) = await CreateSetupSceneFixtures();

            // Fixture seed OutputId = "1"; SetupScene đọc kịch bản từ CSDL nên phải ghi đè ở đó.
            await _db.Updateable<VwScene>()
                .SetColumns(s => new VwScene { OutputId = sid })
                .Where(s => s.ID == scene.ID)
                .ExecuteCommandAsync();

            return await _service.SetupScene(new VwSetupSceneInput
            {
                ControllerId = controller.ID,
                SceneId = scene.ID,
                DryRun = false,
                ResetWindows = true,
                Activate = true
            });
        }

        /// <summary>
        /// Description: Bước "Lưu kịch bản" phải ĐỎ với thông báo nêu khoảng hợp lệ, và tuyệt đối
        ///              không có lệnh saveData/activate nào bắn xuống thiết bị.
        /// </summary>
        private void AssertSidRejected(VwSetupSceneOutput output, string sid, int maxSceneNums)
        {
            Assert.NotNull(output);
            Assert.False(output.Success);

            var saveStep = Assert.Single(output.Steps.Where(s => s.Name == "Lưu kịch bản"));
            Assert.False(saveStep.Success);

            // Bước ĐỎ, không phải bước bỏ qua — bỏ qua sẽ khiến cả kịch bản báo thành công.
            Assert.False(saveStep.Skipped);
            Assert.NotNull(saveStep.Message);
            Assert.Contains($"1..{maxSceneNums}", saveStep.Message);
            Assert.Contains(sid, saveStep.Message);

            // Chặn TRƯỚC KHI gọi thiết bị: không lệnh ghi nào được phát.
            Assert.Equal(0, host.MockServer.SaveSceneDataCallCount);
            Assert.Equal(0, host.MockServer.ActivateSceneCallCount);
        }

        #region Sequence Verification (§Nhóm B)

        /// <summary>
        /// Author: Đạt
        /// Description: SetupScene tuân thủ bất biến an toàn lằn ranh: thực hiện toàn bộ các bước đọc trước khi gửi bất kỳ lệnh ghi nào xuống thiết bị.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SetupScene_PerformsAllReadsBeforeAnyWrite_Test()
        {
            host.MockServer.ResetDefaults();
            var (controller, scene) = await CreateSetupSceneFixtures();

            var output = await _service.SetupScene(new VwSetupSceneInput
            {
                ControllerId = controller.ID,
                SceneId = scene.ID,
                DryRun = false,
                ResetWindows = true,
                Activate = true
            });

            Assert.NotNull(output);
            Assert.True(output.Success);

            var log = host.MockServer.ReceivedRequests;
            var firstWriteIdx = -1;
            for (var i = 0; i < log.Count; i++)
            {
                var req = log[i];
                if (req.Contains("POST ", StringComparison.OrdinalIgnoreCase) ||
                    req.Contains("PUT ", StringComparison.OrdinalIgnoreCase) ||
                    req.Contains("DELETE ", StringComparison.OrdinalIgnoreCase))
                {
                    firstWriteIdx = i;
                    break;
                }
            }

            Assert.True(firstWriteIdx >= 0, "Ít nhất một lệnh ghi (POST/PUT/DELETE) phải được thực thi");

            var idxUserCheck = FirstIndexOf(log, "GET", "/ISAPI/Security/userCheck");
            var idxCapabilities = FirstIndexOf(log, "GET", "/ISAPI/DisplayDev/VideoWall/capabilities");
            var idxWallList = FirstIndexOf(log, "GET", "/ISAPI/DisplayDev/VideoWall");
            var idxOutputs = FirstIndexOf(log, "GET", "/outputs");
            var idxWindows = FirstIndexOf(log, "GET", "/windows");

            Assert.True(idxUserCheck >= 0, "Bước 1: userCheck phải được gọi");
            Assert.True(idxCapabilities >= 0, "Bước 2: capabilities phải được gọi");
            Assert.True(idxWallList >= 0, "Bước 3: VideoWall list phải được gọi");
            Assert.True(idxOutputs >= 0, "Bước 4: outputs phải được gọi");
            Assert.True(idxWindows >= 0, "Bước 6: windows snapshot phải được gọi");

            Assert.True(firstWriteIdx > idxUserCheck, "Lệnh ghi đầu tiên phải xuất hiện sau userCheck");
            Assert.True(firstWriteIdx > idxCapabilities, "Lệnh ghi đầu tiên phải xuất hiện sau capabilities");
            Assert.True(firstWriteIdx > idxWallList, "Lệnh ghi đầu tiên phải xuất hiện sau VideoWall list");
            Assert.True(firstWriteIdx > idxOutputs, "Lệnh ghi đầu tiên phải xuất hiện sau outputs");
            Assert.True(firstWriteIdx > idxWindows, "Lệnh ghi đầu tiên phải xuất hiện sau windows snapshot");
        }

        /// <summary>
        /// Author: Đạt
        /// Description: SetupScene thực thi các bước đọc theo đúng trình tự tài liệu đặc tả: userCheck -> capabilities -> VideoWall -> outputs -> windows.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SetupScene_ReadStepsFollowDocumentedOrder_Test()
        {
            host.MockServer.ResetDefaults();
            var (controller, scene) = await CreateSetupSceneFixtures();

            var output = await _service.SetupScene(new VwSetupSceneInput
            {
                ControllerId = controller.ID,
                SceneId = scene.ID,
                DryRun = false,
                ResetWindows = true,
                Activate = true
            });

            Assert.NotNull(output);
            Assert.True(output.Success);

            var log = host.MockServer.ReceivedRequests;
            var idxUserCheck = FirstIndexOf(log, "GET", "/ISAPI/Security/userCheck");
            var idxCapabilities = FirstIndexOf(log, "GET", "/ISAPI/DisplayDev/VideoWall/capabilities");
            var idxWallList = FirstIndexOf(log, "GET", "/ISAPI/DisplayDev/VideoWall");
            var idxOutputs = FirstIndexOf(log, "GET", "/outputs");
            var idxWindows = FirstIndexOf(log, "GET", "/windows");

            Assert.True(idxUserCheck >= 0, "userCheck phải được gọi");
            Assert.True(idxCapabilities >= 0, "capabilities phải được gọi");
            Assert.True(idxWallList >= 0, "VideoWall list phải được gọi");
            Assert.True(idxOutputs >= 0, "outputs phải được gọi");
            Assert.True(idxWindows >= 0, "windows snapshot phải được gọi");

            Assert.True(idxUserCheck < idxCapabilities, "userCheck phải đứng trước capabilities");
            Assert.True(idxCapabilities < idxWallList, "capabilities phải đứng trước VideoWall list");
            Assert.True(idxWallList < idxOutputs, "VideoWall list phải đứng trước outputs");
            Assert.True(idxOutputs < idxWindows, "outputs phải đứng trước windows snapshot");
        }

        /// <summary>
        /// Author: Đạt
        /// Description: SetupScene gửi lệnh lưu dữ liệu kịch bản (saveData) trước khi kích hoạt kịch bản (activate) để tránh kịch bản rỗng.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SetupScene_SaveDataPrecedesActivate_Test()
        {
            host.MockServer.ResetDefaults();
            var (controller, scene) = await CreateSetupSceneFixtures();

            var output = await _service.SetupScene(new VwSetupSceneInput
            {
                ControllerId = controller.ID,
                SceneId = scene.ID,
                DryRun = false,
                Activate = true
            });

            Assert.NotNull(output);
            Assert.True(output.Success);

            var log = host.MockServer.ReceivedRequests;
            var idxSaveData = FirstIndexOf(log, "PUT", "saveData");
            var idxActivate = FirstIndexOf(log, "PUT", "activate");

            Assert.True(idxSaveData >= 0, "saveData phải được gọi");
            Assert.True(idxActivate >= 0, "activate phải được gọi");
            Assert.True(idxSaveData < idxActivate, "saveData phải đứng trước activate");
        }

        #endregion

        #region Safety Switches (§Nhóm C)

        /// <summary>
        /// Author: Đạt
        /// Description: Khi DryRun=true dù Activate=true và ResetWindows=true, hệ thống tuyệt đối không phát bất kỳ lệnh ghi nào (DryRun luôn thắng Activate).
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SetupScene_DryRunWithActivateTrue_StillEmitsNoWrite_Test()
        {
            host.MockServer.ResetDefaults();
            var (controller, scene) = await CreateSetupSceneFixtures();

            var output = await _service.SetupScene(new VwSetupSceneInput
            {
                ControllerId = controller.ID,
                SceneId = scene.ID,
                DryRun = true,
                ResetWindows = true,
                Activate = true
            });

            Assert.NotNull(output);
            Assert.True(output.Success);
            Assert.True(output.DryRun);

            Assert.Equal(0, host.MockServer.ActivateSceneCallCount);
            Assert.Equal(0, host.MockServer.SaveSceneDataCallCount);
            Assert.Equal(0, host.MockServer.AddWindowCallCount);
            Assert.Equal(0, host.MockServer.DeleteAllWindowsCallCount);

            Assert.Contains(output.Steps, s => s.Name.Contains("Lưu kịch bản") && s.Skipped);
            Assert.Contains(output.Steps, s => s.Name.Contains("Kích hoạt kịch bản") && s.Skipped);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Khi ResetWindows=false trong chế độ thực thi (DryRun=false), hệ thống không xóa các cửa sổ hiện có mà chỉ thêm cửa sổ mới.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SetupScene_ResetWindowsFalse_DoesNotDeleteWindows_Test()
        {
            host.MockServer.ResetDefaults();
            var (controller, scene) = await CreateSetupSceneFixtures();

            var output = await _service.SetupScene(new VwSetupSceneInput
            {
                ControllerId = controller.ID,
                SceneId = scene.ID,
                DryRun = false,
                ResetWindows = false,
                Activate = false
            });

            Assert.NotNull(output);
            Assert.True(output.Success);

            Assert.Equal(0, host.MockServer.DeleteAllWindowsCallCount);
            Assert.True(host.MockServer.AddWindowCallCount >= 1);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Khi ResetWindows=true trong chế độ thực thi (DryRun=false), hệ thống phát lệnh xóa toàn bộ cửa sổ cũ đúng 1 lần.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SetupScene_ResetWindowsTrue_DeletesWindowsOnce_Test()
        {
            host.MockServer.ResetDefaults();
            var (controller, scene) = await CreateSetupSceneFixtures();

            var output = await _service.SetupScene(new VwSetupSceneInput
            {
                ControllerId = controller.ID,
                SceneId = scene.ID,
                DryRun = false,
                ResetWindows = true,
                Activate = false
            });

            Assert.NotNull(output);
            Assert.True(output.Success);

            Assert.Equal(1, host.MockServer.DeleteAllWindowsCallCount);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Khi Activate=false trong chế độ thực thi (DryRun=false), hệ thống lưu dữ liệu kịch bản nhưng bỏ qua bước kích hoạt (Skipped=true).
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SetupScene_ActivateFalse_SavesButDoesNotActivate_Test()
        {
            host.MockServer.ResetDefaults();
            var (controller, scene) = await CreateSetupSceneFixtures();

            var output = await _service.SetupScene(new VwSetupSceneInput
            {
                ControllerId = controller.ID,
                SceneId = scene.ID,
                DryRun = false,
                ResetWindows = false,
                Activate = false
            });

            Assert.NotNull(output);
            Assert.True(output.Success);

            Assert.Equal(1, host.MockServer.SaveSceneDataCallCount);
            Assert.Equal(0, host.MockServer.ActivateSceneCallCount);
            Assert.Contains(output.Steps, s => s.Name.Contains("Kích hoạt kịch bản") && s.Skipped);
        }

        #endregion

        #endregion

        #region Serial Transparent Transmission

        /// <summary>
        /// Author: Đạt
        /// Description: Gửi lệnh serial trong suốt tuân thủ nghiêm ngặt trình tự open -> transData -> close.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SendScreenSerialCommand_FollowsOpenSendCloseSequence_Test()
        {
            host.MockServer.ResetDefaults();
            var controller = TestController;
            byte[] testPayload = [0xAA, 0x11, 0x00, 0xBB];

            await _service.SendScreenSerialCommand(controller, 1, 1, testPayload);

            Assert.Equal(1, host.MockServer.SerialOpenCallCount);
            Assert.Equal(1, host.MockServer.SerialSendCallCount);
            Assert.Equal(1, host.MockServer.SerialCloseCallCount);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kênh truyền serial luôn được đóng trong khối finally khi bước gửi transData bị lỗi.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SendScreenSerialCommand_ClosesChannelWhenSendFails_Test()
        {
            host.MockServer.ResetDefaults();
            host.MockServer.SimulateSerialSendFailure = true;
            var controller = TestController;
            byte[] testPayload = [0xAA, 0x11, 0x00, 0xBB];

            await Assert.ThrowsAnyAsync<Exception>(() =>
                _service.SendScreenSerialCommand(controller, 1, 1, testPayload));

            Assert.Equal(1, host.MockServer.SerialOpenCallCount);
            Assert.Equal(1, host.MockServer.SerialSendCallCount);
            Assert.Equal(1, host.MockServer.SerialCloseCallCount);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Thiết bị không hỗ trợ serial capabilities thì ném lỗi và không phát bất kỳ lệnh open/send/close nào.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SendScreenSerialCommand_UnsupportedCapability_ThrowsWithoutSending_Test()
        {
            host.MockServer.ResetDefaults();
            host.MockServer.IsSupportSerialTransparent = false;
            var controller = TestController;
            byte[] testPayload = [0xAA, 0x11, 0x00, 0xBB];

            await Assert.ThrowsAnyAsync<Exception>(() =>
                _service.SendScreenSerialCommand(controller, 1, 1, testPayload));

            Assert.Equal(0, host.MockServer.SerialOpenCallCount);
            Assert.Equal(0, host.MockServer.SerialSendCallCount);
            Assert.Equal(0, host.MockServer.SerialCloseCallCount);
        }

        #endregion

        #region SyncSceneWindowsToDevice Missing Device Window ID (§Điểm vênh 03)

        /// <summary>
        /// Author: Đạt
        /// Description: POST /windows không trả về thẻ ID trong XML phản hồi — service phải tra ngược danh sách cửa sổ trên thiết bị theo toạ độ Rect để lấy ID 33554433.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SyncSceneWindowsToDevice_AddWindowWithoutId_ResolvesIdByRect_Test()
        {
            host.MockServer.ResetDefaults();
            host.MockServer.SimulateAddWindowWithoutId = true;

            var ctrl = new VwController
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}CTRL_NOID_{Guid.NewGuid():N}",
                Name = "Controller Missing Window ID",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(ctrl).ExecuteCommandAsync();

            var screen = new VwScreen
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}SCR_NOID_{Guid.NewGuid():N}",
                Name = "Screen Panel 0-0",
                ControllerId = ctrl.ID,
                GridCol = 0,
                GridRow = 0,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(screen).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}SCN_NOID_{Guid.NewGuid():N}",
                Name = "Scene Missing Window ID",
                ControllerId = ctrl.ID,
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var win = new VwWindowScene
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}WIN_NOID_{Guid.NewGuid():N}",
                Name = "Window No ID Rect Fallback",
                SceneId = scene.ID,
                X = 0,
                Y = 0,
                W = 3840,
                H = 2160,
                Visible = BaseEnums.SceneWindowVisible.Visible,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(win).ExecuteCommandAsync();

            // Act
            await _service.SyncSceneWindowsToDevice(scene.ID);

            // Assert
            Assert.Equal(1, host.MockServer.AddWindowCallCount);
            var dbWin = await _db.Queryable<VwWindowScene>().FirstAsync(u => u.ID == win.ID);
            Assert.NotNull(dbWin);
            Assert.Equal("33554433", dbWin.DeviceWindowId);
            Assert.Equal(1, host.MockServer.SaveSceneDataCallCount);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: POST /windows trả về thẻ ID 33554435 — service gán trực tiếp ID từ response mà không cần tra cứu danh sách cửa sổ.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SyncSceneWindowsToDevice_AddWindowReturnsId_SkipsRectLookup_Test()
        {
            host.MockServer.ResetDefaults();

            var ctrl = new VwController
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}CTRL_WITHID_{Guid.NewGuid():N}",
                Name = "Controller With Window ID",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(ctrl).ExecuteCommandAsync();

            var screen = new VwScreen
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}SCR_WITHID_{Guid.NewGuid():N}",
                Name = "Screen Panel 0-0",
                ControllerId = ctrl.ID,
                GridCol = 0,
                GridRow = 0,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(screen).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}SCN_WITHID_{Guid.NewGuid():N}",
                Name = "Scene With Window ID",
                ControllerId = ctrl.ID,
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var win = new VwWindowScene
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}WIN_WITHID_{Guid.NewGuid():N}",
                Name = "Window With Direct ID",
                SceneId = scene.ID,
                X = 0,
                Y = 0,
                W = 3840,
                H = 2160,
                Visible = BaseEnums.SceneWindowVisible.Visible,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(win).ExecuteCommandAsync();

            // Act
            await _service.SyncSceneWindowsToDevice(scene.ID);

            // Assert
            Assert.Equal(1, host.MockServer.AddWindowCallCount);
            var dbWin = await _db.Queryable<VwWindowScene>().FirstAsync(u => u.ID == win.ID);
            Assert.NotNull(dbWin);
            Assert.Equal("33554435", dbWin.DeviceWindowId);
            Assert.Equal(1, host.MockServer.SaveSceneDataCallCount);
        }

        #endregion

        #region SyncSources (§Điểm vênh 04)

        /// <summary>
        /// Author: Đạt
        /// Description: Đồng bộ nguồn tín hiệu ở chế độ xem trước (Apply = false) trả về danh sách chênh lệch nhưng không ghi đè vào DB.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SyncSources_ApplyFalse_ReturnsPreviewWithoutWritingDb_Test()
        {
            host.MockServer.ResetDefaults();

            var ctrl = new VwController
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}CTRL_SYNC_PREV_{Guid.NewGuid():N}",
                Name = "Controller Sync Preview",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(ctrl).ExecuteCommandAsync();

            var source1 = new VwSource
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}SRC_1_{Guid.NewGuid():N}",
                Name = "Source 1",
                ControllerId = ctrl.ID,
                SignalNo = 1,
                OrderNo = 1,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var source2 = new VwSource
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}SRC_2_{Guid.NewGuid():N}",
                Name = "Source 2",
                ControllerId = ctrl.ID,
                SignalNo = 2,
                OrderNo = 2,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(new[] { source1, source2 }).ExecuteCommandAsync();

            // Act
            var output = await _service.SyncSources(new VwSyncSourcesInput { ID = ctrl.ID, Apply = false });

            // Assert
            Assert.NotNull(output);
            Assert.False(output.Applied);
            Assert.Equal(0, output.UpdatedCount);
            Assert.Equal(2, output.Changes.Count);

            Assert.Contains(output.Changes, c => c.EntityId == source1.ID && c.DbValue == "1" && c.DeviceValue == "16842753");
            Assert.Contains(output.Changes, c => c.EntityId == source2.ID && c.DbValue == "2" && c.DeviceValue == "16842754");

            var dbSource1 = await _db.Queryable<VwSource>().FirstAsync(u => u.ID == source1.ID);
            var dbSource2 = await _db.Queryable<VwSource>().FirstAsync(u => u.ID == source2.ID);
            Assert.Equal(1, dbSource1.SignalNo);
            Assert.Equal(2, dbSource2.SignalNo);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Đồng bộ nguồn tín hiệu với Apply = true ghi đè đúng ID đóng gói theo byte của kênh thiết bị (16842753, 16842754) vào DB.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SyncSources_ApplyTrue_WritesPackedChannelIdToDb_Test()
        {
            host.MockServer.ResetDefaults();

            var ctrl = new VwController
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}CTRL_SYNC_APPLY_{Guid.NewGuid():N}",
                Name = "Controller Sync Apply",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(ctrl).ExecuteCommandAsync();

            var source1 = new VwSource
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}SRC_1_{Guid.NewGuid():N}",
                Name = "Source 1",
                ControllerId = ctrl.ID,
                SignalNo = 1,
                OrderNo = 1,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var source2 = new VwSource
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}SRC_2_{Guid.NewGuid():N}",
                Name = "Source 2",
                ControllerId = ctrl.ID,
                SignalNo = 2,
                OrderNo = 2,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(new[] { source1, source2 }).ExecuteCommandAsync();

            // Act
            var output = await _service.SyncSources(new VwSyncSourcesInput { ID = ctrl.ID, Apply = true });

            // Assert
            Assert.NotNull(output);
            Assert.True(output.Applied);
            Assert.Equal(2, output.UpdatedCount);

            var dbSource1 = await _db.Queryable<VwSource>().FirstAsync(u => u.ID == source1.ID);
            var dbSource2 = await _db.Queryable<VwSource>().FirstAsync(u => u.ID == source2.ID);
            Assert.Equal(16842753, dbSource1.SignalNo);
            Assert.Equal(16842754, dbSource2.SignalNo);
            Assert.NotEqual(1, dbSource1.SignalNo);
            Assert.NotEqual(2, dbSource2.SignalNo);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Khi chạy đồng bộ lần thứ hai trên nguồn đã chuẩn khớp với thiết bị, UpdatedCount = 0 và không có thay đổi phát sinh.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SyncSources_RunTwice_SecondRunReportsNoChange_Test()
        {
            host.MockServer.ResetDefaults();

            var ctrl = new VwController
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}CTRL_SYNC_TWICE_{Guid.NewGuid():N}",
                Name = "Controller Sync Twice",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(ctrl).ExecuteCommandAsync();

            var source1 = new VwSource
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}SRC_1_{Guid.NewGuid():N}",
                Name = "Source 1",
                ControllerId = ctrl.ID,
                SignalNo = 1,
                OrderNo = 1,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var source2 = new VwSource
            {
                ID = Guid.NewGuid().ToString(),
                Code = $"{TestPrefix}SRC_2_{Guid.NewGuid():N}",
                Name = "Source 2",
                ControllerId = ctrl.ID,
                SignalNo = 2,
                OrderNo = 2,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(new[] { source1, source2 }).ExecuteCommandAsync();

            // Act
            var firstRun = await _service.SyncSources(new VwSyncSourcesInput { ID = ctrl.ID, Apply = true });
            var secondRun = await _service.SyncSources(new VwSyncSourcesInput { ID = ctrl.ID, Apply = true });

            // Assert
            Assert.Equal(2, firstRun.UpdatedCount);
            Assert.Equal(0, secondRun.UpdatedCount);
            Assert.Empty(secondRun.Changes);
        }

        #endregion

        #region Chọn tường ISAPI (ResolveWallNo)

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm thử ResolveWallNo ưu tiên số hiệu tường truyền vào (requestedWallNo) cao nhất và
        ///              không gọi thiết bị — kể cả khi không cấu hình WallNo hay đã cấu hình một giá trị khác.
        /// Created date: 24/08/2026
        /// </summary>
        [Theory]
        [InlineData(null)]  // không cấu hình
        [InlineData(5)]     // có cấu hình khác -> requested vẫn thắng
        public async Task VwISAPIDeviceService_ResolveWallNo_RequestedWallNo_AlwaysWins_Test(int? configuredWallNo)
        {
            // Arrange
            _mock.ResetDefaults();
            var controller = CreateResolverTestController();
            using var scope = host.Services.CreateScope();
            var resolver = CreateResolverWithWallNo(scope, configuredWallNo);
            resolver.ForgetWall(controller.ID);

            // Act
            var wallNo = await resolver.ResolveWall(controller, requestedWallNo: 7);

            // Assert
            Assert.Equal(7, wallNo);
            Assert.Equal(0, _mock.GetVideoWallsCallCount);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm thử ResolveWallNo sử dụng cấu hình WallNo khi không có requestedWallNo và không gọi thiết bị.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_ResolveWallNo_ConfiguredWallNo_UsedWhenNoRequested_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            var controller = CreateResolverTestController();
            using var scope = host.Services.CreateScope();
            var resolver = CreateResolverWithWallNo(scope, configuredWallNo: 5);
            resolver.ForgetWall(controller.ID);

            // Act
            var wallNo = await resolver.ResolveWall(controller, requestedWallNo: null);

            // Assert
            Assert.Equal(5, wallNo);
            Assert.Equal(0, _mock.GetVideoWallsCallCount);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm thử auto-detect tự động chọn tường bound (tường 2) thay vì mặc định tường 1 sandbox.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_ResolveWallNo_AutoDetect_SelectsBoundWallNotWallOne_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.SimulateWall1Unbound = true;
            var controller = CreateResolverTestController();
            using var scope = host.Services.CreateScope();
            var resolver = CreateResolverWithWallNo(scope, configuredWallNo: null);
            resolver.ForgetWall(controller.ID);

            // Act
            var wallNo = await resolver.ResolveWall(controller, requestedWallNo: null);

            // Assert
            Assert.Equal(2, wallNo);
            Assert.Equal(1, _mock.GetVideoWallsCallCount);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm thử lần gọi thứ hai lấy kết quả từ cache 30 phút mà không phát HTTP tới thiết bị.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_ResolveWallNo_SecondCall_UsesCacheWithoutCallingDevice_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.SimulateWall1Unbound = true;
            var controller = CreateResolverTestController();
            using var scope = host.Services.CreateScope();
            var resolver = CreateResolverWithWallNo(scope, configuredWallNo: null);
            resolver.ForgetWall(controller.ID);

            // Act
            var wallNo1 = await resolver.ResolveWall(controller, requestedWallNo: null);
            var wallNo2 = await resolver.ResolveWall(controller, requestedWallNo: null);

            // Assert
            Assert.Equal(2, wallNo1);
            Assert.Equal(2, wallNo2);
            Assert.Equal(1, _mock.GetVideoWallsCallCount);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm thử ForgetWall xoá cache thành công, buộc lần gọi kế tiếp phải khảo sát thiết bị lại và cache lại sau đó.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_ForgetWall_ForcesReDetectionOnNextResolve_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.SimulateWall1Unbound = true;
            var controller = CreateResolverTestController();
            using var scope = host.Services.CreateScope();
            var resolver = CreateResolverWithWallNo(scope, configuredWallNo: null);
            resolver.ForgetWall(controller.ID);

            // Act & Assert 1: Dò lần đầu -> gọi thiết bị 1 lần
            var wallNo1 = await resolver.ResolveWall(controller, requestedWallNo: null);
            Assert.Equal(2, wallNo1);
            Assert.Equal(1, _mock.GetVideoWallsCallCount);

            // Act & Assert 2: Xoá cache và dò lại -> gọi thiết bị lần 2
            resolver.ForgetWall(controller.ID);
            var wallNo2 = await resolver.ResolveWall(controller, requestedWallNo: null);
            Assert.Equal(2, wallNo2);
            Assert.Equal(2, _mock.GetVideoWallsCallCount);

            // Act & Assert 3: Dò lần 3 -> đã cache lại, không gọi thiết bị nữa
            var wallNo3 = await resolver.ResolveWall(controller, requestedWallNo: null);
            Assert.Equal(2, wallNo3);
            Assert.Equal(2, _mock.GetVideoWallsCallCount);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm thử ném ngoại lệ khi thiết bị không có tường nào ở trạng thái bound.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_ResolveWallNo_NoBoundWall_Throws_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.SimulateNoBoundWall = true;
            var controller = CreateResolverTestController();
            using var scope = host.Services.CreateScope();
            var resolver = CreateResolverWithWallNo(scope, configuredWallNo: null);
            resolver.ForgetWall(controller.ID);

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(() => resolver.ResolveWall(controller, requestedWallNo: null));
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm thử khi thiết bị có nhiều tường bound, ResolveWallNo chọn tường bound đầu tiên và ghi log cảnh báo Warning.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_ResolveWallNo_MultipleBoundWalls_LogsWarning_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.SimulateMultipleBoundWalls = true;
            var controller = CreateResolverTestController();
            using var scope = host.Services.CreateScope();
            var capturingLogger = new CapturingLoggerTest<VwISAPIDeviceService>();
            var resolver = CreateResolverWithWallNo(scope, configuredWallNo: null, customLogger: capturingLogger);
            resolver.ForgetWall(controller.ID);

            // Act
            var wallNo = await resolver.ResolveWall(controller, requestedWallNo: null);

            // Assert
            Assert.Equal(1, wallNo);
            var warningLogs = capturingLogger.Entries.Where(e => e.Level == LogLevel.Warning).ToList();
            Assert.Single(warningLogs);
            Assert.Contains("2", warningLogs[0].Message);
        }

        #endregion

        #region ISAPI Client, Registration & Digest Auth (Liên đới Client)

        /// <summary>
        /// Author: Đạt
        /// Description: Controller thiếu IP phải throw InvalidOperationException.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public void VwISAPIDeviceService_EnsureRegistered_MissingIp_ThrowsInvalidOperationException_Test()
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
        public void VwISAPIDeviceService_EnsureRegistered_BuildsHttpBaseUriOnPort80_Test()
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
        /// Description: Resolver giải quyết đúng cặp tài khoản/mật khẩu và Client gửi request xác thực thành công qua HTTP Digest.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_EnsureRegistered_RegistersDigestCredentialForController_Test()
        {
            var controller = new VwController
            {
                ID = "ctrl-1",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword
            };

            var resolver = host.Services.GetRequiredService<VwISAPICredentialResolver>();
            var (account, password) = resolver.Resolve(controller);

            Assert.Equal(VwISAPIMockServerHikvision.DefaultUser, account);
            Assert.Equal(VwISAPIMockServerHikvision.DefaultPassword, password);

            var result = await _client.UserCheckAsync(controller);
            Assert.True(result.Success);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Khi cập nhật mật khẩu mới trên đối tượng controller, Resolver phản ánh mật khẩu mới ngay lập tức.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public void VwISAPIDeviceService_EnsureRegistered_CalledAgainWithNewPassword_RefreshesCredential_Test()
        {
            var controller = new VwController { ID = "ctrl-1", IP = "10.10.9.236", Account = "admin", PassWord = "old-pass" };
            var resolver = host.Services.GetRequiredService<VwISAPICredentialResolver>();

            var (_, oldPassword) = resolver.Resolve(controller);
            Assert.Equal("old-pass", oldPassword);

            controller.PassWord = "new-pass";
            var (_, newPassword) = resolver.Resolve(controller);

            Assert.Equal("new-pass", newPassword);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Controller không kèm port trên IP sẽ sử dụng port mặc định và scheme từ VwDeviceProfile.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public void VwISAPIDeviceService_EnsureRegistered_UsesProfileDefaultPort_Test()
        {
            var controller = new VwController { ID = "ctrl-profile-port", IP = "10.10.9.236", Account = "admin", PassWord = "123" };

            var baseUri = _client.EnsureRegistered(controller);

            Assert.Equal(VwDeviceProfile.Scheme, baseUri.Scheme);
            Assert.Equal(VwDeviceProfile.DefaultPort, baseUri.Port);
            Assert.Equal("10.10.9.236", baseUri.Host);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Controller có cấu hình IP kèm Port tùy chỉnh (ví dụ 127.0.0.1:18080) phải giữ đúng port đó.
        /// Created date: 17/08/2026
        /// </summary>
        [Fact]
        public void VwISAPIDeviceService_EnsureRegistered_BuildsHttpBaseUriWithCustomPort_Test()
        {
            var controller = new VwController { ID = "ctrl-custom-port", IP = "127.0.0.1:18080", Account = "admin", PassWord = "123" };

            var baseUri = _client.EnsureRegistered(controller);

            Assert.Equal("http", baseUri.Scheme);
            Assert.Equal(18080, baseUri.Port);
            Assert.Equal("127.0.0.1", baseUri.Host);
            Assert.Equal("/", baseUri.AbsolutePath);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: XmlSerializer tuần tự hoá payload không chứa <?xml declaration và không chứa BOM (tránh lỗi badXmlFormat).
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public void VwISAPIDeviceService_XmlSerializer_OmitsXmlDeclaration_Test()
        {
            var request = new VwISAPIWindowRequest
            {
                Rect = new VwISAPIRect { Coordinate = new VwISAPICoordinate { X = 0, Y = 0 }, Width = 1920, Height = 1080 }
            };

            var xml = _client.SerializeToXml(request);

            Assert.DoesNotContain("<?xml", xml);
            Assert.False(xml.Contains('\uFEFF'));
            Assert.StartsWith("<WallWindow", xml);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm chứng hằng số profile khớp với giá trị mặc định của Core DTO (bảo vệ việc Core không tham chiếu Infrastructure).
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public void VwISAPIDeviceService_ProfileSignalMode_MatchesCoreDtoDefault_Test()
        {
            var subWindowParam = new VwISAPISubWindowParam();
            var windowRequest = new VwISAPIWindowRequest();

            Assert.Equal(VwDeviceProfile.SignalMode, subWindowParam.SignalMode);
            Assert.Equal(VwDeviceProfile.WndOperateMode, windowRequest.WndOperateMode);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Gửi dữ liệu serial SendSerialDataAsync gửi byte thô qua application/octet-stream, không bị bọc XML.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SendSerialData_SendsRawBytesNotXml_Test()
        {
            host.MockServer.ResetDefaults();
            byte[] rawPayload = [0xAA, 0x55, 0x01, 0xFF];

            var result = await _client.SendSerialDataAsync(TestController, 1, 1, rawPayload);

            Assert.True(result.Success);
            Assert.NotNull(host.MockServer.LastReceivedContentType);
            Assert.Contains("application/octet-stream", host.MockServer.LastReceivedContentType);
            Assert.Equal(rawPayload, host.MockServer.LastReceivedSerialData);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Đọc danh sách kênh cổng ra video GetOutputChannelsAsync parse đúng outputPortAccessStatus và IsConnected.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_GetOutputChannels_ParsesAccessStatus_Test()
        {
            host.MockServer.ResetDefaults();
            host.MockServer.NotConnectedOutputChannels.Add(17235972);

            var result = await _client.GetOutputChannelsAsync(TestController);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(12, result.Data.VideoOutputChannel.Count);

            var ch1 = result.Data.VideoOutputChannel[0];
            Assert.Equal(17235971, ch1.Id);
            Assert.Equal("HDMI", ch1.PortType);
            Assert.Equal("Output 1 (H1-C1)", ch1.Name);
            Assert.Equal("normal", ch1.OutputPortAccessStatus);
            Assert.True(ch1.IsConnected);
            Assert.NotNull(ch1.PortInBoard);
            Assert.Equal(7, ch1.PortInBoard.BoardId);
            Assert.Equal(1, ch1.PortInBoard.PortId);

            var ch2 = result.Data.VideoOutputChannel[1];
            Assert.Equal(17235972, ch2.Id);
            Assert.Equal("Output 2 (H1-C2)", ch2.Name);
            Assert.Equal("notConnected", ch2.OutputPortAccessStatus);
            Assert.False(ch2.IsConnected);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Challenge 401 không chứa tham số algorithm — client phải mặc định dùng MD5 (RFC 7616) và tính hash hợp lệ.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_Digest_ChallengeWithoutAlgorithm_AuthenticatesWithMd5_Test()
        {
            host.MockServer.ResetDefaults();
            host.MockServer.SimulateChallengeWithoutAlgorithm = true;
            host.MockServer.VerifyDigestResponseHash = true;

            var result = await _client.UserCheckAsync(TestController);

            Assert.True(result.Success);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Header WWW-Authenticate chứa cả 2 challenge SHA-256 và MD5 trong cùng 1 chuỗi — client phải chọn đúng nonce của MD5 challenge.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_Digest_DualChallengeInOneHeader_PicksMd5Nonce_Test()
        {
            host.MockServer.ResetDefaults();
            host.MockServer.SimulateDualChallengeHeader = true;

            var result = await _client.UserCheckAsync(TestController);

            Assert.True(result.Success);
            Assert.NotNull(host.MockServer.LastReceivedAuthNonce);
            Assert.NotNull(host.MockServer.LastIssuedMd5Nonce);
            Assert.Equal(host.MockServer.LastIssuedMd5Nonce, host.MockServer.LastReceivedAuthNonce);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra luồng Digest Auth thông thường khi MockServer bật tính năng kiểm tra hash MD5 chuẩn xác.
        /// Created date: 24/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_Digest_VerifiedHash_AuthenticatesSuccessfully_Test()
        {
            host.MockServer.ResetDefaults();
            host.MockServer.VerifyDigestResponseHash = true;

            var result = await _client.UserCheckAsync(TestController);

            Assert.True(result.Success);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Nhiều controller với thông tin xác thực khác nhau hoạt động độc lập qua cùng 1 client.
        /// Created date: 16/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_MultipleControllers_HandleIndependentCredentials_Test()
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
        /// Description: SendRawAsync GET tới path capabilities trả về Success và RawResponse chứa XML từ MockServer.
        /// Created date: 25/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SendRawAsync_Get_ReturnsRawResponse_Test()
        {
            host.MockServer.ResetDefaults();

            var result = await _client.SendRawAsync(
                TestController,
                HttpMethod.Get,
                "ISAPI/DisplayDev/VideoWall/capabilities",
                null,
                null);

            Assert.True(result.Success);
            Assert.NotNull(result.RawResponse);
            Assert.Contains("VideoWallCap", result.RawResponse);
            Assert.Null(result.RawRequest);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: SendRawAsync POST gửi body XML và trả về RawRequest = body đã gửi, RawResponse = phản hồi thiết bị.
        /// Created date: 25/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SendRawAsync_Post_SendsBodyAndReturnsResponse_Test()
        {
            host.MockServer.ResetDefaults();
            var xmlBody = "<WallWindow><id>1</id></WallWindow>";

            var result = await _client.SendRawAsync(
                TestController,
                HttpMethod.Post,
                "ISAPI/DisplayDev/VideoWall/2/windows",
                xmlBody,
                "application/xml");

            Assert.NotNull(result.RawRequest);
            Assert.Equal(xmlBody, result.RawRequest);
            Assert.NotNull(result.RawResponse);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: SendRawAsync khi circuit breaker đang chặn IP phải trả Fail với HttpStatus 429.
        /// Created date: 25/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SendRawAsync_CircuitBreakerBlocked_ReturnsFail_Test()
        {
            host.MockServer.ResetDefaults();
            var controller = TestController;

            for (var i = 0; i < 10; i++)
                _client.RecordCircuitBreakerFailure(controller.IP, 401);

            var result = await _client.SendRawAsync(
                controller,
                HttpMethod.Get,
                "ISAPI/Security/userCheck",
                null,
                null);

            Assert.False(result.Success);
            Assert.Equal(429, result.HttpStatusCode);

            _client.ResetCircuitBreaker(controller.IP);
        }

        #endregion

        #region SendPassthrough

        /// <summary>
        /// Author: Đạt
        /// Description: SendPassthrough với ControllerId hợp lệ (đã lưu trong DB) trả về step Success khi GET capabilities.
        /// Created date: 25/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SendPassthrough_WithControllerId_Success_Test()
        {
            _mock.ResetDefaults();
            var controller = TestController;
            await _db.Insertable(controller).ExecuteCommandAsync();

            var input = new VwISAPIPassthroughInput
            {
                ControllerId = controller.ID,
                Method = "GET",
                Path = "ISAPI/DisplayDev/VideoWall/capabilities"
            };

            var step = await _service.SendPassthrough(input);

            Assert.NotNull(step);
            Assert.True(step.Success);
            Assert.Equal("Passthrough", step.Name);
            Assert.Equal("GET", step.Method);
            Assert.Equal("ISAPI/DisplayDev/VideoWall/capabilities", step.Endpoint);
            Assert.NotNull(step.ResponseXml);
            Assert.Contains("VideoWallCap", step.ResponseXml);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: SendPassthrough với Device object adhoc (không cần DB) trỏ thẳng tới MockServer trả về Success.
        /// Created date: 25/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SendPassthrough_WithDevice_Success_Test()
        {
            _mock.ResetDefaults();

            var input = new VwISAPIPassthroughInput
            {
                Device = new VwISAPIPassthroughDevice
                {
                    Ip = "127.0.0.1",
                    Port = VwISAPIMockServerHikvision.DefaultPort,
                    Account = VwISAPIMockServerHikvision.DefaultUser,
                    Password = VwISAPIMockServerHikvision.DefaultPassword
                },
                Method = "GET",
                Path = "ISAPI/Security/userCheck"
            };

            var step = await _service.SendPassthrough(input);

            Assert.NotNull(step);
            Assert.True(step.Success);
            Assert.Equal("Passthrough", step.Name);
            Assert.NotNull(step.ResponseXml);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: SendPassthrough khi có cả ControllerId lẫn Device phải trả step thất bại (Success=false) với message rõ ràng.
        /// Created date: 25/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SendPassthrough_BothControllerIdAndDevice_ReturnsFail_Test()
        {
            var input = new VwISAPIPassthroughInput
            {
                ControllerId = "some-controller",
                Device = new VwISAPIPassthroughDevice
                {
                    Ip = "127.0.0.1",
                    Port = VwISAPIMockServerHikvision.DefaultPort,
                    Account = "admin",
                    Password = "pass"
                },
                Method = "GET",
                Path = "ISAPI/Security/userCheck"
            };

            var step = await _service.SendPassthrough(input);

            Assert.NotNull(step);
            Assert.False(step.Success);
            Assert.Contains("không được cả hai", step.Message);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: SendPassthrough khi thiếu cả ControllerId lẫn Device phải trả step thất bại (Success=false) với message rõ ràng.
        /// Created date: 25/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SendPassthrough_NeitherControllerIdNorDevice_ReturnsFail_Test()
        {
            var input = new VwISAPIPassthroughInput
            {
                Method = "GET",
                Path = "ISAPI/Security/userCheck"
            };

            var step = await _service.SendPassthrough(input);

            Assert.NotNull(step);
            Assert.False(step.Success);
            Assert.Contains("Phải cung cấp", step.Message);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: SendPassthrough với method không hợp lệ (PATCH) phải trả step thất bại (Success=false).
        /// Created date: 25/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SendPassthrough_InvalidMethod_ReturnsFail_Test()
        {
            var input = new VwISAPIPassthroughInput
            {
                Device = new VwISAPIPassthroughDevice
                {
                    Ip = "127.0.0.1",
                    Port = VwISAPIMockServerHikvision.DefaultPort,
                    Account = "admin",
                    Password = "pass"
                },
                Method = "PATCH",
                Path = "ISAPI/Security/userCheck"
            };

            var step = await _service.SendPassthrough(input);

            Assert.NotNull(step);
            Assert.False(step.Success);
            Assert.Contains("Method không hỗ trợ", step.Message);
        }

        #endregion

        #region MockServer Coverage & Edge-Case Tests

        /// <summary>
        /// Author: Đạt
        /// Description: Lấy thông tin năng lực Video Wall thành công từ Mock Server, xác thực IsSupportScene và BaseOutputSize.
        /// Created date: 26/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_GetCapabilities_ReturnsCapabilitiesAndIncrementsMockCount_Test()
        {
            host.MockServer.ResetDefaults();
            var controller = TestController;

            var result = await _client.GetCapabilitiesAsync(controller);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.IsSupportScene);
            Assert.Equal(1920, result.Data.BaseOutputSize);
            Assert.True(host.MockServer.GetCapabilitiesCallCount >= 1);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Lấy danh sách cổng ra màn hình của tường logic từ Mock Server và tăng biến đếm GetOutputsCallCount.
        /// Created date: 26/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_GetOutputs_ReturnsWallOutputsAndIncrementsMockCount_Test()
        {
            host.MockServer.ResetDefaults();
            var controller = TestController;

            var result = await _client.GetOutputsAsync(controller, wallNo: 2);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.NotNull(result.Data.WallOutput);
            Assert.NotEmpty(result.Data.WallOutput);
            Assert.Equal(17235971, result.Data.WallOutput[0].OutputId);
            Assert.True(host.MockServer.GetOutputsCallCount >= 1);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Lấy danh sách cửa sổ hiện có trên tường logic từ Mock Server để chụp trạng thái trước khi cấu hình.
        /// Created date: 26/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_GetWindows_ReturnsWindowListAndIncrementsMockCount_Test()
        {
            host.MockServer.ResetDefaults();
            var controller = TestController;

            var result = await _client.GetWindowsAsync(controller, wallNo: 2);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.NotNull(result.Data.WallWindow);
            Assert.NotEmpty(result.Data.WallWindow);
            Assert.Equal(33554433, result.Data.WallWindow[0].Id);
            Assert.True(host.MockServer.GetWindowsCallCount >= 1);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Lấy danh sách kênh đầu vào qua client trực tiếp và xác thực tăng bộ đếm GetInputChannelsCallCount.
        /// Created date: 26/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_GetInputChannels_IncrementsMockCallCount_Test()
        {
            host.MockServer.ResetDefaults();
            var controller = TestController;

            var result = await _client.GetInputChannelsAsync(controller);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.NotNull(result.Data.VideoInputChannel);
            Assert.NotEmpty(result.Data.VideoInputChannel);
            Assert.True(host.MockServer.GetInputChannelsCallCount >= 1);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Lấy thông tin năng lực cổng serial truyền trong suốt từ Mock Server và tăng GetSerialCapabilitiesCallCount.
        /// Created date: 26/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_GetSerialCapabilities_ReturnsCapabilitiesAndIncrementsMockCount_Test()
        {
            host.MockServer.ResetDefaults();
            var controller = TestController;

            var result = await _client.GetSerialCapabilitiesAsync(controller);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.IsSupportSerialTransparent);
            Assert.True(host.MockServer.GetSerialCapabilitiesCallCount >= 1);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Nhận dữ liệu nhị phân từ kênh truyền trong suốt cổng serial của Mock Server thành công.
        /// Created date: 26/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_ReceiveSerialData_ReturnsRawBytesAndIncrementsMockCount_Test()
        {
            host.MockServer.ResetDefaults();
            var controller = TestController;
            byte[] expectedData = [0x55, 0xAA, 0x01, 0x02, 0x03];
            host.MockServer.SerialDataToReturn = expectedData;

            var result = await _client.ReceiveSerialDataAsync(controller, portId: 1, channelId: 1);

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(expectedData, result.Data);
            Assert.True(host.MockServer.SerialReceiveCallCount >= 1);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Thiết bị không phản hồi / rớt mạng (SimulateUnreachable) — Ping và Probe bắt lỗi gọn gàng, không crash unhandled exception.
        /// Created date: 26/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SimulateUnreachable_HandlesNetworkFailureGracefully_Test()
        {
            host.MockServer.ResetDefaults();
            var controller = TestController;
            await _db.Insertable(controller).ExecuteCommandAsync();

            host.MockServer.SimulateUnreachable = true;

            try
            {
                // Ping flow
                var pingStep = await _service.Ping(controller.ID);
                Assert.NotNull(pingStep);
                Assert.False(pingStep.Success);

                // Probe flow
                var probeInput = new VwProbeDeviceInput { ID = controller.ID };
                var probeOutput = await _service.Probe(probeInput);
                Assert.NotNull(probeOutput);
                Assert.False(probeOutput.Reachable);
                Assert.NotEmpty(probeOutput.Steps);
                Assert.False(probeOutput.Steps[0].Success);
            }
            finally
            {
                host.MockServer.ResetDefaults();
                _client.ResetCircuitBreaker(controller.IP);
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Thiết bị trả về nội dung XML hỏng thật sự (SimulateMalformedXmlResponse) — client bắt lỗi deserialize, trả về Fail an toàn.
        /// Created date: 26/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_SimulateMalformedXml_ReturnsFailWithoutThrowing_Test()
        {
            host.MockServer.ResetDefaults();
            var controller = TestController;
            host.MockServer.SimulateMalformedXmlResponse = true;

            try
            {
                var result = await _client.GetCapabilitiesAsync(controller);

                Assert.NotNull(result);
                Assert.False(result.Success);
                Assert.NotNull(result.ErrorMessage);
            }
            finally
            {
                host.MockServer.ResetDefaults();
            }
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Xác thực thiết bị sai mật khẩu gây lỗi 401 thật liên tiếp — kích hoạt Circuit Breaker tự động và chặn cuộc gọi tiếp theo.
        /// Created date: 26/08/2026
        /// </summary>
        [Fact]
        public async Task VwISAPIDeviceService_CircuitBreaker_TriggersOnConsecutiveReal401s_BlocksNextCall_Test()
        {
            host.MockServer.ResetDefaults();
            host.MockServer.VerifyDigestResponseHash = true;

            var controller = new VwController
            {
                ID = $"ctrl-cb-{Guid.NewGuid():N}",
                Name = "TEST_Controller_CB",
                Code = $"{TestPrefix}CTRL_CB_{Guid.NewGuid():N}",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = "WrongPassword123!",
                Status = BaseEnums.StatusEnum.Enable
            };

            try
            {
                _client.ResetCircuitBreaker(controller.IP);

                // Cuộc gọi 1: nhận 401 Unauthorized thật
                var res1 = await _client.UserCheckAsync(controller);
                Assert.False(res1.Success);
                Assert.Equal(401, res1.HttpStatusCode);

                // Cuộc gọi 2: nhận 401 Unauthorized thật
                var res2 = await _client.UserCheckAsync(controller);
                Assert.False(res2.Success);
                Assert.Equal(401, res2.HttpStatusCode);

                // Sau 2 lần 401 liên tiếp, Circuit Breaker phải chuyển sang trạng thái Blocked
                var isBlocked = _client.IsCircuitBreakerBlocked(controller.IP, out var remaining);
                Assert.True(isBlocked);
                Assert.True(remaining > TimeSpan.Zero);

                var callsBefore = host.MockServer.UserCheckCallCount;

                // Cuộc gọi 3: bị Circuit Breaker chặn tại client (trả HTTP 429), không gửi request ra MockServer
                var res3 = await _client.UserCheckAsync(controller);
                Assert.False(res3.Success);
                Assert.Equal(429, res3.HttpStatusCode);
                Assert.Equal(callsBefore, host.MockServer.UserCheckCallCount);
            }
            finally
            {
                _client.ResetCircuitBreaker(controller.IP);
                host.MockServer.ResetDefaults();
            }
        }

        #endregion

        #region Private Helpers

        private async Task<(VwController Controller, VwScene Scene)> CreateSetupSceneFixtures()
        {
            var controller = TestController;
            await _db.Insertable(controller).ExecuteCommandAsync();

            var screen = new VwScreen
            {
                ID = $"{TestPrefix}SCR_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}SCR_{Guid.NewGuid():N}",
                Name = "Screen 1",
                ControllerId = controller.ID,
                GridCol = 0,
                GridRow = 0,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(screen).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCENE_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}SCENE_{Guid.NewGuid():N}",
                Name = "Test Scene Setup",
                OutputId = "1",
                ControllerId = controller.ID,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var source = new VwSource
            {
                ID = $"{TestPrefix}SRC_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}SRC_{Guid.NewGuid():N}",
                Name = "Source 1",
                ControllerId = controller.ID,
                SignalNo = 1,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(source).ExecuteCommandAsync();

            var window = new VwWindowScene
            {
                ID = $"{TestPrefix}WIN_{Guid.NewGuid():N}",
                Code = $"{TestPrefix}WIN_{Guid.NewGuid():N}",
                SceneId = scene.ID,
                SourceId = source.ID,
                X = 0,
                Y = 0,
                W = 1920,
                H = 1080,
                Visible = BaseEnums.SceneWindowVisible.Visible,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(window).ExecuteCommandAsync();

            return (controller, scene);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Vị trí xuất hiện ĐẦU TIÊN của một lệnh trong log ReceivedRequests; -1 nếu không có.
        /// Created date: 24/08/2026
        /// </summary>
        private static int FirstIndexOf(IReadOnlyList<string> log, string method, string urlFragment)
        {
            for (var i = 0; i < log.Count; i++)
            {
                var entry = log[i];
                var parts = entry.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    continue;

                var entryMethod = parts[1];
                var entryPath = parts[2];

                if (!entryMethod.Equals(method, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (urlFragment.Equals("/ISAPI/DisplayDev/VideoWall", StringComparison.OrdinalIgnoreCase))
                {
                    if (entryPath.Equals("/ISAPI/DisplayDev/VideoWall", StringComparison.OrdinalIgnoreCase))
                        return i;
                }
                else if (entryPath.Contains(urlFragment, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static VwController CreateResolverTestController() => new()
        {
            ID = $"TEST_RESOLVER_CTRL_{Guid.NewGuid():N}",
            Name = "Test Controller Resolver",
            Code = $"TEST_CTRL_{Guid.NewGuid():N}",
            IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
            Account = VwISAPIMockServerHikvision.DefaultUser,
            PassWord = VwISAPIMockServerHikvision.DefaultPassword,
            Status = BaseEnums.StatusEnum.Enable
        };

        private VwISAPIDeviceService CreateResolverWithWallNo(
            IServiceScope scope,
            int? configuredWallNo,
            ILogger<VwISAPIDeviceService>? customLogger = null)
        {
            var customOptions = Microsoft.Extensions.Options.Options.Create(new VwDeviceOptions
            {
                Device = new VwDeviceConnectionOptions
                {
                    WallNo = configuredWallNo
                }
            });

            return new VwISAPIDeviceService(
                _client,
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwController>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwScene>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwWindowScene>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwSource>>(),
                scope.ServiceProvider.GetRequiredService<BaseRepository<VwScreen>>(),
                scope.ServiceProvider.GetRequiredService<VwSceneRegionService>(),
                scope.ServiceProvider.GetRequiredService<VwISAPICredentialResolver>(),
                scope.ServiceProvider.GetRequiredService<VwOrgAccessService>(),
                scope.ServiceProvider.GetRequiredService<BaseCacheService>(),
                scope.ServiceProvider.GetRequiredService<IVwEventTriggerLogWriter>(),
                customOptions,
                customLogger ?? scope.ServiceProvider.GetRequiredService<ILogger<VwISAPIDeviceService>>());
        }

        private sealed class CapturingLoggerTest<T> : ILogger<T>
        {
            public List<(LogLevel Level, string Message)> Entries { get; } = [];

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Entries.Add((logLevel, formatter(state, exception)));
            }
        }

        #endregion
    }
}

