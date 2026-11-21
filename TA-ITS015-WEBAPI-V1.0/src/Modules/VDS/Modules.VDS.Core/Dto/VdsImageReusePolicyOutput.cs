using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Modules.VDS.Core.Dto;

/// <summary>
/// Author: KienLT
/// Description: Image policy stored inside the existing automation config snapshot, never an entity column.
/// Created date: 02/10/2026
/// </summary>
public sealed record VdsImageReusePolicyOutput(string TenantId, string EventTypeId, int MaxAgeSeconds)
{
    public const string SnapshotKey = "VdsImageReusePolicyOutput";
    public int Version { get; init; } = 1;

    public static int? ParseSeconds(string? value) =>
        int.TryParse(value?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
            ? seconds : null;

    public static string Freeze(string configJson, VdsImageReusePolicyOutput policy)
    {
        var root = JsonNode.Parse(configJson) as JsonObject
            ?? throw new JsonException("Automation config snapshot must be an object.");
        root[SnapshotKey] = JsonSerializer.SerializeToNode(policy);
        return root.ToJsonString();
    }

    public static VdsImageReusePolicyOutput? Read(string? snapshot, string? tenant, string? eventType)
    {
        if (string.IsNullOrWhiteSpace(snapshot) || string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(eventType)) return null;
        try
        {
            var policy = (JsonNode.Parse(snapshot) as JsonObject)?[SnapshotKey]?.Deserialize<VdsImageReusePolicyOutput>();
            return policy is { Version: 1, MaxAgeSeconds: > 0 } && policy.TenantId == tenant && policy.EventTypeId == eventType
                ? policy : null;
        }
        catch (JsonException) { return null; }
        catch (InvalidOperationException) { return null; }
    }

    public bool CanReuse(bool automation, string? source, int pictureType, string? tenant, string? eventType,
        DateTime registrationUtc, DateTime stageUtc)
    {
        var age = stageUtc - registrationUtc;
        return automation && source?.Trim().Equals("vds", StringComparison.OrdinalIgnoreCase) == true
            && pictureType is 2 or 3 && Version == 1 && MaxAgeSeconds > 0
            && !string.IsNullOrWhiteSpace(tenant) && TenantId == tenant && EventTypeId == eventType
            && registrationUtc != default && stageUtc != default
            && age >= TimeSpan.Zero && age < TimeSpan.FromSeconds(MaxAgeSeconds);
    }
}
