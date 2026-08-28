using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Module.VideoWall.WPF.Storage;
using Module.VideoWall.WPF.ViewModels;
using Xunit;

namespace Tests.Modules.VideoWall.Wpf;

public class VwScenarioStoreTests : IDisposable
{
    private readonly string _tempDirectory;

    public VwScenarioStoreTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "VwScenarioStoreTests_" + Guid.NewGuid().ToString("N"));
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
                // Ignored
            }
        }
    }

    [Fact]
    public void SanitizeFileName_ReplacesInvalidCharacters()
    {
        Assert.Equal("test_scenario_1", VwScenarioStore.SanitizeFileName("test/scenario:1"));
        Assert.Equal("scenario", VwScenarioStore.SanitizeFileName(""));
        Assert.Equal("scenario", VwScenarioStore.SanitizeFileName("   "));
    }

    [Fact]
    public void SaveAndLoad_PersistsScenarioStepsAccurately()
    {
        var data = new VwScenarioData
        {
            Name = "Kịch bản kiểm thử cổng ra",
            Steps =
            [
                new VwScenarioStepData
                {
                    Section = "9.7.3.4",
                    PathFieldValues = new Dictionary<string, string> { { "outputChannelNo", "1" } },
                    Body = "<OutputChannel />"
                },
                new VwScenarioStepData
                {
                    Section = "9.7.5.3",
                    PathFieldValues = new Dictionary<string, string> { { "videoWallNo", "1" } },
                    Body = "<VideoWall><id>1</id></VideoWall>"
                }
            ]
        };

        VwScenarioStore.Save(data, _tempDirectory);

        Assert.True(VwScenarioStore.Exists(data.Name, _tempDirectory));

        var loaded = VwScenarioStore.Load(data.Name, _tempDirectory);
        Assert.NotNull(loaded);
        Assert.Equal("Kịch bản kiểm thử cổng ra", loaded.Name);
        Assert.Equal(2, loaded.Steps.Count);
        Assert.Equal("9.7.3.4", loaded.Steps[0].Section);
        Assert.Equal("1", loaded.Steps[0].PathFieldValues["outputChannelNo"]);
        Assert.Equal("<OutputChannel />", loaded.Steps[0].Body);
        Assert.Equal("9.7.5.3", loaded.Steps[1].Section);
    }

    [Fact]
    public void ListAll_ReturnsAllSavedScenarioNames()
    {
        var sc1 = new VwScenarioData { Name = "Scenario_A" };
        var sc2 = new VwScenarioData { Name = "Scenario_B" };

        VwScenarioStore.Save(sc1, _tempDirectory);
        VwScenarioStore.Save(sc2, _tempDirectory);

        var list = VwScenarioStore.ListAll(_tempDirectory);
        Assert.Contains("Scenario_A", list);
        Assert.Contains("Scenario_B", list);
    }

    [Fact]
    public void Delete_RemovesScenarioFile()
    {
        var data = new VwScenarioData { Name = "ToDelete" };
        VwScenarioStore.Save(data, _tempDirectory);
        Assert.True(VwScenarioStore.Exists("ToDelete", _tempDirectory));

        var deleted = VwScenarioStore.Delete("ToDelete", _tempDirectory);
        Assert.True(deleted);
        Assert.False(VwScenarioStore.Exists("ToDelete", _tempDirectory));
    }

    [Fact]
    public void StepViewModel_CorrectlyWrapsPresetAndGeneratesDisplayName()
    {
        var preset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.5.3");
        var stepVm = new VwScenarioStepViewModel(preset, "<custom_body />");

        Assert.Equal(preset, stepVm.Preset);
        Assert.Equal("<custom_body />", stepVm.RawBody);
        Assert.Contains("9.7.5.3", stepVm.DisplayName);
        Assert.True(stepVm.NeedsBody);
        Assert.True(stepVm.HasPathFields);
    }
}
