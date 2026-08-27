using System.Net;
using System.Threading.Tasks;

namespace Tests.Modules.VideoWall.MockServer;

public partial class VwISAPIMockServerHikvision
{
    private async Task<bool> TryHandleDecodingAsync(HttpListenerContext context, string method, string path)
    {
        var res = context.Response;

        // 9.7.2.2. Get network pre-monitor parameters of a video wall [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/nPreMonitor]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/nPreMonitor", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<WallNPreMonitor xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>
    1
  </id>
  <resolution opt="1080*720P,1920*1080P,...">
    test
  </resolution>
  <frameRate opt="15,20,25,...">
    1
  </frameRate>
  <bitRateType opt="CBR,VBR">
    test
  </bitRateType>
  <bitRate opt="8,16,32,...">
""");
            return true;
        }

        // 9.7.2.3. Set network pre-monitor parameters of a video wall [PUT ISAPI/DisplayDev/VideoWall/{videoWallID}/nPreMonitor]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/nPreMonitor", path))
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

        // 9.7.2.4. Get sub window configuration capability [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/param/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/param/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<SubWindowParamCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <enabledAudio opt="true,fasle">
    true
  </enabledAudio>
  <rotateAngle min="0" max="270">
    1
  </rotateAngle>
  <borderEnabled opt="true,fasle">
    true
  </borderEnabled>
  <borderWidth min="0" max="100">
    1
  </borderWidth>
  <borderColor opt="red,orange,yellow,green,blue,purple,black">
    red
  </borderColor>
  <flashEnabled opt="true,fasle">
    true
  </flashEnabled>
  <flashDurationTime min="0" max="100">
    1
  </flashDurationTime>
  <flashOnTime min="0" max="100">
    1
  </flashOnTime>
  <flashOffTime min="0" max="100">
    1
  </flashOffTime>
  <rolateOSD opt="true,fasle">
    true
  </rolateOSD>
</SubWindowParamCap>
""");
            return true;
        }

        // 9.7.2.9. Get sub-board stream exporting configurations [GET ISAPI/DisplayDev/VideoWall/DecodeMgr/BoardStreamExportCfg?format=json]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/DecodeMgr/BoardStreamExportCfg", path))
        {
            await WriteJsonResponseAsync(res, HttpStatusCode.OK, """
{
  "enabled": "true"
}
""");
            return true;
        }

        // 9.7.2.10. Set sub-board stream exporting configurations [PUT ISAPI/DisplayDev/VideoWall/DecodeMgr/BoardStreamExportCfg?format=json]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/DecodeMgr/BoardStreamExportCfg", path))
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

        // 9.7.2.11. Get capability of default decoding delay parameters [GET ISAPI/DisplayDev/VideoWall/DecodeMgr/DefaultDecodeDelayParams/capabilities?format=json]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/DecodeMgr/DefaultDecodeDelayParams/capabilities", path))
        {
            await WriteJsonResponseAsync(res, HttpStatusCode.OK, """
{
  "DefaultDecodeDelayParamsCap": {
    "defaultDecodeDelayParam": {
      "@opt": [],
      "@def": "mostRealTime"
    }
  }
}
""");
            return true;
        }

        // 9.7.2.12. Get default decoding delay parameters [GET ISAPI/DisplayDev/VideoWall/DecodeMgr/DefaultDecodeDelayParams?format=json]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/DecodeMgr/DefaultDecodeDelayParams", path))
        {
            await WriteJsonResponseAsync(res, HttpStatusCode.OK, """
{
  "defaultDecodeDelayParam": "mostRealTime"
}
""");
            return true;
        }

        // 9.7.2.13. Set default decoding delay parameters [PUT ISAPI/DisplayDev/VideoWall/DecodeMgr/DefaultDecodeDelayParams?format=json]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/DecodeMgr/DefaultDecodeDelayParams", path))
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

        // 9.7.2.14. Get network pre-monitoring parameters of all video walls [GET ISAPI/DisplayDev/VideoWall/nPreMonitor]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/nPreMonitor", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<WallNPreMonitorList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <WallNPreMonitor version="2.0">
    <id>
      1
    </id>
    <resolution>
      test
    </resolution>
    <frameRate>
      1
    </frameRate>
    <bitRateType>
      test
    </bitRateType>
    <bitRate>
""");
            return true;
        }

        // 9.7.2.15. Set network pre-monitoring parameters of all video walls [PUT ISAPI/DisplayDev/VideoWall/nPreMonitor]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/nPreMonitor", path))
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

        // 9.7.2.16. Get capability of network pre-monitoring parameters of video wall [GET ISAPI/DisplayDev/VideoWall/nPreMonitor/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/nPreMonitor/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<WallNPreMonitorListCap xmlns="http://www.isapi.org/ver20/XMLSchema" size="10" version="2.0">
  <WallNPreMonitor>
    <id min="1" max="10">
      1
    </id>
    <resolution opt="1080*720P,1920*1080P,...">
      1080*720P
    </resolution>
    <frameRate opt="15,20,25,...">
      15
    </frameRate>
    <bitRateType opt="CBR,VBR">
      CBR
    </bitRateType>
    <bitRate opt="8,16,32,...">
      8
    </bitRate>
    <maxBitRate opt="128,256,...">
      1
    </maxBitRate>
    <intervalFrameI min="1" max="10">
      1
    </intervalFrameI>
    <maxScreenRow>
      1
    </maxScreenRow>
    <maxScreenColumn>
      1
    </maxScreenColumn>
    <maxScreenCount>
      1
    </maxScreenCount>
  </WallNPreMonitor>
</WallNPreMonitorListCap>
""");
            return true;
        }

        return false;
    }
}
