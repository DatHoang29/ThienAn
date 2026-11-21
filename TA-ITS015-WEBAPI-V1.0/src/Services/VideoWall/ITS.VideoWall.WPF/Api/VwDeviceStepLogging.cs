using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Module.VideoWall.WPF.Api.Dto;
using Services.Shared.Events;
using Shared.Utility.Utilities;

namespace Module.VideoWall.WPF.Api;

/// <summary>
/// Description: Bộ tiện ích ghi nhận và phát tán log các bước (steps) thực thi tác vụ thiết bị VideoWall
/// Created date: 13/09/2026
/// </summary>
public static class VwDeviceStepLogging
{
    public static async Task PublishSteps(
        IPublisher? publisher,
        ActivityPublisher activityPublisher,
        string action,
        IEnumerable<VwSetupSceneStep>? steps,
        CancellationToken cancellationToken)
    {
        if (publisher is null || steps is null)
            return;

        foreach (var step in steps.OrderBy(s => s.Order))
            await PublishStep(publisher, activityPublisher, action, step, cancellationToken).ConfigureAwait(false);
    }

    private static async Task PublishStep(
        IPublisher publisher,
        ActivityPublisher activityPublisher,
        string action,
        VwSetupSceneStep step,
        CancellationToken cancellationToken)
    {
        var level = step.Skipped
            ? ActivityLevel.Warning
            : step.Success ? ActivityLevel.Success : ActivityLevel.Error;

        var detail = $"{action} · {step.Name} · {step.Outcome}"
            + (step.HttpStatus is null ? string.Empty : $" · HTTP {step.HttpStatus}")
            + (string.IsNullOrWhiteSpace(step.Message) ? string.Empty : $" · {step.Message.Trim()}")
            + $" ({step.DurationMs} ms)";

        var activity = new Activity(
            BaseUtil.TimeServer.GetTimeServer(),
            "Device",
            detail,
            level);

        try
        {
            await publisher.Publish(new DeviceStepNotification(activity, step), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            activityPublisher.Publish("Device", $"Không ghi được log bước {step.Name}: {exception.Message}", ActivityLevel.Warning);
        }
    }
}
