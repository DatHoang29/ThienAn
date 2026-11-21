namespace Modules.Media.Core.Dto.Vms;

/// <summary>
/// Description: Ảnh VMS.
/// Created date: 08/10/2026
/// </summary>
public class MediaVmsOutput : MediaBaseOutput
{
    public string ID { get; set; } = string.Empty;
    public string Field { get; set; } = "Url";
}
