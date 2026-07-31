using System;
using System.Windows.Input;

namespace ITS.Sync.Wpf.Infrastructure;

/// <summary>
/// ICommand đơn giản để bind nút bấm (Start/Stop) tới phương thức trong ViewModel.
/// Có thể thay bằng CommunityToolkit.Mvvm ở bước triển khai.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();
}
