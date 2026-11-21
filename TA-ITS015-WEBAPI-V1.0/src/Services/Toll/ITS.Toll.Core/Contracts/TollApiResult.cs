namespace ITS.Toll.Core.Contracts;

/// <summary>
/// Bao phản hồi chuẩn cho các endpoint nhận dữ liệu TCS (dùng chung cho Worker và form WPF).
/// Author: ChuongLP
/// Created date: 12/09/2026
/// </summary>
public sealed class TollApiResult
{
    /// <summary>Thành công hay không.</summary>
    public bool Ok { get; set; }

    /// <summary>Mã HTTP tương ứng (200/202/400/500).</summary>
    public int Code { get; set; }

    /// <summary>Thông báo cho phía gọi.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Dữ liệu bổ sung (số bản ghi insert/update/delete...).</summary>
    public object? Data { get; set; }

    public static TollApiResult Success(string message, object? data = null, int code = 200) =>
        new() { Ok = true, Code = code, Message = message, Data = data };

    public static TollApiResult Fail(string message, int code = 400) =>
        new() { Ok = false, Code = code, Message = message };
}
