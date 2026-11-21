namespace Modules.VDS.Controllers.Media.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả tra file media — ba nhánh lỗi được TRẢ VỀ chứ không ném ngoại lệ.
///
/// <para>Lý do: đây là endpoint trả file cho thẻ ảnh của FE, ba nhánh phải giữ đúng mã HTTP hiện hữu
/// (400 sai tham số / 503 chưa cấu hình kho / 404 chưa có ảnh). Nếu handler ném <c>Oops.Oh</c> thì cả ba
/// về cùng một mã và FE mất khả năng phân biệt.</para>
/// Created date: 21/09/2026
/// </summary>
public enum MediaFileStatus
{
    /// <summary>Tìm thấy file, trả về nội dung.</summary>
    Ok = 0,

    /// <summary>Tham số <c>kind</c> không thuộc tập nhánh media cho phép.</summary>
    KindInvalid = 1,

    /// <summary>Chưa cấu hình thư mục gốc kho media.</summary>
    StorageNotConfigured = 2,

    /// <summary>Bản tin có thật nhưng file media chưa tồn tại trên kho.</summary>
    NotFound = 3,

    /// <summary>
    /// Khóa kép <c>eventId</c> + <c>detectTime</c> ngoài miền hợp lệ — xem <c>VdsEventKeyRules</c>.
    ///
    /// <para>Trả về thay vì ném để endpoint này giữ đúng một hình dạng lỗi: nhánh <c>kind</c> sai đã là
    /// <c>ProblemDetails</c> 400, khóa sai cũng là lỗi tham số nên phải cùng hình dạng, cùng mã. Ném
    /// <c>Oops.Oh</c> ở đây sẽ cho ra 400 theo khuôn của Furion — khác hình dạng với ba nhánh còn lại trên
    /// cùng một endpoint.</para>
    /// </summary>
    KeyInvalid = 4
}

/// <summary>
/// Author: KienLT
/// Description: Đường dẫn và kiểu nội dung của file media đã tra được.
/// Created date: 21/09/2026
/// </summary>
public class MediaFileOutput
{
    public MediaFileStatus Status { get; set; }
    public string? FilePath { get; set; }
    [System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public Stream? Content { get; set; }
    public string? ContentType { get; set; }

    /// <summary>
    /// Khóa localization của thông báo lỗi, chỉ dùng cho <see cref="MediaFileStatus.KeyInvalid"/>.
    ///
    /// <para>Cần mang theo vì khóa sai có hai nguyên nhân (thiếu <c>detectTime</c> / <c>eventId</c> âm) và hai
    /// câu thông báo khác nhau; ba nhánh còn lại mỗi nhánh chỉ có một câu nên controller tự biết.</para>
    /// </summary>
    public string? MessageKey { get; set; }
}
