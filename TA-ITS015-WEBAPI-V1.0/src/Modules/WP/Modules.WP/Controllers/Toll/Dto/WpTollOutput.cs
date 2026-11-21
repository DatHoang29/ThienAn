using Modules.TOLL.Core.Entities;

namespace Modules.WP.Controllers.Toll.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Các Output DTO cho tính năng trạm thu phí hệ thống WP (Toll)
/// Created date: 03/07/2026
/// Update: 07/10/2026 - Kế thừa TollStation + thông tin đơn vị TMS được gán (TmsWorkUnit, qua RefId).
/// </summary>
public class WpPageTollOutput : TollStation
{
    /// <summary>Mã đơn vị TMS (TmsWorkUnit.Code)</summary>
    public string? RefCode { get; set; }

    /// <summary>Tên đơn vị TMS (TmsWorkUnit.Name)</summary>
    public string? RefName { get; set; }

    /// <summary>Điện thoại đơn vị TMS (TmsWorkUnit.Phone)</summary>
    public string? RefPhone { get; set; }

    /// <summary>Địa chỉ đơn vị TMS (TmsWorkUnit.Address)</summary>
    public string? RefAddress { get; set; }

    /// <summary>Thông tin đơn vị TMS (TmsWorkUnit.Info)</summary>
    public string? RefInfo { get; set; }
}

/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Toll
/// Created date: 14/08/2026
/// </summary>
public class WpTollOutput : WpPageTollOutput
{
}

/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Detail
/// Created date: 14/08/2026
/// </summary>
public class TollDetailOutput : WpPageTollOutput
{
}
