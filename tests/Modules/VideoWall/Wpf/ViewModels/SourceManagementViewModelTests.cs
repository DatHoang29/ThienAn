using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Module.VideoWall.WPF.Api.Direct;
using Module.VideoWall.WPF.Api.Dto;
using Module.VideoWall.WPF.Storage;
using Module.VideoWall.WPF.ViewModels;
using Tests.Modules.VideoWall.MockServer;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf.ViewModels;

public sealed class SourceManagementViewModelTests : IDisposable
{
    private readonly string _tempDirectory;

    public SourceManagementViewModelTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"SourceMgmtVmTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
            }
        }
    }

    [Fact]
    public void SourceManagementViewModel_Validation_RejectsInvalidInputs_Test()
    {
        const string deviceKey = "172.25.0.32";
        var vm = new SourceManagementViewModel(deviceKey, [], _tempDirectory);

        // Empty code
        vm.FormCode = "";
        vm.FormName = "Test Name";
        vm.FormUrl = "rtsp://10.0.0.1";
        vm.SaveSource();
        Assert.Equal("Vui lòng nhập Mã nguồn (Code).", vm.ErrorMessage);

        // Empty name
        vm.FormCode = "CAM_01";
        vm.FormName = "";
        vm.SaveSource();
        Assert.Equal("Vui lòng nhập Tên nguồn (Name).", vm.ErrorMessage);

        // Empty URL
        vm.FormName = "Camera 1";
        vm.FormUrl = "";
        vm.SaveSource();
        Assert.Equal("Vui lòng nhập địa chỉ URL luồng IP Stream.", vm.ErrorMessage);

        // URL > 256 characters
        vm.FormUrl = "rtsp://" + new string('a', 260);
        vm.SaveSource();
        Assert.Equal("Địa chỉ URL không được vượt quá 256 ký tự (chuẩn hệ thống).", vm.ErrorMessage);
    }

    [Fact]
    public void SourceManagementViewModel_SaveAndReset_WorksCorrectly_Test()
    {
        const string deviceKey = "172.25.0.32";
        var probe = new List<VwSourceDto>
        {
            new() { ID = "1", Code = "HDMI_1", Name = "Kênh 1", SignalNo = 1, SourceType = "local_signal" },
        };

        var vm = new SourceManagementViewModel(deviceKey, probe, _tempDirectory);

        vm.FormCode = "CAM_TEST";
        vm.FormName = "Camera Tuyến Chính";
        vm.FormSignalType = "RTSP";
        vm.FormUrl = "rtsp://admin:12345@10.10.8.31:554/ch1";
        vm.FormAspectRatio = "16:9";

        vm.SaveSource();

        Assert.Null(vm.ErrorMessage);
        Assert.NotNull(vm.StatusMessage);
        Assert.Contains("Camera Tuyến Chính", vm.StatusMessage);

        // Form should be reset after save
        Assert.Empty(vm.FormCode);
        Assert.Empty(vm.FormName);
        Assert.Empty(vm.FormUrl);

        // Should be in sources list
        var saved = vm.Sources.FirstOrDefault(s => s.Code == "CAM_TEST");
        Assert.NotNull(saved);
        Assert.Equal("ip_stream", saved.SourceType);
        Assert.Equal("[IP] Camera Tuyến Chính (RTSP)", saved.DisplayName);

        // Cannot delete probe HDMI
        var hdmiSource = vm.Sources.FirstOrDefault(s => s.ID == "1");
        Assert.NotNull(hdmiSource);
        vm.DeleteSource(hdmiSource);
        Assert.Equal("Không thể xoá kênh tín hiệu HDMI vật lý từ phần cứng thiết bị.", vm.ErrorMessage);

        // Can delete custom source
        vm.DeleteSource(saved);
        Assert.DoesNotContain(vm.Sources, s => s.Code == "CAM_TEST");
    }

    [Fact]
    public async Task VwDirectSetupSceneOrchestrator_IPStream_BuildsStreamSettingAndCallsDynamicDecode_Test()
    {
        const int port = 18215;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var creds = new VwDirectDeviceCredentials("127.0.0.1", port, "admin", "12345");
        var orchestrator = VwDirectClientFactory.CreateSetupSceneOrchestrator(creds);

        const string streamUrl = "rtsp://admin:pass@172.25.0.100:554/Streaming/Channels/101";

        var input = new VwDirectPushSceneInput
        {
            SceneId = 1,
            WallNo = 1,
            DryRun = false,
            Windows =
            [
                new VwDirectWindowInput
                {
                    X = 0,
                    Y = 0,
                    W = 1920,
                    H = 1080,
                    SignalNo = 1,
                    WindowMode = 1,
                    SourceType = "ip_stream",
                    StreamUrl = streamUrl,
                    StreamProtocol = "RTSP",
                }
            ],
        };

        var result = await orchestrator.Execute(input, CancellationToken.None);

        Assert.True(result.Success);

        var addStep = result.Steps.FirstOrDefault(s => s.Name.Contains("AddWindow"));
        Assert.NotNull(addStep);
        Assert.NotNull(addStep.RequestXml);
        Assert.Contains("<signalMode>stream setting</signalMode>", addStep.RequestXml);
        Assert.Contains("<StreamInURL>", addStep.RequestXml);
        Assert.Contains($"<URL>{streamUrl}</URL>", addStep.RequestXml);

        var decodeStep = result.Steps.FirstOrDefault(s => s.Name.Contains("StartDynamicDecode"));
        Assert.NotNull(decodeStep);
        Assert.True(decodeStep.Success);
        Assert.Contains("/start", decodeStep.Endpoint);
        Assert.True(
            string.IsNullOrEmpty(decodeStep.RequestXml) || !decodeStep.RequestXml.Contains("<StartDynamicDecode"),
            "Endpoint /start phải gửi body rỗng theo spec 9.7.2.5 (Request Message: None)");
    }
}