namespace Module.VideoWall.WPF.ViewModels.Isapi;

/// <summary>
/// Nhóm API ISAPI, xếp theo đúng thứ tự mục của tài liệu gốc (9.7.1 → 9.7.11).
///
/// Plan (9.7.6), Scene (9.7.7), TextLed (9.7.9), Wallpaper (9.7.10) chen giữa các nhóm cũ cho khớp
/// thứ tự tài liệu — KHÔNG thêm vào cuối enum, để danh sách tab trên UI đọc theo đúng trình tự
/// người vận hành tra cứu trong 09A-api-reference.md.
/// </summary>
public enum VwIsapiGroup
{
    Board,
    Decoding,
    OutputChannel,
    SignalSource,
    VideoWall,
    Plan,
    Scene,
    Screen,
    TextLed,
    Wallpaper,
    Window
}

/// <summary>
/// Description: Phương thức mở rộng hiển thị nhãn tên nhóm API ISAPI
/// Created date: 13/09/2026
/// </summary>
public static class VwIsapiGroupExtensions
{
    /// <summary>
    /// Nhãn hiển thị trên tab. Chỉ tiếng Anh theo quy ước đã chốt — trước đây lẫn tiếng Việt
    /// ("2. Giải mã (Decoding)") gây rối khi đối chiếu với tên API trong tài liệu.
    /// </summary>
    public static string ToDisplayName(this VwIsapiGroup group) => group switch
    {
        VwIsapiGroup.Board => "1. Board",
        VwIsapiGroup.Decoding => "2. Decoding",
        VwIsapiGroup.OutputChannel => "3. Output",
        VwIsapiGroup.SignalSource => "4. Input",
        VwIsapiGroup.VideoWall => "5. Video Wall",
        VwIsapiGroup.Plan => "6. Plan",
        VwIsapiGroup.Scene => "7. Scene",
        VwIsapiGroup.Screen => "8. Screen",
        VwIsapiGroup.TextLed => "9. Text (LED)",
        VwIsapiGroup.Wallpaper => "10. Wallpaper",
        VwIsapiGroup.Window => "11. Window",
        _ => group.ToString()
    };
}
