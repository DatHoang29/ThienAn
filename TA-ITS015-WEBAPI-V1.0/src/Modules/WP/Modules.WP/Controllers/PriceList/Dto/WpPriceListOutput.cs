using Modules.TOLL.Core.Entities;

namespace Modules.WP.Controllers.PriceList.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Các Output DTO cho tính năng bảng giá hệ thống WP (PriceList)
/// Created date: 03/07/2026
/// Update: 07/10/2026 - Kế thừa entity TollPriceList.
/// </summary>
public class WpPagePriceListOutput : TollPriceList
{
    /// <summary>Trạng thái sử dụng (PriceListUseStateEnum): Đang áp dụng / Sắp áp dụng / Cũ</summary>
    public int? UseState { get; set; }
}

/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Price List
/// Created date: 14/08/2026
/// </summary>
public class WpPriceListOutput : TollPriceList
{
    public int? UseState { get; set; }
}

/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Price List View Detail
/// Created date: 14/08/2026
/// </summary>
public class WpPriceListViewDetailOutput : TollPriceList
{
}

/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Update Status Price List
/// Created date: 14/08/2026
/// </summary>
public class WpUpdateStatusPriceListOutput : TollPriceList
{
}
