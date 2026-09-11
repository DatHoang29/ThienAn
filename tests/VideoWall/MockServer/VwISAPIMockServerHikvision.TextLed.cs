using System.Net;
using System.Threading.Tasks;

namespace Tests.Modules.VideoWall.MockServer;

public partial class VwISAPIMockServerHikvision
{
    private async Task<bool> TryHandleTextLedAsync(HttpListenerContext context, string method, string path)
    {
        var res = context.Response;

        // 9.7.9.1. Set parameters of all virtual LEDs [PUT ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED", path))
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

        // 9.7.9.2. Get parameters of all virtual LEDs [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<VirtualLEDOnWallList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <VirtualLEDOnWall>
    <id>
      1
    </id>
    <enabled>
      true
    </enabled>
    <wndOperateMode>
      uniformCoordinate
    </wndOperateMode>
    <Rect>
      <Coordinate>
        <x>
          1
        </x>
        <y>
          1
        </y>
      </Coordinate>
      <width>
        1
      </width>
      <height>
        1
      </height>
    </Rect>
    <Coordinate>
      <x>
        0
      </x>
      <y>
        0
      </y>
    </Coordinate>
    <ResolutionRect>
      <Coordinate>
        <x>
          1
        </x>
        <y>
          1
        </y>
      </Coordinate>
      <width>
        1
      </width>
      <height>
        1
      </height>
    </ResolutionRect>
    <text>
      test
    </text>
    <foregroundColor>
      <RGB>
        ff00ff
      </RGB>
    </foregroundColor>
    <backgroundColor>
      <RGB>
        ff00ff
      </RGB>
    </backgroundColor>
    <transparencyMode>
      opaque
/t
M d
    </transparencyMode>
    <moveDirection>
      left to right
    </moveDirection>
    <moveSpeed>
      1
    </moveSpeed>
    <moveMode>
      normal
    </moveMode>
    <ledType>
      text
    </ledType>
    <ledDirection>
      horizontal
    </ledDirection>
    <timeType>
      dateTime
    </timeType>
    <showWeekInTime>
      true
    </showWeekInTime>
    <dateFormat>
      yyyy-mm-dd
    </dateFormat>
    <timeFormat>
      h:mm:ss
    </timeFormat>
    <hourFormat>
      12h
    </hourFormat>
    <amFormat>
      am
    </amFormat>
    <pmFormat>
      pm
    </pmFormat>
    <clockLineFeedEnabled>
      true
    </clockLineFeedEnabled>
    <alignmentX>
      left
    </alignmentX>
    <alignmentY>
      up
    </alignmentY>
    <fontType>
      simSun
    </fontType>
    <fontSize>
      1times
    </fontSize>
    <fontDisplayMode>
      0
    </fontDisplayMode>
    <fontBold>
      true
    </fontBold>
    <backgroundPicType>
      0
    </backgroundPicType>
    <clockParam>
      <clockType>
        dialClock
      </clockType>
      <clockStyle>
        style1
      </clockStyle>
    </clockParam>
    <weatherParam>
      <weatherCondition>
        1
      </weatherCondition>
      <temperature>
""");
            return true;
        }

        // 9.7.9.3. Add all virtual LEDs [POST ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED]
        if (method == "POST" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <ResponseStatus version="1.0" xmlns="{{Ns}}">
                  <requestURL>{{path}}</requestURL>
                  <statusCode>1</statusCode>
                  <statusString>OK</statusString>
                  <subStatusCode>ok</subStatusCode>
                  <ID>1</ID>
                </ResponseStatus>
                """);
            return true;
        }

        // 9.7.9.4. Get parameters of a specified LED [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED/{SubtitlesID}]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED/{SubtitlesID}", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<VirtualLEDOnWall xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>
    1
  </id>
  <enabled>
    true
  </enabled>
  <wndOperateMode>
    uniformCoordinate
  </wndOperateMode>
  <Rect>
    <Coordinate>
      <x>
        1
      </x>
      <y>
        1
      </y>
    </Coordinate>
    <width>
      1
    </width>
    <height>
      1
    </height>
  </Rect>
  <Coordinate>
    <x>
      0
    </x>
    <y>
      0
    </y>
  </Coordinate>
  <ResolutionRect>
    <Coordinate>
      <x>
        1
      </x>
      <y>
        1
      </y>
    </Coordinate>
    <width>
      1
    </width>
    <height>
      1
    </height>
  </ResolutionRect>
  <text>
    test
  </text>
  <foregroundColor>
    <RGB>
      ff00ff
    </RGB>
  </foregroundColor>
  <backgroundColor>
    <RGB>
      ff00ff
    </RGB>
  </backgroundColor>
  <transparencyMode>
    opaque
  </transparencyMode>
  <moveDirection>
    left to right
  </moveDirection>
  <moveSpeed>
    1
  </moveSpeed>
  <moveMode>
    normal
  </moveMode>
  <ledType>
    text
  </ledType>
  <ledDirection>
    horizontal
  </ledDirection>
  <timeType>
    dateTime
  </timeType>
  <showWeekInTime>
    true
  </showWeekInTime>
  <dateFormat>
    yyyy-mm-dd
  </dateFormat>
  <timeFormat>
    h:mm:ss
  </timeFormat>
  <hourFormat>
    12h
  </hourFormat>
  <amFormat>
    am
  </amFormat>
  <pmFormat>
    pm
  </pmFormat>
  <clockLineFeedEnabled>
    true
  </clockLineFeedEnabled>
  <alignmentX>
    left
  </alignmentX>
  <alignmentY>
    up
  </alignmentY>
  <fontType>
    simSun
  </fontType>
  <fontSize>
    1times
  </fontSize>
  <fontDisplayMode>
    0
  </fontDisplayMode>
  <fontBold>
    true
  </fontBold>
  <backgroundPicType>
    0
  </backgroundPicType>
  <clockParam>
    <clockType>
      dialClock
    </clockType>
    <clockStyle>
      style1
    </clockStyle>
  </clockParam>
  <weatherParam>
    <weatherCondition>
      1
    </weatherCondition>
    <temperature>
""");
            return true;
        }

        // 9.7.9.5. Delete a specific virtual LED [DELETE ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED/{SubtitlesID}]
        if (method == "DELETE" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED/{SubtitlesID}", path))
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

        // 9.7.9.6. Set parameters of a specific virtual LED [PUT ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED/{SubtitlesID}]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED/{SubtitlesID}", path))
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

        // 9.7.9.7. Get the virtual LED configuration capability [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED/{SubtitlesID}/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED/{SubtitlesID}/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<VirtualLEDOnWall xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>
    1
  </id>
  <enabled opt="true,false">
    true
  </enabled>
  <wndOperateMode opt="uniformCoordinate,resolutionCoordinate">
    uniformCoordinate
  </wndOperateMode>
  <Rect>
    <Coordinate>
      <x min="0" max="1920">
        1
      </x>
      <y min="0" max="1920">
        1
      </y>
</Coordinate>
    </Coordinate>
    <width min="0" max="1920">
      1
    </width>
    <height min="0" max="1920">
      1
    </height>
  </Rect>
  <Coordinate>
    <x min="0" max="1920">
      0
    </x>
    <y min="0" max="1920">
      0
    </y>
  </Coordinate>
  <ResolutionRect>
    <Coordinate>
      <x min="0" max="1920">
        1
      </x>
      <y min="0" max="1920">
        1
      </y>
    </Coordinate>
    <width min="0" max="1920">
      1
    </width>
    <height min="0" max="1920">
      1
    </height>
  </ResolutionRect>
  <text max="1024">
    test
  </text>
  <foregroundColor>
    <RGB>
      1
    </RGB>
  </foregroundColor>
  <backgroundColor>
    <RGB>
      1
    </RGB>
  </backgroundColor>
  <transparencyMode opt="opaque,half-transparent,transparent">
    opaque
  </transparencyMode>
  <moveDirection opt="left to right,right to left,top to bottom,bottom to top">
    left to right
  </moveDirection>
  <moveSpeed min="1" max="4">
    1
  </moveSpeed>
  <moveMode opt="normal,smooth,static">
    normal
  </moveMode>
  <ledType opt="text,time,clock,weather">
    test
  </ledType>
  <ledDirection opt="horizontal,vertical">
    test
  </ledDirection>
  <timeType opt="timeOnly,dateTime">
    test
  </timeType>
  <showWeekInTime opt="true,false">
    true
  </showWeekInTime>
  <dateFormat opt="yyyy-mm-dd,mm-dd-yyyy,dd-mm-yyyy,xxxxYxxMxxD,xxMxxDxxxxY,xxDxxMxxxxY,yyyy/mm/dd,yy/mm/dd">
    test
  </dateFormat>
  <timeFormat opt="h:mm:ss,hh:mm:ss,hh:mm,hhHmmMssS,hhHmmM">
    test
  </timeFormat>
  <hourFormat opt="12h,24h">
    test
  </hourFormat>
  <amFormat opt="am,morning">
    test
  </amFormat>
  <pmFormat opt="pm,afternoon">
    test
  </pmFormat>
  <clockLineFeedEnabled opt="true,false">
    true
  </clockLineFeedEnabled>
  <alignmentX opt="left,middle,right">
    test
  </alignmentX>
  <alignmentY opt="up,middle,down">
    test
  </alignmentY>
  <fontType opt="simSun,simHei,kaiTi,default,custom,LiSu,custom2,custom3">
    test
  </fontType>
  <fontSize opt="0.5times,0.75times,1times,1.25times,1.5times,1.75times,2times,2.5times,3times,3.5times,4times,6times,8times">
    test
  </fontSize>
  <fontDisplayMode opt="0,1,2">
    test
  </fontDisplayMode>
  <fontBold opt="true,false">
    true
  </fontBold>
  <backgroundPicType opt="0,1,2,3">
    test
  </backgroundPicType>
  <clockParam>
    <clockType opt="dialClock,digitalClock">
      dialClock
    </clockType>
    <clockStyle opt="style1,style2,style3">
      style1
    </clockStyle>
  </clockParam>
  <weatherParam>
    <weatherCondition
opt="1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,5
4,55,56,57,58,59,60,61">
      1
    </weatherCondition>
    <temperature min="-100" max="100">
""");
            return true;
        }

        // 9.7.9.8. Get configuration capability of all virtual LEDs [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<VirtualLEDCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <virtualLEDNums>
    1
  </virtualLEDNums>
  <virtualLEDFirstHeightList>
    <virtualLEDFirstHeight>
      <resolution>
        test
      </resolution>
      <virtualLEDHeight>
        1
      </virtualLEDHeight>
    </virtualLEDFirstHeight>
  </virtualLEDFirstHeightList>
  <isSupportFontRestore>
    true
  </isSupportFontRestore>
  <fontTypeDllMaxSize>
    1
  </fontTypeDllMaxSize>
  <perWallClockSubtitlesMaxNum>
    1
  </perWallClockSubtitlesMaxNum>
  <perWallDynamicSubtitlesMaxNum>
    1
  </perWallDynamicSubtitlesMaxNum>
  <perWallVerticalSubtitlesMaxNum>
    1
  </perWallVerticalSubtitlesMaxNum>
  <dynamicSubtitlesMaxNum>
    1
  </dynamicSubtitlesMaxNum>
  <Rect>
    <MaxVertSubtitleWindowSize>
      <width>
        1
      </width>
      <height>
        1
      </height>
    </MaxVertSubtitleWindowSize>
    <MaxHoriSubtitleWindowSize>
      <width>
        1
      </width>
      <height>
        1
      </height>
    </MaxHoriSubtitleWindowSize>
    <MinVertSubtitleWindowSize>
      <width>
        1
      </width>
      <height>
        1
      </height>
    </MinVertSubtitleWindowSize>
    <MinHoriSubtitleWindowSize>
      <width>
        1
      </width>
      <height>
        1
      </height>
    </MinHoriSubtitleWindowSize>
  </Rect>
  <ResolutionRect>
    <MaxVertSubtitleWindowSize>
      <width>
        1
      </width>
      <height>
        1
</height>
      </height>
    </MaxVertSubtitleWindowSize>
    <MaxHoriSubtitleWindowSize>
      <width>
        1
      </width>
      <height>
        1
      </height>
    </MaxHoriSubtitleWindowSize>
    <MinVertSubtitleWindowSize>
      <width>
        1
      </width>
      <height>
        1
      </height>
    </MinVertSubtitleWindowSize>
    <MinHoriSubtitleWindowSize>
      <width>
        1
      </width>
      <height>
        1
      </height>
    </MinHoriSubtitleWindowSize>
  </ResolutionRect>
  <isSupportShowWeekInClockSubtitles>
    true
  </isSupportShowWeekInClockSubtitles>
</VirtualLEDCap>
""");
            return true;
        }

        return false;
    }
}
