namespace Services.Shared.Devices;

/// <summary>
/// Author: TAC Team
/// Description: Contract portable được các DLL thiết bị implement để chuyển đổi frame và canonical message.
/// Created date: 16/07/2026
/// </summary>
public interface IDeviceCodec
{
    string DeviceType { get; }

    string ProtocolVersion { get; }

    CanonicalDeviceMessage Decode(DeviceFrame frame);

    DeviceFrame Encode(CanonicalDeviceMessage message, DeviceTarget target);
}
