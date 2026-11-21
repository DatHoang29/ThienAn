using System.Net;

namespace Module.VideoWall.WPF.Api.Direct.Isapi;

/// <summary>
/// Description: Kết quả thực thi lệnh ISAPI trực tiếp tới thiết bị phần cứng.
/// Created date: 08/09/2026
/// </summary>
public record VwISAPIResult
{
    /// <summary>Cờ đánh dấu lệnh gọi ISAPI thành công.</summary>
    public bool Success { get; init; }

    /// <summary>Mã trạng thái HTTP phản hồi từ thiết bị.</summary>
    public HttpStatusCode? HttpStatusCode { get; init; }

    /// <summary>Thông báo lỗi chi tiết khi thực thi thất bại.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Nội dung gói tin thô gửi đi.</summary>
    public string? RawRequest { get; init; }

    /// <summary>Nội dung gói tin thô nhận về từ thiết bị.</summary>
    public string? RawResponse { get; init; }

    public static VwISAPIResult Ok(HttpStatusCode? statusCode = null, string? rawRequest = null, string? rawResponse = null)
        => new() { Success = true, HttpStatusCode = statusCode, RawRequest = rawRequest, RawResponse = rawResponse };

    public static VwISAPIResult Fail(string errorMessage, HttpStatusCode? statusCode = null, string? rawRequest = null, string? rawResponse = null)
        => new() { Success = false, ErrorMessage = errorMessage, HttpStatusCode = statusCode, RawRequest = rawRequest, RawResponse = rawResponse };
}

/// <summary>
/// Description: Kết quả thực thi lệnh ISAPI trực tiếp kèm dữ liệu trả về kiểu T.
/// Created date: 08/09/2026
/// </summary>
public record VwISAPIResult<T> : VwISAPIResult
{
    /// <summary>Dữ liệu kết quả deserialize từ XML phản hồi.</summary>
    public T? Data { get; init; }

    public static VwISAPIResult<T> Ok(T data, HttpStatusCode? statusCode = null, string? rawRequest = null, string? rawResponse = null)
        => new() { Success = true, Data = data, HttpStatusCode = statusCode, RawRequest = rawRequest, RawResponse = rawResponse };

    public static new VwISAPIResult<T> Fail(string errorMessage, HttpStatusCode? statusCode = null, string? rawRequest = null, string? rawResponse = null)
        => new() { Success = false, ErrorMessage = errorMessage, HttpStatusCode = statusCode, RawRequest = rawRequest, RawResponse = rawResponse };
}
