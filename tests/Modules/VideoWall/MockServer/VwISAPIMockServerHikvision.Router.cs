using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Modules.VideoWall.MockServer;

/// <summary>
/// Author: Đạt
/// Description: Bộ điều phối định tuyến (Router) ISAPI cho MockServer Hikvision.
///              So khớp chính xác theo (Method, URL Template Segment Matcher), ngăn chặn hoàn toàn
///              tình trạng cướp route âm thầm giữa các API có chuỗi con trùng nhau.
/// Created date: 27/08/2026
/// </summary>
public partial class VwISAPIMockServerHikvision
{
    private static readonly byte[] SampleJpegBytes =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01, 0x00, 0x48,
        0x00, 0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01, 0x00, 0x01, 0x01, 0x01, 0x11,
        0x00, 0xFF, 0xC4, 0x00, 0x1F, 0x00, 0x00, 0x01, 0x05, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
        0x0A, 0x0B, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00, 0xBF, 0x00, 0xFF, 0xD9
    ];

    /// <summary>
    /// So khớp đường dẫn URL theo mẫu template hỗ trợ placeholder {param}.
    /// </summary>
    internal static bool MatchRoute(string pattern, string path, out Dictionary<string, string> parameters)
    {
        parameters = [];
        var patternSegments = pattern.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var pathSegments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (patternSegments.Length != pathSegments.Length)
            return false;

        for (int i = 0; i < patternSegments.Length; i++)
        {
            var pSeg = patternSegments[i];
            var uSeg = pathSegments[i];

            if (pSeg.StartsWith('{') && pSeg.EndsWith('}'))
            {
                var key = pSeg[1..^1];
                parameters[key] = uSeg;
                continue;
            }

            if (!string.Equals(pSeg, uSeg, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    internal static bool MatchRoute(string pattern, string path) => MatchRoute(pattern, path, out _);

    /// <summary>
    /// Điều phối xử lý chính xác từng endpoint ISAPI. Trả về true nếu request được thụ lý, false nếu rơi vào fallback.
    /// </summary>
    private async Task<bool> DispatchIsapiRouteAsync(HttpListenerContext context, string method, string path)
    {
        var req = context.Request;
        var res = context.Response;

        // ═════════════════════════════════════════════════════════════════════
        // 1. SECURITY & CAPABILITIES
        // ═════════════════════════════════════════════════════════════════════

        // M2: Device Info
        if (method == "GET" && MatchRoute("ISAPI/System/deviceInfo", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
                <DeviceInfo xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
                  <deviceName>DS-C30S-S11</deviceName>
                  <deviceID>1</deviceID>
                  <model>DS-C30S-S11</model>
                  <serialNumber>DS-C30S-S1120260101MOCK00001</serialNumber>
                  <macAddress>00:11:22:33:44:55</macAddress>
                  <firmwareVersion>V2.5.0</firmwareVersion>
                  <firmwareReleasedDate>2026-01-01</firmwareReleasedDate>
                  <hardwareVersion>0x0</hardwareVersion>
                  <deviceStatus>normal</deviceStatus>
                </DeviceInfo>
                """);
            return true;
        }

        // M2: System Time
        if (method == "GET" && MatchRoute("ISAPI/System/time", path))
        {
            var now = DateTime.Now;
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $"""
                <Time xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
                  <timeMode>NTP</timeMode>
                  <localTime>{now:yyyy-MM-ddTHH:mm:sszzz}</localTime>
                  <timeZone>CST-7:00:00</timeZone>
                </Time>
                """);
            return true;
        }

        // M2: Serial Ports
        if (method == "GET" && MatchRoute("ISAPI/System/Serial/ports", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
                <SerialPortList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
                  <SerialPort>
                    <id>1</id>
                    <serialPortType>RS485</serialPortType>
                    <baudRate>9600</baudRate>
                    <dataBits>8</dataBits>
                    <parityType>none</parityType>
                    <stopBits>1</stopBits>
                  </SerialPort>
                </SerialPortList>
                """);
            return true;
        }

        // M2: Serial Ports Capabilities
        if (method == "GET" && MatchRoute("ISAPI/System/Serial/ports/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
                <SerialPortCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
                  <baudRate opt="9600,19200,38400,57600,115200" />
                  <dataBits opt="5,6,7,8" />
                  <parityType opt="none,even,odd" />
                  <stopBits opt="1,2" />
                </SerialPortCap>
                """);
            return true;
        }

        // A. GET /ISAPI/Security/userCheck
        if (method == "GET" && MatchRoute("ISAPI/Security/userCheck", path))
        {
            UserCheckCallCount++;
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <userCheck version="2.0" xmlns="{{Ns}}">
                  <statusValue>200</statusValue>
                  <statusString>OK</statusString>
                  <isRiskPassword>false</isRiskPassword>
                  <isActivated>true</isActivated>
                </userCheck>
                """);
            return true;
        }

        // B.1. GET /ISAPI/DisplayDev/capabilities
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/capabilities", path))
        {
            GetCapabilitiesCallCount++;
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <DisplayCap version="2.0" xmlns="{{Ns}}">
                  <isSupportScreenCtrl>true</isSupportScreenCtrl>
                  <isSupportVideoWallOperate>true</isSupportVideoWallOperate>
                  <isSupportVideoWall>true</isSupportVideoWall>
                  <VideoCap>
                    <VideoInputsCap>
                      <videoInputPortNums>24</videoInputPortNums>
                      <isSupportColorSetting>true</isSupportColorSetting>
                      <isSupportCutOffSetting>true</isSupportCutOffSetting>
                      <isSupportPictureCapture>true</isSupportPictureCapture>
                    </VideoInputsCap>
                    <VideoOutputsCap>
                      <videoOutputPortNums>24</videoOutputPortNums>
                      <isSupportMultiOutputType>true</isSupportMultiOutputType>
                      <isSupportMultiResolution>true</isSupportMultiResolution>
                      <resolutionCoordinateX min="0" max="65535"/>
                      <resolutionCoordinateY min="0" max="65535"/>
                    </VideoOutputsCap>
                    <VideoStreamingCap>
                      <streamingNums>2048</streamingNums>
                      <isSupportURL>true</isSupportURL>
                      <isSupportIPAddress>true</isSupportIPAddress>
                    </VideoStreamingCap>
                  </VideoCap>
                  <VideoWallCap>
                    <maxWallNums>8</maxWallNums>
                    <maxWindowNums>512</maxWindowNums>
                    <baseOutputSize>1920</baseOutputSize>
                    <isSupportScene>{{(IsSupportScene ? "true" : "false")}}</isSupportScene>
                    <isSupportPlan>true</isSupportPlan>
                    <isSupportRoam>true</isSupportRoam>
                    <isSupportBaseMap>true</isSupportBaseMap>
                    <isSupportVirtualLED>true</isSupportVirtualLED>
                  </VideoWallCap>
                </DisplayCap>
                """);
            return true;
        }

        // B.1.5. GET /ISAPI/System/Serial/capabilities
        if (method == "GET" && MatchRoute("ISAPI/System/Serial/capabilities", path))
        {
            GetSerialCapabilitiesCallCount++;
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <SerialCap version="2.0" xmlns="{{Ns}}">
                  <isSupportDeviceInfo>{{(IsSupportSerialTransparent ? "true" : "false")}}</isSupportDeviceInfo>
                  <isSupportSerialTransparent>{{(IsSupportSerialTransparent ? "true" : "false")}}</isSupportSerialTransparent>
                </SerialCap>
                """);
            return true;
        }

        // B.2. GET /ISAPI/DisplayDev/VideoWall/capabilities
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/capabilities", path))
        {
            GetCapabilitiesCallCount++;
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <VideoWallCap version="2.0" xmlns="{{Ns}}">
                  <maxWallNums>8</maxWallNums>
                  <maxWindowNums>512</maxWindowNums>
                  <maxSceneNums>{{MaxSceneNums}}</maxSceneNums>
                  <baseOutputSize>1920</baseOutputSize>
                  <isSupportScene>{{(IsSupportScene ? "true" : "false")}}</isSupportScene>
                </VideoWallCap>
                """);
            return true;
        }

        // 9.7.1.4. GET /ISAPI/System/Board/status/capabilities
        if (method == "GET" && MatchRoute("ISAPI/System/Board/status/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <BoardStatusCap version="2.0" xmlns="{{Ns}}">
                  <isSupportSubBoardStatus>true</isSupportSubBoardStatus>
                </BoardStatusCap>
                """);
            return true;
        }

        // 9.7.3.7. GET /ISAPI/DisplayDev/Video/outputs/channels/{channelID}/capabilities
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/outputs/channels/{channelID}/capabilities", path, out var capParams))
        {
            var chanIdStr = capParams.GetValueOrDefault("channelID");
            if (string.Equals(chanIdStr, "all", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!IsValidOutputChannel(chanIdStr, out _))
            {
                await WriteXmlResponseAsync(res, HttpStatusCode.NotFound, $$"""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <ResponseStatus version="1.0" xmlns="{{Ns}}">
                      <requestURL>{{path}}</requestURL>
                      <statusCode>4</statusCode>
                      <statusString>Invalid Operation</statusString>
                      <subStatusCode>badParameters</subStatusCode>
                      <description>The video output channel ID does not exist</description>
                    </ResponseStatus>
                    """);
                return true;
            }

            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <OutputResolutionListCap version="2.0" xmlns="{{Ns}}">
                  <OutputResolutionCap>
                    <resolution>1920*1080@60HZ</resolution>
                  </OutputResolutionCap>
                </OutputResolutionListCap>
                """);
            return true;
        }

        // ═════════════════════════════════════════════════════════════════════
        // 2. DECODING (9.7.2.*)
        // ═════════════════════════════════════════════════════════════════════

        // 9.7.2.1. GET /ISAPI/DisplayDev/decoingDevice/status
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/decoingDevice/status", path))
        {
            await WriteJsonResponseAsync(res, HttpStatusCode.OK, """
                {
                  "DevCaseStatus": {
                    "height": "4.5U",
                    "row": 8,
                    "col": 2
                  },
                  "MainBoardStatusList": [
                    {
                      "ID": 1,
                      "row": 1,
                      "col": 2,
                      "runTime": 12444,
                      "CPUUtilization": 0,
                      "memoryUtilization": 36,
                      "status": "normal"
                    }
                  ],
                  "BackplaneStatusList": [
                    {
                      "ID": 1,
                      "backplaneTemperature": 60
                    }
                  ],
                  "SubBoardStatusList": [
                    { "ID": 1, "row": 3, "col": 1, "status": "normal", "subBoardType": "input" },
                    { "ID": 2, "row": 4, "col": 1, "status": "normal", "subBoardType": "input" },
                    { "ID": 7, "row": 3, "col": 2, "status": "normal", "subBoardType": "output" },
                    { "ID": 8, "row": 4, "col": 2, "status": "normal", "subBoardType": "output" }
                  ],
                  "SubBoardInterfaceList": [
                    { "ID": 1, "subBoardInterfaceType": "HDMI", "outputPortLinkStatus": "notconnect" },
                    { "ID": 2, "subBoardInterfaceType": "HDMI", "outputPortLinkStatus": "notconnect" },
                    { "ID": 3, "subBoardInterfaceType": "HDMI", "outputPortLinkStatus": "connected" },
                    { "ID": 4, "subBoardInterfaceType": "HDMI", "outputPortLinkStatus": "connected" }
                  ]
                }
                """);
            return true;
        }

        // 9.7.2.5. PUT /ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/start
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/start", path))
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

        // 9.7.2.7. PUT /ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/stop
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/stop", path))
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

        // 9.7.2.6. GET /ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/status
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/status", path, out var subStatusParams))
        {
            var vwmwid = subStatusParams.TryGetValue("VWMWID", out var w) ? w : "33554433";
            var vwswid = subStatusParams.TryGetValue("VWSWID", out var s) ? s : "1";
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <WallWindowStatusList xmlns="{{Ns}}" version="2.0">
                  <WallWindowStatus>
                    <id>{{vwmwid}}</id>
                    <windowMode>1</windowMode>
                    <SubWinStatusList>
                      <SubWinStatus>
                        <id>{{vwswid}}</id>
                        <isLinked>true</isLinked>
                        <isDecoding>true</isDecoding>
                        <isDecodingEnabled>true</isDecodingEnabled>
                        <imageWidth>1920</imageWidth>
                        <imageHeight>1080</imageHeight>
                        <videoFPS>25</videoFPS>
                        <streamRate>4096</streamRate>
                        <videoType>H.264</videoType>
                        <wndDecodeType>dynamic</wndDecodeType>
                        <SubWindowParam>
                          <signalMode>video input</signalMode>
                          <videoInputChannelID>16842753</videoInputChannelID>
                        </SubWindowParam>
                      </SubWinStatus>
                    </SubWinStatusList>
                  </WallWindowStatus>
                </WallWindowStatusList>
                """);
            return true;
        }

        // 9.7.2.8. GET /ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/status
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/status", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <WallWindowStatusList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
                  <WallWindowStatus>
                    <id>33554433</id>
                    <windowMode>1</windowMode>
                    <SubWinStatusList>
                      <SubWinStatus>
                        <id>1</id>
                        <isLinked>true</isLinked>
                        <isDecoding>true</isDecoding>
                        <isDecodingEnabled>true</isDecodingEnabled>
                        <imageWidth>1920</imageWidth>
                        <imageHeight>1080</imageHeight>
                        <videoFPS>25</videoFPS>
                        <streamRate>4096</streamRate>
                        <videoType>H.264</videoType>
                        <wndDecodeType>dynamic</wndDecodeType>
                      </SubWinStatus>
                    </SubWinStatusList>
                  </WallWindowStatus>
                </WallWindowStatusList>
                """);
            return true;
        }

        // ═════════════════════════════════════════════════════════════════════
        // 3. OUTPUT CHANNELS (9.7.3.*)
        // ═════════════════════════════════════════════════════════════════════

        // 9.7.3.1. GET /ISAPI/DisplayDev/Audio/outputs/channels
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Audio/outputs/channels", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <AudioOutputChannelList version="2.0" xmlns="{{Ns}}">
                  <AudioOutputChannel>
                    <id>1</id>
                    <portType>lineOut</portType>
                    <name>Audio Output 1</name>
                    <enabled>true</enabled>
                  </AudioOutputChannel>
                </AudioOutputChannelList>
                """);
            return true;
        }

        // 9.7.3.2. PUT /ISAPI/DisplayDev/Audio/outputs/channels/{channelID}
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/Audio/outputs/channels/{channelID}", path))
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

        // 9.7.3.4. GET /ISAPI/DisplayDev/Video/outputs/channels
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/outputs/channels", path))
        {
            GetOutputChannelsCallCount++;
            var status1 = NotConnectedOutputChannels.Contains(17235971) ? "notConnected" : "normal";
            var status2 = NotConnectedOutputChannels.Contains(17235972) ? "notConnected" : "normal";

            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <VideoOutputChannelList version="2.0" xmlns="{{Ns}}">
                  <VideoOutputChannel>
                    <id>17235971</id>
                    <portType>HDMI</portType>
                    <timeSequenceMode>standard</timeSequenceMode>
                    <name>Output 7-3</name>
                    <OutputResolution>
                      <resolution>1920*1080@60HZ</resolution>
                      <imageWidth>0</imageWidth>
                      <imageHeight>0</imageHeight>
                    </OutputResolution>
                    <PortInBoard>
                      <boardID>7</boardID>
                      <portID>3</portID>
                      <ipAddress>10.10.9.236</ipAddress>
                      <port>13191</port>
                    </PortInBoard>
                    <outputPortAccessStatus>{{status1}}</outputPortAccessStatus>
                  </VideoOutputChannel>
                  <VideoOutputChannel>
                    <id>17235972</id>
                    <portType>HDMI</portType>
                    <timeSequenceMode>standard</timeSequenceMode>
                    <name>Output 7-4</name>
                    <OutputResolution>
                      <resolution>1920*1080@60HZ</resolution>
                      <imageWidth>0</imageWidth>
                      <imageHeight>0</imageHeight>
                    </OutputResolution>
                    <PortInBoard>
                      <boardID>7</boardID>
                      <portID>4</portID>
                      <ipAddress>10.10.9.236</ipAddress>
                      <port>13191</port>
                    </PortInBoard>
                    <outputPortAccessStatus>{{status2}}</outputPortAccessStatus>
                  </VideoOutputChannel>
                </VideoOutputChannelList>
                """);
            return true;
        }

        // 9.7.3.5. GET /ISAPI/DisplayDev/Video/outputs/channels/{channelID}
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/outputs/channels/{channelID}", path, out var chanParams))
        {
            var chanId = chanParams["channelID"];
            if (string.Equals(chanId, "all", StringComparison.OrdinalIgnoreCase) || string.Equals(chanId, "capabilities", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!IsValidOutputChannel(chanId, out _))
            {
                await WriteXmlResponseAsync(res, HttpStatusCode.NotFound, $$"""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <ResponseStatus version="1.0" xmlns="{{Ns}}">
                      <requestURL>{{path}}</requestURL>
                      <statusCode>4</statusCode>
                      <statusString>Invalid Operation</statusString>
                      <subStatusCode>badParameters</subStatusCode>
                      <description>The video output channel ID does not exist</description>
                    </ResponseStatus>
                    """);
                return true;
            }

            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <VideoOutputChannel version="2.0" xmlns="{{Ns}}">
                  <id>{{chanId}}</id>
                  <portType>HDMI</portType>
                  <timeSequenceMode>standard</timeSequenceMode>
                  <name>Output Channel {{chanId}}</name>
                  <outputPortAccessStatus>normal</outputPortAccessStatus>
                </VideoOutputChannel>
                """);
            return true;
        }

        // 9.7.3.6. PUT /ISAPI/DisplayDev/Video/outputs/channels/{channelID}
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/Video/outputs/channels/{channelID}", path, out var putChanParams))
        {
            var chanId = putChanParams.GetValueOrDefault("channelID");
            if (string.Equals(chanId, "all", StringComparison.OrdinalIgnoreCase) || string.Equals(chanId, "capabilities", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!IsValidOutputChannel(chanId, out _))
            {
                await WriteXmlResponseAsync(res, HttpStatusCode.NotFound, $$"""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <ResponseStatus version="1.0" xmlns="{{Ns}}">
                      <requestURL>{{path}}</requestURL>
                      <statusCode>4</statusCode>
                      <statusString>Invalid Operation</statusString>
                      <subStatusCode>badParameters</subStatusCode>
                      <description>The video output channel ID does not exist</description>
                    </ResponseStatus>
                    """);
                return true;
            }

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

        // ═════════════════════════════════════════════════════════════════════
        // 4. SIGNAL SOURCES / INPUT CHANNELS (9.7.4.*)
        // ═════════════════════════════════════════════════════════════════════

        // 9.7.4.18. GET /ISAPI/DisplayDev/Video/inputs/channels/{channelID}/picture (Ảnh JPEG)
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/inputs/channels/{channelID}/picture", path))
        {
            await WriteBinaryResponseAsync(res, HttpStatusCode.OK, "image/jpeg", SampleJpegBytes);
            return true;
        }

        // 9.7.4.15. GET /ISAPI/DisplayDev/Video/inputs/channels/{channelID}/cutOff
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/inputs/channels/{channelID}/cutOff", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <CutOffInfo version="2.0" xmlns="{{Ns}}">
                  <enabled>true</enabled>
                  <Coordinate><x>0</x><y>0</y></Coordinate>
                  <width>1920</width>
                  <height>1080</height>
                </CutOffInfo>
                """);
            return true;
        }

        // 9.7.4.16. PUT /ISAPI/DisplayDev/Video/inputs/channels/{channelID}/cutOff
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/Video/inputs/channels/{channelID}/cutOff", path))
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

        // 9.7.4.8. GET /ISAPI/DisplayDev/Video/inputs/channels
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/inputs/channels", path))
        {
            GetInputChannelsCallCount++;
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <VideoInputChannelList version="2.0" xmlns="{{Ns}}">
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
                """);
            return true;
        }

        // 9.7.4.10. PUT /ISAPI/DisplayDev/Video/inputs/channels/{channelID}
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/Video/inputs/channels/{channelID}", path))
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

        // ═════════════════════════════════════════════════════════════════════
        // 5. VIDEO WALL & WINDOWS (9.7.5.* & 9.7.11.*)
        // ═════════════════════════════════════════════════════════════════════

        // 9.7.5.2. GET /ISAPI/DisplayDev/VideoWall
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall", path))
        {
            GetVideoWallsCallCount++;
            var wall1Status = GetWallBindStatus("1");
            var wall2Status = GetWallBindStatus("2");
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <VideoWallList version="2.0" xmlns="{{Ns}}">
                  <VideoWall>
                    <id>1</id>
                    <name>VideoWall1</name>
                    <wndStaticMode>blackScreen</wndStaticMode>
                    <streamFailedMode>lastFrame</streamFailedMode>
                    <wallBindOutputStatus>{{wall1Status}}</wallBindOutputStatus>
                  </VideoWall>
                  <VideoWall>
                    <id>2</id>
                    <name>HoangNhu</name>
                    <wndStaticMode>blackScreen</wndStaticMode>
                    <streamFailedMode>lastFrame</streamFailedMode>
                    <wallBindOutputStatus>{{wall2Status}}</wallBindOutputStatus>
                  </VideoWall>
                </VideoWallList>
                """);
            return true;
        }

        // 9.7.5.5. GET /ISAPI/DisplayDev/VideoWall/{videoWallID}/outputs
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/outputs", path, out var p))
        {
            GetOutputsCallCount++;
            var wallId = p.GetValueOrDefault("videoWallID", "1");
            if (wallId == "2")
            {
                await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <WallOutputList version="2.0" xmlns="{{Ns}}">
                      <WallOutput>
                        <id>1</id>
                        <outputID>17235971</outputID>
                        <Rect><Coordinate><x>0</x><y>0</y></Coordinate><width>1920</width><height>1920</height></Rect>
                        <outputWinNum>1</outputWinNum>
                        <coordinateMode>uniformCoordinate</coordinateMode>
                      </WallOutput>
                      <WallOutput>
                        <id>2</id>
                        <outputID>17235972</outputID>
                        <Rect><Coordinate><x>1920</x><y>0</y></Coordinate><width>1920</width><height>1920</height></Rect>
                        <outputWinNum>1</outputWinNum>
                        <coordinateMode>uniformCoordinate</coordinateMode>
                      </WallOutput>
                      <WallOutput>
                        <id>3</id>
                        <outputID>17235973</outputID>
                        <Rect><Coordinate><x>0</x><y>1920</y></Coordinate><width>1920</width><height>1920</height></Rect>
                        <outputWinNum>1</outputWinNum>
                        <coordinateMode>uniformCoordinate</coordinateMode>
                      </WallOutput>
                      <WallOutput>
                        <id>4</id>
                        <outputID>17235974</outputID>
                        <Rect><Coordinate><x>1920</x><y>1920</y></Coordinate><width>1920</width><height>1920</height></Rect>
                        <outputWinNum>1</outputWinNum>
                        <coordinateMode>uniformCoordinate</coordinateMode>
                      </WallOutput>
                    </WallOutputList>
                    """);
            }
            else
            {
                await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <WallOutputList version="2.0" xmlns="{{Ns}}">
                      <WallOutput>
                        <id>2</id>
                        <outputID>17235971</outputID>
                        <Rect><Coordinate><x>0</x><y>0</y></Coordinate><width>1920</width><height>1920</height></Rect>
                        <outputWinNum>1</outputWinNum>
                        <coordinateMode>uniformCoordinate</coordinateMode>
                      </WallOutput>
                      <WallOutput>
                        <id>3</id>
                        <outputID>17235972</outputID>
                        <Rect><Coordinate><x>0</x><y>1920</y></Coordinate><width>1920</width><height>1920</height></Rect>
                        <outputWinNum>1</outputWinNum>
                        <coordinateMode>uniformCoordinate</coordinateMode>
                      </WallOutput>
                    </WallOutputList>
                    """);
            }
            return true;
        }

        // 9.7.11.5. PUT .../windows/{winId}/sub/{subId}/top hoặc .../windows/{winId}/top
        if (method == "PUT" && (MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{winId}/sub/{subId}/top", path)
                             || MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{winId}/top", path)))
        {
            WindowTopCallCount++;
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

        // 9.7.11.6. PUT .../windows/{winId}/sub/{subId}/bottom hoặc .../windows/{winId}/bottom
        if (method == "PUT" && (MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{winId}/sub/{subId}/bottom", path)
                             || MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{winId}/bottom", path)))
        {
            WindowBottomCallCount++;
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

        // Switch Window Source: PUT .../windows/{winId}/sub/{subId}
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{winId}/sub/{subId}", path))
        {
            SwitchSourceCallCount++;
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

        // Update Window: PUT .../windows/{winId}
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{winId}", path))
        {
            UpdateWindowCallCount++;
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

        // Delete Single Window: DELETE .../windows/{winId}
        if (method == "DELETE" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{winId}", path))
        {
            DeleteWindowCallCount++;
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

        // 9.7.11.3. Delete All Windows: DELETE .../windows
        if (method == "DELETE" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows", path))
        {
            DeleteAllWindowsCallCount++;
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

        // Add Window: POST .../windows
        if (method == "POST" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows", path))
        {
            AddWindowCallCount++;
            if (SimulateAddWindowWithoutId)
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

            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <ResponseStatus version="1.0" xmlns="{{Ns}}">
                  <requestURL>{{path}}</requestURL>
                  <statusCode>1</statusCode>
                  <statusString>OK</statusString>
                  <subStatusCode>ok</subStatusCode>
                  <ID>33554435</ID>
                </ResponseStatus>
                """);
            return true;
        }

        // 9.7.11.2. Get All Windows: GET .../windows
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/windows", path))
        {
            GetWindowsCallCount++;
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <WallWindowList version="2.0" xmlns="{{Ns}}">
                  <WallWindow>
                    <id>33554433</id>
                    <wndOperateMode>uniformCoordinate</wndOperateMode>
                    <Rect>
                      <Coordinate><x>0</x><y>0</y></Coordinate>
                      <width>1920</width>
                      <height>1920</height>
                    </Rect>
                    <layerIdx>67108865</layerIdx>
                    <windowMode>1</windowMode>
                    <wndShowMode>subWndMode</wndShowMode>
                    <SubWindowList>
                      <SubWindow>
                        <id>1</id>
                        <SubWindowParam>
                          <signalMode>video input</signalMode>
                          <videoInputChannelID>16842753</videoInputChannelID>
                        </SubWindowParam>
                      </SubWindow>
                    </SubWindowList>
                    <wndLockKeep>false</wndLockKeep>
                  </WallWindow>
                  <WallWindow>
                    <id>33554434</id>
                    <wndOperateMode>uniformCoordinate</wndOperateMode>
                    <Rect>
                      <Coordinate><x>0</x><y>1920</y></Coordinate>
                      <width>1920</width>
                      <height>1920</height>
                    </Rect>
                    <layerIdx>67108866</layerIdx>
                    <windowMode>1</windowMode>
                    <wndShowMode>subWndMode</wndShowMode>
                    <SubWindowList>
                      <SubWindow>
                        <id>1</id>
                        <SubWindowParam>
                          <signalMode>video input</signalMode>
                          <videoInputChannelID>16842753</videoInputChannelID>
                        </SubWindowParam>
                      </SubWindow>
                    </SubWindowList>
                    <wndLockKeep>false</wndLockKeep>
                  </WallWindow>
                </WallWindowList>
                """);
            return true;
        }

        // 9.7.5.4. GET /ISAPI/DisplayDev/VideoWall/{videoWallID}
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}", path, out var wallParams))
        {
            var wallId = wallParams["videoWallID"];
            var wallStatus = GetWallBindStatus(wallId);
            var wallName = wallId == "2" ? "HoangNhu" : $"VideoWall{wallId}";
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <VideoWall version="2.0" xmlns="{{Ns}}">
                  <id>{{wallId}}</id>
                  <name>{{wallName}}</name>
                  <wndStaticMode>blackScreen</wndStaticMode>
                  <streamFailedMode>lastFrame</streamFailedMode>
                  <wallBindOutputStatus>{{wallStatus}}</wallBindOutputStatus>
                </VideoWall>
                """);
            return true;
        }

        // 9.7.5.3. PUT /ISAPI/DisplayDev/VideoWall/{videoWallID}
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}", path))
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

        // ═════════════════════════════════════════════════════════════════════
        // 6. SCENE (9.7.7.*)
        // ═════════════════════════════════════════════════════════════════════

        // 9.7.7.6. GET .../scene/isRunning
        if (method == "GET" && (MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/isRunning", path)
                             || MatchRoute("ISAPI/DisplayDev/scene/isRunning", path)))
        {
            GetActiveSceneCallCount++;
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <RunningScene version="2.0" xmlns="{{Ns}}">
                  <sceneID>{{ActiveSceneId}}</sceneID>
                </RunningScene>
                """);
            return true;
        }

        // 9.7.7.5. PUT .../scene/{sceneID}/activate
        if (method == "PUT" && (MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{sceneID}/activate", path, out var actParams)
                             || MatchRoute("ISAPI/DisplayDev/scene/{sceneID}/activate", path, out actParams)))
        {
            ActivateSceneCallCount++;
            if (actParams.TryGetValue("sceneID", out var parsedSceneStr) && int.TryParse(parsedSceneStr, out var parsedSceneId))
            {
                ActiveSceneId = parsedSceneId;
            }

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

        // SaveSceneData: PUT .../scene/{sceneID}/saveData
        if (method == "PUT" && (MatchRoute("ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{sceneID}/saveData", path)
                             || MatchRoute("ISAPI/DisplayDev/scene/{sceneID}/saveData", path)))
        {
            SaveSceneDataCallCount++;
            if (SimulateSaveDataFailure)
            {
                await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <ResponseStatus version="1.0" xmlns="{{Ns}}">
                      <requestURL>{{path}}</requestURL>
                      <statusCode>4</statusCode>
                      <statusString>Invalid Operation</statusString>
                      <subStatusCode>invalidOperation</subStatusCode>
                    </ResponseStatus>
                    """);
                return true;
            }

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

        // ═════════════════════════════════════════════════════════════════════
        // 7. SCREEN (9.7.8.*)
        // ═════════════════════════════════════════════════════════════════════

        // 9.7.8.1. PUT /ISAPI/DisplayDev/ScreenCtrl/closeAll
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/ScreenCtrl/closeAll", path))
        {
            if (ScreenCtrlCloseAllThrowsInvalidOperation)
            {
                await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <ResponseStatus version="1.0" xmlns="{{Ns}}">
                      <requestURL>{{path}}</requestURL>
                      <statusCode>4</statusCode>
                      <statusString>Invalid Operation</statusString>
                      <subStatusCode>invalidOperation</subStatusCode>
                    </ResponseStatus>
                    """);
                return true;
            }

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

        // ═════════════════════════════════════════════════════════════════════
        // 8. SERIAL TRANSPARENT (9.1.8.*)
        // ═════════════════════════════════════════════════════════════════════

        // PUT .../Transparent/channels/{channelId}/open
        if (method == "PUT" && (MatchRoute("ISAPI/System/Serial/ports/{portId}/Transparent/channels/{channelId}/open", path)
                             || MatchRoute("ISAPI/System/Serial/Transparent/channels/{channelId}/open", path)))
        {
            SerialOpenCallCount++;
            if (SimulateSerialOpenFailure)
            {
                await WriteXmlResponseAsync(res, HttpStatusCode.InternalServerError, $$"""
                    <?xml version="2.0" xmlns="{{Ns}}">
                      <statusCode>4</statusCode>
                      <statusString>Internal Error</statusString>
                      <subStatusCode>deviceError</subStatusCode>
                    </ResponseStatus>
                    """);
                return true;
            }

            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="2.0" xmlns="{{Ns}}">
                  <statusCode>1</statusCode>
                  <statusString>OK</statusString>
                  <subStatusCode>ok</subStatusCode>
                </ResponseStatus>
                """);
            return true;
        }

        // PUT .../Transparent/channels/{channelId}/transData
        if (method == "PUT" && (MatchRoute("ISAPI/System/Serial/ports/{portId}/Transparent/channels/{channelId}/transData", path)
                             || MatchRoute("ISAPI/System/Serial/Transparent/channels/{channelId}/transData", path)))
        {
            SerialSendCallCount++;
            var mem = new MemoryStream();
            await req.InputStream.CopyToAsync(mem);
            LastReceivedSerialData = mem.ToArray();
            LastReceivedContentType = req.ContentType;

            if (SimulateSerialSendFailure || SimulateDeviceFailure)
            {
                await WriteXmlResponseAsync(res, HttpStatusCode.InternalServerError, $$"""
                    <?xml version="2.0" xmlns="{{Ns}}">
                      <statusCode>4</statusCode>
                      <statusString>Device Error</statusString>
                      <subStatusCode>deviceError</subStatusCode>
                    </ResponseStatus>
                    """);
                return true;
            }

            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="2.0" xmlns="{{Ns}}">
                  <statusCode>1</statusCode>
                  <statusString>OK</statusString>
                  <subStatusCode>ok</subStatusCode>
                </ResponseStatus>
                """);
            return true;
        }

        // GET .../Transparent/channels/{channelId}/transData
        if (method == "GET" && (MatchRoute("ISAPI/System/Serial/ports/{portId}/Transparent/channels/{channelId}/transData", path)
                             || MatchRoute("ISAPI/System/Serial/Transparent/channels/{channelId}/transData", path)))
        {
            SerialReceiveCallCount++;
            res.StatusCode = (int)HttpStatusCode.OK;
            res.ContentType = "application/octet-stream";
            var data = SerialDataToReturn ?? [0x01, 0x02, 0x03];
            await res.OutputStream.WriteAsync(data);
            res.Close();
            return true;
        }

        // PUT .../Transparent/channels/{channelId}/close
        if (method == "PUT" && (MatchRoute("ISAPI/System/Serial/ports/{portId}/Transparent/channels/{channelId}/close", path)
                             || MatchRoute("ISAPI/System/Serial/Transparent/channels/{channelId}/close", path)))
        {
            SerialCloseCallCount++;
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, $$"""
                <?xml version="2.0" xmlns="{{Ns}}">
                  <statusCode>1</statusCode>
                  <statusString>OK</statusString>
                  <subStatusCode>ok</subStatusCode>
                </ResponseStatus>
                """);
            return true;
        }

        // ═════════════════════════════════════════════════════════════════════
        // 9. MỞ RỘNG THEO TỪNG NHÓM NGHIỆP VỤ (135/135 PRESETS)
        // ═════════════════════════════════════════════════════════════════════
        if (await TryHandleBoardAsync(context, method, path))
            return true;
        if (await TryHandleDecodingAsync(context, method, path))
            return true;
        if (await TryHandleOutputChannelAsync(context, method, path))
            return true;
        if (await TryHandleSignalSourceAsync(context, method, path))
            return true;
        if (await TryHandlePlanAsync(context, method, path))
            return true;
        if (await TryHandleSceneAsync(context, method, path))
            return true;
        if (await TryHandleTextLedAsync(context, method, path))
            return true;
        if (await TryHandleWallpaperAsync(context, method, path))
            return true;
        if (await TryHandleWindowAsync(context, method, path))
            return true;

        // Không khớp route cụ thể nào ở trên
        return false;
    }
}
