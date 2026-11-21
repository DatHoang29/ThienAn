using Module.VideoWall.Core.Interfaces;
using Services.Shared.Messaging;

namespace ITS.VideoWall.Messaging.Publishers
{
    /// <summary>
    /// Description: Triển khai IVwNatsPublisher cho Worker host sử dụng TransportManager (chính sách mỏng: bỏ qua êm khi chưa kết nối)
    /// Created date: 11/09/2026
    /// </summary>
    public class VwTransportPublisher(
        TransportManager Transport,
        ILogger<VwTransportPublisher> Logger) : IVwNatsPublisher
    {
        private volatile bool _warnedNotConnected;

        public async Task<bool> PublishAsync(string subject, object payload, CancellationToken ct = default)
        {
            if (payload == null || string.IsNullOrWhiteSpace(subject))
                return false;

            if (!Transport.IsConnected)
            {
                if (!_warnedNotConnected)
                {
                    _warnedNotConnected = true;
                    Logger.LogWarning("[VwTransportPublisher] Chưa kết nối NATS — bỏ qua mọi publish cho tới khi kết nối được.");
                }
                Logger.LogDebug("[VwTransportPublisher:skip] {Subject}", subject);
                return false;
            }

            try
            {
                var ok = await Transport.PublishAsync(subject, payload, ct).ConfigureAwait(false);
                if (ok)
                    Logger.LogInformation("[VwTransportPublisher:Publish] {Subject}", subject);
                else
                    Logger.LogWarning("[VwTransportPublisher:Publish] {Subject} trả về false.", subject);
                return ok;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogError(ex, "[VwTransportPublisher:Publish] {Subject} thất bại; bỏ qua.", subject);
                return false;
            }
        }
    }
}
