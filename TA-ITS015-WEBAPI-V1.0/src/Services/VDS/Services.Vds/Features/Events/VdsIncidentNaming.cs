using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Services.Vds.Features.Events;

/// <summary>
/// Author: KienLT
/// Description: Đặt MÃ và TÊN cho sự cố tự động theo ĐÚNG chuẩn mà hệ thống web đang dùng, vì bảng sự cố
/// là bảng dùng chung — sự cố do service sinh phải nhìn giống sự cố người dùng tạo:
/// <code>
/// Code = ICC_{eventTypeCode}_{location}_{influenceScope}_{km}_{met}_{ddMMyyyyHHmmss}
/// Name = {TênLoạiSựCố} {TênTuyến} {TênHướng} Km{km}+{met} {dd-MM-yyyy HH:mm:ss}
/// </code>
///
/// Mốc thời gian trong cả mã và tên là <c>StartDate</c> — GIỜ HỆ THỐNG, không phải giờ UTC.
///
/// Mã vẫn TẤT ĐỊNH khi replay: mọi thành phần đều suy từ dữ liệu của sự việc (loại, vị trí, hướng, thời
/// điểm), nên cùng bản tin cho ra cùng mã. Nhờ vậy mã vừa đọc được bằng mắt vừa dùng được để chống trùng.
/// Created date: 26/08/2026
/// </summary>
public static class VdsIncidentNaming
{
    public const string UnknownCode = "0";
    public static string Code(string? eventTypeCode, string? location, string? influenceScope,
        int? km, int? met, DateTime startDate) =>
        Modules.TMS.Core.Helper.IncidentNamingHelper.Code(eventTypeCode, location, influenceScope, km, met, startDate);
    public static string Name(string? eventTypeName, string? locationName, string? influenceScopeName,
        int? km, int? met, DateTime startDate) =>
        Modules.TMS.Core.Helper.IncidentNamingHelper.Name(eventTypeName, locationName, influenceScopeName, km, met, startDate);
}
/// <summary>
/// Author: KienLT
/// Description: Khoá gộp dùng cho chống trùng sự việc: hash của (thiết bị, loại bản tin, định danh phụ).
///
/// Dùng hash thay vì ghép chuỗi thô vì khoá này là tên mục trong file trạng thái: biển số và tên loại có
/// thể chứa ký tự không hợp lệ cho tên khoá, còn hash thì luôn cùng độ dài và an toàn.
/// Created date: 26/08/2026
/// </summary>
public static class ConditionHash
{
    public static string Create(string? equipmentId, string? eventType, string? extraKey = null)
    {
        var raw = $"{equipmentId}|{eventType}|{extraKey}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant()[..32];
    }
}
