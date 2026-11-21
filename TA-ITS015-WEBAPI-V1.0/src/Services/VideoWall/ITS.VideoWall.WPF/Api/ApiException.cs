namespace Module.VideoWall.WPF.Api;

/// <summary>
/// Author: Đạt
/// Description: Lỗi trả về từ TAC_WebAPI. Backend gói cả lỗi nghiệp vụ vào vỏ WResult nên
/// EnvelopeCode (200 = thành công, 401 = hết phiên) mới là nguồn sự thật, HTTP status chỉ tham khảo.
/// Created date: 25/08/2026
/// </summary>
public sealed class ApiException : Exception
{
    public ApiException(string message, int? envelopeCode = null, int? httpStatus = null)
        : base(message)
    {
        EnvelopeCode = envelopeCode;
        HttpStatus = httpStatus;
    }

    /// <summary>Mã kết quả nghiệp vụ trả về từ vỏ WResult.</summary>
    public int? EnvelopeCode { get; }

    /// <summary>Mã trạng thái HTTP phản hồi từ máy chủ.</summary>
    public int? HttpStatus { get; }

    /// <summary>Cho biết lỗi có phải do hết hạn phiên hoặc chưa đăng nhập (401) hay không.</summary>
    public bool IsUnauthorized =>
        EnvelopeCode == Dto.WResult<object>.UnauthorizedCode
        || HttpStatus == Dto.WResult<object>.UnauthorizedCode;
}
