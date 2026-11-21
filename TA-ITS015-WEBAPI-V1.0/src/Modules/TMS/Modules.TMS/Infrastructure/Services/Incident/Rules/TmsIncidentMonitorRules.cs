using Furion.FriendlyException;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Exceptions;

namespace Modules.TMS.Infrastructure.Services.Incident.Rules;

/// <summary>
/// Author: KienLT
/// Description: Chuẩn hóa bộ lọc và kiểm tra cửa sổ thời gian của màn hình giám sát sự cố.
/// Created date: 19/10/2026
/// </summary>
public static class TmsIncidentMonitorRules
{
    // Chỉ chuyển đổi bộ lọc nội bộ; màn hình giám sát không chạy truy vấn phân trang.
    public static PageTmsIncidentInput Query(TmsIncidentMonitorInput input)
    {
        var query = input.Adapt<PageTmsIncidentInput>();
        query.Page = 1;
        query.PageSize = 1;
        query.OpenIncidentDaysLimit = input.OpenIncidentDaysLimit ?? IncidentConst.DisplayWindow.DefaultOpenDays;
        query.ClosedIncidentDaysLimit = input.ClosedIncidentDaysLimit ?? IncidentConst.DisplayWindow.DefaultClosedDays;
        TmsIncidentPageRules.Normalize(query);
        if (query.OpenIncidentDaysLimit < 1 || query.ClosedIncidentDaysLimit < 1)
            throw Oops.Oh(BaseMsg.TmsIncident.Exception.DisplayWindowInvalid, TmsIncidentPageRules.MaxDaysLimit);
        return query;
    }

    /// <summary>
    /// KienLT - 26/10/2026: số ngày từ giá trị SysOpConfig (chuỗi). Rỗng / không phải số / ngoài [1, MaxDaysLimit]
    /// ⇒ <paramref name="fallback"/> — cùng cách FE (centerInfo.vue) xử lý giá trị lạ.
    /// </summary>
    public static int DaysOrDefault(string? value, int fallback)
        => int.TryParse(value?.Trim(), out var days) && days is >= 1 and <= TmsIncidentPageRules.MaxDaysLimit ? days : fallback;
}
