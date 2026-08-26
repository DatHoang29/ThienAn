using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Module.VideoWall.WPF.Api.Direct;
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
