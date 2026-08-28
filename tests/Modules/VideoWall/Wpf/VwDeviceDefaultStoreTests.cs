using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Api.Direct;
using WpfDto = Module.VideoWall.WPF.Api.Dto;
using Module.VideoWall.WPF.Auth;
using Module.VideoWall.WPF.Storage;
using Module.VideoWall.WPF.ViewModels;
using Services.Shared.Events;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf;

public class VwDeviceDefaultStoreTests : IDisposable
{
    private readonly string _tempDirectory;

    public VwDeviceDefaultStoreTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "VwDeviceDefaultStoreTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, true);
            }
            catch
            {
                // Ignored in cleanup
            }
        }
    }

    [Fact]
    public void SanitizeKey_ReplacesInvalidCharactersAndColons()
    {
        Assert.Equal("192.168.1.100_8080", VwDeviceDefaultStore.SanitizeKey("192.168.1.100:8080"));
        Assert.Equal("device_name", VwDeviceDefaultStore.SanitizeKey("device/name"));
        Assert.Equal("unknown", VwDeviceDefaultStore.SanitizeKey(""));
        Assert.Equal("unknown", VwDeviceDefaultStore.SanitizeKey("   "));
    }

    [Fact]
    public void Exists_ReturnsFalseWhenNotSaved()
    {
        var exists = VwDeviceDefaultStore.Exists("10.0.0.1", 1, _tempDirectory);
        Assert.False(exists);
    }

    [Fact]
    public void Load_ReturnsNullWhenFileDoesNotExist()
    {
        var loaded = VwDeviceDefaultStore.Load("10.0.0.1", 1, _tempDirectory);
        Assert.Null(loaded);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsAllFieldsCorrectly()
    {
        var deviceKey = "192.168.1.200";
        var wallId = 1;

        var data = new VwDeviceDefaultData
        {
            DeviceKey = deviceKey,
            WallId = wallId,
            CapturedAtUtc = DateTime.UtcNow,
            VideoWallXml = "<VideoWall><id>1</id><name>Wall1</name></VideoWall>",
            Scenes = [new VwSceneDefaultEntry { Id = "1", Name = "Morning" }],
            TextLedXml = "<virtualLEDList><virtualLED><id>1</id></virtualLED></virtualLEDList>",
            WallpaperXml = "<baseMapList><baseMap><id>1</id></baseMap></baseMapList>",
            SignalSourceXml = "<VideoInputChannelList><VideoInputChannel><id>1</id></VideoInputChannel></VideoInputChannelList>",
            OutputChannelXml = "<VideoOutputChannelList><VideoOutputChannel><id>1</id></VideoOutputChannel></VideoOutputChannelList>",
            Decoding = new VwDecodingDefaultData
            {
                BoardStreamExportCfgJson = "{\"enabled\":true}",
                DefaultDecodeDelayParamsJson = "{\"delay\":50}",
                NPreMonitorAllWallsXml = "<nPreMonitorList><id>1</id></nPreMonitorList>"
            }
        };

        VwDeviceDefaultStore.Save(deviceKey, wallId, data, _tempDirectory);

        Assert.True(VwDeviceDefaultStore.Exists(deviceKey, wallId, _tempDirectory));

        var loaded = VwDeviceDefaultStore.Load(deviceKey, wallId, _tempDirectory);
        Assert.NotNull(loaded);
        Assert.Equal(deviceKey, loaded.DeviceKey);
        Assert.Equal(wallId, loaded.WallId);
        Assert.Equal("<VideoWall><id>1</id><name>Wall1</name></VideoWall>", loaded.VideoWallXml);
        Assert.Single(loaded.Scenes);
        Assert.Equal("1", loaded.Scenes[0].Id);
        Assert.Equal("Morning", loaded.Scenes[0].Name);
        Assert.NotNull(loaded.Decoding);
        Assert.Equal("{\"enabled\":true}", loaded.Decoding.BoardStreamExportCfgJson);
        Assert.Equal("{\"delay\":50}", loaded.Decoding.DefaultDecodeDelayParamsJson);
    }

    [Fact]
    public void Delete_RemovesFileSuccessfully()
    {
        var deviceKey = "192.168.1.201";
        var wallId = 2;

        var data = new VwDeviceDefaultData
        {
            DeviceKey = deviceKey,
            WallId = wallId,
        };

        VwDeviceDefaultStore.Save(deviceKey, wallId, data, _tempDirectory);
        Assert.True(VwDeviceDefaultStore.Exists(deviceKey, wallId, _tempDirectory));

        var deleted = VwDeviceDefaultStore.Delete(deviceKey, wallId, _tempDirectory);
        Assert.True(deleted);
        Assert.False(VwDeviceDefaultStore.Exists(deviceKey, wallId, _tempDirectory));
    }

    private sealed class MockDeviceConnectionClient : IVwDeviceConnectionClient
    {
        public List<(string Method, string Path, string? Body, string? ContentType)> RecordedCalls { get; } = [];

        public Task<WpfDto.VwSetupSceneStep> Ping(CancellationToken ct = default)
        {
            return Task.FromResult(new WpfDto.VwSetupSceneStep
            {
                Order = 1,
                Name = "Ping",
                Method = "GET",
                Endpoint = "System/deviceInfo",
                DurationMs = 10,
                Success = true
            });
        }

        public Task<WpfDto.VwProbeDeviceOutput> Probe(int? wallNo, CancellationToken ct = default)
        {
            return Task.FromResult(new WpfDto.VwProbeDeviceOutput { Reachable = true });
        }

        public Task<WpfDto.VwSetupSceneStep> SendIsapi(string method, string path, string? body, string? contentType, CancellationToken ct = default)
        {
            RecordedCalls.Add((method, path, body, contentType));

            string? responseXml = null;
            if (method == "GET")
            {
                if (path.Contains("DisplayDev/VideoWall/1/scene"))
                    responseXml = "<sceneList><Scene><id>1</id><name>DefaultScene</name></Scene></sceneList>";
                else if (path.Contains("DisplayDev/VideoWall/DecodeMgr/BoardStreamExportCfg"))
                    responseXml = "{\"enabled\":true}";
                else if (path.Contains("DisplayDev/VideoWall/DecodeMgr/DefaultDecodeDelayParams"))
                    responseXml = "{\"delay\":20}";
                else if (path.Contains("DisplayDev/VideoWall"))
                    responseXml = "<VideoWallList><VideoWall><id>1</id><name>Wall1</name></VideoWall></VideoWallList>";
                else
                    responseXml = "<mockResponse><id>1</id></mockResponse>";
            }

            return Task.FromResult(new WpfDto.VwSetupSceneStep
            {
                Order = 1,
                Name = path,
                Method = method,
                Endpoint = path,
                DurationMs = 10,
                Success = true,
                ResponseXml = responseXml
            });
        }
    }

    [Fact]
    public async Task CaptureOrchestrator_RunsAllGetCallsAndParsesData()
    {
        var mockClient = new MockDeviceConnectionClient();
        var orchestrator = new VwDeviceDefaultCaptureOrchestrator(mockClient);

        var (data, steps) = await orchestrator.Capture("192.168.1.50", 1);

        Assert.Equal("192.168.1.50", data.DeviceKey);
        Assert.Equal(1, data.WallId);
        Assert.NotNull(data.VideoWallXml);
        Assert.Contains("<name>Wall1</name>", data.VideoWallXml);
        Assert.Single(data.Scenes);
        Assert.Equal("DefaultScene", data.Scenes[0].Name);
        Assert.NotNull(data.Decoding);
        Assert.Equal("{\"enabled\":true}", data.Decoding.BoardStreamExportCfgJson);
        Assert.Equal("{\"delay\":20}", data.Decoding.DefaultDecodeDelayParamsJson);
        Assert.Equal(9, steps.Count);
    }

    [Fact]
    public async Task RestoreOrchestrator_SendsMatchingPutCalls()
    {
        var mockClient = new MockDeviceConnectionClient();
        var orchestrator = new VwDeviceDefaultRestoreOrchestrator(mockClient);

        var data = new VwDeviceDefaultData
        {
            DeviceKey = "192.168.1.50",
            WallId = 1,
            VideoWallXml = "<VideoWall><id>1</id></VideoWall>",
            Scenes = [new VwSceneDefaultEntry { Id = "1", Name = "MainScene" }],
            WallpaperXml = "<baseMapList/>",
            TextLedXml = "<virtualLEDList/>",
            SignalSourceXml = "<VideoInputChannelList/>",
            OutputChannelXml = "<VideoOutputChannelList/>",
            Decoding = new VwDecodingDefaultData
            {
                BoardStreamExportCfgJson = "{\"enabled\":false}",
                DefaultDecodeDelayParamsJson = "{\"delay\":40}",
                NPreMonitorAllWallsXml = "<nPreMonitorList/>"
            }
        };

        var (okCount, totalCount, steps) = await orchestrator.Restore(data);

        Assert.Equal(9, totalCount);
        Assert.Equal(9, okCount);
        Assert.Equal(9, steps.Count);

        // Verify JSON content types
        var boardCfgCall = mockClient.RecordedCalls.First(c => c.Path.Contains("BoardStreamExportCfg"));
        Assert.Equal("application/json", boardCfgCall.ContentType);
        Assert.Equal("{\"enabled\":false}", boardCfgCall.Body);

        var delayCall = mockClient.RecordedCalls.First(c => c.Path.Contains("DefaultDecodeDelayParams"));
        Assert.Equal("application/json", delayCall.ContentType);
    }
}
