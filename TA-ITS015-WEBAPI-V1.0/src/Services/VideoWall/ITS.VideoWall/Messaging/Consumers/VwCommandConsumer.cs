using System.Diagnostics;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.Command;
using Module.VideoWall.Core.Dto.Device;
using Module.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Dto.WindowScene;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Interfaces;
using Newtonsoft.Json.Linq;
using Services.Shared.Messaging;
using Shared.DTO.Enums;

namespace ITS.VideoWall.Messaging.Consumers
{
    /// <summary>
    /// Description: BackgroundService lắng nghe lệnh điều khiển Video Wall từ NATS (ta.its.data.videowall.control)
    ///              và thực thi qua IVwISAPIDeviceService, sau đó phản hồi telemetry (ta.its.data.videowall).
    /// Created date: 10/09/2026
    /// </summary>
    public class VwCommandConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VwCommandConsumer> _logger;
        private readonly TransportManager? _transport;
        private readonly IVwNatsPublisher? _publisher;

        /// <summary>Hook kiểm thử để nhận telemetry trực tiếp trong unit test</summary>
        public Action<string, string, bool, string?, object?>? OnTelemetryPublished { get; set; }

        /// <summary>Hook kiểm thử để nhận VwCommandResponseEnvelope có kiểu trong unit test</summary>
        public Action<VwCommandResponseEnvelope>? OnResponseEnvelopePublished { get; set; }

        public VwCommandConsumer(
            IServiceScopeFactory scopeFactory,
            ILogger<VwCommandConsumer> logger)
            : this(scopeFactory, logger, null, null)
        {
        }

        public VwCommandConsumer(
            IServiceScopeFactory scopeFactory,
            ILogger<VwCommandConsumer> logger,
            TransportManager? transport,
            IVwNatsPublisher? publisher)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _transport = transport;
            _publisher = publisher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("VwCommandConsumer bắt đầu lắng nghe kênh {Subject}", VwSubjects.Control);

            if (_transport != null)
            {
                await _transport.ConnectAsync(stoppingToken);
                await _transport.SubscribeAsync(VwSubjects.Control, async json =>
                {
                    try
                    {
                        await ProcessCommandAsync(json, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi không xác định khi xử lý thông điệp từ kênh {Subject}", VwSubjects.Control);
                    }
                }, stoppingToken);
            }

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Dừng êm khi nhận tín hiệu shutdown
            }

            _logger.LogInformation("VwCommandConsumer đã dừng lắng nghe");
        }

        /// <summary>
        /// Xử lý gói tin lệnh VideoWall. Công khai để hỗ trợ kiểm thử đơn vị.
        /// </summary>
        public async Task ProcessCommandAsync(object raw, CancellationToken ct = default)
        {
            if (raw == null)
                return;

            VwCommandEnvelope? envelope = null;

            if (raw is VwCommandEnvelope directEnvelope)
            {
                envelope = directEnvelope;
            }
            else if (raw is string jsonStr)
            {
                envelope = JsonConvert.DeserializeObject<VwCommandEnvelope>(jsonStr);
            }
            else
            {
                var json = JsonConvert.SerializeObject(raw);
                envelope = JsonConvert.DeserializeObject<VwCommandEnvelope>(json);
            }

            if (envelope == null || string.IsNullOrWhiteSpace(envelope.Action))
            {
                _logger.LogWarning("VwCommandConsumer nhận thông điệp không hợp lệ: {Raw}", raw);
                return;
            }

            // Lọc theo Type/PackageType: VwCommandConsumer CHỈ thụ lý các gói tin chiều gửi lệnh (Control).
            // Nếu nhận phải gói phản hồi (ControlResponse) hoặc kiểu gói khác, bỏ qua để tránh xử lý nhầm.
            if (!string.IsNullOrWhiteSpace(envelope.Type) &&
                !string.Equals(envelope.Type, VwPackageType.Control, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "VwCommandConsumer bỏ qua gói tin vì Type không phải Control: Type={Type}, Action={Action}, MsgId={MessageId}",
                    envelope.Type,
                    envelope.Action,
                    envelope.MessageId);
                return;
            }

            _logger.LogInformation(
                "VwCommandConsumer nhận lệnh {Action} (MsgId: {MessageId}, ControllerId: {ControllerId}, SceneId: {SceneId})",
                envelope.Action,
                envelope.MessageId,
                envelope.ControllerId,
                envelope.SceneId);

            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;

            var deviceService = sp.GetRequiredService<IVwISAPIDeviceService>();
            var deviceClient = sp.GetRequiredService<IVwISAPIDeviceClient>();
            var sceneRep = sp.GetRequiredService<BaseRepository<VwScene>>();
            var controllerRep = sp.GetRequiredService<BaseRepository<VwController>>();

            try
            {
                switch (envelope.Action)
                {
                    case VwCommandActions.ActivateScene:
                        await HandleActivateSceneAsync(envelope, deviceService, sceneRep, controllerRep, ct);
                        break;

                    case VwCommandActions.SyncSceneWindows:
                        await HandleSyncSceneWindowsAsync(envelope, deviceService, ct);
                        break;

                    case VwCommandActions.SetWindowLayer:
                        await HandleSetWindowLayerAsync(envelope, deviceService, controllerRep, ct);
                        break;

                    case VwCommandActions.ResetDeviceAuthFailure:
                        await HandleResetDeviceAuthFailureAsync(envelope, deviceService, ct);
                        break;

                    case VwCommandActions.DeviceSetupPing:
                        await HandleDeviceSetupPingAsync(envelope, deviceService, ct);
                        break;

                    case VwCommandActions.DeviceSetupProbe:
                        await HandleDeviceSetupProbeAsync(envelope, deviceService, ct);
                        break;

                    case VwCommandActions.DeviceSetupSyncSources:
                        await HandleDeviceSetupSyncSourcesAsync(envelope, deviceService, ct);
                        break;

                    case VwCommandActions.DeviceSetupSyncOutputs:
                        await HandleDeviceSetupSyncOutputsAsync(envelope, deviceService, ct);
                        break;

                    case VwCommandActions.DeviceSetupSyncScreens:
                        await HandleDeviceSetupSyncScreensAsync(envelope, deviceService, ct);
                        break;

                    case VwCommandActions.DeviceSetupApplyWallOutputs:
                        await HandleDeviceSetupApplyWallOutputsAsync(envelope, deviceService, ct);
                        break;

                    case VwCommandActions.DeviceSetupScreenPower:
                        await HandleDeviceSetupScreenPowerAsync(envelope, deviceService, ct);
                        break;

                    case VwCommandActions.DeviceSetupScreenBacklight:
                        await HandleDeviceSetupScreenBacklightAsync(envelope, deviceService, ct);
                        break;

                    case VwCommandActions.DeviceSetupScreenStatus:
                        await HandleDeviceSetupScreenStatusAsync(envelope, deviceService, ct);
                        break;

                    case VwCommandActions.DeviceSetupSyncActiveScene:
                        await HandleDeviceSetupSyncActiveSceneAsync(envelope, deviceService, ct);
                        break;

                    case VwCommandActions.DeviceSetupScene:
                        await HandleDeviceSetupSceneAsync(envelope, deviceService, ct);
                        break;

                    case VwCommandActions.DeviceSetupSendPassthrough:
                        await HandleDeviceSetupSendPassthroughAsync(envelope, deviceService, ct);
                        break;

                    case VwCommandActions.DeviceSetupResetDeviceAuthFailure:
                        await HandleDeviceSetupResetDeviceAuthFailureAsync(envelope, deviceService, controllerRep, ct);
                        break;

                    case VwCommandActions.DeviceProxyInputChannels:
                        await HandleDeviceProxyInputChannelsAsync(envelope, deviceClient, controllerRep, ct);
                        break;

                    default:
                        _logger.LogWarning("VwCommandConsumer: Hành động chưa được hỗ trợ: {Action}", envelope.Action);
                        await PublishTelemetry(envelope.MessageId, envelope.Action, false, $"Unsupported action: {envelope.Action}", null, ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "VwCommandConsumer: Thực thi lệnh {Action} (MsgId: {MessageId}) thất bại: {Message}",
                    envelope.Action,
                    envelope.MessageId,
                    ex.Message);

                await PublishTelemetry(envelope.MessageId, envelope.Action, false, ex.Message, null, ct);
            }
        }

        private async Task HandleActivateSceneAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            BaseRepository<VwScene> sceneRep,
            BaseRepository<VwController> controllerRep,
            CancellationToken ct)
        {
            var payload = ExtractPayload<VwActivateScenePayload>(envelope.Payload);
            var sceneId = payload?.SceneId ?? envelope.SceneId;

            if (string.IsNullOrWhiteSpace(sceneId))
                throw new ArgumentException("SceneId không được để trống trong lệnh ACTIVATE_SCENE");

            var scene = await sceneRep.GetFirstAsync(u => u.IsDelete == null && u.ID == sceneId, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy kịch bản ID: {sceneId}");

            if (!string.IsNullOrWhiteSpace(payload?.OutputId))
                scene.OutputId = payload.OutputId;

            var controllers = await controllerRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .ToListAsync(ct);

            var targetControllerIds = payload?.TargetControllerIds ?? [];
            await deviceService.ActivateScene(scene, controllers, targetControllerIds, ct);

            // Ghi DB sau khi ISAPI thành công: Guard "stale" bằng envelope.Timestamp UTC
            var now = DateTime.UtcNow;
            var envelopeTimestamp = envelope.Timestamp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(envelope.Timestamp).UtcDateTime
                : now;

            var targetControllers = controllers.Where(c => targetControllerIds.Contains(c.ID)).ToList();
            bool isStale = targetControllers.Any(c => c.ActiveSceneAt.HasValue && c.ActiveSceneAt.Value > envelopeTimestamp);

            if (!isStale && targetControllerIds.Count > 0)
            {
                // Đọc danh sách controller để tính activeSceneIds dự kiến TRƯỚC khi mở transaction ghi
                var allControllers = await controllerRep.AsQueryable().Where(u => u.IsDelete == null).ToListAsync(ct);
                var activeSceneIds = allControllers
                    .Select(c => targetControllerIds.Contains(c.ID) ? scene.ID : c.ActiveSceneId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToHashSet();

                controllerRep.Context.Ado.BeginTran();
                try
                {
                    await controllerRep.AsUpdateable()
                        .SetColumns(u => new VwController { ActiveSceneId = scene.ID, ActiveSceneAt = now })
                        .Where(u => u.IsDelete == null && targetControllerIds.Contains(u.ID))
                        .ExecuteCommandAsync();

                    await sceneRep.AsUpdateable()
                        .SetColumns(u => new VwScene { ActiveScene = BaseEnums.ActiveScene.DeActivate })
                        .Where(u => u.IsDelete == null && u.ActiveScene == BaseEnums.ActiveScene.Activate && !activeSceneIds.Contains(u.ID))
                        .ExecuteCommandAsync();

                    await sceneRep.AsUpdateable()
                        .SetColumns(u => new VwScene { ActiveScene = BaseEnums.ActiveScene.Activate })
                        .Where(u => u.ID == scene.ID)
                        .ExecuteCommandAsync();

                    controllerRep.Context.Ado.CommitTran();
                }
                catch
                {
                    controllerRep.Context.Ado.RollbackTran();
                    throw;
                }
            }

            await PublishTelemetry(envelope.MessageId, envelope.Action, true, null, new
            {
                SceneId = scene.ID,
                OutputId = scene.OutputId,
                TargetControllerIds = targetControllerIds
            }, VwStatusEventType.SceneActivated, ct);

            // Báo trang Giám sát "kịch bản đang phát vừa đổi" — cùng subject + hình payload BE bắn khi ApplySceneOnServer=true
            // (FE useTransporterVideoWallScene). BE tắt ApplySceneOnServer ⇒ Worker là nơi duy nhất báo, SAU khi thiết bị đã đổi.
            if (_publisher != null)
            {
                await _publisher.PublishAsync(VwSubjects.DataScene, new
                {
                    ActiveValue = new { SceneId = scene.ID, SceneCode = scene.Code, SceneName = scene.Name },
                    ControllerIds = targetControllerIds
                }, ct);
            }
        }

        private async Task HandleSyncSceneWindowsAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            CancellationToken ct)
        {
            var payload = ExtractPayload<VwSyncWindowsPayload>(envelope.Payload);
            var sceneId = payload?.SceneId ?? envelope.SceneId;

            if (string.IsNullOrWhiteSpace(sceneId))
                throw new ArgumentException("SceneId không được để trống trong lệnh SYNC_SCENE_WINDOWS");

            var excludeIds = payload?.ExcludeWindowIds?.ToHashSet();
            await deviceService.SyncSceneWindowsToDevice(
                sceneId,
                extraWindow: null,
                excludeWindowId: payload?.ExcludeWindowId,
                excludeWindowIds: excludeIds,
                ct: ct);

            await PublishTelemetry(envelope.MessageId, envelope.Action, true, null, new
            {
                SceneId = sceneId,
                payload?.ExcludeWindowId
            }, ct);
        }

        private async Task HandleSetWindowLayerAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            BaseRepository<VwController> controllerRep,
            CancellationToken ct)
        {
            var payload = ExtractPayload<VwSetWindowLayerPayload>(envelope.Payload);
            if (payload == null)
                throw new ArgumentException("Payload không hợp lệ cho lệnh SET_WINDOW_LAYER");

            var controllerId = payload.ControllerId ?? envelope.ControllerId;
            VwController? controller = null;

            if (!string.IsNullOrWhiteSpace(controllerId))
            {
                controller = await controllerRep.GetFirstAsync(u => u.IsDelete == null && u.ID == controllerId, ct);
            }

            controller ??= await deviceService.GetCenterController(ct);
            if (controller == null)
                throw new KeyNotFoundException($"Không xác định được bộ điều khiển cho lệnh SET_WINDOW_LAYER (ID: {controllerId})");

            if (!Enum.TryParse<VwWindowLayerAction>(payload.Action, ignoreCase: true, out var layerAction))
            {
                layerAction = VwWindowLayerAction.Top;
            }

            await deviceService.SetWindowLayer(controller, payload.DeviceWindowId, layerAction, ct);

            await PublishTelemetry(envelope.MessageId, envelope.Action, true, null, new
            {
                ControllerId = controller.ID,
                payload.DeviceWindowId,
                Action = layerAction.ToString()
            }, ct);
        }

        private async Task HandleResetDeviceAuthFailureAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            CancellationToken ct)
        {
            var payload = ExtractPayload<VwResetDeviceAuthFailurePayload>(envelope.Payload);
            var target = payload?.TargetIpOrKey;

            if (string.IsNullOrWhiteSpace(target))
                deviceService.ResetAllDeviceAuthFailures();
            else
                deviceService.ResetDeviceAuthFailure(target);

            await PublishTelemetry(envelope.MessageId, envelope.Action, true, null, new
            {
                Target = string.IsNullOrWhiteSpace(target) ? "ALL" : target
            }, ct);
        }

        private async Task HandleDeviceSetupPingAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            CancellationToken ct)
        {
            string? controllerId = null;
            if (envelope.Payload is string strPayload)
            {
                controllerId = strPayload;
            }
            else if (envelope.Payload != null)
            {
                var input = ExtractPayload<VwPingDeviceInput>(envelope.Payload);
                controllerId = input?.ID;
            }

            controllerId ??= envelope.ControllerId;

            if (string.IsNullOrWhiteSpace(controllerId))
                throw new ArgumentException("ControllerId/ID không được để trống trong lệnh DEVICE_SETUP_PING");

            var step = await deviceService.Ping(controllerId, ct);
            await PublishTelemetry(envelope.MessageId, envelope.Action, true, null, step, ct);
        }

        private async Task HandleDeviceSetupProbeAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            CancellationToken ct)
        {
            var input = ExtractPayload<VwProbeDeviceInput>(envelope.Payload);
            if (input == null || string.IsNullOrWhiteSpace(input.ID))
                input = new VwProbeDeviceInput { ID = envelope.ControllerId! };

            if (string.IsNullOrWhiteSpace(input.ID))
                throw new ArgumentException("ID không được để trống trong lệnh DEVICE_SETUP_PROBE");

            var output = await deviceService.Probe(input, ct);
            await PublishTelemetry(envelope.MessageId, envelope.Action, true, null, output, VwStatusEventType.DeviceProbeCompleted, ct);
        }

        private async Task HandleDeviceSetupSyncSourcesAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            CancellationToken ct)
        {
            var input = ExtractPayload<VwSyncSourcesInput>(envelope.Payload);
            if (input == null || string.IsNullOrWhiteSpace(input.ID))
                input = new VwSyncSourcesInput { ID = envelope.ControllerId! };

            if (string.IsNullOrWhiteSpace(input.ID))
                throw new ArgumentException("ID không được để trống trong lệnh DEVICE_SETUP_SYNC_SOURCES");

            var output = await deviceService.SyncSources(input, ct);
            await PublishTelemetry(envelope.MessageId, envelope.Action, true, null, output, VwStatusEventType.SourceSynced, ct);
        }

        /// <summary>Đồng bộ cổng ra. ID trống ⇒ mọi bộ điều khiển.</summary>
        private async Task HandleDeviceSetupSyncOutputsAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            CancellationToken ct)
        {
            var input = ExtractPayload<VwSyncOutputsInput>(envelope.Payload) ?? new VwSyncOutputsInput();
            if (string.IsNullOrWhiteSpace(input.ID) && !string.IsNullOrWhiteSpace(envelope.ControllerId))
                input.ID = envelope.ControllerId;

            var output = await deviceService.SyncOutputs(input, ct);
            await PublishTelemetry(envelope.MessageId, envelope.Action, true, null, output, VwStatusEventType.OutputsSynced, ct);
        }

        /// <summary>Đồng bộ màn đang cắm của bộ con. ID trống ⇒ mọi bộ con.</summary>
        private async Task HandleDeviceSetupSyncScreensAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            CancellationToken ct)
        {
            var input = ExtractPayload<VwSyncScreensInput>(envelope.Payload) ?? new VwSyncScreensInput();
            if (string.IsNullOrWhiteSpace(input.ID) && !string.IsNullOrWhiteSpace(envelope.ControllerId))
                input.ID = envelope.ControllerId;

            var output = await deviceService.SyncScreens(input, ct);
            await PublishTelemetry(envelope.MessageId, envelope.Action, true, null, output, VwStatusEventType.ScreensSynced, ct);
        }

        /// <summary>Ghi bảng cổng ra → vị trí xuống bộ con theo Bố cục gán cổng (DryRun ⇒ chỉ so sánh). Thành công khi mọi bộ thành công.</summary>
        private async Task HandleDeviceSetupApplyWallOutputsAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            CancellationToken ct)
        {
            var input = ExtractPayload<VwApplyWallOutputsInput>(envelope.Payload) ?? new VwApplyWallOutputsInput();
            var output = await deviceService.ApplyWallOutputs(input, ct);
            var ok = output.Controllers.All(c => c.Success);
            await PublishTelemetry(envelope.MessageId, envelope.Action, ok,
                ok ? null : string.Join("; ", output.Controllers.Where(c => !c.Success).Select(c => $"{c.ControllerName}: {c.Message}")),
                output, VwStatusEventType.WallOutputsApplied, ct);
        }

        /// <summary>Bật / tắt toàn bộ màn của bộ hiển thị (ControllerIds trống ⇒ toàn tường). Thành công khi mọi bộ thành công.</summary>
        private async Task HandleDeviceSetupScreenPowerAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            CancellationToken ct)
        {
            var input = ExtractPayload<VwScreenPowerInput>(envelope.Payload)
                ?? throw new ArgumentException("Payload không hợp lệ cho lệnh DEVICE_SETUP_SCREEN_POWER");
            var output = await deviceService.ScreenPower(input, ct);
            await PublishScreenControlAsync(envelope, output, VwStatusEventType.ScreenPowerChanged, ct);
        }

        /// <summary>Đặt độ sáng (đèn nền 0-100) cho từng màn (ScreenIds).</summary>
        private async Task HandleDeviceSetupScreenBacklightAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            CancellationToken ct)
        {
            var input = ExtractPayload<VwScreenBacklightInput>(envelope.Payload)
                ?? throw new ArgumentException("Payload không hợp lệ cho lệnh DEVICE_SETUP_SCREEN_BACKLIGHT");
            if (input.Value is < 0 or > 100)
                throw new ArgumentException("Độ sáng phải trong khoảng 0-100");
            var output = await deviceService.ScreenBacklight(input, ct);
            await PublishScreenControlAsync(envelope, output, VwStatusEventType.ScreenBacklightChanged, ct);
        }

        /// <summary>Đọc trạng thái 1 màn (bật/tắt, độ sáng) khi người dùng chọn màn trên Bố cục màn.</summary>
        private async Task HandleDeviceSetupScreenStatusAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            CancellationToken ct)
        {
            var input = ExtractPayload<VwScreenStatusInput>(envelope.Payload);
            if (input == null || string.IsNullOrWhiteSpace(input.ID))
                throw new ArgumentException("ID màn không được để trống trong lệnh DEVICE_SETUP_SCREEN_STATUS");
            var output = await deviceService.ReadScreenStatus(input, ct);
            await PublishTelemetry(envelope.MessageId, envelope.Action, output.Success, output.Message, output, VwStatusEventType.ScreenStatusRead, ct);
        }

        /// <summary>Kết quả lệnh màn: thành công khi mọi bộ thành công, lỗi ghép theo từng bộ.</summary>
        private Task PublishScreenControlAsync(VwCommandEnvelope envelope, ITS.VideoWall.Core.Dto.DeviceSetup.VwScreenControlOutput output, string eventType, CancellationToken ct)
        {
            var ok = output.Controllers.All(c => c.Success);
            return PublishTelemetry(envelope.MessageId, envelope.Action, ok,
                ok ? null : string.Join("; ", output.Controllers.Where(c => !c.Success).Select(c => $"{c.ControllerName}: {c.Message}")),
                output, eventType, ct);
        }

        private async Task HandleDeviceSetupSyncActiveSceneAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            CancellationToken ct)
        {
            var input = ExtractPayload<VwSyncActiveSceneInput>(envelope.Payload);
            if (input == null || string.IsNullOrWhiteSpace(input.ID))
                input = new VwSyncActiveSceneInput { ID = envelope.ControllerId! };

            if (string.IsNullOrWhiteSpace(input.ID))
                throw new ArgumentException("ID không được để trống trong lệnh DEVICE_SETUP_SYNC_ACTIVE_SCENE");

            var output = await deviceService.SyncActiveScene(input, ct);
            await PublishTelemetry(envelope.MessageId, envelope.Action, true, null, output, VwStatusEventType.SceneActivated, ct);
        }

        private async Task HandleDeviceSetupSceneAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            CancellationToken ct)
        {
            var input = ExtractPayload<VwSetupSceneInput>(envelope.Payload)
                ?? throw new ArgumentException("Payload không hợp lệ cho lệnh DEVICE_SETUP_SCENE");

            var output = await deviceService.SetupScene(input, ct);
            await PublishTelemetry(envelope.MessageId, envelope.Action, true, null, output, null, ct);
        }

        private async Task HandleDeviceSetupSendPassthroughAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            CancellationToken ct)
        {
            var input = ExtractPayload<VwISAPIPassthroughInput>(envelope.Payload)
                ?? throw new ArgumentException("Payload không hợp lệ cho lệnh DEVICE_SETUP_SEND_PASSTHROUGH");

            var step = await deviceService.SendPassthrough(input, ct);
            await PublishTelemetry(envelope.MessageId, envelope.Action, true, null, step, null, ct);
        }

        private async Task HandleDeviceSetupResetDeviceAuthFailureAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceService deviceService,
            BaseRepository<VwController> controllerRep,
            CancellationToken ct)
        {
            var input = ExtractPayload<VwResetDeviceAuthFailureInput>(envelope.Payload);
            var controllerId = input?.ControllerId ?? envelope.ControllerId;

            if (string.IsNullOrWhiteSpace(controllerId))
            {
                deviceService.ResetAllDeviceAuthFailures();
            }
            else
            {
                var controller = await controllerRep.GetFirstAsync(u => u.IsDelete == null && u.ID == controllerId, ct);
                deviceService.ResetDeviceAuthFailure(controller?.IP ?? controllerId);
                deviceService.ForgetWall(controllerId);
            }

            await PublishTelemetry(envelope.MessageId, envelope.Action, true, null, null, null, ct);
        }

        private async Task HandleDeviceProxyInputChannelsAsync(
            VwCommandEnvelope envelope,
            IVwISAPIDeviceClient deviceClient,
            BaseRepository<VwController> controllerRep,
            CancellationToken ct)
        {
            var input = ExtractPayload<VwDeviceInputChannelsInput>(envelope.Payload) ?? new VwDeviceInputChannelsInput();
            var controllerId = input.ControllerId ?? envelope.ControllerId;

            VwController controller;
            if (!string.IsNullOrWhiteSpace(controllerId))
            {
                controller = await controllerRep.GetFirstAsync(u => u.IsDelete == null && u.ID == controllerId, ct)
                    ?? throw new InvalidOperationException($"Không tìm thấy bộ điều khiển [{controllerId}].");
            }
            else if (input.Device != null)
            {
                controller = new VwController
                {
                    IP = input.Device.Ip,
                    Port = input.Device.Port,
                    Account = input.Device.Account,
                    PassWord = input.Device.Password,
                    Name = $"Adhoc ({input.Device.Ip})"
                };
            }
            else
            {
                throw new InvalidOperationException("Phải cung cấp ControllerId hoặc thông tin thiết bị (Device).");
            }

            var sw = Stopwatch.StartNew();
            var result = await deviceClient.GetInputChannelsAsync(controller, ct);
            sw.Stop();

            var step = new VwSetupSceneStep
            {
                Name = "GetInputChannels",
                Method = "GET",
                Endpoint = "ISAPI/DisplayDev/Video/inputs/channels",
                Success = result.Success,
                HttpStatus = result.HttpStatusCode,
                Message = result.ErrorMessage,
                DurationMs = sw.ElapsedMilliseconds,
                RequestXml = result.RawRequest,
                ResponseXml = result.RawResponse
            };

            var output = new VwDeviceGenericOutput<VwISAPIInputChannelsResponse>
            {
                Data = result.Data,
                Step = step
            };

            await PublishTelemetry(envelope.MessageId, envelope.Action, result.Success, result.ErrorMessage, output, ct);
        }

        private Task PublishTelemetry(
            string messageId,
            string action,
            bool success,
            string? errorMessage,
            object? data,
            CancellationToken ct = default) =>
            PublishTelemetry(messageId, action, success, errorMessage, data, null, ct);

        private async Task PublishTelemetry(
            string messageId,
            string action,
            bool success,
            string? errorMessage,
            object? data,
            string? eventType,
            CancellationToken ct = default)
        {
            try
            {
                var responseEnvelope = new VwCommandResponseEnvelope
                {
                    MessageId = messageId,
                    Action = action,
                    EventType = eventType ?? action,
                    Success = success,
                    ErrorMessage = errorMessage,
                    Data = data,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    PackageType = VwPackageType.ControlResponse
                };

                OnResponseEnvelopePublished?.Invoke(responseEnvelope);

                if (OnTelemetryPublished != null)
                {
                    OnTelemetryPublished.Invoke(messageId, action, success, errorMessage, data);
                    return;
                }

                if (_publisher != null)
                    await _publisher.PublishAsync(VwSubjects.Data, responseEnvelope, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VwCommandConsumer: Lỗi khi broadcast telemetry cho MsgId {MessageId}", messageId);
            }
        }

        private static T? ExtractPayload<T>(object? payload)
        {
            if (payload == null) return default;
            if (payload is T direct) return direct;

            try
            {
                if (payload is JToken jToken)
                {
                    if (typeof(T) == typeof(string) && jToken.Type != JTokenType.String)
                        return default;
                    return jToken.ToObject<T>();
                }

                if (typeof(T) == typeof(string))
                    return default;

                var json = JsonConvert.SerializeObject(payload);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default;
            }
        }
    }
}


