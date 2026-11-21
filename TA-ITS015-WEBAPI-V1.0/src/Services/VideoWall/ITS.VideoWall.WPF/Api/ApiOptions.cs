namespace Module.VideoWall.WPF.Api;

/// <summary>
/// Author: Đạt
/// Description: Cấu hình kết nối tới TAC_WebAPI, nạp từ Configuration/Api.json (section "Api").
/// Created date: 25/08/2026
/// </summary>
public sealed class ApiOptions
{
    /// <summary>Tên section cấu hình trong file JSON.</summary>
    public const string SectionName = "Api";

    /// <summary>Địa chỉ BaseUrl của dịch vụ TAC_WebAPI.</summary>
    public string BaseUrl { get; set; } = "http://localhost:5005";

    /// <summary>Thời gian chờ tối đa (giây) khi gọi API.</summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Mã ngôn ngữ mặc định (ví dụ: vi-VN).</summary>
    public string Language { get; set; } = "vi-VN";
}
