using Services.Shared.Devices;

namespace Services.Sample.Features.Devices;

/// <summary>
/// Author: TAC Team
/// Description: Chuyển đổi frame giữa mọi codec thông qua canonical model, tránh converter theo từng cặp.
/// Created date: 16/07/2026
/// </summary>
public sealed class DeviceMessageConverter(DeviceCodecRegistry registry)
{
    public CanonicalDeviceMessage Decode(DeviceFrame source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        return registry
            .Get(source.DeviceType, source.ProtocolVersion)
            .Decode(source);
    }

    public DeviceFrame Encode(
        CanonicalDeviceMessage message,
        DeviceTarget target)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        return registry
            .Get(target.DeviceType, target.ProtocolVersion)
            .Encode(message, target);
    }

    public DeviceFrame Convert(DeviceFrame source, DeviceTarget target) =>
        Encode(Decode(source), target);
}
