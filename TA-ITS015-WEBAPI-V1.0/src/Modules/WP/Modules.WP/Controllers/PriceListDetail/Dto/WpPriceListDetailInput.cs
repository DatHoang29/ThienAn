using Modules.TOLL.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.WP.Controllers.PriceListDetail.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Các Input DTO cho tính năng chi tiết bảng giá hệ thống WP (PriceListDetail)
/// Created date: 03/07/2026
/// Update: 07/10/2026 - Dùng trực tiếp entity TollPriceListDetail. Lọc theo mã nguồn ETC:
///     PriceListSourceId (= TollPriceList.SourceId), VehicleTypeSourceId, RoadSourceId (tuyến = cặp trạm vào/ra).
/// </summary>
public class WpPagePriceListDetailInput : BasePageInput
{
    /// <summary>Mã bảng giá nguồn (TollPriceList.SourceId)</summary>
    public string? PriceListSourceId { get; set; }

    /// <summary>Mã loại xe ETC (TollMasterData.SourceId, DataType = VehicleType)</summary>
    public string? VehicleTypeSourceId { get; set; }

    /// <summary>Mã tuyến ETC (TollRoad.SourceId)</summary>
    public string? RoadSourceId { get; set; }

    /// <summary>Tìm theo mã hoặc tên chi tiết (chứa)</summary>
    public string? Keyword { get; set; }

    public bool? Active { get; set; }
}

/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Price List Detail
/// Created date: 14/08/2026
/// </summary>
public class WpPriceListDetailInput
{
    public string? PriceListSourceId { get; set; }
    public string? VehicleTypeSourceId { get; set; }
    public string? RoadSourceId { get; set; }
    public string? Keyword { get; set; }
    public bool? Active { get; set; }
}

/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Add Price List Detail
/// Created date: 14/08/2026
/// </summary>
public class WpAddPriceListDetailInput : TollPriceListDetail
{
}

/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Update Price List Detail
/// Created date: 14/08/2026
/// </summary>
public class WpUpdatePriceListDetailInput : WpAddPriceListDetailInput
{
}

/// <summary>
/// Author: HieuNV
/// Description: Tham số đầu vào của Delete Price List Detail
/// Created date: 14/08/2026
/// </summary>
public class WpDeletePriceListDetailInput : BaseIdInput
{
}
