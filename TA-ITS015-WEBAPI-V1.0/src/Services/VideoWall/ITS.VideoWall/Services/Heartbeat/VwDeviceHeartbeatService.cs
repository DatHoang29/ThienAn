using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Device;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;
using Module.VideoWall.Core.Options;
using Shared.DTO.Enums;
using static Shared.DTO.Enums.BaseEnums;

namespace ITS.VideoWall.Services.Heartbeat
{
    /// <summary>
    /// Description: BackgroundService định kỳ thăm dò trạng thái từng bộ điều khiển VideoWall qua ISAPI,
    ///              cập nhật ScreenState / SignalStatus vào CSDL, và phát delta-change lên NATS
    ///              subject ta.its.data.videowall để FE nhận realtime theo thời gian thực.
    /// Created date: 12/09/2026
    /// </summary>
    public class VwDeviceHeartbeatService(
        IServiceScopeFactory scopeFactory,
        IOptions<VwDeviceOptions> options,
        ILogger<VwDeviceHeartbeatService> logger,
        IVwNatsPublisher? publisher = null) : BackgroundService
    {
        // ExecuteAsync: override bắt buộc từ BackgroundService (framework .NET) — không tính vào
        // quy tắc «bỏ suffix Async» của người dùng (chỉ áp dụng cho method tự viết thêm dưới đây).
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var intervalSeconds = options.Value?.Device?.HeartbeatIntervalSeconds ?? 20;
            logger.LogInformation(
                "VwDeviceHeartbeatService bắt đầu, chu kỳ: {IntervalSeconds}s",
                intervalSeconds);

            while (!ct.IsCancellationRequested)
            {
                await PollAllControllers(ct);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
            }

            logger.LogInformation("VwDeviceHeartbeatService đã dừng");
        }

        /// <summary>
        /// Description: Lấy danh sách controller đang hoạt động và duyệt từng controller độc lập.
        ///              1 controller lỗi không chặn các controller còn lại.
        /// Created date: 12/09/2026
        /// </summary>
        private async Task PollAllControllers(CancellationToken ct)
        {
            using var scope = scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var controllerRep = sp.GetRequiredService<BaseRepository<VwController>>();

            var controllers = await controllerRep.AsQueryable()
                .Where(c => c.IsDelete == null
                         && c.Status == BaseEnums.StatusEnum.Enable
                         && !string.IsNullOrWhiteSpace(c.IP))
                .ToListAsync();

            foreach (var controller in controllers)
            {
                try
                {
                    await PollOneController(controller, sp, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "VwDeviceHeartbeatService: Lỗi không xác định khi thăm dò controller {ControllerId} ({IP})",
                        controller.ID, controller.IP);
                }
            }
        }

        /// <summary>
        /// Description: Thăm dò 1 bộ điều khiển: lấy trạng thái input channels (SignalStatus) và
        ///              output channels (ScreenState), cập nhật CSDL, phát NATS delta-change.
        ///              Khi gọi ISAPI thất bại, đánh dấu toàn bộ VwScreen của controller là Offline.
        /// Created date: 12/09/2026
        /// </summary>
        private async Task PollOneController(VwController controller, IServiceProvider sp, CancellationToken ct)
        {
            var deviceClient = sp.GetRequiredService<IVwISAPIDeviceClient>();
            var db = sp.GetRequiredService<BaseRepository<VwController>>().Client;

            var now = DateTime.UtcNow;
            var telemetry = new VwDeviceHeartbeatTelemetry
            {
                ControllerId = controller.ID,
                Timestamp = now
            };

            bool anyFailure = false;

            // --- SignalStatus: kênh đầu vào video (HDMI/DP/...) ---
            var inputResult = await deviceClient.GetInputChannelsAsync(controller, ct);
            if (!inputResult.Success || inputResult.Data == null)
            {
                logger.LogWarning(
                    "VwDeviceHeartbeatService: Không lấy được InputChannels từ controller {ControllerId} ({IP}): {Error}",
                    controller.ID, controller.IP, inputResult.ErrorMessage);
                anyFailure = true;
            }
            else
            {
                var channels = inputResult.Data.VideoInputChannel;
                var sources = await db.Queryable<VwSource>()
                    .Where(s => s.IsDelete == null && s.ControllerId == controller.ID && s.SignalNo != null)
                    .ToListAsync();

                foreach (var source in sources)
                {
                    var ch = channels.FirstOrDefault(c => c.Id == source.SignalNo);
                    if (ch == null) continue;

                    // Thiết bị thật DS-C66S trả signalStatus="signal" (có tín hiệu)
                    // hoặc các giá trị khác (NoSignal) — đo 67 bản ghi log thật §B.2/§H.1.
                    // "normal" là giá trị đặc tả hãng, KHÔNG khớp thiết bị thật.
                    //TODO: comment code cũ để  bypass type check & logic
                    var newStatus = string.Equals(
                                ch.VideoInputChannelAccessStatus,
                                SignalStatus.Signal,
                                StringComparison.OrdinalIgnoreCase)
                                ? BaseEnums.SignalStatus.Signal
                                : BaseEnums.SignalStatus.NoSignal;
                    //? BaseEnums.StatusEnum.Enable
                    //: BaseEnums.StatusEnum.Disable;

                    if (source.SignalStatus != newStatus)
                    {
                        await db.Updateable<VwSource>()
                            .SetColumns(s => s.SignalStatus == newStatus)
                            .SetColumns(s => s.LastCheckedAt == now)
                            .Where(s => s.ID == source.ID)
                            .ExecuteCommandAsync();

                        telemetry.Sources.Add(new VwSourceSignalChangeDto
                        {
                            SourceId = source.ID,
                            SignalStatus = newStatus
                        });
                    }
                    else
                    {
                        // Vẫn cập nhật LastCheckedAt để ghi nhận đã kiểm tra
                        await db.Updateable<VwSource>()
                            .SetColumns(s => s.LastCheckedAt == now)
                            .Where(s => s.ID == source.ID)
                            .ExecuteCommandAsync();
                    }
                }
            }

            // --- ScreenState: kênh cổng ra video (màn hình vật lý) ---
            var outputResult = await deviceClient.GetOutputChannelsAsync(controller, ct);
            if (!outputResult.Success || outputResult.Data == null)
            {
                logger.LogWarning(
                    "VwDeviceHeartbeatService: Không lấy được OutputChannels từ controller {ControllerId} ({IP}): {Error}",
                    controller.ID, controller.IP, outputResult.ErrorMessage);
                anyFailure = true;
            }
            else
            {
                var channels = outputResult.Data.VideoOutputChannel;
                // Màn đang cắm vào cổng ra của bộ điều khiển này (VwScreen.ControllerOutputId → VwControllerOutput)
                var ports = await db.Queryable<VwControllerOutput>()
                    .Where(o => o.IsDelete == null && o.ControllerId == controller.ID && o.DeviceOutputId != null)
                    .ToListAsync();
                var portById = ports.ToDictionary(o => o.ID);
                var portIds = ports.Select(o => o.ID).ToList();
                var screens = portIds.Count == 0 ? [] : await db.Queryable<VwScreen>()
                    .Where(s => s.IsDelete == null && s.ControllerOutputId != null && portIds.Contains(s.ControllerOutputId))
                    .ToListAsync();

                foreach (var screen in screens)
                {
                    var deviceOutputId = portById[screen.ControllerOutputId!].DeviceOutputId;
                    var ch = channels.FirstOrDefault(c => c.Id == deviceOutputId);
                    if (ch == null) continue;

                    string newState;
                    if (string.Equals(ch.OutputPortAccessStatus, "normal", StringComparison.OrdinalIgnoreCase))
                        newState = BaseEnums.ScreenState.Online;
                    else if (!string.IsNullOrWhiteSpace(ch.AbnormalReason))
                        newState = BaseEnums.ScreenState.Warning;
                    else
                        newState = BaseEnums.ScreenState.Offline;

                    if (screen.ScreenState != newState)
                    {
                        await db.Updateable<VwScreen>()
                            .SetColumns(s => s.ScreenState == newState)
                            .SetColumns(s => s.LastCheckedAt == now)
                            .Where(s => s.ID == screen.ID)
                            .ExecuteCommandAsync();

                        telemetry.Screens.Add(new VwScreenStateChangeDto
                        {
                            ScreenId = screen.ID,
                            ScreenState = newState
                        });
                    }
                    else
                    {
                        await db.Updateable<VwScreen>()
                            .SetColumns(s => s.LastCheckedAt == now)
                            .Where(s => s.ID == screen.ID)
                            .ExecuteCommandAsync();
                    }
                }
            }

            // Nếu cả 2 ISAPI đều thất bại → đánh offline toàn bộ màn hình của controller này
            if (anyFailure && !outputResult.Success)
            {
                await MarkControllerScreensOffline(controller, db, now, telemetry);
            }

            // Phát NATS chỉ khi có thay đổi
            if ((telemetry.Screens.Count > 0 || telemetry.Sources.Count > 0) && publisher != null)
            {
                await publisher.PublishAsync(VwSubjects.Data, telemetry, ct);
                logger.LogInformation(
                    "VwDeviceHeartbeatService: Đã phát telemetry controller {ControllerId}: {ScreenCount} màn hình, {SourceCount} nguồn thay đổi",
                    controller.ID, telemetry.Screens.Count, telemetry.Sources.Count);
            }
        }

        /// <summary>
        /// Description: Đánh trạng thái Offline cho toàn bộ VwScreen thuộc controller khi ISAPI thất bại.
        ///              Đây là tín hiệu «controller mất mạng» — chính là ý nghĩa heartbeat cần làm.
        /// Created date: 12/09/2026
        /// </summary>
        private async Task MarkControllerScreensOffline(
            VwController controller,
            ISqlSugarClient db,
            DateTime now,
            VwDeviceHeartbeatTelemetry telemetry)
        {
            logger.LogWarning(
                "VwDeviceHeartbeatService: Đánh Offline toàn bộ màn hình của controller {ControllerId} ({IP}) do mất kết nối ISAPI",
                controller.ID, controller.IP);

            var portIds = await db.Queryable<VwControllerOutput>()
                .Where(o => o.IsDelete == null && o.ControllerId == controller.ID)
                .Select(o => o.ID)
                .ToListAsync();
            if (portIds.Count == 0)
                return;

            var screens = await db.Queryable<VwScreen>()
                .Where(s => s.IsDelete == null
                         && s.ControllerOutputId != null && portIds.Contains(s.ControllerOutputId)
                         && s.ScreenState != BaseEnums.ScreenState.Offline)
                .ToListAsync();

            foreach (var screen in screens)
            {
                await db.Updateable<VwScreen>()
                    .SetColumns(s => s.ScreenState == BaseEnums.ScreenState.Offline)
                    .SetColumns(s => s.LastCheckedAt == now)
                    .Where(s => s.ID == screen.ID)
                    .ExecuteCommandAsync();

                telemetry.Screens.Add(new VwScreenStateChangeDto
                {
                    ScreenId = screen.ID,
                    ScreenState = BaseEnums.ScreenState.Offline
                });
            }
        }
    }
}

