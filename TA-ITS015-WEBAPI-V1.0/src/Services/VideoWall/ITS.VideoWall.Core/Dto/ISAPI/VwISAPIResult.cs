namespace ITS.VideoWall.Core.Dto.ISAPI
{
    /// <summary>
    /// Kết quả gọi thiết bị ISAPI, không có dữ liệu trả về.
    ///
    /// RawRequest/RawResponse giữ nguyên XML hai chiều: thông báo lỗi thật của thiết bị nằm ở
    /// subStatusCode chứ không phải statusString, nên không đọc được XML gốc thì gần như không
    /// chẩn đoán được lỗi badParameters.
    /// </summary>
    public record VwISAPIResult
    {
        public bool Success { get; init; }
        public int? HttpStatusCode { get; init; }
        public string? ErrorMessage { get; init; }
        public string? RawRequest { get; init; }
        public string? RawResponse { get; init; }

        public static VwISAPIResult Ok(int? httpStatusCode = null, string? rawRequest = null, string? rawResponse = null)
            => new()
            {
                Success = true,
                HttpStatusCode = httpStatusCode,
                RawRequest = rawRequest,
                RawResponse = rawResponse
            };

        public static VwISAPIResult Fail(string errorMessage, int? httpStatusCode = null, string? rawRequest = null, string? rawResponse = null)
            => new()
            {
                Success = false,
                ErrorMessage = errorMessage,
                HttpStatusCode = httpStatusCode,
                RawRequest = rawRequest,
                RawResponse = rawResponse
            };
    }

    /// <summary>
    /// Kết quả gọi thiết bị ISAPI, có dữ liệu trả về kiểu <typeparamref name="T"/>.
    /// </summary>
    public record VwISAPIResult<T> : VwISAPIResult
    {
        public T? Data { get; init; }

        public static VwISAPIResult<T> Ok(T data, int? httpStatusCode = null, string? rawRequest = null, string? rawResponse = null)
            => new()
            {
                Success = true,
                Data = data,
                HttpStatusCode = httpStatusCode,
                RawRequest = rawRequest,
                RawResponse = rawResponse
            };

        public static new VwISAPIResult<T> Fail(string errorMessage, int? httpStatusCode = null, string? rawRequest = null, string? rawResponse = null)
            => new()
            {
                Success = false,
                ErrorMessage = errorMessage,
                HttpStatusCode = httpStatusCode,
                RawRequest = rawRequest,
                RawResponse = rawResponse
            };
    }
}
