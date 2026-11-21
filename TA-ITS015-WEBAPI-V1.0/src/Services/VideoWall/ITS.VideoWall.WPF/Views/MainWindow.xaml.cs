using System.Windows;
using System.Windows.Controls;
using Module.VideoWall.WPF.ViewModels;

namespace Module.VideoWall.WPF.Views;

/// <summary>
/// Author: Đạt
/// Description: Cửa sổ chính với 11 tab nhóm API ISAPI + Tab 12 Thiết lập Scene + Tab 13 Kịch bản.
/// Code-behind xử lý: đọc PasswordBox (WPF không cho bind), mở hộp thoại chi tiết log.
/// Created date: 25/08/2026
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        if (!string.IsNullOrEmpty(viewModel.Connection.AdHocPassword))
        {
            AdHocPasswordInput.Password = viewModel.Connection.AdHocPassword;
            AdHocPasswordVisibleInput.Text = viewModel.Connection.AdHocPassword;
        }

        viewModel.Connection.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ConnectionViewModel.AdHocPassword))
            {
                var pwd = _viewModel.Connection.AdHocPassword ?? string.Empty;
                if (AdHocPasswordInput.Password != pwd)
                    AdHocPasswordInput.Password = pwd;
                if (AdHocPasswordVisibleInput.Text != pwd)
                    AdHocPasswordVisibleInput.Text = pwd;
            }
        };
    }

    private void OnAdHocPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            _viewModel.Connection.AdHocPassword = passwordBox.Password;
            if (AdHocPasswordVisibleInput.Text != passwordBox.Password)
                AdHocPasswordVisibleInput.Text = passwordBox.Password;
        }
    }

    private void OnAdHocPasswordVisibleTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            _viewModel.Connection.AdHocPassword = textBox.Text;
            if (AdHocPasswordInput.Password != textBox.Text)
                AdHocPasswordInput.Password = textBox.Text;
        }
    }

    private void OnTogglePasswordVisibility(object sender, RoutedEventArgs e)
    {
        if (AdHocPasswordVisibleInput.Visibility == Visibility.Visible)
        {
            AdHocPasswordInput.Password = AdHocPasswordVisibleInput.Text;
            AdHocPasswordVisibleInput.Visibility = Visibility.Collapsed;
            AdHocPasswordInput.Visibility = Visibility.Visible;
            AdHocPasswordInput.Focus();
            TogglePasswordBtn.Content = "👁";
            TogglePasswordBtn.ToolTip = "Hiện mật khẩu";
        }
        else
        {
            AdHocPasswordVisibleInput.Text = AdHocPasswordInput.Password;
            AdHocPasswordInput.Visibility = Visibility.Collapsed;
            AdHocPasswordVisibleInput.Visibility = Visibility.Visible;
            AdHocPasswordVisibleInput.Focus();
            AdHocPasswordVisibleInput.CaretIndex = AdHocPasswordVisibleInput.Text.Length;
            TogglePasswordBtn.Content = "🙈";
            TogglePasswordBtn.ToolTip = "Ẩn mật khẩu";
        }
    }

    private void OnViewActivityDetails(object sender, RoutedEventArgs e)
    {
        var entry = _viewModel.SelectedActivity;
        if (entry?.Step is null)
        {
            MessageBox.Show(
                "Dòng log này không có request/response kèm theo. Chỉ các bước Ping/Probe/SetupScene/Isapi mới có.",
                "Xem chi tiết",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var detailWindow = new ActivityDetailWindow(entry)
        {
            Owner = this,
        };

        detailWindow.ShowDialog();
    }
}
