using Modules.TOLL.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.WP.Controllers.PriceList.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Các Input DTO cho tính năng bảng giá hệ thống WP (PriceList)
/// Created date: 03/07/2026
/// Update: 07/10/2026 - Dùng trực tiếp entity TollPriceList (bảng đồng bộ từ ITS10 qua WP_Sync).
///     Trường lọc theo tên cột Toll: SourceId (mã bảng giá), Name, Active.
/// </summary>
public class WpPagePriceListInput : BasePageInput
{
    /// <summary>Tên bảng giá (chứa)</summary>
    public string? Name { get; set; }

    /// <summary>Mã bảng giá nguồn ETC (chứa)</summary>
    public string? SourceId { get; set; }

    /// <summary>Trạng thái hoạt động</summary>
    public bool? Active { get; set; }
}

/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Price List
/// Created date: 14/08/2026
/// </summary>
public class WpPriceListInput
{
    public string? Name { get; set; }
    public string? SourceId { get; set; }
    public bool? Active { get; set; }
}

/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Price List View Detail (bảng giá đang áp dụng)
/// Created date: 14/08/2026
/// </summary>
public class WpPriceListViewDetailInput
{
    public DateTime? CurrentDate { get; set; }
}

/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Add Price List
/// Created date: 14/08/2026
/// </summary>
public class WpAddPriceListInput : TollPriceList
{
}

/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Update Price List
/// Created date: 14/08/2026
/// </summary>
public class WpUpdatePriceListInput : WpAddPriceListInput
{
}

/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Update Status Price List
/// Created date: 14/08/2026
/// </summary>
public class WpUpdateStatusPriceListInput
{
    public DateTime? CurrentDate { get; set; }
}

/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Delete Price List
/// Created date: 14/08/2026
/// </summary>
public class WpDeletePriceListInput : BaseIdInput
{
}
