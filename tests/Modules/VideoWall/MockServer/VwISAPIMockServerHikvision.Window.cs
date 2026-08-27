using System.Net;
using System.Threading.Tasks;

namespace Tests.Modules.VideoWall.MockServer;

public partial class VwISAPIMockServerHikvision
{
    private async Task<bool> TryHandleWindowAsync(HttpListenerContext context, string method, string path)
    {
        var res = context.Response;

        // 9.7.11.5. Get parameters configuration of a specific window [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<WallWindow xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>
    test
  </id>
  <wndOperateMode>
    uniformCoordinate
  </wndOperateMode>
<Rect>
  <Rect>
    <Coordinate>
      <x>
        1
      </x>
      <y>
        1
      </y>
    </Coordinate>
    <width min="0" max="1920">
      1
    </width>
    <height min="0" max="1920">
      1
    </height>
  </Rect>
  <Coordinate>
    <x>
      1
    </x>
    <y>
      1
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
    <width min="0" max="1920">
      1
    </width>
    <height min="0" max="1920">
      1
    </height>
  </ResolutionRect>
  <layerIdx>
    1
  </layerIdx>
  <displayWinNo>
    true
  </displayWinNo>
  <windowMode>
    1
  </windowMode>
  <wndShowMode>
    subWndMode
  </wndShowMode>
  <amplifyingSubWndNo>
    1
  </amplifyingSubWndNo>
  <wndTopKeep>
    true
  </wndTopKeep>
  <wndOpenKeep>
    true
  </wndOpenKeep>
  <SubWindowList>
    <SubWindow>
      <id>
        1
      </id>
      <SubWindowParam>
        <signalMode>
          video input
        </signalMode>
        <videoInputChannelID>
          16842753
        </videoInputChannelID>
        <streamingChannelID>
          test
        </streamingChannelID>
        <StreamInput>
          <streamInputMode>
            realtime
          </streamInputMode>
          <StreamInputRealtime>
            <durationInUnit>
              1
            </durationInUnit>
            <StreamRealtimeUnitList>
              <StreamRealtimeUnit>
                <streamType>
                  in URL
                </streamType>
                <StreamInURL>
                  <URL>
                    test
                  </URL>
                </StreamInURL>
                <StreamByDdns>
                  <DdnsServerInfo>
                    <domain>
                      ipv4
                    </domain>
                    <port>
                      80
                    </port>
                    <ddnsType>
                      test
                    </ddnsType>
                    <username>
                      test
                    </username>
                    <password>
                      test
                    </password>
                  </DdnsServerInfo>
                  <EncodeDevInfo>
                    <domain>
                      ipv4
                    </domain>
                    <port>
                      1
                    </port>
                    <transmitProtocol>
                      tcp
                    </transmitProtocol>
                    <protocol>
                      DAHUA
                    </protocol>
                    <username>
                      test
                    </username>
                    <password>
                      test
                    </password>
                    <channelMode>
                      normal
                    </channelMode>
                    <channelType>
                      main
                    </channelType>
                    <channelZero>
                      1
                    </channelZero>
                    <channelNormal>
                      1
                    </channelNormal>
                    <channelStreaming>
                      1
                    </channelStreaming>
                    <channelDistributed>
                      1
                    </channelDistributed>
                  </EncodeDevInfo>
                  <MediaGatewayInfo>
                    <enabled>
                      true
                    </enabled>
                    <domain>
                      ipv4
                    </domain>
                    <port>
                      1
                    </port>
                    <transmitProtocol>
                      tcp
                    </transmitProtocol>
                  </MediaGatewayInfo>
                </StreamByDdns>
                <StreamByDomain>
                  <EncodeDevInfo>
!
t
bj t
                    <domain>
                      ipv4
                    </domain>
                    <port>
                      1
                    </port>
                    <transmitProtocol>
                      tcp
                    </transmitProtocol>
                    <protocol>
                      DAHUA
                    </protocol>
                    <username>
                      test
                    </username>
                    <password>
                      test
                    </password>
                    <channelMode>
                      normal
                    </channelMode>
                    <channelType>
                      main
                    </channelType>
                    <channelZero>
                      1
                    </channelZero>
                    <channelNormal>
                      1
                    </channelNormal>
                    <channelStreaming>
                      1
                    </channelStreaming>
                    <channelDistributed>
                      1
                    </channelDistributed>
                  </EncodeDevInfo>
                  <MediaGatewayInfo>
                    <enabled>
                      true
                    </enabled>
                    <domain>
                      ipv4
                    </domain>
                    <port>
                      1
                    </port>
                    <transmitProtocol>
                      tcp
                    </transmitProtocol>
                  </MediaGatewayInfo>
                </StreamByDomain>
              </StreamRealtimeUnit>
            </StreamRealtimeUnitList>
          </StreamInputRealtime>
          <StreamInputPlayback>
            <playbackMode>
              file name
            </playbackMode>
            <EncodeDevInfo>
              <domain>
                ipv4
              </domain>
              <port>
                1
              </port>
              <transmitProtocol>
                tcp
              </transmitProtocol>
              <protocol>
                DAHUA
              </protocol>
              <username>
                test
              </username>
              <password>
                test
              </password>
              <channelMode>
                normal
              </channelMode>
              <channelType>
                main
              </channelType>
              <channelZero>
                1
              </channelZero>
              <channelNormal>
                1
              </channelNormal>
</c a e o a >
              <channelStreaming>
                1
              </channelStreaming>
              <channelDistributed>
                1
              </channelDistributed>
            </EncodeDevInfo>
            <fileName>
              test
            </fileName>
            <TimeRange>
              <beginTime>
                00:00:00+08:00
              </beginTime>
              <endTime>
                00:00:00+08:00
              </endTime>
            </TimeRange>
          </StreamInputPlayback>
          <streamEncryptEnable>
            true
          </streamEncryptEnable>
          <streamPassword>
            test
          </streamPassword>
        </StreamInput>
        <pictureFormat>
          BMP
        </pictureFormat>
        <signalSourceName>
          test
        </signalSourceName>
      </SubWindowParam>
    </SubWindow>
  </SubWindowList>
  <wndType>
    signalSource
  </wndType>
  <zoomEnabled>
    true
  </zoomEnabled>
  <audioEnabled>
    true
  </audioEnabled>
  <wndLockKeep>
    true
  </wndLockKeep>
  <Graphic>
    <windowRegisterID>
      0
    </windowRegisterID>
  </Graphic>
  <Subtitle>
    <moveDirection>
      left
    </moveDirection>
    <moveSpeed>
      fast
    </moveSpeed>
  </Subtitle>
</WallWindow>
""");
            return true;
        }

        // 9.7.11.6. Set parameters of a specific window [PUT ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}", path))
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

        // 9.7.11.7. Delete a specific window [DELETE ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}]
        if (method == "DELETE" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}", path))
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

        // 9.7.11.8. Bottom the window [PUT ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/bottom]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/bottom", path))
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

        // 9.7.11.13. Top the window [PUT ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/top]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/top", path))
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

        // 9.7.11.14. Get the window configuration capability of the video wall [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<WallWindowCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <windowMode opt="1,4,6,8,9,16,25,36,49,64">
    1
  </windowMode>
  <CycleCap>
    <maxCycleNum>
      0
    </maxCycleNum>
    <singleCycleMaxSignalNum>
      0
    </singleCycleMaxSignalNum>
    <duration min="0" max="10">
      0
    </duration>
    <maxMonitorNum>
      64
    </maxMonitorNum>
    <isSupportCycleCtrl>
      0
    </isSupportCycleCtrl>
  </CycleCap>
  <isSupportWinTopBottom>
    true
  </isSupportWinTopBottom>
  <isSupportPlayBack>
    true
  </isSupportPlayBack>
  <isSupportPicCapture>
    true
  </isSupportPicCapture>
  <isSupportDecodeDelay>
    true
  </isSupportDecodeDelay>
  <wndWidthAlignUint>
    1
  </wndWidthAlignUint>
  <wndHeightAlignUint>
    1
  </wndHeightAlignUint>
  <resWndWidth min="1" max="10">
    1
  </resWndWidth>
  <resWndHeight min="1" max="10">
    1
  </resWndHeight>
  <isSupportSubWndAmplify>
    true
  </isSupportSubWndAmplify>
  <isSupportFullFrame>
    true
  </isSupportFullFrame>
  <isSptMutiScreenGetSubStream>
    true
  </isSptMutiScreenGetSubStream>
  <isSptCatchStreamAlarmHint>
    true
  </isSptCatchStreamAlarmHint>
  <hisiWndWidthAlignUint>
    1
  </hisiWndWidthAlignUint>
  <hisiWndHeightAlignUint>
    1
  </hisiWndHeightAlignUint>
  <isSupportClearStreamCfg>
    true
  </isSupportClearStreamCfg>
  <subScreenWindowMode opt="1,4">
    1
  </subScreenWindowMode>
</WallWindowCap>
""");
            return true;
        }

        // 9.7.11.1. Get LED or LCD areas [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/ledArea]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/ledArea", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<LedAreaList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <LedArea>
    <id>
      1
    </id>
    <areaType>
      LED
    </areaType>
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
    <OutputChanList>
      <id>
        1
      </id>
    </OutputChanList>
  </LedArea>
</LedAreaList>
""");
            return true;
        }

        // 9.7.11.9. Get single configuration capabilities of sub-windows [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<SubWindow xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id min="1" max="10">
    1
  </id>
  <SubWindowParam>
    <signalMode opt="video input, stream id, stream setting, alarm linkage">
      video input
    </signalMode>
    <videoInputChannelID min="1" max="10">
      1
    </videoInputChannelID>
    <streamingChannelID min="1" max="10">
      test
    </streamingChannelID>
    <StreamInput>
      <streamInputMode opt="realtime,playback">
        realtime
      </streamInputMode>
      <StreamInputRealtime>
        <durationInUnit min="1" max="10">
          1
        </durationInUnit>
        <StreamRealtimeUnitList>
          <StreamRealtimeUnit>
            <streamType opt="in URL,by ddns,by domain">
              in URL
            </streamType>
            <StreamInURL>
              <URL min="1" max="10">
                test
              </URL>
            </StreamInURL>
            <StreamByDdns>
              <DdnsServerInfo>
                <domain opt="ipv4,ipv6,domain">
                  ipv4
                </domain>
                <port min="1" max="10">
                  80
                </port>
                <ddnsType opt="hiDdns,noIp,...">
                  test
                </ddnsType>
                <username min="1" max="10">
                  test
                </username>
                <password min="1" max="10">
                  test
                </password>
              </DdnsServerInfo>
              <EncodeDevInfo>
                <domain opt="ipv4,ipv6,domain">
                  ipv4
                </domain>
                <port min="1" max="10">
                  1
                </port>
                <transmitProtocol opt="tcp,udp,mcast">
                  tcp
                </transmitProtocol>
                <username min="1" max="10">
                  test
                </username>
                <password min="1" max="10">
                  test
                </password>
                <channelMode opt="normal,zero,streaming,distributed">
                  normal
                </channelMode>
                <channelZero min="1" max="10">
                  1
                </channelZero>
                <channelNormal min="1" max="10">
                  1
                </channelNormal>
                <channelStreaming min="1" max="10">
                  1
                </channelStreaming>
                <channelDistributed min="1" max="10">
                  1
                </channelDistributed>
              </EncodeDevInfo>
              <MediaGatewayInfo>
                <enabled opt="true,false">
                  true
                </enabled>
                <domain opt="ipv4,ipv6,domain">
                  ipv4
                </domain>
                <port min="1" max="10">
                  1
                </port>
                <transmitProtocol opt="tcp,udp,mcast">
                  tcp
                </transmitProtocol>
              </MediaGatewayInfo>
            </StreamByDdns>
            <StreamByDomain>
              <EncodeDevInfo>
                <domain opt="ipv4,ipv6,domain">
                  ipv4
                </domain>
                <port min="1" max="10">
                  1
                </port>
                <transmitProtocol opt="tcp,udp,mcast">
                  tcp
                </transmitProtocol>
                <username min="1" max="10">
                  test
                </username>
                <password min="1" max="10">
                  test
                </password>
                <channelMode opt="normal,zero,streaming,distributed">
                  normal
                </channelMode>
                <channelZero min="1" max="10">
                  1
                </channelZero>
                <channelNormal min="1" max="10">
                  1
                </channelNormal>
                <channelStreaming min="1" max="10">
                  1
                </channelStreaming>
                <channelDistributed min="1" max="10">
                  1
                </channelDistributed>
              </EncodeDevInfo>
              <MediaGatewayInfo>
                <enabled opt="true,false">
                  true
                </enabled>
                <domain opt="ipv4,ipv6,domain">
                  ipv4
                </domain>
                <port min="1" max="10">
                  1
                </port>
                <transmitProtocol opt="tcp,udp,mcast">
                  tcp
                </transmitProtocol>
              </MediaGatewayInfo>
            </StreamByDomain>
</StreamRealtimeUnit>
          </StreamRealtimeUnit>
        </StreamRealtimeUnitList>
      </StreamInputRealtime>
      <StreamInputPlayback>
        <playbackMode>
          file name
        </playbackMode>
        <EncodeDevInfo>
          <domain opt="ipv4,ipv6,domain">
            ipv4
          </domain>
          <port min="1" max="10">
            1
          </port>
          <transmitProtocol opt="tcp,udp,mcast">
            tcp
          </transmitProtocol>
          <username min="1" max="10">
            test
          </username>
          <password min="1" max="10">
            test
          </password>
          <channelMode opt="normal,zero,streaming,distributed">
            normal
          </channelMode>
          <channelZero min="1" max="10">
            1
          </channelZero>
          <channelNormal min="1" max="10">
            1
          </channelNormal>
          <channelStreaming min="1" max="10">
            1
          </channelStreaming>
          <channelDistributed min="1" max="10">
            1
          </channelDistributed>
        </EncodeDevInfo>
        <fileName min="1" max="10">
          test
        </fileName>
        <TimeRange>
          <beginTime>
            00:00:00+08:00
          </beginTime>
          <endTime>
            00:00:00+08:00
          </endTime>
        </TimeRange>
      </StreamInputPlayback>
      <streamEncryptEnable opt="true,false">
        true
      </streamEncryptEnable>
      <streamPassword min="1" max="10">
        test
      </streamPassword>
    </StreamInput>
    <AlarmLinkageInfoList size="10">
      <AlarmLinkageInfo>
        <alarmLinkageID min="1" max="10">
          1
        </alarmLinkageID>
      </AlarmLinkageInfo>
    </AlarmLinkageInfoList>
    <subWindowMode opt="normal,polling">
      test
    </subWindowMode>
    <hostname opt="ipv4Address,ipv6Address">
      ipv4Address
    </hostname>
    <ipv4Address min="1" max="16">
      192.168.1.1
    </ipv4Address>
    <ipv6Address min="1" max="128">
      fe80::4ba5:790e
    </ipv6Address>
    <port min="0" max="65535">
      1
    </port>
    <trackNum>
      <x min="0" max="16">
        1
      </x>
      <y min="0" max="16">
        1
      </y>
    </trackNum>
  </SubWindowParam>
</SubWindow>
""");
            return true;
        }

        // 9.7.11.10. Get parameters of decoding delay [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/decodeDelay]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/decodeDelay", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<DecodeDelayParam xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <param>
    default
  </param>
</DecodeDelayParam>
""");
            return true;
        }

        // 9.7.11.11. Get decoding delay capability [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/decodeDelay/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/decodeDelay/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<DecodeDelayParam xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <param>
    default
  </param>
</DecodeDelayParam>
""");
            return true;
        }

        // 9.7.11.12. Get the configuration capability of full-frame-rate fluent video mode [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/fullFrame/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/fullFrame/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<FullFrameParam xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <enabled opt="true,false">
    true
  </enabled>
</FullFrameParam>
""");
            return true;
        }

        // 9.7.11.15. Get the parameters configuration capability of sub-stream in multi-screen mode [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/subSteam/capabilities?format=json]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/subSteam/capabilities", path))
        {
            await WriteJsonResponseAsync(res, HttpStatusCode.OK, """
{
  "MutiScreenSubStreamCap": {
    "enabled": {
      "@opt": [
        true,
        false
      ]
    },
    "winConutLimit": {
      "@opt": [
        "1",
        "4",
        "6",
        "8",
        "9",
        "16",
        "32",
        "64"
      ]
    },
    "streamType": {
      "@opt": [
        "main",
        "sub"
      ]
    }
  }
}
""");
            return true;
        }

        // 9.7.11.16. Get the configuration parameters of the stream type for streaming when the number of windows [GET ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/subSteam?format=json]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/subSteam", path))
        {
            await WriteJsonResponseAsync(res, HttpStatusCode.OK, """
{
  "MutiScreenSubStream": {
    "enabled": true,
    "winConutLimit": "test",
    "streamType": "main"
  }
}
""");
            return true;
        }

        // 9.7.11.17. Get the pre-editing capability of video wall [GET ISAPI/DisplayDev/VideoWall/preEdit/capabilities?format=json]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/preEdit/capabilities", path))
        {
            await WriteJsonResponseAsync(res, HttpStatusCode.OK, """
{
  "wallID": {
    "@min": 0,
    "@max": 0
  },
  "operateType": {
    "@opt": [
      "start",
      "save",
      "forward",
      "back",
      "up",
      "undo",
      "quit"
    ]
  }
}
""");
            return true;
        }

        return false;
    }
}
