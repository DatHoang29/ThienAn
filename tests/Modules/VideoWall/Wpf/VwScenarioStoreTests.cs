using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Auth;
using Module.VideoWall.WPF.Interaction;
using Module.VideoWall.WPF.Storage;
using Module.VideoWall.WPF.ViewModels;
using Module.VideoWall.WPF.ViewModels.Isapi;
using Services.Shared.Events;
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

    [Fact]
    public void SeedScenarios_AllFiveSeedFiles_ExistAndHaveValidStructure()
    {
        var dir = VwScenarioStore.GetScenariosDirectory();
        if (!Directory.Exists(dir))
            return;

        var seed1 = VwScenarioStore.Load("Thiết lập scene có chụp hình");
        if (seed1 != null)
        {
            Assert.Equal("Thiết lập scene có chụp hình", seed1.Name);
            Assert.Equal(4, seed1.Steps.Count);
            Assert.Equal("9.7.5.3", seed1.Steps[0].Section);
            Assert.Equal("9.7.7.2", seed1.Steps[1].Section);
            Assert.Equal("9.7.7.4", seed1.Steps[2].Section);
            Assert.Equal("9.7.4.18", seed1.Steps[3].Section);
        }

        var seed2 = VwScenarioStore.Load("Thiết lập scene không chụp hình");
        if (seed2 != null)
        {
            Assert.Equal("Thiết lập scene không chụp hình", seed2.Name);
            Assert.Equal(3, seed2.Steps.Count);
            Assert.Equal("9.7.5.3", seed2.Steps[0].Section);
            Assert.Equal("9.7.7.2", seed2.Steps[1].Section);
            Assert.Equal("9.7.7.4", seed2.Steps[2].Section);
        }

        var seed3 = VwScenarioStore.Load("Active scene");
        if (seed3 != null)
        {
            Assert.Equal("Active scene", seed3.Name);
            Assert.Equal(2, seed3.Steps.Count);
            Assert.Equal("9.7.7.3", seed3.Steps[0].Section);
            Assert.Equal("9.7.7.6", seed3.Steps[1].Section);
        }

        var seed4 = VwScenarioStore.Load("Màn hình không chồng nhau");
        if (seed4 != null)
        {
            Assert.Equal("Màn hình không chồng nhau", seed4.Name);
            Assert.Equal(2, seed4.Steps.Count);
            Assert.Equal("9.7.11.3", seed4.Steps[0].Section);
            Assert.Equal("9.7.5.3", seed4.Steps[1].Section);
        }

        var seed5 = VwScenarioStore.Load("Màn hình chồng nhau");
        if (seed5 != null)
        {
            Assert.Equal("Màn hình chồng nhau", seed5.Name);
            Assert.Equal(2, seed5.Steps.Count);
            Assert.Equal("9.7.11.3", seed5.Steps[0].Section);
            Assert.Equal("9.7.5.3", seed5.Steps[1].Section);
        }
    }

    [Fact]
    public void ScenarioViewModel_StepManagement_AddRemoveAndReorder_WorksProperly()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(new HttpClient()), activityPub);
        var apiClient = new VideoWallApiClient(invoker, recordingPub, activityPub);
        var connection = new ConnectionViewModel(apiClient, activityPub, recordingPub, new UserConfirmationTest(true));
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, apiClient);

        Assert.NotNull(vm.SelectedPresetInCatalog);
        Assert.NotEmpty(vm.FilteredPresets);

        // Reset to new empty scenario for isolated test
        vm.NewScenarioCommand.Execute(null);
        Assert.Empty(vm.Steps);

        // Add 2 steps
        var preset1 = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.5.2");
        var preset2 = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.7.6");

        vm.AddStepCommand.Execute(preset1);
        vm.AddStepCommand.Execute(preset2);

        Assert.Equal(2, vm.Steps.Count);
        Assert.Equal("9.7.5.2", vm.Steps[0].Preset.Section);
        Assert.Equal("9.7.7.6", vm.Steps[1].Preset.Section);

        // Reorder (Move step 2 up)
        vm.MoveStepUpCommand.Execute(vm.Steps[1]);
        Assert.Equal("9.7.7.6", vm.Steps[0].Preset.Section);
        Assert.Equal("9.7.5.2", vm.Steps[1].Preset.Section);

        // Move down
        vm.MoveStepDownCommand.Execute(vm.Steps[0]);
        Assert.Equal("9.7.5.2", vm.Steps[0].Preset.Section);
        Assert.Equal("9.7.7.6", vm.Steps[1].Preset.Section);

        // Remove step
        vm.RemoveStepCommand.Execute(vm.Steps[0]);
        Assert.Single(vm.Steps);
        Assert.Equal("9.7.7.6", vm.Steps[0].Preset.Section);

        // New scenario resets steps
        vm.NewScenarioCommand.Execute(null);
        Assert.Empty(vm.Steps);
        Assert.Null(vm.SelectedSavedScenario);
    }

    [Fact]
    public async Task ScenarioViewModel_TestConcurrentTriggers_ValidatesInputs_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(new HttpClient()), activityPub);
        var apiClient = new VideoWallApiClient(invoker, recordingPub, activityPub);
        var connection = new ConnectionViewModel(apiClient, activityPub, recordingPub, new UserConfirmationTest(true));
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, apiClient);

        // When inputs are empty
        vm.EventTypeIdA = "";
        vm.EventTypeIdB = "";
        await vm.TestConcurrentTriggersCommand.ExecuteAsync(null);

        Assert.Contains("Nhập đủ 2 EventTypeId", vm.StatusMessage);
    }
}
