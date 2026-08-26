using System.Linq;
using Module.VideoWall.WPF.ViewModels;
using Module.VideoWall.WPF.ViewModels.Isapi;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf;

public class VwIsapiFormViewModelTests
{
    [Fact]
    public void BuildPath_ReplacesPlaceholdersCorrectly()
    {
        var preset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.11.5");
        var formVm = new VwIsapiFormViewModel(preset);

        var wallField = formVm.PathFields.First(f => f.Definition.Key == "videoWallID");
        var winField = formVm.PathFields.First(f => f.Definition.Key == "VWMWID");

        wallField.Value = "3";
        winField.Value = "7";

        var path = formVm.BuildPath();

        Assert.Equal("ISAPI/DisplayDev/VideoWall/3/windows/7", path);
    }

    [Fact]
    public void Validate_Fails_WhenRequiredFieldIsEmpty()
    {
        var preset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.3.6");
        var formVm = new VwIsapiFormViewModel(preset);

        var channelField = formVm.PathFields.First(f => f.Definition.Key == "channelID");
        channelField.Value = "";

        var isValid = formVm.Validate(out var error);

        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("bắt buộc", error);
    }

    [Fact]
    public void Validate_HybridVideoWall_RequiresConfirmation()
    {
        var preset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.5.3");
        var formVm = new VwIsapiFormViewModel(preset);

        formVm.IsVideoWallPutConfirmed = false;
        var isValid = formVm.Validate(out var error);
        Assert.False(isValid);
        Assert.Contains("tick xác nhận", error);

        formVm.IsVideoWallPutConfirmed = true;
        isValid = formVm.Validate(out error);
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void WindowDtoForm_BuildBody_GeneratesValidWallWindowXml()
    {
        var preset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.11.4");
        var formVm = new VwIsapiFormViewModel(preset);

        formVm.WindowX = 100;
        formVm.WindowY = 200;
        formVm.WindowWidth = 800;
        formVm.WindowHeight = 600;

        var xml = formVm.BuildBody(out var error);

        Assert.Null(error);
        Assert.NotNull(xml);
        Assert.Contains("<WallWindow", xml);
        Assert.Contains("<x>100</x>", xml);
        Assert.Contains("<y>200</y>", xml);
        Assert.Contains("<width>800</width>", xml);
        Assert.Contains("<height>600</height>", xml);
    }

    [Fact]
    public void WindowDtoForm_AddAndRemoveSubWindow_WorksCorrectly()
    {
        var preset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.11.4");
        var formVm = new VwIsapiFormViewModel(preset);

        Assert.Single(formVm.SubWindows);

        formVm.AddSubWindowCommand.Execute(null);
        Assert.Equal(2, formVm.SubWindows.Count);
        Assert.Equal(2, formVm.SubWindows[1].Id);

        formVm.RemoveSubWindowCommand.Execute(formVm.SubWindows[1]);
        Assert.Single(formVm.SubWindows);
    }
}
