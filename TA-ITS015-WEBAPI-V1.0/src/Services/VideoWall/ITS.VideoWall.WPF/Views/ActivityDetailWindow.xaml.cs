using System.Windows;
using Module.VideoWall.WPF.ViewModels;

namespace Module.VideoWall.WPF.Views;

/// <summary>
/// Author: Đạt
/// Description: Hộp thoại xem trọn request/response của một bước đấu nối. Tách riêng khỏi bảng log
/// để XML thô không làm hỏng bố cục bảng chính.
/// Created date: 25/08/2026
/// </summary>
public partial class ActivityDetailWindow : Window
{
    private readonly EventTriggerLogEntry _entry;

    public ActivityDetailWindow(EventTriggerLogEntry entry)
    {
        InitializeComponent();

        _entry = entry;
        DataContext = entry;
    }

    private void OnCopy(object sender, RoutedEventArgs e)
    {
        var response = _entry.Step?.ResponseXml;
        if (string.IsNullOrWhiteSpace(response))
            return;

        Clipboard.SetText(response);
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
