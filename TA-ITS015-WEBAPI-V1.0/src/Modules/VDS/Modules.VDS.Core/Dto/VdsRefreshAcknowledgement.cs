namespace Modules.VDS.Core.Dto;

/// <summary>
/// Author: KienLT
/// Description: Triển khai VdsRefreshAcknowledgement.
/// Created date: 02/10/2026
/// </summary>
public sealed class VdsRefreshAcknowledgement
{
    public const string Subject = "ta.its.refresh.vds.ack";
    public string RequestId { get; set; } = "";
    public string TenantId { get; set; } = "";
    public string Instance { get; set; } = "";
    public bool Applied { get; set; }
    public string[] Issues { get; set; } = Array.Empty<string>();
    public DateTime CompletedUtc { get; set; }
}
