using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Module.VideoWall.WPF.Api.Dto;
using Module.VideoWall.WPF.Storage;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf.Storage;

public sealed class VwLocalSourceStoreTests : IDisposable
{
    private readonly string _tempDirectory;

    public VwLocalSourceStoreTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"VwLocalSourceStoreTests_{Guid.NewGuid():N}");
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
    public void VwLocalSourceStore_SaveAndLoad_RoundTripsSuccessfully_Test()
    {
        const string deviceKey = "172.25.0.32";
        var sources = new List<VwSourceDto>
        {
            new()
            {
                ID = "ip-1",
                Code = "CAM_01",
                Name = "Camera Trạm Km10",
                SourceType = "ip_stream",
                SignalType = "RTSP",
                Url = "rtsp://172.25.0.100:554/live/ch1",
                Status = VwStatus.Enable,
                OrderNo = 10,
            },
            new()
            {
                ID = "ip-2",
                Code = "CAM_02",
                Name = "Camera Hầm Km25",
                SourceType = "ip_stream",
                SignalType = "ONVIF",
                Url = "rtsp://172.25.0.101:554/live/ch1",
                Status = VwStatus.Enable,
                OrderNo = 20,
            }
        };

        VwLocalSourceStore.Save(deviceKey, sources, _tempDirectory);

        Assert.True(VwLocalSourceStore.Exists(deviceKey, _tempDirectory));
        var loaded = VwLocalSourceStore.Load(deviceKey, _tempDirectory);
        Assert.Equal(2, loaded.Count);
        Assert.Equal("CAM_01", loaded[0].Code);
        Assert.Equal("rtsp://172.25.0.100:554/live/ch1", loaded[0].Url);
        Assert.Equal("RTSP", loaded[0].SignalType);
        Assert.Equal("[IP] Camera Trạm Km10 (RTSP)", loaded[0].DisplayName);
    }

    [Fact]
    public void VwLocalSourceStore_SaveSource_AppendsOrUpdates_Test()
    {
        const string deviceKey = "172.25.0.32";
        var initial = new VwSourceDto
        {
            ID = "src-1",
            Code = "CAM_01",
            Name = "Initial Camera",
            SourceType = "ip_stream",
            Url = "rtsp://10.0.0.1/1",
        };

        VwLocalSourceStore.SaveSource(deviceKey, initial, _tempDirectory);
        var loaded1 = VwLocalSourceStore.Load(deviceKey, _tempDirectory);
        Assert.Single(loaded1);
        Assert.Equal("Initial Camera", loaded1[0].Name);

        var updated = new VwSourceDto
        {
            ID = "src-1",
            Code = "CAM_01_NEW",
            Name = "Updated Camera",
            SourceType = "ip_stream",
            Url = "rtsp://10.0.0.1/1_hd",
        };
        VwLocalSourceStore.SaveSource(deviceKey, updated, _tempDirectory);

        var loaded2 = VwLocalSourceStore.Load(deviceKey, _tempDirectory);
        Assert.Single(loaded2);
        Assert.Equal("Updated Camera", loaded2[0].Name);
        Assert.Equal("CAM_01_NEW", loaded2[0].Code);

        var newSource = new VwSourceDto
        {
            ID = "src-2",
            Code = "CAM_02",
            Name = "Second Camera",
            SourceType = "ip_stream",
            Url = "rtsp://10.0.0.2/stream",
        };
        VwLocalSourceStore.SaveSource(deviceKey, newSource, _tempDirectory);

        var loaded3 = VwLocalSourceStore.Load(deviceKey, _tempDirectory);
        Assert.Equal(2, loaded3.Count);
    }

    [Fact]
    public void VwLocalSourceStore_DeleteSource_RemovesTarget_Test()
    {
        const string deviceKey = "172.25.0.32";
        var s1 = new VwSourceDto { ID = "del-1", Code = "CAM_DEL1", Name = "Cam 1", SourceType = "ip_stream", Url = "rtsp://del1" };
        var s2 = new VwSourceDto { ID = "del-2", Code = "CAM_DEL2", Name = "Cam 2", SourceType = "ip_stream", Url = "rtsp://del2" };

        VwLocalSourceStore.Save(deviceKey, [s1, s2], _tempDirectory);

        var removed = VwLocalSourceStore.DeleteSource(deviceKey, "del-1", _tempDirectory);
        Assert.True(removed);

        var remaining = VwLocalSourceStore.Load(deviceKey, _tempDirectory);
        Assert.Single(remaining);
        Assert.Equal("del-2", remaining[0].ID);

        var removedAgain = VwLocalSourceStore.DeleteSource(deviceKey, "del-1", _tempDirectory);
        Assert.False(removedAgain);
    }

    [Fact]
    public void VwLocalSourceStore_Merge_CombinesProbeAndCustom_Test()
    {
        var probeSources = new List<VwSourceDto>
        {
            new() { ID = "1", Code = "HDMI_1", Name = "Kênh 1", SignalNo = 1, SourceType = "local_signal", OrderNo = 1 },
            new() { ID = "2", Code = "HDMI_2", Name = "Kênh 2", SignalNo = 2, SourceType = "local_signal", OrderNo = 2 },
        };

        var customSources = new List<VwSourceDto>
        {
            new() { ID = "ip-cam-1", Code = "CAM_01", Name = "Camera Tuyến Chính", SourceType = "ip_stream", SignalType = "RTSP", Url = "rtsp://cam1", OrderNo = 10 },
            new() { ID = "ip-cam-2", Code = "CAM_02", Name = "Camera Nút Giao", SourceType = "ip_stream", SignalType = "ONVIF", Url = "rtsp://cam2", OrderNo = 20 },
        };

        var merged = VwLocalSourceStore.Merge(probeSources, customSources);

        Assert.Equal(4, merged.Count);
        Assert.Equal("1", merged[0].ID);
        Assert.Equal("[HDMI] Kênh 1 (kênh 1)", merged[0].DisplayName);
        Assert.Equal("[IP] Camera Tuyến Chính (RTSP)", merged[2].DisplayName);
    }

    [Fact]
    public void VwLocalSourceStore_SanitizeDeviceKey_ReplacesSpecialChars_Test()
    {
        Assert.Equal("default", VwLocalSourceStore.SanitizeDeviceKey(string.Empty));
        Assert.Equal("172.25.0.32_80", VwLocalSourceStore.SanitizeDeviceKey("172.25.0.32:80"));
        Assert.Equal("device_name", VwLocalSourceStore.SanitizeDeviceKey("device/name"));
    }

    [Fact]
    public void VwLocalSourceStore_NormalizeControllerKey_StripsWallSuffix_Test()
    {
        Assert.Equal("127.0.0.1", VwLocalSourceStore.NormalizeControllerKey(null));
        Assert.Equal("127.0.0.1", VwLocalSourceStore.NormalizeControllerKey(string.Empty));
        Assert.Equal("172.25.0.32", VwLocalSourceStore.NormalizeControllerKey("172.25.0.32"));
        Assert.Equal("172.25.0.32", VwLocalSourceStore.NormalizeControllerKey("172.25.0.32_wall_1"));
        Assert.Equal("172.25.0.32", VwLocalSourceStore.NormalizeControllerKey("172.25.0.32_wall_2"));
    }

    [Fact]
    public void VwLocalSourceStore_Load_SeedsSampleSourcesWhenEmpty_Test()
    {
        const string deviceKey = "192.168.1.99";
        var loaded = VwLocalSourceStore.Load(deviceKey, _tempDirectory);

        Assert.NotEmpty(loaded);
        Assert.All(loaded, s => Assert.Equal("ip_stream", s.SourceType));
        Assert.Contains(loaded, s => s.Code == "CAM_KM01");
        Assert.True(VwLocalSourceStore.Exists(deviceKey, _tempDirectory));
    }

    [Fact]
    public void VwLocalSourceStore_Load_FallsBackToWallFileAndMigrates_Test()
    {
        const string legacyKey = "172.25.0.32_wall_1";
        const string controllerKey = "172.25.0.32";

        var legacySources = new List<VwSourceDto>
        {
            new()
            {
                ID = "legacy-ip-1",
                Code = "CAM_LEGACY_01",
                Name = "Camera Nút Giao Phía Bắc",
                SourceType = "ip_stream",
                SignalType = "RTSP",
                Url = "rtsp://10.10.10.10/ch1",
                OrderNo = 5,
            }
        };

        var legacyPath = Path.Combine(_tempDirectory, $"{legacyKey}.json");
        var json = System.Text.Json.JsonSerializer.Serialize(legacySources, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(legacyPath, json);

        var loaded = VwLocalSourceStore.Load(controllerKey, _tempDirectory);
        Assert.Single(loaded);
        Assert.Equal("CAM_LEGACY_01", loaded[0].Code);
        Assert.Equal("[IP] Camera Nút Giao Phía Bắc (RTSP)", loaded[0].DisplayName);
    }
}