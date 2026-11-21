using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;
using System;

namespace Modules.TOLL.Core.Entities;

/// <summary>
/// Bảng giá thu phí (header) đồng bộ từ ETC.
/// Nguồn: TCS_PriceList (mảng tcsPriceLists trong API mdsmasterdatatcs/allmasterdatatcs)
/// Liên kết: TollPriceListDetail.PriceListSourceId = TollPriceList.SourceId
/// Author: HieuNV
/// Created date: 06/10/2026
/// </summary>
[SugarTable("TollPriceList")]
public class TollPriceList : EntityTenant
{
    [SugarColumn(IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Mã bảng giá nguồn ETC - TCS_PriceList.ID")]
    public string? SourceId { get; set; }

    [SugarColumn(IsNullable = true,
        Length = EntityConst.Length256,
        ColumnDescription = "Tên bảng giá - TCS_PriceList.Name")]
    public string? Name { get; set; }

    [SugarColumn(IsNullable = true,
        ColumnDescription = "Ngày bắt đầu hiệu lực - TCS_PriceList.StartDate")]
    public DateTime? StartDate { get; set; }

    [SugarColumn(IsNullable = true,
        ColumnDescription = "Ngày kết thúc hiệu lực - TCS_PriceList.EndDate")]
    public DateTime? EndDate { get; set; }

    [SugarColumn(IsNullable = true,
        ColumnDescription = "Trạng thái hoạt động - TCS_PriceList.Active")]
    public bool? Active { get; set; }

    [SugarColumn(IsNullable = true,
        Length = EntityConst.Length256,
        ColumnDescription = "Ghi chú - TCS_PriceList.Note")]
    public string? Note { get; set; }

    [SugarColumn(IsNullable = true,
        ColumnDataType = "datetime2(7)", // giữ đủ 7 chữ số giây lẻ như TCS trả, tránh Update lặp do làm tròn
        ColumnDescription = "Ngày tạo bên TCS - TCS_PriceList.Create_Date")]
    public DateTime? SourceCreateDate { get; set; }

    [SugarColumn(IsNullable = true,
        ColumnDataType = "datetime2(7)", // giữ đủ 7 chữ số giây lẻ như TCS trả, tránh Update lặp do làm tròn
        ColumnDescription = "Ngày sửa gần nhất bên TCS - TCS_PriceList.Write_Date")]
    public DateTime? SourceWriteDate { get; set; }

    [SugarColumn(IsNullable = true,
        ColumnDescription = "Thời điểm ITS đồng bộ bản ghi")]
    public DateTime? SyncTime { get; set; }
}
