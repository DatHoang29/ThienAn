using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ITS.Toll.Wpf.ViewModels;

namespace ITS.Toll.Wpf;

/// <summary>
/// Cửa sổ test API thu phí. Code-behind chỉ gắn <see cref="MainViewModel"/> làm DataContext
/// và đảm bảo click vào ô (TextBox/CheckBox trong lưới) sẽ chọn đúng dòng.
/// Author: ChuongLP
/// Created date: 24/08/2026
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    /// <summary>
    /// Khi bất kỳ control nào trong một dòng nhận focus (click vào ô Url/checkbox...),
    /// chọn luôn dòng đó và giữ nguyên (không bị bỏ chọn khi rời lưới) để
    /// "Gọi API đang chọn" dùng đúng dòng.
    /// </summary>
    private void ApiGrid_GotFocus(object sender, RoutedEventArgs e)
    {
        var dep = e.OriginalSource as DependencyObject;
        while (dep is not null && dep is not DataGridRow)
            dep = VisualTreeHelper.GetParent(dep);

        if (dep is DataGridRow row)
            row.IsSelected = true;
    }
}
