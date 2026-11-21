using System;
using System.Globalization;
using System.Text;
using Services.Shared.Devices;

namespace Services.Sample.DeviceA;

/// <summary>
/// Author: TAC Team
/// Description: Codec mẫu cho Device A với payload UTF-8 dạng deviceId|timestamp|value|status.
/// Created date: 16/07/2026
/// </summary>
public sealed class DeviceACodec : IDeviceCodec
{
    public const string TypeCode = "device-a";
    public const string Version = "1.0";

    public string DeviceType => TypeCode;

    public string ProtocolVersion => Version;

    public CanonicalDeviceMessage Decode(DeviceFrame frame)
    {
        ValidateFrame(frame);

        var payload = Encoding.UTF8.GetString(frame.Payload);
        var fields = payload.Split(new[] { '|' }, 4);
        if (fields.Length != 4)
            throw new FormatException("Device A payload must contain deviceId, timestamp, value and status.");

        if (string.IsNullOrWhiteSpace(fields[0]))
            throw new FormatException("Device A payload deviceId is required.");

        if (!DateTime.TryParseExact(
                fields[1],
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var observedAt))
        {
            throw new FormatException("Device A timestamp must use the round-trip ISO-8601 format.");
        }

        if (!double.TryParse(
                fields[2],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value))
        {
            throw new FormatException("Device A value is invalid.");
        }

        EnsureMatchingDeviceId(frame.DeviceId, fields[0]);

        return new CanonicalDeviceMessage
        {
            DeviceId = fields[0],
            ObservedAtUtc = observedAt.Kind == DateTimeKind.Utc
                ? observedAt
                : observedAt.ToUniversalTime(),
            Value = value,
            Status = fields[3],
        };
    }

    public DeviceFrame Encode(CanonicalDeviceMessage message, DeviceTarget target)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        ValidateTarget(target);
        var deviceId = string.IsNullOrWhiteSpace(target.DeviceId)
            ? message.DeviceId
            : target.DeviceId;
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("A source or target deviceId is required.", nameof(target));
        if (deviceId.IndexOf('|') >= 0)
            throw new ArgumentException("Device A deviceId cannot contain the '|' delimiter.", nameof(target));

        var observedAtUtc = message.ObservedAtUtc.Kind == DateTimeKind.Utc
            ? message.ObservedAtUtc
            : message.ObservedAtUtc.ToUniversalTime();
        var payload = string.Join(
            "|",
            deviceId,
            observedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            message.Value.ToString("R", CultureInfo.InvariantCulture),
            message.Status ?? string.Empty);

        return new DeviceFrame
        {
            DeviceType = TypeCode,
            ProtocolVersion = Version,
            DeviceId = deviceId,
            Payload = Encoding.UTF8.GetBytes(payload),
        };
    }

    private static void ValidateFrame(DeviceFrame frame)
    {
        if (frame is null)
            throw new ArgumentNullException(nameof(frame));
        if (!string.Equals(frame.DeviceType, TypeCode, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Frame is not a Device A frame.", nameof(frame));
        if (!string.Equals(frame.ProtocolVersion, Version, StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Unsupported Device A protocol version: " + frame.ProtocolVersion);
        if (frame.Payload is null || frame.Payload.Length == 0)
            throw new ArgumentException("Device A payload is required.", nameof(frame));
    }

    private static void ValidateTarget(DeviceTarget target)
    {
        if (!string.Equals(target.DeviceType, TypeCode, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Target is not Device A.", nameof(target));
        if (!string.Equals(target.ProtocolVersion, Version, StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Unsupported Device A protocol version: " + target.ProtocolVersion);
    }

    private static void EnsureMatchingDeviceId(string frameDeviceId, string payloadDeviceId)
    {
        if (!string.IsNullOrWhiteSpace(frameDeviceId)
            && !string.Equals(frameDeviceId, payloadDeviceId, StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("Device A frame and payload deviceId do not match.");
        }
    }
}
