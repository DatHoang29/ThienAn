using Modules.Media.Core.Dto.Vms;

namespace Modules.Media.Controllers.MediaPublic.Vms.Dto;

/// <summary>
/// Description: [WP] Tham số Vms của MediaPublic.
/// Created date: 08/10/2026
/// </summary>
public class MediaPublicVmsInput : MediaVmsInput
{
    /// <summary>Phiên bản FE truyền (updateTime) chỉ để phá cache trình duyệt, BE không dùng.</summary>
    public string? V { get; set; }
}
