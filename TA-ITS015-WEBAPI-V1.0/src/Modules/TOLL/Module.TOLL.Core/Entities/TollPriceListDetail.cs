using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;
using System;

namespace Modules.TOLL.Core.Entities;

/// <summary>
/// Chi tiết bảng giá thu phí đồng bộ từ ETC.
/// Nguồn: TCS_PriceList_Detail
/// Author: ChuongLP
/// Created date: 24/07/2026
/// </summary>
[SugarTable("TollPriceListDetail")]
public class TollPriceListDetail : EntityTenant
{
    [SugarColumn(IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Mã chi tiết bảng giá nguồn ETC - TCS_PriceList_Detail.ID")]
    public string? SourceId { get; set; }

    [SugarColumn(IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Mã chi tiết bảng giá - TCS_PriceList_Detail.Code")]
    public string? Code { get; set; }

    [SugarColumn(IsNullable = true,
        Length = EntityConst.Length256,
        ColumnDescription = "Tên chi tiết bảng giá - TCS_PriceList_Detail.Name")]
    public string? Name { get; set; }

    [SugarColumn(IsNullable = true,
        ColumnDataType = "decimal(18,2)",
        ColumnDescription = "Mức giá - TCS_PriceList_Detail.Price")]
    public decimal? Price { get; set; }

    [SugarColumn(IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Mã loại xe nguồn ETC - TCS_PriceList_Detail.VehicleType_ID")]
    public string? VehicleTypeSourceId { get; set; }

    [SugarColumn(IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Mã loại vé nguồn ETC - TCS_PriceList_Detail.TicketType_ID")]
    public string? TicketTypeSourceId { get; set; }

    [SugarColumn(IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Mã nhóm giá nguồn ETC - TCS_PriceList_Detail.PriceType_ID")]
    public string? PriceTypeSourceId { get; set; }

    [SugarColumn(IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Mã bảng giá nguồn ETC - TCS_PriceList_Detail.PriceList_ID")]
    public string? PriceListSourceId { get; set; }

    [SugarColumn(IsNullable = true,
        ColumnDescription = "Trạng thái hoạt động - TCS_PriceList_Detail.Active")]
    public bool? Active { get; set; }

    [SugarColumn(IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Mã tuyến đường nguồn ETC - TCS_PriceList_Detail.Road_ID")]
    public string? RoadSourceId { get; set; }

    [SugarColumn(IsNullable = true,
        Length = EntityConst.Length256,
        ColumnDescription = "Ghi chú - TCS_PriceList_Detail.Note")]
    public string? Note { get; set; }

    [SugarColumn(IsNullable = true,
        ColumnDescription = "Thời điểm ITS đồng bộ bản ghi")]
    public DateTime? SyncTime { get; set; }
}