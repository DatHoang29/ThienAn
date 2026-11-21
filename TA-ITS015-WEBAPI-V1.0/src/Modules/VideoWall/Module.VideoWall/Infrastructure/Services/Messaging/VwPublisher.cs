using Microsoft.Extensions.Logging;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Command;
using Module.VideoWall.Core.Interfaces;
using Modules.DataTransporter.ITS.Infrastructure.Services;

namespace Module.VideoWall.Infrastructure.Services.Messaging
{
    /// <summary>
    /// Description: Lớp phát thông điệp điều khiển Video Wall qua hạ tầng NATS (ItsDataTransporter)
    /// Created date: 10/09/2026
    /// </summary>
    public class VwPublisher(ILogger<VwPublisher> Logger) : IVwPublisher, IScoped
    {
        private readonly ILogger<VwPublisher> _logger = Logger;

        public bool PublishCommand(VwCommandEnvelope envelope)
        {
            if (envelope == null)
                return false;

            try
            {
                ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.VideoWallControl, envelope);
                _logger.LogInformation(
                    "VwPublisher: Đã publish lệnh {Action} (MsgId: {MessageId}) lên {Channel}",
                    envelope.Action,
                    envelope.MessageId,
                    ItsDataTransporterChannel.VideoWallControl);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "VwPublisher: Publish lệnh {Action} thất bại; bỏ qua (dữ liệu đã ghi DB)",
                    envelope.Action);
                return false;
            }
        }

        public bool PublishCommand<T>(string action, T payload, string? controllerId = null, string? sceneId = null)
        {
            var envelope = new VwCommandEnvelope
            {
                Action = action,
                Type = VwPackageType.Control,
                Payload = payload,
                ControllerId = controllerId,
                SceneId = sceneId
            };

            return PublishCommand(envelope);
        }

        public Task<bool> PublishCommandAsync<T>(string action, T payload, string? controllerId = null, string? sceneId = null, CancellationToken ct = default)
        {
            // ItsDataTransporter.BroadcastData là sync; wrap thành Task để interface nhất quán.
            var result = PublishCommand(action, payload, controllerId, sceneId);
            return Task.FromResult(result);
        }
    }
}
