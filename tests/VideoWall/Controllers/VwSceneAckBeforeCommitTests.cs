using Microsoft.Extensions.DependencyInjection;
using Module.VideoWall.Controllers.Scene.Commands;
using Module.VideoWall.Controllers.Scene.Validators;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Command;
using Module.VideoWall.Core.Dto.Scene;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Access;
using Shared.DTO.Enums;
using Shared.Infrastructure.Services;
using SqlSugar;
using Tests.Modules.VideoWall.MockServer;
using Xunit;

namespace Tests.Modules.VideoWall.Controllers
{
    /// <summary>
    /// Description: Kiểm thử luồng kích hoạt kịch bản Fire-and-Forget phía WebAPI.
    ///              BE publish lệnh xuống Worker qua NATS và trả về ngay mà không chờ ACK;
    ///              BE không tự cập nhật ActiveScene trong DB (Worker phụ trách cập nhật).
    /// Created date: 13/09/2026
    /// </summary>
    [Collection("api")]
    public class VwSceneAckBeforeCommitTests(Host host)
    {
        private const string TestPrefix = "TEST_FAF_";
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly VwPermissionService _permission = host.Services.GetRequiredService<VwPermissionService>();
        private readonly IVwEventTriggerLogWriter _logWriter = host.Services.GetRequiredService<IVwEventTriggerLogWriter>();

        /// <summary>
        /// Description: Kích hoạt kịch bản theo cơ chế Fire-and-Forget: validate input, publish lệnh NATS và trả output ngay
        /// Created date: 13/09/2026
        /// </summary>
        [Fact]
        public async Task ActivateScene_FireAndForget_PublishesCommandAndReturnsOutputImmediately_Test()
        {
            host.MockServer.ResetDefaults();

            var ctrlCode = $"{TestPrefix}CTRL_{Guid.NewGuid():N}";
            var sceneCode = $"{TestPrefix}SCN_{Guid.NewGuid():N}";

            var controller = new VwController
            {
                Code = ctrlCode,
                Name = "Center Controller",
                Role = "center",
                IntegrationMode = "cascade",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(controller).ExecuteCommandAsync();

            var scene = new VwScene
            {
                Code = sceneCode,
                Name = "Scene Fire And Forget",
                ControllerId = controller.ID,
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Enable,
                ActiveScene = BaseEnums.ActiveScene.DeActivate,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            try
            {
                // 1. Kiểm tra Validator bắt buộc
                var validator = new VwActiveSceneValidator(host.Localizer);
                var invalidResult = await validator.ValidateAsync(new VwActiveSceneInput());
                Assert.False(invalidResult.IsValid);

                var validResult = await validator.ValidateAsync(new VwActiveSceneInput { Code = sceneCode });
                Assert.True(validResult.IsValid);

                // 2. Thực thi CommandHandler với FakeVwPublisher
                using var scope = host.Services.CreateScope();
                var sp = scope.ServiceProvider;
                var fakePublisher = new FakeVwPublisher();

                var handler = new VwSceneWorkflowCommandHandler(
                    sp.GetRequiredService<BaseRepository<VwScene>>(),
                    sp.GetRequiredService<BaseRepository<VwWindowScene>>(),
                    sp.GetRequiredService<BaseRepository<VwController>>(),
                    sp.GetRequiredService<BaseRepository<VwEventRule>>(),
                    _permission,
                    fakePublisher,
                    _logWriter);

                var output = await handler.HandleAsync(new VwActiveSceneInput { Code = sceneCode });

                // Assert: Output trả về ngay
                Assert.NotNull(output);
                Assert.Equal(scene.ID, output.ID);
                Assert.NotNull(output.TriggerLogId);

                // Assert: Lệnh đã được publish fire-and-forget qua IVwPublisher
                Assert.Equal(1, fakePublisher.CallCount);
                Assert.Equal(VwCommandActions.ActivateScene, fakePublisher.LastAction);
                Assert.Equal(scene.ID, fakePublisher.LastSceneId);
                Assert.Equal(controller.ID, fakePublisher.LastControllerId);

                // Assert: BE không tự cập nhật ActiveScene trong DB (việc này do Worker thực thi)
                var dbCtrl = await _db.Queryable<VwController>().FirstAsync(u => u.ID == controller.ID);
                Assert.Null(dbCtrl.ActiveSceneId);

                var dbScene = await _db.Queryable<VwScene>().FirstAsync(u => u.ID == scene.ID);
                Assert.Equal(BaseEnums.ActiveScene.DeActivate, dbScene.ActiveScene);

                // Assert: Log trigger được ghi nhận trạng thái Success
                var log = await _db.Queryable<VwEventTriggerLog>().FirstAsync(u => u.ID == output.TriggerLogId);
                Assert.NotNull(log);
                Assert.Equal(BaseEnums.SuccessEnums.Success, log.Success);
            }
            finally
            {
                await _db.Deleteable<VwScene>(s => s.ID == scene.ID).ExecuteCommandAsync();
                await _db.Deleteable<VwController>(c => c.ID == controller.ID).ExecuteCommandAsync();
                await _db.Deleteable<VwEventTriggerLog>(l => l.SceneId == scene.ID).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// Description: Kích hoạt kịch bản khi Status = Disable -> ném lỗi và ghi nhận log thất bại
        /// Created date: 13/09/2026
        /// </summary>
        [Fact]
        public async Task ActivateScene_WhenSceneDisabled_LogsFailAndThrows_Test()
        {
            var ctrlCode = $"{TestPrefix}CTRL_{Guid.NewGuid():N}";
            var sceneCode = $"{TestPrefix}SCN_{Guid.NewGuid():N}";

            var controller = new VwController
            {
                Code = ctrlCode,
                Name = "Center Controller",
                Role = "center",
                IntegrationMode = "cascade",
                IP = $"127.0.0.1:{VwISAPIMockServerHikvision.DefaultPort}",
                Account = VwISAPIMockServerHikvision.DefaultUser,
                PassWord = VwISAPIMockServerHikvision.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(controller).ExecuteCommandAsync();

            var scene = new VwScene
            {
                Code = sceneCode,
                Name = "Scene Disabled",
                ControllerId = controller.ID,
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Disable,
                ActiveScene = BaseEnums.ActiveScene.DeActivate,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            try
            {
                using var scope = host.Services.CreateScope();
                var sp = scope.ServiceProvider;
                var fakePublisher = new FakeVwPublisher();

                var handler = new VwSceneWorkflowCommandHandler(
                    sp.GetRequiredService<BaseRepository<VwScene>>(),
                    sp.GetRequiredService<BaseRepository<VwWindowScene>>(),
                    sp.GetRequiredService<BaseRepository<VwController>>(),
                    sp.GetRequiredService<BaseRepository<VwEventRule>>(),
                    _permission,
                    fakePublisher,
                    _logWriter);

                await Assert.ThrowsAnyAsync<Exception>(() =>
                    handler.HandleAsync(new VwActiveSceneInput { Code = sceneCode }));

                // Không publish lệnh nào
                Assert.Equal(0, fakePublisher.CallCount);

                // Log thất bại được ghi
                var log = await _db.Queryable<VwEventTriggerLog>().FirstAsync(u => u.SceneId == scene.ID);
                Assert.NotNull(log);
                Assert.Equal(BaseEnums.SuccessEnums.Fail, log.Success);
            }
            finally
            {
                await _db.Deleteable<VwScene>(s => s.ID == scene.ID).ExecuteCommandAsync();
                await _db.Deleteable<VwController>(c => c.ID == controller.ID).ExecuteCommandAsync();
                await _db.Deleteable<VwEventTriggerLog>(l => l.SceneId == scene.ID).ExecuteCommandAsync();
            }
        }

        private sealed class FakeVwPublisher : IVwPublisher
        {
            public int CallCount { get; private set; }
            public string? LastAction { get; private set; }
            public string? LastSceneId { get; private set; }
            public string? LastControllerId { get; private set; }
            public object? LastPayload { get; private set; }

            public bool PublishCommand(VwCommandEnvelope envelope) => true;
            public bool PublishCommand<T>(string action, T payload, string? controllerId = null, string? sceneId = null) => true;

            public Task<bool> PublishCommandAsync<T>(string action, T payload, string? controllerId = null, string? sceneId = null, CancellationToken ct = default)
            {
                CallCount++;
                LastAction = action;
                LastPayload = payload;
                LastControllerId = controllerId;
                LastSceneId = sceneId;
                return Task.FromResult(true);
            }
        }
    }
}
