using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Api.Direct;
using Module.VideoWall.WPF.ViewModels;
using Services.Shared.Events;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf;

[Collection("api")]
public class VwWpfStandaloneModeTests(Host host)
{
    [Fact]
    public async Task DirectMode_PingProbeIsapi_WorksWithoutBackend()
    {
        host.MockServer.ResetDefaults();
        
        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(host.ApiClient), activityPublisher);
        var apiClient = new VideoWallApiClient(invoker, publisher, activityPublisher);
        var viewModel = new ConnectionViewModel(apiClient, activityPublisher, publisher, new UserConfirmationTest(true))
        {
            IsDirectMode = true,
            AdHocIp = "127.0.0.1",
            AdHocPort = 18080,
            AdHocAccount = "admin",
            AdHocPassword = "hik12345"
        };

        await viewModel.ConnectCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsConnected);
        Assert.Contains("127.0.0.1", viewModel.StatusMessage);

        await viewModel.ProbeCommand.ExecuteAsync(null);
        Assert.NotNull(viewModel.ProbeResult);
        Assert.True(viewModel.ProbeResult.Reachable);

        viewModel.IsapiMethod = "GET";
        viewModel.IsapiPath = "ISAPI/DisplayDev/VideoWall/capabilities";
        await viewModel.SendIsapiCommand.ExecuteAsync(null);
        Assert.NotNull(viewModel.IsapiResponse);
    }

    [Fact]
    public async Task DirectMode_PushWindows_CreatesWindowsOnDevice()
    {
        host.MockServer.ResetDefaults();

        var credentials = new VwDirectDeviceCredentials("127.0.0.1", 18080, "admin", "hik12345");
        var digestHandler = new VwDirectDigestHandler { InnerHandler = new HttpClientHandler() };
        var httpClient = new HttpClient(digestHandler)
        {
            BaseAddress = new Uri($"http://{credentials.Ip}:{credentials.Port}"),
            Timeout = TimeSpan.FromSeconds(30),
        };
        var isApiClient = new VwDirectISAPIClient(httpClient, credentials);
        var orchestrator = new VwDirectSetupSceneOrchestrator(isApiClient);

        var input = new VwDirectPushSceneInput
        {
            SceneId = 1,
            DryRun = false,
            Windows =
            [
                new VwDirectWindowInput(),
                new VwDirectWindowInput()
            ]
        };

        var result = await orchestrator.Execute(input, default);

        Assert.True(result.Success);
        Assert.True(host.MockServer.AddWindowCallCount >= 2);
    }

    [Fact]
    public async Task DirectMode_AddWindow_CreatesSingleWindowOnDevice()
    {
        host.MockServer.ResetDefaults();

        var credentials = new VwDirectDeviceCredentials("127.0.0.1", 18080, "admin", "Password123!");
        var digestHandler = new VwDirectDigestHandler { InnerHandler = new HttpClientHandler() };
        var httpClient = new HttpClient(digestHandler)
        {
            BaseAddress = new Uri($"http://{credentials.Ip}:{credentials.Port}"),
            Timeout = TimeSpan.FromSeconds(30),
        };
        var isApiClient = new VwDirectISAPIClient(httpClient, credentials);

        var req = new Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPIWindowRequest
        {
            Rect = new Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPIRect
            {
                Coordinate = new Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPICoordinate
                {
                    X = 0,
                    Y = 0
                },
                Width = 1920,
                Height = 1080
            },
            SubWindowList = new Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPISubWindowList
            {
                SubWindow =
                [
                    new Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPISubWindow
                    {
                        SubWindowParam = new Module.VideoWall.WPF.Api.Direct.Isapi.VwISAPISubWindowParam
                        {
                            VideoInputChannelId = "1"
                        }
                    }
                ]
            }
        };

        var result = await isApiClient.AddWindow(1, req, default);

        Assert.True(result.Success);
        Assert.True(host.MockServer.AddWindowCallCount >= 1);
    }
}
