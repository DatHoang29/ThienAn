namespace Modules.VDS.Core.Constants;

/// <summary>
/// Author: KienLT
/// Description: Giá trị cột <c>PeriodType</c> của bảng tổng hợp <c>TmsTrafficSummary</c>.
///
/// <para>Phạm vi (<c>ScopeType</c>) KHÔNG khai ở đây — đã có <c>TrafficCountScopes</c> trong
/// <c>Shared.Utility.Counting</c> (tầng đếm chuyển xuống thư viện dùng chung ngày 23/09/2026, trước đó ở
/// <c>Modules.VDS.Core.Counting</c>). Khai lại sẽ thành hai nguồn sự thật cho cùng một khái niệm.</para>
///
/// <para><b>Đừng gộp với các chuỗi trông giống:</b> <c>("catalog", "equipment")</c> trong
/// <c>VdsRefreshAfterSaveFilter</c> là scope của LUỒNG REFRESH, <c>[Sse("zone")]</c> là tên sự kiện SSE, và
/// <c>Mode = "snapshot"</c> của payload dashboard là chế độ trả dữ liệu — trùng chữ nhưng khác nghĩa hoàn
/// toàn với nhánh media <c>snapshot</c> ở <see cref="VdsMediaKindConst"/>.</para>
/// Created date: 21/09/2026
/// </summary>
public static class VdsSummaryPeriodConst
{
    /// <summary>Chốt theo giờ — mức chi tiết nhất mà bảng tổng hợp lưu.</summary>
    public const string Hour = "hour";

    /// <summary>Chốt theo ngày.</summary>
    public const string Day = "day";

    /// <summary>Chốt theo tháng.</summary>
    public const string Month = "month";

    /// <summary>Tập giá trị hợp lệ — dùng cho kiểm tra đầu vào, không rải điều kiện ở nhiều lớp.</summary>
    public static bool IsValid(string? period) => period is Hour or Day or Month;
}
