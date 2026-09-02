using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Interaction;
using Module.VideoWall.WPF.ViewModels;
using Module.VideoWall.WPF.ViewModels.Isapi;
using Services.Shared.Events;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf;

public class VwIsapiResponseSummaryTests
{
    [Fact]
    public void Parse_NullOrEmpty_ReturnsHasResponseFalse_Test()
    {
        var resNull = VwIsapiResponseSummary.Parse(null);
        Assert.False(resNull.HasResponse);
        Assert.Empty(resNull.Badges);

        var resEmpty = VwIsapiResponseSummary.Parse("   ");
        Assert.False(resEmpty.HasResponse);
        Assert.Empty(resEmpty.Badges);
    }

    [Fact]
    public void Parse_VideoInputChannelList_UserSample_ExtractsCountsAndBadges_Test()
    {
        const string xml = """
            <VideoInputChannelList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
              <VideoInputChannel>
                <id>16842753</id>
                <inputPortType>HDMI</inputPortType>
                <name>Input 1-1</name>
                <videoInputChannelAccessStatus>normal</videoInputChannelAccessStatus>
                <PortInBoard>
                  <boardID>1</boardID>
                  <portID>1</portID>
                  <ipAddress>127.0.0.1</ipAddress>
                  <port>13191</port>
                </PortInBoard>
              </VideoInputChannel>
              <VideoInputChannel>
                <id>16842754</id>
                <inputPortType>HDMI</inputPortType>
                <name>Input 1-2</name>
                <videoInputChannelAccessStatus>notConnected</videoInputChannelAccessStatus>
                <PortInBoard>
                  <boardID>1</boardID>
                  <portID>2</portID>
                  <ipAddress>127.0.0.1</ipAddress>
                  <port>13191</port>
                </PortInBoard>
              </VideoInputChannel>
            </VideoInputChannelList>
            """;

        var summary = VwIsapiResponseSummary.Parse(xml);

        Assert.True(summary.HasResponse);
        Assert.Equal("VideoInputChannelList", summary.RootElement);
        Assert.Equal(2, summary.TotalItems);
        Assert.Equal(1, summary.NormalCount);
        Assert.Equal(1, summary.WarningCount);
        Assert.Equal("200 OK", summary.StatusBadge);
        Assert.False(summary.IsSuccess); // 1 notConnected -> isSuccess = false to trigger red badge accent

        Assert.Contains(summary.Badges, b => b.Contains("2 Cổng vào"));
        Assert.Contains(summary.Badges, b => b.Contains("1 normal"));
        Assert.Contains(summary.Badges, b => b.Contains("1 notConnected"));
    }

    [Fact]
    public void Parse_VideoOutputChannelList_AllNormal_ReturnsSuccessAndBadges_Test()
    {
        const string xml = """
            <VideoOutputChannelList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
              <VideoOutputChannel>
                <id>17235971</id>
                <name>Output 1</name>
                <outputPortAccessStatus>normal</outputPortAccessStatus>
              </VideoOutputChannel>
              <VideoOutputChannel>
                <id>17235972</id>
                <name>Output 2</name>
                <outputPortAccessStatus>normal</outputPortAccessStatus>
              </VideoOutputChannel>
            </VideoOutputChannelList>
            """;

        var summary = VwIsapiResponseSummary.Parse(xml);

        Assert.True(summary.HasResponse);
        Assert.Equal(2, summary.TotalItems);
        Assert.Equal(2, summary.NormalCount);
        Assert.Equal(0, summary.WarningCount);
        Assert.True(summary.IsSuccess);

        Assert.Contains(summary.Badges, b => b.Contains("2 Cổng ra"));
        Assert.Contains(summary.Badges, b => b.Contains("2 normal"));
    }

    [Fact]
    public void Parse_ResponseStatus_Success_Returns200OK_Test()
    {
        const string xml = """
            <ResponseStatus xmlns="http://www.isapi.org/ver20/XMLSchema" version="1.0">
              <requestURL>/ISAPI/System/time</requestURL>
              <statusCode>1</statusCode>
              <statusString>OK</statusString>
              <subStatusCode>ok</subStatusCode>
            </ResponseStatus>
            """;

        var summary = VwIsapiResponseSummary.Parse(xml);

        Assert.True(summary.HasResponse);
        Assert.True(summary.IsSuccess);
        Assert.Equal("200 OK", summary.StatusBadge);
        Assert.Contains(summary.Badges, b => b.Contains("Thành công"));
    }

    [Fact]
    public void Parse_ResponseStatus_Error_ReturnsInvalidOperationAndBadges_Test()
    {
        const string xml = """
            <ResponseStatus xmlns="http://www.isapi.org/ver20/XMLSchema" version="1.0">
              <requestURL>/ISAPI/DisplayDev/Video/outputs/channels/999/capabilities</requestURL>
              <statusCode>4</statusCode>
              <statusString>Invalid Operation</statusString>
              <subStatusCode>badParameters</subStatusCode>
              <description>The video output channel ID does not exist</description>
            </ResponseStatus>
            """;

        var summary = VwIsapiResponseSummary.Parse(xml);

        Assert.True(summary.HasResponse);
        Assert.False(summary.IsSuccess);
        Assert.Equal("Lỗi (Invalid Operation)", summary.StatusBadge);
        Assert.Contains(summary.Badges, b => b.Equals("badParameters"));
        Assert.Contains(summary.Badges, b => b.Contains("does not exist"));
    }

    [Fact]
    public void ConnectionViewModel_IsapiResponseChanged_UpdatesSummaryProperties_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));

        Assert.False(connection.HasIsapiResponse);
        Assert.Empty(connection.IsapiResponseBadges);

        connection.IsapiResponse = """
            <VideoInputChannelList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
              <VideoInputChannel>
                <id>16842753</id>
                <videoInputChannelAccessStatus>normal</videoInputChannelAccessStatus>
              </VideoInputChannel>
            </VideoInputChannelList>
            """;

        Assert.True(connection.HasIsapiResponse);
        Assert.Equal("200 OK", connection.IsapiResponseStatusBadge);
        Assert.True(connection.IsapiResponseSuccess);
        Assert.NotEmpty(connection.IsapiResponseBadges);
        Assert.Contains(connection.IsapiResponseBadges, b => b.Contains("1 Cổng vào"));
    }

    [Fact]
    public void Parse_BinaryImageData_ReturnsImageBadgeAndSuccess_Test()
    {
        const string binaryImageResponse = "<!-- [BINARY IMAGE DATA: image/jpeg, Size: 1,024 bytes] -->\n[data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEASABIAAD/2wBD...]";

        var summary = VwIsapiResponseSummary.Parse(binaryImageResponse);

        Assert.True(summary.HasResponse);
        Assert.True(summary.IsSuccess);
        Assert.Equal("200 OK", summary.StatusBadge);
        Assert.Equal("Image", summary.RootElement);
        Assert.Contains(summary.Badges, b => b.Contains("Ảnh JPEG"));
        Assert.Contains(summary.Badges, b => b.Contains("1,024 bytes"));
    }
}
