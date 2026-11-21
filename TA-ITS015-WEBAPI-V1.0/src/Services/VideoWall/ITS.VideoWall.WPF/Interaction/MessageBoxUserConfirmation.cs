using System.Windows;

namespace Module.VideoWall.WPF.Interaction;

/// <summary>
/// Author: Đạt
/// Description: Bản chạy thật của <see cref="IUserConfirmation"/> — hộp thoại Yes/No của WPF,
/// mặc định con trỏ ở No để không ai bấm Enter mà ghi thật xuống tường.
/// Đây là mảnh DUY NHẤT của luồng xác nhận có phụ thuộc WPF, nhờ vậy ViewModel test được.
/// Created date: 25/08/2026
/// </summary>
public sealed class MessageBoxUserConfirmation : IUserConfirmation
{
    public bool Confirm(string title, string message) =>
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;
}
