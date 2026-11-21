using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Command;
using Module.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Interfaces;
using Wolverine;

namespace Module.VideoWall.Controllers.DeviceSetup.Commands
{
    /// <summary>
    /// Description: Điều phối các lệnh của kịch bản đấu nối thiết bị VideoWall.
    ///              Fire-and-forget: publish lệnh xuống Worker qua NATS, trả về {Accepted, MessageId} ngay.
    ///              Worker tự thực thi ISAPI, ghi DB, và phát Status lên NATS cho FE (cùng MessageId ⇒ FE ghép kết quả).
    /// Created date: 11/09/2026 — cập nhật 07/11/2026 (MessageId trả về = MessageId của lệnh; Accepted = publish thành công)
    /// </summary>
    public class VwDeviceSetupCommandHandler(
        IVwPublisher publisher) : IWolverineHandler
    {
        public Task<VwDeviceSetupAcceptedOutput> HandleAsync(VwPingDeviceInput command, CancellationToken ct)
            => Task.FromResult(Send(VwCommandActions.DeviceSetupPing, command, command.ID));

        /// <summary>Worker trả VwDeviceGenericOutput&lt;VwISAPIInputChannelsResponse&gt; (danh sách cổng vào vật lý).</summary>
        public Task<VwDeviceSetupAcceptedOutput> HandleAsync(VwInputChannelsInput command, CancellationToken ct)
            => Task.FromResult(Send(VwCommandActions.DeviceProxyInputChannels, command, command.ID));

        public Task<VwDeviceSetupAcceptedOutput> HandleAsync(VwProbeDeviceInput command, CancellationToken ct)
            => Task.FromResult(Send(VwCommandActions.DeviceSetupProbe, command, command.ID));

        public Task<VwDeviceSetupAcceptedOutput> HandleAsync(VwSyncSourcesInput command, CancellationToken ct)
            => Task.FromResult(Send(VwCommandActions.DeviceSetupSyncSources, command, command.ID));

        /// <summary>ID trống ⇒ Worker đồng bộ lần lượt mọi bộ điều khiển rồi tính lại vị trí cổng ra một lần.</summary>
        public Task<VwDeviceSetupAcceptedOutput> HandleAsync(VwSyncOutputsInput command, CancellationToken ct)
            => Task.FromResult(Send(VwCommandActions.DeviceSetupSyncOutputs, command, string.IsNullOrWhiteSpace(command.ID) ? null : command.ID));

        /// <summary>ID trống ⇒ Worker đồng bộ màn của mọi bộ con (cần đã có cổng ra).</summary>
        public Task<VwDeviceSetupAcceptedOutput> HandleAsync(VwSyncScreensInput command, CancellationToken ct)
            => Task.FromResult(Send(VwCommandActions.DeviceSetupSyncScreens, command, string.IsNullOrWhiteSpace(command.ID) ? null : command.ID));

        /// <summary>Ghi bảng cổng ra → vị trí xuống bộ con (DryRun ⇒ chỉ so sánh). Worker tự đọc Bố cục gán cổng từ DB.</summary>
        public Task<VwDeviceSetupAcceptedOutput> HandleAsync(VwApplyWallOutputsInput command, CancellationToken ct)
            => Task.FromResult(Send(VwCommandActions.DeviceSetupApplyWallOutputs, command, null));

        /// <summary>Bật / tắt toàn bộ màn của bộ hiển thị (ControllerIds trống ⇒ toàn tường).</summary>
        public Task<VwDeviceSetupAcceptedOutput> HandleAsync(VwScreenPowerInput command, CancellationToken ct)
            => Task.FromResult(Send(VwCommandActions.DeviceSetupScreenPower, command, null));

        /// <summary>Đặt độ sáng (đèn nền 0-100) cho từng màn (ScreenIds).</summary>
        public Task<VwDeviceSetupAcceptedOutput> HandleAsync(VwScreenBacklightInput command, CancellationToken ct)
            => Task.FromResult(Send(VwCommandActions.DeviceSetupScreenBacklight, command, null));

        /// <summary>Đọc trạng thái 1 màn (bật/tắt, độ sáng). ID = VwScreen.ID.</summary>
        public Task<VwDeviceSetupAcceptedOutput> HandleAsync(VwScreenStatusInput command, CancellationToken ct)
            => Task.FromResult(Send(VwCommandActions.DeviceSetupScreenStatus, command, null));

        public Task<VwDeviceSetupAcceptedOutput> HandleAsync(VwSyncActiveSceneInput command, CancellationToken ct)
            => Task.FromResult(Send(VwCommandActions.DeviceSetupSyncActiveScene, command, command.ID));

        public Task<VwDeviceSetupAcceptedOutput> HandleAsync(VwSetupSceneInput command, CancellationToken ct)
            => Task.FromResult(Send(VwCommandActions.DeviceSetupScene, command, command.ControllerId, command.SceneId));

        public Task<VwDeviceSetupAcceptedOutput> HandleAsync(VwISAPIPassthroughInput command, CancellationToken ct)
            => Task.FromResult(Send(VwCommandActions.DeviceSetupSendPassthrough, command, command.ControllerId));

        public async Task HandleAsync(VwResetDeviceAuthFailureInput command, CancellationToken ct = default)
        {
            await publisher.PublishCommandAsync(VwCommandActions.DeviceSetupResetDeviceAuthFailure, command, controllerId: command.ControllerId, ct: ct);
        }

        /// <summary>Dựng lệnh với MessageId biết trước ⇒ trả đúng MessageId cho FE; Accepted = false khi publish NATS lỗi.</summary>
        private VwDeviceSetupAcceptedOutput Send<T>(string action, T payload, string? controllerId, string? sceneId = null)
        {
            var envelope = new VwCommandEnvelope
            {
                Action = action,
                Type = VwPackageType.Control,
                Payload = payload,
                ControllerId = controllerId,
                SceneId = sceneId,
            };
            var accepted = publisher.PublishCommand(envelope);
            return new VwDeviceSetupAcceptedOutput { Accepted = accepted, MessageId = envelope.MessageId };
        }
    }
}
