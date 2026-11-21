namespace ITS.Toll.Core.Configuration;

/// <summary>
/// Cấu hình MỘT API thu phí (ETC) cần . Toàn bộ có thể chỉnh động trên form.
/// Author: ChuongLP
/// Created date: 24/08/2026
/// </summary>
public sealed class TollApiEndpoint
{
    /// <summary>Mã ngắn để tham chiếu (vd: "outSummary").</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Tên hiển thị trên form (vd: "Tổng hợp vé ra").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn API. Có thể là URL tuyệt đối
    /// (vd: http://10.10.9.223:9089/api/tcs23quanly/mdsmasterdatatcs/alloutsummaryticket)
    /// hoặc tương đối (khi đó sẽ ghép với <see cref="TollOptions.BaseUrl"/>).
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// True = API cần truyền khoảng thời gian FromDate/ToDate (query string);
    /// False = chỉ GET, không tham số.
    /// </summary>
    public bool RequiresDateRange { get; set; }

    /// <summary>Tên tham số ngày bắt đầu trên query string (mặc định "FromDate").</summary>
    public string DateFromParam { get; set; } = "FromDate";

    /// <summary>Tên tham số ngày kết thúc trên query string (mặc định "ToDate").</summary>
    public string DateToParam { get; set; } = "ToDate";

    /// <summary>
    /// Loại dữ liệu để nhận biết bảng đích khi lưu DB sau này
    /// (vd: "TransactionIn" / "TransactionOut" / "OutSummary" / "Master").
    /// Hiện chỉ dùng để ghi log; phần lưu DB chưa xây dựng.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Có đưa API này vào lượt "gọi tất cả" hay không.</summary>
    public bool Enabled { get; set; } = true;
}
