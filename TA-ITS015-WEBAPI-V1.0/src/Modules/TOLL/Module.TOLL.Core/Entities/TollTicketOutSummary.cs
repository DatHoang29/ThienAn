using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.TOLL.Core.Entities;

/// <summary>
/// Dữ liệu tổng hợp doanh thu chính thức (ITS không tự tính lại từ giao dịch chi tiết).
/// Nguồn ETC: TCS_Ticket_Out_Summary
/// Author: ChuongLP
/// Created date: 23/07/2026
/// </summary>
[SugarTable("TollTicketOutSummary")]
public class TollTicketOutSummary : EntityTenant
{
    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã tổng hợp nguồn - TCS_Ticket_Out_Summary.ID")]
    public string? SourceId { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Ngày chốt số liệu - ProcessDate")]
    public DateTime? ProcessDate { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Mốc bắt đầu kỳ tổng hợp - StartDate")]
    public DateTime? StartTime { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Mốc kết thúc kỳ tổng hợp - EndDate")]
    public DateTime? EndTime { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã trạm - Station_ID")]
    public string? StationId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã trạm vào - Station_ID_In")]
    public string? EntryStationId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "Hướng: 1 = vào, 2 = ra")]
    public string? Direction { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã loại xe - VehicleType_ID")]
    public string? VehicleTypeId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã loại vé - TicketType_ID")]
    public string? TicketTypeId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã nhóm giá - PriceType_ID")]
    public string? PriceTypeId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã bảng giá - PriceList_ID")]
    public string? PriceListId { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = "decimal(18,2)", ColumnDescription = "Mệnh giá - Price")]
    public decimal? Price { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Số lượng gốc - Amount_Total")]
    public long? AmountTotal { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = "decimal(20,2)", ColumnDescription = "Doanh thu gốc - Total")]
    public decimal? TotalAmount { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Số lượng điều chỉnh - Amount_Edit")]
    public long? AmountEdit { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = "decimal(20,2)", ColumnDescription = "Doanh thu điều chỉnh - Total_Edit")]
    public decimal? TotalEdit { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Tổng lưu lượng - Amount_Sum")]
    public long? AmountSum { get; set; }

    [SugarColumn(IsNullable = true, ColumnDataType = "decimal(20,2)", ColumnDescription = "Tổng doanh thu - Total_Sum")]
    public decimal? TotalSum { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã lý do - Reason_ID (cần ETC xác nhận field trong bảng summary)")]
    public string? ReasonId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã xác nhận đối soát - Ticket_Confirmed_ID")]
    public string? ConfirmedId { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm ITS đồng bộ bản ghi")]
    public DateTime? LastSyncTime { get; set; }
}
