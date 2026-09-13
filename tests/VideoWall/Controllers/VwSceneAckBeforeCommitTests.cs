using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Module.VideoWall.Controllers.Scene.Commands;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Scene;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;
using Module.VideoWall.Core.Options;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Access;
using Module.VideoWall.Infrastructure.Services.Messaging;
using Shared.DTO.Enums;
using Shared.Infrastructure.Services;
using SqlSugar;
using System.Security.Claims;
using Tests.Modules.VideoWall.MockServer;
using Xunit;

namespace Tests.Modules.VideoWall.Controllers
{
    /// <summary>
    /// Description: Kiểm thử cơ chế ACK-before-commit cho kích hoạt kịch bản (Task 4)
    ///              Chỉ ghi DB sau khi thiết bị thật/Worker xác nhận thành công qua NATS;
    ///              khi lỗi hoặc timeout thì giữ nguyên DB cũ và ghi log thất bại.
    /// Created date: 12/09/2026
    /// </summary>
    [Collection("api")]
    public class VwSceneAckBeforeCommitTests(Host host)
    {
        private const string TestPrefix = "TEST_ACK_";
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly BaseCacheService _cache = host.Services.GetRequiredService<BaseCacheService>();
        private readonly VwPermissionService _permission = host.Services.GetRequiredService<VwPermissionService>();
        private readonly IVwPublisher _publisher = host.Services.GetRequiredService<IVwPublisher>();
        private readonly IVwEventTriggerLogWriter _logWriter = host.Services.GetRequiredService<IVwEventTriggerLogWriter>();

        [Fact]
        public async Task ActivateScene_WhenAckSuccess_CommitsDbAndReturnsOutput_Test()
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
                Name = "Scene Ack Success",
                ControllerId = controller.ID,
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Enable,
                ActiveScene = BaseEnums.ActiveScene.DeActivate,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            try
            {
                using var scope = host.Services.CreateScope();
                var sp = scope.ServiceProvider;
                var fakeClient = new FakeNatsRequestClient();

                var handler = new VwSceneWorkflowCommandHandler(
                    sp.GetRequiredService<BaseRepository<VwScene>>(),
                    sp.GetRequiredService<BaseRepository<VwWindowScene>>(),
                    sp.GetRequiredService<BaseRepository<VwController>>(),
                    sp.GetRequiredService<BaseRepository<VwEventRule>>(),
                    _cache,
                    _permission,
                    _publisher,
                    _logWriter,
                    fakeClient);

                var output = await handler.HandleAsync(new VwActiveSceneInput { Code = sceneCode });

                Assert.NotNull(output);
                Assert.Equal(BaseEnums.ActiveScene.Activate, output.ActiveScene);

                var dbCtrl = await _db.Queryable<VwController>().FirstAsync(u => u.ID == controller.ID);
                Assert.Equal(scene.ID, dbCtrl.ActiveSceneId);

                var dbScene = await _db.Queryable<VwScene>().FirstAsync(u => u.ID == scene.ID);
                Assert.Equal(BaseEnums.ActiveScene.Activate, dbScene.ActiveScene);

                Assert.Equal(1, fakeClient.CallCount);
                Assert.Equal(VwCommandActions.ActivateScene, fakeClient.LastAction);
            }
            finally
            {
                await _db.Deleteable<VwScene>(s => s.ID == scene.ID).ExecuteCommandAsync();
                await _db.Deleteable<VwController>(c => c.ID == controller.ID).ExecuteCommandAsync();
                await _db.Deleteable<VwEventTriggerLog>(l => l.SceneId == scene.ID).ExecuteCommandAsync();
            }
        }

        [Fact]
        public async Task ActivateScene_WhenAckFails_ThrowsAndDoesNotUpdateDb_Test()
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
                Name = "Scene Ack Fails",
                ControllerId = controller.ID,
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Enable,
                ActiveScene = BaseEnums.ActiveScene.DeActivate,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            try
            {
                using var scope = host.Services.CreateScope();
                var sp = scope.ServiceProvider;
                var fakeClient = new FakeNatsRequestClient
                {
                    ExceptionToThrow = new InvalidOperationException("Thiết bị từ chối lệnh kích hoạt.")
                };

                var handler = new VwSceneWorkflowCommandHandler(
                    sp.GetRequiredService<BaseRepository<VwScene>>(),
                    sp.GetRequiredService<BaseRepository<VwWindowScene>>(),
                    sp.GetRequiredService<BaseRepository<VwController>>(),
                    sp.GetRequiredService<BaseRepository<VwEventRule>>(),
                    _cache,
                    _permission,
                    _publisher,
                    _logWriter,
                    fakeClient);

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    handler.HandleAsync(new VwActiveSceneInput { Code = sceneCode }));

                Assert.Contains("từ chối", ex.Message);

                // Verify DB NOT updated
                var dbCtrl = await _db.Queryable<VwController>().FirstAsync(u => u.ID == controller.ID);
                Assert.Null(dbCtrl.ActiveSceneId);

                var dbScene = await _db.Queryable<VwScene>().FirstAsync(u => u.ID == scene.ID);
                Assert.Equal(BaseEnums.ActiveScene.DeActivate, dbScene.ActiveScene);

                // Verify trigger logs recorded Fail for both business and device step
                var businessLog = await _db.Queryable<VwEventTriggerLog>().FirstAsync(u => u.SceneId == scene.ID && string.IsNullOrEmpty(u.StepName));
                Assert.NotNull(businessLog);
                Assert.Equal(BaseEnums.SuccessEnums.Fail, businessLog.Success);

                var deviceLog = await _db.Queryable<VwEventTriggerLog>().FirstAsync(u => u.SceneId == scene.ID && u.StepName != null);
                Assert.NotNull(deviceLog);
                Assert.Equal(BaseEnums.SuccessEnums.Fail, deviceLog.Success);
            }
            finally
            {
                await _db.Deleteable<VwScene>(s => s.ID == scene.ID).ExecuteCommandAsync();
                await _db.Deleteable<VwController>(c => c.ID == controller.ID).ExecuteCommandAsync();
                await _db.Deleteable<VwEventTriggerLog>(l => l.SceneId == scene.ID).ExecuteCommandAsync();
            }
        }

        [Fact]
        public async Task ActivateScene_WhenTimeout_ThrowsAndDoesNotUpdateDb_Test()
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
                Name = "Scene Timeout",
                ControllerId = controller.ID,
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Enable,
                ActiveScene = BaseEnums.ActiveScene.DeActivate,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            try
            {
                using var scope = host.Services.CreateScope();
                var sp = scope.ServiceProvider;
                var fakeClient = new FakeNatsRequestClient
                {
                    ExceptionToThrow = new TimeoutException("Hết thời gian chờ phản hồi từ thiết bị.")
                };

                var handler = new VwSceneWorkflowCommandHandler(
                    sp.GetRequiredService<BaseRepository<VwScene>>(),
                    sp.GetRequiredService<BaseRepository<VwWindowScene>>(),
                    sp.GetRequiredService<BaseRepository<VwController>>(),
                    sp.GetRequiredService<BaseRepository<VwEventRule>>(),
                    _cache,
                    _permission,
                    _publisher,
                    _logWriter,
                    fakeClient);

                var ex = await Assert.ThrowsAsync<TimeoutException>(() =>
                    handler.HandleAsync(new VwActiveSceneInput { Code = sceneCode }));

                Assert.Contains("Hết thời gian chờ", ex.Message);

                // Verify DB NOT updated
                var dbCtrl = await _db.Queryable<VwController>().FirstAsync(u => u.ID == controller.ID);
                Assert.Null(dbCtrl.ActiveSceneId);

                var dbScene = await _db.Queryable<VwScene>().FirstAsync(u => u.ID == scene.ID);
                Assert.Equal(BaseEnums.ActiveScene.DeActivate, dbScene.ActiveScene);

                // Verify trigger logs recorded Fail for both business and device step
                var businessLog = await _db.Queryable<VwEventTriggerLog>().FirstAsync(u => u.SceneId == scene.ID && string.IsNullOrEmpty(u.StepName));
                Assert.NotNull(businessLog);
                Assert.Equal(BaseEnums.SuccessEnums.Fail, businessLog.Success);

                var deviceLog = await _db.Queryable<VwEventTriggerLog>().FirstAsync(u => u.SceneId == scene.ID && u.StepName != null);
                Assert.NotNull(deviceLog);
                Assert.Equal(BaseEnums.SuccessEnums.Fail, deviceLog.Success);
            }
            finally
            {
                await _db.Deleteable<VwScene>(s => s.ID == scene.ID).ExecuteCommandAsync();
                await _db.Deleteable<VwController>(c => c.ID == controller.ID).ExecuteCommandAsync();
                await _db.Deleteable<VwEventTriggerLog>(l => l.SceneId == scene.ID).ExecuteCommandAsync();
            }
        }

        [Fact]
        public void Complete_WhenDuplicateAckReceived_IsIdempotent_Test()
        {
            var fakePublisher = new FakeNatsPublisher();
            var options = Microsoft.Extensions.Options.Options.Create(new VwDeviceOptions());
            var client = new VwNatsRequestClient(fakePublisher, options, NullLogger<VwNatsRequestClient>.Instance);

            // Duplicate Complete calls with unknown or already completed messageId must be completely safe and not throw
            var ex1 = Record.Exception(() => client.Complete("MSG_UNKNOWN_01", true, null, null));
            var ex2 = Record.Exception(() => client.Complete("MSG_UNKNOWN_01", false, "Error", null));

            Assert.Null(ex1);
            Assert.Null(ex2);
        }

        private sealed class FakeNatsRequestClient : IVwNatsRequestClient
        {
            public Exception? ExceptionToThrow { get; set; }
            public int CallCount { get; private set; }
            public string? LastAction { get; private set; }

            public Task<TResponse> RequestAsync<TResponse>(string action, object? payload, string? controllerId = null, string? sceneId = null, CancellationToken ct = default)
            {
                CallCount++;
                LastAction = action;
                if (ExceptionToThrow != null)
                    throw ExceptionToThrow;
                return Task.FromResult(default(TResponse)!);
            }

            public Task RequestAsync(string action, object? payload, string? controllerId = null, string? sceneId = null, CancellationToken ct = default)
            {
                CallCount++;
                LastAction = action;
                if (ExceptionToThrow != null)
                    throw ExceptionToThrow;
                return Task.CompletedTask;
            }
        }

        private sealed class FakeNatsPublisher : IVwNatsPublisher
        {
            public Task<bool> PublishAsync(string subject, object payload, CancellationToken ct = default)
            {
                return Task.FromResult(true);
            }
        }
    }
}
