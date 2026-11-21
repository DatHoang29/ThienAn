using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;
using System;

namespace Modules.TOLL.Core.Entities;

/// <summary>
/// Giao dịch xe ra đồng bộ từ ETC.
/// Nguồn: TCS_Transaction_Out
/// Author: ChuongLP
/// Created date: 24/07/2026
/// </summary>
[SugarTable("TollTransactionOut")]
public class TollTransactionOut : EntityTenant
{
    [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Mã giao dịch nguồn ETC - TCS_Transaction_Out.Transaction_ID")]
    public string? TransactionId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length32, ColumnDescription = "Mã trạm vào - TCS_Transaction_Out.Station_ID_In")]
    public string? StationIdIn { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length32, ColumnDescription = "Mã trạm ra - TCS_Transaction_Out.Station_ID")]
    public string? StationId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length32, ColumnDescription = "Mã làn vào - TCS_Transaction_Out.Lane_ID_In")]
    public string? LaneIdIn { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length32 , ColumnDescription = "Mã làn ra - TCS_Transaction_Out.Lane_ID")]
    public string? LaneId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length32, ColumnDescription = "Trạng thái hậu kiểm - TCS_Transaction_Out.Status_Checked")]
    public string? StatusChecked { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã loại xe - TCS_Transaction_Out.VehicleType_ID")]
    public string? VehicleTypeId { get; set; }

    [SugarColumn(IsNullable = true,Length = EntityConst.Length64,ColumnDescription = "Mã lý do - TCS_Transaction_Out.Reason_ID")]

    public string? ReasonId { get; set; }

    [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã vé - TCS_Transaction_Out.Ticket_ID")]
    public string? TicketId { get; set; }

    [SugarColumn(
   IsNullable = true,
   Length = EntityConst.Length64,
   ColumnDescription = "Loại giá vé - TCS_Transaction_Out.TicketType_ID")]
    public string? TicketTypeId { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Mã thẻ eTag - TCS_Transaction_Out.TagID")]
    public string? TagId { get; set; }

    [SugarColumn(IsNullable = true,
        ColumnDataType = "decimal(18,2)",
        ColumnDescription = "Mức giá - TCS_PriceList_Detail.Price")]
    public decimal? Price { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length32,
        ColumnDescription = "Biển số chính thức - TCS_Transaction_Out.Plate")]
    public string? Plate { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length32,
        ColumnDescription = "Biển số nhận dạng LPR - TCS_Transaction_Out.Plate_LPR")]
    public string? PlateLpr { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length32,
        ColumnDescription = "Biển số sau hậu kiểm - TCS_Transaction_Out.Plate_Edit")]
    public string? PlateEdit { get; set; }

    [SugarColumn(IsNullable = true, ColumnDescription = "Thời gian xe ra - TCS_Transaction_Out.Transaction_Datetime")]
    public DateTime? TransactionDateTime { get; set; }

    [SugarColumn(IsNullable = true,  ColumnDescription = "Thời gian xe vào - TCS_Transaction_Out.Transaction_Datetime_In")]
    public DateTime? TransactionDateTimeIn { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length128,
        ColumnDescription = "Mã trạng thái giao dịch - TCS_Transaction_Out.Status_ID")]
    public string? StatusId { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.RemarkFieldLength,
        ColumnDescription = "Đường dẫn ảnh toàn cảnh xe ra - TCS_Transaction_Out.ImageLane")]
    public string? ImageLane { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.RemarkFieldLength,
        ColumnDescription = "Đường dẫn ảnh biển số xe ra - TCS_Transaction_Out.ImagePlate")]
    public string? ImagePlate { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.RemarkFieldLength,
        ColumnDescription = "Đường dẫn ảnh làn vào - TCS_Transaction_Out.ImageLane_In")]
    public string? ImageLaneIn { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.RemarkFieldLength,
        ColumnDescription = "Đường dẫn ảnh biển số vào - TCS_Transaction_Out.ImagePlate_In")]
    public string? ImagePlateIn { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Mã nhân viên - TCS_Transaction_Out.User_ID")]
    public string? UserId { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length128,
        ColumnDescription = "Tên nhân viên vận hành - dữ liệu ETC kết hợp SysUser")]
    public string? OperatorName { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Mã ca làm việc - TCS_Transaction_Out.Shift_ID")]
    public string? ShiftCode { get; set; }

    [SugarColumn(
        IsNullable = true, ColumnDescription = "Ngày doanh thu - TCS_Transaction_Out.Proccess_Date")]
    public DateTime? ProcessDate { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length128,
        ColumnDescription = "Mã vé hoặc QR - TCS_Transaction_Out.Serial")]
    public string? Serial { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDescription = "Thời điểm ITS đồng bộ bản ghi")]
    public DateTime? SyncTime { get; set; }
}