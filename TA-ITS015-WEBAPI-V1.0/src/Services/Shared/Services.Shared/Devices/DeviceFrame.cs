using System;

namespace Services.Shared.Devices;

/// <summary>
/// Author: TAC Team
/// Description: Frame dữ liệu portable tại boundary của một loại thiết bị.
/// Created date: 16/07/2026
/// </summary>
public sealed class DeviceFrame
{
    public string DeviceType { get; set; } = string.Empty;

    public string ProtocolVersion { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public byte[] Payload { get; set; } = Array.Empty<byte>();
}
