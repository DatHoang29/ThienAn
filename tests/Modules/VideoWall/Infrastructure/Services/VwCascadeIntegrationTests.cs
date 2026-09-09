using Microsoft.Extensions.DependencyInjection;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;
using Module.VideoWall.Infrastructure.Services.Scene;
using Shared.DTO.Constants.Application;
using SqlSugar;
using System.Reflection;
using Tests.Modules.VideoWall.MockServer;
using Xunit;

namespace Tests.Modules.VideoWall.Infrastructure.Services
{
    /// <summary>
    /// Description: Bộ kiểm thử tích hợp Nhóm D (D1-D12) cho luồng kích hoạt kịch bản và đồng bộ cửa sổ
    ///              qua MockServer theo kiến trúc Cascade DS-C66S (chỉ giao tiếp với bộ trung tâm).
    /// Created date: 08/09/2026
    /// </summary>
    [Collection("api")]
    public class VwCascadeIntegrationTests(Host host)
    {
        private const string TestPrefix = "TEST_CASCADE_INT_";
        private readonly IVwISAPIDeviceService _service = host.Services.GetRequiredService<IVwISAPIDeviceService>();
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly VwISAPIMockServerHikvision _mock = host.MockServer;

        /// <summary>
        /// Description: D1 - Kích hoạt scene toàn tường chỉ gửi lệnh tới 1 IP của bộ điều khiển trung tâm.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task D1_ActivateScene_FullScreen_SendsRequestsOnlyToCenterController()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;

            var center = new VwController
            {
                ID = $"{TestPrefix}CTRL_CTR_{Guid.NewGuid():N}",
                Role = "center",
                IntegrationMode = "active",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(center).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCN_FULL_{Guid.NewGuid():N}",
                Code = "SCN_FULL_01",
                Name = "Toàn Tường 8x4",
                ControllerId = null,
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            // Act
            await _service.ActivateScene(scene, [center], [center.ID]);

            // Assert
            Assert.True(_mock.ActivateSceneCallCount >= 1);
        }

        /// <summary>
        /// Description: D2 - Đồng bộ kịch bản toàn tường 21 cửa sổ tạo đúng 21 cửa sổ đơn trên bộ trung tâm không bị cắt lát.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task D2_SyncWindows_FullScreen_CreatesExactlyTwentyOneWindowsWithoutSlicing()
        {
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;
            await _db.Deleteable<VwScreen>().ExecuteCommandAsync();
            await _db.Deleteable<VwController>().ExecuteCommandAsync();

            var center = new VwController
            {
                ID = $"{TestPrefix}CTRL_CTR_{Guid.NewGuid():N}",
                Role = "center",
                IntegrationMode = "active",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(center).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCN_{Guid.NewGuid():N}",
                Code = "SCN_21WND",
                Name = "Kịch bản 21 cửa sổ",
                ControllerId = null,
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var windows = new List<VwWindowScene>();
            for (var i = 1; i <= 21; i++)
            {
                windows.Add(new VwWindowScene
                {
                    ID = $"{TestPrefix}WND_{i}_{Guid.NewGuid():N}",
                    SceneId = scene.ID,
                    X = (i % 8) * 1920,
                    Y = (i / 8) * 1080,
                    W = 1920,
                    H = 1080
                });
            }
            await _db.Insertable(windows).ExecuteCommandAsync();

            // Act
            await _service.SyncSceneWindowsToDevice(scene.ID);

            // Assert
            Assert.Equal(21, _mock.AddWindowCallCount);
            Assert.True(_mock.SaveSceneDataCallCount >= 1);
        }

        /// <summary>
        /// Description: D3 - Khi SaveSceneData trả 403, hệ thống fallback tạo mới scene và lưu lại thành công vào DB.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task D3_SaveData_WhenInitialSaveFails_FallsBackToCreateSceneAndRetriesSave()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;
            _mock.SimulateSaveData403Once = true;

            var center = new VwController
            {
                ID = $"{TestPrefix}CTRL_CTR_{Guid.NewGuid():N}",
                Role = "center",
                IntegrationMode = "active",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(center).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCN_FB_{Guid.NewGuid():N}",
                Code = "SCN_FALLBACK",
                Name = "Scene Fallback Test",
                ControllerId = null,
                OutputId = "999",
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var window = new VwWindowScene
            {
                ID = $"{TestPrefix}WND_FB_{Guid.NewGuid():N}",
                SceneId = scene.ID,
                X = 0,
                Y = 0,
                W = 1920,
                H = 1080
            };
            await _db.Insertable(window).ExecuteCommandAsync();

            // Act
            await _service.SyncSceneWindowsToDevice(scene.ID, window);

            // Assert
            var updatedScene = await _db.Queryable<VwScene>().FirstAsync(s => s.ID == scene.ID);
            Assert.NotNull(updatedScene);
            Assert.NotEqual("999", updatedScene.OutputId);
        }

        /// <summary>
        /// Description: D4 - Khi POST window trả lỗi kênh không hợp lệ, luồng dừng và ném ngoại lệ rõ ràng.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task D4_AddWindow_WhenChannelInvalid_StopsSyncAndThrows()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;
            _mock.SimulateWindowInputChannelInvalid = true;

            var center = new VwController
            {
                ID = $"{TestPrefix}CTRL_CTR_{Guid.NewGuid():N}",
                Role = "center",
                IntegrationMode = "active",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(center).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCN_ERR_{Guid.NewGuid():N}",
                Code = "SCN_ERR_01",
                ControllerId = null,
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var window = new VwWindowScene
            {
                ID = $"{TestPrefix}WND_ERR_{Guid.NewGuid():N}",
                SceneId = scene.ID,
                X = 0,
                Y = 0,
                W = 1920,
                H = 1080
            };
            await _db.Insertable(window).ExecuteCommandAsync();

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(() =>
                _service.SyncSceneWindowsToDevice(scene.ID, window));
        }

        /// <summary>
        /// Description: D5 - Kích hoạt kịch bản sau đó kiểm tra lại kịch bản đang chạy khớp với SID vừa kích hoạt.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task D5_ActivateScene_ThenVerifyRunningScene_MatchesActivatedSid()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;

            var center = new VwController
            {
                ID = $"{TestPrefix}CTRL_CTR_{Guid.NewGuid():N}",
                Role = "center",
                IntegrationMode = "active",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(center).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCN_ACT_{Guid.NewGuid():N}",
                Code = "SCN_ACT_01",
                ControllerId = null,
                OutputId = "3",
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            // Act
            await _service.ActivateScene(scene, [center], [center.ID]);

            // Assert
            Assert.Equal(3, _mock.ActiveSceneId);
        }

        /// <summary>
        /// Description: D6 - Bộ trung tâm quản lý 2 wall logic được kích hoạt trên cả 2 wall.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task D6_CenterController_WithTwoBoundWalls_ActivatesOnBothWalls()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;
            _mock.SimulateMultipleBoundWalls = true;

            var center = new VwController
            {
                ID = $"{TestPrefix}CTRL_CTR_{Guid.NewGuid():N}",
                Role = "center",
                IntegrationMode = "active",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(center).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCN_W2_{Guid.NewGuid():N}",
                Code = "SCN_W2",
                ControllerId = null,
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            // Act
            await _service.ActivateScene(scene, [center], [center.ID]);

            // Assert
            Assert.Equal(2, _mock.ActivateSceneCallCount);
        }

        /// <summary>
        /// Description: D7 - Chạy SyncSceneWindowsToDevice hai lần liên tiếp xoá và tạo lại sạch sẽ, không nhân đôi cửa sổ.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task D7_SyncWindows_RunTwice_DeletesAndRecreatesWithoutDuplication()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;

            var center = new VwController
            {
                ID = $"{TestPrefix}CTRL_CTR_{Guid.NewGuid():N}",
                Role = "center",
                IntegrationMode = "active",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(center).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCN_REP_{Guid.NewGuid():N}",
                Code = "SCN_REPEAT",
                ControllerId = null,
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var windows = new List<VwWindowScene>
            {
                new()
                {
                    ID = $"{TestPrefix}WND_1_{Guid.NewGuid():N}",
                    SceneId = scene.ID,
                    X = 0,
                    Y = 0,
                    W = 1920,
                    H = 1080
                },
                new()
                {
                    ID = $"{TestPrefix}WND_2_{Guid.NewGuid():N}",
                    SceneId = scene.ID,
                    X = 1920,
                    Y = 0,
                    W = 1920,
                    H = 1080
                }
            };
            await _db.Insertable(windows).ExecuteCommandAsync();

            // Act
            await _service.SyncSceneWindowsToDevice(scene.ID);
            var initialAddCount = _mock.AddWindowCallCount;
            await _service.SyncSceneWindowsToDevice(scene.ID);

            // Assert
            Assert.Equal(initialAddCount + 2, _mock.AddWindowCallCount);
            Assert.True(_mock.DeleteAllWindowsCallCount >= 2);
        }

        /// <summary>
        /// Description: D8 - Lỗi xác thực nhiều lần liên tiếp sẽ kích hoạt circuit breaker khoá IP bộ điều khiển.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public void D8_CircuitBreaker_ConsecutiveAuthFailures_LocksOutControllerIp()
        {
            // Arrange
            var testIp = $"192.168.200.{Random.Shared.Next(10, 250)}";
            _service.ResetCircuitBreaker(testIp);

            try
            {
                // Act
                for (var i = 0; i < 5; i++)
                {
                    _service.RecordCircuitBreakerFailure(testIp, 401);
                }

                var isBlocked = _service.IsCircuitBreakerBlocked(testIp, out var remaining);

                // Assert
                Assert.True(isBlocked);
                Assert.True(remaining.TotalSeconds > 0);
            }
            finally
            {
                _service.ResetCircuitBreaker(testIp);
            }
        }

        /// <summary>
        /// Description: D9 - Cửa sổ vượt quá toạ độ canvas toàn tường bị từ chối trước khi gửi lệnh thiết bị.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task D9_WindowExceedingWallCanvas_RejectedBeforeDeviceCalls()
        {
            // Arrange
            _mock.ResetDefaults();
            var initialAddCalls = _mock.AddWindowCallCount;

            var topo = new VwWallTopology
            {
                ID = $"{TestPrefix}TOPO_{Guid.NewGuid():N}",
                Code = "WALL-01",
                Name = "Topology 8x4",
                Cols = 8,
                Rows = 4,
                ScreenWidth = 1920,
                ScreenHeight = 1080
            };
            await _db.Insertable(topo).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCN_OOB_{Guid.NewGuid():N}",
                Code = "SCN_OOB",
                ControllerId = null,
                OutputId = "1"
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var regionService = host.Services.GetRequiredService<VwSceneRegionService>();

            // Act & Assert (15000 + 1000 = 16000 > 15360)
            var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
                regionService.EnsureWindowInsideSceneRegionAsync(scene.ID, 15000, 0, 1000, 500, "OOB Window"));

            Assert.Contains("ngoài tường", ex.Message);
            Assert.Equal(initialAddCalls, _mock.AddWindowCallCount);
        }

        /// <summary>
        /// Description: D10 - Kịch bản vùng (cửa sổ cột 5-6) vẫn được định tuyến tới bộ trung tâm mà không bị chia cắt cho bộ con.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task D10_ZoneScene_WindowRoutedToCenterControllerWithoutSubSlicing()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;

            var center = new VwController
            {
                ID = $"{TestPrefix}CTRL_CTR_{Guid.NewGuid():N}",
                Role = "center",
                IntegrationMode = "active",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(center).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCN_ZONE_{Guid.NewGuid():N}",
                Code = "SCN_ZONE_01",
                ControllerId = center.ID,
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            var window = new VwWindowScene
            {
                ID = $"{TestPrefix}WND_ZONE_{Guid.NewGuid():N}",
                SceneId = scene.ID,
                X = 9600,
                Y = 0,
                W = 3840,
                H = 2160
            };
            await _db.Insertable(window).ExecuteCommandAsync();

            // Act
            await _service.SyncSceneWindowsToDevice(scene.ID, window);

            // Assert
            Assert.Equal(1, _mock.AddWindowCallCount);
        }

        /// <summary>
        /// Description: D11 - Khi capabilities thiết bị báo IsSupportScene=false, activate được bỏ qua an toàn không throw fatal.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task D11_Capabilities_WhenIsSupportSceneFalse_SkipsActivateWithoutFatal()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.IsCascadeCenter = true;
            _mock.IsSupportScene = false;

            var center = new VwController
            {
                ID = $"{TestPrefix}CTRL_CTR_{Guid.NewGuid():N}",
                Role = "center",
                IntegrationMode = "active",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(center).ExecuteCommandAsync();

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCN_NOSUPP_{Guid.NewGuid():N}",
                Code = "SCN_NOSUPP",
                ControllerId = null,
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            // Act
            var exception = await Record.ExceptionAsync(() =>
                _service.ActivateScene(scene, [center], [center.ID]));

            // Assert
            Assert.Null(exception);
        }

        /// <summary>
        /// Description: D12 - Khi GET VideoWall không có wall nào bound cổng ra, ResolveWalls ném ngoại lệ rõ ràng.
        /// Created date: 08/09/2026
        /// </summary>
        [Fact]
        public async Task D12_ResolveWalls_WhenNoBoundWalls_ThrowsOops()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.SimulateNoBoundWall = true;

            var center = new VwController
            {
                ID = $"{TestPrefix}CTRL_CTR_{Guid.NewGuid():N}",
                Role = "center",
                IntegrationMode = "active",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable
            };
            await _db.Insertable(center).ExecuteCommandAsync();

            // Act & Assert
            var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
                _service.ResolveWalls(center));

            Assert.Contains("không có tường nào có cổng ra", ex.Message);
        }
    }
}
