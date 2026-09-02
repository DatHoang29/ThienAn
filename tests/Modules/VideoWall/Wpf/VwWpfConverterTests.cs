using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Module.VideoWall.WPF.Interaction;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf;

public class VwWpfConverterTests
{
    [Fact]
    public void InverseNullToVisibilityConverter_NullOrEmpty_ReturnsVisible_Test()
    {
        var converter = new InverseNullToVisibilityConverter();

        Assert.Equal(Visibility.Visible, converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Visible, converter.Convert("", typeof(Visibility), null, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Visible, converter.Convert("   ", typeof(Visibility), null, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Visible, converter.Convert(new List<int>(), typeof(Visibility), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void InverseNullToVisibilityConverter_NonNull_ReturnsCollapsed_Test()
    {
        var converter = new InverseNullToVisibilityConverter();

        Assert.Equal(Visibility.Collapsed, converter.Convert(1, typeof(Visibility), null, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Collapsed, converter.Convert("127.0.0.1", typeof(Visibility), null, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Collapsed, converter.Convert(new List<int> { 1, 2 }, typeof(Visibility), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void NullToVisibilityConverter_NullOrEmpty_ReturnsCollapsed_Test()
    {
        var converter = new NullToVisibilityConverter();

        Assert.Equal(Visibility.Collapsed, converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Collapsed, converter.Convert("", typeof(Visibility), null, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Collapsed, converter.Convert("   ", typeof(Visibility), null, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Collapsed, converter.Convert(new List<int>(), typeof(Visibility), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void NullToVisibilityConverter_NonNull_ReturnsVisible_Test()
    {
        var converter = new NullToVisibilityConverter();

        Assert.Equal(Visibility.Visible, converter.Convert("Data", typeof(Visibility), null, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Visible, converter.Convert(100, typeof(Visibility), null, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Visible, converter.Convert(new List<string> { "item" }, typeof(Visibility), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void InverseBoolToVisibilityConverter_Convert_InvertsBoolean_Test()
    {
        var converter = new InverseBoolToVisibilityConverter();

        Assert.Equal(Visibility.Collapsed, converter.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Visible, converter.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture));
        Assert.Equal(Visibility.Visible, converter.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void BoolToStatusBackgroundConverter_Convert_ReturnsExpectedColors_Test()
    {
        var converter = new BoolToStatusBackgroundConverter();

        var successBrush = Assert.IsType<SolidColorBrush>(converter.Convert(true, typeof(Brush), null, CultureInfo.InvariantCulture));
        Assert.Equal(Color.FromRgb(0xDC, 0xFC, 0xE7), successBrush.Color);

        var errorBrush = Assert.IsType<SolidColorBrush>(converter.Convert(false, typeof(Brush), null, CultureInfo.InvariantCulture));
        Assert.Equal(Color.FromRgb(0xFE, 0xE2, 0xE2), errorBrush.Color);
    }

    [Fact]
    public void BoolToStatusForegroundConverter_Convert_ReturnsExpectedColors_Test()
    {
        var converter = new BoolToStatusForegroundConverter();

        var successBrush = Assert.IsType<SolidColorBrush>(converter.Convert(true, typeof(Brush), null, CultureInfo.InvariantCulture));
        Assert.Equal(Color.FromRgb(0x15, 0x80, 0x3D), successBrush.Color);

        var errorBrush = Assert.IsType<SolidColorBrush>(converter.Convert(false, typeof(Brush), null, CultureInfo.InvariantCulture));
        Assert.Equal(Color.FromRgb(0xB9, 0x1C, 0x1C), errorBrush.Color);
    }
}
