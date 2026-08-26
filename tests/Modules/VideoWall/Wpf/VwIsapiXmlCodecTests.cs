using Module.VideoWall.WPF.Api.Direct.Isapi;
using Xunit;
using WpfIsapi = Module.VideoWall.WPF.Api.Direct.Isapi;

namespace Tests.Modules.VideoWall.Wpf;

public class VwIsapiXmlCodecTests
{
    [Fact]
    public void Serialize_VwISAPIWindowRequest_ProducesCorrectXmlWithoutDeclaration()
    {
        var req = new WpfIsapi.VwISAPIWindowRequest
        {
            Id = 1,
            IdSpecified = true,
            WndOperateMode = "uniformCoordinate",
            Rect = new WpfIsapi.VwISAPIRect
            {
                Coordinate = new WpfIsapi.VwISAPICoordinate { X = 0, Y = 0 },
                Width = 1920,
                Height = 1080,
            },
            WindowMode = 0,
            WindowModeSpecified = true,
            SubWindowList = new WpfIsapi.VwISAPISubWindowList
            {
                SubWindow =
                [
                    new WpfIsapi.VwISAPISubWindow
                    {
                        Id = 1,
                        SubWindowParam = new WpfIsapi.VwISAPISubWindowParam
                        {
                            SignalMode = "video input",
                            VideoInputChannelId = "1",
                        },
                    },
                ],
            },
        };

        var xml = VwIsapiXmlCodec.Serialize(req);

        Assert.False(xml.StartsWith("<?xml"), "XML không được chứa <?xml...?> declaration");
        Assert.Contains("<WallWindow", xml);
        Assert.Contains("<wndOperateMode>uniformCoordinate</wndOperateMode>", xml);
        Assert.Contains("<x>0</x>", xml);
        Assert.Contains("<width>1920</width>", xml);
        Assert.Contains("<videoInputChannelID>1</videoInputChannelID>", xml);
    }

    [Fact]
    public void Deserialize_VwISAPIResponseStatus_ParsesSuccessfully()
    {
        var xml = """
            <ResponseStatus xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
              <requestURL>/ISAPI/DisplayDev/VideoWall/1/windows</requestURL>
              <statusCode>1</statusCode>
              <statusString>OK</statusString>
              <subStatusCode>ok</subStatusCode>
            </ResponseStatus>
            """;

        var status = VwIsapiXmlCodec.Deserialize<WpfIsapi.VwISAPIResponseStatus>(xml);

        Assert.NotNull(status);
        Assert.Equal(1, status.StatusCode);
        Assert.Equal("OK", status.StatusString);
        Assert.Equal("ok", status.SubStatusCode);
    }
}
