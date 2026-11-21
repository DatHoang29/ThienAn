using Module.VideoWall.Core.Dto.Command;

namespace Module.VideoWall.Core.Interfaces
{
    /// <summary>
    /// Description: Giao diện phát thông điệp điều khiển và sự kiện Video Wall qua NATS
    /// Created date: 10/09/2026
    /// </summary>
    public interface IVwPublisher
    {
        /// <summary>Phát một gói tin lệnh chuẩn (VwCommandEnvelope) qua kênh NATS</summary>
        bool PublishCommand(VwCommandEnvelope envelope);

        /// <summary>Đóng gói và phát nhanh một lệnh điều khiển qua NATS (sync)</summary>
        bool PublishCommand<T>(string action, T payload, string? controllerId = null, string? sceneId = null);

        /// <summary>Đóng gói và phát nhanh một lệnh điều khiển qua NATS (async, fire-and-forget)</summary>
        Task<bool> PublishCommandAsync<T>(string action, T payload, string? controllerId = null, string? sceneId = null, CancellationToken ct = default);
    }
}
