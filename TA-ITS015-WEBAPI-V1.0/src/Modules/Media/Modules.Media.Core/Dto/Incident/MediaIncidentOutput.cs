namespace Modules.Media.Core.Dto.Incident;

/// <summary>
/// Description: Ảnh camera của sự cố (mỗi camera một phần tử).
/// Created date: 08/10/2026
/// </summary>
public class MediaIncidentOutput : MediaBaseOutput
{
    public string IncidentId { get; set; } = string.Empty;
    public string EquipmentId { get; set; } = string.Empty;
    public string? EquipmentName { get; set; }
    public int? KmNumber { get; set; }
    public int? MetNumber { get; set; }
    public int PictureType { get; set; }
    public DateTime CaptureTime { get; set; }
    public DateTime? ImageCapturedAt { get; set; }
    public string ImageSource { get; set; } = "cctv";
    public DateTime? WaitUntilUtc { get; set; }
}
