using Services.Vds.Wpf.ViewModels;
using System.Windows;

namespace Services.Vds.Wpf;

/// <summary>
/// Author: KienLT
/// Description: Cửa sổ test VDS. Code-behind chỉ còn hai việc: gắn <see cref="MainViewModel"/> làm
/// DataContext và dừng bộ mô phỏng khi đóng cửa sổ. Toàn bộ trạng thái, lệnh và validate nằm trong
/// ViewModel (P9) — trước đây file này dài ~450 dòng.
/// Created date: 04/08/2026
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override async void OnClosed(EventArgs e)
    {
        // Dừng vòng lặp mô phỏng trước khi cửa sổ biến mất; App mới hạ manager + host.
        if (_viewModel.StopSimulationCommand.CanExecute(null))
            await _viewModel.StopSimulationCommand.ExecuteAsync(null);

        base.OnClosed(e);
    }
}
