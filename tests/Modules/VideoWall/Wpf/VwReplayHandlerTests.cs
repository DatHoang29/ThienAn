using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Api.Direct;
using Module.VideoWall.WPF.Api.Direct.Replay;
using Module.VideoWall.WPF.Api.Dto;
using Module.VideoWall.WPF.Auth;
using Module.VideoWall.WPF.Interaction;
using Module.VideoWall.WPF.ViewModels;
using Services.Shared.Events;
using DirectIsapi = Module.VideoWall.WPF.Api.Direct.Isapi;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf;

[Collection("api")]
public class VwReplayHandlerTests
{
    private sealed class ThrowingHandlerTest : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new NotSupportedException($"Network call not allowed in Replay test: {request.Method} {request.RequestUri}");
    }

    private static string GetSampleTapePath()
    {
        var primaryPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "TA-ITS015-WEBAPI-V1.0", "src", "Modules", "VideoWall", "Module.VideoWall.WPF", "SampleData", "sample-tape.json");

        if (File.Exists(primaryPath))
            return Path.GetFullPath(primaryPath);

        var localPath = Path.Combine(AppContext.BaseDirectory, "SampleData", "sample-tape.json");
        if (File.Exists(localPath))
            return localPath;

        return primaryPath;
    }

    [Fact]
    public async Task ReplayMode_Probe_ReadsTopologyFromSampleTape()
    {
        var sampleTapePath = GetSampleTapePath();
        Assert.True(File.Exists(sampleTapePath), $"Sample tape must exist at {sampleTapePath}");

        var tape = VwTapeStore.Load(sampleTapePath);
        Assert.NotEmpty(tape.Entries);

        var credentials = new VwDirectDeviceCredentials("127.0.0.1", 80, "admin", "hik12345");
        var publisher = new RecordingPublisherTest();

        var replayHandler = new VwReplayHandler(VwDeviceIoMode.Replay, tape, null, publisher)
        {
            InnerHandler = new ThrowingHandlerTest()
        };

        var httpClient = new HttpClient(replayHandler)
        {
            BaseAddress = new Uri("http://127.0.0.1:80")
        };

        var isApiClient = new VwDirectISAPIClient(httpClient, credentials);
        var client = new VwDirectDeviceConnectionClient(isApiClient, publisher);

        var probeResult = await client.Probe(1, default);

        Assert.True(probeResult.Reachable);
        Assert.True(probeResult.IsSupportScene);
        Assert.Equal(128, probeResult.MaxSceneNums);
        Assert.Equal(16, probeResult.MaxWindowNums);
        Assert.Equal(1, probeResult.WallNo);
        Assert.NotNull(probeResult.Walls);
        Assert.Equal(8, probeResult.Walls.Count);
        Assert.NotNull(probeResult.Outputs);
        Assert.True(probeResult.Outputs.Count >= 12);
        Assert.NotNull(probeResult.InputChannels);
        Assert.NotEmpty(probeResult.InputChannels);
    }

    [Fact]
    public async Task ReplayMode_AddWindow_SucceedsViaTape()
    {
        var sampleTapePath = GetSampleTapePath();
        var tape = VwTapeStore.Load(sampleTapePath);

        var credentials = new VwDirectDeviceCredentials("127.0.0.1", 80, "admin", "hik12345");
        var publisher = new RecordingPublisherTest();

        var replayHandler = new VwReplayHandler(VwDeviceIoMode.Replay, tape, null, publisher)
        {
            InnerHandler = new ThrowingHandlerTest()
        };

        var httpClient = new HttpClient(replayHandler)
        {
            BaseAddress = new Uri("http://127.0.0.1:80")
        };

        var isApiClient = new VwDirectISAPIClient(httpClient, credentials);

        var windowRequest = new DirectIsapi.VwISAPIWindowRequest
        {
            Rect = new DirectIsapi.VwISAPIRect
            {
                Coordinate = new DirectIsapi.VwISAPICoordinate { X = 0, Y = 0 },
                Width = 1920,
                Height = 1080
            },
            SubWindowList = new DirectIsapi.VwISAPISubWindowList
            {
                SubWindow =
                [
                    new DirectIsapi.VwISAPISubWindow
                    {
                        SubWindowParam = new DirectIsapi.VwISAPISubWindowParam { VideoInputChannelId = "1" }
                    }
                ]
            }
        };

        var result = await isApiClient.AddWindow(1, windowRequest, default);

        Assert.True(result.Success);
        Assert.Equal(1, result.Data?.StatusCode);
    }

    [Fact]
    public async Task ReplayMode_MissingEntry_Returns404AndLogsWarning()
    {
        var customTape = new VwTape
        {
            DeviceKey = "TestMissing",
            Entries =
            [
                new VwTapeEntry
                {
                    Seq = 1,
                    Method = "GET",
                    Path = "ISAPI/Security/userCheck",
                    StatusCode = 200,
                    ResponseBody = "<userCheck xmlns=\"http://www.isapi.org/ver20/XMLSchema\"><statusValue>200</statusValue></userCheck>"
                }
            ]
        };

        var credentials = new VwDirectDeviceCredentials("127.0.0.1", 80, "admin", "hik12345");
        var publisher = new RecordingPublisherTest();

        var replayHandler = new VwReplayHandler(VwDeviceIoMode.Replay, customTape, null, publisher)
        {
            InnerHandler = new ThrowingHandlerTest()
        };

        var httpClient = new HttpClient(replayHandler)
        {
            BaseAddress = new Uri("http://127.0.0.1:80")
        };

        var isApiClient = new VwDirectISAPIClient(httpClient, credentials);

        var result = await isApiClient.GetWindows(1, default);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.NotFound, result.HttpStatusCode);
        Assert.NotEmpty(publisher.ActivityRows);
        Assert.Contains("Không tìm thấy bản ghi", publisher.ActivityRows[0].Activity.Detail);
    }

    [Fact]
    public async Task ReplayMode_Orchestrator_Guardrail_ExceedingMaxWindows_BlockedBeforeSend()
    {
        var sampleTapePath = GetSampleTapePath();
        var tape = VwTapeStore.Load(sampleTapePath);

        var credentials = new VwDirectDeviceCredentials("127.0.0.1", 80, "admin", "hik12345");
        var publisher = new RecordingPublisherTest();

        var replayHandler = new VwReplayHandler(VwDeviceIoMode.Replay, tape, null, publisher)
        {
            InnerHandler = new ThrowingHandlerTest()
        };

        var httpClient = new HttpClient(replayHandler)
        {
            BaseAddress = new Uri("http://127.0.0.1:80")
        };

        var isApiClient = new VwDirectISAPIClient(httpClient, credentials);
        var orchestrator = new VwDirectSetupSceneOrchestrator(isApiClient);

        var input = new VwDirectPushSceneInput
        {
            SceneId = 1,
            WallNo = 1,
            DryRun = false,
            Windows = Enumerable.Range(1, 20).Select(i => new VwDirectWindowInput
            {
                X = 0,
                Y = 0,
                W = 1920,
                H = 1080,
                SignalNo = 1
            }).ToList()
        };

        var result = await orchestrator.Execute(input, default);

        Assert.False(result.Success);
        Assert.Contains("16", result.Message);
    }

    [Fact]
    public void TapeStore_AppendAndLoad_WorksWithJsonAndExportLog()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_tape_{Guid.NewGuid():N}.jsonl");
        try
        {
            var entry1 = new VwTapeEntry
            {
                Seq = 1,
                Method = "GET",
                Path = "ISAPI/Security/userCheck",
                StatusCode = 200,
                ResponseBody = "<userCheck><statusValue>200</statusValue></userCheck>"
            };
            var entry2 = new VwTapeEntry
            {
                Seq = 2,
                Method = "GET",
                Path = "ISAPI/DisplayDev/VideoWall/capabilities",
                StatusCode = 200,
                ResponseBody = "<VideoWallCap><maxWindowNums>16</maxWindowNums></VideoWallCap>"
            };

            VwTapeStore.Append(tempFile, entry1);
            VwTapeStore.Append(tempFile, entry2);

            var loadedTape = VwTapeStore.Load(tempFile);
            Assert.Equal(2, loadedTape.Entries.Count);
            Assert.Equal("GET", loadedTape.Entries[0].Method);
            Assert.Equal("ISAPI/Security/userCheck", loadedTape.Entries[0].Path);

            var exportLogJson = "[{\"Method\":\"GET\",\"Endpoint\":\"ISAPI/Security/userCheck\",\"HttpStatus\":200,\"ResponseXml\":\"<ok/>\"}]";
            var fromExportLog = VwTape.FromExportLogJson(exportLogJson);
            Assert.Single(fromExportLog.Entries);
            Assert.Equal("GET", fromExportLog.Entries[0].Method);
            Assert.Equal("ISAPI/Security/userCheck", fromExportLog.Entries[0].Path);
            Assert.Equal("<ok/>", fromExportLog.Entries[0].ResponseBody);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    private sealed class DigestChallengeMockHandlerTest : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization == null)
            {
                var response401 = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                response401.Headers.WwwAuthenticate.Add(new System.Net.Http.Headers.AuthenticationHeaderValue("Digest", "realm=\"Hikvision\", nonce=\"abc123nonce\", qop=\"auth\""));
                response401.Content = new StringContent("<ResponseStatus><statusCode>4</statusCode><statusString>Unauthorized</statusString></ResponseStatus>", Encoding.UTF8, "application/xml");
                return Task.FromResult(response401);
            }

            var response200 = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<userCheck xmlns=\"http://www.isapi.org/ver20/XMLSchema\"><statusValue>200</statusValue></userCheck>", Encoding.UTF8, "application/xml")
            };
            return Task.FromResult(response200);
        }
    }

    [Fact]
    public async Task RecordMode_DigestAuth_OnlyRecordsAuthenticatedResponse_DoesNotRecord401Challenge()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_rec_{Guid.NewGuid():N}.jsonl");
        try
        {
            var publisher = new RecordingPublisherTest();
            var credentials = new VwDirectDeviceCredentials("127.0.0.1", 80, "admin", "hik12345");
            var mockHttp = new DigestChallengeMockHandlerTest();

            var digestHandler = new VwDirectDigestHandler
            {
                InnerHandler = mockHttp
            };
            var replayHandler = new VwReplayHandler(VwDeviceIoMode.Record, null, tempFile, publisher)
            {
                InnerHandler = digestHandler
            };

            var httpClient = new HttpClient(replayHandler)
            {
                BaseAddress = new Uri("http://127.0.0.1:80")
            };

            var isApiClient = new VwDirectISAPIClient(httpClient, credentials);
            var result = await isApiClient.UserCheck(default);

            Assert.True(result.Success);

            // Kiểm tra file tape: CHỈ CÓ 1 dòng 200 OK, KHÔNG dính dòng 401 challenge
            var tape = VwTapeStore.Load(tempFile);
            Assert.Single(tape.Entries);
            Assert.Equal(200, tape.Entries[0].StatusCode);
            Assert.Equal("GET", tape.Entries[0].Method);
            Assert.Equal("ISAPI/Security/userCheck", tape.Entries[0].Path);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void ReplayMode_LivePushBlocked_In_SceneSetup_And_Scenario()
    {
        var publisher = new RecordingPublisherTest();
        var actPublisher = new ActivityPublisher(publisher, NullLogger<ActivityPublisher>.Instance);
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(new HttpClient()), actPublisher);
        var apiClient = new VideoWallApiClient(invoker, publisher, actPublisher);
        var connection = new ConnectionViewModel(apiClient, actPublisher, publisher, new UserConfirmationTest(true));

        connection.DeviceIoMode = VwDeviceIoMode.Replay;
        Assert.True(connection.IsReplayMode);

        var sceneSetupVm = new SceneSetupViewModel(
            apiClient,
            actPublisher,
            connection,
            new UserConfirmationTest(true));

        var scene = new VwSceneDto { ID = "scene-1", Code = "SC01", Name = "Scene 1", ControllerId = "ctrl-1" };
        sceneSetupVm.Scenes.Add(scene);
        sceneSetupVm.CurrentScene = scene;

        // Khi DryRun = false (muốn đẩy thật) ở chế độ Replay -> CanPushToDevice phải = false
        sceneSetupVm.DryRun = false;
        Assert.False(sceneSetupVm.PushToDeviceCommand.CanExecute(null));

        // Khi DryRun = true -> được phép chạy thử
        sceneSetupVm.DryRun = true;
        Assert.True(sceneSetupVm.PushToDeviceCommand.CanExecute(null));

        var scenarioVm = new ScenarioViewModel(
            connection,
            actPublisher,
            publisher,
            apiClient,
            new UserConfirmationTest(true));

        scenarioVm.SelectedBuiltInScenario = scenarioVm.BuiltInScenarios[0];

        // Scenario: khi BuiltInDryRun = false ở Replay -> khoá
        scenarioVm.BuiltInDryRun = false;
        Assert.False(scenarioVm.RunBuiltInScenarioCommand.CanExecute(null));
        Assert.False(scenarioVm.RunOverlappingSizeTestCommand.CanExecute(null));

        // Khi BuiltInDryRun = true ở Replay -> cho phép
        scenarioVm.BuiltInDryRun = true;
        Assert.True(scenarioVm.RunBuiltInScenarioCommand.CanExecute(null));
        Assert.True(scenarioVm.RunOverlappingSizeTestCommand.CanExecute(null));
    }

    [Fact]
    public void TapeStore_Append_EmitsWarning_WhenWritingFails()
    {
        var publisher = new RecordingPublisherTest();
        var entry = new VwTapeEntry
        {
            Seq = 1,
            Method = "GET",
            Path = "ISAPI/Security/userCheck",
            StatusCode = 200
        };

        // Đường dẫn chứa ký tự không hợp lệ trên Windows
        var invalidPath = "Z:\\invalid|path\\test.jsonl";
        VwTapeStore.Append(invalidPath, entry, publisher);

        Assert.NotEmpty(publisher.ActivityRows);
        Assert.Contains("Lỗi ghi bản ghi tape", publisher.ActivityRows[0].Activity.Detail);
    }
}
