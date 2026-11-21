using Modules.Media.Core.Dto.EventType;

namespace Modules.Media.Controllers.MediaPublic.EventType.Dto;

/// <summary>
/// Description: [WP] Tham số EventType của MediaPublic.
/// Created date: 08/10/2026
/// </summary>
public class MediaPublicEventTypeInput : MediaEventTypeInput
{
    /// <summary>Phiên bản FE truyền (updateTime) chỉ để phá cache trình duyệt, BE không dùng.</summary>
    public string? V { get; set; }
}
