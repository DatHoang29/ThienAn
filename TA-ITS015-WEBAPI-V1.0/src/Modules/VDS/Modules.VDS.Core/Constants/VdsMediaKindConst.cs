namespace Modules.VDS.Core.Constants;

/// <summary>
/// Author: KienLT
/// Description: Sáu nhánh media của một bản tin VDS và HẬU TỐ tên file tương ứng.
///
/// <para><b>Vì sao phải là một nguồn duy nhất.</b> Bộ mã này xuất hiện ở ba vai khác nhau và trước
/// 23/09/2026 mỗi vai tự khai một bản:</para>
/// <list type="number">
/// <item><c>VdsMediaLocator.Suffix</c> — dựng lại đường dẫn file để đọc.</item>
/// <item><c>EventQueryHandler</c> — trả danh sách nhánh có thể tra cho client, khai bằng một mảng chuỗi
/// viết thẳng. Thêm nhánh mới ở Locator mà quên mảng này thì API chi tiết sự kiện không báo nhánh đó, và
/// không có lỗi nào để lần ra.</item>
/// <item><c>Services.Vds/Features/Media/VdsMediaStorage.SuffixOf</c> — worker GHI tên file theo hậu tố.
/// Bản đó CHƯA dùng chung (worker build riêng, ngoài <c>TAC_WebAPI.sln</c>), nhưng đặt lớp này ở
/// <c>Modules.VDS.Core</c> — project mà worker đã tham chiếu — để khi gộp không phải di chuyển lần nữa.</item>
/// </list>
///
/// <para><b>Hậu tố là hợp đồng trên ĐĨA, không phải chi tiết nội bộ.</b> Worker ghi
/// <c>{yyyyMMddHHmmssfff}_{suffix}{ext}</c>, WebAPI dựng lại đúng chuỗi đó để tìm. Đổi một hậu tố là mọi
/// file đã ghi của nhánh đó thành không đọc được, và API media trả 404 vĩnh viễn cho nhánh đó.</para>
/// Created date: 23/09/2026
/// </summary>
public static class VdsMediaKindConst
{
    /// <summary>Ảnh toàn cảnh.</summary>
    public const string Full = "full";

    /// <summary>Ảnh cắt phương tiện.</summary>
    public const string Vehicle = "vehicle";

    /// <summary>Ảnh cắt biển số.</summary>
    public const string Plate = "plate";

    /// <summary>Ảnh vi phạm.</summary>
    public const string Violation = "violation";

    /// <summary>Ảnh chụp tức thời.</summary>
    public const string Snapshot = "snapshot";

    /// <summary>Đoạn video.</summary>
    public const string Clip = "clip";

    /// <summary>Hậu tố tên file của <see cref="Clip"/> — nhánh DUY NHẤT là video, các nhánh còn lại là ảnh.</summary>
    public const string ClipSuffix = "cl";

    /// <summary>
    /// Thứ tự trả ra cho client. Là danh sách nhánh CÓ THỂ tra, không cam kết file đã tồn tại.
    /// </summary>
    public static readonly IReadOnlyList<string> All = [Full, Vehicle, Plate, Violation, Snapshot, Clip];

    /// <summary>
    /// Hậu tố tên file của một nhánh. Nhận cả tên đầy đủ (<c>vehicle</c>) và chính hậu tố (<c>v</c>) vì
    /// client cũ gửi cả hai dạng.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Nhánh không thuộc sáu giá trị trên. Người gọi bắt ngoại lệ này để trả 400 thay vì đi tra đĩa — một
    /// <c>kind</c> lạ có thể là mưu toan thoát thư mục.
    /// </exception>
    public static string SuffixOf(string? kind) => (kind ?? "").Trim().ToLowerInvariant() switch
    {
        Full or "f" => "f",
        Vehicle or "v" => "v",
        Plate or "p" => "p",
        Violation or "vi" => "vi",
        Snapshot or "sn" => "sn",
        Clip or ClipSuffix => ClipSuffix,
        _ => throw new ArgumentException("Loại media không hợp lệ.", nameof(kind)),
    };
}
