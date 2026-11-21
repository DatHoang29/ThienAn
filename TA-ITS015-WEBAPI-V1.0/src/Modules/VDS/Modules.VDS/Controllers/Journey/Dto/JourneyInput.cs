using Shared.Core.Utilities;

namespace Modules.VDS.Controllers.Journey.Dto;

/// <summary>
/// Author: KienLT
/// Description: DTO đầu vào VDS, giữ nguyên contract và validation hiện hữu.
/// Created date: 08/09/2026
/// </summary>
public class JourneyInput : VdsReadInput
{
    public string? SegmentId { get; set; }
    public double? SpeedFrom { get; set; }
    public double? SpeedTo { get; set; }
    public bool? IsValid { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Khóa tra chi tiết một hành trình.
///
/// <para>DTO này KHÔNG lộ ra swagger: action <c>Detail</c> giữ tham số <c>id</c> trên query string để
/// contract HTTP không đổi, controller tự dựng DTO rồi dispatch qua message bus.</para>
/// Created date: 21/09/2026
/// </summary>
public class JourneyDetailInput : BaseIdInput { }

/// <summary>
/// Author: KienLT
/// Description: Message nội bộ cho <c>TripPage</c> (phân trang theo CHUYẾN).
/// <para>KHÔNG lộ ra swagger: action nhận <see cref="JourneyInput"/> (cùng bộ lọc + validator với <c>Page</c>) rồi bọc
/// vào đây, vì Wolverine chọn handler theo KIỂU message mà <see cref="JourneyInput"/> đã có handler trả danh sách
/// hành trình.</para>
/// Created date: 21/10/2026
/// </summary>
public class JourneyTripInput
{
    public JourneyInput Filter { get; set; } = new();
}
