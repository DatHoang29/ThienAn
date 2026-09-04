using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Module.VideoWall.WPF.Api;
using Module.VideoWall.WPF.Api.Direct;
using Module.VideoWall.WPF.Api.Direct.Isapi;
using Module.VideoWall.WPF.Auth;
using Module.VideoWall.WPF.Controls;
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
    public void SceneSetupViewModel_AddSceneWindow_RestrictsToAvailableScreensCount_AndAssignsCorrectCoordinates_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            ProbeResult = new Module.VideoWall.WPF.Api.Dto.VwProbeDeviceOutput
            {
                Reachable = true,
                MaxWindowNums = 6,
                Outputs =
                [
                    new() { Id = 1, OutputId = 1, Rect = new() { Width = 1920, Height = 1080, Coordinate = new() { X = 0, Y = 0 } } },
                    new() { Id = 2, OutputId = 2, Rect = new() { Width = 1920, Height = 1080, Coordinate = new() { X = 1920, Y = 0 } } },
                    new() { Id = 3, OutputId = 3, Rect = new() { Width = 1920, Height = 1080, Coordinate = new() { X = 0, Y = 1920 } } },
                    new() { Id = 4, OutputId = 4, Rect = new() { Width = 1920, Height = 1080, Coordinate = new() { X = 1920, Y = 1920 } } },
                ]
            }
        };

        var vm = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub)
        {
            CurrentScene = new Module.VideoWall.WPF.Api.Dto.VwSceneDto
            {
                ID = "test-scene-4screens",
                Code = "SCN_4SCR",
                Name = "Tường 4 Màn Hình"
            }
        };

        // Nạp kết quả khảo sát
        vm.SyncFromProbeResult();
        Assert.Equal(4, vm.AvailableStartScreens.Count);
        Assert.Equal(6, vm.GetMaxAllowedWindows());

        // Xoá danh sách ô hiện tại để thêm từ đầu
        vm.SceneWindows.Clear();
        Assert.True(vm.AddSceneWindowCommand.CanExecute(null));

        // Thêm 4 ô lớp 1 phủ kín 4 màn hình (ZIndex = 1)
        vm.AddSceneWindowCommand.Execute(null); // Ô 1 -> Màn 1 (0, 0), ZIndex = 1
        vm.AddSceneWindowCommand.Execute(null); // Ô 2 -> Màn 2 (1920, 0), ZIndex = 1
        vm.AddSceneWindowCommand.Execute(null); // Ô 3 -> Màn 3 (0, 1920), ZIndex = 1
        vm.AddSceneWindowCommand.Execute(null); // Ô 4 -> Màn 4 (1920, 1920), ZIndex = 1

        Assert.Equal(4, vm.SceneWindows.Count);
        Assert.Equal(1, vm.SceneWindows[0].ZIndex);
        Assert.Equal(1, vm.SceneWindows[3].ZIndex);
        Assert.True(vm.AddSceneWindowCommand.CanExecute(null)); // Vẫn còn slot vì MaxWindowNums = 6

        // Thêm ô 5 -> Xoay vòng Màn 1 (0, 0) nhưng là Lớp 2 (ZIndex = 2 đè lên trên ô 1)
        vm.AddSceneWindowCommand.Execute(null);
        Assert.Equal(5, vm.SceneWindows.Count);
        Assert.Equal(0, vm.SceneWindows[4].X);
        Assert.Equal(0, vm.SceneWindows[4].Y);
        Assert.Equal(2, vm.SceneWindows[4].ZIndex);

        // Thêm ô 6 -> Xoay vòng Màn 2 (1920, 0), Lớp 2 (ZIndex = 2 đè lên trên ô 2)
        vm.AddSceneWindowCommand.Execute(null);
        Assert.Equal(6, vm.SceneWindows.Count);
        Assert.Equal(1920, vm.SceneWindows[5].X);
        Assert.Equal(0, vm.SceneWindows[5].Y);
        Assert.Equal(2, vm.SceneWindows[5].ZIndex);

        // Đã đạt MaxWindowNums = 6 -> CanExecute chuyển sang false
        Assert.False(vm.AddSceneWindowCommand.CanExecute(null));

        // Cố tình gọi thêm ô thứ 7 -> Bị chặn và hiển thị thông báo
        vm.AddSceneWindowCommand.Execute(null);
        Assert.Equal(6, vm.SceneWindows.Count);
        Assert.Contains("tối đa giới hạn của thiết bị", vm.StatusMessage);

        // Xoá bớt 1 ô -> CanExecute bật lại true
        vm.SceneWindows.RemoveAt(5);
        Assert.True(vm.AddSceneWindowCommand.CanExecute(null));
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
        Assert.Contains("Khảo sát", connection.StatusMessage);
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
        // Ngay lần Probe đầu tiên khi WallNo = null, Outputs và Dimensions đã được nạp đầy đủ
        Assert.NotNull(connection.ProbeResult.Outputs);
        Assert.NotEmpty(connection.ProbeResult.Outputs);
        Assert.True(connection.ProbeTotalWidth > 0);
        Assert.True(connection.ProbeTotalHeight > 0);
        Assert.Equal("7680 × 5760 px", connection.ProbeTotalDimensionText);
        Assert.Equal("12 Cổng (Màn hình)", connection.ProbeOutputCountText);
        Assert.Contains("Tường #1", connection.StatusMessage);
    }

    [Fact]
    public void ConnectionViewModel_DefaultWallNo_IsNull_And_HasProbeWallsIsFalse_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));

        Assert.Null(connection.WallNo);
        Assert.False(connection.HasProbeWalls);
    }

    [Fact]
    public void SceneSetupViewModel_InitializesWithNullWallNo_AndSyncsWhenConnectionWallNoChanges_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));
        var sceneSetup = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub);

        Assert.Null(sceneSetup.WallNo);

        connection.WallNo = 2;
        Assert.Equal(2, sceneSetup.WallNo);
    }

    [Fact]
    public void ConnectionViewModel_SwitchingWallNo_PreservesSelectedWallNoAndWallsCollection_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            ProbeResult = new Module.VideoWall.WPF.Api.Dto.VwProbeDeviceOutput
            {
                Reachable = true,
                WallNo = 1,
                Walls =
                [
                    new() { Id = 1, Name = "VideoWall 1" },
                    new() { Id = 2, Name = "hoangnhu" }
                ],
                Outputs = [new() { Id = 1, OutputId = 1, Rect = new() { Width = 1920, Height = 1080 } }]
            }
        };

        Assert.Equal(2, connection.Walls.Count);
        Assert.Equal(1, connection.WallNo);

        // Chuyển sang chọn Wall #2 ("hoangnhu")
        connection.WallNo = 2;

        Assert.Equal(2, connection.WallNo);
        Assert.Equal(2, connection.Walls.Count);
        Assert.Equal("hoangnhu", connection.Walls[1].Name);
    }

    [Fact]
    public void SceneSetupViewModel_AvailableStartScreens_MatchesProbeOutputsCount_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));
        var sceneSetup = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub);

        // Mặc định trước khi Probe: danh sách màn bắt đầu trống rỗng (bắt buộc phải Probe)
        Assert.Empty(sceneSetup.AvailableStartScreens);

        // Giả lập Probe Wall #2 có 4 màn hình (lưới 2x2)
        connection.ProbeResult = new Module.VideoWall.WPF.Api.Dto.VwProbeDeviceOutput
        {
            Reachable = true,
            WallNo = 2,
            Outputs =
            [
                new() { Id = 1, OutputId = 17235971, Rect = new() { Coordinate = new() { X = 0, Y = 0 }, Width = 1920, Height = 1920 } },
                new() { Id = 2, OutputId = 17235972, Rect = new() { Coordinate = new() { X = 1920, Y = 0 }, Width = 1920, Height = 1920 } },
                new() { Id = 3, OutputId = 17235973, Rect = new() { Coordinate = new() { X = 0, Y = 1920 }, Width = 1920, Height = 1920 } },
                new() { Id = 4, OutputId = 17235974, Rect = new() { Coordinate = new() { X = 1920, Y = 1920 }, Width = 1920, Height = 1920 } },
            ]
        };

        // Sau khi nạp ProbeResult của Wall 2: Danh sách màn bắt đầu co lại đúng 4 màn hình!
        Assert.Equal(4, sceneSetup.AvailableStartScreens.Count);
        Assert.Equal("Màn 1", sceneSetup.AvailableStartScreens[0].Name);
        Assert.Equal("Màn 4", sceneSetup.AvailableStartScreens[3].Name);
    }

    [Fact]
    public void SceneSetupViewModel_WallSpecificScenes_DistinctPerWall_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = $"10.55.{Random.Shared.Next(10, 99)}.{Random.Shared.Next(10, 99)}",
            AdHocPort = 18080,
            WallNo = 1
        };
        var sceneSetup = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub);

        // Wall 1: có 3 kịch bản mẫu cho lưới 2x2 (khớp phần cứng và log thiết bị)
        Assert.Equal(3, sceneSetup.Scenes.Count);
        Assert.Contains("Màn 1 Đơn + Màn 4 Chia 4", sceneSetup.Scenes[0].Name);

        // Chuyển sang Wall 2: tự động tải 3 kịch bản mẫu cho lưới 2x2
        connection.WallNo = 2;

        Assert.Equal(3, sceneSetup.Scenes.Count);
        Assert.Contains("Lưới 2×2", sceneSetup.Scenes[0].Name);
        Assert.Contains("Khối lớn 2×1", sceneSetup.Scenes[1].Name);
        Assert.Contains("Toàn tường 2×2", sceneSetup.Scenes[2].Name);
    }

    [Fact]
    public void SceneWindowRow_SelectedSizePreset_MatchesUniformAndStandardSizes_Test()
    {
        // 1. Tường 1 (1920x1920) -> Phải map về 1x1
        var row1 = new SceneWindowRow(new Module.VideoWall.WPF.Api.Dto.VwWindowSceneDto
        {
            W = 1920,
            H = 1920,
        });
        Assert.NotNull(row1.SelectedSizePreset);
        Assert.Equal("1x1", row1.SelectedSizePreset.Name);
        Assert.Contains(row1.SelectedSizePreset, SceneWindowRow.AvailableSizePresets);

        // 2. Chuẩn 1080p (1920x1080) -> Phải map về 1x1
        var row2 = new SceneWindowRow(new Module.VideoWall.WPF.Api.Dto.VwWindowSceneDto
        {
            W = 1920,
            H = 1080,
        });
        Assert.NotNull(row2.SelectedSizePreset);
        Assert.Contains("1x1", row2.SelectedSizePreset.Name);
        Assert.Contains(row2.SelectedSizePreset, SceneWindowRow.AvailableSizePresets);

        // 3. Khối lớn 2x2 (3840x3840) -> Phải map về 2x2
        var row3 = new SceneWindowRow(new Module.VideoWall.WPF.Api.Dto.VwWindowSceneDto
        {
            W = 3840,
            H = 3840,
        });
        Assert.NotNull(row3.SelectedSizePreset);
        Assert.Equal("2x2", row3.SelectedSizePreset.Name);

        // 4. Đổi kích thước qua SelectedSizePreset -> W và H phải tự cập nhật
        row1.SelectedSizePreset = SceneWindowRow.AvailableSizePresets.First(p => p.Name.StartsWith("2x1"));
        Assert.Equal(3840, row1.W);
        Assert.Equal(1920, row1.H);
    }

    [Fact]
    public void SceneWindowRow_NameProperty_UpdatesUnderlyingDto_AndNotifies_Test()
    {
        var dto = new Module.VideoWall.WPF.Api.Dto.VwWindowSceneDto
        {
            Name = "Ô 1 Ban đầu",
            Label = "Màn 1",
            OrderNo = 1
        };
        var row = new SceneWindowRow(dto);

        var propertyChangedList = new List<string>();
        row.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null)
                propertyChangedList.Add(e.PropertyName);
        };

        // Chỉnh sửa tên ô
        row.Name = "Camera Thu Phí Làn 1";

        Assert.Equal("Camera Thu Phí Làn 1", row.Name);
        Assert.Equal("Camera Thu Phí Làn 1", dto.Name);
        Assert.Contains(nameof(SceneWindowRow.Name), propertyChangedList);
    }

    [Fact]
    public void VisualWallCanvas_SnapToGrid_And_BoundingBox_Calculations_Test()
    {
        // 1. SnapToGrid: làm tròn khi kéo gần mép 1920px (độ lệch < 300px)
        Assert.Equal(1920, Module.VideoWall.WPF.Controls.VisualWallCanvas.SnapToGrid(1950));
        Assert.Equal(1920, Module.VideoWall.WPF.Controls.VisualWallCanvas.SnapToGrid(1850));
        Assert.Equal(3840, Module.VideoWall.WPF.Controls.VisualWallCanvas.SnapToGrid(3800));
        Assert.Equal(0, Module.VideoWall.WPF.Controls.VisualWallCanvas.SnapToGrid(50));

        // Snap vô điều kiện về bội số 1920px (ngăn chặn lỗi 53 winNotFillScreenOrCrossScreen)
        Assert.Equal(1920, Module.VideoWall.WPF.Controls.VisualWallCanvas.SnapToGrid(1000));

        // 2. SnapSize: làm tròn kích thước
        Assert.Equal(1920, Module.VideoWall.WPF.Controls.VisualWallCanvas.SnapSize(1900));
        Assert.Equal(3840, Module.VideoWall.WPF.Controls.VisualWallCanvas.SnapSize(3850));

        // 3. Tính toán Bounding Box của toàn bộ tường (TotalWallWidth / TotalWallHeight)
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            ProbeResult = new Module.VideoWall.WPF.Api.Dto.VwProbeDeviceOutput
            {
                Reachable = true,
                Outputs =
                [
                    new() { Id = 1, OutputId = 1, Rect = new() { Width = 1920, Height = 1080, Coordinate = new() { X = 0, Y = 0 } } },
                    new() { Id = 2, OutputId = 2, Rect = new() { Width = 1920, Height = 1080, Coordinate = new() { X = 1920, Y = 0 } } },
                    new() { Id = 3, OutputId = 3, Rect = new() { Width = 1920, Height = 1080, Coordinate = new() { X = 0, Y = 1920 } } },
                    new() { Id = 4, OutputId = 4, Rect = new() { Width = 1920, Height = 1080, Coordinate = new() { X = 1920, Y = 1920 } } },
                ]
            }
        };

        var sceneVm = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub);
        sceneVm.SyncFromProbeResult();

        // 4 màn hình 2x2 -> Chiều rộng tối thiểu 3840, Chiều cao tối thiểu 3840
        Assert.Equal(3840, sceneVm.TotalWallWidth);
        Assert.Equal(3840, sceneVm.TotalWallHeight);
    }

    [Fact]
    public void SceneSetupViewModel_Selection_And_SetGeometry_TwoWaySync_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true));
        var sceneVm = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub)
        {
            CurrentScene = new Module.VideoWall.WPF.Api.Dto.VwSceneDto { ID = "scn-sync-test", Name = "Test Sync" }
        };

        var row1 = new SceneWindowRow(new Module.VideoWall.WPF.Api.Dto.VwWindowSceneDto { ID = "w1", OrderNo = 1, X = 0, Y = 0, W = 1920, H = 1920 });
        var row2 = new SceneWindowRow(new Module.VideoWall.WPF.Api.Dto.VwWindowSceneDto { ID = "w2", OrderNo = 2, X = 1920, Y = 0, W = 1920, H = 1920 });
        sceneVm.SceneWindows.Add(row1);
        sceneVm.SceneWindows.Add(row2);

        // 1. Đồng bộ chọn trên Canvas -> DataGrid và IsActiveSelected
        sceneVm.SelectWindowFromCanvas(row1);
        Assert.Equal(row1, sceneVm.SelectedSceneWindow);
        Assert.True(row1.IsActiveSelected);
        Assert.False(row2.IsActiveSelected);

        sceneVm.SelectedSceneWindow = row2;
        Assert.False(row1.IsActiveSelected);
        Assert.True(row2.IsActiveSelected);

        // 2. Kéo thả SetGeometry -> Cập nhật W, H, X, Y và SizeLabel
        row1.SetGeometry(1920, 1920, 3840, 2160);
        Assert.Equal(1920, row1.X);
        Assert.Equal(1920, row1.Y);
        Assert.Equal(3840, row1.W);
        Assert.Equal(2160, row1.H);
        Assert.Equal("Khối lớn (2x2)", row1.SizeLabel);
    }

    [Fact]
    public void VisualWallCanvas_FormatScreenCoordinateRange_StartAndEndCoordinates_FormatsCorrectly_Test()
    {
        // Arrange & Act
        var screen1 = VisualWallCanvas.FormatScreenCoordinateRange(0, 0);
        var screen2 = VisualWallCanvas.FormatScreenCoordinateRange(1920, 0);
        var screen3 = VisualWallCanvas.FormatScreenCoordinateRange(3840, 0);
        var screen4 = VisualWallCanvas.FormatScreenCoordinateRange(5760, 0);
        var screen8 = VisualWallCanvas.FormatScreenCoordinateRange(5760, 1920);
        var screen9 = VisualWallCanvas.FormatScreenCoordinateRange(0, 3840);
        var screen12 = VisualWallCanvas.FormatScreenCoordinateRange(5760, 3840);

        // Assert - Màn 1 (Góc trên-trái)
        Assert.Equal("X: 0 ➔ 1920\nY: 0 ➔ 1920", screen1);

        // Assert - Màn 2 & 3
        Assert.Equal("X: 1920 ➔ 3840\nY: 0 ➔ 1920", screen2);
        Assert.Equal("X: 3840 ➔ 5760\nY: 0 ➔ 1920", screen3);

        // Assert - Màn 4 (Mép phải hàng 1) hiển thị rõ con số 7680
        Assert.Equal("X: 5760 ➔ 7680\nY: 0 ➔ 1920", screen4);

        // Assert - Màn 8 (Mép phải hàng 2)
        Assert.Equal("X: 5760 ➔ 7680\nY: 1920 ➔ 3840", screen8);

        // Assert - Màn 9 (Mép dưới hàng 3) hiển thị rõ con số 5760
        Assert.Equal("X: 0 ➔ 1920\nY: 3840 ➔ 5760", screen9);

        // Assert - Màn 12 (Góc dưới-phải) hiển thị rõ cả 7680 lẫn 5760
        Assert.Equal("X: 5760 ➔ 7680\nY: 3840 ➔ 5760", screen12);
    }

    [Theory]
    [InlineData(0.1, 4, 3, 7680, 5760, "X[0 ➔ 7680 px] × Y[0 ➔ 5760 px]")]
    [InlineData(0.25, 2, 2, 3840, 3840, "X[0 ➔ 3840 px] × Y[0 ➔ 3840 px]")]
    [InlineData(0.15, 1, 2, 1920, 3840, "X[0 ➔ 1920 px] × Y[0 ➔ 3840 px]")]
    public void VisualWallCanvas_FormatScaleInfo_DisplaysFullCoverageRange_Test(
        double scale, int cols, int rows, int wallW, int wallH, string expectedCoverage)
    {
        // Act
        var result = VisualWallCanvas.FormatScaleInfo(scale, cols, rows, wallW, wallH);

        // Assert
        Assert.Contains(expectedCoverage, result);
        Assert.Contains($"Lưới {cols} × {rows}", result);
    }

    [Fact]
    public void VisualWallCanvas_Grid4x3_All12Screens_CoverContinuousSpaceUpTo7680x5760_Test()
    {
        // Arrange - Lưới 4x3 chuẩn gồm 12 màn hình
        var screens = new List<StartScreenPreset>();
        var id = 1;
        for (var row = 1; row <= 3; row++)
        {
            for (var col = 1; col <= 4; col++)
            {
                var x = (col - 1) * VwDirectDeviceConstants.UniformTileSize;
                var y = (row - 1) * VwDirectDeviceConstants.UniformTileSize;
                screens.Add(new StartScreenPreset($"Màn {id}", x, y, id, row, col));
                id++;
            }
        }

        // Act & Assert - Kiểm tra từng ô tạo nên dải tọa độ liên tục không bị hở hay chồng chéo
        var maxEndX = 0;
        var maxEndY = 0;

        foreach (var scr in screens)
        {
            var endX = scr.X + VwDirectDeviceConstants.UniformTileSize;
            var endY = scr.Y + VwDirectDeviceConstants.UniformTileSize;
            var rangeText = VisualWallCanvas.FormatScreenCoordinateRange(scr.X, scr.Y);

            Assert.Equal($"X: {scr.X} ➔ {endX}\nY: {scr.Y} ➔ {endY}", rangeText);

            if (endX > maxEndX)
                maxEndX = endX;
            if (endY > maxEndY)
                maxEndY = endY;
        }

        // Assert - Tọa độ mép ngoài cùng của lưới 4x3 đạt đúng 7680 x 5760
        Assert.Equal(7680, maxEndX);
        Assert.Equal(5760, maxEndY);
    }

    [Fact]
    public void SceneWindowRow_DragMoveAndResize_UpdatesCoordinatesAndNotifiesProperties_Test()
    {
        // Arrange
        var screens = new List<StartScreenPreset>
        {
            new("Màn 1", 0, 0, 1, 1, 1),
            new("Màn 2", 1920, 0, 2, 1, 2),
            new("Màn 5", 0, 1920, 5, 2, 1),
            new("Màn 6", 1920, 1920, 6, 2, 2),
        };
        SceneWindowRow.StartScreenProvider = () => screens;

        var dto = new Module.VideoWall.WPF.Api.Dto.VwWindowSceneDto
        {
            ID = Guid.NewGuid().ToString("N"),
            Name = "Ô 1",
            X = 0,
            Y = 0,
            W = 1920,
            H = 1920,
            ZIndex = 1,
        };
        var row = new SceneWindowRow(dto);

        var changedProperties = new List<string>();
        row.PropertyChanged += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.PropertyName))
                changedProperties.Add(e.PropertyName);
        };

        // Assert trạng thái ban đầu
        Assert.Equal("Màn 1", row.SelectedStartScreen?.Name);
        Assert.Equal("1x1", row.SelectedSizePreset?.Name);

        // Act 1: Kéo thả di chuyển ô sang Màn 6 (X: 1920, Y: 1920)
        row.X = 1920;
        row.Y = 1920;

        // Assert 1: Tọa độ cập nhật và bắn event cho UI
        Assert.Equal(1920, row.X);
        Assert.Equal(1920, row.Y);
        Assert.Contains(nameof(SceneWindowRow.X), changedProperties);
        Assert.Contains(nameof(SceneWindowRow.Y), changedProperties);
        Assert.Contains(nameof(SceneWindowRow.SelectedStartScreen), changedProperties);
        Assert.Equal("Màn 6", row.SelectedStartScreen?.Name);

        // Act 2: Co giãn kích thước ô lên khối 2x2 (3840 x 3840)
        changedProperties.Clear();
        row.W = 3840;
        row.H = 3840;

        // Assert 2: Kích thước cập nhật và bắn event cho UI
        Assert.Equal(3840, row.W);
        Assert.Equal(3840, row.H);
        Assert.Contains(nameof(SceneWindowRow.W), changedProperties);
        Assert.Contains(nameof(SceneWindowRow.H), changedProperties);
        Assert.Contains(nameof(SceneWindowRow.SelectedSizePreset), changedProperties);
        Assert.Equal("2x2", row.SelectedSizePreset?.Name);
        Assert.Equal("Khối lớn (2x2)", row.SizeLabel);

        // Act 3: Kéo thả tự do không khớp lưới chuẩn (ví dụ: X=500, Y=300)
        row.X = 500;
        row.Y = 300;
        Assert.Equal(500, row.X);
        Assert.Equal(300, row.Y);
        Assert.Equal("Tùy chỉnh (500, 300)", row.SelectedStartScreen?.Name);

        // Act 4: Co giãn resize tự do theo pixel lẻ (ví dụ: W=5760, H=5181)
        row.W = 5760;
        row.H = 5181;
        Assert.Equal(5760, row.W);
        Assert.Equal(5181, row.H);
        Assert.Null(row.SelectedSizePreset);
        Assert.Equal("5760 × 5181", row.SizeLabel);
    }

    [Fact]
    public void SceneSetupViewModel_CanPushToDevice_RequiresProbeResultAndWallNo_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = 18080,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!"
        };
        var sceneSetup = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub);

        Assert.False(sceneSetup.PushToDeviceCommand.CanExecute(null));

        sceneSetup.CurrentScene = new Module.VideoWall.WPF.Api.Dto.VwSceneDto
        {
            ID = "scn-1",
            Code = "SCN_01",
            Name = "Test Scene"
        };
        Assert.NotNull(sceneSetup.CurrentScene);

        Assert.False(sceneSetup.PushToDeviceCommand.CanExecute(null));

        // Set ProbeResult nhưng không có tường (Walls rỗng) => WallNo vẫn null => CanExecute = false
        connection.ProbeResult = new Module.VideoWall.WPF.Api.Dto.VwProbeDeviceOutput
        {
            Reachable = true,
            Walls = []
        };
        Assert.False(sceneSetup.PushToDeviceCommand.CanExecute(null));

        // Khi ProbeResult có danh sách tường => tự động gán WallNo = 1 => CanExecute = true
        connection.ProbeResult = new Module.VideoWall.WPF.Api.Dto.VwProbeDeviceOutput
        {
            Reachable = true,
            Walls = [new() { Id = 1, Name = "Wall 1" }]
        };
        Assert.Equal(1, connection.WallNo);
        Assert.True(sceneSetup.PushToDeviceCommand.CanExecute(null));

        sceneSetup.IsBusy = true;
        Assert.False(sceneSetup.PushToDeviceCommand.CanExecute(null));

        sceneSetup.IsBusy = false;
        Assert.True(sceneSetup.PushToDeviceCommand.CanExecute(null));
    }

    [Fact]
    public async Task SceneSetupViewModel_SaveAllSceneWindowsCommand_CanExecute_EnablesWhenWindowsExist_AndUpdatesOnRowChange_Test()
    {
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            AdHocPort = 18188,
            AdHocAccount = "admin",
            AdHocPassword = "Password123!",
            WallNo = 1,
        };
        VwLocalSceneStore.SeedSampleScenes(connection.DeviceKey, 1);
        var sceneSetup = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub)
        {
            WallNo = 1,
        };
        sceneSetup.ReloadScenesForCurrentWall();

        // Ban đầu khi có Scene và Windows -> CanExecute của Lưu cấu hình phải là true
        Assert.NotNull(sceneSetup.CurrentScene);
        Assert.True(sceneSetup.SceneWindows.Count > 0);
        Assert.True(sceneSetup.SaveAllSceneWindowsCommand.CanExecute(null));

        // Người dùng chọn màn hình bắt đầu khác (vd từ Màn 1 sang Màn 2)
        var firstRow = sceneSetup.SceneWindows[0];
        firstRow.SelectedStartScreen = new StartScreenPreset("Màn 2 (0, 1)", 1920, 0, 2, 0, 1);
        Assert.Equal(1920, firstRow.X);
        Assert.Equal(0, firstRow.Y);

        // Nút Lưu cấu hình vẫn luôn sẵn sàng (CanExecute = true)
        Assert.True(sceneSetup.SaveAllSceneWindowsCommand.CanExecute(null));

        // Thực hiện lưu cấu hình
        await sceneSetup.SaveAllSceneWindowsCommand.ExecuteAsync(null);
        Assert.Contains("thành công", sceneSetup.StatusMessage);

        // Khi IsBusy = true -> CanExecute = false
        sceneSetup.IsBusy = true;
        Assert.False(sceneSetup.SaveAllSceneWindowsCommand.CanExecute(null));

        // Khi IsBusy = false -> CanExecute = true trở lại
        sceneSetup.IsBusy = false;
        Assert.True(sceneSetup.SaveAllSceneWindowsCommand.CanExecute(null));

        // Khi CurrentScene = null -> CanExecute = false
        sceneSetup.CurrentScene = null;
        Assert.False(sceneSetup.SaveAllSceneWindowsCommand.CanExecute(null));
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
            WallNo = 1,
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
        Assert.Contains("<WallScene", scenePutPreset.SampleBody);
        Assert.Contains("<name>", scenePutPreset.SampleBody);
        Assert.DoesNotContain("<Scene>", scenePutPreset.SampleBody);

        var planPostPreset = Module.VideoWall.WPF.ViewModels.VwIsapiPresetList.Presets.First(p => p.Section == "9.7.6.1");
        Assert.NotNull(planPostPreset.SampleBody);
        Assert.Contains("<WallPlan", planPostPreset.SampleBody);
        Assert.Contains("<ActTimeDetail>", planPostPreset.SampleBody);
        Assert.Contains("<PlanDetailList>", planPostPreset.SampleBody);
        Assert.Contains("<operationType>activateScene</operationType>", planPostPreset.SampleBody);
        Assert.Contains("<sceneID>1</sceneID>", planPostPreset.SampleBody);
        Assert.Contains("<actCount>1</actCount>", planPostPreset.SampleBody);
        Assert.DoesNotContain("<enabled>", planPostPreset.SampleBody);
        Assert.DoesNotContain("<planTemplateList>", planPostPreset.SampleBody);

        var screenClosePreset = Module.VideoWall.WPF.ViewModels.VwIsapiPresetList.Presets.First(p => p.Section == "9.7.8.1");
        Assert.NotNull(screenClosePreset.SampleBody);
        Assert.Contains("<ScreenCtrl", screenClosePreset.SampleBody);
        Assert.Contains("<OutputID>", screenClosePreset.SampleBody);
        Assert.DoesNotContain("<action>", screenClosePreset.SampleBody);

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

        // Tab 1: Thiết lập Scene & Bố cục (Index 0) -> Không hiện Response
        mainVm.SelectedTabIndex = 0;
        Assert.False(mainVm.IsResponseVisible);

        // Tab 2: Bo mạch (Index 1) -> Hiện Response
        mainVm.SelectedTabIndex = 1;
        Assert.True(mainVm.IsResponseVisible);

        // Tab 6: Video Wall (Index 5) -> Hiện Response
        mainVm.SelectedTabIndex = 5;
        Assert.True(mainVm.IsResponseVisible);

        // Tab 12: Cửa sổ (Index 11) -> Hiện Response
        mainVm.SelectedTabIndex = 11;
        Assert.True(mainVm.IsResponseVisible);
    }

    [Fact]
    public void MainViewModel_FormatActivitiesAsJsonl_OutputsValidNewlineDelimitedJson_Test()
    {
        var now = DateTime.Now;
        var activities = new List<EventTriggerLogEntry>
        {
            new(new Activity(now, "Khảo sát", "Bắt đầu khảo sát thiết bị", ActivityLevel.Info)),
            new(
                new Activity(now.AddSeconds(1), "ISAPI", "Gọi GET /ISAPI/System/deviceInfo", ActivityLevel.Success),
                new VwSetupSceneStep
                {
                    Order = 1,
                    Name = "GetDeviceInfo",
                    Method = "GET",
                    Endpoint = "ISAPI/System/deviceInfo",
                    HttpStatus = 200,
                    Success = true,
                    Message = "200 OK",
                    RequestXml = null,
                    ResponseXml = """
                        <DeviceInfo version="2.0">
                          <deviceName>DS-C30S-S11</deviceName>
                          <model>DS-C30S-S11</model>
                        </DeviceInfo>
                        """,
                }
            ),
            new(
                new Activity(now.AddSeconds(2), "Window", "Tạo cửa sổ mới", ActivityLevel.Success),
                new VwSetupSceneStep
                {
                    Order = 2,
                    Name = "AddWindow",
                    Method = "POST",
                    Endpoint = "ISAPI/DisplayDev/VideoWall/1/windows",
                    HttpStatus = 200,
                    Success = true,
                    Message = "OK",
                    RequestXml = """
                        <WallWindow version="2.0">
                          <id>1</id>
                          <Rect><x>0</x><y>0</y><width>1920</width><height>1920</height></Rect>
                        </WallWindow>
                        """,
                    ResponseXml = "<ResponseStatus><statusCode>1</statusCode><statusString>OK</statusString></ResponseStatus>",
                }
            ),
        };

        // Act
        var jsonlOutput = MainViewModel.FormatActivitiesAsJsonl(activities);

        // Assert 1: Output is NOT a JSON array (no leading [ or trailing ])
        var trimmed = jsonlOutput.Trim();
        Assert.False(trimmed.StartsWith("["));
        Assert.False(trimmed.EndsWith("]"));

        // Assert 2: Exactly 3 non-empty lines (one line per activity)
        var lines = jsonlOutput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);

        // Assert 3: Each individual line is a valid, independently parseable JSON object
        for (var i = 0; i < lines.Length; i++)
        {
            using var doc = JsonDocument.Parse(lines[i]);
            var root = doc.RootElement;
            Assert.Equal(JsonValueKind.Object, root.ValueKind);
            Assert.True(root.TryGetProperty("Time", out _));
            Assert.True(root.TryGetProperty("Stage", out var stageProp));
            Assert.True(root.TryGetProperty("Level", out var levelProp));
            Assert.True(root.TryGetProperty("Detail", out _));

            if (i == 1)
            {
                Assert.Equal("ISAPI", stageProp.GetString());
                Assert.Equal("GET", root.GetProperty("Method").GetString());
                Assert.Equal("ISAPI/System/deviceInfo", root.GetProperty("Endpoint").GetString());
                Assert.Equal(200, root.GetProperty("HttpStatus").GetInt32());
                Assert.True(root.GetProperty("Success").GetBoolean());
                // Newline in XML payload must be escaped, NOT breaking the single line
                Assert.Contains("DS-C30S-S11", root.GetProperty("ResponsePayload").GetString());
            }
            else if (i == 2)
            {
                Assert.Equal("Window", stageProp.GetString());
                Assert.Equal("POST", root.GetProperty("Method").GetString());
                Assert.Contains("WallWindow", root.GetProperty("RequestPayload").GetString());
            }
        }
    }

    [Fact]
    public void MainViewModel_FormatActivitiesAsJson_OutputsValidJsonArray_Test()
    {
        var now = DateTime.Now;
        var activities = new List<EventTriggerLogEntry>
        {
            new(new Activity(now, "Init", "Ứng dụng khởi động", ActivityLevel.Info)),
            new(
                new Activity(now.AddSeconds(1), "ISAPI", "GET ISAPI/System/deviceInfo thành công", ActivityLevel.Success),
                new VwSetupSceneStep
                {
                    Order = 1,
                    Name = "DeviceInfo",
                    Method = "GET",
                    Endpoint = "ISAPI/System/deviceInfo",
                    HttpStatus = 200,
                    Success = true,
                    Message = "OK",
                    ResponseXml = "<DeviceInfo><model>DS-C30S-S11</model></DeviceInfo>",
                }
            ),
        };

        var jsonOutput = MainViewModel.FormatActivitiesAsJson(activities);
        using var doc = JsonDocument.Parse(jsonOutput);
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(2, root.GetArrayLength());
        Assert.Equal("Init", root[0].GetProperty("Stage").GetString());
        Assert.Equal("ISAPI", root[1].GetProperty("Stage").GetString());
    }

    [Fact]
    public void MainViewModel_AppendSessionLogRecord_MaintainsValidJsonArrayForPrettier_Test()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_session_{Guid.NewGuid():N}.json");

        try
        {
            var record1 = JsonSerializer.Serialize(new
            {
                Time = "2026-09-03 08:00:00.000",
                Stage = "Direct",
                Method = "GET",
                Endpoint = "ISAPI/System/deviceInfo",
                HttpStatus = 200,
                Success = true,
            });

            var record2 = JsonSerializer.Serialize(new
            {
                Time = "2026-09-03 08:00:01.000",
                Stage = "Direct",
                Method = "POST",
                Endpoint = "ISAPI/DisplayDev/VideoWall/v20/walls/1/plans",
                HttpStatus = 200,
                Success = true,
            });

            var record3 = JsonSerializer.Serialize(new
            {
                Time = "2026-09-03 08:00:02.000",
                Stage = "Direct",
                Method = "PUT",
                Endpoint = "ISAPI/DisplayDev/VideoWall/v20/walls/1/scenes/1",
                HttpStatus = 200,
                Success = true,
            });

            // Act 1: Ghi bản ghi đầu tiên vào file chưa tồn tại
            MainViewModel.AppendSessionLogRecord(tempFile, record1);

            // Assert 1: File sinh ra là JSON Array hợp lệ với 1 phần tử
            var json1 = File.ReadAllText(tempFile, Encoding.UTF8);
            using (var doc1 = JsonDocument.Parse(json1))
            {
                Assert.Equal(JsonValueKind.Array, doc1.RootElement.ValueKind);
                Assert.Equal(1, doc1.RootElement.GetArrayLength());
                Assert.Equal("GET", doc1.RootElement[0].GetProperty("Method").GetString());
            }

            // Act 2: Nối tiếp bản ghi thứ hai vào file đang có
            MainViewModel.AppendSessionLogRecord(tempFile, record2);

            // Assert 2: File tiếp tục là JSON Array hợp lệ với 2 phần tử
            var json2 = File.ReadAllText(tempFile, Encoding.UTF8);
            using (var doc2 = JsonDocument.Parse(json2))
            {
                Assert.Equal(JsonValueKind.Array, doc2.RootElement.ValueKind);
                Assert.Equal(2, doc2.RootElement.GetArrayLength());
                Assert.Equal("GET", doc2.RootElement[0].GetProperty("Method").GetString());
                Assert.Equal("POST", doc2.RootElement[1].GetProperty("Method").GetString());
            }

            // Act 3: Giả lập người dùng chạy Prettier trong VS Code làm file có thụt dòng đẹp (pretty-printed)
            var prettyJson = JsonSerializer.Serialize(
                JsonSerializer.Deserialize<JsonElement>(json2),
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tempFile, prettyJson, Encoding.UTF8);

            // Nối tiếp bản ghi thứ ba sau khi đã bị Prettier format
            MainViewModel.AppendSessionLogRecord(tempFile, record3);

            // Assert 3: File sau khi bị Prettier format vẫn duy trì cấu trúc JSON Array hợp lệ với 3 phần tử
            var json3 = File.ReadAllText(tempFile, Encoding.UTF8);
            using (var doc3 = JsonDocument.Parse(json3))
            {
                Assert.Equal(JsonValueKind.Array, doc3.RootElement.ValueKind);
                Assert.Equal(3, doc3.RootElement.GetArrayLength());
                Assert.Equal("PUT", doc3.RootElement[2].GetProperty("Method").GetString());
            }

            // Act 4: Kiểm tra khả năng tự động di chuyển (migration) từ định dạng cũ (JSONL từng dòng không có ngoặc [])
            var legacyJsonlFile = Path.Combine(Path.GetTempPath(), $"test_legacy_{Guid.NewGuid():N}.json");
            try
            {
                File.WriteAllText(legacyJsonlFile, record1 + Environment.NewLine + record2 + Environment.NewLine, Encoding.UTF8);

                // Nối tiếp bản ghi mới vào file dạng legacy
                MainViewModel.AppendSessionLogRecord(legacyJsonlFile, record3);

                var migratedJson = File.ReadAllText(legacyJsonlFile, Encoding.UTF8);
                using var docMigrated = JsonDocument.Parse(migratedJson);
                Assert.Equal(JsonValueKind.Array, docMigrated.RootElement.ValueKind);
                Assert.Equal(3, docMigrated.RootElement.GetArrayLength());
                Assert.Equal("GET", docMigrated.RootElement[0].GetProperty("Method").GetString());
                Assert.Equal("POST", docMigrated.RootElement[1].GetProperty("Method").GetString());
                Assert.Equal("PUT", docMigrated.RootElement[2].GetProperty("Method").GetString());
            }
            finally
            {
                if (File.Exists(legacyJsonlFile))
                    File.Delete(legacyJsonlFile);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void SceneSetupViewModel_EditingAdHocIp_DoesNotResetOrOverwriteCurrentScene_Test()
    {
        // Arrange
        var recordingPub = new RecordingPublisherTest();
        var activityPub = new ActivityPublisher(recordingPub, NullLogger<ActivityPublisher>.Instance);
        var connection = new ConnectionViewModel(activityPub, recordingPub, new UserConfirmationTest(true))
        {
            AdHocIp = "127.0.0.1",
            WallNo = 1,
        };
        var sceneSetup = new SceneSetupViewModel(activityPub, connection, new UserConfirmationTest(true), recordingPub);

        var customScene = new Module.VideoWall.WPF.Api.Dto.VwSceneDto
        {
            ID = "custom_scene_01",
            Name = "Kịch bản tùy chỉnh đặc biệt của người dùng",
        };
        sceneSetup.Scenes.Add(customScene);
        sceneSetup.CurrentScene = customScene;

        // Act: Người dùng gõ hoặc copy-paste IP mới vào ô AdHocIp
        connection.AdHocIp = "10.10.8.113";

        // Assert: Kịch bản đang chọn không bị tự ý đổi hay ghi đè
        Assert.NotNull(sceneSetup.CurrentScene);
        Assert.Equal("custom_scene_01", sceneSetup.CurrentScene.ID);
        Assert.Equal("Kịch bản tùy chỉnh đặc biệt của người dùng", sceneSetup.CurrentScene.Name);
    }

    [Fact]
    public async Task VwISAPIMockServerHikvision_Start_ConnectingViaLocalLanIp_10_10_8_113_Succeeds_Test()
    {
        // Arrange
        const int port = 18098;
        using var mockServer = new VwISAPIMockServerHikvision();

        // Act: Start mock server
        mockServer.Start(port);

        // Assert: Kết nối trực tiếp bằng IP card mạng LAN của máy cục bộ phải thành công 100%
        var localLanIp = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(a.Address))
            ?.Address.ToString() ?? "127.0.0.1";
        var creds = new VwDirectDeviceCredentials(localLanIp, port, "admin", "Password123!");
        var client = VwDirectClientFactory.CreateISAPIClient(creds);

        var userCheckRes = await client.UserCheck(CancellationToken.None);
        Assert.True(userCheckRes.Success);
        Assert.Equal(System.Net.HttpStatusCode.OK, userCheckRes.HttpStatusCode);

        var capsRes = await client.GetCapabilities(CancellationToken.None);
        Assert.True(capsRes.Success);
        Assert.Equal(System.Net.HttpStatusCode.OK, capsRes.HttpStatusCode);
        Assert.NotNull(capsRes.Data);
        Assert.Equal(512, capsRes.Data.MaxWindowNums);
    }

    [Fact]
    public void VisualWallCanvas_CalculateCardZIndex_HigherLayerAlwaysAboveLowerLayer_Test()
    {
        var layer1SelectedZ = VisualWallCanvas.CalculateCardZIndex(1, isSelected: true, orderNo: 1);
        var layer2UnselectedZ = VisualWallCanvas.CalculateCardZIndex(2, isSelected: false, orderNo: 1);
        Assert.True(layer2UnselectedZ > layer1SelectedZ);

        var layer2SelectedZ = VisualWallCanvas.CalculateCardZIndex(2, isSelected: true, orderNo: 1);
        Assert.True(layer2SelectedZ > layer2UnselectedZ);

        var layer1Order1 = VisualWallCanvas.CalculateCardZIndex(1, isSelected: false, orderNo: 1);
        var layer1Order2 = VisualWallCanvas.CalculateCardZIndex(1, isSelected: false, orderNo: 2);
        Assert.True(layer1Order2 > layer1Order1);

        var nullZ = VisualWallCanvas.CalculateCardZIndex(null, isSelected: false, orderNo: 1);
        var zeroZ = VisualWallCanvas.CalculateCardZIndex(0, isSelected: false, orderNo: 1);
        Assert.Equal(layer1Order1, nullZ);
        Assert.Equal(layer1Order1, zeroZ);
    }

    [Fact]
    public async Task VwDirectSetupSceneOrchestrator_AddWindow_OrdersByZIndexAscending_Test()
    {
        const int port = 18210;
        using var mockServer = new VwISAPIMockServerHikvision();
        mockServer.Start(port);

        var creds = new VwDirectDeviceCredentials("127.0.0.1", port, "admin", "12345");
        var orchestrator = VwDirectClientFactory.CreateSetupSceneOrchestrator(creds);

        var input = new VwDirectPushSceneInput
        {
            SceneId = 1,
            WallNo = 1,
            DryRun = true,
            Windows =
            [
                new VwDirectWindowInput
                {
                    Label = "Window Layer 3",
                    ZIndex = 3,
                    X = 0,
                    Y = 0,
                    W = 1920,
                    H = 1080,
                    SignalNo = 3,
                },
                new VwDirectWindowInput
                {
                    Label = "Window Layer 1",
                    ZIndex = 1,
                    X = 1920,
                    Y = 0,
                    W = 1920,
                    H = 1080,
                    SignalNo = 1,
                },
                new VwDirectWindowInput
                {
                    Label = "Window Layer 2",
                    ZIndex = 2,
                    X = 3840,
                    Y = 0,
                    W = 1920,
                    H = 1080,
                    SignalNo = 2,
                },
            ],
        };

        var result = await orchestrator.Execute(input, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(3, result.Windows.Count);
        Assert.Equal("Window Layer 1", result.Windows[0].Label);
        Assert.Equal(1, result.Windows[0].ZIndex);
        Assert.Equal("Window Layer 2", result.Windows[1].Label);
        Assert.Equal(2, result.Windows[1].ZIndex);
        Assert.Equal("Window Layer 3", result.Windows[2].Label);
        Assert.Equal(3, result.Windows[2].ZIndex);
    }

    private sealed class FaultyHttpMessageHandlerTest(Exception exceptionToThrow) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(exceptionToThrow);
        }
    }
}
