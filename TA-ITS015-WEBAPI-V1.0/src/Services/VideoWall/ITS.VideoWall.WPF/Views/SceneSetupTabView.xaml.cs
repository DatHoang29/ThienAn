using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Module.VideoWall.WPF.Views;

/// <summary>
/// Description: View hiển thị tab thiết lập kịch bản hiển thị, bố cục màn hình và kéo thả nguồn tín hiệu
/// Created date: 13/09/2026
/// </summary>
public partial class SceneSetupTabView : UserControl
{
    private Point _dragStartPoint;

    public SceneSetupTabView()
    {
        InitializeComponent();
    }

    private void ProbeInputsGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void ProbeInputsGrid_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is DataGrid dg && dg.SelectedItem != null)
        {
            var mousePos = e.GetPosition(null);
            var diff = _dragStartPoint - mousePos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                DragDrop.DoDragDrop(dg, dg.SelectedItem, DragDropEffects.Copy);
            }
        }
    }
}
