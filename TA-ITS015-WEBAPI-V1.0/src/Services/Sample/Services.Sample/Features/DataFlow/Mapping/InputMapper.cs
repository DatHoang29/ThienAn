using System.Globalization;
using System.Text.Json;
using Services.Sample.Features.DataFlow.Messages;
using Shared.Utility.Utilities;

namespace Services.Sample.Features.DataFlow.Mapping;

/// <summary>
/// Author: TAC Team
/// Description: Chuyển payload API/demo/NATS thành ProcessDataCommand thống nhất (Wolverine command).
/// Created date: 16/07/2026
/// </summary>
public sealed class InputMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProcessDataCommand FromNats(string json)
    {
        var envelope = TryDeserializeEnvelope(json);
        return envelope ?? Create("nats", json);
    }

    public ProcessDataCommand FromApi(string json) => Create("api", json);

    private static ProcessDataCommand? TryDeserializeEnvelope(string json)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<ProcessDataCommand>(json, JsonOptions);
            return IsValid(envelope) ? envelope : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private ProcessDataCommand Create(string source, string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        return new ProcessDataCommand(
            source,
            ResolveMessageId(root),
            ResolveOccurredAtUtc(root),
            json);
    }

    private static bool IsValid(ProcessDataCommand? message) =>
        message is not null
        && !string.IsNullOrWhiteSpace(message.Source)
        && !string.IsNullOrWhiteSpace(message.MessageId)
        && message.OccurredAtUtc != default
        && !string.IsNullOrWhiteSpace(message.PayloadJson);

    private static string ResolveMessageId(JsonElement root)
    {
        foreach (var propertyName in new[] { "id", "messageId", "externalId" })
        {
            if (TryGetPropertyIgnoreCase(root, propertyName, out var property)
                && property.ValueKind is not JsonValueKind.Null
                && property.ValueKind is not JsonValueKind.Undefined)
            {
                var value = property.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        return Guid.NewGuid().ToString("N");
    }

    private DateTime ResolveOccurredAtUtc(JsonElement root)
    {
        foreach (var propertyName in new[] { "occurredAtUtc", "timestamp", "time" })
        {
            if (!TryGetPropertyIgnoreCase(root, propertyName, out var property))
                continue;

            if (property.ValueKind == JsonValueKind.String
                && DateTime.TryParse(
                    property.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed))
            {
                return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }

            if (property.ValueKind == JsonValueKind.Number
                && property.TryGetInt64(out var unixSeconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
            }
        }

        return BaseUtil.TimeServer.GetTimeServer().ToUniversalTime();
    }

    private static bool TryGetPropertyIgnoreCase(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}
