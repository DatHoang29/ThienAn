using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;
using System;

namespace Modules.TOLL.Core.Entities;

/// <summary>
/// Danh mục tuyến đường đồng bộ từ ETC.
/// Nguồn: TCS_Road
/// Author: ChuongLP
/// Created date: 24/07/2026
/// </summary>
[SugarTable("TollRoad")]
public class TollRoad : EntityTenant
{
    [SugarColumn(
        IsNullable = true,
         Length = EntityConst.Length32,
        ColumnDescription = "Mã tuyến đường nguồn ETC - TCS_Road.ID")]
    public string? SourceId { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length128,
        ColumnDescription = "Tên tuyến đường - TCS_Road.Name")]
    public string? Name { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Mã trạm vào - TCS_Road.Station_ID_in")]
    public string? StationIdIn { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Mã trạm ra - TCS_Road.Station_ID_out")]
    public string? StationIdOut { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length32,
        ColumnDescription = "Số kilomet của tuyến đường - TCS_Road.KM")]
    public string? Km { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.RemarkFieldLength,
        ColumnDescription = "Ghi chú - TCS_Road.Note")]
    public string? Remark { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDescription = "Trạng thái hoạt động - TCS_Road.Active")]
    public bool? Active { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDescription = "Thời điểm ITS đồng bộ bản ghi")]
    public DateTime? SyncTime { get; set; }
}