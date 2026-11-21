using System.Globalization;
using System.Text.Json;
using ITS.Toll.Core.Contracts;
using ITS.Toll.Core.Logging;
using ITS.Toll.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ITS.Toll.Core.Endpoints;

/// <summary>
/// Khai báo 2 endpoint TCS gọi vào (dùng chung cho ITS.Toll.Worker và ITS.Toll.Wpf):
///   POST /api/toll/record : push 1 bản ghi kèm thao tác Insert/Update/Delete.
///   POST /api/toll/import  : báo import theo ngày → ITS tự pull lại TCS (trả 202, xử lý nền).
/// Chỉ phụ thuộc <see cref="TollCallBackService"/> + <see cref="TollLogger"/> (đều ở Core) nên nhúng được
/// vào bất kỳ host nào (Kestrel của Worker hoặc Kestrel nhúng trong form WPF).
/// Author: ChuongLP
/// Created date: 12/09/2026
/// </summary>
public static class TollCallBackEndpoints
{
    public static void MapTollCallBackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/toll");

        // ============ POST /api/toll/record ============
        group.MapPost("/record", async (TollRecordRequest? req, TollCallBackService callback) =>
        {
            if (req is null)
                return Json(TollApiResult.Fail("Body rỗng hoặc không đọc được JSON."));

            var dataType = TollCallBackService.NormalizeDataType(req.DataType);
            if (dataType is null)
                return Json(TollApiResult.Fail(
                    $"'dataType' không hợp lệ. Chỉ nhận: {string.Join(", ", TollCallBackService.SupportedDataTypes)}."));

            var operation = TollCallBackService.NormalizeOperation(req.Operation);
            if (operation is null)
                return Json(TollApiResult.Fail(
                    $"'operation' không hợp lệ. Chỉ nhận: {string.Join(", ", TollCallBackService.SupportedOperations)}."));

            var isDelete = string.Equals(operation, "Delete", StringComparison.OrdinalIgnoreCase);

            // Delete: cần id bên TCS (sourceId gửi kèm hoặc rút từ data).
            if (isDelete)
            {
                var id = ResolveDeleteId(dataType, req);
                if (string.IsNullOrWhiteSpace(id))
                    return Json(TollApiResult.Fail("Thiếu id để xóa (gửi 'sourceId' hoặc kèm khóa trong 'data')."));
            }
            else
            {
                // Insert/Update: bắt buộc có 'data' là object.
                if (req.Data is not { ValueKind: JsonValueKind.Object })
                    return Json(TollApiResult.Fail("Thiếu 'data' (object bản ghi) cho thao tác Insert/Update."));
            }

            var data = req.Data ?? default;
            var result = await callback.HandleRecordAsync(dataType, operation, data, req.SourceId);

            if (!result.Ok)
                return Json(TollApiResult.Fail(result.Message, 500), 500);

            return Json(TollApiResult.Success(result.Message, new
            {
                dataType,
                operation,
                result.Received,
                result.Inserted,
                result.Updated,
                result.Unchanged,
                result.Deleted,
            }));
        });

        // ============ POST /api/toll/import ============
        // TCS báo đã import dữ liệu của MỘT ngày → ITS pull lại đúng ngày đó (theo dataType).
        group.MapPost("/import", (TollImportRequest? req, TollCallBackService callback, TollLogger logger) =>
        {
            if (req is null)
                return Json(TollApiResult.Fail("Body rỗng hoặc không đọc được JSON."));

            var dataType = TollCallBackService.NormalizeDataType(req.DataType);
            if (dataType is null)
                return Json(TollApiResult.Fail(
                    $"'dataType' không hợp lệ. Chỉ nhận: {string.Join(", ", TollCallBackService.SupportedDataTypes)}."));

            if (string.IsNullOrWhiteSpace(req.DateTime))
                return Json(TollApiResult.Fail("Thiếu 'dateTime'."));
            if (!TryParseDate(req.DateTime, out var date))
                return Json(TollApiResult.Fail("'dateTime' sai định dạng (nhận 'yyyy-MM-dd', 'yyyy-MM-dd HH:mm:ss' hoặc ISO 8601)."));

            var (from, to) = DayRange(date);

            // Trả 202 ngay; pull lại chạy nền (không bắt TCS chờ).
            _ = Task.Run(async () =>
            {
                try
                {
                    await callback.RepullRangeAsync(dataType, from, to, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.Error(LogCategory.CallBack, $"[IMPORT] Lỗi pull lại nền [{dataType}] ngày {date:yyyy-MM-dd}: {ex.Message}");
                }
            });

            return Json(TollApiResult.Success(
                $"Đã nhận yêu cầu import [{dataType}] ngày {date:yyyy-MM-dd}. ITS đang pull lại dữ liệu.",
                new { dataType, dateTime = date, fromDate = from, toDate = to }, 202), 202);
        });
    }

    /// <summary>Dựng khoảng cả ngày [00:00:00 … 23:59:59] từ 1 mốc thời gian (chỉ lấy phần ngày).</summary>
    private static (DateTime From, DateTime To) DayRange(DateTime date)
    {
        var from = date.Date;
        var to = date.Date.AddDays(1).AddSeconds(-1);
        return (from, to);
    }

    /// <summary>Lấy id xóa: ưu tiên sourceId trong request; nếu rỗng thì rút từ 'data' theo khóa của DataType.</summary>
    private static string? ResolveDeleteId(string dataType, TollRecordRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.SourceId)) return req.SourceId!.Trim();
        if (req.Data is not { ValueKind: JsonValueKind.Object } data) return null;

        // OutSummary xóa theo ticket_Confirmed_ID (cột ConfirmedId); transaction xóa theo transaction_ID.
        var key = string.Equals(dataType, "OutSummary", StringComparison.OrdinalIgnoreCase) ? "ticket_Confirmed_ID" : "transaction_ID";
        foreach (var p in data.EnumerateObject())
        {
            if (string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase))
                return p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.GetRawText();
        }
        return null;
    }

    /// <summary>Parse ngày linh hoạt: chấp nhận "yyyy-MM-dd HH:mm:ss" (chuẩn hệ Toll) và ISO 8601.</summary>
    private static bool TryParseDate(string? value, out DateTime result) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

    /// <summary>Trả JSON kèm HTTP status code khớp với Code trong kết quả.</summary>
    private static IResult Json(TollApiResult result, int? statusCode = null) =>
        Results.Json(result, statusCode: statusCode ?? result.Code);
}
