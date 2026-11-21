using Modules.TMS.Core.Constants;

namespace Services.Vds.Features.Events;

/// <summary>
/// Author: KienLT
/// Description: Cổng quyết định một loại TMS đã phân giải có được ghi hồ sơ <c>TmsIncident</c> hay không.
///
/// <c>TmsIncident</c> chỉ dành cho SỰ CỐ (nhóm incident). Loại nhóm event và AID chưa ánh xạ (unknown) chỉ nằm ở
/// bản ghi thô (QuestDB, hoặc <c>TmsTrafficJourney</c> với hành trình); màn hình nhận chúng qua SSE
/// <c>api/vds/vdsEvent/Stream</c>. Không tạo hồ sơ thì cũng không publish, không nhờ TMS chạy tự động.
///
/// Hàm THUẦN để kiểm chứng trực tiếp (tests/VdsEventSse).
/// Created date: 19/10/2026
/// </summary>
public static class VdsEventGate
{
    /// <summary>AID chưa ánh xạ sang mã TMS.</summary>
    public const string Unmapped = "unmapped";

    /// <summary>Loại TMS nhóm event: chỉ hiển thị qua SSE.</summary>
    public const string EventOnly = "event_only";

    /// <summary>Mã đã ánh xạ nhưng danh mục thiếu loại hoặc thiếu nhóm Event/Incident.</summary>
    public const string NotConfigured = "event_group_not_configured";

    /// <summary>
    /// Lý do KHÔNG ghi hồ sơ, hoặc <c>null</c> nếu được ghi. Unknown xét TRƯỚC danh mục: không cần seed
    /// <c>TmsEventType.unknown</c> để bỏ qua đúng.
    /// </summary>
    /// <param name="code">Mã TMS đã phân giải (unknown nếu chưa ánh xạ).</param>
    /// <param name="group">Nhóm đã chuẩn hoá của loại trong danh mục; null nếu thiếu.</param>
    /// <param name="hasCatalogId">Danh mục có dòng loại này.</param>
    public static string? SkipReason(string? code, string? group, bool hasCatalogId)
    {
        if (IsUnknown(code)) return Unmapped;
        if (group is null || !hasCatalogId) return NotConfigured;
        return group == IncidentEventGroupConst.Incident ? null : EventOnly;
    }

    /// <summary>Kiểm tra mã TMS chưa được ánh xạ, bỏ qua khoảng trắng và hoa thường.</summary>
    public static bool IsUnknown(string? code)
        => string.Equals(code?.Trim(), IncidentEventTypeConst.Unknown, StringComparison.OrdinalIgnoreCase);
}
