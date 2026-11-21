using System.Text.Json;
using System.Text.Json.Serialization;

namespace Module.VideoWall.WPF.Api;

/// <summary>
/// Author: Đạt
/// Description: Cấu hình System.Text.Json khớp với backend: tên trường camelCase, enum truyền
/// dạng SỐ (backend bật EnumToNumber), bỏ qua trường null khi gửi để payload gọn.
/// Created date: 25/08/2026
/// </summary>
public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Định dạng dễ đọc, dùng cho hộp thoại xem chi tiết request/response.</summary>
    public static readonly JsonSerializerOptions Pretty = new(Options)
    {
        WriteIndented = true,
    };
}
