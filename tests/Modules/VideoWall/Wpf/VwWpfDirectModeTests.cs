using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Api.Direct;
using Module.VideoWall.WPF.Api.Dto;
using Module.VideoWall.WPF.Auth;
using Module.VideoWall.WPF.Controls;
using Module.VideoWall.WPF.Interaction;
using Module.VideoWall.WPF.Storage;
using Module.VideoWall.WPF.ViewModels;
using Module.VideoWall.WPF.ViewModels.Isapi;
using Services.Shared.Events;
using Tests.Modules.VideoWall.MockServer;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf;

public class VwWpfDirectModeTests
{
    [Fact]
    public async Task DirectMode_Ping_ReturnsSuccess()
    {
        var (client, mockServer) = BuildDirectClient(18121);
        using (mockServer)
        {
            var result = await client.Ping();
            Assert.True(result.Success);
        }
    }

    [Fact]
    public async Task DirectMode_Probe_ReadsTopology()
    {
        var (client, mockServer) = BuildDirectClient(18122);
        using (mockServer)
        {
            // Probe không có WallNo -> tự động phân giải tường đầu tiên và nạp đầy đủ Walls & Outputs (12 màn hình)
            var resultWithoutWall = await client.Probe(null);
            Assert.True(resultWithoutWall.Reachable);
            Assert.NotEmpty(resultWithoutWall.Walls!);
            Assert.Equal(12, resultWithoutWall.Outputs!.Count);
            Assert.Equal(1, resultWithoutWall.WallNo);

            // Probe WallNo=1 -> đọc 12 Outputs của Wall 1
            var resultWithWall1 = await client.Probe(1);
            Assert.True(resultWithWall1.Reachable);
            Assert.NotEmpty(resultWithWall1.Walls!);
            Assert.Equal(12, resultWithWall1.Outputs!.Count);
            Assert.Equal(1, resultWithWall1.WallNo);

            // Probe WallNo=2 -> đọc 12 Outputs của Wall 2 (lưới 4x3)
            var resultWithWall2 = await client.Probe(2);
            Assert.True(resultWithWall2.Reachable);
            Assert.NotEmpty(resultWithWall2.Walls!);
            Assert.Equal(12, resultWithWall2.Outputs!.Count);
            Assert.Equal(2, resultWithWall2.WallNo);
        }
    }

    [Fact]
    public async Task DirectMode_SendIsapi_ReturnsXml()
    {
        var (client, mockServer) = BuildDirectClient(18123);
        using (mockServer)
        {
            var result = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/capabilities", null, null);
            Assert.True(result.Success);
            Assert.Contains("VideoWallCap", result.ResponseXml);
        }
    }

    [Fact]
    public async Task DirectMode_SendIsapi_ListWrapper_AudioOutputChannelList_HasWrapperInRequestXml()
    {
        const int port = 18124;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var preset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.3.2");
        var formVm = new VwIsapiFormViewModel(preset)
        {
            RawBody = "<AudioOutputChannelList xmlns=\"http://www.isapi.org/ver20/XMLSchema\"><AudioOutputChannel><id>1</id><enabled>true</enabled></AudioOutputChannel></AudioOutputChannelList>"
        };

        var body = formVm.RawBody;
        Assert.NotNull(body);

        var publisher = new RecordingPublisherTest();
        var isApiClient = BuildIsapiClient(port);
        var client = new VwDirectDeviceConnectionClient(isApiClient, publisher);

        var step = await client.SendIsapi(preset.Method, formVm.BuildPath(), body, null);

        Assert.NotNull(step);
        Assert.NotNull(step.RequestXml);
        Assert.Contains("<AudioOutputChannelList", step.RequestXml);
        Assert.Contains("<AudioOutputChannel>", step.RequestXml);
        Assert.Contains("<id>1</id>", step.RequestXml);
        Assert.Contains("<enabled>true</enabled>", step.RequestXml);
        Assert.Contains("</AudioOutputChannelList>", step.RequestXml);

        var lastNotification = Assert.Single(publisher.DeviceStepRows);
        Assert.Contains("<AudioOutputChannelList", lastNotification.Step.RequestXml);
    }

    [Fact]
    public async Task DirectMode_SendIsapi_JsonBody_SendsJsonPayloadToMockServer()
    {
        const int port = 18125;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var preset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.2.10");
        var formVm = new VwIsapiFormViewModel(preset)
        {
            RawBody = "{ \"enabled\": true }"
        };

        var body = formVm.RawBody;
        Assert.NotNull(body);
        Assert.Contains("\"enabled\": true", body);

        var publisher = new RecordingPublisherTest();
        var isApiClient = BuildIsapiClient(port);
        var client = new VwDirectDeviceConnectionClient(isApiClient, publisher);

        var step = await client.SendIsapi(preset.Method, formVm.BuildPath(), body, "application/json");

        Assert.NotNull(step);
        Assert.NotNull(step.RequestXml);
        Assert.Contains("\"enabled\": true", step.RequestXml);

        var lastNotification = Assert.Single(publisher.DeviceStepRows);
        Assert.Contains("\"enabled\": true", lastNotification.Step.RequestXml);
    }

    [Fact]
    public async Task DirectMode_PushScene_CreatesWindows()
    {
        const int port = 18126;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var isApiClient = BuildIsapiClient(port);
        var orchestrator = new VwDirectSetupSceneOrchestrator(isApiClient);

        var input = new VwDirectPushSceneInput
        {
            SceneId = 1,
            WallNo = 1,
            DryRun = false,
            ResetWindows = true,
            Activate = false,
            Windows =
            [
                new VwDirectWindowInput(),
                new VwDirectWindowInput()
            ]
        };

        var result = await orchestrator.Execute(input, default);

        Assert.True(result.Success);
        Assert.True(mockServer.AddWindowCallCount >= 2);
        Assert.True(mockServer.SaveSceneDataCallCount >= 1);
    }

    [Fact]
    public async Task DirectMode_PushScene_WithoutWallNo_ReturnsFailureWithoutDeviceWrites()
    {
        const int port = 18125;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var isApiClient = BuildIsapiClient(port);
        var orchestrator = new VwDirectSetupSceneOrchestrator(isApiClient);

        var input = new VwDirectPushSceneInput
        {
            SceneId = 1,
            WallNo = null,
            DryRun = false,
            ResetWindows = true,
            Activate = false,
            Windows =
            [
                new VwDirectWindowInput(),
                new VwDirectWindowInput()
            ]
        };

        var result = await orchestrator.Execute(input, default);

        Assert.False(result.Success);
        Assert.Contains("Chưa chỉ định WallNo", result.Message);
        Assert.Equal(0, mockServer.AddWindowCallCount);
    }

    [Fact]
    public async Task DirectMode_PushScene_DryRun_SkipsDeviceWrites()
    {
        const int port = 18127;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var isApiClient = BuildIsapiClient(port);
        var orchestrator = new VwDirectSetupSceneOrchestrator(isApiClient);

        var input = new VwDirectPushSceneInput
        {
            SceneId = 1,
            WallNo = 1,
            DryRun = true,
            ResetWindows = true,
            Activate = false,
            Windows =
            [
                new VwDirectWindowInput(),
                new VwDirectWindowInput()
            ]
        };

        var result = await orchestrator.Execute(input, default);

        Assert.True(result.Success);
        Assert.All(result.Steps.Where(s => s.Method != "GET"), s => Assert.True(s.Skipped));
        Assert.Equal(0, mockServer.AddWindowCallCount);
    }

    [Fact]
    public async Task DirectMode_PushScene_SaveDataFalse_SkipsSaveSceneData_ExecutesWindowWrites_Test()
    {
        const int port = 18144;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var isApiClient = BuildIsapiClient(port);
        var orchestrator = new VwDirectSetupSceneOrchestrator(isApiClient);

        var input = new VwDirectPushSceneInput
        {
            SceneId = 1,
            WallNo = 1,
            DryRun = false,
            ResetWindows = true,
            SaveData = false, // KHÔNG chụp hình / lưu scene
            Activate = false,
            Windows =
            [
                new VwDirectWindowInput { X = 0, Y = 0, W = 1920, H = 1920, ZIndex = 1, SignalNo = 1 },
                new VwDirectWindowInput { X = 1920, Y = 0, W = 1920, H = 1920, ZIndex = 1, SignalNo = 2 }
            ]
        };

        var result = await orchestrator.Execute(input, default);

        Assert.True(result.Success);
        Assert.True(mockServer.AddWindowCallCount >= 2);
        Assert.Equal(0, mockServer.SaveSceneDataCallCount);

        var saveStep = result.Steps.FirstOrDefault(s => s.Name == "SaveSceneData");
        Assert.NotNull(saveStep);
        Assert.True(saveStep.Skipped);
        Assert.Contains("SaveData = false", saveStep.Message);
    }

    [Fact]
    public async Task DirectMode_VideoWall_ConsistencyBetweenListAndSingleWallEndpoints_Test()
    {
        var (client, mockServer) = BuildDirectClient(18145);
        using (mockServer)
        {
            var isApiClient = BuildIsapiClient(18145);

            // 1. GET ISAPI/DisplayDev/VideoWall (List)
            var listResult = await isApiClient.GetVideoWalls(default);
            Assert.True(listResult.Success);
            Assert.NotNull(listResult.Data?.VideoWall);
            Assert.Equal(2, listResult.Data.VideoWall.Count);

            var wall1FromList = listResult.Data.VideoWall.First(w => w.Id == 1);
            Assert.Equal("bound", wall1FromList.WallBindOutputStatus);
            Assert.True(wall1FromList.IsBound);

            // 2. GET ISAPI/DisplayDev/VideoWall/1 (Single Wall)
            var singleResult = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/1", null, null);
            Assert.True(singleResult.Success);
            Assert.Contains("<wallBindOutputStatus>bound</wallBindOutputStatus>", singleResult.ResponseXml);

            // 3. Response Summary Badges for Single Wall
            var summary = VwIsapiResponseSummary.Parse(singleResult.ResponseXml);
            Assert.True(summary.HasResponse);
            Assert.Equal("VideoWall", summary.RootElement);
            Assert.Contains(summary.Badges, b => b.Contains("Tường #1: VideoWall1"));
            Assert.Contains(summary.Badges, b => b.Contains("bound"));

            // 4. Response Summary Badges for List
            var listSummary = VwIsapiResponseSummary.Parse(listResult.RawResponse);
            Assert.True(listSummary.HasResponse);
            Assert.Equal("VideoWallList", listSummary.RootElement);
            Assert.Contains(listSummary.Badges, b => b.Contains("2 Tường"));
            Assert.Contains(listSummary.Badges, b => b.Contains("2 bound"));
        }
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Thiết bị từ chối Digest Auth khi sai thông tin đăng nhập, trả về kết quả thất bại mà không ném ngoại lệ.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task DirectMode_Ping_WithWrongDeviceCredentials_ReportsFailureWithoutThrowing()
    {
        const int port = 18128;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);
        mockServer.VerifyDigestResponseHash = true;

        var credentials = new VwDirectDeviceCredentials("127.0.0.1", port, "admin", "SaiMatKhau!");
        var digestHandler = new VwDirectDigestHandler { InnerHandler = new HttpClientHandler() };
        var httpClient = new HttpClient(digestHandler)
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        var isApiClient = new VwDirectISAPIClient(httpClient, credentials);
        var client = new VwDirectDeviceConnectionClient(isApiClient, new RecordingPublisherTest());

        var result = await client.Ping();

        Assert.False(result.Success);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Nonce hết hạn giữa chừng (stale="true") được DigestHandler tự động retry với nonce mới và thành công.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task DirectMode_Ping_WithStaleNonce_RetriesAndSucceeds()
    {
        var (client, mockServer) = BuildDirectClient(18129);
        using (mockServer)
        {
            mockServer.SimulateNonceExpiry = true;
            var result = await client.Ping();

            Assert.True(result.Success);
            Assert.Equal(1, mockServer.NonceExpiryTriggerCount);
        }
    }

    [Fact]
    public async Task DirectMode_SendIsapi_DecodeStart_DoesNotTriggerSwitchSource()
    {
        var (client, mockServer) = BuildDirectClient(18130);
        using (mockServer)
        {
            var step = await client.SendIsapi("PUT", "ISAPI/DisplayDev/VideoWall/1/windows/33554433/sub/1/start", null, null);

            Assert.NotNull(step);
            Assert.Equal(200, step.HttpStatus);
            Assert.Equal(0, mockServer.SwitchSourceCallCount);
        }
    }

    // Các endpoint GET chỉ cần khẳng định "200 + payload chứa/không chứa token". Mỗi case giữ port
    // riêng như bản [Fact] gốc để không thay đổi hành vi lắng nghe của mock server.
    // (port, path, tokens bắt buộc có, tokens bắt buộc KHÔNG có)
    public static TheoryData<int, string, string[], string[]> SimpleGetEndpointCases => new()
    {
        { 18131, "ISAPI/DisplayDev/VideoWall/1/windows/status", ["WallWindowStatusList", "isDecoding"], ["WallWindowList"] },
        { 18141, "ISAPI/DisplayDev/VideoWall/1/windows/33554433/sub/1/status", ["WallWindowStatusList", "<isDecoding>true</isDecoding>"], ["DynamicDecodeStatus"] },
        { 18132, "ISAPI/DisplayDev/Audio/outputs/channels", ["AudioOutputChannelList"], ["VideoOutputChannelList"] },
        { 18134, "ISAPI/DisplayDev/VideoWall/1", ["<VideoWall"], ["<VideoWallList"] },
        { 18136, "SDK/activateStatus", ["<Activated>true</Activated>"], [] }
    };

    [Theory]
    [MemberData(nameof(SimpleGetEndpointCases))]
    public async Task DirectMode_SendIsapi_GetEndpoint_ReturnsExpectedPayload_Test(
        int port, string path, string[] mustContain, string[] mustNotContain)
    {
        var (client, mockServer) = BuildDirectClient(port);
        using (mockServer)
        {
            var step = await client.SendIsapi("GET", path, null, null);

            Assert.NotNull(step);
            Assert.Equal(200, step.HttpStatus);
            Assert.NotNull(step.ResponseXml);

            foreach (var token in mustContain)
                Assert.Contains(token, step.ResponseXml);

            foreach (var token in mustNotContain)
                Assert.DoesNotContain(token, step.ResponseXml);
        }
    }

    [Fact]
    public async Task DirectMode_SendIsapi_CapturedPicture_ReturnsBinaryImage_WithoutQueryingInputChannels_Test()
    {
        var (client, mockServer) = BuildDirectClient(18133);
        using (mockServer)
        {
            var step = await client.SendIsapi("GET", "ISAPI/DisplayDev/Video/inputs/channels/16842753/picture", null, null);

            Assert.NotNull(step);
            Assert.Equal(200, step.HttpStatus);
            Assert.NotNull(step.ResponseXml);
            Assert.Contains("BINARY IMAGE DATA", step.ResponseXml);
            Assert.Contains("[data:image/jpeg;base64,", step.ResponseXml);
            Assert.Equal(0, mockServer.GetInputChannelsCallCount);
        }
    }

    [Fact]
    public async Task DirectMode_SendIsapi_All135Presets_ReturnHttpStatus200_AndValidResponses()
    {
        var (client, mockServer) = BuildDirectClient(18135);
        using (mockServer)
        {
            Assert.Equal(135, VwIsapiPresetList.Presets.Count);

            foreach (var preset in VwIsapiPresetList.Presets)
            {
                var resolvedUrl = preset.Url;
                if (resolvedUrl.Contains("Video/outputs/channels/{channelID}", StringComparison.OrdinalIgnoreCase))
                {
                    resolvedUrl = resolvedUrl.Replace("{channelID}", "17235971");
                }
                else if (resolvedUrl.Contains("Video/inputs/channels/", StringComparison.OrdinalIgnoreCase))
                {
                    resolvedUrl = resolvedUrl.Replace("{channelID}", "16842753")
                                             .Replace("{inputID}", "16842753")
                                             .Replace("{inputChannelID}", "16842753");
                }
                resolvedUrl = System.Text.RegularExpressions.Regex.Replace(resolvedUrl, @"\{[^}]+\}", "1");
                var body = preset.Method is "PUT" or "POST" ? "<dummy/>" : null;
                var contentType = preset.Url.Contains("format=json") ? "application/json" : "application/xml";
                if (contentType == "application/json" && body != null)
                    body = "{}";

                var step = await client.SendIsapi(preset.Method, resolvedUrl, body, contentType);

                Assert.True(step.HttpStatus == 200, $"Preset {preset.Section} ({preset.Method} {resolvedUrl}) failed with status {step.HttpStatus}: {step.Message}");
                Assert.False(string.IsNullOrWhiteSpace(step.ResponseXml), $"Preset {preset.Section} ({preset.Method} {resolvedUrl}) returned empty response");
            }
        }
    }

    [Fact]
    public async Task ConnectionViewModel_SendIsapiCommand_DoesNotOverwriteStatusMessageWithIsapiGetText_Test()
    {
        const int port = 18136;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
            StatusMessage = "Probe trực tiếp xong: WallNo 1, max 64 windows.",
        };

        connection.IsapiMethod = "GET";
        connection.IsapiPath = "ISAPI/DisplayDev/Audio/capabilities";

        // Act
        await connection.SendIsapiCommand.ExecuteAsync(null);

        // Assert: Response is populated, activity log published, but StatusMessage does NOT have verbose ISAPI GET text
        Assert.False(string.IsNullOrWhiteSpace(connection.IsapiResponse));
        Assert.DoesNotContain("ISAPI GET", connection.StatusMessage);
        Assert.DoesNotContain("Xem chi tiết ở tab Log", connection.StatusMessage);
        Assert.Equal("Probe trực tiếp xong: WallNo 1, max 64 windows.", connection.StatusMessage);

        // Verify activity log received the log row
        Assert.Contains(recordingPub.ActivityRows, a => a.Activity.Detail.Contains("ISAPI/DisplayDev/Audio/capabilities"));
    }

    [Fact]
    public async Task DirectMode_ConnectionViewModel_ConnectCommand_PingDevice_SetsConnected_Test()
    {
        const int port = 18137;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
        };

        // Act: Click Connect
        await connection.ConnectCommand.ExecuteAsync(null);

        // Assert
        Assert.True(connection.IsConnected);
        Assert.Contains("Kết nối & khảo sát thành công", connection.StatusMessage);
    }

    [Fact]
    public async Task DirectMode_ConnectionViewModel_ProbeCommand_PopulatesDynamicOutputResolution_Test()
    {
        const int port = 18138;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
        };

        // Act: Run Probe
        await connection.ProbeCommand.ExecuteAsync(null);

        // Assert
        Assert.True(connection.HasProbeResult);
        Assert.Equal("7680 × 5760 px", connection.ProbeTotalDimensionText);
        Assert.Equal("12 Cổng (Màn hình)", connection.ProbeOutputCountText);
        Assert.Equal("1920 × 1920 px / cổng ra", connection.ProbeOutputResolutionSummary);
        Assert.Equal("Lưới 4 × 3 — 12 màn hình", connection.ProbeWallDimensionsFormatted);
    }

    [Fact]
    public void DirectMode_ConnectionViewModel_UnprobedState_ReturnsDashesAndUnprobedNotice_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));

        // Assert: Before probing, all dynamic text properties return dashes and no hardcoded dimensions
        Assert.False(connection.HasProbeResult);
        Assert.Equal("-- × --", connection.ProbeTotalDimensionText);
        Assert.Equal("-- Cổng", connection.ProbeOutputCountText);
        Assert.Equal("-- Kênh", connection.ProbeInputCountText);
        Assert.Equal("--", connection.ProbeMaxWindowNumsText);
        Assert.Equal("--", connection.ProbeMaxSceneNumsText);
        Assert.Equal("(Chưa khảo sát)", connection.ProbeOutputResolutionSummary);
        Assert.Equal("(Chưa khảo sát)", connection.ProbeWallDimensionsFormatted);
    }

    [Fact]
    public void DirectMode_ConnectionViewModel_BuildDirectISAPIClient_ReturnsClientWhenConfigured_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = 18080,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!"
        };

        var isapiClient = connection.BuildDirectISAPIClient();

        Assert.NotNull(isapiClient);
    }

    [Fact]
    public async Task DirectMode_SceneSetupViewModel_ActivateSceneCommand_CallsIsapiActivate_UpdatesStoreAndStatus_Test()
    {
        const int port = 18139;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.139",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
        };

        await connection.ConnectCommand.ExecuteAsync(null);
        var sceneSetup = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub);
        var targetScene = new Module.VideoWall.WPF.Api.Dto.VwSceneDto
        {
            ID = "SCENE_ACT_TEST_CUSTOM",
            Name = "Kịch bản Kiểm thử Active Custom",
            Code = "SCENE_ACT_CUSTOM",
            OutputId = "1"
        };
        VwLocalSceneStore.SaveData(connection.DeviceKey, new VwLocalSceneData { Scenes = [targetScene] });
        sceneSetup.Scenes.Clear();
        sceneSetup.Scenes.Add(targetScene);
        sceneSetup.CurrentScene = targetScene;

        // Act: Execute ActivateSceneCommand
        await sceneSetup.ActivateSceneCommand.ExecuteAsync(null);

        // Assert: ActiveScene is set, store is updated, and status message reports success with SID
        Assert.NotNull(sceneSetup.ActiveScene);
        Assert.Equal(targetScene.ID, sceneSetup.ActiveScene.ID);
        Assert.Contains("Đã kích hoạt thành công kịch bản", sceneSetup.StatusMessage);

        var activeSceneInStore = VwLocalSceneStore.GetActiveScene(connection.DeviceKey);
        Assert.NotNull(activeSceneInStore);
        Assert.Equal(targetScene.ID, activeSceneInStore.ID);
    }

    [Fact]
    public async Task DirectMode_SceneSetupViewModel_ActivateScene_OnWall2_SendsWall2Endpoint_Test()
    {
        const int port = 18146;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 2,
        };

        var sceneSetup = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub);
        var targetScene = new Module.VideoWall.WPF.Api.Dto.VwSceneDto
        {
            ID = "TEST_SCENE_WALL2",
            Name = "Kịch bản Tường 2",
            Code = "SCENE_WALL2",
            OutputId = "3"
        };
        sceneSetup.Scenes.Add(targetScene);
        sceneSetup.CurrentScene = targetScene;

        // Act: Execute ActivateScene on Wall #2
        await sceneSetup.ActivateSceneCommand.ExecuteAsync(null);

        // Assert: Thao tác gửi ISAPI thành công tới Wall 2 với SID 3
        Assert.NotNull(sceneSetup.ActiveScene);
        Assert.Equal(targetScene.ID, sceneSetup.ActiveScene.ID);
        Assert.Contains("lên tường #2", sceneSetup.StatusMessage);
    }

    [Fact]
    public async Task DirectMode_MockServer_Wall1AndWall2_DistinctSceneList_Test()
    {
        var (client, mockServer) = BuildDirectClient(18147);
        using (mockServer)
        {
            var resWall1 = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/1/scene", null, null);
            Assert.Equal(200, resWall1.HttpStatus);
            Assert.Contains("Hữu Nghị - Chi Lăng", resWall1.ResponseXml);

            var resWall2 = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/2/scene", null, null);
            Assert.Equal(200, resWall2.HttpStatus);
            Assert.Contains("Hữu Nghị - Chi Lăng", resWall2.ResponseXml);
            Assert.Contains("Sự cố Trọng điểm", resWall2.ResponseXml);
        }
    }

    [Fact]
    public void DirectMode_MainViewModel_IsResponseVisible_Tab0False_Tab1PlusTrue_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var session = new SessionState();
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(new HttpClient()), activityPub);
        var apiClient = new VideoWallApiClient(invoker, recordingPub, activityPub);
        var parameters = new ParametersViewModel(connection);
        var sceneSetup = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub);
        var schedule = new ScheduleViewModel(apiClient, activityPub);
        var scenario = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true));

        var mainVm = new MainViewModel(session, activityPub, connection, parameters, sceneSetup, schedule, scenario);

        // Tab 0: Thiết lập Scene & Bố cục -> IsResponseVisible must be false
        mainVm.SelectedTabIndex = 0;
        Assert.False(mainVm.IsResponseVisible);

        // Tab 1..11 (ISAPI tabs): IsResponseVisible must be true
        mainVm.SelectedTabIndex = 1;
        Assert.True(mainVm.IsResponseVisible);

        mainVm.SelectedTabIndex = 5;
        Assert.True(mainVm.IsResponseVisible);

        mainVm.SelectedTabIndex = 11;
        Assert.True(mainVm.IsResponseVisible);
    }

    [Fact]
    public async Task DirectMode_SendIsapi_SystemEndpoints_ReturnExpectedXml_Test()
    {
        var (client, mockServer) = BuildDirectClient(18137);
        using (mockServer)
        {
            var devInfo = await client.SendIsapi("GET", "ISAPI/System/deviceInfo", null, null);
            Assert.Equal(200, devInfo.HttpStatus);
            Assert.Contains("DS-C30S-S11", devInfo.ResponseXml);

            var time = await client.SendIsapi("GET", "ISAPI/System/time", null, null);
            Assert.Equal(200, time.HttpStatus);
            Assert.Contains("<Time", time.ResponseXml);

            var ports = await client.SendIsapi("GET", "ISAPI/System/Serial/ports", null, null);
            Assert.Equal(200, ports.HttpStatus);
            Assert.Contains("SerialPortList", ports.ResponseXml);

            var portCaps = await client.SendIsapi("GET", "ISAPI/System/Serial/ports/capabilities", null, null);
            Assert.Equal(200, portCaps.HttpStatus);
            Assert.Contains("SerialPortCap", portCaps.ResponseXml);

            var serialCaps = await client.SendIsapi("GET", "ISAPI/System/Serial/capabilities", null, null);
            Assert.Equal(200, serialCaps.HttpStatus);
            Assert.Contains("SerialCap", serialCaps.ResponseXml);
        }
    }

    [Fact]
    public async Task DirectMode_SendIsapi_StatefulSceneCRUD_Test()
    {
        var (client, mockServer) = BuildDirectClient(18138);
        using (mockServer)
        {
            var list1 = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/1/scene", null, null);
            Assert.Equal(200, list1.HttpStatus);
            Assert.Contains("Hữu Nghị - Chi Lăng", list1.ResponseXml);

            var post = await client.SendIsapi("POST", "ISAPI/DisplayDev/VideoWall/1/scene", "<WallScene><name>Scene Alpha</name></WallScene>", "application/xml");
            Assert.Equal(200, post.HttpStatus);
            Assert.Contains("<ID>", post.ResponseXml);

            var list2 = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/1/scene", null, null);
            Assert.Contains("Scene Alpha", list2.ResponseXml);

            // Export all scenes
            var exportRes = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/1/scene/export?format=json", null, "application/json");
            Assert.Equal(200, exportRes.HttpStatus);
            Assert.Contains("SceneExport", exportRes.ResponseXml);

            // Import scenes
            var importRes = await client.SendIsapi("POST", "ISAPI/DisplayDev/VideoWall/1/scene/import?format=json", "{\"SceneImport\":{\"data\":\"dGVzdA==\"}}", "application/json");
            Assert.Equal(200, importRes.HttpStatus);
            Assert.Contains("<ID>", importRes.ResponseXml);

            // Copy scene 1
            var copyRes = await client.SendIsapi("PUT", "ISAPI/DisplayDev/VideoWall/1/scene/1/copy", null, null);
            Assert.Equal(200, copyRes.HttpStatus);

            var createdSceneId = System.Text.RegularExpressions.Regex.Match(post.ResponseXml, @"<ID>(\d+)</ID>").Groups[1].Value;
            var del = await client.SendIsapi("DELETE", $"ISAPI/DisplayDev/VideoWall/1/scene/{createdSceneId}", null, null);
            Assert.Equal(200, del.HttpStatus);

            var list3 = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/1/scene", null, null);
            Assert.DoesNotContain("Scene Alpha", list3.ResponseXml);
        }
    }

    [Fact]
    public async Task DirectMode_SendIsapi_StatefulPlanCRUD_Test()
    {
        var (client, mockServer) = BuildDirectClient(18139);
        using (mockServer)
        {
            var list1 = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/1/plan", null, null);
            Assert.Equal(200, list1.HttpStatus);
            Assert.Contains("Default Plan", list1.ResponseXml);

            var post = await client.SendIsapi("POST", "ISAPI/DisplayDev/VideoWall/1/plan", "<WallPlan><name>Plan Beta</name></WallPlan>", "application/xml");
            Assert.Equal(200, post.HttpStatus);
            Assert.Contains("<ID>2</ID>", post.ResponseXml);

            var put = await client.SendIsapi("PUT", "ISAPI/DisplayDev/VideoWall/1/plan/2", "<WallPlan><name>Plan Beta Updated</name></WallPlan>", "application/xml");
            Assert.Equal(200, put.HttpStatus);

            var start = await client.SendIsapi("PUT", "ISAPI/DisplayDev/VideoWall/1/plan/2/start", null, null);
            Assert.Equal(200, start.HttpStatus);

            var running = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/1/plan/isRunning", null, null);
            Assert.Equal(200, running.HttpStatus);
            Assert.Contains("<planID>2</planID>", running.ResponseXml);

            var stop = await client.SendIsapi("PUT", "ISAPI/DisplayDev/VideoWall/1/plan/2/stop", null, null);
            Assert.Equal(200, stop.HttpStatus);

            var stopped = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/1/plan/isRunning", null, null);
            Assert.Contains("<planID>0</planID>", stopped.ResponseXml);

            var del = await client.SendIsapi("DELETE", "ISAPI/DisplayDev/VideoWall/1/plan/2", null, null);
            Assert.Equal(200, del.HttpStatus);

            var list2 = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/1/plan", null, null);
            Assert.DoesNotContain("Plan Beta", list2.ResponseXml);
        }
    }

    [Fact]
    public void DirectMode_WpfSceneSetup_UniformTileSize_CoordinateGrid_Test()
    {
        Assert.Equal(1920, Module.VideoWall.WPF.Api.Direct.Isapi.VwDirectDeviceConstants.UniformTileSize);
        Assert.Equal(1920, Module.VideoWall.WPF.Api.Direct.Isapi.VwDirectDeviceConstants.BaseOutputSize);

        var col = 2;
        var row = 1;
        var expectedX = col * Module.VideoWall.WPF.Api.Direct.Isapi.VwDirectDeviceConstants.UniformTileSize;
        var expectedY = row * Module.VideoWall.WPF.Api.Direct.Isapi.VwDirectDeviceConstants.UniformTileSize;

        Assert.Equal(3840, expectedX);
        Assert.Equal(1920, expectedY);
    }

    [Fact]
    public async Task DirectMode_WpfScenario_ActivateScene_CallsCorrectEndpoint_Test()
    {
        var (client, mockServer) = BuildDirectClient(18140);
        using (mockServer)
        {
            var res = await client.SendIsapi("PUT", "ISAPI/DisplayDev/VideoWall/1/scene/3/activate", null, null);
            Assert.Equal(200, res.HttpStatus);
            Assert.Contains("OK", res.ResponseXml);
        }
    }

    [Fact]
    public async Task DirectMode_SendIsapi_ScreenCtrlCloseAll_Flags_Test()
    {
        var (client, mockServer) = BuildDirectClient(18142);
        using (mockServer)
        {
            // Default: throws invalidOperation (simulates unplugged RS232)
            var step1 = await client.SendIsapi("PUT", "ISAPI/DisplayDev/ScreenCtrl/closeAll", null, null);
            Assert.Equal(200, step1.HttpStatus);
            Assert.Contains("invalidOperation", step1.ResponseXml);

            // Flag enabled (--closeall-ok): returns OK
            mockServer.ScreenCtrlCloseAllThrowsInvalidOperation = false;
            var step2 = await client.SendIsapi("PUT", "ISAPI/DisplayDev/ScreenCtrl/closeAll", null, null);
            Assert.Equal(200, step2.HttpStatus);
            Assert.Contains("<statusCode>1</statusCode>", step2.ResponseXml);
            Assert.Contains("<statusString>OK</statusString>", step2.ResponseXml);
        }
    }

    [Fact]
    public void VwIsapiFormViewModel_TwoWayBinding_ExtractsParametersFromEndpointString_Test()
    {
        // 1. Single param: VideoWall ({videoWallID})
        var wallPreset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.5.4");
        var wallForm = new VwIsapiFormViewModel(wallPreset);
        var wallField = wallForm.PathFields.First(f => f.Definition.Key == "videoWallID");
        Assert.Equal("1", wallField.Value);

        // Edit endpoint path -> extracts param
        var updated = wallForm.TryExtractPathParameters("ISAPI/DisplayDev/VideoWall/2");
        Assert.True(updated);
        Assert.Equal("2", wallField.Value);
        Assert.Equal("ISAPI/DisplayDev/VideoWall/2", wallForm.BuildPath());

        // 2. Multi param: Window ({videoWallID} and {VWMWID})
        var winPreset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.11.5");
        var winForm = new VwIsapiFormViewModel(winPreset);
        var winWallField = winForm.PathFields.First(f => f.Definition.Key == "videoWallID");
        var winIdField = winForm.PathFields.First(f => f.Definition.Key == "VWMWID");

        // Edit endpoint path -> extracts both params
        var winUpdated = winForm.TryExtractPathParameters("ISAPI/DisplayDev/VideoWall/3/windows/33554433");
        Assert.True(winUpdated);
        Assert.Equal("3", winWallField.Value);
        Assert.Equal("33554433", winIdField.Value);
        Assert.Equal("ISAPI/DisplayDev/VideoWall/3/windows/33554433", winForm.BuildPath());

        // 3. Channel param: Input Picture ({channelID})
        var picPreset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.4.18");
        var picForm = new VwIsapiFormViewModel(picPreset);
        var chanField = picForm.PathFields.First(f => f.Definition.Key == "channelID");

        var picUpdated = picForm.TryExtractPathParameters("ISAPI/DisplayDev/Video/inputs/channels/16842754/picture");
        Assert.True(picUpdated);
        Assert.Equal("16842754", chanField.Value);
        Assert.Equal("ISAPI/DisplayDev/Video/inputs/channels/16842754/picture", picForm.BuildPath());
    }

    [Fact]
    public void ConnectionViewModel_TwoWayBinding_SyncsBetweenIsapiPathAndFormFields_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, Microsoft.Extensions.Logging.Abstractions.NullLogger<ActivityPublisher>.Instance);
        var connVm = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));

        // Select Window preset (9.7.11.5: ISAPI/DisplayDev/VideoWall/{videoWallID}/windows/{VWMWID})
        var preset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.11.5");
        connVm.SelectedIsapiPreset = preset;

        Assert.NotNull(connVm.ActiveIsapiForm);
        Assert.Equal("ISAPI/DisplayDev/VideoWall/1/windows/1", connVm.IsapiPath);

        var wallField = connVm.ActiveIsapiForm.PathFields.First(f => f.Definition.Key == "videoWallID");
        var winField = connVm.ActiveIsapiForm.PathFields.First(f => f.Definition.Key == "VWMWID");
        Assert.Equal("1", wallField.Value);
        Assert.Equal("1", winField.Value);

        // 1. Direction: Edit IsapiPath directly -> Form fields update automatically
        connVm.IsapiPath = "ISAPI/DisplayDev/VideoWall/2/windows/33554433";
        Assert.Equal("2", wallField.Value);
        Assert.Equal("33554433", winField.Value);

        // 2. Direction: Edit Form fields -> IsapiPath updates automatically
        wallField.Value = "4";
        Assert.Equal("ISAPI/DisplayDev/VideoWall/4/windows/33554433", connVm.IsapiPath);

        winField.Value = "999";
        Assert.Equal("ISAPI/DisplayDev/VideoWall/4/windows/999", connVm.IsapiPath);

        // 3. Change Header WallNo -> Form field and IsapiPath update automatically
        connVm.WallNo = 5;
        Assert.Equal("5", wallField.Value);
        Assert.Equal("ISAPI/DisplayDev/VideoWall/5/windows/999", connVm.IsapiPath);
    }

    [Fact]
    public async Task DirectMode_Probe_FetchesScenesFromDevice_Succeeds_Test()
    {
        const int port = 18131;
        var (client, mockServer) = BuildDirectClient(port);
        using (mockServer)
        {
            mockServer.GetSceneStore(1)[3] = "3_12345";

            var output = await client.Probe(1, default);

            Assert.True(output.Reachable);
            Assert.NotNull(output.Scenes);
            Assert.Equal(4, output.Scenes.Count);
            Assert.Contains(output.Scenes, s => s.Id == 3 && s.Name == "3_12345");
        }
    }

    [Fact]
    public async Task DirectMode_Probe_FetchesActiveWindowsFromDevice_Succeeds_Test()
    {
        const int port = 18132;
        var (client, mockServer) = BuildDirectClient(port);
        using (mockServer)
        {
            var output = await client.Probe(1, default);

            Assert.True(output.Reachable);
            Assert.NotNull(output.ActiveWindows);
            Assert.NotEmpty(output.ActiveWindows);
            Assert.Contains(output.ActiveWindows, w => w.Rect != null && w.Rect.Width > 0);
        }
    }

    [Fact]
    public async Task DirectMode_Probe_WhenDeviceRunsScene3_SyncsAndSwitchesCurrentSceneToScene3_Test()
    {
        const int port = 18133;
        var (client, mockServer) = BuildDirectClient(port);
        using (mockServer)
        {
            mockServer.GetSceneStore(1)[3] = "3_12345";
            mockServer.ActiveSceneId = 3;

            var recordingPub = new RecordingPublisherTest();
            var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
            var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
            {
                AdHocIp = "127.0.0.1",
                AdHocPort = port,
                AdHocAccount = "admin",
                AdHocPassword = "Password123!",
                WallNo = 1,
            };

            var sceneSetup = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub)
            {
                WallNo = 1,
            };

            if (sceneSetup.Scenes.Count > 1)
            {
                sceneSetup.CurrentScene = sceneSetup.Scenes[1];
            }

            await connection.ProbeCommand.ExecuteAsync(null);

            Assert.NotNull(sceneSetup.CurrentScene);
            Assert.Equal("3", sceneSetup.CurrentScene.OutputId);
            Assert.Equal("3_12345", sceneSetup.CurrentScene.Name);
            Assert.Equal(sceneSetup.CurrentScene.ID, sceneSetup.ActiveScene?.ID);
            Assert.NotEmpty(sceneSetup.SceneWindows);
        }
    }

    [Fact]
    public async Task DirectMode_CreateScene_AutoIncrementsOutputId_WhenNotSpecified_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));
        var sceneSetup = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub)
        {
            WallNo = 1,
        };

        sceneSetup.Scenes.Clear();
        sceneSetup.Scenes.Add(new VwSceneDto { ID = "s1", OutputId = "1", Name = "Kịch bản 1" });
        sceneSetup.Scenes.Add(new VwSceneDto { ID = "s2", OutputId = "2", Name = "Kịch bản 2" });
        sceneSetup.Scenes.Add(new VwSceneDto { ID = "s3", OutputId = "3", Name = "Kịch bản 3" });

        sceneSetup.SceneName = "Kịch bản mới";
        sceneSetup.SceneOutputId = string.Empty;

        await sceneSetup.CreateSceneCommand.ExecuteAsync(null);

        var created = sceneSetup.Scenes.FirstOrDefault(s => s.Name == "Kịch bản mới");
        Assert.NotNull(created);
        Assert.Equal("4", created.OutputId);
        Assert.Equal(created.ID, sceneSetup.CurrentScene?.ID);
    }

    [Fact]
    public async Task DirectMode_CreateScene_WhenDeviceReturnsInvalidOperation_CatchesAndSavesLocallyWithClearMessage_Test()
    {
        const int port = 18155;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.SimulateInvalidOperation = true;
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
        };

        var sceneSetup = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub)
        {
            WallNo = 1,
            SceneName = "Kịch bản test lỗi",
            SceneOutputId = "5",
        };

        await sceneSetup.CreateSceneCommand.ExecuteAsync(null);

        var created = sceneSetup.Scenes.FirstOrDefault(s => s.Name == "Kịch bản test lỗi");
        Assert.NotNull(created);
        Assert.Equal("5", created.OutputId);
        Assert.Contains("Invalid Operation", sceneSetup.StatusMessage);
        Assert.Contains("Đã lưu kịch bản vào bộ nhớ ứng dụng", sceneSetup.StatusMessage);
    }

    [Fact]
    public async Task DirectMode_SaveSceneData_SendsProperXmlPlaceholder_Succeeds_Test()
    {
        const int port = 18156;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var client = BuildIsapiClient(port);
        var res = await client.SaveSceneData(1, 1, default);

        Assert.True(res.Success, res.ErrorMessage);
        Assert.Contains("<Request", res.RawRequest);
        Assert.Contains("xmlns=\"http://www.isapi.org/ver20/XMLSchema\"", res.RawRequest);
    }

    [Fact]
    public void DirectMode_SyncScenesFromDevice_PrunesStaleLocalScenes_SuchAsTest7787_Test()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "vw_test_prune_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            const string deviceKey = "172.25.0.32_wall_1";

            var initialData = new VwLocalSceneData
            {
                Scenes =
                [
                    new VwSceneDto { ID = "s1", OutputId = "1", Name = "Kịch bản 1" },
                    new VwSceneDto { ID = "s2", OutputId = "2", Name = "Kịch bản 2" },
                    new VwSceneDto { ID = "s3", OutputId = "3", Name = "Kịch bản 3" },
                    new VwSceneDto { ID = "s4", OutputId = "4", Name = "test7787" }
                ],
                Windows =
                [
                    new VwWindowSceneDto { ID = "w1", SceneId = "s1", Name = "Win 1" },
                    new VwWindowSceneDto { ID = "w4", SceneId = "s4", Name = "Win 4 for test7787" }
                ],
                ActiveSceneId = "s4"
            };
            VwLocalSceneStore.SaveData(deviceKey, initialData, tempDir);

            var deviceScenes = new List<VwWallSceneSummary>
            {
                new() { Id = 1, Name = "Giám sát 1" },
                new() { Id = 2, Name = "Giám sát 2" },
                new() { Id = 3, Name = "Giám sát 3" }
            };

            var syncedScenes = VwLocalSceneStore.SyncScenesFromDevice(deviceKey, 1, deviceScenes, tempDir);

            Assert.Equal(3, syncedScenes.Count);
            Assert.DoesNotContain(syncedScenes, s => s.Name == "test7787" || s.OutputId == "4");
            Assert.Contains(syncedScenes, s => s.OutputId == "1" && s.Name == "Giám sát 1");
            Assert.Contains(syncedScenes, s => s.OutputId == "2" && s.Name == "Giám sát 2");
            Assert.Contains(syncedScenes, s => s.OutputId == "3" && s.Name == "Giám sát 3");

            var reloadedData = VwLocalSceneStore.LoadData(deviceKey, tempDir);
            Assert.DoesNotContain(reloadedData.Windows, w => w.SceneId == "s4");
            Assert.Contains(reloadedData.Windows, w => w.SceneId == "s1");
            Assert.Null(reloadedData.ActiveSceneId);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DirectMode_UpdateDeviceWindowRect_SendsCorrectXml_Test()
    {
        var port = 18135;
        var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);
        using (mockServer)
        {
            var isapiClient = BuildIsapiClient(port);
            var result = await isapiClient.UpdateDeviceWindowRect(1, 16777217, 1920, 0, 1920, 1920);

            Assert.True(result.Success);
            Assert.Equal(1, mockServer.UpdateWindowCallCount);
        }
    }

    [Fact]
    public async Task DirectMode_SetDeviceWindowTopAndBottom_SendsStatus_Test()
    {
        var port = 18136;
        var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);
        using (mockServer)
        {
            var isapiClient = BuildIsapiClient(port);
            var topRes = await isapiClient.SetDeviceWindowTop(1, 16777217);
            Assert.True(topRes.Success);

            var bottomRes = await isapiClient.SetDeviceWindowBottom(1, 16777217);
            Assert.True(bottomRes.Success);
        }
    }

    [Fact]
    public async Task DirectMode_Probe_AutoPopulatesCameraRtspUrl_FromPortInBoard_Test()
    {
        var port = 18137;
        var (client, mockServer) = BuildDirectClient(port);
        using (mockServer)
        {
            var probeResult = await client.Probe(1);
            Assert.True(probeResult.Reachable);
            Assert.NotEmpty(probeResult.InputChannels!);

            var ipChannel = probeResult.InputChannels!.FirstOrDefault(c => c.PortInBoard?.IpAddress != null);
            Assert.NotNull(ipChannel);
            Assert.Equal("127.0.0.1", ipChannel.PortInBoard?.IpAddress);
            Assert.Equal(13191, ipChannel.PortInBoard?.Port);

            var probeSources = probeResult.InputChannels!.Select(ch =>
            {
                var isIpPort = !string.IsNullOrWhiteSpace(ch.InputPortType)
                               && (ch.InputPortType.Contains("IP", StringComparison.OrdinalIgnoreCase)
                                   || ch.InputPortType.Contains("Stream", StringComparison.OrdinalIgnoreCase));
                var ip = ch.PortInBoard?.IpAddress?.Trim();
                var p = ch.PortInBoard?.Port > 0 ? ch.PortInBoard.Port : 554;
                var url = isIpPort && !string.IsNullOrWhiteSpace(ip)
                    ? $"rtsp://{ip}:{p}/Streaming/Channels/101"
                    : null;
                return new VwSourceDto
                {
                    ID = ch.Id.ToString(),
                    Code = isIpPort ? $"IP_{ch.Id}" : $"HDMI_{ch.Id}",
                    Name = ch.Name,
                    SourceType = isIpPort ? "ip_stream" : "local_signal",
                    SignalType = isIpPort ? (ch.InputPortType ?? "RTSP") : "HDMI",
                    SignalNo = ch.Id,
                    OrderNo = ch.Id,
                    Url = url,
                };
            }).ToList();

            var customSources = VwLocalSourceStore.GetSampleSources();
            var merged = VwLocalSourceStore.Merge(probeSources, customSources);

            Assert.DoesNotContain(merged, s => s.ID.StartsWith("sample_cam_"));
            var firstIpSource = merged.FirstOrDefault(s => s.SourceType == "ip_stream");
            Assert.NotNull(firstIpSource);
            Assert.Equal("rtsp://127.0.0.1:13191/Streaming/Channels/101", firstIpSource.Url);
        }
    }

    [Fact]
    public void DirectMode_SyncWindowsFromDevice_PreservesStreamUrl_Test()
    {
        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vw_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var devKey = "test_device_stream";
            var sources = new List<VwSourceDto>
            {
                new() { ID = "src_1", SignalNo = 1, Name = "HDMI 1", SourceType = "local_signal" },
                new() { ID = "src_ip", SignalNo = 16842753, Name = "Cam Km01", SourceType = "ip_stream", Url = "rtsp://172.25.0.32:554/Streaming/Channels/101" },
            };

            var devWindows = new List<Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPIWallWindowItem>
            {
                new()
                {
                    Id = 16777217,
                    Rect = new Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPIRect
                    {
                        Coordinate = new Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPICoordinate { X = 0, Y = 0 },
                        Width = 1920,
                        Height = 1920,
                    },
                    SubWindowList = new Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPISubWindowList
                    {
                        SubWindow =
                        [
                            new Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPISubWindow
                            {
                                Id = 1,
                                SubWindowParam = new Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPISubWindowParam
                                {
                                    SignalMode = "streamSetting",
                                    StreamInput = new Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPIStreamInput
                                    {
                                        StreamInputRealtime = new Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPIStreamInputRealtime
                                        {
                                            StreamRealtimeUnitList =
                                            [
                                                new Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPIStreamRealtimeUnit
                                                {
                                                    StreamInUrl = new Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPIStreamInUrl
                                                    {
                                                        Url = "rtsp://172.25.0.32:554/Streaming/Channels/101",
                                                    },
                                                },
                                            ],
                                        },
                                    },
                                },
                            },
                        ],
                    },
                },
            };

            var synced = VwLocalSceneStore.SyncWindowsFromDevice(devKey, "scene_test", devWindows, sources, tempDir);
            Assert.Single(synced);
            Assert.Equal("src_ip", synced[0].SourceId);
            Assert.Equal("src_ip", synced[0].SubWindows[0].SourceId);
        }
        finally
        {
            if (System.IO.Directory.Exists(tempDir))
                System.IO.Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DirectMode_DeleteWindow_StopsDynamicDecodeAndDeletesWindow_Test()
    {
        const int port = 18161;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var isApiClient = BuildIsapiClient(port);

        var result = await isApiClient.DeleteWindow(1, 33554433);

        Assert.True(result.Success);
        Assert.True(mockServer.StopDynamicDecodeCallCount >= 1);
        Assert.True(mockServer.DeleteWindowCallCount >= 1);
    }

    [Fact]
    public async Task DirectMode_DeleteAllWindows_CallsWallLevelBulkDelete_Test()
    {
        const int port = 18162;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var isApiClient = BuildIsapiClient(port);

        var result = await isApiClient.DeleteAllWindows(1, null, default);

        Assert.True(result.Success);
        Assert.True(mockServer.DeleteAllWindowsCallCount >= 1);
    }

    [Fact]
    public async Task DirectMode_PushLiveWindowUpdate_NewWindowWithoutId_AddsWindowAndDecodesStream_Test()
    {
        const int port = 18163;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
        };
        await connection.ConnectCommand.ExecuteAsync(null);

        var sceneSetup = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub)
        {
            DryRun = false,
        };

        var targetScene = new VwSceneDto
        {
            ID = "SCENE_LIVE_ADD",
            Name = "Scene Live Add Test",
            OutputId = "1",
        };
        sceneSetup.Scenes.Clear();
        sceneSetup.Scenes.Add(targetScene);
        sceneSetup.CurrentScene = targetScene;

        var ipSource = new VwSourceDto
        {
            ID = "cam_ip_test",
            Name = "Camera IP Live",
            SourceType = "ip_stream",
            Url = "rtsp://172.25.0.10:554/live/ch0",
        };
        sceneSetup.Sources.Add(ipSource);

        var newRow = new SceneWindowRow(new VwWindowSceneDto
        {
            ID = Guid.NewGuid().ToString("N"),
            Name = "Ô Test Mới",
            X = 0,
            Y = 0,
            W = 1920,
            H = 1920,
            ZIndex = 1,
            DeviceWindowId = null,
        })
        {
            SelectedSource = ipSource,
        };
        sceneSetup.SceneWindows.Add(newRow);

        // Act
        await sceneSetup.PushLiveWindowUpdate(newRow, default);

        // Assert
        Assert.True(mockServer.AddWindowCallCount >= 1);
        Assert.True(mockServer.StartDynamicDecodeCallCount >= 1);
        Assert.NotNull(newRow.Window.DeviceWindowId);
        Assert.Contains("Đã mở ô mới", sceneSetup.StatusMessage);
    }

    [Fact]
    public async Task DirectMode_DeleteSelectedSceneWindows_LiveDeletesOnDevice_Test()
    {
        const int port = 18164;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
        };
        await connection.ConnectCommand.ExecuteAsync(null);

        var sceneSetup = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub)
        {
            DryRun = false,
        };

        var targetScene = new VwSceneDto
        {
            ID = "SCENE_LIVE_DEL",
            Name = "Scene Live Del Test",
            OutputId = "1",
        };
        sceneSetup.Scenes.Clear();
        sceneSetup.Scenes.Add(targetScene);
        sceneSetup.CurrentScene = targetScene;

        var existingRow = new SceneWindowRow(new VwWindowSceneDto
        {
            ID = Guid.NewGuid().ToString("N"),
            Name = "Ô Cần Xoá",
            X = 0,
            Y = 0,
            W = 1920,
            H = 1920,
            ZIndex = 1,
            DeviceWindowId = "33554433",
        })
        {
            IsSelected = true,
        };
        sceneSetup.SceneWindows.Add(existingRow);

        // Act
        await sceneSetup.DeleteSelectedSceneWindowsCommand.ExecuteAsync(null);
        await Task.Delay(200);

        // Assert
        Assert.Empty(sceneSetup.SceneWindows);
        Assert.True(mockServer.DeleteWindowCallCount >= 1);
        Assert.True(mockServer.StopDynamicDecodeCallCount >= 1);
    }

    [Fact]
    public void SnapToGrid_And_SnapSize_UnconditionallyRoundToGridMultiples_Test()
    {
        Assert.Equal(0, VisualWallCanvas.SnapToGrid(0));
        Assert.Equal(0, VisualWallCanvas.SnapToGrid(500));
        Assert.Equal(1920, VisualWallCanvas.SnapToGrid(1200));
        Assert.Equal(1920, VisualWallCanvas.SnapToGrid(1800));
        Assert.Equal(1920, VisualWallCanvas.SnapToGrid(2100));
        Assert.Equal(3840, VisualWallCanvas.SnapToGrid(3500));

        Assert.Equal(1920, VisualWallCanvas.SnapSize(0));
        Assert.Equal(1920, VisualWallCanvas.SnapSize(1080));
        Assert.Equal(1920, VisualWallCanvas.SnapSize(1920));
        Assert.Equal(3840, VisualWallCanvas.SnapSize(3000));
        Assert.Equal(3840, VisualWallCanvas.SnapSize(3840));
    }

    [Fact]
    public void SceneSetupViewModel_SceneCountAndCapabilitySummary_ReflectsActualScenes_Test()
    {
        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPublisher, publisher, new UserConfirmationTest(true));
        var sceneSetup = new SceneSetupViewModel(activityPublisher, connection, new UserConfirmationTest(true));

        sceneSetup.Scenes.Clear();
        Assert.Equal(0, sceneSetup.SceneCount);
        Assert.Equal("0 Kịch bản", sceneSetup.SceneCountSummary);

        sceneSetup.Scenes.Add(new VwSceneDto { ID = "1", Name = "Kịch bản Sáng" });
        sceneSetup.Scenes.Add(new VwSceneDto { ID = "2", Name = "Kịch bản Tối" });

        Assert.Equal(2, sceneSetup.SceneCount);
        Assert.Equal("2 Kịch bản", sceneSetup.SceneCountSummary);

        sceneSetup.Scenes.RemoveAt(0);
        Assert.Equal(1, sceneSetup.SceneCount);
        Assert.Equal("1 Kịch bản", sceneSetup.SceneCountSummary);
    }

    [Fact]
    public void VwLocalSceneStore_PopulateSampleScenes_Wall1_Generates2x2LayoutMatchingDeviceLog_Test()
    {
        var data = new VwLocalSceneData();
        var sampleScenes = VwLocalSceneStore.PopulateSampleScenes(data, wallNo: 1);

        Assert.NotEmpty(sampleScenes);
        var scene1 = sampleScenes[0];
        Assert.Equal(2, scene1.GridCols);
        Assert.Equal(2, scene1.GridRows);

        var windows = data.Windows.Where(w => w.SceneId == scene1.ID).ToList();
        Assert.Equal(4, windows.Count);

        var win1 = windows.First(w => w.X == 0 && w.Y == 0);
        Assert.Equal(1920, win1.W);
        Assert.Equal(1920, win1.H);
        Assert.Equal(1, win1.WindowMode);

        var win4 = windows.First(w => w.X == 1920 && w.Y == 1920);
        Assert.Equal(1920, win4.W);
        Assert.Equal(1920, win4.H);
        Assert.Equal(4, win4.WindowMode);
        Assert.Equal(4, win4.SubWindows.Count);
    }

    [Fact]
    public void SceneSetupViewModel_UpdateStartScreenPresets_Generates2x2GridFallback_WhenProbeEmpty_Test()
    {
        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPublisher, publisher, new UserConfirmationTest(true));
        var sceneSetup = new SceneSetupViewModel(activityPublisher, connection, new UserConfirmationTest(true))
        {
            GridCols = 2,
            GridRows = 2,
        };

        sceneSetup.UpdateStartScreenPresets();

        Assert.Equal(4, sceneSetup.AvailableStartScreens.Count);
        Assert.Contains(sceneSetup.AvailableStartScreens, s => s.X == 0 && s.Y == 0);
        Assert.Contains(sceneSetup.AvailableStartScreens, s => s.X == 1920 && s.Y == 0);
        Assert.Contains(sceneSetup.AvailableStartScreens, s => s.X == 0 && s.Y == 1920);
        Assert.Contains(sceneSetup.AvailableStartScreens, s => s.X == 1920 && s.Y == 1920);
    }

    private static VwDirectISAPIClient BuildIsapiClient(int port)
    {
        var credentials = new VwDirectDeviceCredentials("127.0.0.1", port, "admin", "Password123!");
        var digestHandler = new VwDirectDigestHandler { InnerHandler = new HttpClientHandler() };
        var httpClient = new HttpClient(digestHandler)
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}"),
            Timeout = TimeSpan.FromSeconds(30),
        };
        return new VwDirectISAPIClient(httpClient, credentials);
    }

    private static (VwDirectDeviceConnectionClient Client, VwISAPIMockServerHikvision MockServer) BuildDirectClient(int port)
    {
        var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);
        var isApiClient = BuildIsapiClient(port);
        var publisher = new RecordingPublisherTest();
        return (new VwDirectDeviceConnectionClient(isApiClient, publisher), mockServer);
    }
}
