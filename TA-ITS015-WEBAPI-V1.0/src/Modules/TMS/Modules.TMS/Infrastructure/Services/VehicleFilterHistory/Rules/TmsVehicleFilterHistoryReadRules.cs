using Modules.TMS.Controllers.VehicleFilterHistory.Dto;
using Shared.Utility.Utilities;
using BaseMsg = Modules.VDS.Core.Exceptions.BaseMsg;

namespace Modules.TMS.Infrastructure.Services.VehicleFilterHistory.Rules;

/// <summary>
/// Author: KienLT
/// Description: Kiểm tra cửa sổ đọc lịch sử lọc phương tiện: UTC, tối đa 60 ngày và 500 dòng/trang.
/// Created date: 19/10/2026
///
/// <para><b>Dùng <c>ToUtc</c> là ĐÚNG ở đây, đừng đổi sang <c>ToServerUtc</c>.</b> Cột
/// <c>TmsVehicleFilterHistory.DetectTime</c> theo hợp đồng là UTC: worker ghi thẳng
/// <c>VdsTrafficData.DetectTime</c> (luôn UTC) và đường thêm của web cũng chuẩn hoá về UTC từ 25/09/2026.
/// <c>ToServerUtc</c> chỉ dành cho cột SQL Server ghi bằng <c>DateTime.Now</c> — áp vào đây sẽ tạo đúng lệch
/// 7 tiếng mà nó vốn để chữa.</para>
/// </summary>
public static class TmsVehicleFilterHistoryReadRules
{
    public static (DateTime From, DateTime To) Window(VehicleFilterHistoryWindowInput input)
    {
        if (input.FromDate is null || input.ToDate is null)
            throw Oops.Oh(BaseMsg.Vds.Exception.DateRangeRequired);
        var from = BaseUtil.TimeServer.ToUtc(input.FromDate.Value);
        var to = BaseUtil.TimeServer.ToUtc(input.ToDate.Value);
        if (to <= from || to - from > TimeSpan.FromDays(60))
            throw Oops.Oh(BaseMsg.Vds.Exception.DateRangeInvalid);
        if (input.Page < 1 || input.PageSize < 1 || input.PageSize > 500)
            throw Oops.Oh(BaseMsg.Vds.Exception.PagingInvalid);
        return (from, to);
    }

    public static string NormalizePlate(string? plate) => new string((plate ?? "")
        .Where(c => !char.IsWhiteSpace(c) && c != '.' && c != '-').ToArray()).ToUpperInvariant();
}
