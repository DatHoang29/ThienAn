using System.Net;
using System.Threading.Tasks;

namespace Tests.Modules.VideoWall.MockServer;

public partial class VwISAPIMockServerHikvision
{
    private async Task<bool> TryHandleSignalSourceAsync(HttpListenerContext context, string method, string path)
    {
        var res = context.Response;

        // 9.7.4.1. Get the audio capabilities [GET ISAPI/DisplayDev/Audio/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Audio/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<AudioCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <audioInputsPortNums>
    0
  </audioInputsPortNums>
  <audioOutputsPortNums>
    0
  </audioOutputsPortNums>
  <audioOutputMaxNum>
    0
  </audioOutputMaxNum>
</AudioCap>
""");
            return true;
        }

        // 9.7.4.2. Get capability set of adding signal source group [GET ISAPI/DisplayDev/SignalSource/AddSignalSourceGroup/capabilities?format=json]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/SignalSource/AddSignalSourceGroup/capabilities", path))
        {
            await WriteJsonResponseAsync(res, HttpStatusCode.OK, """
{
  "AddSignalSourceGroupCap": {
    "groupName": {
      "@def": "undefined",
      "@min": 1,
      "@max": 64
    },
    "signalSourceList": {
      "@size": 128,
      "signalSourceID": {
        "@def": 1,
        "@min": 1,
        "@max": 2147483647
      }
    }
  }
}
""");
            return true;
        }

        // 9.7.4.3. Get signal source groups [POST ISAPI/DisplayDev/SignalSource/GetSignalSourceGroup?format=json]
        if (method == "POST" && MatchRoute("ISAPI/DisplayDev/SignalSource/GetSignalSourceGroup", path))
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

        // 9.7.4.4. Get capability of editing signal source group [GET ISAPI/DisplayDev/SignalSource/ModifySignalSourceGroup/capabilities?format=json]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/SignalSource/ModifySignalSourceGroup/capabilities", path))
        {
            await WriteJsonResponseAsync(res, HttpStatusCode.OK, """
{
  "ModifySignalSourceGroupCap": {
    "groupID": {
      "@def": 1,
      "@min": 1,
      "@max": 32
    },
    "groupName": {
      "@def": "undefined",
      "@min": 1,
      "@max": 64
    },
    "signalSourceList": {
      "@size": 128,
      "signalSourceID": {
        "@def": 1,
        "@min": 1,
        "@max": 2147483647
      }
    }
  }
}
""");
            return true;
        }

        // 9.7.4.5. Get capability of no signal parameters of signal source [GET ISAPI/DisplayDev/SignalSource/SignalSourceNoSignalParams/capabilities?format=json]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/SignalSource/SignalSourceNoSignalParams/capabilities", path))
        {
            await WriteJsonResponseAsync(res, HttpStatusCode.OK, """
{
  "SignalSourceNoSignalParamsCap": {
    "noSignalScreenMode": {
      "@opt": [],
      "@def": "default"
    },
    "customFile": {
      "filePathType": {
        "@opt": [],
        "@def": "URL"
      },
      "filePath": {
        "@def": "",
        "@min": 0,
        "@max": 30720
      }
    }
  }
}
""");
            return true;
        }

        // 9.7.4.6. Get no signal parameters of signal source [GET ISAPI/DisplayDev/SignalSource/SignalSourceNoSignalParams?format=json]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/SignalSource/SignalSourceNoSignalParams", path))
        {
            await WriteJsonResponseAsync(res, HttpStatusCode.OK, """
{
  "noSignalScreenMode": "default",
  "customFile": {
    "filePathType": "URL",
    "filePath": ""
  }
}
""");
            return true;
        }

        // 9.7.4.7. Get video capabilities [GET ISAPI/DisplayDev/Video/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<VideoCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <VideoInputsCap version="2.0">
    <videoInputPortNums>
      0
    </videoInputPortNums>
    <isSupportColorSetting>
      true
    </isSupportColorSetting>
    <isSupportPostionSetting>
      true
    </isSupportPostionSetting>
    <isSupportCutOffSetting>
      true
    </isSupportCutOffSetting>
    <isSupportPictureCapture>
      true
    </isSupportPictureCapture>
    <isSupportText>
      true
    </isSupportText>
    <encodeOSDInputSignalType opt="HDMI,DP">
      test
    </encodeOSDInputSignalType>
    <SupportSelfdefineResolution>
      <signalType opt="DVI,VGA,HDMI,DP">
        test
      </signalType>
      <supportedDPResolution opt="640*480@60,800*600@60">
        test
      </supportedDPResolution>
    </SupportSelfdefineResolution>
    <isSupportEDIDResolution>
      true
    </isSupportEDIDResolution>
    <isSupportJoinSignalCfg>
      true
    </isSupportJoinSignalCfg>
    <SupportAudioCfg>
      <signalType opt="DVI,VGA,HDMI,DP">
        test
      </signalType>
    </SupportAudioCfg>
  </VideoInputsCap>
  <VideoOutputsCap version="2.0">
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
        <imageWidth min="0" max="10">
          0
        </imageWidth>
        <imageHeight min="0" max="10">
          0
        </imageHeight>
      </OutputResolutionCap>
    </OutputResolutionCapList>
    <isSupportEDIDResolution>
      true
    </isSupportEDIDResolution>
  </VideoOutputsCap>
  <VideoStreamingCap version="2.0">
    <streamingNums>
      0
    </streamingNums>
    <isSupportURL>
      true
    </isSupportURL>
    <isSupportDDNS>
      true
    </isSupportDDNS>
    <isSupportIPAddress>
      true
    </isSupportIPAddress>
    <isSupportDistributedIPSignal opt="true,false">
      true
    </isSupportDistributedIPSignal>
    <isSupportAddBatch>
      true
    </isSupportAddBatch>
    <isSupportStreamChanSearch>
      true
    </isSupportStreamChanSearch>
    <isSupportEncryptStream>
      true
    </isSupportEncryptStream>
    <isSupportLoopEncryptStream>
      true
    </isSupportLoopEncryptStream>
  </VideoStreamingCap>
  <isSupportClarityConfig>
    true
  </isSupportClarityConfig>
</VideoCap>
""");
            return true;
        }

        // 9.7.4.12. Get color parameters of a specific signal source [GET ISAPI/DisplayDev/Video/inputs/channels/{channelID}/color]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/inputs/channels/{channelID}/color", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<Color xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <brightnessLevel>
    1
  </brightnessLevel>
  <contrastLevel>
    1
  </contrastLevel>
  <saturationLevel>
    1
  </saturationLevel>
  <hueLevel>
    1
  </hueLevel>
  <sharpnessLevel>
    1
  </sharpnessLevel>
</Color>
""");
            return true;
        }

        // 9.7.4.13. Set color parameters of a specified signal source [PUT ISAPI/DisplayDev/Video/inputs/channels/{channelID}/color]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/Video/inputs/channels/{channelID}/color", path))
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

        // 9.7.4.14. Get the color configuration capability of a signal source [GET ISAPI/DisplayDev/Video/inputs/channels/{channelID}/color/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/inputs/channels/{channelID}/color/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<Color xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <brightnessLevel min="0" max="100">
    1
  </brightnessLevel>
  <contrastLevel min="0" max="100">
    1
  </contrastLevel>
  <saturationLevel min="0" max="100">
    1
  </saturationLevel>
  <hueLevel min="0" max="100">
    1
  </hueLevel>
  <sharpnessLevel min="0" max="100">
    1
  </sharpnessLevel>
</Color>
""");
            return true;
        }

        // 9.7.4.17. Get the capability of configuring picture cropping parameters of a signal source [GET ISAPI/DisplayDev/Video/inputs/channels/{channelID}/cutOff/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/inputs/channels/{channelID}/cutOff/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<InputCutOff xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <leftCutOff min="0" max="30">
    1
  </leftCutOff>
  <rightCutOff min="0" max="30">
    1
  </rightCutOff>
  <topCutOff min="0" max="30">
    1
  </topCutOff>
  <bottomCutOff min="0" max="30">
    1
  </bottomCutOff>
  <leftCutOffMultiple>
    16
  </leftCutOffMultiple>
  <rightCutOffMultiple>
    16
  </rightCutOffMultiple>
  <topCutOffMultiple>
    4
  </topCutOffMultiple>
  <bottomCutOffMultiple>
    4
  </bottomCutOffMultiple>
  <displayCutOffIndicatorLine>
    true
  </displayCutOffIndicatorLine>
</InputCutOff>
""");
            return true;
        }

        // 9.7.4.19. Get the capability of configuring image position adjustment parameters of a signal source [GET ISAPI/DisplayDev/Video/inputs/channels/{channelID}/position/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/inputs/channels/{channelID}/position/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<InputPosition xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <horizontal min="-30" max="30">
    1
  </horizontal>
  <vertical min="-30" max="30">
    1
  </vertical>
  <adjustmentUnit opt="0,8,16,24,32">
    0
  </adjustmentUnit>
</InputPosition>
""");
            return true;
        }

        // 9.7.4.20. Set the custom resolution of a specified signal source [PUT ISAPI/DisplayDev/Video/inputs/channels/{channelID}/resolution]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/Video/inputs/channels/{channelID}/resolution", path))
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

        // 9.7.4.21. Get the capability of customizing the resolution of a specified signal source [GET ISAPI/DisplayDev/Video/inputs/channels/{channelID}/resolution/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/inputs/channels/{channelID}/resolution/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<Resolution xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>
    0
  </id>
  <resolutionName min="0" max="10">
    test
  </resolutionName>
  <imageWidth min="0" max="10">
    0
  </imageWidth>
  <imageHeight min="0" max="10">
    0
  </imageHeight>
  <refreshRate opt="25,30,60">
    0
  </refreshRate>
  <colorDepth opt="8,16,32">
    32
  </colorDepth>
  <scanType opt="progressiveScan,intervalScan">
    true
  </enabled>
  <imageWidthMultiple opt="2,4,8">
    2
  </imageWidthMultiple>
  <imageHeightMultiple opt="2,4,8">
    2
  </imageHeightMultiple>
  <resolutionMin opt="800*600@30Hz,800*600@60Hz">
    800*600@30Hz
  </resolutionMin>
  <resolutionMax opt="3840*2160@30Hz,1920*1200@60Hz">
    3840*2160@30Hz
  </resolutionMax>
  <resolutionCapList>
    <resolutionCap>
      <refreshRate opt="25,30,60">
        0
      </refreshRate>
      <resolutionWidth min="800" max="3840">
        test
      </resolutionWidth>
      <resolutionHeight min="600" max="2160">
        test
      </resolutionHeight>
      <resolutionArea min="480000" max="100000000">
        test
      </resolutionArea>
    </resolutionCap>
  </resolutionCapList>
</Resolution>
""");
            return true;
        }

        // 9.7.4.22. Get the OSD configuration capability of a signal source [GET ISAPI/DisplayDev/Video/inputs/channels/{channelID}/text/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/inputs/channels/{channelID}/text/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<SignalSourceTextList xmlns="http://www.isapi.org/ver20/XMLSchema" size="10" version="2.0">
  <SignalSourceText version="2.0">
    <id>
      0
    </id>
    <enable>
      true
    </enable>
    <fontSize opt="1">
      0
    </fontSize>
    <backgroudMode opt="transparent">
      transparent
    </backgroudMode>
    <positionX min="0" max="100">
      0
    </positionX>
    <positionY min="0" max="100">
      0
    </positionY>
    <ForegroudColor>
      <RGB>
        0
      </RGB>
    </ForegroudColor>
    <BackgroudColor>
      <RGB>
        0
      </RGB>
    </BackgroudColor>
    <textContent>
      test
    </textContent>
    <alignment opt="customize">
      alignRight
    </alignment>
  </SignalSourceText>
</SignalSourceTextList>
""");
            return true;
        }

        // 9.7.4.23. Get the video input capability [GET ISAPI/DisplayDev/Video/inputs/channels/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/inputs/channels/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<VideoInputsCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <videoInputPortNums>
    0
  </videoInputPortNums>
  <isSupportColorSetting>
    true
  </isSupportColorSetting>
  <isSupportPostionSetting>
    true
  </isSupportPostionSetting>
  <isSupportCutOffSetting>
    true
  </isSupportCutOffSetting>
  <isSupportPictureCapture>
    true
  </isSupportPictureCapture>
  <isSupportText>
    true
  </isSupportText>
  <encodeOSDInputSignalType opt="HDMI,DP">
!
i
i
i
l
{
i }
    test
  </encodeOSDInputSignalType>
  <SupportSelfdefineResolution>
    <signalType opt="DVI,VGA,HDMI,DP">
      test
    </signalType>
    <supportedDPResolution opt="640*480@60,800*600@60">
      test
    </supportedDPResolution>
  </SupportSelfdefineResolution>
  <isSupportEDIDResolution>
    true
  </isSupportEDIDResolution>
  <isSupportJoinSignalCfg>
    true
  </isSupportJoinSignalCfg>
  <SupportAudioCfg>
    <signalType opt="DVI,VGA,HDMI,DP">
      test
    </signalType>
  </SupportAudioCfg>
  <inputPortEnabled opt="true,false">
    true
  </inputPortEnabled>
  <timeSequenceResolution
opt="1024*768@60HZ,720P@50HZ,720P@60HZ#720P@60HZ,1366*768@60HZ#1366,1440*900@60HZ,1280*1024@60HZ,1680*1050@60,1080P@50HZ,1080P@60HZ,3840*2160@30HZ">
    1024*768@60HZ
  </timeSequenceResolution>
  <inputContent opt="default,broadcastVideo,wirelessMirroring">
    default
  </inputContent>
  <OptimumResolution>
    <enabled opt="true,false">
      true
    </enabled>
    <ResolutionList>
      <imageWidth min="0" max="4096">
        0
      </imageWidth>
      <imageHeight min="0" max="4096">
        0
      </imageHeight>
    </ResolutionList>
  </OptimumResolution>
  <inputPortAccessType opt="computer,notebook,mobilePhone,tablet,wirelessMirroring,highCamera,recordHost,broadcastVideo,universal">
    computer
  </inputPortAccessType>
  <isSupportVideoInputSubtrack>
    true
  </isSupportVideoInputSubtrack>
  <streamType opt="1,2">
    1
  </streamType>
  <bitRateType opt="CBR,VBR">
    CBR
  </bitRateType>
  <intervalFrame>
    25
  </intervalFrame>
  <picQuality opt="1,2,3,4,5,6">
    1
  </picQuality>
  <resolution
opt="352*240,352*288,176*120,176*144,704*288,704*240,528*384,528*320,704*576,704*480,960*576,960*480,1280*720,1280*960,1600*1200,1080P,1080I,1024*768,1360*7
68,1366*768,1280*1024,1400*1050,1440*900,1680*1050">
    352*240
  </resolution>
  <videoType opt="0,1,2">
    1
  </videoType>
  <videoBitRate opt="2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23">
    1
  </videoBitRate>
  <customBitRate>
    128
  </customBitRate>
  <frameType opt="P">
    P
  </frameType>
  <frameRate opt="full,1/16,1/8,1/4,1/2,1,2,4,6,8,10,12,15,16,18,20,22,25,30">
    25
16, 18, 20, 22, 25, 30 >25
  </frameRate>
  <audioBitRate opt="64">
    64
  </audioBitRate>
  <audioSamplingRate opt="8000,16000,32000,44100,48000">
    8000
  </audioSamplingRate>
  <audioCodecType opt="G.711alaw,G.711ulaw,G.722.1,AAC">
    G.711alaw
  </audioCodecType>
  <audioVolume min="0" max="100">
    1
  </audioVolume>
  <audioCompressionType opt="G.711alaw,G.711ulaw,G.722.1,AAC">
    G.711alaw
  </audioCompressionType>
  <isSupportVideoPreview>
    true
  </isSupportVideoPreview>
  <isSupportSubStreamVideoPreview>
    true
  </isSupportSubStreamVideoPreview>
  <localStreamDeepEncodeRatioType opt="low,middle,high">
    low
  </localStreamDeepEncodeRatioType>
  <localStreamLightEncodeRatioType opt="low,middle,high">
    low
  </localStreamLightEncodeRatioType>
  <name min="1" max="32">
    1
  </name>
  <isSupportJoinSignalResolution>
    true
  </isSupportJoinSignalResolution>
  <bitDepth opt="8,10">
    8
  </bitDepth>
</VideoInputsCap>
""");
            return true;
        }

        // 9.7.4.24. Get splicing configuration of all signal resources [GET ISAPI/DisplayDev/Video/inputs/joinSignal]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/inputs/joinSignal", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<JoinSignalList xmlns="http://www.isapi.org/ver20/XMLSchema" size="10" version="2.0">
  <JoinSignal>
    <id>
      0
    </id>
    <enabled>
      true
    </enabled>
    <joinName>
      test
    </joinName>
    <camMode>
      0
    </camMode>
    <rows>
      0
    </rows>
    <columns>
      0
    </columns>
    <SignalList>
      <signalNo>
        0
      </signalNo>
    </SignalList>
    <joinSignalNo>
      1
    </joinSignalNo>
    <SignalInfoList>
      <SignalInfo>
        <signalNo>
          0
        </signalNo>
        <name>
          test
        </name>
        <portType>
          SDI
        </portType>
        <hostname>
          ipv4Address
        </hostname>
        <ipv4Address>
          192.168.1.1
        </ipv4Address>
        <ipv6Address>
          fe80::4ba5:790e
        </ipv6Address>
        <port>
          1
        </port>
        <trackNum>
          <x>
            1
          </x>
          <y>
            1
          </y>
        </trackNum>
        <signalStatus>
          signal
        </signalStatus>
      </SignalInfo>
    </SignalInfoList>
  </JoinSignal>
</JoinSignalList>
""");
            return true;
        }

        // 9.7.4.25. Set jointing parameters of a specified signal source [PUT ISAPI/DisplayDev/Video/inputs/joinSignal/{channelID}]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/Video/inputs/joinSignal/{channelID}", path))
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

        // 9.7.4.26. Get splicing parameters of a signal source [GET ISAPI/DisplayDev/Video/inputs/joinSignal/{channelID}]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/inputs/joinSignal/{channelID}", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<JoinSignal xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>
    0
  </id>
  <enabled>
    true
  </enabled>
  <joinName>
    test
  </joinName>
  <camMode>
    0
  </camMode>
  <rows>
    0
  </rows>
  <columns>
    0
  </columns>
  <SignalList>
    <signalNo>
      0
    </signalNo>
  </SignalList>
  <joinSignalNo>
    1
  </joinSignalNo>
  <SignalInfoList>
    <SignalInfo>
      <signalNo>
        0
      </signalNo>
      <name>
        test
      </name>
      <portType>
        SDI
      </portType>
      <hostname>
        ipv4Address
      </hostname>
      <ipv4Address>
        192.168.1.1
      </ipv4Address>
      <ipv6Address>
        fe80::4ba5:790e
      </ipv6Address>
      <port>
        1
      </port>
      <trackNum>
        <x>
          1
        </x>
        <y>
          1
        </y>
      </trackNum>
      <signalStatus>
        signal
      </signalStatus>
    </SignalInfo>
  </SignalInfoList>
</JoinSignal>
""");
            return true;
        }

        // 9.7.4.27. Get signal source splicing capability [GET ISAPI/DisplayDev/Video/inputs/joinSignal/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/inputs/joinSignal/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<JoinSignalList xmlns="http://www.isapi.org/ver20/XMLSchema" size="10" version="2.0">
  <JoinSignal version="2.0">
    <id min="0" max="10">
      0
    </id>
    <enabled opt="true,false">
      true
    </enabled>
    <joinName max="10">
      test
    </joinName>
    <camMode>
      0
    </camMode>
    <rows min="0" max="100">
      0
    </rows>
    <columns min="0" max="100">
      0
    </columns>
    <SignalList size="10">
      <signalNo>
        0
      </signalNo>
    </SignalList>
    <joinSignalNo>
      1
    </joinSignalNo>
  </JoinSignal>
</JoinSignalList>
""");
            return true;
        }

        // 9.7.4.28. Get all video streams' parameters [GET ISAPI/DisplayDev/Video/streaming/channels]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/streaming/channels", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<StreamInputChannelList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <StreamInputChannel version="2.0">
    <id>
      1
    </id>
    <name>
      Camera IP Mock 1
    </name>
    <group>
      default
    </group>
    <startDecoding>
      true
    </startDecoding>
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
              by domain
            </streamType>
            <StreamInURL>
              <URL>
                rtsp://172.25.1.12:554/Streaming/Channels/101
              </URL>
            </StreamInURL>
            <StreamByDdns>
              <DdnsServerInfo>
                <domain>
                  ipv4
                </domain>
                <port>
                  0
                </port>
                <ddnsType>
                  hiDdns
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
                  0
                </port>
                <transmitProtocol>
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
                  test
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
                  test
                </domain>
                <port>
                  0
                </port>
                <transmitProtocol>
              <EncodeDevInfo>
                <domain>
                  ipv4
                </domain>
                <port>
                  0
                </port>
<transmitProtocol>
                <transmitProtocol>
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
                  test
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
                  0
                </port>
                <transmitProtocol>
        <playbackMode>
          file name
        </playbackMode>
        <EncodeDevInfo>
          <domain>
            ipv4
          </domain>
          <port>
            0
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
            0
          </channelZero>
          <channelNormal>
            0
          </channelNormal>
          <channelStreaming>
            test
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
  </StreamInputChannel>
</StreamInputChannelList>
""");
            return true;
        }

        // 9.7.4.29. Set all video stream parameters [PUT ISAPI/DisplayDev/Video/streaming/channels]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/Video/streaming/channels", path))
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

        // 9.7.4.30. Set parameters of a specific video stream [PUT ISAPI/DisplayDev/Video/streaming/channels/{channelID}]
        if (method == "PUT" && MatchRoute("ISAPI/DisplayDev/Video/streaming/channels/{channelID}", path))
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

        // 9.7.4.31. Delete parameters of a specific video stream [DELETE ISAPI/DisplayDev/Video/streaming/channels/{channelID}]
        if (method == "DELETE" && MatchRoute("ISAPI/DisplayDev/Video/streaming/channels/{channelID}", path))
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

        // 9.7.4.32. Get parameters of a specified video stream [GET ISAPI/DisplayDev/Video/streaming/channels/{channelID}]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/streaming/channels/{channelID}", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<StreamInputChannel xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>
    test
  </id>
  <name>
    test
  </name>
  <group>
    test
  </group>
  <startDecoding>
    true
  </startDecoding>
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
            by domain
          </streamType>
          <StreamInURL>
            <URL>
              test
/URL
            </URL>
          </StreamInURL>
          <StreamByDdns>
            <DdnsServerInfo>
              <domain>
                ipv4
              </domain>
              <port>
                0
              </port>
              <ddnsType>
                hiDdns
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
                0
              </port>
              <transmitProtocol>
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
                test
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
                test
              </domain>
              <port>
                0
              </port>
              <transmitProtocol>
            <EncodeDevInfo>
              <domain>
                ipv4
              </domain>
              <port>
                0
              </port>
              <transmitProtocol>
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
                test
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
                0
              </port>
              <transmitProtocol>
      <playbackMode>
        file name
      </playbackMode>
      <EncodeDevInfo>
        <domain>
          ipv4
        </domain>
        <port>
          0
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
          0
        </channelZero>
        <channelNormal>
          0
        </channelNormal>
        <channelStreaming>
          test
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
</StreamInputChannel>
""");
            return true;
        }

        // 9.7.4.33. Get video stream capability [GET ISAPI/DisplayDev/Video/streaming/channels/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/streaming/channels/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<VideoStreamingCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <streamingNums>
    0
  </streamingNums>
  <isSupportURL>
    true
  </isSupportURL>
  <isSupportDDNS>
    true
  </isSupportDDNS>
  <isSupportIPAddress>
    true
  </isSupportIPAddress>
  <isSupportDistributedIPSignal opt="true,false">
    true
  </isSupportDistributedIPSignal>
  <isSupportAddBatch>
    true
  </isSupportAddBatch>
  <isSupportStreamChanSearch>
    true
  </isSupportStreamChanSearch>
  <isSupportEncryptStream>
    true
  </isSupportEncryptStream>
  <isSupportLoopEncryptStream>
    true
  </isSupportLoopEncryptStream>
  <id min="1" max="100">
    test
  </id>
  <name min="1" max="32">
    test
  </name>
  <group min="1" max="32">
    test
  </group>
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
            <URL min="1" max="1024">
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
              <username min="1" max="32">
                test
              </username>
              <password min="1" max="16">
                test
              </password>
            </DdnsServerInfo>
            <EncodeDevInfo>
              <domain opt="ipv4,ipv6,domain">
                ipv4
              </domain>
              <port min="1" max="65535">
                1
              </port>
              <transmitProtocol opt="tcp,udp,mcast">
                tcp
              </transmitProtocol>
              <protocol opt="HIKVISION,DAHUA,…">
                DAAHUA
              </protocol>
              <username min="1" max="32">
                test
              </username>
              <password min="1" max="16">
                test
              </password>
              <channelMode opt="normal,zero,streaming,distributed">
                normal
              </channelMode>
              <channelType opt="main,sub,third">
                main
              </channelType>
              <channelZero min="1" max="1024">
                1
              </channelZero>
              <channelNormal min="1" max="1024">
                1
              </channelNormal>
              <channelStreaming min="1" max="1024">
                1
              </channelStreaming>
              <channelDistributed min="1" max="1024">
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
              <port min="1" max="65535">
                1
              </port>
              <transmitProtocol opt="tcp,udp,mcast">
                tcp
              </transmitProtocol>
            </MediaGatewayInfo>
          </StreamByDdns>
          <StreamByDomain>
            <EncodeDevInfo>
!
t
bj t i f
ti
f d i
i
l b k
              <domain opt="ipv4,ipv6,domain">
                ipv4
              </domain>
              <port min="1" max="65535">
                1
              </port>
              <transmitProtocol opt="tcp,udp,mcast">
                tcp
              </transmitProtocol>
              <protocol opt="HIKVISION,DAHUA,…">
                DAHUA
              </protocol>
              <username min="1" max="32">
                test
              </username>
              <password min="1" max="16">
                test
              </password>
              <channelMode opt="normal,zero,streaming,distributed">
                normal
              </channelMode>
              <channelType opt="main,sub,third">
                main
              </channelType>
              <channelZero min="1" max="1024">
                1
              </channelZero>
              <channelNormal min="1" max="1024">
                1
              </channelNormal>
              <channelStreaming min="1" max="1024">
                1
              </channelStreaming>
              <channelDistributed min="1" max="1024">
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
              <port min="1" max="65535">
                1
              </port>
              <transmitProtocol opt="tcp,udp,mcast">
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
        <domain opt="ipv4,ipv6,domain">
          ipv4
        </domain>
        <port min="1" max="10">
          1
        </port>
        <transmitProtocol opt="tcp,udp,mcast">
          tcp
        </transmitProtocol>
        <protocol opt="HIKVISION,DAHUA,…">
          DAHUA
        </protocol>
        <username min="1" max="32">
          test
        </username>
        <password min="1" max="16">
          test
        </password>
        <channelMode opt="normal,zero,streaming,distributed">
          normal
        </channelMode>
        <channelType opt="main,sub,third">
          1
        </channelZero>
        <channelNormal min="1" max="1024">
          1
        </channelNormal>
        <channelStreaming min="1" max="1240">
          1
        </channelStreaming>
        <channelDistributed min="1" max="1024">
          1
        </channelDistributed>
      </EncodeDevInfo>
      <fileName min="1" max="128">
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
    <streamPassword min="1" max="16">
      test
    </streamPassword>
  </StreamInput>
</VideoStreamingCap>
""");
            return true;
        }

        // 9.7.4.34. Get capability of searching for network input source parameters [GET ISAPI/DisplayDev/Video/streaming/channels/search/capabilities]
        if (method == "GET" && MatchRoute("ISAPI/DisplayDev/Video/streaming/channels/search/capabilities", path))
        {
            await WriteXmlResponseAsync(res, HttpStatusCode.OK, """
<?xml version="1.0" encoding="UTF-8"?>
<StreamInChanSearchCap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <searchID min="0" max="10">
    test
  </searchID>
  <maxMatchResults min="0" max="10">
    0
  </maxMatchResults>
  <maxConcurrentSearches min="0" max="10">
    0
  </maxConcurrentSearches>
</StreamInChanSearchCap>
""");
            return true;
        }

        return false;
    }
}
