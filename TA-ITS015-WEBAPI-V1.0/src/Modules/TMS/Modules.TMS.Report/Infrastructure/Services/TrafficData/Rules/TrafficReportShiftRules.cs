using Furion.FriendlyException;

namespace Modules.TMS.Report.Infrastructure.Services.TrafficData.Rules;

internal static class TrafficReportShiftRules
{
    public static (int FromHour, int ToHour, bool HasNextDayShift) Parse(string? value)
    {
        var parts = value?.Trim().Split('-');
        if (parts is not { Length: 2 }
            || !int.TryParse(parts[0], out var fromHour)
            || !int.TryParse(parts[1], out var toHour)
            || fromHour is < 0 or > 24 || toHour is < 0 or > 24
            || fromHour == toHour)
            throw new AppFriendlyException("Cấu hình ca không hợp lệ. Giờ bắt đầu và kết thúc phải khác nhau, trong khoảng 0–24 (ví dụ: 22-6 hoặc 0-24).", null).StatusCode(400);

        if (fromHour == 24) fromHour = 0;
        if (toHour == 0) toHour = 24;
        return (fromHour, toHour, fromHour > toHour);
    }
}
