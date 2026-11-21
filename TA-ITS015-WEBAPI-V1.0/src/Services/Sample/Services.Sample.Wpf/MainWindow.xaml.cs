using System.Windows;
using Services.Sample.Wpf.ViewModels;

namespace Services.Sample.Wpf;

/// <summary>
/// Author: TAC Team
/// Description: Cửa sổ operator tối thiểu để gửi dữ liệu demo.
/// Created date: 16/07/2026
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
