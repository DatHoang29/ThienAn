using System.Windows;
using Module.VideoWall.WPF.ViewModels;

namespace Module.VideoWall.WPF.Views;

/// <summary>
/// Description: Cửa sổ đối thoại quản lý danh sách nguồn tín hiệu VideoWall cục bộ
/// Created date: 13/09/2026
/// </summary>
public partial class SourceManagementDialog : Window
{
    public SourceManagementDialog(SourceManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}