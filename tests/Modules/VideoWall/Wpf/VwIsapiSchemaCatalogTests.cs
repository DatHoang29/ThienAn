using System.Linq;
using Module.VideoWall.WPF.ViewModels;
using Module.VideoWall.WPF.ViewModels.Isapi;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf;

public class VwIsapiSchemaCatalogTests
{
    [Fact]
    public void Presets_All31Items_HaveRegisteredSchemas()
    {
        var presets = VwIsapiPresetList.Presets;
        Assert.Equal(31, presets.Count);

        foreach (var preset in presets)
        {
            var schema = VwIsapiSchemaCatalog.GetSchema(preset.Section);
            Assert.NotNull(schema);
            Assert.Equal(preset.Section, schema.Section);
        }
    }

    [Theory]
    [InlineData(VwIsapiGroup.Board, 1)]
    [InlineData(VwIsapiGroup.Decoding, 5)]
    [InlineData(VwIsapiGroup.OutputChannel, 4)]
    [InlineData(VwIsapiGroup.SignalSource, 5)]
    [InlineData(VwIsapiGroup.VideoWall, 6)]
    [InlineData(VwIsapiGroup.Screen, 1)]
    [InlineData(VwIsapiGroup.Window, 9)]
    public void Presets_GroupCounts_MatchExpected(VwIsapiGroup group, int expectedCount)
    {
        var items = VwIsapiPresetList.Presets.Where(p => p.Group == group).ToList();
        Assert.Equal(expectedCount, items.Count);
    }

    [Fact]
    public void Schema_9_7_5_3_HasRawBlockElements()
    {
        var schema = VwIsapiSchemaCatalog.GetSchema("9.7.5.3");
        Assert.NotNull(schema);
        Assert.Equal("VideoWall", schema.RootElement);
        Assert.NotNull(schema.RawBlockElements);
        Assert.Contains("WallOutputList", schema.RawBlockElements);
        Assert.Contains("WallWindowList", schema.RawBlockElements);
    }

    [Fact]
    public void Schema_9_7_3_6_HasOptionsForPortType()
    {
        var schema = VwIsapiSchemaCatalog.GetSchema("9.7.3.6");
        Assert.NotNull(schema);
        var portTypeField = schema.Fields.FirstOrDefault(f => f.Key == "portType");
        Assert.NotNull(portTypeField);
        Assert.NotNull(portTypeField.Options);
        Assert.Contains("HDMI", portTypeField.Options);
        Assert.Contains("DVI", portTypeField.Options);
    }
}
