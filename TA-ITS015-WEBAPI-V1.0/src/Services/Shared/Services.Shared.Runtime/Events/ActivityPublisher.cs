using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Utility.Utilities;

namespace Services.Shared.Events;

/// <summary>
/// Author: TAC Team
/// Description: Helper phát ActivityNotification qua MediatR. Giữ chữ ký Publish(stage, detail, level)
/// quen thuộc; mọi consumer subscribe qua INotificationHandler. Best-effort: handler lỗi không làm hỏng pipeline.
/// Created date: 17/07/2026
/// </summary>
public sealed class ActivityPublisher(
    IPublisher publisher,
    ILogger<ActivityPublisher> logger)
{
    public void Publish(
        string stage,
        string detail,
        ActivityLevel level = ActivityLevel.Info)
    {
        if (string.IsNullOrWhiteSpace(stage))
            throw new ArgumentException("Stage is required.", nameof(stage));

        Publish(new Activity(BaseUtil.TimeServer.GetTimeServer(), stage, detail ?? string.Empty, level));
    }

    public void Publish(Activity activity)
    {
        if (activity is null)
            throw new ArgumentNullException(nameof(activity));

        // Fire-and-forget best-effort: không block pipeline chính khi publish INotification.
        _ = PublishAsync(activity);
    }

    private async Task PublishAsync(Activity activity)
    {
        try
        {
            await publisher
                .Publish(new ActivityNotification(activity))
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "An activity subscriber threw while handling stage {Stage}",
                activity.Stage);
        }
    }
}
