using Modules.VDS.Core.Dto;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Modules.TMS.Infrastructure.Services.Incident.Snapshot;

/// <summary>
/// Hàng đợi trong bộ nhớ dùng để tách việc chụp ảnh camera khỏi request cập nhật sự cố.
/// </summary>
public sealed class IncidentSnapshotQueue
{
    private readonly Channel<IncidentSnapshotRequest> _channel;
    private readonly ILogger<IncidentSnapshotQueue> _logger;

    public IncidentSnapshotQueue(ILogger<IncidentSnapshotQueue> logger)
    {
        _logger = logger;
        _channel = Channel.CreateUnbounded<IncidentSnapshotRequest>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    }

    /// <summary>
    /// Ghi nhanh yêu cầu chụp vào queue. Không giữ request nghiệp vụ để chờ CCTV/RTSP.
    /// </summary>
    public bool Enqueue(string incidentId, int state, VdsAutomationImageDecisionOutput? automationImage = null)
    {
        if (string.IsNullOrWhiteSpace(incidentId))
        {
            _logger.LogWarning(
                "[IncidentSnapshotQueue][REJECTED] Không đưa yêu cầu chụp vào queue vì IncidentId trống; state {State}.",
                state);
            return false;
        }

        var request = new IncidentSnapshotRequest(incidentId, state, DateTimeOffset.UtcNow, automationImage);
        var queued = _channel.Writer.TryWrite(request);

        if (queued)
        {
            _logger.LogInformation(
                "[IncidentSnapshotQueue][QUEUED] Đã xếp hàng chụp ảnh sự cố {IncidentId}, state {State}, lúc {EnqueuedAt:O}.",
                request.IncidentId,
                request.State,
                request.EnqueuedAt);
        }
        else
        {
            _logger.LogError(
                "[IncidentSnapshotQueue][QUEUE_FAILED] Không thể xếp hàng chụp ảnh sự cố {IncidentId}, state {State}.",
                request.IncidentId,
                request.State);
        }

        return queued;
    }

    internal IAsyncEnumerable<IncidentSnapshotRequest> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
