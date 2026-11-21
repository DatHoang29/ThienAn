using Modules.TMS.Infrastructure.Services.Incident.Classification;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Constants;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Infrastructure.Services.Incident.Mapping;

/// <summary>
/// Author: KienLT
/// Description: Hàm THUẦN tính trường runtime của hồ sơ sự cố — không chạm DB.
///
/// <para><b>Phần bù <c>EventGroup</c> đã chuyển sang <see cref="IncidentGroupResolver"/></b> (22/09/2026):
/// nó cần truy DB, mà bản cũ tự dựng <c>new SqlSugarRepository&lt;TmsEventType&gt;()</c> nên bỏ qua DI. Lớp
/// này giữ lại đúng phần tính toán thuần, và vì thuần nên <c>static</c> là đúng.</para>
/// </summary>
public static class IncidentRuntimeFields
{
    /// <summary>
    /// Mốc tới hạn xử lý tính từ thời điểm tạo hồ sơ, theo UTC.
    /// </summary>
    public static DateTime? DeadlineAfterCreation(TmsIncident row, int seconds, DateTime localNow, DateTime utcNow)
    {
        if (IncidentEventGroupConst.Normalize(row.EventGroup) != IncidentEventGroupConst.Event) return null;
        // Neo hạn xử lý vào MỐC AUDIT do server ghi (CreatedDate ưu tiên, dự phòng CreateTime).
        // KHÔNG dùng StartDate: đó là thời điểm nghiệp vụ do người dùng nhập, có thể lùi giờ khiến hồ sơ
        // hết hạn ngay khi vừa tạo. StartDate chỉ mang ý nghĩa nghiệp vụ, không quyết định hạn xử lý.
        DateTime? anchor = row.CreatedDate ?? (row.CreateTime != default ? row.CreateTime : (DateTime?)null);
        if (anchor == null || seconds <= 0) return null;
        var offset = TimeSpan.FromMinutes(Math.Round((localNow - utcNow).TotalMinutes));
        var utc = anchor.Value.Kind == DateTimeKind.Utc ? anchor.Value : DateTime.SpecifyKind(anchor.Value - offset, DateTimeKind.Utc);
        return utc.AddSeconds(seconds);
    }

    /// <summary>
    /// Hồ sơ hai nhóm hiển thị theo trạng thái. Không dùng TTL để ẩn hồ sơ mở. Trước đây
    /// TTL làm mất icon khi đang chờ cảnh báo hoặc chờ xác minh. Giữ icon
    /// tới lúc kết thúc qua workflow TMS.
    /// </summary>
    public static bool IsVisible(TmsIncident row) =>
        int.TryParse(row.State, out var state)
        && (TmsIncidentStateEnum)state is TmsIncidentStateEnum.Registration or TmsIncidentStateEnum.Verified or TmsIncidentStateEnum.InProgress
        && row.IsDelete == null && !IncidentRecordingConst.IsDuplicate(row.Metadata);
}
