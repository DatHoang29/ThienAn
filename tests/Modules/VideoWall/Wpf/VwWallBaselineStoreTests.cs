using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Auth;
using Module.VideoWall.WPF.Storage;
using Module.VideoWall.WPF.ViewModels;
using Services.Shared.Events;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf;

public class VwWallBaselineStoreTests : IDisposable
{
    private readonly string _tempDirectory;

    public VwWallBaselineStoreTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "VwWallBaselineStoreTests_" + Guid.NewGuid().ToString("N"));
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
        Assert.Equal("192.168.1.100_8080", VwWallBaselineStore.SanitizeKey("192.168.1.100:8080"));
        Assert.Equal("device_name", VwWallBaselineStore.SanitizeKey("device/name"));
        Assert.Equal("unknown", VwWallBaselineStore.SanitizeKey(""));
        Assert.Equal("unknown", VwWallBaselineStore.SanitizeKey("   "));
    }

    [Fact]
    public void BaselineExists_ReturnsFalseWhenNotSaved()
    {
        var exists = VwWallBaselineStore.BaselineExists("10.0.0.1", 1, _tempDirectory);
        Assert.False(exists);
    }

    [Fact]
    public void LoadBaseline_ReturnsNullWhenFileDoesNotExist()
    {
        var loaded = VwWallBaselineStore.LoadBaseline("10.0.0.1", 1, _tempDirectory);
        Assert.Null(loaded);
    }

    [Fact]
    public void SaveAndLoadBaseline_RoundTripsCorrectly()
    {
        var deviceKey = "192.168.1.200";
        var wallId = 1;

        var data = new VwWallBaselineData
        {
            DeviceKey = deviceKey,
            WallId = wallId,
            SavedAtUtc = DateTime.UtcNow,
            WallOutputListXml = "<WallOutputList><id>1</id></WallOutputList>",
            WallWindowListXml = "<WallWindowList><id>1</id></WallWindowList>",
            StandardFields = new Dictionary<string, string?>
            {
                ["videoWallName"] = "MainWall",
                ["sceneNum"] = "16",
            },
        };

        VwWallBaselineStore.SaveBaseline(deviceKey, wallId, data, _tempDirectory);

        Assert.True(VwWallBaselineStore.BaselineExists(deviceKey, wallId, _tempDirectory));

        var loaded = VwWallBaselineStore.LoadBaseline(deviceKey, wallId, _tempDirectory);
        Assert.NotNull(loaded);
        Assert.Equal(deviceKey, loaded.DeviceKey);
        Assert.Equal(wallId, loaded.WallId);
        Assert.Equal("<WallOutputList><id>1</id></WallOutputList>", loaded.WallOutputListXml);
        Assert.Equal("<WallWindowList><id>1</id></WallWindowList>", loaded.WallWindowListXml);
        Assert.Equal("MainWall", loaded.StandardFields["videoWallName"]);
        Assert.Equal("16", loaded.StandardFields["sceneNum"]);
    }

    [Fact]
    public void DeleteBaseline_RemovesFileSuccessfully()
    {
        var deviceKey = "192.168.1.201";
        var wallId = 2;

        var data = new VwWallBaselineData
        {
            DeviceKey = deviceKey,
            WallId = wallId,
        };

        VwWallBaselineStore.SaveBaseline(deviceKey, wallId, data, _tempDirectory);
        Assert.True(VwWallBaselineStore.BaselineExists(deviceKey, wallId, _tempDirectory));

        var deleted = VwWallBaselineStore.DeleteBaseline(deviceKey, wallId, _tempDirectory);
        Assert.True(deleted);
        Assert.False(VwWallBaselineStore.BaselineExists(deviceKey, wallId, _tempDirectory));
    }

    [Fact]
    public void ConnectionViewModel_SaveAndRestoreBaseline_IncludesAdvancedFields()
    {
        var publisher = new RecordingPublisherTest();
        var activityPublisher = new ActivityPublisher(publisher, Microsoft.Extensions.Logging.Abstractions.NullLogger<ActivityPublisher>.Instance);
        using var httpClient = new HttpClient();
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(httpClient), activityPublisher);
        var apiClient = new VideoWallApiClient(invoker, publisher, activityPublisher);

        var testIp = "192.168.99.123";
        var connectionVm = new ConnectionViewModel(apiClient, activityPublisher, publisher)
        {
            AdHocIp = testIp,
            SelectedIsapiPreset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.5.3")
        };

        Assert.NotNull(connectionVm.ActiveIsapiForm);

        var standardField = connectionVm.ActiveIsapiForm.StandardBodyFields.First(f => f.Definition.Key == "name");
        var advancedField = connectionVm.ActiveIsapiForm.AdvancedBodyFields.First(f => f.Definition.Key == "subWndWidth");

        standardField.Value = "MasterWall";
        advancedField.Value = "1280";

        try
        {
            connectionVm.SaveWallBaselineCommand.Execute(null);

            Assert.True(VwWallBaselineStore.BaselineExists(testIp, 1));

            // Sửa đổi giá trị khác trên UI
            standardField.Value = "ModifiedWall";
            advancedField.Value = "640";

            // Khôi phục
            connectionVm.RestoreWallBaselineCommand.Execute(null);

            // Kiểm tra các giá trị đã được trả lại đúng giá trị đã lưu
            Assert.Equal("MasterWall", standardField.Value);
            Assert.Equal("1280", advancedField.Value);
        }
        finally
        {
            VwWallBaselineStore.DeleteBaseline(testIp, 1);
        }
    }
}
