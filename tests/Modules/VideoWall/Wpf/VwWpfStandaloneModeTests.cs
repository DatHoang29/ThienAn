using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Api.Direct;
using Module.VideoWall.WPF.Storage;
using Module.VideoWall.WPF.ViewModels;
using Services.Shared.Events;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf;

[Collection("api")]
public class VwWpfStandaloneModeTests(Host host)
{
    [Fact]
    public void DirectMode_InitialStatusAndProbeSummary_AreEmpty()
    {
        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(host.ApiClient), activityPublisher);
        var apiClient = new VideoWallApiClient(invoker, publisher, activityPublisher);
        var viewModel = new ConnectionViewModel(apiClient, activityPublisher, publisher, new UserConfirmationTest(true));

        Assert.Equal(string.Empty, viewModel.StatusMessage);
        Assert.Equal(string.Empty, viewModel.ProbeSummary);
    }

    [Fact]
    public async Task DirectMode_Connect_DoesNotTriggerAutoCaptureSnapshot()
    {
        host.MockServer.ResetDefaults();

        var defaultPath = VwDeviceDefaultStore.GetFilePath("127.0.0.1", 1);
        if (File.Exists(defaultPath))
            File.Delete(defaultPath);

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
            AdHocPassword = "hik12345",
            WallNo = 1
        };

        await viewModel.ConnectCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsConnected);
        Assert.False(VwDeviceDefaultStore.Exists("127.0.0.1", 1));
    }

    [Fact]
    public void SceneSetupViewModel_OverlappingAndIndividualMode_TwoWaySwitching_Works()
    {
        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(host.ApiClient), activityPublisher);
        var apiClient = new VideoWallApiClient(invoker, publisher, activityPublisher);
        var connection = new ConnectionViewModel(apiClient, activityPublisher, publisher, new UserConfirmationTest(true));
        var viewModel = new SceneSetupViewModel(apiClient, activityPublisher, connection, new UserConfirmationTest(true));

        Assert.True(viewModel.IsIndividualMode);
        Assert.False(viewModel.IsOverlappingMode);

        viewModel.IsOverlappingMode = true;
        Assert.False(viewModel.IsIndividualMode);
        Assert.True(viewModel.IsOverlappingMode);

        viewModel.IsIndividualMode = true;
        Assert.True(viewModel.IsIndividualMode);
        Assert.False(viewModel.IsOverlappingMode);
    }

    [Fact]
    public void ScenarioViewModel_LoadScenarioByName_DoesNotPublishActivityLog()
    {
        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(host.ApiClient), activityPublisher);
        var apiClient = new VideoWallApiClient(invoker, publisher, activityPublisher);
        var connection = new ConnectionViewModel(apiClient, activityPublisher, publisher, new UserConfirmationTest(true));

        var testScenarioName = $"test_auto_load_{Guid.NewGuid():N}";
        var testData = new VwScenarioData
        {
            Name = testScenarioName,
            Steps =
            [
                new VwScenarioStepData { Section = "9.7.5.2", Body = "" },
                new VwScenarioStepData { Section = "9.7.1.1", Body = "" }
            ]
        };
        VwScenarioStore.Save(testData);

        try
        {
            var viewModel = new ScenarioViewModel(connection, activityPublisher, publisher, apiClient, new UserConfirmationTest(true));
            publisher.Clear();

            viewModel.LoadScenarioByName(testScenarioName);

            Assert.Equal(2, viewModel.Steps.Count);
            Assert.Contains(testScenarioName, viewModel.StatusMessage);
            Assert.Empty(publisher.ActivityRows);
        }
        finally
        {
            VwScenarioStore.Delete(testScenarioName);
        }
    }

    [Fact]
    public void ScenarioViewModel_ApiPresetFiltering_And_AddStep_Works()
    {
        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(host.ApiClient), activityPublisher);
        var apiClient = new VideoWallApiClient(invoker, publisher, activityPublisher);
        var connection = new ConnectionViewModel(apiClient, activityPublisher, publisher, new UserConfirmationTest(true));

        var viewModel = new ScenarioViewModel(connection, activityPublisher, publisher, apiClient, new UserConfirmationTest(true));

        Assert.Equal(116, viewModel.AllPresets.Count);

        viewModel.SelectedApiGroup = "Screen";
        Assert.NotEmpty(viewModel.FilteredPresets);
        Assert.All(viewModel.FilteredPresets, p => Assert.Equal("Screen", p.Group.ToString()));

        viewModel.SelectedApiGroup = "Tất cả";
        viewModel.ApiSearchText = "9.7.5.2";
        var preset = Assert.Single(viewModel.FilteredPresets);
        Assert.Equal("9.7.5.2", preset.Section);

        viewModel.Steps.Clear();
        viewModel.AddStepCommand.Execute(preset);
        var step = Assert.Single(viewModel.Steps);
        Assert.Equal("9.7.5.2", step.Preset.Section);
    }

    [Fact]
    public void ScenarioViewModel_MoveStep_And_RemoveStep_Works()
    {
        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(host.ApiClient), activityPublisher);
        var apiClient = new VideoWallApiClient(invoker, publisher, activityPublisher);
        var connection = new ConnectionViewModel(apiClient, activityPublisher, publisher, new UserConfirmationTest(true));

        var viewModel = new ScenarioViewModel(connection, activityPublisher, publisher, apiClient, new UserConfirmationTest(true));
        viewModel.Steps.Clear();

        var presetA = viewModel.AllPresets.First(p => p.Section == "9.7.5.2");
        var presetB = viewModel.AllPresets.First(p => p.Section == "9.7.1.1");

        viewModel.AddStepCommand.Execute(presetA);
        viewModel.AddStepCommand.Execute(presetB);

        Assert.Equal(2, viewModel.Steps.Count);
        Assert.Equal("9.7.5.2", viewModel.Steps[0].Preset.Section);
        Assert.Equal("9.7.1.1", viewModel.Steps[1].Preset.Section);

        viewModel.MoveStepUpCommand.Execute(viewModel.Steps[1]);
        Assert.Equal("9.7.1.1", viewModel.Steps[0].Preset.Section);
        Assert.Equal("9.7.5.2", viewModel.Steps[1].Preset.Section);

        viewModel.MoveStepDownCommand.Execute(viewModel.Steps[0]);
        Assert.Equal("9.7.5.2", viewModel.Steps[0].Preset.Section);
        Assert.Equal("9.7.1.1", viewModel.Steps[1].Preset.Section);

        viewModel.RemoveStepCommand.Execute(viewModel.Steps[0]);
        var remaining = Assert.Single(viewModel.Steps);
        Assert.Equal("9.7.1.1", remaining.Preset.Section);
    }

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
