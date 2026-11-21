using System;

namespace Services.Shared.Devices;

/// <summary>
/// Author: TAC Team
/// Description: Model trung gian độc lập vendor để chuyển đổi giữa nhiều protocol thiết bị.
/// Created date: 16/07/2026
/// </summary>
public sealed class CanonicalDeviceMessage
{
    public string DeviceId { get; set; } = string.Empty;

    public DateTime ObservedAtUtc { get; set; }

    public double Value { get; set; }

    public string Status { get; set; } = string.Empty;
}
