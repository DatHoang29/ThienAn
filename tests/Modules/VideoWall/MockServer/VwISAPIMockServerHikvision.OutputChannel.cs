using System.Net;
using System.Threading.Tasks;

namespace Tests.Modules.VideoWall.MockServer;

public partial class VwISAPIMockServerHikvision
{
    private async Task<bool> TryHandleOutputChannelAsync(HttpListenerContext context, string method, string path)
    {
        var res = context.Response;

        // 9.7.3.8. Set parameters of all video output channels [PUT ISAPI/DisplayDev/Video/outputs/channels/all]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/Video/outputs/channels/all", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <ResponseStatus version="1.0" xmlns="{{Ns}}">
                  <requestURL>{{path}}</requestURL>
                  <statusCode>1</statusCode>
                  <statusString>OK</statusString>
                  <subStatusCode>ok</subStatusCode>
                </ResponseStatus>
                """);
            return true;
        }

        // 9.7.3.9. Get the configuration capability of all video output channels [GET ISAPI/DisplayDev/Video/outputs/channels/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/outputs/channels/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<VideoOutputsCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <videoOutputPortNums>
    0
  </videoOutputPortNums>
  <isSupportMultiOutputType>
    true
  </isSupportMultiOutputType>
  <isSupportMultiResolution>
    true
  </isSupportMultiResolution>
  <isSupportColorSetting>
    true
  </isSupportColorSetting>
  <isSupportWidthHeightSetting>
    true
  </isSupportWidthHeightSetting>
  <isSupportOutputIdentity>
    true
  </isSupportOutputIdentity>
  <OutputResolutionCapList>
    <OutputResolutionCap>
      <resolution>
        test
      </resolution>
      <imageWidth min="0" max="4096">
        0
      </imageWidth>
      <imageHeight min="0" max="4096">
        0
      </imageHeight>
    </OutputResolutionCap>
  </OutputResolutionCapList>
  <isSupportEDIDResolution>
    true
  </isSupportEDIDResolution>
  <isNeedWallResolutionUnanimous>
    true
  </isNeedWallResolutionUnanimous>
  <outputPortAccessType opt="display,projector,LCD,LED,bigScreen,conferenceTablet,recordHost,universal">
    display
  </outputPortAccessType>
  <outputBackgroundType opt="default,solidColor">
    default
  </outputBackgroundType>
  <outputBackgroundRGBColor>
    1
  </outputBackgroundRGBColor>
</VideoOutputsCap>
""");
            return true;
        }

        return false;
    }
}
