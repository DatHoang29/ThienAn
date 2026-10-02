using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Module.VideoWall.Controllers.Scene.Commands;
using Module.VideoWall.Controllers.Scene.Validators;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Command;
using Module.VideoWall.Core.Dto.Scene;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Access;
using System.Diagnostics;

namespace Tests.VideoWall.WebApi.Controllers
{
    /// <summary>
    /// Description: Bộ kiểm thử Fire-and-Forget cho kích hoạt kịch bản:
    ///              BE chỉ validate và phát lệnh qua IVwPublisher (NATS), KHÔNG chờ thiết bị thực thi,
    ///              KHÔNG tự động cập nhật ActiveScene trong DB.
    /// Created date: 13/09/2026 — Cập nhật bỏ fake publisher: 29/09/2026
    /// </summary>
    [Collection("api")]
    public class VwSceneAckBeforeCommitTests(Host host)
    {
        private const string TestPrefix = "TEST_VWACK_";
        private readonly ISqlSugarClient _db = host.Services.GetRequiredService<ISqlSugarClient>();
        private readonly IVwEventTriggerLogService _logService = host.Services.GetRequiredService<IVwEventTriggerLogService>();

        /// <summary>
        /// Description: Kích hoạt kịch bản theo cơ chế Fire-and-Forget: validate thành công -> publish lệnh qua IVwPublisher
        ///              -> trả output ngay lập tức, Worker Consumer nhận qua NATS và thực thi xuống thiết bị.
        /// Created date: 13/09/2026
        /// </summary>
        [Fact]
        public async Task ActivateScene_FireAndForget_PublishesCommandAndReturnsOutputImmediately_Test()
        {
            var ctrlCode = $"{TestPrefix}CTRL_{Guid.NewGuid():N}";
            var sceneCode = $"{TestPrefix}SCN_{Guid.NewGuid():N}";

            var controller = new VwController
            {
                Code = ctrlCode,
                Name = "Center Controller",
                IntegrationMode = "cascade",
                IP = $"127.0.0.1:{VwISAPIServerHikvisionMock.DefaultPort}",
                Account = VwISAPIServerHikvisionMock.DefaultUser,
                PassWord = VwISAPIServerHikvisionMock.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(controller).ExecuteCommandAsync();

            var scene = new VwScene
            {
                Code = sceneCode,
                Name = "Scene Fire-and-Forget",
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Enable,
                ActiveScene = BaseEnums.ActiveScene.DeActivate,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            // 1. Kiểm tra Validator bắt buộc
            var validator = new VwActiveSceneValidator(host.Localizer);
            var invalidResult = await validator.ValidateAsync(new VwActiveSceneInput());
            Assert.False(invalidResult.IsValid);

            var validResult = await validator.ValidateAsync(new VwActiveSceneInput { Code = sceneCode });
            Assert.True(validResult.IsValid);

            // 2. Thực thi CommandHandler với IVwPublisher THẬT — lệnh đi qua NATS thật xuống VwCommandConsumer
            using var scope = host.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var initialActivateCount = host.MockServer.ActivateSceneCallCount;

            var handler = new VwSceneWorkflowCommandHandler(
                sp.GetRequiredService<BaseRepository<VwScene>>(),
                sp.GetRequiredService<BaseRepository<VwWindowScene>>(),
                sp.GetRequiredService<BaseRepository<VwController>>(),
                sp.GetRequiredService<BaseRepository<VwEventRule>>(),
                sp.GetRequiredService<VwPermissionService>(),
                sp.GetRequiredService<IVwPublisher>(),
                _logService);

            var output = await handler.HandleAsync(new VwActiveSceneInput { Code = sceneCode });

            // Assert: Output trả về ngay
            Assert.NotNull(output);
            Assert.Equal(scene.ID, output.ID);
            Assert.NotNull(output.TriggerLogId);

            // Assert: Worker đã thật sự nhận lệnh qua NATS và chạm tới thiết bị
            var timeout = TimeSpan.FromSeconds(10);
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (host.MockServer.ActivateSceneCallCount > initialActivateCount)
                    break;

                await Task.Delay(100);
            }

            Assert.True(host.MockServer.ActivateSceneCallCount > initialActivateCount,
                "Consumer chưa nhận được lệnh ActivateScene qua NATS hoặc chưa gọi xuống thiết bị.");

            // Assert: Log trigger được ghi nhận trạng thái Success
            var log = await _db.Queryable<VwEventTriggerLog>().FirstAsync(u => u.ID == output.TriggerLogId);
            Assert.NotNull(log);
            Assert.Equal(BaseEnums.SuccessEnums.Success, log.Success);
        }

        /// <summary>
        /// Description: Kích hoạt kịch bản khi Status = Disable -> ném lỗi và ghi nhận log thất bại
        /// Created date: 13/09/2026
        /// </summary>
        [Fact]
        public async Task ActivateScene_WhenSceneDisabled_LogsFailAndThrows_Test()
        {
            host.MockServer.ResetDefaults();

            var ctrlCode = $"{TestPrefix}CTRL_{Guid.NewGuid():N}";
            var sceneCode = $"{TestPrefix}SCN_{Guid.NewGuid():N}";

            var controller = new VwController
            {
                Code = ctrlCode,
                Name = "Center Controller",
                IntegrationMode = "cascade",
                IP = $"127.0.0.1:{VwISAPIServerHikvisionMock.DefaultPort}",
                Account = VwISAPIServerHikvisionMock.DefaultUser,
                PassWord = VwISAPIServerHikvisionMock.DefaultPassword,
                Status = BaseEnums.StatusEnum.Enable,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(controller).ExecuteCommandAsync();

            var scene = new VwScene
            {
                Code = sceneCode,
                Name = "Scene Disabled",
                OutputId = "1",
                Status = BaseEnums.StatusEnum.Disable,
                ActiveScene = BaseEnums.ActiveScene.DeActivate,
                CreateTime = DateTime.Now
            };
            await _db.Insertable(scene).ExecuteCommandAsync();

            using var scope = host.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var initialActivateCount = host.MockServer.ActivateSceneCallCount;

            var handler = new VwSceneWorkflowCommandHandler(
                sp.GetRequiredService<BaseRepository<VwScene>>(),
                sp.GetRequiredService<BaseRepository<VwWindowScene>>(),
                sp.GetRequiredService<BaseRepository<VwController>>(),
                sp.GetRequiredService<BaseRepository<VwEventRule>>(),
                sp.GetRequiredService<VwPermissionService>(),
                sp.GetRequiredService<IVwPublisher>(),
                _logService);

            await Assert.ThrowsAnyAsync<Exception>(() =>
                handler.HandleAsync(new VwActiveSceneInput { Code = sceneCode }));

            // Không có lệnh nào chạm tới thiết bị — kiểm từ phía MockServer thay vì đếm lời gọi hàm giả
            await Task.Delay(500);
            Assert.Equal(initialActivateCount, host.MockServer.ActivateSceneCallCount);

            // Log thất bại được ghi
            var log = await _db.Queryable<VwEventTriggerLog>().FirstAsync(u => u.TargetSceneId == scene.ID);
            Assert.NotNull(log);
            Assert.Equal(BaseEnums.SuccessEnums.Fail, log.Success);
        }
    }
}
