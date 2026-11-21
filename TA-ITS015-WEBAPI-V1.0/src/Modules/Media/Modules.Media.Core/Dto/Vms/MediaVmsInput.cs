namespace Modules.Media.Core.Dto.Vms;

/// <summary>
/// Description: Tham số chung lấy ảnh đang hiển thị của VMS (VmsCurrent).
/// Created date: 08/10/2026
/// </summary>
public class MediaVmsInput
{
    /// <summary>VmsCurrent.ID</summary>
    public string ID { get; set; } = string.Empty;

    /// <summary>Url | DefaultUrl (bỏ trống = Url).</summary>
    public string? Field { get; set; }
}
