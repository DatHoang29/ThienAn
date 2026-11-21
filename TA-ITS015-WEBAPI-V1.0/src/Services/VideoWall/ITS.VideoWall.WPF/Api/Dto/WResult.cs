namespace Module.VideoWall.WPF.Api.Dto;

/// <summary>
/// Author: Đạt
/// Description: Vỏ bọc phản hồi thống nhất của TAC_WebAPI (Shared.DTO.ResponseWrapper.WResult).
/// Thành công khi Code == 200 — KHÔNG phải 0, và cũng không dựa vào HTTP status vì
/// UseUnifyResultStatusCodes gói cả lỗi nghiệp vụ vào cùng vỏ này.
/// Created date: 25/08/2026
/// </summary>
public sealed class WResult<T>
{
    /// <summary>Mã phản hồi thành công chuẩn (200).</summary>
    public const int SuccessCode = 200;

    /// <summary>Mã phản hồi lỗi xác thực (401).</summary>
    public const int UnauthorizedCode = 401;

    /// <summary>Thông điệp phản hồi từ hệ thống.</summary>
    public string? Message { get; set; }

    /// <summary>Mã trạng thái trả về (200 là thành công).</summary>
    public int Code { get; set; }

    /// <summary>Cờ đánh dấu yêu cầu thành công hay thất bại.</summary>
    public bool Succeeded { get; set; }

    /// <summary>Thời điểm xử lý và phản hồi yêu cầu.</summary>
    public DateTime Time { get; set; }

    /// <summary>Dữ liệu kết quả nghiệp vụ trả về kiểu T.</summary>
    public T? Result { get; set; }

    /// <summary>Cờ bảo mật dữ liệu phản hồi.</summary>
    public bool IsSecure { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Kết quả phân trang của backend (Shared.Infrastructure.Persistence.SqlSugar.SqlSugarPagedList).
/// Lưu ý bất đối xứng: request gửi Page/PageSize nhưng response trả về Current/Size.
/// Created date: 25/08/2026
/// </summary>
public sealed class PagedResult<T>
{
    /// <summary>Trang hiện tại (Current Page).</summary>
    public int Current { get; set; }

    /// <summary>Số lượng bản ghi trên một trang (Page Size).</summary>
    public int Size { get; set; }

    /// <summary>Tổng số bản ghi thỏa mãn điều kiện lọc.</summary>
    public int Total { get; set; }

    /// <summary>Tổng số trang kết quả.</summary>
    public int TotalPages { get; set; }

    /// <summary>Danh sách bản ghi dữ liệu của trang hiện tại.</summary>
    public List<T>? Records { get; set; }

    /// <summary>Cho biết có trang trước đó hay không.</summary>
    public bool HasPrevPage { get; set; }

    /// <summary>Cho biết có trang kế tiếp hay không.</summary>
    public bool HasNextPage { get; set; }
}

/// <summary>
/// Author: Đạt
/// Description: Tham số phân trang dùng chung cho mọi endpoint Page (Shared.Core.Utilities.BasePageInput).
/// Created date: 25/08/2026
/// </summary>
public class PageInput
{
    /// <summary>Chỉ số trang cần lấy (bắt đầu từ 1).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Số lượng bản ghi trên một trang.</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Tên trường dùng để sắp xếp dữ liệu.</summary>
    public string? Field { get; set; }

    /// <summary>Chiều sắp xếp (asc hoặc desc).</summary>
    public string? Order { get; set; }

    /// <summary>Mô tả chuỗi sắp xếp mở rộng.</summary>
    public string? DescStr { get; set; }
}
