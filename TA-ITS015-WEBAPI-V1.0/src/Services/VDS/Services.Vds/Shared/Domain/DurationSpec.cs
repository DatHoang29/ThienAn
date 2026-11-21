using System.Globalization;

namespace Services.Vds.Shared.Domain;

/// <summary>
/// Author: KienLT
/// Description: Đọc cấu hình THỜI LƯỢNG dạng chuỗi có HẬU TỐ đơn vị, dùng chung cho cửa Skip (ghi thô),
/// cửa Dedup (tạo sự cố) và các giá trị mặc định.
///
/// <b>Quy ước</b>: <c>s</c>=giây, <c>m</c>=phút, <c>h</c>=giờ, <c>d</c>=ngày. KHÔNG có hậu tố ⇒ tính bằng
/// PHÚT. Hoa/thường đều được, cho phép khoảng trắng hai đầu (<c>" 10 s "</c>).
///
/// <b>Vì sao phải có hàm riêng thay vì <c>int.Parse</c></b>: bộ cấu hình cũ trong appsettings tính bằng
/// GIÂY. Nếu chép nguyên số sang cấu hình mới thì <c>10</c> hoá thành 10 PHÚT — sai gấp 60 lần mà không có
/// lỗi nào báo. Vì vậy giá trị seed BẮT BUỘC ghi kèm hậu tố, và hàm này luôn trả về GIÂY để tầng trên chỉ
/// làm việc với một đơn vị duy nhất.
///
/// <b>Fail-safe</b>: chuỗi rỗng/sai định dạng/âm/vượt trần ⇒ trả <c>null</c> để bên gọi dùng giá trị mặc
/// định và ghi log, KHÔNG ném lỗi — một dòng cấu hình sai trên DB không được phép hạ service.
/// <c>0</c> là giá trị HỢP LỆ và có nghĩa TẮT CỬA (không chặn gì).
/// Created date: 07/09/2026
/// </summary>
public static class DurationSpec
{
    /// <summary>Trần an toàn: 30 ngày. Lớn hơn mức này gần như chắc chắn là gõ sai (ví dụ thiếu hậu tố).</summary>
    public const int MaxSeconds = 30 * 24 * 60 * 60;

    /// <summary>
    /// Đổi chuỗi cấu hình thành SỐ GIÂY. Trả <c>null</c> nếu không đọc được (bên gọi tự dùng mặc định).
    /// </summary>
    /// <param name="value">Ví dụ: <c>10s</c>, <c>5m</c>, <c>2h</c>, <c>1d</c>, <c>3</c> (=3 phút), <c>0</c> (tắt).</param>
    public static int? TryParseSeconds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var text = value.Trim();

        // Hậu tố là KÝ TỰ CUỐI. Không có hậu tố hợp lệ ⇒ coi cả chuỗi là số và đơn vị mặc định là PHÚT.
        var last = text[^1];
        var multiplier = last switch
        {
            's' or 'S' => 1,
            'm' or 'M' => 60,
            'h' or 'H' => 3600,
            'd' or 'D' => 86400,
            _ => 0,
        };

        var numberPart = multiplier == 0 ? text : text[..^1].TrimEnd();
        if (multiplier == 0) multiplier = 60; // không hậu tố ⇒ PHÚT

        if (!long.TryParse(numberPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount))
            return null;

        // Âm là vô nghĩa với cửa thời gian; 0 giữ lại vì đó là cách TẮT cửa.
        if (amount < 0) return null;

        var seconds = amount * multiplier;
        if (seconds > MaxSeconds) return null;

        return (int)seconds;
    }

    /// <summary>
    /// Như <see cref="TryParseSeconds"/> nhưng luôn có kết quả: đọc không được thì trả
    /// <paramref name="fallbackSeconds"/>.
    /// </summary>
    public static int ParseSecondsOrDefault(string? value, int fallbackSeconds)
        => TryParseSeconds(value) ?? fallbackSeconds;
}
