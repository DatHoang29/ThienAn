using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ITS.VMS.Wpf.ViewModels;

namespace ITS.VMS.Wpf;

/// <summary>
/// Cửa sổ test luồng điều khiển VMS. Code-behind chỉ gắn <see cref="MainViewModel"/> làm DataContext
/// và đảm bảo click vào ô trong lưới sẽ chọn đúng dòng.
/// Author: ChuongLP
/// Created date: 22/09/2026
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel, TelemetryViewModel telemetryViewModel)
    {
        InitializeComponent();

        // Tab "Điều khiển thiết bị" dùng DataContext của cửa sổ; tab "Telemetry tự động" có VM riêng
        // để 2 luồng độc lập, tắt/bật cái này không ảnh hưởng cái kia.
        DataContext = viewModel;
        TelemetryTab.DataContext = telemetryViewModel;
    }

    /// <summary>
    /// Khi bất kỳ control nào trong một dòng nhận focus (click vào ô Ip/Url/checkbox...),
    /// chọn luôn dòng đó và giữ nguyên để nút "Xóa dòng đang chọn" dùng đúng dòng.
    /// </summary>
    private void TargetGrid_GotFocus(object sender, RoutedEventArgs e)
    {
        var dep = e.OriginalSource as DependencyObject;
        while (dep is not null && dep is not DataGridRow)
            dep = VisualTreeHelper.GetParent(dep);

        if (dep is DataGridRow row)
            row.IsSelected = true;
    }
}
