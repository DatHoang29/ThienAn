using System;
using System.IO;
using System.Text;
using Services.Shared.Devices;

namespace Services.Sample.DeviceB;

/// <summary>
/// Author: TAC Team
/// Description: Codec mẫu cho Device B với payload binary để minh họa protocol khác Device A.
/// Created date: 16/07/2026
/// </summary>
public sealed class DeviceBCodec : IDeviceCodec
{
    private static readonly byte[] Magic = { 0x44, 0x42, 0x30, 0x31 };

    public const string TypeCode = "device-b";
    public const string Version = "1.0";

    public string DeviceType => TypeCode;

    public string ProtocolVersion => Version;

    public CanonicalDeviceMessage Decode(DeviceFrame frame)
    {
        ValidateFrame(frame);

        try
        {
            using (var stream = new MemoryStream(frame.Payload, false))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                EnsureMagic(reader.ReadBytes(Magic.Length));
                var payloadDeviceId = reader.ReadString();
                var observedAtTicks = reader.ReadInt64();
                var value = reader.ReadDouble();
                var status = reader.ReadString();

                if (stream.Position != stream.Length)
                    throw new FormatException("Device B payload has trailing bytes.");
                if (string.IsNullOrWhiteSpace(payloadDeviceId))
                    throw new FormatException("Device B payload deviceId is required.");

                EnsureMatchingDeviceId(frame.DeviceId, payloadDeviceId);

                return new CanonicalDeviceMessage
                {
                    DeviceId = payloadDeviceId,
                    ObservedAtUtc = new DateTime(observedAtTicks, DateTimeKind.Utc),
                    Value = value,
                    Status = status,
                };
            }
        }
        catch (EndOfStreamException exception)
        {
            throw new FormatException("Device B payload is incomplete.", exception);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new FormatException("Device B timestamp is invalid.", exception);
        }
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

        var observedAtUtc = message.ObservedAtUtc.Kind == DateTimeKind.Utc
            ? message.ObservedAtUtc
            : message.ObservedAtUtc.ToUniversalTime();

        byte[] payload;
        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(Magic);
                writer.Write(deviceId);
                writer.Write(observedAtUtc.Ticks);
                writer.Write(message.Value);
                writer.Write(message.Status ?? string.Empty);
                writer.Flush();
            }

            payload = stream.ToArray();
        }

        return new DeviceFrame
        {
            DeviceType = TypeCode,
            ProtocolVersion = Version,
            DeviceId = deviceId,
            Payload = payload,
        };
    }

    private static void ValidateFrame(DeviceFrame frame)
    {
        if (frame is null)
            throw new ArgumentNullException(nameof(frame));
        if (!string.Equals(frame.DeviceType, TypeCode, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Frame is not a Device B frame.", nameof(frame));
        if (!string.Equals(frame.ProtocolVersion, Version, StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Unsupported Device B protocol version: " + frame.ProtocolVersion);
        if (frame.Payload is null || frame.Payload.Length == 0)
            throw new ArgumentException("Device B payload is required.", nameof(frame));
    }

    private static void ValidateTarget(DeviceTarget target)
    {
        if (!string.Equals(target.DeviceType, TypeCode, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Target is not Device B.", nameof(target));
        if (!string.Equals(target.ProtocolVersion, Version, StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Unsupported Device B protocol version: " + target.ProtocolVersion);
    }

    private static void EnsureMagic(byte[] actual)
    {
        if (actual.Length != Magic.Length)
            throw new FormatException("Device B payload header is incomplete.");

        for (var index = 0; index < Magic.Length; index++)
        {
            if (actual[index] != Magic[index])
                throw new FormatException("Device B payload header is invalid.");
        }
    }

    private static void EnsureMatchingDeviceId(string frameDeviceId, string payloadDeviceId)
    {
        if (!string.IsNullOrWhiteSpace(frameDeviceId)
            && !string.Equals(frameDeviceId, payloadDeviceId, StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("Device B frame and payload deviceId do not match.");
        }
    }
}
