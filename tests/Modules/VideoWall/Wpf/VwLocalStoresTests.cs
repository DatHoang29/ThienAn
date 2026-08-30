using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Module.VideoWall.WPF.Api.Dto;
using Module.VideoWall.WPF.Storage;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf;

public sealed class VwLocalStoresTests : IDisposable
{
    private readonly string _tempDirectory;

    public VwLocalStoresTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"VwLocalStoresTests_{Guid.NewGuid():N}");
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
                // Ignored in test cleanup
            }
        }
    }

    [Fact]
    public void VwLocalScreenStore_SaveAndLoad_RoundTripsSuccessfully_Test()
    {
        // Arrange
        const string deviceKey = "192.168.1.100";
        var screens = new List<VwScreenDto>
        {
            new()
            {
                ID = "screen-1",
                Name = "Màn hình 1",
                OutPutPort = "1",
                GridCol = 0,
                GridRow = 0,
                WidthPx = "1920",
                HeightPx = "1080",
                ControllerId = deviceKey,
            },
            new()
            {
                ID = "screen-2",
                Name = "Màn hình 2",
                OutPutPort = "2",
                GridCol = 1,
                GridRow = 0,
                WidthPx = "1920",
                HeightPx = "1080",
                ControllerId = deviceKey,
            },
        };

        // Act
        VwLocalScreenStore.Save(deviceKey, screens, _tempDirectory);
        var loaded = VwLocalScreenStore.Load(deviceKey, _tempDirectory);

        // Assert
        Assert.Equal(2, loaded.Count);
        Assert.Equal("screen-1", loaded[0].ID);
        Assert.Equal("Màn hình 1", loaded[0].Name);
        Assert.Equal("1920", loaded[0].WidthPx);
        Assert.Equal("screen-2", loaded[1].ID);
        Assert.Equal("2", loaded[1].OutPutPort);
    }

    [Fact]
    public void VwLocalScreenStore_Load_NonExistentDevice_ReturnsEmptyList_Test()
    {
        // Act
        var loaded = VwLocalScreenStore.Load("unknown-device", _tempDirectory);

        // Assert
        Assert.Empty(loaded);
    }

    [Fact]
    public void VwLocalSceneStore_AddAndListScenes_GeneratesIdAndCode_Test()
    {
        // Arrange
        const string deviceKey = "10.0.0.50";
        var sceneInput = new VwSceneDto
        {
            Name = "Kịch bản Họp",
            OutputId = "1",
            GridCols = 2,
            GridRows = 2,
        };

        // Act
        var created = VwLocalSceneStore.AddScene(deviceKey, sceneInput, _tempDirectory);
        var allScenes = VwLocalSceneStore.ListScenes(deviceKey, _tempDirectory);

        // Assert
        Assert.NotNull(created.ID);
        Assert.NotNull(created.Code);
        Assert.Equal("Kịch bản Họp", created.Name);
        Assert.Single(allScenes);
        Assert.Equal(created.ID, allScenes[0].ID);
    }

    [Fact]
    public void VwLocalSceneStore_WindowScenes_AddUpdateListDelete_WorksCorrectly_Test()
    {
        // Arrange
        const string deviceKey = "10.0.0.50";
        var scene = VwLocalSceneStore.AddScene(deviceKey, new VwSceneDto
        {
            Name = "Scene A",
        }, _tempDirectory);

        var win1 = new VwWindowSceneDto
        {
            Name = "Cửa sổ 1",
            SceneId = scene.ID,
            SourceId = "src-1",
            X = 0,
            Y = 0,
            W = 1920,
            H = 1080,
            ZIndex = 1,
        };

        var win2 = new VwWindowSceneDto
        {
            Name = "Cửa sổ 2",
            SceneId = scene.ID,
            SourceId = "src-2",
            X = 1920,
            Y = 0,
            W = 1920,
            H = 1080,
            ZIndex = 2,
        };

        // Act 1: Add windows
        var addedWin1 = VwLocalSceneStore.AddWindowScene(deviceKey, win1, _tempDirectory);
        var addedWin2 = VwLocalSceneStore.AddWindowScene(deviceKey, win2, _tempDirectory);

        var windows = VwLocalSceneStore.ListWindowScenes(deviceKey, scene.ID!, _tempDirectory);

        // Assert 1
        Assert.Equal(2, windows.Count);
        Assert.NotNull(addedWin1.ID);
        Assert.NotNull(addedWin2.ID);

        // Act 2: Update window
        addedWin1.W = 1280;
        addedWin1.H = 720;
        VwLocalSceneStore.UpdateWindowScene(deviceKey, addedWin1, _tempDirectory);
        var reloadedWindows = VwLocalSceneStore.ListWindowScenes(deviceKey, scene.ID!, _tempDirectory);
        var reloadedWin1 = reloadedWindows.First(w => w.ID == addedWin1.ID);

        // Assert 2
        Assert.Equal(1280, reloadedWin1.W);
        Assert.Equal(720, reloadedWin1.H);

        // Act 3: Delete 1 window
        VwLocalSceneStore.DeleteWindowScene(deviceKey, addedWin2.ID!, _tempDirectory);
        var remainingWindows = VwLocalSceneStore.ListWindowScenes(deviceKey, scene.ID!, _tempDirectory);

        // Assert 3
        Assert.Single(remainingWindows);
        Assert.Equal(addedWin1.ID, remainingWindows[0].ID);
    }

    [Fact]
    public void VwLocalSceneStore_DeleteScene_CascadesDeleteWindowsAndActiveState_Test()
    {
        // Arrange
        const string deviceKey = "192.168.1.150";
        var scene1 = VwLocalSceneStore.AddScene(deviceKey, new VwSceneDto { Name = "Scene 1" }, _tempDirectory);
        var scene2 = VwLocalSceneStore.AddScene(deviceKey, new VwSceneDto { Name = "Scene 2" }, _tempDirectory);

        VwLocalSceneStore.AddWindowScene(deviceKey, new VwWindowSceneDto
        {
            Name = "Win 1",
            SceneId = scene1.ID,
            SourceId = "1",
        }, _tempDirectory);

        VwLocalSceneStore.AddWindowScene(deviceKey, new VwWindowSceneDto
        {
            Name = "Win 2",
            SceneId = scene2.ID,
            SourceId = "2",
        }, _tempDirectory);

        VwLocalSceneStore.SetActiveScene(deviceKey, scene1.ID!, _tempDirectory);
        Assert.Equal(scene1.ID, VwLocalSceneStore.GetActiveScene(deviceKey, _tempDirectory)?.ID);

        // Act: Delete scene 1
        VwLocalSceneStore.DeleteScene(deviceKey, scene1.ID!, _tempDirectory);

        // Assert
        var scenes = VwLocalSceneStore.ListScenes(deviceKey, _tempDirectory);
        Assert.Single(scenes);
        Assert.Equal(scene2.ID, scenes[0].ID);

        // Windows for scene 1 should be gone
        Assert.Empty(VwLocalSceneStore.ListWindowScenes(deviceKey, scene1.ID!, _tempDirectory));
        // Windows for scene 2 should remain
        Assert.Single(VwLocalSceneStore.ListWindowScenes(deviceKey, scene2.ID!, _tempDirectory));

        // Active scene was scene 1, so it should now be null
        Assert.Null(VwLocalSceneStore.GetActiveScene(deviceKey, _tempDirectory));
    }

    [Fact]
    public void VwLocalSceneStore_DifferentDeviceKeys_AreIsolated_Test()
    {
        // Arrange
        const string dev1 = "10.0.0.1";
        const string dev2 = "10.0.0.2";

        VwLocalSceneStore.AddScene(dev1, new VwSceneDto { Name = "Scene on Dev1" }, _tempDirectory);
        VwLocalSceneStore.AddScene(dev2, new VwSceneDto { Name = "Scene on Dev2" }, _tempDirectory);

        // Act & Assert
        var scenesDev1 = VwLocalSceneStore.ListScenes(dev1, _tempDirectory);
        var scenesDev2 = VwLocalSceneStore.ListScenes(dev2, _tempDirectory);

        Assert.Single(scenesDev1);
        Assert.Equal("Scene on Dev1", scenesDev1[0].Name);

        Assert.Single(scenesDev2);
        Assert.Equal("Scene on Dev2", scenesDev2[0].Name);
    }

    [Fact]
    public void VwLocalSceneStore_DeleteWindowScenes_RemovesMultipleWindowsBatch_Test()
    {
        // Arrange
        const string deviceKey = "10.0.0.99";
        var scene = VwLocalSceneStore.AddScene(deviceKey, new VwSceneDto { Name = "Scene Test" }, _tempDirectory);
        var win1 = VwLocalSceneStore.AddWindowScene(deviceKey, new VwWindowSceneDto { SceneId = scene.ID, Name = "Win 1" }, _tempDirectory);
        var win2 = VwLocalSceneStore.AddWindowScene(deviceKey, new VwWindowSceneDto { SceneId = scene.ID, Name = "Win 2" }, _tempDirectory);
        var win3 = VwLocalSceneStore.AddWindowScene(deviceKey, new VwWindowSceneDto { SceneId = scene.ID, Name = "Win 3" }, _tempDirectory);

        var before = VwLocalSceneStore.ListWindowScenes(deviceKey, scene.ID!, _tempDirectory);
        Assert.Equal(3, before.Count);

        // Act: Delete win1 and win3
        VwLocalSceneStore.DeleteWindowScenes(deviceKey, [win1.ID!, win3.ID!], _tempDirectory);

        // Assert: Only win2 remains
        var after = VwLocalSceneStore.ListWindowScenes(deviceKey, scene.ID!, _tempDirectory);
        Assert.Single(after);
        Assert.Equal(win2.ID, after[0].ID);
        Assert.Equal("Win 2", after[0].Name);
    }

    [Fact]
    public void VwLocalSceneStore_SeedSampleScenes_GeneratesThreeRealisticScenesWithWindows_Test()
    {
        // Arrange
        const string deviceKey = "10.10.8.200";
        var sources = new List<VwSourceDto>
        {
            new() { ID = "src-01", Name = "CAM-01" },
            new() { ID = "src-02", Name = "CAM-02" },
            new() { ID = "src-03", Name = "CAM-03" },
        };

        // Act
        var seeded = VwLocalSceneStore.SeedSampleScenes(deviceKey, _tempDirectory, sources);

        // Assert 1: Exactly 3 scenes generated
        Assert.Equal(3, seeded.Count);

        // Scene 1: Giờ cao điểm (16 Cam nút giao)
        var s1 = seeded.First(s => s.Code == "VWSCENE_SAMPLE_01");
        Assert.Equal("1", s1.OutputId);
        Assert.Equal(4, s1.GridCols);
        Assert.Equal(4, s1.GridRows);
        var s1Windows = VwLocalSceneStore.ListWindowScenes(deviceKey, s1.ID!, _tempDirectory);
        Assert.Equal(16, s1Windows.Count);
        Assert.All(s1Windows, w =>
        {
            Assert.Equal(480, w.W);
            Assert.Equal(270, w.H);
            Assert.Equal(1, w.ZIndex);
        });

        // Scene 2: Ban đêm (Bản đồ sự cố + 4 Trạm thu phí)
        var s2 = seeded.First(s => s.Code == "VWSCENE_SAMPLE_02");
        Assert.Equal("2", s2.OutputId);
        var s2Windows = VwLocalSceneStore.ListWindowScenes(deviceKey, s2.ID!, _tempDirectory);
        Assert.Equal(5, s2Windows.Count);
        var mapWindow = s2Windows.First(w => w.W == 1280 && w.H == 1080);
        Assert.Contains("Bản đồ", mapWindow.Name);
        var tollWindows = s2Windows.Where(w => w.W == 640 && w.H == 270).ToList();
        Assert.Equal(4, tollWindows.Count);

        // Scene 3: Khẩn cấp (Full màn hình tai nạn)
        var s3 = seeded.First(s => s.Code == "VWSCENE_SAMPLE_03");
        Assert.Equal("3", s3.OutputId);
        var s3Windows = VwLocalSceneStore.ListWindowScenes(deviceKey, s3.ID!, _tempDirectory);
        Assert.Single(s3Windows);
        Assert.Equal(1920, s3Windows[0].W);
        Assert.Equal(1080, s3Windows[0].H);

        // Act 2: Re-seeding overwrites cleanly without duplication
        var reseeded = VwLocalSceneStore.SeedSampleScenes(deviceKey, _tempDirectory, sources);
        var allScenes = VwLocalSceneStore.ListScenes(deviceKey, _tempDirectory);
        Assert.Equal(3, allScenes.Count);
    }
}
