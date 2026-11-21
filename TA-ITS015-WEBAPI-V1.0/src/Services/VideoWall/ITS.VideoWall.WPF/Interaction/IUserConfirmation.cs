namespace Module.VideoWall.WPF.Interaction;

/// <summary>
/// Author: Đạt
/// Description: Hỏi người vận hành xác nhận một hành động không thể hoàn tác.
///
/// Tách interface để ViewModel không gọi trực tiếp MessageBox: lượt đẩy thật xuống thiết bị
/// (DryRun = false) là hành vi BẮT BUỘC phải kiểm thử được, mà MessageBox thì hoặc bật hộp thoại
/// thật hoặc phải dựng UI automation. Bản chạy thật dùng <see cref="MessageBoxUserConfirmation"/>,
/// bản test dùng fake trả về sẵn Yes/No.
/// Created date: 25/08/2026
/// </summary>
public interface IUserConfirmation
{
    /// <summary>Trả về true khi người vận hành đồng ý tiếp tục.</summary>
    bool Confirm(string title, string message);
}

/// <summary>
/// Description: Converter chuyển đổi thông báo sang màu sắc trạng thái (đỏ: lỗi, xanh: thành công, cam: cảnh báo)
/// Created date: 13/09/2026
/// </summary>
public sealed class StatusColorConverter : System.Windows.Data.IValueConverter
{
    private static readonly System.Windows.Media.SolidColorBrush ErrorBrush = new(System.Windows.Media.Color.FromRgb(0xC5, 0x30, 0x30));
    private static readonly System.Windows.Media.SolidColorBrush SuccessBrush = new(System.Windows.Media.Color.FromRgb(0x27, 0x67, 0x49));
    private static readonly System.Windows.Media.SolidColorBrush WarningBrush = new(System.Windows.Media.Color.FromRgb(0xC0, 0x56, 0x21));
    private static readonly System.Windows.Media.SolidColorBrush DefaultBrush = new(System.Windows.Media.Color.FromRgb(0x2D, 0x37, 0x48));

    static StatusColorConverter()
    {
        ErrorBrush.Freeze();
        SuccessBrush.Freeze();
        WarningBrush.Freeze();
        DefaultBrush.Freeze();
    }

    public object Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not string message || string.IsNullOrWhiteSpace(message))
            return DefaultBrush;

        var lower = message.ToLowerInvariant();

        // CHỈ KHI LỖI / THẤT BẠI MỚI HIỂN THỊ MÀU ĐỎ
        if (lower.Contains("thất bại") || lower.Contains("lỗi") || lower.Contains("không thể")
            || lower.Contains("không kết nối") || lower.Contains("fail") || lower.Contains("vượt quá")
            || lower.Contains("từ chối") || message.Contains("❌"))
        {
            return ErrorBrush;
        }

        if (lower.Contains("thành công") || message.Contains("✅"))
        {
            return SuccessBrush;
        }

        if (lower.Contains("cảnh báo") || message.Contains("⚠️"))
        {
            return WarningBrush;
        }

        // Đang kết nối, đang xử lý và các thông điệp thông thường giữ màu đen mặc định
        return DefaultBrush;
    }

    public object ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new System.NotSupportedException();
}

/// <summary>
/// Description: Converter đảo ngược giá trị Boolean sang Visibility (true -> Collapsed, false -> Visible)
/// Created date: 13/09/2026
/// </summary>
public sealed class InverseBoolToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b)
            return b ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        return System.Windows.Visibility.Visible;
    }

    public object ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is System.Windows.Visibility v)
            return v != System.Windows.Visibility.Visible;
        return false;
    }
}

/// <summary>
/// Description: Converter kiểm tra null hoặc rỗng sang Visibility (null/rỗng -> Collapsed, có giá trị -> Visible)
/// Created date: 13/09/2026
/// </summary>
public sealed class NullToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is null)
            return System.Windows.Visibility.Collapsed;

        if (value is string s && string.IsNullOrWhiteSpace(s))
            return System.Windows.Visibility.Collapsed;

        if (value is System.Collections.ICollection coll && coll.Count == 0)
            return System.Windows.Visibility.Collapsed;

        return System.Windows.Visibility.Visible;
    }

    public object ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new System.NotSupportedException();
}

/// <summary>
/// Description: Converter đảo ngược kiểm tra null sang Visibility (null/rỗng -> Visible, có giá trị -> Collapsed)
/// Created date: 13/09/2026
/// </summary>
public sealed class InverseNullToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is null)
            return System.Windows.Visibility.Visible;

        if (value is string s && string.IsNullOrWhiteSpace(s))
            return System.Windows.Visibility.Visible;

        if (value is System.Collections.ICollection coll && coll.Count == 0)
            return System.Windows.Visibility.Visible;

        return System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new System.NotSupportedException();
}

/// <summary>
/// Description: Converter chuyển đổi trạng thái Boolean sang màu nền (xanh lá nếu true, đỏ nhạt nếu false)
/// Created date: 13/09/2026
/// </summary>
public sealed class BoolToStatusBackgroundConverter : System.Windows.Data.IValueConverter
{
    private static readonly System.Windows.Media.SolidColorBrush SuccessBrush = new(System.Windows.Media.Color.FromRgb(0xDC, 0xFC, 0xE7));
    private static readonly System.Windows.Media.SolidColorBrush ErrorBrush = new(System.Windows.Media.Color.FromRgb(0xFE, 0xE2, 0xE2));

    static BoolToStatusBackgroundConverter()
    {
        SuccessBrush.Freeze();
        ErrorBrush.Freeze();
    }

    public object Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b && !b)
            return ErrorBrush;
        return SuccessBrush;
    }

    public object ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new System.NotSupportedException();
}

/// <summary>
/// Description: Converter chuyển đổi trạng thái Boolean sang màu chữ (xanh đậm nếu true, đỏ đậm nếu false)
/// Created date: 13/09/2026
/// </summary>
public sealed class BoolToStatusForegroundConverter : System.Windows.Data.IValueConverter
{
    private static readonly System.Windows.Media.SolidColorBrush SuccessBrush = new(System.Windows.Media.Color.FromRgb(0x15, 0x80, 0x3D));
    private static readonly System.Windows.Media.SolidColorBrush ErrorBrush = new(System.Windows.Media.Color.FromRgb(0xB9, 0x1C, 0x1C));

    static BoolToStatusForegroundConverter()
    {
        SuccessBrush.Freeze();
        ErrorBrush.Freeze();
    }

    public object Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool b && !b)
            return ErrorBrush;
        return SuccessBrush;
    }

    public object ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new System.NotSupportedException();
}


