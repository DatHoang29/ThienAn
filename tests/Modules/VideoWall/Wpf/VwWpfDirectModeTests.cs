using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Module.VideoWall.WPF.Api.Direct;
using Module.VideoWall.WPF.ViewModels;
using Module.VideoWall.WPF.ViewModels.Isapi;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf;

[Collection("api")]
public class VwWpfDirectModeTests(Host host)
{
    [Fact]
    public async Task DirectMode_Ping_ReturnsSuccess()
    {
        host.MockServer.ResetDefaults();
        var client = BuildDirectClient(host);

        var result = await client.Ping();
        
        Assert.True(result.Success);
    }

    [Fact]
    public async Task DirectMode_Probe_ReadsTopology()
    {
        host.MockServer.ResetDefaults();
        var client = BuildDirectClient(host);

        var result = await client.Probe(null);
        
        Assert.True(result.Reachable);
        Assert.NotEmpty(result.Walls);
        Assert.NotEmpty(result.Outputs);
    }

    [Fact]
    public async Task DirectMode_SendIsapi_ReturnsXml()
    {
        host.MockServer.ResetDefaults();
        var client = BuildDirectClient(host);

        var result = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/capabilities", null, null);
        
        Assert.True(result.Success);
        Assert.Contains("VideoWallCap", result.ResponseXml);
    }

    [Fact]
    public async Task DirectMode_SendIsapi_ListWrapper_AudioOutputChannelList_HasWrapperInRequestXml()
    {
        host.MockServer.ResetDefaults();
        var preset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.3.2");
        var formVm = new VwIsapiFormViewModel(preset)
        {
            RawBody = "<AudioOutputChannelList xmlns=\"http://www.isapi.org/ver20/XMLSchema\"><AudioOutputChannel><id>1</id><enabled>true</enabled></AudioOutputChannel></AudioOutputChannelList>"
        };

        var body = formVm.RawBody;
        Assert.NotNull(body);

        var publisher = new RecordingPublisherTest();
        var isApiClient = BuildIsapiClient();
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
        host.MockServer.ResetDefaults();
        var preset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.2.10");
        var formVm = new VwIsapiFormViewModel(preset)
        {
            RawBody = "{ \"enabled\": true }"
        };

        var body = formVm.RawBody;
        Assert.NotNull(body);
        Assert.Contains("\"enabled\": true", body);

        var publisher = new RecordingPublisherTest();
        var isApiClient = BuildIsapiClient();
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
        host.MockServer.ResetDefaults();
        var isApiClient = BuildIsapiClient();
        var orchestrator = new VwDirectSetupSceneOrchestrator(isApiClient);

        var input = new VwDirectPushSceneInput
        {
            SceneId = 1,
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
        Assert.True(host.MockServer.AddWindowCallCount >= 2);
        Assert.True(host.MockServer.SaveSceneDataCallCount >= 1);
    }

    [Fact]
    public async Task DirectMode_PushScene_DryRun_SkipsDeviceWrites()
    {
        host.MockServer.ResetDefaults();
        var isApiClient = BuildIsapiClient();
        var orchestrator = new VwDirectSetupSceneOrchestrator(isApiClient);

        var input = new VwDirectPushSceneInput
        {
            SceneId = 1,
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
        Assert.Equal(0, host.MockServer.AddWindowCallCount);
    }

    /// <summary>
    /// Author: Đạt
    /// Description: Thiết bị từ chối Digest Auth khi sai thông tin đăng nhập, trả về kết quả thất bại mà không ném ngoại lệ.
    /// Created date: 26/08/2026
    /// </summary>
    [Fact]
    public async Task DirectMode_Ping_WithWrongDeviceCredentials_ReportsFailureWithoutThrowing()
    {
        host.MockServer.ResetDefaults();
        host.MockServer.VerifyDigestResponseHash = true;

        var credentials = new VwDirectDeviceCredentials("127.0.0.1", 18080, "admin", "SaiMatKhau!");
        var digestHandler = new VwDirectDigestHandler { InnerHandler = new HttpClientHandler() };
        var httpClient = new HttpClient(digestHandler)
        {
            BaseAddress = new Uri("http://127.0.0.1:18080"),
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
        host.MockServer.ResetDefaults();
        host.MockServer.SimulateNonceExpiry = true;

        var client = BuildDirectClient(host);

        var result = await client.Ping();

        Assert.True(result.Success);
        Assert.Equal(1, host.MockServer.NonceExpiryTriggerCount);
    }

    [Fact]
    public async Task DirectMode_SendIsapi_DecodeStart_DoesNotTriggerSwitchSource()
    {
        host.MockServer.ResetDefaults();
        var client = BuildDirectClient(host);

        var step = await client.SendIsapi("PUT", "ISAPI/DisplayDev/VideoWall/1/windows/33554433/sub/1/start", null, null);

        Assert.NotNull(step);
        Assert.Equal(200, step.HttpStatus);
        Assert.Equal(0, host.MockServer.SwitchSourceCallCount);
    }

    [Fact]
    public async Task DirectMode_SendIsapi_DecodeStatus_ReturnsDecodeStatusXml()
    {
        host.MockServer.ResetDefaults();
        var client = BuildDirectClient(host);

        var step = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/1/windows/status", null, null);

        Assert.NotNull(step);
        Assert.Equal(200, step.HttpStatus);
        Assert.NotNull(step.ResponseXml);
        Assert.Contains("AllSubWndDecodeStatus", step.ResponseXml);
        Assert.DoesNotContain("WallWindowList", step.ResponseXml);
    }

    [Fact]
    public async Task DirectMode_SendIsapi_AudioOutputs_ReturnsAudioOutputChannelList()
    {
        host.MockServer.ResetDefaults();
        var client = BuildDirectClient(host);

        var step = await client.SendIsapi("GET", "ISAPI/DisplayDev/Audio/outputs/channels", null, null);

        Assert.NotNull(step);
        Assert.Equal(200, step.HttpStatus);
        Assert.NotNull(step.ResponseXml);
        Assert.Contains("AudioOutputChannelList", step.ResponseXml);
        Assert.DoesNotContain("VideoOutputChannelList", step.ResponseXml);
    }

    [Fact]
    public async Task DirectMode_SendIsapi_GetCapturedPicture_ReturnsJpegBinary()
    {
        host.MockServer.ResetDefaults();
        var client = BuildDirectClient(host);

        var step = await client.SendIsapi("GET", "ISAPI/DisplayDev/Video/inputs/channels/16842753/picture", null, null);

        Assert.NotNull(step);
        Assert.Equal(200, step.HttpStatus);
        Assert.Equal(0, host.MockServer.GetInputChannelsCallCount);
    }

    [Fact]
    public async Task DirectMode_SendIsapi_GetSpecificVideoWall_ReturnsVideoWallXml()
    {
        host.MockServer.ResetDefaults();
        var client = BuildDirectClient(host);

        var step = await client.SendIsapi("GET", "ISAPI/DisplayDev/VideoWall/1", null, null);

        Assert.NotNull(step);
        Assert.Equal(200, step.HttpStatus);
        Assert.NotNull(step.ResponseXml);
        Assert.Contains("<VideoWall", step.ResponseXml);
        Assert.DoesNotContain("<VideoWallList", step.ResponseXml);
    }

    [Fact]
    public async Task DirectMode_SendIsapi_All116Presets_ReturnHttpStatus200_AndValidResponses()
    {
        host.MockServer.ResetDefaults();
        var client = BuildDirectClient(host);

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

    private static VwDirectISAPIClient BuildIsapiClient()
    {
        var credentials = new VwDirectDeviceCredentials("127.0.0.1", 18080, "admin", "Password123!");
        var digestHandler = new VwDirectDigestHandler { InnerHandler = new HttpClientHandler() };
        var httpClient = new HttpClient(digestHandler)
        {
            BaseAddress = new Uri("http://127.0.0.1:18080"),
            Timeout = TimeSpan.FromSeconds(30),
        };
        return new VwDirectISAPIClient(httpClient, credentials);
    }

    private static VwDirectDeviceConnectionClient BuildDirectClient(Host host)
    {
        var isApiClient = BuildIsapiClient();
        var publisher = new RecordingPublisherTest();
        return new VwDirectDeviceConnectionClient(isApiClient, publisher);
    }
}
