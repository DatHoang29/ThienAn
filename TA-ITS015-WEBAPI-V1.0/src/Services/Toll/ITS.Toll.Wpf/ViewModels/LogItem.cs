namespace ITS.Toll.Wpf.ViewModels;

/// <summary>Một dòng nhật ký hiển thị trên form (giờ + mức + nội dung).</summary>
public sealed class LogItem
{
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>Mức: Info / Success / Warning / Error (dùng để tô màu).</summary>
    public string Level { get; init; } = "Info";

    public string Message { get; init; } = string.Empty;

    public string TimeText => Timestamp.ToString("HH:mm:ss");
}
