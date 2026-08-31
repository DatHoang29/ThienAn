using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Api.Direct;
using Module.VideoWall.WPF.Interaction;
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
            // Probe không có WallNo -> đọc danh sách Walls, Outputs null
            var resultWithoutWall = await client.Probe(null);
            Assert.True(resultWithoutWall.Reachable);
            Assert.NotEmpty(resultWithoutWall.Walls!);
            Assert.Null(resultWithoutWall.Outputs);

            // Probe có WallNo=1 -> đọc Outputs của Wall 1
            var resultWithWall = await client.Probe(1);
            Assert.True(resultWithWall.Reachable);
            Assert.NotEmpty(resultWithWall.Walls!);
            Assert.NotEmpty(resultWithWall.Outputs!);
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

    [Fact]
    public async Task DirectMode_SendIsapi_DecodeStatus_ReturnsDecodeStatusXml()
    {
        var (client, mockServer) = BuildDirectClient(18131);
        using (mockServer)
        {
            var step = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/1/windows/status", null, null);

            Assert.NotNull(step);
            Assert.Equal(200, step.HttpStatus);
            Assert.NotNull(step.ResponseXml);
            Assert.Contains("AllSubWndDecodeStatus", step.ResponseXml);
            Assert.DoesNotContain("WallWindowList", step.ResponseXml);
        }
    }

    [Fact]
    public async Task DirectMode_SendIsapi_AudioOutputs_ReturnsAudioOutputChannelList()
    {
        var (client, mockServer) = BuildDirectClient(18132);
        using (mockServer)
        {
            var step = await client.SendIsapi("GET", "ISAPI/DisplayDev/Audio/outputs/channels", null, null);

            Assert.NotNull(step);
            Assert.Equal(200, step.HttpStatus);
            Assert.NotNull(step.ResponseXml);
            Assert.Contains("AudioOutputChannelList", step.ResponseXml);
            Assert.DoesNotContain("VideoOutputChannelList", step.ResponseXml);
        }
    }

    [Fact]
    public async Task DirectMode_SendIsapi_GetCapturedPicture_ReturnsJpegBinary()
    {
        var (client, mockServer) = BuildDirectClient(18133);
        using (mockServer)
        {
            var step = await client.SendIsapi("GET", "ISAPI/DisplayDev/Video/inputs/channels/16842753/picture", null, null);

            Assert.NotNull(step);
            Assert.Equal(200, step.HttpStatus);
            Assert.Equal(0, mockServer.GetInputChannelsCallCount);
        }
    }

    [Fact]
    public async Task DirectMode_SendIsapi_GetSpecificVideoWall_ReturnsVideoWallXml()
    {
        var (client, mockServer) = BuildDirectClient(18134);
        using (mockServer)
        {
            var step = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/1", null, null);

            Assert.NotNull(step);
            Assert.Equal(200, step.HttpStatus);
            Assert.NotNull(step.ResponseXml);
            Assert.Contains("<VideoWall", step.ResponseXml);
            Assert.DoesNotContain("<VideoWallList", step.ResponseXml);
        }
    }

    [Fact]
    public async Task DirectMode_SendIsapi_All116Presets_ReturnHttpStatus200_AndValidResponses()
    {
        var (client, mockServer) = BuildDirectClient(18135);
        using (mockServer)
        {
            Assert.Equal(116, VwIsapiPresetList.Presets.Count);

            foreach (var preset in VwIsapiPresetList.Presets)
            {
                var resolvedUrl = System.Text.RegularExpressions.Regex.Replace(preset.Url, @"\{[^}]+\}", "1");
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
