using System.Text.RegularExpressions;

namespace Services.Vds.Shared.Domain;

/// <summary>
/// Author: KienLT
/// Description: Quy tắc biển số dùng chung: chuẩn hoá, kiểm biển Việt Nam, che một phần khi ghi log.
/// Hàm THUẦN và tất định — cùng đầu vào cho cùng kết quả, không đọc cấu hình, không I/O.
///
/// Gom vào một chỗ vì cả ba việc đều phải nhất quán: nếu nơi ghi chuẩn hoá theo cách này mà nơi so khớp
/// chuẩn hoá theo cách khác thì cùng một xe sẽ thành hai biển khác nhau.
/// Created date: 26/08/2026
/// </summary>
public static partial class PlateRules
{
    // Biển VN: 2 số tỉnh + 1–2 chữ + 4–5 số sau khi bỏ dấu chấm/gạch. Ví dụ: 60A90685, 29AB12345.
    [GeneratedRegex(@"^\d{2}[A-Z]{1,2}\d{4,5}$")]
    private static partial Regex VnPlateRegex();

    /// <summary>Bỏ khoảng trắng, dấu chấm và gạch nối, rồi in hoa.</summary>
    public static string Normalize(string? plate)
    {
        if (string.IsNullOrWhiteSpace(plate)) return string.Empty;

        return new string(plate
                .Where(c => !char.IsWhiteSpace(c) && c != '.' && c != '-')
                .ToArray())
            .ToUpperInvariant();
    }

    /// <summary>Biển hợp lệ theo mẫu Việt Nam sau khi chuẩn hoá.</summary>
    public static bool IsVietnamPlate(string? plate)
    {
        var normalized = Normalize(plate);
        return normalized.Length > 0 && VnPlateRegex().IsMatch(normalized);
    }

    /// <summary>
    /// Che giữa để ghi log: <c>60A90685</c> → <c>60A***85</c>. Chuỗi quá ngắn thì che toàn bộ.
    ///
    /// Dùng ở mọi chỗ log có biển số: log ứng dụng thường đi ra ngoài phạm vi hệ thống (file, tập trung
    /// hoá), không cần mang theo dữ liệu nhận dạng đầy đủ.
    /// </summary>
    public static string Mask(string? plate)
    {
        if (string.IsNullOrWhiteSpace(plate)) return string.Empty;

        var value = plate.Trim();
        if (value.Length <= 4) return new string('*', value.Length);

        const int keepHead = 3;
        const int keepTail = 2;
        return value[..keepHead] + new string('*', value.Length - keepHead - keepTail) + value[^keepTail..];
    }
}
