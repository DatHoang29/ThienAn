using System.Collections.Generic;
using Module.VideoWall.WPF.ViewModels.Isapi;

namespace Module.VideoWall.WPF.ViewModels;

// Đã bỏ property DisplayName ($"{Section} — {Name}"): nơi dùng duy nhất là khối tiêu đề trong
// GroupBox "Tham số API" của IsapiGroupTabView.xaml, đã xoá vì lặp lại đúng thông tin mà cột
// "Danh sách API" bên trái đã hiển thị.
/// <summary>
/// Description: Mục định nghĩa API ISAPI mẫu trong danh mục chuẩn (Section, Name, Method, URL, Group, SampleBody)
/// Created date: 13/09/2026
/// </summary>
public sealed record VwIsapiPresetItem(
    string Section,
    string Name,
    string Method,
    string Url,
    VwIsapiGroup Group,
    string? SampleBody = null);

/// <summary>
/// Description: Bộ sưu tập các mẫu Body XML chuẩn phục vụ kiểm thử các API ISAPI
/// Created date: 13/09/2026
/// </summary>
public static class VwIsapiSampleBodies
{
    public const string VideoWallPutWarning =
        "⚠️ API này ghi đè layout lưới + gán output của cả tường (WallOutputList/WallWindowList trong 09A-api-reference.md dòng 31158-31234). Bắt buộc: gọi GET 9.7.5.2 (Get parameters of all video walls) trước để copy đúng WallOutputList/WallWindowList, dán vào 2 ô XML bên dưới và chỉnh sửa field cần đổi trước khi gửi PUT.";

    public const string BoardConfig = """
        <BoardConfig xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <id>1</id>
          <name>SubBoard 1</name>
        </BoardConfig>
        """;

    public const string BoardConfigList = """
        <BoardConfigList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <BoardConfig>
            <id>1</id>
            <name>SubBoard 1</name>
          </BoardConfig>
        </BoardConfigList>
        """;

    public const string PreMonitor = """
        <PreMonitorParam xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <enabled>true</enabled>
          <streamType>subStream</streamType>
        </PreMonitorParam>
        """;

    public const string StartDynamicDecode = """
        <StartDynamicDecode xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <inputNo>1</inputNo>
          <streamType>main</streamType>
          <url>rtsp://admin:12345@192.168.1.64:554/Streaming/Channels/101</url>
          <protocol>RTSP</protocol>
        </StartDynamicDecode>
        """;

    public const string StopDynamicDecode = """
        <StopDynamicDecode xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <inputNo>1</inputNo>
        </StopDynamicDecode>
        """;

    public const string BoardStreamExportJson = """
        {
          "BoardStreamExportCfg": {
            "enable": true,
            "boardNo": 1,
            "streamExportType": "decode"
          }
        }
        """;

    public const string DefaultDecodeDelayJson = """
        {
          "DefaultDecodeDelayParams": {
            "delayTime": 200
          }
        }
        """;

    public const string AudioOutputChannelList = """
        <AudioOutputChannelList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <AudioOutputChannel>
            <id>1</id>
            <volume>50</volume>
            <mute>false</mute>
          </AudioOutputChannel>
        </AudioOutputChannelList>
        """;

    public const string VideoOutputChannel = """
        <VideoOutputChannel xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <id>1</id>
          <resolution>1920*1080@60</resolution>
          <brightness>50</brightness>
          <contrast>50</contrast>
        </VideoOutputChannel>
        """;

    public const string VideoOutputChannelList = """
        <VideoOutputChannelList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <VideoOutputChannel>
            <id>1</id>
            <resolution>1920*1080@60</resolution>
          </VideoOutputChannel>
        </VideoOutputChannelList>
        """;

    public const string VideoOutputChannelAll = """
        <VideoOutputChannelAll xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <resolution>1920*1080@60</resolution>
        </VideoOutputChannelAll>
        """;

    public const string SignalSourceGroupJson = """
        {
          "GetSignalSourceGroup": {
            "pageNo": 1,
            "pageSize": 20
          }
        }
        """;

    public const string VideoInputChannel = """
        <VideoInputChannel xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <id>1</id>
          <name>Camera 1</name>
          <videoFormat>1080P60</videoFormat>
        </VideoInputChannel>
        """;

    public const string VideoInputChannelList = """
        <VideoInputChannelList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <VideoInputChannel>
            <id>1</id>
            <name>Camera 1</name>
          </VideoInputChannel>
        </VideoInputChannelList>
        """;

    public const string Color = """
        <Color xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <brightness>50</brightness>
          <contrast>50</contrast>
          <saturation>50</saturation>
          <hue>50</hue>
        </Color>
        """;

    public const string CutOff = """
        <CutOff xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <enabled>true</enabled>
          <X>0</X>
          <Y>0</Y>
          <width>1920</width>
          <height>1080</height>
        </CutOff>
        """;

    public const string CustomResolution = """
        <CustomResolution xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <width>1920</width>
          <height>1080</height>
          <frameRate>60</frameRate>
        </CustomResolution>
        """;

    public const string JoinSignal = """
        <JoinSignal xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <enabled>true</enabled>
          <joinType>horizontal</joinType>
        </JoinSignal>
        """;

    public const string StreamingChannel = """
        <StreamingChannel xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <id>1</id>
          <enabled>true</enabled>
          <url>rtsp://192.168.1.64:554/ch1/main/av_stream</url>
        </StreamingChannel>
        """;

    public const string StreamingChannelList = """
        <StreamingChannelList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <StreamingChannel>
            <id>1</id>
            <enabled>true</enabled>
          </StreamingChannel>
        </StreamingChannelList>
        """;

    public const string VideoWallPut = """
        <VideoWall xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <id>1</id>
          <name>Tuong Trung Tam</name>
          <WallOutputList>
            <WallOutput>
              <outputID>17235969</outputID>
              <Rect>
                <Coordinate>
                  <x>0</x>
                  <y>0</y>
                </Coordinate>
                <width>1920</width>
                <height>1920</height>
              </Rect>
            </WallOutput>
          </WallOutputList>
        </VideoWall>
        """;

    public const string PlanPost = """
        <WallPlan xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <id>1</id>
          <name>Plan 1</name>
          <ActTimeDetail>
            <actTimeMode>at once</actTimeMode>
          </ActTimeDetail>
          <PlanDetailList>
            <PlanDetail>
              <operationType>activateScene</operationType>
              <sceneID>1</sceneID>
              <duration>60</duration>
            </PlanDetail>
          </PlanDetailList>
          <actCount>1</actCount>
        </WallPlan>
        """;

    public const string ScenePut = """
        <WallScene xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <id>1</id>
          <name>Scene 1</name>
        </WallScene>
        """;

    public const string SceneSaveDataPut = """
        <SceneSaveData xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <id>1</id>
        </SceneSaveData>
        """;

    public const string SceneControlParamsJson = """
        {
          "SceneControlParams": {
            "sceneId": 1,
            "mode": "switch"
          }
        }
        """;

    public const string ScreenCloseAll = """
        <ScreenCtrl xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <VideoWallID>1</VideoWallID>
          <OutputID>1</OutputID>
        </ScreenCtrl>
        """;

    public const string VirtualLED = """
        <VirtualLED xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <id>1</id>
          <content>THÔNG TIN VIDEO WALL</content>
          <fontSize>32</fontSize>
          <fontColor>#FFFFFF</fontColor>
          <X>0</X>
          <Y>0</Y>
          <width>1920</width>
          <height>100</height>
        </VirtualLED>
        """;

    public const string VirtualLEDList = """
        <VirtualLEDList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <VirtualLED>
            <id>1</id>
            <content>THÔNG TIN VIDEO WALL</content>
            <fontSize>32</fontSize>
            <fontColor>#FFFFFF</fontColor>
          </VirtualLED>
        </VirtualLEDList>
        """;

    public const string BaseMap = """
        <BaseMap xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <id>1</id>
          <name>Background 1</name>
          <enabled>true</enabled>
        </BaseMap>
        """;

    public const string BaseMapList = """
        <BaseMapList xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <BaseMap>
            <id>1</id>
            <name>Background 1</name>
            <enabled>true</enabled>
          </BaseMap>
        </BaseMapList>
        """;

    public const string WallWindow = """
        <WallWindow xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <id>1</id>
          <wndOperateMode>uniformCoordinate</wndOperateMode>
          <Rect>
            <Coordinate>
              <x>0</x>
              <y>0</y>
            </Coordinate>
            <width>1920</width>
            <height>1920</height>
          </Rect>
          <windowMode>1</windowMode>
          <SubWindowList>
            <SubWindow>
              <id>1</id>
              <SubWindowParam>
                <signalMode>video input</signalMode>
                <videoInputChannelID>16842753</videoInputChannelID>
              </SubWindowParam>
            </SubWindow>
          </SubWindowList>
        </WallWindow>
        """;

    public const string ScenePost = """
        <WallScene xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <name>New Scene</name>
        </WallScene>
        """;

    public const string PlanPut = """
        <WallPlan xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <id>1</id>
          <name>Plan 1</name>
          <ActTimeDetail>
            <actTimeMode>at once</actTimeMode>
          </ActTimeDetail>
          <PlanDetailList>
            <PlanDetail>
              <operationType>activateScene</operationType>
              <sceneID>1</sceneID>
              <duration>60</duration>
            </PlanDetail>
          </PlanDetailList>
          <actCount>1</actCount>
        </WallPlan>
        """;

    public const string SceneImport = """
        {
          "SceneImport": {
            "data": "base64encodeddata=="
          }
        }
        """;

    public const string SubWindowSource = """
        <SubWindow xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
          <id>1</id>
          <SubWindowParam>
            <signalMode>video input</signalMode>
            <videoInputChannelID>16842753</videoInputChannelID>
          </SubWindowParam>
        </SubWindow>
        """;
}

/// <summary>
/// Description: Danh sách toàn bộ 64 preset API ISAPI mẫu được cấu hình sẵn theo chuẩn tài liệu Hikvision
/// Created date: 13/09/2026
/// </summary>
public static class VwIsapiPresetList
{
    public static readonly IReadOnlyList<VwIsapiPresetItem> Presets =
    [
        // Board
        new("9.7.1.4", "Lấy khả năng trạng thái của tất cả sub-board", "GET", "ISAPI/System/Board/status/capabilities", VwIsapiGroup.Board),

        // Decoding
        new("9.7.2.1", "Lấy trạng thái thiết bị giải mã", "GET", "ISAPI/DisplayDev/decoingDevice/status?format=json", VwIsapiGroup.Decoding),
        new("9.7.2.5", "Bắt đầu giải mã động", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/start", VwIsapiGroup.Decoding, VwIsapiSampleBodies.StartDynamicDecode),
        new("9.7.2.6", "Lấy trạng thái giải mã của tất cả cửa sổ con thuộc 1 cửa sổ cụ thể", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/status", VwIsapiGroup.Decoding),
        new("9.7.2.7", "Dừng giải mã động", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/stop", VwIsapiGroup.Decoding, VwIsapiSampleBodies.StopDynamicDecode),
        new("9.7.2.8", "Lấy trạng thái giải mã của tất cả cửa sổ con thuộc tất cả cửa sổ", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/status", VwIsapiGroup.Decoding),

        // Output Channel
        new("9.7.3.4", "Lấy tham số cơ bản của tất cả cổng xuất video", "GET", "ISAPI/DisplayDev/Video/outputs/channels", VwIsapiGroup.OutputChannel),
        new("9.7.3.5", "Lấy tham số của 1 cổng xuất video cụ thể", "GET", "ISAPI/DisplayDev/Video/outputs/channels/{channelID}", VwIsapiGroup.OutputChannel),
        new("9.7.3.6", "Cấu hình tham số của 1 cổng xuất video cụ thể", "PUT", "ISAPI/DisplayDev/Video/outputs/channels/{channelID}", VwIsapiGroup.OutputChannel, VwIsapiSampleBodies.VideoOutputChannel),
        new("9.7.3.7", "Lấy khả năng của 1 cổng xuất video cụ thể", "GET", "ISAPI/DisplayDev/Video/outputs/channels/{channelID}/capabilities", VwIsapiGroup.OutputChannel),

        // Signal Source
        new("9.7.4.8", "Lấy tham số của tất cả kênh vào video", "GET", "ISAPI/DisplayDev/Video/inputs/channels", VwIsapiGroup.SignalSource),
        new("9.7.4.10", "Cấu hình tham số của 1 nguồn tín hiệu được chỉ định", "PUT", "ISAPI/DisplayDev/Video/inputs/channels/{channelID}", VwIsapiGroup.SignalSource, VwIsapiSampleBodies.VideoInputChannel),
        new("9.7.4.15", "Lấy tham số cắt ảnh của 1 nguồn tín hiệu cụ thể", "GET", "ISAPI/DisplayDev/Video/inputs/channels/{channelID}/cutOff", VwIsapiGroup.SignalSource),
        new("9.7.4.16", "Cấu hình tham số cắt ảnh của 1 nguồn tín hiệu được chỉ định", "PUT", "ISAPI/DisplayDev/Video/inputs/channels/{channelID}/cutOff", VwIsapiGroup.SignalSource, VwIsapiSampleBodies.CutOff),
        new("9.7.4.18", "Lấy ảnh đã chụp", "GET", "ISAPI/DisplayDev/Video/inputs/channels/{channelID}/picture", VwIsapiGroup.SignalSource),

        // Video Wall
        new("9.7.5.1", "Lấy khả năng của bộ điều khiển tường ghép", "GET", "ISAPI/DisplayDev/capabilities", VwIsapiGroup.VideoWall),
        new("9.7.5.2", "Lấy tham số của tất cả tường ghép", "GET", "ISAPI/DisplayDev/VideoWall", VwIsapiGroup.VideoWall),
        new("9.7.5.3", "Cấu hình tham số của 1 tường ghép cụ thể", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}", VwIsapiGroup.VideoWall, VwIsapiSampleBodies.VideoWallPut),
        new("9.7.5.4", "Lấy tham số của 1 tường ghép cụ thể", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}", VwIsapiGroup.VideoWall),
        new("9.7.5.5", "Lấy tham số màn hình liên kết của tất cả cổng xuất", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/outputs", VwIsapiGroup.VideoWall),
        new("9.7.5.6", "Lấy khả năng của tường ghép", "GET", "ISAPI/DisplayDev/VideoWall/capabilities", VwIsapiGroup.VideoWall),

        // Screen
        new("9.7.8.1", "Đóng tất cả màn hình", "PUT", "ISAPI/DisplayDev/ScreenCtrl/closeAll", VwIsapiGroup.Screen, VwIsapiSampleBodies.ScreenCloseAll),

        // Window
        new("9.7.11.2", "Lấy tham số của tất cả cửa sổ", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows", VwIsapiGroup.Window),
        new("9.7.11.3", "Xoá tất cả cửa sổ", "DELETE", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows", VwIsapiGroup.Window),
        new("9.7.11.4", "Thêm 1 cửa sổ", "POST", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows", VwIsapiGroup.Window, VwIsapiSampleBodies.WallWindow),
        new("9.7.11.5", "Lấy cấu hình tham số của 1 cửa sổ cụ thể", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}", VwIsapiGroup.Window),
        new("9.7.11.6", "Cấu hình tham số của 1 cửa sổ cụ thể", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}", VwIsapiGroup.Window, VwIsapiSampleBodies.WallWindow),
        new("9.7.11.7", "Xoá 1 cửa sổ cụ thể", "DELETE", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}", VwIsapiGroup.Window),
        new("9.7.11.8", "Hạ cửa sổ xuống dưới cùng", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/bottom", VwIsapiGroup.Window),
        new("9.7.11.13", "Đưa cửa sổ lên trên cùng", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/top", VwIsapiGroup.Window),
        new("9.7.11.14", "Lấy khả năng cấu hình cửa sổ của tường ghép", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/capabilities", VwIsapiGroup.Window),

        // ════════════════════════════════════════════════════════════════════════════════════════
        // 85 API "Tùy chọn/mở rộng"
        // ════════════════════════════════════════════════════════════════════════════════════════

        // ── Board (9.7.1) ──
        new("9.7.1.1", "Cấu hình tham số của 1 sub-board được chỉ định", "PUT", "ISAPI/System/Board/{BoardID}/config", VwIsapiGroup.Board, VwIsapiSampleBodies.BoardConfig),
        new("9.7.1.2", "Lấy khả năng của sub-board", "GET", "ISAPI/System/Board/capabilities", VwIsapiGroup.Board),
        new("9.7.1.3", "Cấu hình tham số của tất cả sub-board", "PUT", "ISAPI/System/Board/config", VwIsapiGroup.Board, VwIsapiSampleBodies.BoardConfigList),

        // ── Decoding (9.7.2) ──
        new("9.7.2.2", "Lấy tham số pre-monitor mạng của 1 tường ghép", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/nPreMonitor", VwIsapiGroup.Decoding),
        new("9.7.2.3", "Cấu hình tham số pre-monitor mạng của 1 tường ghép", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/nPreMonitor", VwIsapiGroup.Decoding, VwIsapiSampleBodies.PreMonitor),
        new("9.7.2.4", "Lấy khả năng cấu hình cửa sổ con", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/param/capabilities", VwIsapiGroup.Decoding),
        new("9.7.2.9", "Lấy cấu hình xuất stream của sub-board", "GET", "ISAPI/DisplayDev/VideoWall/DecodeMgr/BoardStreamExportCfg?format=json", VwIsapiGroup.Decoding),
        new("9.7.2.10", "Cấu hình xuất stream của sub-board", "PUT", "ISAPI/DisplayDev/VideoWall/DecodeMgr/BoardStreamExportCfg?format=json", VwIsapiGroup.Decoding, VwIsapiSampleBodies.BoardStreamExportJson),
        new("9.7.2.11", "Lấy khả năng tham số độ trễ giải mã mặc định", "GET", "ISAPI/DisplayDev/VideoWall/DecodeMgr/DefaultDecodeDelayParams/capabilities?format=json", VwIsapiGroup.Decoding),
        new("9.7.2.12", "Lấy tham số độ trễ giải mã mặc định", "GET", "ISAPI/DisplayDev/VideoWall/DecodeMgr/DefaultDecodeDelayParams?format=json", VwIsapiGroup.Decoding),
        new("9.7.2.13", "Cấu hình tham số độ trễ giải mã mặc định", "PUT", "ISAPI/DisplayDev/VideoWall/DecodeMgr/DefaultDecodeDelayParams?format=json", VwIsapiGroup.Decoding, VwIsapiSampleBodies.DefaultDecodeDelayJson),
        new("9.7.2.14", "Lấy tham số pre-monitor mạng của tất cả tường ghép", "GET", "ISAPI/DisplayDev/VideoWall/nPreMonitor", VwIsapiGroup.Decoding),
        new("9.7.2.15", "Cấu hình tham số pre-monitor mạng của tất cả tường ghép", "PUT", "ISAPI/DisplayDev/VideoWall/nPreMonitor", VwIsapiGroup.Decoding, VwIsapiSampleBodies.PreMonitor),
        new("9.7.2.16", "Lấy khả năng tham số pre-monitor mạng của tường ghép", "GET", "ISAPI/DisplayDev/VideoWall/nPreMonitor/capabilities", VwIsapiGroup.Decoding),

        // ── Output Channel (9.7.3) ──
        new("9.7.3.1", "Lấy tham số của các kênh xuất audio", "GET", "ISAPI/DisplayDev/Audio/outputs/channels", VwIsapiGroup.OutputChannel),
        new("9.7.3.2", "Cấu hình tham số của tất cả kênh xuất audio", "PUT", "ISAPI/DisplayDev/Audio/outputs/channels", VwIsapiGroup.OutputChannel, VwIsapiSampleBodies.AudioOutputChannelList),
        new("9.7.3.3", "Cấu hình tham số của tất cả cổng xuất video", "PUT", "ISAPI/DisplayDev/Video/outputs/channels", VwIsapiGroup.OutputChannel, VwIsapiSampleBodies.VideoOutputChannelList),
        new("9.7.3.8", "Cấu hình tham số của tất cả kênh xuất video", "PUT", "ISAPI/DisplayDev/Video/outputs/channels/all", VwIsapiGroup.OutputChannel, VwIsapiSampleBodies.VideoOutputChannelAll),
        new("9.7.3.9", "Lấy khả năng cấu hình của tất cả kênh xuất video", "GET", "ISAPI/DisplayDev/Video/outputs/channels/capabilities", VwIsapiGroup.OutputChannel),

        // ── Signal Source (9.7.4) ──
        new("9.7.4.1", "Lấy khả năng audio", "GET", "ISAPI/DisplayDev/Audio/capabilities", VwIsapiGroup.SignalSource),
        new("9.7.4.2", "Lấy khả năng thêm nhóm nguồn tín hiệu", "GET", "ISAPI/DisplayDev/SignalSource/AddSignalSourceGroup/capabilities?format=json", VwIsapiGroup.SignalSource),
        new("9.7.4.3", "Lấy danh sách nhóm nguồn tín hiệu", "POST", "ISAPI/DisplayDev/SignalSource/GetSignalSourceGroup?format=json", VwIsapiGroup.SignalSource, VwIsapiSampleBodies.SignalSourceGroupJson),
        new("9.7.4.4", "Lấy khả năng sửa nhóm nguồn tín hiệu", "GET", "ISAPI/DisplayDev/SignalSource/ModifySignalSourceGroup/capabilities?format=json", VwIsapiGroup.SignalSource),
        new("9.7.4.5", "Lấy khả năng tham số khi mất tín hiệu của nguồn tín hiệu", "GET", "ISAPI/DisplayDev/SignalSource/SignalSourceNoSignalParams/capabilities?format=json", VwIsapiGroup.SignalSource),
        new("9.7.4.6", "Lấy tham số khi mất tín hiệu của nguồn tín hiệu", "GET", "ISAPI/DisplayDev/SignalSource/SignalSourceNoSignalParams?format=json", VwIsapiGroup.SignalSource),
        new("9.7.4.7", "Lấy khả năng video", "GET", "ISAPI/DisplayDev/Video/capabilities", VwIsapiGroup.SignalSource),
        new("9.7.4.9", "Cấu hình tham số của tất cả kênh vào video", "PUT", "ISAPI/DisplayDev/Video/inputs/channels", VwIsapiGroup.SignalSource, VwIsapiSampleBodies.VideoInputChannelList),
        new("9.7.4.11", "Lấy tham số của 1 nguồn tín hiệu cụ thể", "GET", "ISAPI/DisplayDev/Video/inputs/channels/{channelID}", VwIsapiGroup.SignalSource),
        new("9.7.4.12", "Lấy tham số màu của 1 nguồn tín hiệu cụ thể", "GET", "ISAPI/DisplayDev/Video/inputs/channels/{channelID}/color", VwIsapiGroup.SignalSource),
        new("9.7.4.13", "Cấu hình tham số màu của 1 nguồn tín hiệu được chỉ định", "PUT", "ISAPI/DisplayDev/Video/inputs/channels/{channelID}/color", VwIsapiGroup.SignalSource, VwIsapiSampleBodies.Color),
        new("9.7.4.14", "Lấy khả năng cấu hình màu của nguồn tín hiệu", "GET", "ISAPI/DisplayDev/Video/inputs/channels/{channelID}/color/capabilities", VwIsapiGroup.SignalSource),
        new("9.7.4.17", "Lấy khả năng cấu hình tham số cắt ảnh của nguồn tín hiệu", "GET", "ISAPI/DisplayDev/Video/inputs/channels/{channelID}/cutOff/capabilities", VwIsapiGroup.SignalSource),
        new("9.7.4.19", "Lấy khả năng cấu hình tham số chỉnh vị trí ảnh của nguồn tín hiệu", "GET", "ISAPI/DisplayDev/Video/inputs/channels/{channelID}/position/capabilities", VwIsapiGroup.SignalSource),
        new("9.7.4.20", "Cấu hình độ phân giải tuỳ chỉnh của 1 nguồn tín hiệu được chỉ định", "PUT", "ISAPI/DisplayDev/Video/inputs/channels/{channelID}/resolution", VwIsapiGroup.SignalSource, VwIsapiSampleBodies.CustomResolution),
        new("9.7.4.21", "Lấy khả năng tuỳ chỉnh độ phân giải của 1 nguồn tín hiệu được chỉ định", "GET", "ISAPI/DisplayDev/Video/inputs/channels/{channelID}/resolution/capabilities", VwIsapiGroup.SignalSource),
        new("9.7.4.22", "Lấy khả năng cấu hình OSD của nguồn tín hiệu", "GET", "ISAPI/DisplayDev/Video/inputs/channels/{channelID}/text/capabilities", VwIsapiGroup.SignalSource),
        new("9.7.4.23", "Lấy khả năng kênh vào video", "GET", "ISAPI/DisplayDev/Video/inputs/channels/capabilities", VwIsapiGroup.SignalSource),
        new("9.7.4.24", "Lấy cấu hình ghép của tất cả nguồn tín hiệu", "GET", "ISAPI/DisplayDev/Video/inputs/joinSignal", VwIsapiGroup.SignalSource),
        new("9.7.4.25", "Cấu hình tham số ghép của 1 nguồn tín hiệu được chỉ định", "PUT", "ISAPI/DisplayDev/Video/inputs/joinSignal/{channelID}", VwIsapiGroup.SignalSource, VwIsapiSampleBodies.JoinSignal),
        new("9.7.4.26", "Lấy tham số ghép của nguồn tín hiệu", "GET", "ISAPI/DisplayDev/Video/inputs/joinSignal/{channelID}", VwIsapiGroup.SignalSource),
        new("9.7.4.27", "Lấy khả năng ghép của nguồn tín hiệu", "GET", "ISAPI/DisplayDev/Video/inputs/joinSignal/capabilities", VwIsapiGroup.SignalSource),
        new("9.7.4.28", "Lấy tham số của tất cả stream video", "GET", "ISAPI/DisplayDev/Video/streaming/channels", VwIsapiGroup.SignalSource),
        new("9.7.4.29", "Cấu hình tham số của tất cả stream video", "PUT", "ISAPI/DisplayDev/Video/streaming/channels", VwIsapiGroup.SignalSource, VwIsapiSampleBodies.StreamingChannelList),
        new("9.7.4.30", "Cấu hình tham số của 1 stream video cụ thể", "PUT", "ISAPI/DisplayDev/Video/streaming/channels/{channelID}", VwIsapiGroup.SignalSource, VwIsapiSampleBodies.StreamingChannel),
        new("9.7.4.31", "Xoá tham số của 1 stream video cụ thể", "DELETE", "ISAPI/DisplayDev/Video/streaming/channels/{channelID}", VwIsapiGroup.SignalSource),
        new("9.7.4.32", "Lấy tham số của 1 stream video được chỉ định", "GET", "ISAPI/DisplayDev/Video/streaming/channels/{channelID}", VwIsapiGroup.SignalSource),
        new("9.7.4.33", "Lấy khả năng stream video", "GET", "ISAPI/DisplayDev/Video/streaming/channels/capabilities", VwIsapiGroup.SignalSource),
        new("9.7.4.34", "Lấy khả năng tìm tham số nguồn tín hiệu mạng", "GET", "ISAPI/DisplayDev/Video/streaming/channels/search/capabilities", VwIsapiGroup.SignalSource),

        // ── Plan (9.7.6) — nhóm mới ──
        new("9.7.6.1", "Thêm 1 plan", "POST", "ISAPI/DisplayDev/VideoWall/{videoWallID}/plan", VwIsapiGroup.Plan, VwIsapiSampleBodies.PlanPost),
        new("9.7.6.2", "Lấy khả năng cấu hình của 1 plan cụ thể", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/{planTemplateID}/capabilities", VwIsapiGroup.Plan),
        new("9.7.6.3", "Lấy khả năng cấu hình plan", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/capabilities", VwIsapiGroup.Plan),
        new("9.7.6.4", "Lấy plan hiện tại", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/isRunning", VwIsapiGroup.Plan),

        // ── Scene (9.7.7) — nhóm mới ──
        new("9.7.7.1", "Lấy tham số của tất cả scene", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/scene", VwIsapiGroup.Scene),
        new("9.7.7.2", "Cấu hình tham số của 1 scene cụ thể", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}", VwIsapiGroup.Scene, VwIsapiSampleBodies.ScenePut),
        new("9.7.7.3", "Chuyển sang 1 scene cụ thể", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}/activate", VwIsapiGroup.Scene),
        new("9.7.7.4", "Lưu scene hiện tại", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}/saveData", VwIsapiGroup.Scene, VwIsapiSampleBodies.SceneSaveDataPut),
        new("9.7.7.5", "Lấy khả năng cấu hình scene", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/capabilities", VwIsapiGroup.Scene),
        new("9.7.7.6", "Lấy scene hiện tại", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/isRunning", VwIsapiGroup.Scene),
        new("9.7.7.7", "Lấy khả năng tham số điều khiển scene", "GET", "ISAPI/DisplayDev/VideoWallScene/SceneControlParams/capabilities?format=json", VwIsapiGroup.Scene),
        new("9.7.7.8", "Lấy tham số điều khiển scene", "GET", "ISAPI/DisplayDev/VideoWallScene/SceneControlParams?format=json", VwIsapiGroup.Scene),
        new("9.7.7.9", "Cấu hình tham số điều khiển scene", "PUT", "ISAPI/DisplayDev/VideoWallScene/SceneControlParams?format=json", VwIsapiGroup.Scene, VwIsapiSampleBodies.SceneControlParamsJson),

        // ── Text/LED (9.7.9) — nhóm mới ──
        new("9.7.9.1", "Cấu hình tham số của tất cả LED ảo", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED", VwIsapiGroup.TextLed, VwIsapiSampleBodies.VirtualLEDList),
        new("9.7.9.2", "Lấy tham số của tất cả LED ảo", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED", VwIsapiGroup.TextLed),
        new("9.7.9.3", "Thêm tất cả LED ảo", "POST", "ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED", VwIsapiGroup.TextLed, VwIsapiSampleBodies.VirtualLED),
        new("9.7.9.4", "Lấy tham số của 1 LED được chỉ định", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED/{SubtitlesID}", VwIsapiGroup.TextLed),
        new("9.7.9.5", "Xoá 1 LED ảo cụ thể", "DELETE", "ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED/{SubtitlesID}", VwIsapiGroup.TextLed),
        new("9.7.9.6", "Cấu hình tham số của 1 LED ảo cụ thể", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED/{SubtitlesID}", VwIsapiGroup.TextLed, VwIsapiSampleBodies.VirtualLED),
        new("9.7.9.7", "Lấy khả năng cấu hình LED ảo", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED/{SubtitlesID}/capabilities", VwIsapiGroup.TextLed),
        new("9.7.9.8", "Lấy khả năng cấu hình của tất cả LED ảo", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/virtualLED/capabilities", VwIsapiGroup.TextLed),

        // ── Wallpaper (9.7.10) — nhóm mới ──
        new("9.7.10.1", "Lấy khả năng cấu hình cửa sổ ảnh nền", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/baseMap/{mapFileID}/capabilities", VwIsapiGroup.Wallpaper),
        new("9.7.10.2", "Lấy khả năng của tất cả ảnh nền", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/baseMap/capabilities", VwIsapiGroup.Wallpaper),
        new("9.7.10.3", "Cấu hình tham số của tất cả ảnh nền", "PUT", "ISAPI/DisplayDev/VideoWall/baseMap", VwIsapiGroup.Wallpaper, VwIsapiSampleBodies.BaseMapList),
        new("9.7.10.4", "Xoá 1 ảnh nền cụ thể", "DELETE", "ISAPI/DisplayDev/VideoWall/baseMap/{mapFileID}", VwIsapiGroup.Wallpaper),
        new("9.7.10.5", "Cấu hình tham số của 1 ảnh nền cụ thể", "PUT", "ISAPI/DisplayDev/VideoWall/baseMap/{mapFileID}", VwIsapiGroup.Wallpaper, VwIsapiSampleBodies.BaseMap),
        new("9.7.10.6", "Lấy tham số của 1 ảnh nền", "GET", "ISAPI/DisplayDev/VideoWall/baseMap/{mapFileID}", VwIsapiGroup.Wallpaper),
        new("9.7.10.7", "Lấy khả năng cấu hình ảnh nền", "GET", "ISAPI/DisplayDev/VideoWall/baseMap/capabilities", VwIsapiGroup.Wallpaper),
        new("9.7.10.8", "Lấy cấu hình của tất cả ảnh nền", "GET", "ISAPI/DisplayDev/VideoWall/baseMap?isGetBaseMapFile={isGetBaseMapFile}", VwIsapiGroup.Wallpaper),

        // ── Window (9.7.11) ──
        new("9.7.11.1", "Lấy vùng LED hoặc LCD", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/ledArea", VwIsapiGroup.Window),
        new("9.7.11.9", "Lấy khả năng cấu hình đơn lẻ của cửa sổ con", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/capabilities", VwIsapiGroup.Window),
        new("9.7.11.10", "Lấy tham số độ trễ giải mã", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/decodeDelay", VwIsapiGroup.Window),
        new("9.7.11.11", "Lấy khả năng độ trễ giải mã", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/decodeDelay/capabilities", VwIsapiGroup.Window),
        new("9.7.11.12", "Lấy khả năng cấu hình chế độ video mượt full-frame-rate", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}/fullFrame/capabilities", VwIsapiGroup.Window),
        new("9.7.11.15", "Lấy khả năng cấu hình tham số sub-stream ở chế độ đa màn hình", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/subSteam/capabilities?format=json", VwIsapiGroup.Window),
        new("9.7.11.16", "Lấy tham số cấu hình loại stream khi có nhiều cửa sổ", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/subSteam?format=json", VwIsapiGroup.Window),
        new("9.7.11.17", "Lấy khả năng pre-editing của tường ghép", "GET", "ISAPI/DisplayDev/VideoWall/preEdit/capabilities?format=json", VwIsapiGroup.Window),
        new("9.7.11.18", "Đổi nguồn của 1 cửa sổ con (đặt lại videoInputChannelID)", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID}/sub/{VWSWID}", VwIsapiGroup.Window, VwIsapiSampleBodies.SubWindowSource),

        // ════════════════════════════════════════════════════════════════════════════════════════
        // W2: System APIs (ngoài namespace ISAPI)
        // ════════════════════════════════════════════════════════════════════════════════════════
        new("SYS-1", "Kiểm tra thiết bị hoạt động (SDK liveness)", "GET", "SDK/activateStatus", VwIsapiGroup.Board),
        new("SYS-2", "Lấy thông tin thiết bị", "GET", "ISAPI/System/deviceInfo", VwIsapiGroup.Board),
        new("SYS-3", "Lấy thời gian hệ thống", "GET", "ISAPI/System/time", VwIsapiGroup.Board),
        new("SYS-4", "Lấy danh sách cổng serial", "GET", "ISAPI/System/Serial/ports", VwIsapiGroup.Board),
        new("SYS-5", "Lấy khả năng cổng serial", "GET", "ISAPI/System/Serial/ports/capabilities", VwIsapiGroup.Board),
        new("SYS-6", "Lấy khả năng serial chung", "GET", "ISAPI/System/Serial/capabilities", VwIsapiGroup.Board),

        // ════════════════════════════════════════════════════════════════════════════════════════
        // W3: Scene — bổ sung POST / DELETE / hidden APIs
        // ════════════════════════════════════════════════════════════════════════════════════════
        new("9.7.7.X1", "Tạo scene mới", "POST", "ISAPI/DisplayDev/VideoWall/{videoWallID}/scene", VwIsapiGroup.Scene, VwIsapiSampleBodies.ScenePost),
        new("9.7.7.X2", "Xoá 1 scene cụ thể", "DELETE", "ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}", VwIsapiGroup.Scene),
        new("9.7.7.X3", "Xoá tất cả scene", "DELETE", "ISAPI/DisplayDev/VideoWall/{videoWallID}/scene", VwIsapiGroup.Scene),
        new("9.7.7.X4", "Lấy thông tin scene (JSON)", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}/sceneInfo?format=json", VwIsapiGroup.Scene),
        new("9.7.7.X5", "Xuất scene (backup toàn bộ)", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/export?format=json", VwIsapiGroup.Scene),
        new("9.7.7.X6", "Nhập scene (restore)", "POST", "ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/import?format=json", VwIsapiGroup.Scene, VwIsapiSampleBodies.SceneImport),
        new("9.7.7.X7", "Sao chép scene", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/scene/{SID}/copy", VwIsapiGroup.Scene),

        // ════════════════════════════════════════════════════════════════════════════════════════
        // W4: Plan — bổ sung GET list / PUT / DELETE / start / stop
        // ════════════════════════════════════════════════════════════════════════════════════════
        new("9.7.6.X1", "Lấy danh sách tất cả plan", "GET", "ISAPI/DisplayDev/VideoWall/{videoWallID}/plan", VwIsapiGroup.Plan),
        new("9.7.6.X2", "Cập nhật 1 plan cụ thể", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/{planTemplateID}", VwIsapiGroup.Plan, VwIsapiSampleBodies.PlanPut),
        new("9.7.6.X3", "Xoá 1 plan cụ thể", "DELETE", "ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/{planTemplateID}", VwIsapiGroup.Plan),
        new("9.7.6.X4", "Kích hoạt plan", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/{planTemplateID}/start", VwIsapiGroup.Plan),
        new("9.7.6.X5", "Tạm dừng plan", "PUT", "ISAPI/DisplayDev/VideoWall/{videoWallID}/plan/{planTemplateID}/stop", VwIsapiGroup.Plan),
    ];
}
