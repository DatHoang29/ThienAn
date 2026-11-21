using Modules.TOLL.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.WP.Controllers.Toll.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Các Input DTO cho tính năng trạm thu phí hệ thống WP (Toll)
/// Created date: 03/07/2026
/// Update: 07/10/2026 - Dùng trực tiếp entity TollStation (đồng bộ từ ITS10 qua WP_Sync), bỏ bảng WpToll.
///     Mã trạm = StationId (mã ETC, TollRoad.StationIdIn/Out tham chiếu mã này),
///     RefId = TmsWorkUnit.ID (đơn vị TMS loại toll = điểm trạm trên bản đồ).
/// </summary>
public class WpPageTollInput : BasePageInput
{
    /// <summary>Mã trạm ETC (chứa)</summary>
    public string? StationId { get; set; }

    /// <summary>Tên trạm (chứa)</summary>
    public string? Name { get; set; }

    /// <summary>ID đơn vị TMS đã gán</summary>
    public string? RefId { get; set; }

    /// <summary>Kích hoạt (1) / vô hiệu (0)</summary>
    public int? Active { get; set; }
}

/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Toll
/// Created date: 14/08/2026
/// </summary>
public class WpTollInput
{
    public string? StationId { get; set; }
    public string? Name { get; set; }
    public string? RefId { get; set; }
    public int? Active { get; set; }
}

/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Add Toll
/// Created date: 14/08/2026
/// </summary>
public class WpAddTollInput : TollStation
{
}

/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Update Toll
/// Created date: 14/08/2026
/// </summary>
public class WpUpdateTollInput : WpAddTollInput
{
}

/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Delete Toll
/// Created date: 14/08/2026
/// </summary>
public class WpDeleteTollInput : BaseIdInput
{
}
