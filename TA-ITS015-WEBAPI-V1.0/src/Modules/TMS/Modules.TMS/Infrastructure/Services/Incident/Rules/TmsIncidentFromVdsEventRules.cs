using Modules.TMS.Core.Constants;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Infrastructure.Services.Incident.Rules;

/// <summary>
/// Author: KienLT
/// Description: Luật thuần của "Tạo sự cố từ sự kiện" (tab Sự kiện): loại nào được tạo và trạng thái ban đầu theo
/// hình thức người vận hành chọn.
/// Created date: 26/10/2026
/// </summary>
public static class TmsIncidentFromVdsEventRules
{
    /// <summary>Trạng thái kết thúc được phép khi chỉ ghi nhận: Hoàn thành, Bỏ qua, Huỷ.</summary>
    public static readonly int[] RecordStates =
    {
        (int)TmsIncidentStateEnum.Finished,
        (int)TmsIncidentStateEnum.Skipped,
        (int)TmsIncidentStateEnum.Canceled,
    };

    /// <summary>
    /// Chỉ sự kiện có loại (đã ánh xạ) thuộc nhóm SỰ CỐ mới tạo được sự cố: loại nhóm sự kiện chỉ phát SSE, còn
    /// <c>unknown</c> (AID chưa ánh xạ) không bao giờ vào TmsIncident.
    /// </summary>
    public static bool IsIncidentType(string? typeGroup, string? typeCode)
        => IncidentEventGroupConst.Normalize(typeGroup) == IncidentEventGroupConst.Incident
           && !string.Equals(typeCode?.Trim(), IncidentEventTypeConst.Unknown, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Trạng thái lúc tạo: <c>process</c> ⇒ Đăng ký (rồi theo luồng xử lý cũ); <c>record</c> ⇒ trạng thái kết thúc đã
    /// chọn. Hình thức lạ hoặc trạng thái ghi nhận không hợp lệ ⇒ null.
    /// </summary>
    public static int? InitialState(string? mode, int? recordState)
    {
        var normalized = mode?.Trim().ToLowerInvariant();
        if (normalized == IncidentConst.FromVdsEvent.Mode.Process) return (int)TmsIncidentStateEnum.Registration;
        if (normalized == IncidentConst.FromVdsEvent.Mode.Record && recordState is { } state && RecordStates.Contains(state)) return state;
        return null;
    }
}
