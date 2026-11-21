namespace ITS.VMS.Wpf.ViewModels;

/// <summary>Một dòng nhật ký hiển thị trên form (giờ + mức + nội dung).</summary>
public sealed class LogItem
{
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>Mức: Info / Success / Warning / Error (dùng để tô màu).</summary>
    public string Level { get; init; } = "Info";

    public string Message { get; init; } = string.Empty;

    public string TimeText => Timestamp.ToString("HH:mm:ss");
}

/// <summary>Một gói tin quan sát được trên kênh, hiển thị nguyên văn để đối chiếu hợp đồng bản tin.</summary>
public sealed class PacketItem
{
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>"request" (lệnh đi) hoặc "result" (kết quả về).</summary>
    public string Type { get; init; } = "?";

    public string Json { get; init; } = string.Empty;

    public string TimeText => Timestamp.ToString("HH:mm:ss.fff");

    /// <summary>Nhãn hiển thị trên danh sách: giờ + chiều gói tin.</summary>
    public string Title => Type.Equals("request", StringComparison.OrdinalIgnoreCase)
        ? $"{TimeText}  ▲ REQUEST (BE → dịch vụ)"
        : Type.Equals("result", StringComparison.OrdinalIgnoreCase)
            ? $"{TimeText}  ▼ RESULT (dịch vụ → BE)"
            : $"{TimeText}  ? {Type}";
}
