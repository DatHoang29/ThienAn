using Module.VideoWall.Core.Interfaces;
using Modules.DataTransporter.ITS.Infrastructure.Services;

namespace Module.VideoWall.Infrastructure.Services.Messaging
{
    /// <summary>
    /// Description: Triển khai IVwNatsPublisher cho WebAPI host sử dụng ItsDataTransporter
    /// Created date: 11/09/2026
    /// </summary>
    public class VwItsNatsPublisher : IVwNatsPublisher
    {
        public Task<bool> PublishAsync(string subject, object payload, CancellationToken ct = default)
        {
            if (payload == null || string.IsNullOrWhiteSpace(subject))
                return Task.FromResult(false);

            ItsDataTransporter.BroadcastData(subject, payload);
            return Task.FromResult(true);
        }
    }
}
