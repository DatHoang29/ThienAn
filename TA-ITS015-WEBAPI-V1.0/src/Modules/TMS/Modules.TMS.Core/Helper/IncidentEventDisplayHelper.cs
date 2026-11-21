using Modules.TMS.Core.Constants;
using Newtonsoft.Json.Linq;

namespace Modules.TMS.Core.Helper;

/// <summary>
/// Author: KienLT
/// Description: Hiển thị loại unknown theo AID đã lưu, không tra lại ánh xạ hiện hành.
/// Created date: 19/10/2026
/// </summary>
public static class IncidentEventDisplayHelper
{
    /// <summary>
    /// Author: KienLT
    /// Description: Mã loại sự kiện là unknown (không phân biệt hoa thường).
    /// Created date: 22/10/2026
    /// </summary>
    public static bool IsUnknown(string? code)
        => string.Equals(code, IncidentEventTypeConst.Unknown, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Author: KienLT
    /// Description: Tên hiển thị loại sự kiện; loại unknown ghép AID đã lưu trong metadata, metadata lỗi dùng nhãn dự phòng do bên gọi dịch.
    /// Created date: 19/10/2026
    /// </summary>
    public static string? Name(string? code, string? name, string? metadata, string unknownLabel)
    {
        if (!IsUnknown(code)) return name;
        var aid = IncidentMetadataConst.Read(metadata, preserveDateStrings: true)[IncidentMetadataConst.AidEventType];
        if (aid?.Type == JTokenType.String && !string.IsNullOrWhiteSpace(aid.Value<string>()))
            return $"{unknownLabel} ({aid.Value<string>()})";
        return unknownLabel;
    }
}
