namespace Modules.VDS.Core.Dto;

/// <summary>
/// Author: KienLT
/// Description: One successful automation transition's frozen image choice, kept outside wwwroot.
/// Created date: 02/10/2026
/// </summary>
public sealed record VdsAutomationImageDecisionOutput(string TenantId, string IncidentId, string RunId,
    int PictureType, DateTime CaptureTime, DateTime RegistrationTime, string ImageSource)
{
    public VdsRegistrationImageSourceOutput? Source { get; init; }
    public string Status { get; init; } = "Selected";
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public string? Reason { get; init; }

    public VdsAutomationImageDecisionOutput UseCamera(string reason) => this with
    {
        ImageSource = "camera", Source = null, FileName = null, ContentType = null,
        Status = "Selected", Reason = reason
    };

    public bool Matches(string? tenant, string incident, int pictureType, DateTime captureTime) =>
        !string.IsNullOrWhiteSpace(TenantId) && TenantId == tenant && IncidentId == incident
        && PictureType == pictureType && pictureType is 2 or 3 && CaptureTime == captureTime
        && !string.IsNullOrWhiteSpace(RunId) && ImageSource is "camera" or "vds";
}
