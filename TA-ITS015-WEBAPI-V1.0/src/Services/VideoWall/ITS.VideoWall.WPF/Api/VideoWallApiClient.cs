using MediatR;
using Services.Shared.Events;
using Module.VideoWall.WPF.Api.Dto;
using Shared.Utility.Utilities;

namespace Module.VideoWall.WPF.Api;

/// <summary>
/// Author: Đạt
/// Description: Một hàm cho mỗi endpoint Video Wall của TAC_WebAPI. Toàn bộ nghiệp vụ thiết bị
/// (ISAPI digest auth, quy đổi toạ độ, thứ tự lệnh) nằm ở backend — client CHỈ gọi API, tuyệt đối
/// không nói chuyện trực tiếp với bộ điều khiển.
///
/// Đường dẫn viết thường theo đúng route mà client Vue đang chạy thật sinh ra
/// (api-services/videoWall/apis/*.ts) — ASP.NET Core khớp route không phân biệt hoa thường,
/// nhưng cứ theo đúng bản đã kiểm chứng.
/// Created date: 25/08/2026
/// </summary>
public sealed class VideoWallApiClient(
    ApiInvoker apiInvoker,
    IPublisher publisher,
    ActivityPublisher activityPublisher)
{
    private const string Base = "/api/videowall";
    private const string StageController = "Controller";
    private const string StageScreen = "Screen";
    private const string StageOutput = "ControllerOutput";
    private const string StageSource = "Source";
    private const string StageScene = "Scene";
    private const string StageWindow = "Window";
    private const string StageSchedule = "Schedule";
    private const string StageDevice = "Device";

    // ════════════════════════════════════════════════════════════════════════
    // Bộ điều khiển / màn hình / cổng / nguồn tín hiệu — chỉ đọc trong công cụ này.
    // ════════════════════════════════════════════════════════════════════════

    public async Task<List<VwControllerDto>> ListControllers(CancellationToken cancellationToken)
    {
        var filter = new VwControllerFilter();
        var result = await apiInvoker
            .GetAsync<List<VwControllerDto>>(ApiInvoker.VideoWallClient, $"{Base}/vwcontroller/list", filter, StageController, cancellationToken)
            .ConfigureAwait(false);

        return result ?? [];
    }

    public async Task<List<VwScreenDto>> ListScreens(string controllerId, CancellationToken cancellationToken)
    {
        var filter = new VwScreenFilter { ControllerId = controllerId };
        var result = await apiInvoker
            .GetAsync<List<VwScreenDto>>(ApiInvoker.VideoWallClient, $"{Base}/vwscreen/list", filter, StageScreen, cancellationToken)
            .ConfigureAwait(false);

        return result ?? [];
    }

    /// <summary>Cổng ra của bộ điều khiển (đồng bộ từ thiết bị) kèm màn đang cắm — thay cho VwSlotPort đã bỏ.</summary>
    public async Task<List<VwControllerOutputDto>> ListControllerOutputs(string? controllerId, CancellationToken cancellationToken)
    {
        var filter = new VwControllerOutputFilter { ControllerId = controllerId };
        var result = await apiInvoker
            .GetAsync<List<VwControllerOutputDto>>(ApiInvoker.VideoWallClient, $"{Base}/vwcontrolleroutput/list", filter, StageOutput, cancellationToken)
            .ConfigureAwait(false);

        return result ?? [];
    }

    public async Task<List<VwSourceDto>> ListSources(string? controllerId, CancellationToken cancellationToken)
    {
        var filter = new VwSourceFilter { ControllerId = controllerId };
        var result = await apiInvoker
            .GetAsync<List<VwSourceDto>>(ApiInvoker.VideoWallClient, $"{Base}/vwsource/list", filter, StageSource, cancellationToken)
            .ConfigureAwait(false);

        return result ?? [];
    }

    // ════════════════════════════════════════════════════════════════════════
    // Kịch bản hiển thị.
    // ════════════════════════════════════════════════════════════════════════

    public async Task<List<VwSceneDto>> ListScenes(VwSceneFilter filter, CancellationToken cancellationToken)
    {
        var result = await apiInvoker
            .GetAsync<List<VwSceneDto>>(ApiInvoker.VideoWallClient, $"{Base}/vwscene/list", filter, StageScene, cancellationToken)
            .ConfigureAwait(false);

        return result ?? [];
    }

    public Task AddScene(VwSceneDto scene, CancellationToken cancellationToken) =>
        apiInvoker.PostAsync(ApiInvoker.VideoWallClient, $"{Base}/vwscene/add", scene, StageScene, cancellationToken);

    public Task UpdateScene(VwSceneDto scene, CancellationToken cancellationToken) =>
        apiInvoker.PostAsync(ApiInvoker.VideoWallClient, $"{Base}/vwscene/update", scene, StageScene, cancellationToken);

    public Task DeleteScene(string sceneId, CancellationToken cancellationToken) =>
        apiInvoker.PostAsync(ApiInvoker.VideoWallClient, $"{Base}/vwscene/delete", new VwIdInput { ID = sceneId }, StageScene, cancellationToken);

    /// <summary>Kích hoạt kịch bản theo Code (POST). Cùng đường dẫn với GetActiveScene nhưng khác verb.</summary>
    public Task<VwActiveSceneOutput?> ActivateScene(string sceneCode, CancellationToken cancellationToken) =>
        apiInvoker.PostAsync<VwActiveSceneOutput>(
            ApiInvoker.VideoWallClient,
            $"{Base}/vwscene/active",
            new VwActiveSceneInput { Code = sceneCode },
            StageScene,
            cancellationToken);

    /// <summary>Kích hoạt kịch bản theo EventTypeId, dùng để kiểm tra độ ưu tiên của VwEventRule khi có sự kiện kích hoạt.</summary>
    public Task<VwActiveSceneOutput?> ActivateSceneByEvent(string eventTypeId, CancellationToken cancellationToken) =>
        apiInvoker.PostAsync<VwActiveSceneOutput>(
            ApiInvoker.VideoWallClient,
            $"{Base}/vwscene/active",
            new VwActiveSceneInput { EventTypeId = eventTypeId },
            StageScene,
            cancellationToken);

    /// <summary>Đọc kịch bản đang chiếu (GET) — lệnh chỉ-đọc, không đổi hình trên tường.</summary>
    public Task<VwSceneDto?> GetActiveScene(CancellationToken cancellationToken) =>
        apiInvoker.GetAsync<VwSceneDto>(ApiInvoker.VideoWallClient, $"{Base}/vwscene/active", query: null, StageScene, cancellationToken);

    /// <summary>Lấy kịch bản đang chiếu của từng vùng màn hình.</summary>
    public async Task<List<VwActiveRegionOutput>> GetActiveRegions(CancellationToken cancellationToken)
    {
        var result = await apiInvoker
            .GetAsync<List<VwActiveRegionOutput>>(ApiInvoker.VideoWallClient, $"{Base}/vwscene/activeregions", query: null, StageScene, cancellationToken)
            .ConfigureAwait(false);

        return result ?? [];
    }

    /// <summary>
    /// Tra ID của kịch bản theo Code. Cần vì endpoint Add trả về void và bộ lọc List KHÔNG có
    /// tham số Code — nên lọc theo Name rồi tự đối chiếu Code phía client.
    /// </summary>
    public async Task<VwSceneDto?> FindSceneByCode(string sceneCode, string? sceneName, CancellationToken cancellationToken)
    {
        var filter = new VwSceneFilter { Name = sceneName };
        var scenes = await ListScenes(filter, cancellationToken).ConfigureAwait(false);

        return scenes.FirstOrDefault(scene => string.Equals(scene.Code, sceneCode, StringComparison.Ordinal));
    }

    // ════════════════════════════════════════════════════════════════════════
    // Cửa sổ trong kịch bản.
    // ════════════════════════════════════════════════════════════════════════

    public async Task<List<VwWindowSceneDto>> ListWindowScenes(string sceneId, CancellationToken cancellationToken)
    {
        var filter = new VwWindowSceneFilter { SceneId = sceneId };
        var result = await apiInvoker
            .GetAsync<List<VwWindowSceneDto>>(ApiInvoker.VideoWallClient, $"{Base}/vwwindowscene/list", filter, StageWindow, cancellationToken)
            .ConfigureAwait(false);

        return result ?? [];
    }

    public Task AddWindowScene(VwWindowSceneDto window, CancellationToken cancellationToken) =>
        apiInvoker.PostAsync(ApiInvoker.VideoWallClient, $"{Base}/vwwindowscene/add", window, StageWindow, cancellationToken);

    public Task UpdateWindowScene(VwWindowSceneDto window, CancellationToken cancellationToken) =>
        apiInvoker.PostAsync(ApiInvoker.VideoWallClient, $"{Base}/vwwindowscene/update", window, StageWindow, cancellationToken);

    public Task DeleteWindowScene(string windowSceneId, CancellationToken cancellationToken) =>
        apiInvoker.PostAsync(ApiInvoker.VideoWallClient, $"{Base}/vwwindowscene/delete", new VwIdInput { ID = windowSceneId }, StageWindow, cancellationToken);

    // ════════════════════════════════════════════════════════════════════════
    // Lịch bật/tắt kịch bản.
    // ════════════════════════════════════════════════════════════════════════

    public async Task<PagedResult<VwScheduleDto>> PageSchedules(VwSchedulePageInput input, CancellationToken cancellationToken)
    {
        var result = await apiInvoker
            .PostAsync<PagedResult<VwScheduleDto>>(ApiInvoker.VideoWallClient, $"{Base}/vwschedule/page", input, StageSchedule, cancellationToken)
            .ConfigureAwait(false);

        return result ?? new PagedResult<VwScheduleDto>();
    }

    public Task AddSchedule(VwScheduleDto schedule, CancellationToken cancellationToken) =>
        apiInvoker.PostAsync(ApiInvoker.VideoWallClient, $"{Base}/vwschedule/add", schedule, StageSchedule, cancellationToken);

    public Task UpdateSchedule(VwScheduleDto schedule, CancellationToken cancellationToken) =>
        apiInvoker.PostAsync(ApiInvoker.VideoWallClient, $"{Base}/vwschedule/update", schedule, StageSchedule, cancellationToken);

    public Task DeleteSchedule(string scheduleId, CancellationToken cancellationToken) =>
        apiInvoker.PostAsync(ApiInvoker.VideoWallClient, $"{Base}/vwschedule/delete", new VwIdInput { ID = scheduleId }, StageSchedule, cancellationToken);

    // ════════════════════════════════════════════════════════════════════════
    // Đấu nối thiết bị (VwDeviceSetupController). Mỗi bước ISAPI được phát thành một dòng log
    // riêng mang theo RequestXml/ResponseXml để mở hộp thoại chi tiết.
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Xác nhận Digest Auth vào bộ điều khiển. Bắt buộc ControllerId có trong CSDL.</summary>
    public async Task<VwSetupSceneStep> Ping(string controllerId, CancellationToken cancellationToken)
    {
        var step = await apiInvoker
            .PostAsync<VwSetupSceneStep>(ApiInvoker.VideoWallClient, $"{Base}/vwdevicesetup/ping", new VwIdInput { ID = controllerId }, StageDevice, cancellationToken, suppressSuccessActivity: true)
            .ConfigureAwait(false)
            ?? throw new ApiException("Ping không trả về bước thực thi nào.");

        await PublishStep("Ping", step, cancellationToken).ConfigureAwait(false);
        return step;
    }

    /// <summary>Khảo sát thiết bị bằng lệnh chỉ-đọc — an toàn để gọi bất cứ lúc nào.</summary>
    public async Task<VwProbeDeviceOutput> Probe(string controllerId, int? wallNo, CancellationToken cancellationToken)
    {
        var input = new VwProbeDeviceInput
        {
            ID = controllerId,
            WallNo = wallNo,
        };

        var output = await apiInvoker
            .PostAsync<VwProbeDeviceOutput>(ApiInvoker.VideoWallClient, $"{Base}/vwdevicesetup/probe", input, StageDevice, cancellationToken, suppressSuccessActivity: true)
            .ConfigureAwait(false)
            ?? throw new ApiException("Probe không trả về dữ liệu khảo sát.");

        await PublishSteps("Probe", output.Steps, cancellationToken).ConfigureAwait(false);
        return output;
    }

    /// <summary>
    /// Chạy kịch bản dựng cảnh xuống thiết bị. DryRun = false là lượt gọi DUY NHẤT làm đổi hình
    /// đang chiếu — phía UI phải hỏi lại người vận hành trước khi gửi.
    /// </summary>
    public async Task<VwSetupSceneOutput> SetupScene(VwSetupSceneInput input, CancellationToken cancellationToken)
    {
        var output = await apiInvoker
            .PostAsync<VwSetupSceneOutput>(ApiInvoker.VideoWallClient, $"{Base}/vwdevicesetup/setupscene", input, StageDevice, cancellationToken, suppressSuccessActivity: true)
            .ConfigureAwait(false)
            ?? throw new ApiException("SetupScene không trả về kết quả.");

        await PublishSteps("SetupScene", output.Steps, cancellationToken).ConfigureAwait(false);
        return output;
    }

    /// <summary>
    /// Chuyển tiếp ISAPI thô. Đây là lối duy nhất dùng được thiết bị ad-hoc (Ip/Port/Account/Password)
    /// chưa khai báo trong CSDL — Ping/Probe/SetupScene đều đòi ControllerId.
    /// </summary>
    public async Task<VwSetupSceneStep> Isapi(VwISAPIPassthroughInput input, CancellationToken cancellationToken)
    {
        var step = await apiInvoker
            .PostAsync<VwSetupSceneStep>(ApiInvoker.VideoWallClient, $"{Base}/vwdevicesetup/isapi", input, StageDevice, cancellationToken, suppressSuccessActivity: true)
            .ConfigureAwait(false)
            ?? throw new ApiException("Isapi không trả về bước thực thi nào.");

        await PublishStep("Isapi", step, cancellationToken).ConfigureAwait(false);
        return step;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Nhật ký kích hoạt kịch bản / thao tác thiết bị (VwEventTriggerLogController) — lịch sử các phiên TRƯỚC ĐÓ.
    // Log của phiên đang chạy không đi qua đây: nó đã có sẵn trên bảng log qua
    // ActivityNotification / DeviceStepNotification.
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Đọc lịch sử nhật ký thao tác thiết bị / kích hoạt. Lệnh CHỈ-ĐỌC, không chạm tới thiết bị.
    ///
    /// suppressSuccessActivity: KHÔNG phát dòng log chung cho lượt gọi này. Nếu phát thì mỗi lần bấm
    /// "Tải lịch sử" lại thêm một dòng rác lên đúng bảng vừa tải — và các bài test đấu nối đang đếm
    /// chính xác số dòng log sẽ lệch.
    /// </summary>
    public async Task<PagedResult<VwEventTriggerLogDto>> PageEventTriggerLogs(
        VwEventTriggerLogPageInput input, CancellationToken cancellationToken)
    {
        var result = await apiInvoker
            .PostAsync<PagedResult<VwEventTriggerLogDto>>(
                ApiInvoker.VideoWallClient,
                $"{Base}/vweventtriggerlog/page",
                input,
                StageDevice,
                cancellationToken,
                suppressSuccessActivity: true)
            .ConfigureAwait(false);

        return result ?? new PagedResult<VwEventTriggerLogDto>();
    }

    /// <summary>
    /// Sinh Code duy nhất cho bản ghi mới. Backend bắt buộc có Code, kiểm tra trùng và giới hạn
    /// 64 ký tự; endpoint Add lại không trả ID nên Code chính là chìa khoá để tra lại bản ghi.
    /// </summary>
    public static string NewCode(string prefix) =>
        $"{prefix}-{BaseUtil.TimeServer.GetTimeServer():yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";

    private Task PublishSteps(string action, List<VwSetupSceneStep>? steps, CancellationToken cancellationToken)
    {
        if (steps is null || steps.Count == 0)
            return Task.CompletedTask;

        return PublishStepsCore(action, steps, cancellationToken);
    }

    private async Task PublishStepsCore(string action, List<VwSetupSceneStep> steps, CancellationToken cancellationToken)
    {
        foreach (var step in steps.OrderBy(item => item.Order))
            await PublishStep(action, step, cancellationToken).ConfigureAwait(false);
    }

    private async Task PublishStep(string action, VwSetupSceneStep step, CancellationToken cancellationToken)
    {
        var level = step.Skipped
            ? ActivityLevel.Warning
            : step.Success ? ActivityLevel.Success : ActivityLevel.Error;

        var detail = $"{action} · {step.Name} · {step.Outcome}"
            + (step.HttpStatus is null ? string.Empty : $" · HTTP {step.HttpStatus}")
            + (string.IsNullOrWhiteSpace(step.Message) ? string.Empty : $" · {step.Message.Trim()}")
            + $" ({step.DurationMs} ms)";

        var activity = new Activity(BaseUtil.TimeServer.GetTimeServer(), StageDevice, detail, level);

        try
        {
            await publisher.Publish(new DeviceStepNotification(activity, step), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Log là phụ trợ: subscriber lỗi không được làm gãy luồng đấu nối.
            activityPublisher.Publish(StageDevice, $"Không ghi được log bước {step.Name}: {exception.Message}", ActivityLevel.Warning);
        }
    }
}
