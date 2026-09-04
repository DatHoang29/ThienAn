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
    // Giá trị "rỗng" theo quy ước của cả hai converter: null, chuỗi trắng, và collection không phần tử.
    // Phải đi qua MemberData vì List<int> không biểu diễn được trong [InlineData].
    public static TheoryData<object?> NullishValues => new() { null, "", "   ", new List<int>() };

    public static TheoryData<object?> NonNullValues => new() { 1, "127.0.0.1", new List<int> { 1, 2 } };

    [Theory]
    [MemberData(nameof(NullishValues))]
    public void InverseNullToVisibilityConverter_Nullish_ReturnsVisible_Test(object? value)
    {
        var converter = new InverseNullToVisibilityConverter();

        var result = converter.Convert(value, typeof(Visibility), null, CultureInfo.InvariantCulture);

        Assert.Equal(Visibility.Visible, result);
    }

    [Theory]
    [MemberData(nameof(NonNullValues))]
    public void InverseNullToVisibilityConverter_NonNull_ReturnsCollapsed_Test(object? value)
    {
        var converter = new InverseNullToVisibilityConverter();

        var result = converter.Convert(value, typeof(Visibility), null, CultureInfo.InvariantCulture);

        Assert.Equal(Visibility.Collapsed, result);
    }

    [Theory]
    [MemberData(nameof(NullishValues))]
    public void NullToVisibilityConverter_Nullish_ReturnsCollapsed_Test(object? value)
    {
        var converter = new NullToVisibilityConverter();

        var result = converter.Convert(value, typeof(Visibility), null, CultureInfo.InvariantCulture);

        Assert.Equal(Visibility.Collapsed, result);
    }

    [Theory]
    [MemberData(nameof(NonNullValues))]
    public void NullToVisibilityConverter_NonNull_ReturnsVisible_Test(object? value)
    {
        var converter = new NullToVisibilityConverter();

        var result = converter.Convert(value, typeof(Visibility), null, CultureInfo.InvariantCulture);

        Assert.Equal(Visibility.Visible, result);
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
