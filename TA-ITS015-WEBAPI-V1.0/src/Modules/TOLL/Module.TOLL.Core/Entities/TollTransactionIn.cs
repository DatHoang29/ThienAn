using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;
using System;

namespace Modules.TOLL.Core.Entities;

/// <summary>
/// Giao dịch xe vào đồng bộ từ ETC.
/// Nguồn: TCS_Transaction_In
/// Author: ChuongLP
/// Created date: 24/07/2026
/// </summary>
[SugarTable("TollTransactionIn")]
public class TollTransactionIn : EntityTenant
{
    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length128,
        ColumnDescription = "Mã giao dịch nguồn ETC - TCS_Transaction_In.Transaction_ID")]
    public string? TransactionId { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length128,
        ColumnDescription = "Mã trạm - TCS_Transaction_In.Station_ID")]
    public string? StationId { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length128,
        ColumnDescription = "Mã làn - TCS_Transaction_In.Lane_ID")]
    public string? LaneId { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDescription = "Thời gian giao dịch - TCS_Transaction_In.Transaction_Datetime")]
    public DateTime? TransactionDateTime { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length32,
        ColumnDescription = "Trạng thái hậu kiểm - TCS_Transaction_In.Status_Checked")]
    public string? StatusChecked { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length128,
        ColumnDescription = "Mã loại xe - TCS_Transaction_In.VehicleType_ID")]
    public string? VehicleTypeId { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length128,
        ColumnDescription = "Mã lý do - TCS_Transaction_In.Reason_ID")]
    public string? ReasonId { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length128,
        ColumnDescription = "Mã vé - TCS_Transaction_In.Ticket_ID")]
    public string? TicketId { get; set; }

    [SugarColumn(
       IsNullable = true,
       Length = EntityConst.Length64,
       ColumnDescription = "Loại giá vé - TCS_Transaction_In.TicketType_ID")]
    public string? TicketTypeId { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Mã thẻ eTag - TCS_Transaction_In.TagID")]
    public string? TagId { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length32,
        ColumnDescription = "Biển số chính thức - TCS_Transaction_In.Plate")]
    public string? Plate { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length32,
        ColumnDescription = "Biển số nhận dạng LPR - TCS_Transaction_In.Plate_LPR")]
    public string? PlateLpr { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length32,
        ColumnDescription = "Biển số sau hậu kiểm - TCS_Transaction_In.Plate_Edit")]
    public string? PlateEdit { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDescription = "Thời gian xe vào - TCS_Transaction_In.Transaction_Datetime_In")]
    public DateTime? TransactionDateTimeIn { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDataType = "varchar(100)",
        ColumnDescription = "Mã trạng thái giao dịch - TCS_Transaction_In.Status_ID")]
    public string? StatusId { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.RemarkFieldLength,
        ColumnDescription = "Đường dẫn ảnh làn vào - TCS_Transaction_In.ImageLane")]
    public string? ImageLane { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.RemarkFieldLength,
        ColumnDescription = "Đường dẫn ảnh biển số vào - TCS_Transaction_In.ImagePlate")]
    public string? ImagePlate { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnName = "UserID",
        Length = EntityConst.RemarkFieldLength,
        ColumnDescription = "Mã nhân viên - TCS_Transaction_In.User_ID")]
    public string? UserId { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.RemarkFieldLength,
        ColumnDescription = "Tên nhân viên vận hành - dữ liệu ETC kết hợp SysUser")]
    public string? OperatorName { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Mã ca làm việc - TCS_Transaction_In.Shift_ID")]
    public string? ShiftCode { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDescription = "Ngày doanh thu - TCS_Transaction_In.Proccess_Date")]
    public DateTime? ProcessDate { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length128,
        ColumnDescription = "Mã vé hoặc QR - TCS_Transaction_In.Serial")]
    public string? Serial { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDescription = "Thời điểm ITS đồng bộ bản ghi")]
    public DateTime? SyncTime { get; set; }
}