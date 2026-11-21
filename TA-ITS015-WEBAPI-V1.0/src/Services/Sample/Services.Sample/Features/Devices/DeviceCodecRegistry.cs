using Services.Sample.Configuration;
using Services.Shared.Devices;
using Services.Shared.Logging;

namespace Services.Sample.Features.Devices;

/// <summary>
/// Author: TAC Team
/// Description: Immutable registry cho built-in và plugin codec theo device type/protocol version.
/// Created date: 16/07/2026
/// </summary>
public sealed class DeviceCodecRegistry
{
    private const char KeySeparator = '\u001f';
    private readonly IReadOnlyDictionary<string, IDeviceCodec> _codecs;

    public DeviceCodecRegistry(
        IEnumerable<IDeviceCodec> builtInCodecs,
        DevicePluginLoader pluginLoader,
        IConfigManager configService)
    {
        if (builtInCodecs is null)
            throw new ArgumentNullException(nameof(builtInCodecs));

        var allCodecs = builtInCodecs
            .Concat(pluginLoader.Load(configService.Current.Devices.PluginDirectory))
            .ToArray();
        var codecs = new Dictionary<string, IDeviceCodec>(StringComparer.Ordinal);

        foreach (var codec in allCodecs)
        {
            if (codec is null)
                throw new InvalidOperationException("A null device codec was registered.");

            var key = CreateKey(codec.DeviceType, codec.ProtocolVersion);
            if (codecs.TryGetValue(key, out var existing))
            {
                throw new InvalidOperationException(
                    $"Duplicate device codec '{codec.DeviceType}/{codec.ProtocolVersion}': "
                    + $"'{existing.GetType().FullName}' and '{codec.GetType().FullName}'.");
            }

            codecs.Add(key, codec);
        }

        if (codecs.Count == 0)
            throw new InvalidOperationException("At least one device codec must be registered.");

        _codecs = codecs;
        LogManager.Info(
            $"Device codec registry initialized with {codecs.Count} codec(s): "
            + string.Join(", ", allCodecs.Select(item => $"{item.DeviceType}/{item.ProtocolVersion}")),
            kind: LogKind.Device);
    }

    public IReadOnlyCollection<IDeviceCodec> Codecs => _codecs.Values.ToArray();

    public IDeviceCodec Get(string deviceType, string protocolVersion)
    {
        var key = CreateKey(deviceType, protocolVersion);
        if (_codecs.TryGetValue(key, out var codec))
            return codec;

        throw new KeyNotFoundException(
            $"No device codec is registered for '{deviceType}/{protocolVersion}'.");
    }

    public bool TryGet(
        string deviceType,
        string protocolVersion,
        out IDeviceCodec? codec) =>
        _codecs.TryGetValue(CreateKey(deviceType, protocolVersion), out codec);

    private static string CreateKey(string deviceType, string protocolVersion)
    {
        if (string.IsNullOrWhiteSpace(deviceType))
            throw new ArgumentException("Device type is required.", nameof(deviceType));
        if (string.IsNullOrWhiteSpace(protocolVersion))
            throw new ArgumentException("Protocol version is required.", nameof(protocolVersion));

        return string.Concat(
            deviceType.Trim().ToUpperInvariant(),
            KeySeparator,
            protocolVersion.Trim().ToUpperInvariant());
    }
}
