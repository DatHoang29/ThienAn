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
                _service.ResetAllCircuitBreakers();
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
                _service.ResetAllCircuitBreakers();
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
                _service.ResetAllCircuitBreakers();
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
    }
}
