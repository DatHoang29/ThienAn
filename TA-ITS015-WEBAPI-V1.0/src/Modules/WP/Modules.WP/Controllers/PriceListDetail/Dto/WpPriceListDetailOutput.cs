using Modules.TOLL.Core.Entities;

namespace Modules.WP.Controllers.PriceListDetail.Dto;

/// <summary>
/// Author: HieuNV
/// Description: Các Output DTO cho tính năng chi tiết bảng giá hệ thống WP (PriceListDetail)
/// Created date: 03/07/2026
/// Update: 07/10/2026 - Kế thừa entity TollPriceListDetail + các trường hiển thị tra từ danh mục Toll.
/// </summary>
public class WpPagePriceListDetailOutput : TollPriceListDetail
{
    /// <summary>Tên bảng giá (TollPriceList.Name)</summary>
    public string? PriceListName { get; set; }

    /// <summary>Tên tuyến (TollRoad.Name)</summary>
    public string? RoadName { get; set; }

    /// <summary>Mã trạm vào (TollRoad.StationIdIn)</summary>
    public string? StationIdIn { get; set; }

    /// <summary>Mã trạm ra (TollRoad.StationIdOut)</summary>
    public string? StationIdOut { get; set; }

    /// <summary>Tên trạm vào (TollStation.Name)</summary>
    public string? StationInName { get; set; }

    /// <summary>Tên trạm ra (TollStation.Name)</summary>
    public string? StationOutName { get; set; }

    /// <summary>Tên loại xe (TollMasterData.Name, DataType = VehicleType)</summary>
    public string? VehicleTypeName { get; set; }
}

/// <summary>
/// Author: HieuNV
/// Description: Dữ liệu trả về của Price List Detail
/// Created date: 14/08/2026
/// </summary>
public class WpPriceListDetailOutput : WpPagePriceListDetailOutput
{
}
