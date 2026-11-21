using System.Globalization;
using Modules.TMS.Core.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Constants;

/// <summary>New events register normally; only rejected duplicates are canceled.</summary>
public static class IncidentRecordingConst
{
    public const string LegacyState = "recorded";
    public const string SuppressedAutomationMarker = "\"VdsAutomationSuppressed\":true";

    /// <summary>Khoá Metadata trỏ về hồ sơ gốc của một bản trùng.</summary>
    public const string DuplicateKey = "VdsDuplicateOf";

    /// <summary>
    /// Dấu nhận diện bản trùng cho truy vấn hiển thị/thống kê. Dò theo khoá Metadata do hệ thống ghi,
    /// KHÔNG dò chuỗi trong Remark vì Remark là nội dung người dùng sửa được.
    /// </summary>
    public const string DuplicateMarker = "\"" + DuplicateKey + "\"";
    public const string EventReason = "[EventOnly] Sự kiện chỉ ghi nhận, không yêu cầu xử lý.";
    public const string LegacyReason = "[LegacyRecorded] Dữ liệu cũ, chuyển từ trạng thái recorded.";
    public static readonly string CanceledState = ((int)BaseEnums.TmsIncidentStateEnum.Canceled).ToString(CultureInfo.InvariantCulture);

    public static void ApplyToNewEvent(TmsIncident incident)
    {
        if (IncidentEventGroupConst.Normalize(incident.EventGroup) != IncidentEventGroupConst.Event) return;
        incident.State = ((int)BaseEnums.TmsIncidentStateEnum.Registration).ToString(CultureInfo.InvariantCulture);
    }

    public static void ApplyToDuplicate(TmsIncident incident, string originalId)
    {
        // Kiểm tra ghi chú TRƯỚC khi đổi entity: ghi chú quá dài không được để hồ sơ bị sửa một phần.
        var remark = AppendReason(incident.Remark, $"[Duplicate] Trùng trong thời gian chống trùng với hồ sơ {originalId}.");
        var metadata = WithDuplicateMarker(incident.Metadata, originalId);
        incident.State = CanceledState;
        incident.Remark = remark;
        incident.Metadata = metadata;
    }

    /// <summary>
    /// Ghi khoá bản trùng vào Metadata, giữ nguyên các khoá đang có (dispatch, suppressed…).
    /// Gọi lại nhiều lần không nhân đôi khoá. Nếu vượt độ dài cột thì GIỮ NGUYÊN Metadata cũ: chặn bản
    /// tin trùng không được phép làm hỏng lệnh ghi của luồng thu nhận dữ liệu.
    /// </summary>
    public static string? WithDuplicateMarker(string? metadata, string originalId)
    {
        var root = string.IsNullOrWhiteSpace(metadata) ? new JObject() : JObject.Parse(metadata);
        root[DuplicateKey] = originalId;
        var result = root.ToString(Formatting.None);
        return result.Length > Shared.DTO.Constants.Application.EntityConst.Length512 ? metadata : result;
    }

    /// <summary>Hồ sơ bị loại vì trùng trong cửa sổ chống trùng.</summary>
    public static bool IsDuplicate(string? metadata) =>
        metadata?.Contains(DuplicateMarker, StringComparison.Ordinal) == true;

    public static string AppendReason(string? remark, string reason)
    {
        var result = remark?.Contains(reason, StringComparison.Ordinal) == true
            ? remark
            : string.IsNullOrEmpty(remark) ? reason : remark + "\n" + reason;
        IncidentRemarkConst.Validate(result);
        return result;
    }
}
