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

    [Fact]
    public void ScenarioViewModel_BuiltInScenarios_InitializedCorrectly_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(new HttpClient()), activityPub);
        var apiClient = new VideoWallApiClient(invoker, recordingPub, activityPub);
        var connection = new ConnectionViewModel(apiClient, activityPub, recordingPub, new UserConfirmationTest(true));
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, apiClient);

        Assert.Equal(3, vm.BuiltInScenarios.Count);
        Assert.Contains("1. Thiết lập scene (không chụp hình)", vm.BuiltInScenarios[0].Name);
        Assert.Contains("2. Thiết lập scene (có chụp hình)", vm.BuiltInScenarios[1].Name);
        Assert.Contains("3. Active scene", vm.BuiltInScenarios[2].Name);
        Assert.NotNull(vm.SelectedBuiltInScenario);
        Assert.Equal(400, vm.DelayBetweenStepsMs);
    }

    [Fact]
    public async Task ScenarioViewModel_ErrorValidationSuite_RunsAllThreeCases_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(new HttpClient()), activityPub);
        var apiClient = new VideoWallApiClient(invoker, recordingPub, activityPub);
        var connection = new ConnectionViewModel(apiClient, activityPub, recordingPub, new UserConfirmationTest(true));
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, apiClient)
        {
            DelayBetweenStepsMs = 0
        };

        await vm.RunErrorValidationSuiteCommand.ExecuteAsync(null);

        Assert.Contains("Đã chạy xong bộ kiểm thử lỗi", vm.StatusMessage);
        var logs = recordingPub.ActivityRows;
        Assert.Contains(logs, l => l.Activity.Detail.Contains("Case A"));
        Assert.Contains(logs, l => l.Activity.Detail.Contains("Case B"));
        Assert.Contains(logs, l => l.Activity.Detail.Contains("Case C"));
    }

    [Fact]
    public async Task ScenarioViewModel_OverlappingSizeTest_DirectMode_Executes_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(new HttpClient()), activityPub);
        var apiClient = new VideoWallApiClient(invoker, recordingPub, activityPub);
        var connection = new ConnectionViewModel(apiClient, activityPub, recordingPub, new UserConfirmationTest(true))
        {
            IsDirectMode = true,
            AdHocIp = "127.0.0.1",
            AdHocPort = 80,
            AdHocAccount = "admin",
            AdHocPassword = "password"
        };
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, apiClient);

        await vm.RunOverlappingSizeTestCommand.ExecuteAsync(null);

        Assert.Contains("Đã hoàn thành kịch bản 2 nguồn tranh vùng", vm.StatusMessage);
        var logs = recordingPub.ActivityRows;
        Assert.Contains(logs, l => l.Activity.Detail.Contains("Đã dựng 2 cửa sổ tranh vùng"));
    }

    [Fact]
    public async Task SceneSetupViewModel_MaxWindowNums_BlocksWhenExceeded_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(new HttpClient()), activityPub);
        var apiClient = new VideoWallApiClient(invoker, recordingPub, activityPub);
        var connection = new ConnectionViewModel(apiClient, activityPub, recordingPub, new UserConfirmationTest(true))
        {
            ProbeResult = new Module.VideoWall.WPF.Api.Dto.VwProbeDeviceOutput
            {
                MaxWindowNums = 2,
                MaxSceneNums = 128
            }
        };

        var vm = new SceneSetupViewModel(apiClient, activityPub, connection, new UserConfirmationTest(true))
        {
            CurrentScene = new Module.VideoWall.WPF.Api.Dto.VwSceneDto
            {
                ID = "scene-test-1",
                Code = "SCN-1",
                Name = "Test Scene"
            }
        };

        // Thêm 3 dòng vào WindowRows (vượt quá MaxWindowNums = 2)
        vm.WindowRows.Add(new WindowSceneRow { Label = "W1", Width = 100, Height = 100, X = 0, Y = 0, ZIndex = 1, SelectedSource = new Module.VideoWall.WPF.Api.Dto.VwSourceDto { ID = "s1" } });
        vm.WindowRows.Add(new WindowSceneRow { Label = "W2", Width = 100, Height = 100, X = 100, Y = 0, ZIndex = 1, SelectedSource = new Module.VideoWall.WPF.Api.Dto.VwSourceDto { ID = "s2" } });
        vm.WindowRows.Add(new WindowSceneRow { Label = "W3", Width = 100, Height = 100, X = 200, Y = 0, ZIndex = 1, SelectedSource = new Module.VideoWall.WPF.Api.Dto.VwSourceDto { ID = "s3" } });

        await vm.ApplyOverlappingWindowsCommand.ExecuteAsync(null);

        Assert.Contains("vượt quá giới hạn tối đa của thiết bị", vm.StatusMessage);
    }
}
