using CommunityToolkit.Mvvm.ComponentModel;
using ITS.VMS.Core.Messaging;

namespace ITS.VMS.Wpf.ViewModels;

/// <summary>
/// Một thiết bị trong danh sách test, sửa trực tiếp trên lưới của form.
/// Ánh xạ 1-1 với <see cref="VmsControlTarget"/> của gói tin; thêm cột Bật để chọn thiết bị gửi.
/// Author: ChuongLP
/// Created date: 22/09/2026
/// </summary>
public sealed partial class TargetItemViewModel : ObservableObject
{
    /// <summary>Có đưa thiết bị này vào gói tin khi bấm Gửi lệnh hay không.</summary>
    [ObservableProperty] private bool _enabled = true;

    [ObservableProperty] private string _equipmentId = string.Empty;

    /// <summary>"chainzone" | "dysten" (khớp hằng IntgVmsVendor, viết thường).</summary>
    [ObservableProperty] private string _vendor = "chainzone";

    [ObservableProperty] private string _ip = string.Empty;

    /// <summary>Port SDK. Để trống -> dịch vụ dùng 80.</summary>
    [ObservableProperty] private int? _port = 80;

    /// <summary>Cấu hình thiết bị dạng JSON: Width/Height cho SDK, PortApi cho kênh API.</summary>
    [ObservableProperty] private string _config = "{\"Width\":384,\"Height\":64,\"PortApi\":8080}";

    /// <summary>Đường dẫn ảnh cho send-content: tương đối (ghép StorageBasePath) hoặc HTTP URL.</summary>
    [ObservableProperty] private string _url = string.Empty;

    /// <summary>Độ sáng: 0 = tự động, 1-100 = mức sáng.</summary>
    [ObservableProperty] private int _brightness = 100;

    // ===== Kết quả lần gửi gần nhất (chỉ đọc, do form cập nhật) =====

    [ObservableProperty] private string _lastState = string.Empty;
    [ObservableProperty] private string _lastMessage = string.Empty;
    [ObservableProperty] private string _lastAt = string.Empty;

    /// <summary>Chuyển sang target của gói tin.</summary>
    public VmsControlTarget ToTarget() => new VmsControlTarget
    {
        EquipmentId = EquipmentId?.Trim(),
        Vendor = Vendor?.Trim().ToLowerInvariant(),
        Ip = Ip?.Trim(),
        Port = Port,
        Config = Config,
        Url = Url?.Trim(),
        Brightness = Brightness,
    };
}

/// <summary>
/// Một dòng kết quả trả về theo thiết bị. Tách khỏi TargetItemViewModel để giữ lại đủ chi tiết
/// (SendImageOk / SetBrightnessOk / Data) của từng lần gửi.
/// </summary>
public sealed class TargetResultItem
{
    public string EquipmentId { get; init; } = string.Empty;

    public bool Success { get; init; }

    /// <summary>Nhãn ngắn để đọc nhanh trên lưới.</summary>
    public string State => Success ? "OK" : "LỖI";

    public string SendImage { get; init; } = string.Empty;

    public string SetBrightness { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    /// <summary>Dữ liệu kèm theo (Base64 ảnh / thông số / true-false), đã cắt ngắn để hiển thị.</summary>
    public string DataPreview { get; init; } = string.Empty;
}
