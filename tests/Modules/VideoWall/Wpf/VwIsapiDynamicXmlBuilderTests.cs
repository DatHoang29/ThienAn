using System.Collections.Generic;
using Module.VideoWall.WPF.Api.Direct.Isapi;
using Module.VideoWall.WPF.ViewModels.Isapi;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf;

public class VwIsapiDynamicXmlBuilderTests
{
    [Fact]
    public void BuildXml_GenericForm_GeneratesCorrectStructure()
    {
        var fields = new List<VwIsapiFormFieldViewModel>
        {
            new(new("id", "ID cổng xuất", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, DefaultValue: "1")),
            new(new("portType", "Loại cổng", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Enum, Options: ["HDMI", "DVI"], DefaultValue: "HDMI")),
            new(new("channelID", "Channel Path", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int)),
        };

        var xml = VwIsapiDynamicXmlBuilder.BuildXml("VideoOutputChannel", fields, version: "2.0");

        Assert.False(xml.StartsWith("<?xml"));
        Assert.Contains("<VideoOutputChannel", xml);
        Assert.Contains("version=\"2.0\"", xml);
        Assert.Contains("<id>1</id>", xml);
        Assert.Contains("<portType>HDMI</portType>", xml);
        Assert.DoesNotContain("<channelID>", xml); // PathParam không sinh vào body
    }

    [Fact]
    public void BuildXml_NestedGroupPath_GeneratesNestedElements()
    {
        var fields = new List<VwIsapiFormFieldViewModel>
        {
            new(new("id", "ID kênh vào", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, DefaultValue: "1")),
            new(new("resolution", "Độ phân giải", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, GroupPath: "OutputResolution", DefaultValue: "1920*1080")),
        };

        var xml = VwIsapiDynamicXmlBuilder.BuildXml("VideoInputChannel", fields);

        Assert.Contains("<VideoInputChannel", xml);
        Assert.Contains("<id>1</id>", xml);
        Assert.Contains("<OutputResolution>", xml);
        Assert.Contains("<resolution>1920*1080</resolution>", xml);
        Assert.Contains("</OutputResolution>", xml);
    }

    [Fact]
    public void BuildXml_HybridVideoWall_AttachesRawXmlBlocks()
    {
        var fields = new List<VwIsapiFormFieldViewModel>
        {
            new(new("id", "ID tường", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, DefaultValue: "1")),
            new(new("name", "Tên tường", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, DefaultValue: "VideoWall 1")),
        };

        var rawBlocks = new Dictionary<string, string>
        {
            ["WallOutputList"] = "<WallOutputList xmlns=\"http://www.isapi.org/ver20/XMLSchema\"><WallOutput><id>1</id><outputNo>1</outputNo></WallOutput></WallOutputList>",
            ["WallWindowList"] = "<WallWindowList xmlns=\"http://www.isapi.org/ver20/XMLSchema\"><WallWindow><id>1</id></WallWindow></WallWindowList>",
        };

        var xml = VwIsapiDynamicXmlBuilder.BuildXml("VideoWall", fields, rawBlocks);

        Assert.Contains("<VideoWall", xml);
        Assert.Contains("<name>VideoWall 1</name>", xml);
        Assert.Contains("<WallOutputList", xml);
        Assert.Contains("<outputNo>1</outputNo>", xml);
        Assert.Contains("<WallWindowList", xml);
    }

    [Fact]
    public void BuildXml_OptionalEmptyField_IsOmitted()
    {
        var fields = new List<VwIsapiFormFieldViewModel>
        {
            new(new("id", "ID tường", VwIsapiFieldKind.BodyField, VwIsapiFieldType.Int, Required: true, DefaultValue: "1")),
            new(new("ledShowMode", "LED Show Mode", VwIsapiFieldKind.BodyField, VwIsapiFieldType.String, Required: false, DefaultValue: "")),
        };

        var xml = VwIsapiDynamicXmlBuilder.BuildXml("VideoWall", fields);

        Assert.Contains("<id>1</id>", xml);
        Assert.DoesNotContain("<ledShowMode>", xml);
    }
}
