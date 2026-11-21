namespace Modules.TMS.Core.Constants;

/// <summary>Giá trị snapshot nội bộ, không phải mã số của danh mục event_group.</summary>
public static class IncidentEventGroupConst
{
    public static string? Normalize(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "event" => Event,
        "incident" => Incident,
        _ => null,
    };
    // NULL snapshots belong to legacy incidents. Empty/unknown snapshots fail closed.
    public static bool AllowsProcessing(string? group, string? state) =>
        !string.Equals(state?.Trim(), IncidentRecordingConst.LegacyState, StringComparison.OrdinalIgnoreCase)
        && state != IncidentRecordingConst.CanceledState
        && (group is null || Normalize(group) is Incident or Event);


    public const string Event = "event";
    public const string Incident = "incident";
}
