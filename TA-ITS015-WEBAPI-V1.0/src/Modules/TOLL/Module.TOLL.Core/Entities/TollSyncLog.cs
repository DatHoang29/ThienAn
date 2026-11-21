using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;
using System;

namespace Modules.TOLL.Core.Entities;

/// <summary>
/// Nhật ký đồng bộ dữ liệu thu phí giữa ITS và ETC.
/// Author: ChuongLP
/// Created date: 24/07/2026
/// </summary>
[SugarTable("TollSyncLog")]
public class TollSyncLog : EntityTenant
{
    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length64,
        ColumnDescription = "Loại dữ liệu đồng bộ: Master/Transaction/Summary")]
    public string? DataType { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length256,
        ColumnDescription = "Địa chỉ API ETC đã gọi")]
    public string? ApiEndpoint { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDescription = "Thời gian bắt đầu của khoảng dữ liệu yêu cầu")]
    public DateTime? FromDate { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDescription = "Thời gian kết thúc của khoảng dữ liệu yêu cầu")]
    public DateTime? ToDate { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length512,
        ColumnDescription = "Cursor trước khi gọi API")]
    public string? CursorFrom { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length512,
        ColumnDescription = "Cursor nhận được sau khi gọi API")]
    public string? CursorTo { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDescription = "Thời điểm bắt đầu tiến trình đồng bộ")]
    public DateTime? StartTime { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDescription = "Thời điểm kết thúc tiến trình đồng bộ")]
    public DateTime? EndTime { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDescription = "Tổng số bản ghi nhận từ ETC")]
    public int? ReceivedCount { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDescription = "Tổng số bản ghi được thêm mới")]
    public int? InsertedCount { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDescription = "Tổng số bản ghi được cập nhật")]
    public int? UpdatedCount { get; set; }

    [SugarColumn(
        IsNullable = true,
        ColumnDescription = "Tổng số bản ghi xử lý lỗi")]
    public int? FailedCount { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length32,
        ColumnDescription = "Trạng thái đồng bộ: Running/Success/Partial/Failed")]
    public string? Status { get; set; }

    [SugarColumn(
        IsNullable = true,
        Length = EntityConst.Length128,
        ColumnDescription = "Mã truy vết request dùng để đối chiếu với ETC")]
    public string? RequestId { get; set; }
}