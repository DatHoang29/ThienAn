using System.Windows;
using ITS.Sync.Wpf.ViewModels;

namespace ITS.Sync.Wpf.Views;

/// <summary>
/// Cửa sổ chính - giao diện test đồng bộ. ViewModel được tiêm qua DI.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
