using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Api.Direct;
using Module.VideoWall.WPF.Auth;
using Module.VideoWall.WPF.Interaction;
using Module.VideoWall.WPF.Storage;
using Module.VideoWall.WPF.ViewModels;
using Module.VideoWall.WPF.ViewModels.Isapi;
using Services.Shared.Events;
using Tests.Modules.VideoWall.MockServer;
using Xunit;
using VwSetupSceneStep = Module.VideoWall.WPF.Api.Dto.VwSetupSceneStep;

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
        var defaults = VwScenarioStore.GetDefaultScenarios();
        Assert.Equal(5, defaults.Count);

        var seed1 = VwScenarioStore.Load("1. Thiết lập scene (không chụp hình)");
        Assert.NotNull(seed1);
        Assert.Equal(4, seed1.Steps.Count);
        Assert.Equal("9.7.5.2", seed1.Steps[0].Section);
        Assert.Equal("9.7.5.3", seed1.Steps[1].Section);
        Assert.Equal("9.7.7.2", seed1.Steps[2].Section);
        Assert.Equal("9.7.7.4", seed1.Steps[3].Section);

        var seed2 = VwScenarioStore.Load("2. Thiết lập scene (có chụp hình)");
        Assert.NotNull(seed2);
        Assert.Equal(5, seed2.Steps.Count);
        Assert.Equal("9.7.5.2", seed2.Steps[0].Section);
        Assert.Equal("9.7.5.3", seed2.Steps[1].Section);
        Assert.Equal("9.7.7.2", seed2.Steps[2].Section);
        Assert.Equal("9.7.7.4", seed2.Steps[3].Section);
        Assert.Equal("9.7.4.18", seed2.Steps[4].Section);

        var seed3 = VwScenarioStore.Load("3. Active scene");
        Assert.NotNull(seed3);
        Assert.Equal(2, seed3.Steps.Count);
        Assert.Equal("9.7.7.3", seed3.Steps[0].Section);
        Assert.Equal("9.7.7.6", seed3.Steps[1].Section);

        var seed4 = VwScenarioStore.Load("4. Màn hình không chồng nhau");
        Assert.NotNull(seed4);
        Assert.Equal(2, seed4.Steps.Count);
        Assert.Equal("9.7.11.3", seed4.Steps[0].Section);
        Assert.Equal("9.7.5.3", seed4.Steps[1].Section);

        var seed5 = VwScenarioStore.Load("5. Màn hình chồng nhau");
        Assert.NotNull(seed5);
        Assert.Equal(2, seed5.Steps.Count);
        Assert.Equal("9.7.11.3", seed5.Steps[0].Section);
        Assert.Equal("9.7.5.3", seed5.Steps[1].Section);
    }

    [Fact]
    public void ScenarioViewModel_StepManagement_AddRemoveAndReorder_WorksProperly()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true));

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
        Assert.Null(vm.SelectedScenarioItem);
    }

    [Fact]
    public void ScenarioViewModel_UnifiedScenarioList_ContainsDefaultScenarios_Test()
    {
        // Arrange
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true));

        // Act & Assert 1: Mặc định có các kịch bản chuẩn
        Assert.True(vm.ScenarioList.Count >= 5);
        Assert.Contains(vm.ScenarioList, s => s.DisplayName.Contains("Thiết lập scene (không chụp hình)"));
        Assert.Contains(vm.ScenarioList, s => s.DisplayName.Contains("Thiết lập scene (có chụp hình)"));
        Assert.Contains(vm.ScenarioList, s => s.DisplayName.Contains("Active scene"));

        // Act 2: Thêm bước và lưu kịch bản mới
        var testScenarioName = $"UnitTest_Unified_{Guid.NewGuid():N}";
        vm.ScenarioName = testScenarioName;
        vm.AddStepCommand.Execute(VwIsapiPresetList.Presets.First());
        vm.SaveScenarioCommand.Execute(null);

        // Assert 2: Danh sách có thêm mục saved
        var savedItem = vm.ScenarioList.FirstOrDefault(s => s.SavedFileName == testScenarioName);
        Assert.NotNull(savedItem);
        Assert.Equal(testScenarioName, savedItem.DisplayName);

        // Dọn dẹp
        vm.SelectedScenarioItem = savedItem;
        vm.DeleteScenarioCommand.Execute(null);
    }

    [Fact]
    public void ScenarioViewModel_WhenSavedScenarioSelected_LoadsStepsAndEnablesDelete_Test()
    {
        // Arrange
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true));

        var testName = $"UnitTest_SavedLoad_{Guid.NewGuid():N}";
        vm.NewScenarioCommand.Execute(null);
        vm.ScenarioName = testName;
        var preset = VwIsapiPresetList.Presets.First(p => p.Section == "9.7.5.2");
        vm.AddStepCommand.Execute(preset);
        vm.SaveScenarioCommand.Execute(null);

        var savedItem = vm.ScenarioList.First(s => s.SavedFileName == testName);
        vm.SelectedScenarioItem = savedItem;

        // Assert
        Assert.Single(vm.Steps);
        Assert.Equal("9.7.5.2", vm.Steps[0].Preset.Section);
        Assert.True(vm.DeleteScenarioCommand.CanExecute(null));

        // Dọn dẹp
        vm.DeleteScenarioCommand.Execute(null);
    }

    [Fact]
    public void ScenarioViewModel_RunSelectedScenarioCommand_CanExecuteReflectsItemType_Test()
    {
        // Arrange
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true));

        // Case 1: Kịch bản trống chưa có bước nào -> Không thể chạy
        vm.NewScenarioCommand.Execute(null);
        Assert.False(vm.RunSelectedScenarioCommand.CanExecute(null));

        // Case 2: Thêm bước vào kịch bản -> Bật lại nút chạy
        vm.AddStepCommand.Execute(VwIsapiPresetList.Presets.First());
        Assert.True(vm.RunSelectedScenarioCommand.CanExecute(null));
    }

    [Fact]
    public void ScenarioViewModel_DefaultScenarios_InitializedCorrectly_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true));

        Assert.True(vm.ScenarioList.Count >= 5);
        Assert.NotNull(vm.SelectedScenarioItem);
        Assert.Equal(400, vm.DelayBetweenStepsMs);
    }

    [Fact]
    public void ScenarioViewModel_DirectModeCommands_DisabledWhenAdHocIpEmpty_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "",
        };
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true));

        Assert.False(vm.RunOverlappingSizeTestCommand.CanExecute(null));
        Assert.False(vm.RunErrorValidationSuiteCommand.CanExecute(null));

        connection.AdHocIp = "127.0.0.1";
        Assert.True(vm.RunOverlappingSizeTestCommand.CanExecute(null));
        Assert.True(vm.RunErrorValidationSuiteCommand.CanExecute(null));
    }

    [Fact]
    public async Task ScenarioViewModel_OverlappingSizeTest_DirectMode_ExecutesOrchestrator_Test()
    {
        const int port = 18090;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
        };
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true))
        {
            OverlapSceneId = 1,
            BuiltInDryRun = true,
        };

        await vm.RunOverlappingSizeTestCommand.ExecuteAsync(null);

        Assert.Contains("Chạy thử thành công: 2 cửa sổ tranh vùng", vm.StatusMessage);
        var logs = recordingPub.ActivityRows;
        Assert.Contains(logs, l => l.Activity.Detail.Contains("Đã dựng 2 cửa sổ tranh vùng (SID 1)"));
    }

    [Fact]
    public async Task ScenarioViewModel_ErrorValidationSuite_DirectMode_RunsAllThreeCasesWithMockServer_Test()
    {
        const int port = 18091;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
        };
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true))
        {
            DelayBetweenStepsMs = 0,
        };

        await vm.RunErrorValidationSuiteCommand.ExecuteAsync(null);

        Assert.Contains("Đã chạy xong bộ kiểm thử lỗi (3/3 case, kết quả thật từ thiết bị)", vm.StatusMessage);
        var logs = recordingPub.ActivityRows;
        Assert.Contains(logs, l => l.Activity.Detail.Contains("[PASS] Case A (999 cửa sổ vượt maxWindowNums): thiết bị trả TỪ CHỐI"));
        Assert.Contains(logs, l => l.Activity.Detail.Contains("[PASS] Case B (1 nguồn gán 3 cửa sổ): thiết bị trả CHẤP NHẬN"));
        Assert.Contains(logs, l => l.Activity.Detail.Contains("[PASS] Case C (SceneId 99999 ngoài dải maxSceneNums): thiết bị trả TỪ CHỐI"));
    }

    [Fact]
    public async Task SceneSetupViewModel_MaxWindowNums_BlocksWhenExceeded_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            ProbeResult = new Module.VideoWall.WPF.Api.Dto.VwProbeDeviceOutput
            {
                MaxWindowNums = 2,
                MaxSceneNums = 128
            }
        };

        var vm = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true))
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

    [Fact]
    public async Task VwDeviceStepLogging_PublishSteps_PublishesDeviceStepNotifications_Test()
    {
        // Arrange
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var steps = new List<Module.VideoWall.WPF.Api.Dto.VwSetupSceneStep>
        {
            new()
            {
                Name = "Step1",
                Method = "GET",
                Endpoint = "ISAPI/test",
                DurationMs = 15,
                Success = true,
            },
            new()
            {
                Name = "Step2",
                Method = "POST",
                Endpoint = "ISAPI/test2",
                DurationMs = 25,
                Success = false,
                Message = "Bad Request",
            },
        };

        // Act
        await Module.VideoWall.WPF.Api.VwDeviceStepLogging.PublishSteps(
            recordingPub,
            activityPub,
            "UnitTestStage",
            steps,
            CancellationToken.None);

        // Assert
        Assert.Equal(2, recordingPub.DeviceStepRows.Count);
        Assert.Equal("Device", recordingPub.DeviceStepRows[0].Activity.Stage);
        Assert.Contains("UnitTestStage", recordingPub.DeviceStepRows[0].Activity.Detail);
        Assert.True(recordingPub.DeviceStepRows[0].Step.Success);
        Assert.False(recordingPub.DeviceStepRows[1].Step.Success);
    }

    [Fact]
    public async Task SceneSetupViewModel_PushToDevice_DirectMode_PublishesSteps_Test()
    {
        // Arrange
        const int port = 18092;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
        };

        var vm = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub)
        {
            CurrentScene = new Module.VideoWall.WPF.Api.Dto.VwSceneDto
            {
                ID = "scene-1",
                Code = "SCN-1",
                Name = "Scene 1",
                ControllerId = "127.0.0.1",
                OutputId = "1",
            },
            DryRun = true,
        };

        vm.SceneWindows.Add(new Module.VideoWall.WPF.Api.Dto.VwWindowSceneDto
        {
            ID = "win-1",
            Name = "Window 1",
            X = 0,
            Y = 0,
            W = 1920,
            H = 1080,
            ZIndex = 1,
            SourceId = "1",
        });

        // Act
        await vm.PushToDeviceCommand.ExecuteAsync(null);

        // Assert
        Assert.NotEmpty(recordingPub.DeviceStepRows);
        Assert.All(recordingPub.DeviceStepRows, row => Assert.StartsWith("PushToDevice", row.Activity.Detail));
    }

    [Fact]
    public async Task ScenarioViewModel_OverlappingSizeTest_And_ErrorSuite_PublishDeviceStepNotifications_Test()
    {
        // Arrange
        const int port = 18093;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
        };
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true))
        {
            OverlapSceneId = 1,
            BuiltInDryRun = true,
            DelayBetweenStepsMs = 0,
        };

        // Act 1: Overlap Test
        await vm.RunOverlappingSizeTestCommand.ExecuteAsync(null);

        // Assert 1
        Assert.NotEmpty(recordingPub.DeviceStepRows);
        Assert.Contains(recordingPub.DeviceStepRows, r => r.Activity.Detail.StartsWith("OverlapTest"));

        // Act 2: Error Suite
        recordingPub.Clear();
        await vm.RunErrorValidationSuiteCommand.ExecuteAsync(null);

        // Assert 2
        Assert.Contains(recordingPub.DeviceStepRows, r => r.Activity.Detail.StartsWith("ErrorSuite-CaseA"));
        Assert.Contains(recordingPub.DeviceStepRows, r => r.Activity.Detail.StartsWith("ErrorSuite-CaseB"));
        Assert.Contains(recordingPub.DeviceStepRows, r => r.Activity.Detail.StartsWith("ErrorSuite-CaseC"));
    }

    [Fact]
    public async Task ConnectionViewModel_ProbeCommand_DirectMode_ProbesDeviceAndSetsProbeResult_Test()
    {
        // Arrange
        const int port = 18094;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
        };

        // Act
        await connection.ProbeCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(connection.ProbeResult);
        Assert.Equal(512, connection.ProbeResult.MaxWindowNums);
        Assert.Equal(128, connection.ProbeResult.MaxSceneNums);
        Assert.True(connection.HasProbeWalls);
        Assert.NotEmpty(connection.ProbeResult.Walls!);
        Assert.Contains("Probe trực tiếp xong", connection.StatusMessage);
    }

    [Fact]
    public async Task ScenarioViewModel_RunSetupSceneScenario_ExecutesSuccessfully_Test()
    {
        const int port = 18095;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
        };
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true))
        {
            DelayBetweenStepsMs = 0,
        };

        // Chọn kịch bản #1 (không chụp hình)
        vm.SelectedScenarioItem = vm.ScenarioList.First(s => s.DisplayName.Contains("1. Thiết lập scene (không chụp hình)"));
        await vm.RunSelectedScenarioCommand.ExecuteAsync(null);

        Assert.Contains("Hoàn thành kịch bản", vm.StatusMessage);
        Assert.NotEmpty(recordingPub.DeviceStepRows);
    }

    [Fact]
    public async Task ScenarioViewModel_RunActiveSceneScenario_ExecutesSuccessfully_Test()
    {
        const int port = 18097;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
        };
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true))
        {
            DelayBetweenStepsMs = 0,
        };

        // Chọn kịch bản #3 (Active scene)
        vm.SelectedScenarioItem = vm.ScenarioList.First(s => s.DisplayName.Contains("3. Active scene"));
        await vm.RunSelectedScenarioCommand.ExecuteAsync(null);

        Assert.Contains("Hoàn thành kịch bản", vm.StatusMessage);
    }

    [Fact]
    public async Task VwDirectISAPIClient_PrematureResponseEnded_ReturnsErrorResult_Test()
    {
        // Arrange: Mô phỏng lỗi ngắt kết nối đột ngột ResponseEnded khi sai port hoặc thiết bị ngắt socket
        var httpIoException = new HttpRequestException(
            "An error occurred while sending the request.",
            new IOException("The response ended prematurely. (ResponseEnded)"));
        var mockHandler = new FaultyHttpMessageHandlerTest(httpIoException);
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("http://127.0.0.1:8000")
        };
        var creds = new VwDirectDeviceCredentials("127.0.0.1", 8000, "admin", "12345");
        var client = new VwDirectISAPIClient(httpClient, creds);

        // Act: Gọi UserCheck và SendRaw
        var userCheckResult = await client.UserCheck(CancellationToken.None);
        var rawResult = await client.SendRaw("GET", "DisplayDev/VideoWall/capabilities", null, null, CancellationToken.None);

        // Assert: Không crash app, trả ra kết quả Fail với mã thông báo lỗi chi tiết
        Assert.False(userCheckResult.Success);
        Assert.NotNull(userCheckResult.ErrorMessage);
        Assert.Contains("Không thể kết nối hoặc thiết bị ngắt kết nối đột ngột", userCheckResult.ErrorMessage);
        Assert.Contains("127.0.0.1:8000", userCheckResult.ErrorMessage);

        Assert.False(rawResult.Success);
        Assert.NotNull(rawResult.ErrorMessage);
        Assert.Contains("Không thể kết nối hoặc thiết bị ngắt kết nối đột ngột", rawResult.ErrorMessage);
    }

    [Fact]
    public async Task VwDirectDeviceConnectionClient_WhenHttpFails_PingAndProbeHandleGracefully_Test()
    {
        // Arrange
        var socketException = new HttpRequestException(
            "Connection refused",
            new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.ConnectionRefused));
        var mockHandler = new FaultyHttpMessageHandlerTest(socketException);
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("http://192.168.1.200:554")
        };
        var creds = new VwDirectDeviceCredentials("192.168.1.200", 554, "admin", "12345");
        var isApiClient = new VwDirectISAPIClient(httpClient, creds);
        var recordingPub = new RecordingPublisherTest();
        var directConnectionClient = new VwDirectDeviceConnectionClient(isApiClient, recordingPub);

        // Act: Thực hiện Ping, Probe và SendIsapi
        var pingStep = await directConnectionClient.Ping(CancellationToken.None);
        var probeResult = await directConnectionClient.Probe(null, CancellationToken.None);
        var sendStep = await directConnectionClient.SendIsapi("GET", "DisplayDev/VideoWall", null, null, CancellationToken.None);

        // Assert: Không throw exception, trả về step thất bại và ghi nhận notification
        Assert.False(pingStep.Success);
        Assert.NotNull(pingStep.Message);
        Assert.Contains("192.168.1.200:554", pingStep.Message);

        Assert.False(probeResult.Reachable);
        Assert.NotEmpty(probeResult.Steps!);
        Assert.False(probeResult.Steps![0].Success);

        Assert.False(sendStep.Success);
        Assert.NotNull(sendStep.Message);
        Assert.NotEmpty(recordingPub.DeviceStepRows);
    }

    [Fact]
    public async Task ConnectionViewModel_DirectMode_WhenPortWrong_UpdatesStatusMessageAndDoesNotCrash_Test()
    {
        // Arrange
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = 59999, // Cổng không có dịch vụ lắng nghe
            AdHocAccount = "admin",
            AdHocPassword = "wrong_password"
        };

        // Act: Bấm Connect (Ping) và Probe
        await connection.ConnectCommand.ExecuteAsync(null);

        // Assert: Không crash app, cập nhật IsConnected = false và hiển thị thông báo lỗi lên StatusMessage
        Assert.False(connection.IsConnected);
        Assert.Contains("thất bại", connection.StatusMessage);

        // Act: Bấm Probe
        await connection.ProbeCommand.ExecuteAsync(null);

        // Assert
        Assert.False(connection.IsConnected);
        Assert.Contains("Không kết nối được", connection.StatusMessage);
    }

    [Fact]
    public void StatusColorConverter_ReturnsCorrectBrushes_Test()
    {
        var converter = new StatusColorConverter();
        var redResult = converter.Convert("Kết nối trực tiếp thất bại: Không thể kết nối", typeof(System.Windows.Media.Brush), null, System.Globalization.CultureInfo.InvariantCulture);
        var inProgressResult = converter.Convert("Đang kết nối trực tiếp tới thiết bị...", typeof(System.Windows.Media.Brush), null, System.Globalization.CultureInfo.InvariantCulture);
        var successResult = converter.Convert("✅ Kết nối trực tiếp thành công tới 192.168.1.100", typeof(System.Windows.Media.Brush), null, System.Globalization.CultureInfo.InvariantCulture);
        var warningResult = converter.Convert("⚠️ Vui lòng nhập Scene Code cần kích hoạt", typeof(System.Windows.Media.Brush), null, System.Globalization.CultureInfo.InvariantCulture);

        // Khi lỗi: màu đỏ
        var redBrush = Assert.IsType<System.Windows.Media.SolidColorBrush>(redResult);
        Assert.Equal(System.Windows.Media.Color.FromRgb(0xC5, 0x30, 0x30), redBrush.Color);

        // Khi đang kết nối / bình thường: màu đen mặc định
        var inProgressBrush = Assert.IsType<System.Windows.Media.SolidColorBrush>(inProgressResult);
        Assert.Equal(System.Windows.Media.Color.FromRgb(0x2D, 0x37, 0x48), inProgressBrush.Color);

        // Khi thành công: màu xanh
        var greenBrush = Assert.IsType<System.Windows.Media.SolidColorBrush>(successResult);
        Assert.Equal(System.Windows.Media.Color.FromRgb(0x27, 0x67, 0x49), greenBrush.Color);

        // Khi cảnh báo: màu cam
        var orangeBrush = Assert.IsType<System.Windows.Media.SolidColorBrush>(warningResult);
        Assert.Equal(System.Windows.Media.Color.FromRgb(0xC0, 0x56, 0x21), orangeBrush.Color);
    }

    [Fact]
    public async Task SceneSetupViewModel_PushToDevice_WithoutWallNo_IsBlockedEarly_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = 18090,
            WallNo = null,
        };

        var vm = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub)
        {
            CurrentScene = new Module.VideoWall.WPF.Api.Dto.VwSceneDto
            {
                ID = "scene-1",
                Name = "Scene 1",
            },
            WallNo = null,
            DryRun = true,
        };

        await vm.PushToDeviceCommand.ExecuteAsync(null);

        Assert.Contains("Cần nhập WallNo trước khi đẩy", vm.StatusMessage);
        Assert.Empty(recordingPub.DeviceStepRows);
    }

    [Fact]
    public async Task ScenarioViewModel_RunCommands_WithoutWallNo_AreBlockedEarly_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = 18090,
            WallNo = null,
        };

        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true))
        {
            BuiltInWallNo = null,
            BuiltInSelectedSceneCode = "1",
        };

        // Test Overlapping test blocked
        await vm.RunOverlappingSizeTestCommand.ExecuteAsync(null);
        Assert.Contains("Cần nhập WallNo", vm.StatusMessage);

        // Test Error suite blocked
        await vm.RunErrorValidationSuiteCommand.ExecuteAsync(null);
        Assert.Contains("Cần nhập WallNo", vm.StatusMessage);

        Assert.Empty(recordingPub.DeviceStepRows);
    }

    [Fact]
    public async Task SceneSetupViewModel_CreateScene_WithSidExceedingMaxSceneNums_IsBlockedEarly_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.194",
            ProbeResult = new Module.VideoWall.WPF.Api.Dto.VwProbeDeviceOutput
            {
                MaxSceneNums = 128,
            }
        };

        VwLocalSceneStore.SaveData(connection.DeviceKey, new VwLocalSceneData());

        var vm = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub)
        {
            SceneName = "Scene Invalid",
            SceneOutputId = "129",
            GridCols = 2,
            GridRows = 1,
        };

        await vm.CreateSceneCommand.ExecuteAsync(null);

        Assert.Contains("vượt quá giới hạn thiết bị (tối đa 128)", vm.StatusMessage);
        Assert.Empty(vm.Scenes);
    }

    [Fact]
    public async Task ConnectionViewModel_Probe_WithoutWallNo_AutoSelectsFirstWall_Test()
    {
        const int port = 18098;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = null,
        };

        await connection.ProbeCommand.ExecuteAsync(null);

        Assert.NotNull(connection.ProbeResult);
        Assert.NotEmpty(connection.ProbeResult.Walls!);
        Assert.True(connection.HasProbeWalls);
        // Tự động chọn WallNo đầu tiên
        Assert.Equal(connection.ProbeResult.Walls[0].Id, connection.WallNo);
        Assert.Contains("Probe trực tiếp xong: WallNo", connection.StatusMessage);
    }

    [Fact]
    public async Task SceneSetupViewModel_LoadScreensAndSources_AutoSelectsFirstItemInAllLists_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.199",
            AdHocPort = 18090,
            ProbeResult = new Module.VideoWall.WPF.Api.Dto.VwProbeDeviceOutput
            {
                InputChannels =
                [
                    new() { Id = 1, Name = "Camera 01" },
                    new() { Id = 2, Name = "Camera 02" }
                ],
                Outputs =
                [
                    new() { Id = 1, OutputId = 1 },
                    new() { Id = 2, OutputId = 2 }
                ],
            }
        };

        var deviceKey = connection.DeviceKey;
        // Lưu 2 screens và 2 scenes mẫu
        VwLocalScreenStore.Save(deviceKey,
        [
            new Module.VideoWall.WPF.Api.Dto.VwScreenDto { ID = "scr-1", Name = "Màn 1", OutPutPort = "1", GridCol = 0, GridRow = 0, WidthPx = "1920", HeightPx = "1080" },
            new Module.VideoWall.WPF.Api.Dto.VwScreenDto { ID = "scr-2", Name = "Màn 2", OutPutPort = "2", GridCol = 1, GridRow = 0, WidthPx = "1920", HeightPx = "1080" }
        ]);
        VwLocalSceneStore.SaveData(deviceKey, new VwLocalSceneData());
        VwLocalSceneStore.AddScene(deviceKey, new Module.VideoWall.WPF.Api.Dto.VwSceneDto { ID = "scn-1", Name = "Scene 1", OutputId = "1" });
        VwLocalSceneStore.AddScene(deviceKey, new Module.VideoWall.WPF.Api.Dto.VwSceneDto { ID = "scn-2", Name = "Scene 2", OutputId = "1" });

        var vm = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub);

        await vm.LoadScreensAndSourcesCommand.ExecuteAsync(null);

        // 1. Tự động chọn kịch bản đầu tiên
        Assert.NotNull(vm.CurrentScene);
        Assert.Equal("scn-1", vm.CurrentScene.ID);

        // 2. Tự động chọn màn hình đầu tiên trong danh sách gán
        Assert.NotNull(vm.SelectedScreenAssignment);
        Assert.Equal("scr-1", vm.SelectedScreenAssignment.Screen.ID);

        // 3. Tự động chọn nguồn tín hiệu đầu tiên cho từng dòng gán màn hình
        Assert.All(vm.ScreenAssignments, row =>
        {
            Assert.NotNull(row.SelectedSource);
            Assert.Equal("1", row.SelectedSource.ID);
        });

        // 4. Tự động chọn cổng ra (Output) đầu tiên cho biểu mẫu thêm màn hình
        Assert.NotNull(vm.SelectedOutputChannel);
        Assert.Equal(1, vm.SelectedOutputChannel.Id);
    }

    [Fact]
    public void SceneSetupViewModel_AddWindowRow_AutoSelectsFirstSource_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.198",
        };

        var vm = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub);
        vm.Sources.Add(new Module.VideoWall.WPF.Api.Dto.VwSourceDto { ID = "src-1", Name = "Nguồn 1", SignalNo = 1 });
        vm.Sources.Add(new Module.VideoWall.WPF.Api.Dto.VwSourceDto { ID = "src-2", Name = "Nguồn 2", SignalNo = 2 });

        vm.AddWindowRowCommand.Execute(null);

        Assert.Single(vm.WindowRows);
        Assert.NotNull(vm.WindowRows[0].SelectedSource);
        Assert.Equal("src-1", vm.WindowRows[0].SelectedSource.ID);
    }

    [Fact]
    public async Task SceneSetupViewModel_DeleteScene_AutoSelectsFirstRemainingScene_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.197",
        };

        var deviceKey = connection.DeviceKey;
        VwLocalSceneStore.SaveData(deviceKey, new VwLocalSceneData());
        var s1 = VwLocalSceneStore.AddScene(deviceKey, new Module.VideoWall.WPF.Api.Dto.VwSceneDto { ID = "s-1", Name = "Scene 1", OutputId = "1" });
        var s2 = VwLocalSceneStore.AddScene(deviceKey, new Module.VideoWall.WPF.Api.Dto.VwSceneDto { ID = "s-2", Name = "Scene 2", OutputId = "1" });

        var vm = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub);
        vm.CurrentScene = s1;

        await vm.DeleteSceneCommand.ExecuteAsync(null);

        Assert.Single(vm.Scenes);
        // Tự động chọn kịch bản còn lại đầu tiên
        Assert.NotNull(vm.CurrentScene);
        Assert.Equal("s-2", vm.CurrentScene.ID);
    }

    [Fact]
    public void ScenarioViewModel_CatalogAndScenarios_AutoSelectsFirstItemByDefault_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
        };

        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true));

        // 1. Tự động chọn kịch bản đầu tiên trong danh sách kịch bản mẫu
        Assert.NotNull(vm.SelectedScenarioItem);
        Assert.Equal(vm.ScenarioList[0], vm.SelectedScenarioItem);

        // 2. Nhóm API mặc định là nhóm đầu tiên "Tất cả"
        Assert.Equal("Tất cả", vm.SelectedApiGroup);

        // 3. Tự động chọn API đầu tiên trong catalog
        Assert.NotNull(vm.SelectedPresetInCatalog);
        Assert.Equal(vm.FilteredPresets[0], vm.SelectedPresetInCatalog);
    }

    [Fact]
    public void VwIsapiFormFieldViewModel_WithOptions_AutoSelectsFirstOptionWhenNoDefault_Test()
    {
        var def = new Module.VideoWall.WPF.ViewModels.Isapi.VwIsapiFieldDef(
            "mode",
            "Chế độ",
            Module.VideoWall.WPF.ViewModels.Isapi.VwIsapiFieldKind.BodyField,
            Module.VideoWall.WPF.ViewModels.Isapi.VwIsapiFieldType.Enum,
            false,
            null,
            null,
            ["auto", "manual", "semi"]);

        var fieldVm = new Module.VideoWall.WPF.ViewModels.Isapi.VwIsapiFormFieldViewModel(def);

        Assert.Equal("auto", fieldVm.Value);
    }



    [Fact]
    public void NullToVisibilityConverters_HandleNullAndEmptyCollections_Test()
    {
        var nullToVis = new Module.VideoWall.WPF.Interaction.NullToVisibilityConverter();
        var invNullToVis = new Module.VideoWall.WPF.Interaction.InverseNullToVisibilityConverter();

        // Null -> Collapsed / Visible
        Assert.Equal(System.Windows.Visibility.Collapsed, nullToVis.Convert(null, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(System.Windows.Visibility.Visible, invNullToVis.Convert(null, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));

        // Chuỗi rỗng -> Collapsed / Visible
        Assert.Equal(System.Windows.Visibility.Collapsed, nullToVis.Convert("   ", typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(System.Windows.Visibility.Visible, invNullToVis.Convert("   ", typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));

        // List rỗng -> Collapsed / Visible
        var emptyList = new List<string>();
        Assert.Equal(System.Windows.Visibility.Collapsed, nullToVis.Convert(emptyList, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(System.Windows.Visibility.Visible, invNullToVis.Convert(emptyList, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));

        // List có phần tử -> Visible / Collapsed
        var populatedList = new List<string> { "Item 1" };
        Assert.Equal(System.Windows.Visibility.Visible, nullToVis.Convert(populatedList, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(System.Windows.Visibility.Collapsed, invNullToVis.Convert(populatedList, typeof(System.Windows.Visibility), null, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void SceneSetupViewModel_EmptyStates_ReflectCollectionCount_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.193",
        };
        VwLocalSceneStore.SaveData(connection.DeviceKey, new VwLocalSceneData());
        VwLocalScreenStore.Save(connection.DeviceKey, []);

        var vm = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub);

        Assert.False(vm.HasScenes);
        Assert.False(vm.HasScreenAssignments);
        Assert.False(vm.HasWindowRows);
        Assert.False(vm.HasSceneWindows);

        vm.Scenes.Add(new Module.VideoWall.WPF.Api.Dto.VwSceneDto { ID = "1", Name = "Scene 1" });
        Assert.True(vm.HasScenes);

        vm.ScreenAssignments.Add(new Module.VideoWall.WPF.ViewModels.ScreenAssignmentRow(new Module.VideoWall.WPF.Api.Dto.VwScreenDto { ID = "1", Name = "Screen 1" }));
        Assert.True(vm.HasScreenAssignments);

        vm.WindowRows.Add(new Module.VideoWall.WPF.ViewModels.WindowSceneRow());
        Assert.True(vm.HasWindowRows);

        vm.SceneWindows.Add(new Module.VideoWall.WPF.Api.Dto.VwWindowSceneDto { ID = "w1" });
        Assert.True(vm.HasSceneWindows);
    }

    [Fact]
    public void ScenarioViewModel_SearchApi_WhenNoMatch_HasFilteredPresetsIsFalse_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.192",
        };

        var vm = new ScenarioViewModel(connection, activityPub, recordingPub);
        Assert.True(vm.HasFilteredPresets);

        // Tìm từ khóa không tồn tại
        vm.ApiSearchText = "non_existing_api_xyz_9999";
        Assert.False(vm.HasFilteredPresets);
        Assert.Empty(vm.FilteredPresets);

        // Xóa tìm kiếm -> quay lại có danh sách
        vm.ApiSearchText = string.Empty;
        Assert.True(vm.HasFilteredPresets);
        Assert.NotEmpty(vm.FilteredPresets);
    }

    [Fact]
    public void VwIsapiPresetList_PostAndPutPresets_HaveSampleBodies_Test()
    {
        var windowAddPreset = Module.VideoWall.WPF.ViewModels.VwIsapiPresetList.Presets.First(p => p.Section == "9.7.11.4");
        Assert.NotNull(windowAddPreset.SampleBody);
        Assert.Contains("<WallWindow", windowAddPreset.SampleBody);

        var videoWallPutPreset = Module.VideoWall.WPF.ViewModels.VwIsapiPresetList.Presets.First(p => p.Section == "9.7.5.3");
        Assert.NotNull(videoWallPutPreset.SampleBody);
        Assert.Contains("<VideoWall", videoWallPutPreset.SampleBody);

        var scenePutPreset = Module.VideoWall.WPF.ViewModels.VwIsapiPresetList.Presets.First(p => p.Section == "9.7.7.2");
        Assert.NotNull(scenePutPreset.SampleBody);
        Assert.Contains("<Scene", scenePutPreset.SampleBody);

        var planPostPreset = Module.VideoWall.WPF.ViewModels.VwIsapiPresetList.Presets.First(p => p.Section == "9.7.6.1");
        Assert.NotNull(planPostPreset.SampleBody);
        Assert.Contains("<Plan", planPostPreset.SampleBody);

        var outputPutPreset = Module.VideoWall.WPF.ViewModels.VwIsapiPresetList.Presets.First(p => p.Section == "9.7.3.6");
        Assert.NotNull(outputPutPreset.SampleBody);
        Assert.Contains("<VideoOutputChannel", outputPutPreset.SampleBody);
    }

    [Fact]
    public void VwIsapiFormViewModel_WhenPresetHasSampleBody_InitializesRawBodyAndSyncsConnection_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.191",
        };

        var preset = Module.VideoWall.WPF.ViewModels.VwIsapiPresetList.Presets.First(p => p.Section == "9.7.11.4");
        connection.SelectedIsapiPreset = preset;

        Assert.NotNull(connection.ActiveIsapiForm);
        Assert.Equal(preset.SampleBody, connection.ActiveIsapiForm.RawBody);
        Assert.Equal(preset.SampleBody, connection.IsapiBody);

        // Khi người dùng chỉnh sửa RawBody
        connection.ActiveIsapiForm.RawBody = "<CustomBody>123</CustomBody>";
        Assert.Equal("<CustomBody>123</CustomBody>", connection.IsapiBody);
    }

    [Fact]
    public void VwScenarioStepViewModel_WhenPresetHasSampleBody_InitializesRawBody_Test()
    {
        var preset = Module.VideoWall.WPF.ViewModels.VwIsapiPresetList.Presets.First(p => p.Section == "9.7.11.4");
        var step = new Module.VideoWall.WPF.ViewModels.VwScenarioStepViewModel(preset);

        Assert.Equal(preset.SampleBody, step.RawBody);
        Assert.True(step.NeedsBody);
    }

    [Fact]
    public async Task VwDirectSetupSceneOrchestrator_Guardrails_Bypassed_WhenSkipGuardrailsIsTrue_Test()
    {
        const int port = 18092;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var creds = new VwDirectDeviceCredentials("127.0.0.1", port, "admin", "Password123!");
        var orchestrator = VwDirectClientFactory.CreateSetupSceneOrchestrator(creds);

        // Khi SkipGuardrails = false (mặc định): SceneId 99999 bị chặn sớm
        var resultBlocked = await orchestrator.Execute(new Module.VideoWall.WPF.Api.Direct.VwDirectPushSceneInput
        {
            SceneId = 99999,
            WallNo = 1,
            SkipGuardrails = false,
            DryRun = true,
        }, CancellationToken.None);

        Assert.False(resultBlocked.Success);
        Assert.Contains("must be in range", resultBlocked.Message);

        // Khi SkipGuardrails = true: Vượt qua chặn sớm của Capabilities và gửi tiếp
        var resultBypassed = await orchestrator.Execute(new Module.VideoWall.WPF.Api.Direct.VwDirectPushSceneInput
        {
            SceneId = 99999,
            WallNo = 1,
            SkipGuardrails = true,
            DryRun = true,
        }, CancellationToken.None);

        // Đã đi qua bước 2 và bước 3 (GetVideoWalls)
        Assert.Contains(resultBypassed.Steps, s => s.Name == "GetVideoWalls");
    }

    [Fact]
    public async Task ScenarioViewModel_ErrorValidationSuite_WhenSendRealTrue_PromptsConfirmation_AndWhenConfirmed_SendsReal_Test()
    {
        const int port = 18093;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = port,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
        };

        var confirmation = new UserConfirmationTest(true);
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, confirmation)
        {
            DelayBetweenStepsMs = 0,
            ErrorSuiteSendReal = true,
        };

        await vm.RunErrorValidationSuiteCommand.ExecuteAsync(null);

        Assert.Equal(1, confirmation.CallCount);
        Assert.Contains("Đã chạy xong bộ kiểm thử lỗi", vm.StatusMessage);
    }

    [Fact]
    public async Task ScenarioViewModel_ErrorValidationSuite_WhenSendRealTrue_WhenCancelled_Aborts_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            WallNo = 1,
        };

        var confirmation = new UserConfirmationTest(false);
        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, confirmation)
        {
            DelayBetweenStepsMs = 0,
            ErrorSuiteSendReal = true,
        };

        await vm.RunErrorValidationSuiteCommand.ExecuteAsync(null);

        Assert.Equal(1, confirmation.CallCount);
        Assert.Equal("Đã huỷ chạy bộ kiểm thử lỗi.", vm.StatusMessage);
    }

    [Fact]
    public void VwIsapiFormFieldViewModel_Validate_RequiredField_ReturnsErrorWhenEmpty_Test()
    {
        var fieldDef = new VwIsapiFieldDef("BoardID", "ID Board", VwIsapiFieldKind.PathParam, VwIsapiFieldType.Int, Required: true);
        var fieldVm = new VwIsapiFormFieldViewModel(fieldDef);

        // Trường hợp để trống
        fieldVm.Value = string.Empty;
        var isValidEmpty = fieldVm.Validate();
        Assert.False(isValidEmpty);
        Assert.True(fieldVm.HasError);
        Assert.Contains("ID Board", fieldVm.ErrorMessage);

        // Trường hợp nhập sai kiểu Int
        fieldVm.Value = "abc";
        var isValidAlpha = fieldVm.Validate();
        Assert.False(isValidAlpha);
        Assert.Contains("phải là số nguyên", fieldVm.ErrorMessage);

        // Trường hợp nhập hợp lệ
        fieldVm.Value = "3";
        var isValidValid = fieldVm.Validate();
        Assert.True(isValidValid);
        Assert.False(fieldVm.HasError);
        Assert.Null(fieldVm.ErrorMessage);
    }

    [Fact]
    public void VwIsapiFormViewModel_Validate_FailsWhenRequiredPathParamMissing_Test()
    {
        var preset = Module.VideoWall.WPF.ViewModels.VwIsapiPresetList.Presets.First(p => p.Section == "9.7.1.1");
        var form = new VwIsapiFormViewModel(preset);

        var boardField = form.PathFields.First(f => f.Definition.Key == "BoardID");
        boardField.Value = string.Empty;

        var isValid = form.Validate(out var error);
        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("ID Board", error);

        boardField.Value = "5";
        var isValidSuccess = form.Validate(out var errorSuccess);
        Assert.True(isValidSuccess);
        Assert.Null(errorSuccess);
        Assert.Contains("ISAPI/System/Board/5/config", form.BuildPath());
    }

    [Fact]
    public async Task ConnectionViewModel_SendIsapi_WhenRequiredPathParamMissing_BlocksSend_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
        };

        var preset = Module.VideoWall.WPF.ViewModels.VwIsapiPresetList.Presets.First(p => p.Section == "9.7.1.1");
        connection.SelectedIsapiPreset = preset;

        var boardField = connection.ActiveIsapiForm!.PathFields.First(f => f.Definition.Key == "BoardID");
        boardField.Value = string.Empty;

        await connection.SendIsapiCommand.ExecuteAsync(null);

        Assert.Contains("⚠️", connection.StatusMessage);
        Assert.Contains("ID Board", connection.StatusMessage);
    }

    [Fact]
    public async Task ConnectionViewModel_SendIsapi_WhenPathHasUnreplacedPlaceholders_BlocksSend_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            ActiveIsapiForm = null,
            IsapiPath = "ISAPI/System/Board/{boardId}/config",
        };

        await connection.SendIsapiCommand.ExecuteAsync(null);

        Assert.Contains("Đường dẫn còn chứa tham số chưa nhập", connection.StatusMessage);
    }

    [Fact]
    public async Task ScenarioViewModel_ExecuteScenario_WhenStepHasMissingPathParam_BlocksAndSetsLastFailedStep_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
        };

        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true));
        vm.NewScenarioCommand.Execute(null);
        var preset = Module.VideoWall.WPF.ViewModels.VwIsapiPresetList.Presets.First(p => p.Section == "9.7.1.1");
        var step = new Module.VideoWall.WPF.ViewModels.VwScenarioStepViewModel(preset);
        var boardField = step.Form.PathFields.First(f => f.Definition.Key == "BoardID");
        boardField.Value = string.Empty;

        vm.Steps.Add(step);
        vm.SelectedScenarioItem = null;

        await vm.RunSelectedScenarioCommand.ExecuteAsync(null);

        Assert.Equal(0, vm.LastFailedStepIndex);
        Assert.Contains("thiếu tham số", vm.StatusMessage);
    }

    [Fact]
    public void ScenarioViewModel_SaveScenario_WhenStepHasMissingPathParam_BlocksSave_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));

        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true))
        {
            ScenarioName = "Kịch bản test thiếu tham số",
        };

        var preset = Module.VideoWall.WPF.ViewModels.VwIsapiPresetList.Presets.First(p => p.Section == "9.7.1.1");
        var step = new Module.VideoWall.WPF.ViewModels.VwScenarioStepViewModel(preset);
        var boardField = step.Form.PathFields.First(f => f.Definition.Key == "BoardID");
        boardField.Value = string.Empty;

        vm.Steps.Add(step);

        vm.SaveScenarioCommand.Execute(null);

        Assert.Contains("thiếu tham số bắt buộc", vm.StatusMessage);
    }

    [Fact]
    public async Task ScenarioViewModel_ExecuteScenario_PublishesActivityLogs_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
        };

        var vm = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true));
        var preset = Module.VideoWall.WPF.ViewModels.VwIsapiPresetList.Presets.First(p => p.Section == "9.7.1.1");
        var step = new Module.VideoWall.WPF.ViewModels.VwScenarioStepViewModel(preset);
        var boardField = step.Form.PathFields.First(f => f.Definition.Key == "BoardID");
        boardField.Value = "1";

        vm.Steps.Add(step);
        vm.SelectedScenarioItem = null;

        await vm.RunSelectedScenarioCommand.ExecuteAsync(null);

        Assert.NotEmpty(recordingPub.Notifications);
    }

    [Fact]
    public void MainViewModel_IsResponseVisible_OnlyForIsapiTabs_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));
        var paramsVm = new ParametersViewModel(connection);
        var sceneSetup = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true));
        var invoker = new ApiInvoker(new InMemoryApiClientFactoryTest(new HttpClient()), activityPub);
        var apiClient = new VideoWallApiClient(invoker, recordingPub, activityPub);
        var schedule = new ScheduleViewModel(apiClient, activityPub);
        var scenario = new ScenarioViewModel(connection, activityPub, recordingPub, new UserConfirmationTest(true));
        var sessionState = new Module.VideoWall.WPF.Auth.SessionState();

        var mainVm = new MainViewModel(sessionState, activityPub, connection, paramsVm, sceneSetup, schedule, scenario);

        // Tab 1: Thiết lập Scene (Index 0) -> Không hiện Response
        mainVm.SelectedTabIndex = 0;
        Assert.False(mainVm.IsResponseVisible);

        // Tab 2: Board (Index 1) -> Hiện Response
        mainVm.SelectedTabIndex = 1;
        Assert.True(mainVm.IsResponseVisible);

        // Tab 6: Video Wall (Index 5) -> Hiện Response
        mainVm.SelectedTabIndex = 5;
        Assert.True(mainVm.IsResponseVisible);
    }

    private sealed class FaultyHttpMessageHandlerTest(Exception exceptionToThrow) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(exceptionToThrow);
        }
    }
}
