using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Module.VideoWall.WPF.Interaction;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf;

public class XmlSyntaxHighlighterTests
{
    [Fact]
    public void BuildDocument_NullOrWhitespace_ReturnsEmptyDocument_Test()
    {
        var docNull = XmlSyntaxHighlighter.BuildDocument(null);
        Assert.NotNull(docNull);
        Assert.Empty(docNull.Blocks);

        var docEmpty = XmlSyntaxHighlighter.BuildDocument("   ");
        Assert.NotNull(docEmpty);
        Assert.Empty(docEmpty.Blocks);
    }

    [Fact]
    public void BuildDocument_XmlWithDeclarationAndComments_ProducesItalicRuns_Test()
    {
        const string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!-- Test comment -->\n<Root />";
        var doc = XmlSyntaxHighlighter.BuildDocument(xml);

        Assert.NotNull(doc);
        Assert.NotEmpty(doc.Blocks);

        var paragraph = Assert.IsType<Paragraph>(doc.Blocks.FirstBlock);
        var runs = paragraph.Inlines.OfType<Run>().ToList();

        var commentRun = runs.FirstOrDefault(r => r.Text.Contains("Test comment"));
        Assert.NotNull(commentRun);
        Assert.Equal(FontStyles.Italic, commentRun.FontStyle);
    }

    [Theory]
    [InlineData("normal", 0x16, 0xA3, 0x4A)]        // xanh lá
    [InlineData("notConnected", 0xDC, 0x26, 0x26)]  // đỏ
    public void BuildDocument_AccessStatus_ProducesBoldColorRun_Test(string status, byte r, byte g, byte b)
    {
        var xml = $"<VideoInputChannel><videoInputChannelAccessStatus>{status}</videoInputChannelAccessStatus></VideoInputChannel>";
        var doc = XmlSyntaxHighlighter.BuildDocument(xml);

        var paragraph = Assert.IsType<Paragraph>(doc.Blocks.FirstBlock);
        var run = paragraph.Inlines.OfType<Run>()
            .FirstOrDefault(x => x.Text.Equals(status, StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(run);
        Assert.Equal(FontWeights.Bold, run.FontWeight);
        Assert.Equal(Color.FromRgb(r, g, b), Assert.IsType<SolidColorBrush>(run.Foreground).Color);
    }

    [Fact]
    public void BuildDocument_UserSampleXml_ProducesExpectedTokensAndHighlights_Test()
    {
        const string sampleXml = """
            <VideoInputChannelList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
              <VideoInputChannel>
                <id>16842753</id>
                <inputPortType>HDMI</inputPortType>
                <name>Input 1-1</name>
                <videoInputChannelAccessStatus>normal</videoInputChannelAccessStatus>
              </VideoInputChannel>
              <VideoInputChannel>
                <id>16842754</id>
                <inputPortType>HDMI</inputPortType>
                <name>Input 1-2</name>
                <videoInputChannelAccessStatus>notConnected</videoInputChannelAccessStatus>
              </VideoInputChannel>
            </VideoInputChannelList>
            """;

        var doc = XmlSyntaxHighlighter.BuildDocument(sampleXml);
        Assert.NotNull(doc);

        var paragraph = Assert.IsType<Paragraph>(doc.Blocks.FirstBlock);
        var runs = paragraph.Inlines.OfType<Run>().ToList();

        Assert.Contains(runs, r => r.Text.Equals("normal", StringComparison.OrdinalIgnoreCase)
                                   && ((SolidColorBrush)r.Foreground).Color == Color.FromRgb(0x16, 0xA3, 0x4A));

        Assert.Contains(runs, r => r.Text.Equals("notConnected", StringComparison.OrdinalIgnoreCase)
                                   && ((SolidColorBrush)r.Foreground).Color == Color.FromRgb(0xDC, 0x26, 0x26));

        Assert.Contains(runs, r => r.Text.Equals("VideoInputChannelList", StringComparison.OrdinalIgnoreCase)
                                   && ((SolidColorBrush)r.Foreground).Color == Color.FromRgb(0x02, 0x84, 0xC7));
    }

    [Fact]
    public void BuildDocument_JsonRequestBody_ProducesExpectedColorHighlights_Test()
    {
        const string json = """
            {
              "id": 1,
              "name": "Default Scene",
              "isDecoding": true,
              "isFailed": false
            }
            """;

        var doc = XmlSyntaxHighlighter.BuildDocument(json);
        Assert.NotNull(doc);

        var paragraph = Assert.IsType<Paragraph>(doc.Blocks.FirstBlock);
        var runs = paragraph.Inlines.OfType<Run>().ToList();

        // Key names in purple
        Assert.Contains(runs, r => r.Text.Contains("\"name\"")
                                   && ((SolidColorBrush)r.Foreground).Color == Color.FromRgb(0x7C, 0x3A, 0xED));

        // String value in amber
        Assert.Contains(runs, r => r.Text.Contains("\"Default Scene\"")
                                   && ((SolidColorBrush)r.Foreground).Color == Color.FromRgb(0xB4, 0x53, 0x09));

        // Number in blue
        Assert.Contains(runs, r => r.Text.Equals("1")
                                   && ((SolidColorBrush)r.Foreground).Color == Color.FromRgb(0x02, 0x84, 0xC7));

        // True in green
        Assert.Contains(runs, r => r.Text.Equals("true")
                                   && ((SolidColorBrush)r.Foreground).Color == Color.FromRgb(0x16, 0xA3, 0x4A));

        // False in red
        Assert.Contains(runs, r => r.Text.Equals("false")
                                   && ((SolidColorBrush)r.Foreground).Color == Color.FromRgb(0xDC, 0x26, 0x26));
    }

    [Fact]
    public void BuildDocument_XmlRequestBody_WallWindowSample_ProducesSyntaxHighlighting_Test()
    {
        const string requestBody = """
            <WallWindow xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
              <id>1</id>
              <wndOperateMode>uniformCoordinate</wndOperateMode>
              <Rect>
                <Coordinate>
                  <x>0</x>
                  <y>0</y>
                </Coordinate>
                <width>1920</width>
                <height>1080</height>
              </Rect>
            </WallWindow>
            """;

        var doc = XmlSyntaxHighlighter.BuildDocument(requestBody);
        Assert.NotNull(doc);

        var paragraph = Assert.IsType<Paragraph>(doc.Blocks.FirstBlock);
        var runs = paragraph.Inlines.OfType<Run>().ToList();

        Assert.Contains(runs, r => r.Text.Equals("WallWindow")
                                   && ((SolidColorBrush)r.Foreground).Color == Color.FromRgb(0x02, 0x84, 0xC7));

        Assert.Contains(runs, r => r.Text.Equals("version")
                                   && ((SolidColorBrush)r.Foreground).Color == Color.FromRgb(0x7C, 0x3A, 0xED));

        Assert.Contains(runs, r => r.Text.Equals("2.0")
                                   && ((SolidColorBrush)r.Foreground).Color == Color.FromRgb(0xB4, 0x53, 0x09));
    }

    [Fact]
    public void XmlHighlightingBehavior_GetTextFromDocument_ExtractsTextCorrectly_Test()
    {
        var textNull = XmlHighlightingBehavior.GetTextFromDocument(null);
        Assert.Equal(string.Empty, textNull);

        const string sample = "<Test>123</Test>";
        var doc = XmlSyntaxHighlighter.BuildDocument(sample);
        var extracted = XmlHighlightingBehavior.GetTextFromDocument(doc);
        Assert.Equal(sample, extracted.Trim());
    }
}
