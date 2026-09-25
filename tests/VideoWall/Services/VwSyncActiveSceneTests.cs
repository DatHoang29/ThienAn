using ITS.VideoWall.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Module.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Entities;
using Shared.DTO.Enums;
using SqlSugar;
using Tests.Modules.VideoWall.MockServer;
using Xunit;

namespace Tests.Modules.VideoWall.Services
{
    /// <summary>
    /// Description: Kiểm thử action SyncActiveScene — đối chiếu/đồng bộ ngược active scene thật từ thiết bị về CSDL.
    /// Created date: 13/09/2026
    /// </summary>
    [Collection("api")]
    public class VwSyncActiveSceneTests(Host host)
    {
        private const string TestPrefix = "TEST_SYNC_ACT_";
        private readonly IVwISAPIDeviceService _service = host.Services.GetRequiredService<IVwISAPIDeviceService>();
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly VwISAPIMockServerHikvision _mock = host.MockServer;

        /// <summary>
        /// Description: Thiết bị đang chạy scene 1, DB đang lưu lệch (hoặc rỗng) -> cập nhật VwController.ActiveSceneId,
        ///              bật cờ VwScene.ActiveScene cho đúng scene 1, hạ cờ scene cũ.
        /// Created date: 13/09/2026
        /// </summary>
        [Fact]
        public async Task SyncActiveScene_WhenDeviceSceneMatchesDbScene_UpdatesDbAndSetsSynced_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.ActiveSceneId = 771;

            var controller = new VwController
            {
                ID = $"{TestPrefix}CTRL_{Guid.NewGuid():N}",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                ActiveSceneId = null,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(controller).ExecuteCommandAsync();

            var sceneOld = new VwScene
            {
                ID = $"{TestPrefix}SCN_OLD_{Guid.NewGuid():N}",
                Name = "Old Scene",
                OutputId = "770",
                ControllerId = controller.ID,
                ActiveScene = BaseEnums.ActiveScene.Activate,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var sceneTarget = new VwScene
            {
                ID = $"{TestPrefix}SCN_NEW_{Guid.NewGuid():N}",
                Name = "Target Running Scene",
                OutputId = "771", // Khớp MockServer.ActiveSceneId = 771
                ControllerId = controller.ID,
                ActiveScene = BaseEnums.ActiveScene.DeActivate,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(new[] { sceneOld, sceneTarget }).ExecuteCommandAsync();

            try
            {
                // Act
                var result = await _service.SyncActiveScene(new VwSyncActiveSceneInput
                {
                    ID = controller.ID
                });

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Synced);
                Assert.True(result.HasMismatch);
                Assert.Equal(771, result.DeviceActiveSceneId);
                Assert.Equal(sceneTarget.ID, result.MatchedSceneId);

                // Verify DB controller
                var updatedCtrl = await _db.Queryable<VwController>().FirstAsync(c => c.ID == controller.ID);
                Assert.Equal(sceneTarget.ID, updatedCtrl.ActiveSceneId);
                Assert.NotNull(updatedCtrl.ActiveSceneAt);

                // Verify DB scenes
                var updatedOld = await _db.Queryable<VwScene>().FirstAsync(s => s.ID == sceneOld.ID);
                Assert.Equal(BaseEnums.ActiveScene.DeActivate, updatedOld.ActiveScene);

                var updatedTarget = await _db.Queryable<VwScene>().FirstAsync(s => s.ID == sceneTarget.ID);
                Assert.Equal(BaseEnums.ActiveScene.Activate, updatedTarget.ActiveScene);
            }
            finally
            {
                _service.ResetAllDeviceAuthFailures();
                await _db.Deleteable<VwController>().Where(c => c.ID == controller.ID).ExecuteCommandAsync();
                await _db.Deleteable<VwScene>().Where(s => s.ID == sceneOld.ID || s.ID == sceneTarget.ID).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: Thiết bị đang chạy scene 772, DB đã khớp sẵn (ActiveSceneId = scene) ->
        ///              không có write nào vào DB, HasMismatch = false.
        /// Created date: 13/09/2026
        /// </summary>
        [Fact]
        public async Task SyncActiveScene_WhenDeviceSceneAlreadyMatchesDb_DoesNotModifyDb_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.ActiveSceneId = 772;

            var controller = new VwController
            {
                ID = $"{TestPrefix}CTRL_MATCH_{Guid.NewGuid():N}",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                ActiveSceneId = null,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };

            var scene = new VwScene
            {
                ID = $"{TestPrefix}SCN_MATCH_{Guid.NewGuid():N}",
                Name = "Already Active Scene",
                OutputId = "772",
                ControllerId = controller.ID,
                ActiveScene = BaseEnums.ActiveScene.Activate,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            controller.ActiveSceneId = scene.ID; // Đã khớp sẵn

            await _db.Insertable(scene).ExecuteCommandAsync();
            await _db.Insertable(controller).ExecuteCommandAsync();

            try
            {
                // Act
                var result = await _service.SyncActiveScene(new VwSyncActiveSceneInput
                {
                    ID = controller.ID
                });

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Synced);
                Assert.False(result.HasMismatch);
                Assert.Equal("Đã đồng bộ, không lệch.", result.Message);
            }
            finally
            {
                _service.ResetAllDeviceAuthFailures();
                await _db.Deleteable<VwController>().Where(c => c.ID == controller.ID).ExecuteCommandAsync();
                await _db.Deleteable<VwScene>().Where(s => s.ID == scene.ID).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: Thiết bị đang chạy scene 99 KHÔNG tồn tại trong DB -> chỉ ghi nhận mismatch,
        ///              KHÔNG tự tạo bản ghi VwScene mới, không crash.
        /// Created date: 13/09/2026
        /// </summary>
        [Fact]
        public async Task SyncActiveScene_WhenDeviceSceneNotInDb_RecordsMismatchWithoutCreatingScene_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.ActiveSceneId = 99; // Scene 99 không có trong DB

            var controller = new VwController
            {
                ID = $"{TestPrefix}CTRL_NOMATCH_{Guid.NewGuid():N}",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                ActiveSceneId = null,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(controller).ExecuteCommandAsync();

            var sceneCountBefore = await _db.Queryable<VwScene>().CountAsync();

            try
            {
                // Act
                var result = await _service.SyncActiveScene(new VwSyncActiveSceneInput
                {
                    ID = controller.ID
                });

                // Assert
                Assert.NotNull(result);
                Assert.False(result.Synced);
                Assert.True(result.HasMismatch);
                Assert.NotNull(result.Mismatch);
                Assert.Equal("99", result.Mismatch.DeviceValue);
                Assert.Null(result.MatchedSceneId);

                // Không được tạo scene mới
                var sceneCountAfter = await _db.Queryable<VwScene>().CountAsync();
                Assert.Equal(sceneCountBefore, sceneCountAfter);
            }
            finally
            {
                _service.ResetAllDeviceAuthFailures();
                await _db.Deleteable<VwController>().Where(c => c.ID == controller.ID).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: Gọi SyncActiveScene lên bộ điều khiển có Role == "sub" -> ném InvalidOperationException (kế thừa LoadController guard).
        /// Created date: 13/09/2026
        /// </summary>
        [Fact]
        public async Task SyncActiveScene_WhenSubController_ThrowsInvalidOperationException_Test()
        {
            // Arrange
            var subController = new VwController
            {
                ID = $"{TestPrefix}CTRL_SUB_{Guid.NewGuid():N}",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Role = "sub", // Bộ con
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(subController).ExecuteCommandAsync();

            try
            {
                // Act & Assert
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    _service.SyncActiveScene(new VwSyncActiveSceneInput { ID = subController.ID }));

                Assert.Contains("CON", ex.Message, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                await _db.Deleteable<VwController>().Where(c => c.ID == subController.ID).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: Hệ thống có 3 controller (A, B, C) độc lập, mỗi controller có 1 scene đang Active.
        ///              Khi gọi SyncActiveScene cho controller A với scene mới, chỉ scene cũ của controller A bị DeActivate,
        ///              scene mới của controller A được Activate, còn scene của controller B và C vẫn giữ nguyên cờ Activate.
        /// Created date: 13/09/2026
        /// </summary>
        [Fact]
        public async Task SyncActiveScene_MultiController_DoesNotDeactivateOtherControllersScenes_Test()
        {
            // Arrange
            _mock.ResetDefaults();
            _mock.ActiveSceneId = 774;

            var ctrlA = new VwController
            {
                ID = $"{TestPrefix}CTRL_A_{Guid.NewGuid():N}",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var ctrlB = new VwController
            {
                ID = $"{TestPrefix}CTRL_B_{Guid.NewGuid():N}",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var ctrlC = new VwController
            {
                ID = $"{TestPrefix}CTRL_C_{Guid.NewGuid():N}",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };

            var sceneA1 = new VwScene
            {
                ID = $"{TestPrefix}SCN_A1_{Guid.NewGuid():N}",
                Name = "Ctrl A Old Scene",
                OutputId = "771",
                ControllerId = ctrlA.ID,
                ActiveScene = BaseEnums.ActiveScene.Activate,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var sceneA2 = new VwScene
            {
                ID = $"{TestPrefix}SCN_A2_{Guid.NewGuid():N}",
                Name = "Ctrl A Target Running Scene",
                OutputId = "774",
                ControllerId = ctrlA.ID,
                ActiveScene = BaseEnums.ActiveScene.DeActivate,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var sceneB = new VwScene
            {
                ID = $"{TestPrefix}SCN_B_{Guid.NewGuid():N}",
                Name = "Ctrl B Running Scene",
                OutputId = "772",
                ControllerId = ctrlB.ID,
                ActiveScene = BaseEnums.ActiveScene.Activate,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            var sceneC = new VwScene
            {
                ID = $"{TestPrefix}SCN_C_{Guid.NewGuid():N}",
                Name = "Ctrl C Running Scene",
                OutputId = "773",
                ControllerId = ctrlC.ID,
                ActiveScene = BaseEnums.ActiveScene.Activate,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };

            ctrlA.ActiveSceneId = sceneA1.ID;
            ctrlB.ActiveSceneId = sceneB.ID;
            ctrlC.ActiveSceneId = sceneC.ID;

            await _db.Insertable(new[] { ctrlA, ctrlB, ctrlC }).ExecuteCommandAsync();
            await _db.Insertable(new[] { sceneA1, sceneA2, sceneB, sceneC }).ExecuteCommandAsync();

            try
            {
                // Act
                var result = await _service.SyncActiveScene(new VwSyncActiveSceneInput
                {
                    ID = ctrlA.ID
                });

                // Assert
                Assert.NotNull(result);
                Assert.True(result.Synced);
                Assert.True(result.HasMismatch);
                Assert.Equal(774, result.DeviceActiveSceneId);
                Assert.Equal(sceneA2.ID, result.MatchedSceneId);

                // Controller A updated
                var updatedCtrlA = await _db.Queryable<VwController>().FirstAsync(c => c.ID == ctrlA.ID);
                Assert.Equal(sceneA2.ID, updatedCtrlA.ActiveSceneId);
                Assert.NotNull(updatedCtrlA.ActiveSceneAt);

                // Controller B & C unchanged
                var updatedCtrlB = await _db.Queryable<VwController>().FirstAsync(c => c.ID == ctrlB.ID);
                Assert.Equal(sceneB.ID, updatedCtrlB.ActiveSceneId);

                var updatedCtrlC = await _db.Queryable<VwController>().FirstAsync(c => c.ID == ctrlC.ID);
                Assert.Equal(sceneC.ID, updatedCtrlC.ActiveSceneId);

                // Scene A1 deactivated, Scene A2 activated
                var updatedA1 = await _db.Queryable<VwScene>().FirstAsync(s => s.ID == sceneA1.ID);
                Assert.Equal(BaseEnums.ActiveScene.DeActivate, updatedA1.ActiveScene);

                var updatedA2 = await _db.Queryable<VwScene>().FirstAsync(s => s.ID == sceneA2.ID);
                Assert.Equal(BaseEnums.ActiveScene.Activate, updatedA2.ActiveScene);

                // Scene B & Scene C MUST REMAIN ACTIVATED
                var updatedB = await _db.Queryable<VwScene>().FirstAsync(s => s.ID == sceneB.ID);
                Assert.Equal(BaseEnums.ActiveScene.Activate, updatedB.ActiveScene);

                var updatedC = await _db.Queryable<VwScene>().FirstAsync(s => s.ID == sceneC.ID);
                Assert.Equal(BaseEnums.ActiveScene.Activate, updatedC.ActiveScene);
            }
            finally
            {
                _service.ResetAllDeviceAuthFailures();
                await _db.Deleteable<VwController>().Where(c => c.ID == ctrlA.ID || c.ID == ctrlB.ID || c.ID == ctrlC.ID).ExecuteCommandAsync();
                await _db.Deleteable<VwScene>().Where(s => s.ID == sceneA1.ID || s.ID == sceneA2.ID || s.ID == sceneB.ID || s.ID == sceneC.ID).ExecuteCommandAsync();
            }
        }
    }
}
