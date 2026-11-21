namespace Services.Shared.Devices;

/// <summary>
/// Author: TAC Team
/// Description: Đích cần encode khi chuyển canonical message sang protocol thiết bị.
/// Created date: 16/07/2026
/// </summary>
public sealed class DeviceTarget
{
    public string DeviceType { get; set; } = string.Empty;

    public string ProtocolVersion { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;
}
