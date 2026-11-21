using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.DeviceSetup;
using ITS.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Dto.WindowScene;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Exceptions;
using Module.VideoWall.Core.Options;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using System.Diagnostics;

namespace ITS.VideoWall.Services.ISAPIDevice
{
    public partial class VwISAPIDeviceService
    {
        // ══════════════════════════════════════════════════════════════════════════════════════
        // Lớp ngoài: ghi nhật ký VwEventTriggerLog cho MỌI lệnh đấu nối.
        //
        // VÌ SAO TÁCH `*Core`: việc ghi log phải xảy ra đúng một lần cho mỗi lượt gọi, trên mọi
        // đường ra. Probe/SyncSources/SetupScene có nhiều `return` sớm (thiết bị không tới được,
        // thiếu năng lực, dryRun...), nên nếu chèn lời gọi ghi "ngay trước return" thì phải rải ở
        // hàng chục chỗ và chỉ cần thêm một nhánh return mới là mất log mà không ai biết. Bọc ngoài
        // thì thân nghiệp vụ giữ nguyên toàn bộ return sớm, còn log thì không thể lọt.
        //
        // Nhánh ném ngoại lệ cũng ghi ở đây, nên toàn bộ việc ghi nhật ký thiết bị nằm gọn trong
        // file này — VwDeviceSetupCommandHandler không còn biết tới IVwEventTriggerLogWriter nữa.
        //
        // Vẫn MỘT lệnh insert cho mỗi lượt gọi: WriteSteps gộp cả danh sách bước.
        // ══════════════════════════════════════════════════════════════════════════════════════

        public Task<VwSetupSceneStep> Ping(string controllerId, CancellationToken ct = default) =>
            TrackAndLog(VwEventTriggerAction.Ping,
                () => PingCore(controllerId, ct),
                step => (controllerId, (string?)null, (string?)null, (IReadOnlyList<VwSetupSceneStep>)new[] { step }),
                (controllerId, null, null),
                ct);

        public Task<VwProbeDeviceOutput> Probe(VwProbeDeviceInput input, CancellationToken ct = default) =>
            TrackAndLog(VwEventTriggerAction.Probe,
                () => ProbeCore(input, ct),
                output => (output.ControllerId ?? input.ID, (string?)null, (string?)null, (IReadOnlyList<VwSetupSceneStep>)output.Steps),
                (input.ID, null, null),
                ct);

        public Task<VwSyncSourcesOutput> SyncSources(VwSyncSourcesInput input, CancellationToken ct = default) =>
            TrackAndLog(VwEventTriggerAction.SyncSources,
                () => SyncSourcesCore(input, ct),
                output => (output.ControllerId ?? input.ID, (string?)null, (string?)null, (IReadOnlyList<VwSetupSceneStep>)output.Steps),
                (input.ID, null, null),
                ct);

        public Task<VwSyncActiveSceneOutput> SyncActiveScene(VwSyncActiveSceneInput input, CancellationToken ct = default) =>
            TrackAndLog(VwEventTriggerAction.SyncActiveScene,
                () => SyncActiveSceneCore(input, ct),
                output => (output.ControllerId ?? input.ID, output.MatchedSceneId, (string?)null, (IReadOnlyList<VwSetupSceneStep>)output.Steps),
                (input.ID, null, null),
                ct);

        public Task<VwSetupSceneOutput> SetupScene(VwSetupSceneInput input, CancellationToken ct = default) =>
            TrackAndLog(VwEventTriggerAction.SetupScene,
                () => SetupSceneCore(input, ct),
                output => (
                    output.ControllerId ?? input.ControllerId,
                    output.SceneId ?? input.SceneId,
                    output.SceneCode ?? input.SceneCode,
                    (IReadOnlyList<VwSetupSceneStep>)output.Steps),
                (input.ControllerId, input.SceneId, input.SceneCode),
                ct);

        public Task<VwSetupSceneStep> SendPassthrough(VwISAPIPassthroughInput input, CancellationToken ct = default) =>
            TrackAndLog(VwEventTriggerAction.Isapi,
                () => SendPassthroughCore(input, ct),
                step => (input.ControllerId, (string?)null, (string?)null, (IReadOnlyList<VwSetupSceneStep>)new[] { step }),
                (input.ControllerId, null, null),
                ct);

        /// <summary>
        /// Bọc ngoài mọi lệnh đấu nối: chạy thân *Core (giữ nguyên các return sớm của nó), rồi ghi
        /// nhật ký VwEventTriggerLog đúng một lần trên đường thành công; nhánh ném ngoại lệ ghi một
        /// dòng tổng hợp rồi ném lại nguyên vẹn.
        /// </summary>
        private async Task<T> TrackAndLog<T>(
            string action,
            Func<Task<T>> core,
            Func<T, (string? ControllerId, string? SceneId, string? SceneCode, IReadOnlyList<VwSetupSceneStep> Steps)> onSuccess,
            (string? ControllerId, string? SceneId, string? SceneCode) onThrow,
            CancellationToken ct)
        {
            try
            {
                var result = await core();
                var (controllerId, sceneId, sceneCode, steps) = onSuccess(result);
                await _eventTriggerLogService.WriteSteps(action, controllerId, sceneId, sceneCode, steps, ct);

                return result;
            }
            catch (Exception exception)
            {
                await WriteThrownFailure(action, onThrow.ControllerId, onThrow.SceneId, onThrow.SceneCode, exception, ct);
                throw;
            }
        }

        /// <summary>
        /// Description: Ghi một dòng tổng hợp cho lượt gọi bị ném ngoại lệ giữa đường (bộ điều khiển
        ///              không tồn tại, ngắt mạch đang chặn, thiếu OutputId...). Những lượt đó không
        ///              sinh ra bước ISAPI nào nên không có gì để WriteSteps ghi.
        ///              Writer đã tự nuốt lỗi nên hàm này không thể chen vào việc ném lại ngoại lệ gốc.
        /// Created date: 26/08/2026
        /// </summary>
        private Task WriteThrownFailure(
            string action,
            string? controllerId,
            string? sceneId,
            string? sceneCode,
            Exception exception,
            CancellationToken ct)
        {
            var step = new VwSetupSceneStep
            {
                Order = 1,
                Name = VwEventTriggerAction.FailedStepName,
                Success = false,
                Message = exception.Message,
            };

            return _eventTriggerLogService.WriteStep(action, controllerId, sceneId, sceneCode, step, ct);
        }

        private async Task<VwSetupSceneStep> PingCore(string controllerId, CancellationToken ct)
        {
            var controller = await LoadController(controllerId);
            var steps = new List<VwSetupSceneStep>();

            await Track(steps, "Xác thực Digest", "GET", "/ISAPI/Security/userCheck",
                () => _deviceClient.UserCheckAsync(controller, ct));

            return steps[0];
        }

        private async Task<VwProbeDeviceOutput> ProbeCore(VwProbeDeviceInput input, CancellationToken ct)
        {
            var controller = await LoadController(input.ID);
            var output = new VwProbeDeviceOutput
            {
                ControllerId = controller.ID,
                ControllerName = controller.Name,
                Ip = controller.IP,
                AssumedModel = _deviceOptions.Value?.Device?.Profile?.Model ?? VwDeviceProfile.Model
            };

            var userCheck = await Track(output.Steps, "Xác thực Digest", "GET", "/ISAPI/Security/userCheck",
                () => _deviceClient.UserCheckAsync(controller, ct));
            output.Reachable = userCheck.Success;
            if (!userCheck.Success)
            {
                await _vwControllerRep.AsUpdateable()
                    .SetColumns(u => new VwController { Status = BaseEnums.StatusEnum.Disable })
                    .Where(u => u.ID == controller.ID)
                    .ExecuteCommandAsync();
                return output;
            }

            var capabilities = await Track(output.Steps, "Năng lực Video Wall", "GET", "/ISAPI/DisplayDev/VideoWall/capabilities",
                () => _deviceClient.GetCapabilitiesAsync(controller, ct));
            if (capabilities is { Success: true, Data: not null })
            {
                output.IsSupportScene = capabilities.Data.IsSupportScene;
                output.MaxWindowNums = capabilities.Data.MaxWindowNums;
                output.MaxSceneNums = capabilities.Data.MaxSceneNums;
                output.BaseOutputSize = capabilities.Data.BaseOutputSize;
            }

            var wallNo = await ResolveWall(output.Steps, controller, input.WallNo, output.Walls, ct);
            output.WallNo = wallNo;
            if (wallNo == null)
                return output;

            // Ghi nhận trạng thái và 5 thuộc tính năng lực thiết bị vào VwController (chỉ ghi cho bộ trung tâm —
            // bộ được bộ khác trỏ tới làm cha, xem VwControllerTopology)
            var wallNoStr = output.WallNo.HasValue ? output.WallNo.Value.ToString() : null;
            var isCenter = await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.ParentControllerId == controller.ID)
                .AnyAsync();
            if (isCenter)
            {
                await _vwControllerRep.AsUpdateable()
                    .SetColumns(u => new VwController
                    {
                        Status = BaseEnums.StatusEnum.Enable,
                        MaxWindowNums = output.MaxWindowNums,
                        MaxSceneNums = output.MaxSceneNums,
                        BaseOutputSize = output.BaseOutputSize,
                        IsSupportScene = output.IsSupportScene,
                        WallNo = wallNoStr
                    })
                    .Where(u => u.ID == controller.ID)
                    .ExecuteCommandAsync();
            }
            else
            {
                await _vwControllerRep.AsUpdateable()
                    .SetColumns(u => new VwController { Status = BaseEnums.StatusEnum.Enable })
                    .Where(u => u.ID == controller.ID)
                    .ExecuteCommandAsync();
            }

            var outputs = await Track(output.Steps, "Cổng ra của tường", "GET", $"/ISAPI/DisplayDev/VideoWall/{wallNo}/outputs",
                () => _deviceClient.GetOutputsAsync(controller, wallNo, ct));
            if (outputs is { Success: true, Data: not null })
            {
                output.Outputs = outputs.Data.WallOutput;
                output.ScreenMismatches = await CompareOutputs(controller.ID, outputs.Data.WallOutput);
            }

            var inputs = await Track(output.Steps, "Kênh tín hiệu vào", "GET", "/ISAPI/DisplayDev/Video/inputs/channels",
                () => _deviceClient.GetInputChannelsAsync(controller, ct));
            if (inputs is { Success: true, Data: not null })
            {
                output.InputChannels = inputs.Data.VideoInputChannel;
                // Chỉ báo lệch — không tự gán cổng cho nguồn (12/11/2026): cổng của nguồn chỉ do người dùng chọn.
                output.SourceMismatches = await CompareSources(controller.ID, inputs.Data.VideoInputChannel);
            }

            return output;
        }

        private async Task<VwSyncSourcesOutput> SyncSourcesCore(VwSyncSourcesInput input, CancellationToken ct)
        {
            var controller = await LoadController(input.ID);
            var output = new VwSyncSourcesOutput
            {
                ControllerId = controller.ID,
                Applied = false // Chỉ đọc + báo lệch, không ghi DB (12/11/2026)
            };

            var inputs = await Track(output.Steps, "Kênh tín hiệu vào", "GET", "/ISAPI/DisplayDev/Video/inputs/channels",
                () => _deviceClient.GetInputChannelsAsync(controller, ct));
            if (inputs is not { Success: true, Data: not null })
                return output;

            output.InputChannels = inputs.Data.VideoInputChannel;

            // Không tự gán cổng cho nguồn: cổng chỉ do người dùng chọn ở form nguồn; trạng thái tín hiệu do heartbeat ghi.
            output.Changes = await CompareSources(controller.ID, inputs.Data.VideoInputChannel);

            return output;
        }

        private async Task<VwSyncActiveSceneOutput> SyncActiveSceneCore(VwSyncActiveSceneInput input, CancellationToken ct)
        {
            var controller = await LoadController(input.ID);
            var output = new VwSyncActiveSceneOutput
            {
                ControllerId = controller.ID,
                DbActiveSceneId = controller.ActiveSceneId
            };

            var wallNo = await ResolveWall(output.Steps, controller, input.WallNo, output.Walls, ct);
            if (wallNo == null)
            {
                output.Message = "Không xác định được tường đang gắn màn hình.";
                return output;
            }

            output.WallNo = wallNo;

            var activeSceneResult = await Track(output.Steps, "Kịch bản đang chạy", "GET",
                $"/ISAPI/DisplayDev/VideoWall/{wallNo}/scene/isRunning",
                () => _deviceClient.GetActiveSceneAsync(controller, wallNo, ct));

            if (activeSceneResult is not { Success: true, Data: not null })
            {
                output.Message = activeSceneResult?.ErrorMessage ?? "Không đọc được kịch bản đang chạy từ thiết bị.";
                return output;
            }

            var deviceSceneId = activeSceneResult.Data.SceneId;
            output.DeviceActiveSceneId = deviceSceneId;

            // TODO: VwScene bỏ ControllerId
            var matchedScene = await _vwSceneRep.AsQueryable()
                .Where(s => s.IsDelete == null)
                //.Where(s => s.ControllerId == controller.ID || string.IsNullOrEmpty(s.ControllerId))
                .Where(s => s.OutputId == deviceSceneId.ToString() || s.ID == deviceSceneId.ToString())
                //.OrderBy(s => s.ControllerId == controller.ID ? 0 : 1)
                .FirstAsync();

            if (matchedScene != null)
            {
                output.MatchedSceneId = matchedScene.ID;
                output.MatchedSceneName = matchedScene.Name;
            }

            bool isAlreadySynced = matchedScene != null
                ? string.Equals(controller.ActiveSceneId, matchedScene.ID, StringComparison.OrdinalIgnoreCase)
                : (deviceSceneId == 0 && string.IsNullOrEmpty(controller.ActiveSceneId));

            if (isAlreadySynced)
            {
                output.Synced = true;
                output.Message = "Đã đồng bộ, không lệch.";
                return output;
            }

            output.HasMismatch = true;
            output.Mismatch = new VwDeviceMismatch
            {
                EntityId = controller.ID,
                EntityName = controller.Name,
                Field = nameof(VwController.ActiveSceneId),
                DbValue = controller.ActiveSceneId,
                DeviceValue = deviceSceneId.ToString(),
                Note = matchedScene != null ? matchedScene.Name : "Kịch bản không có trong CSDL"
            };

            if (matchedScene != null)
            {
                var allControllers = await _vwControllerRep.AsQueryable().Where(u => u.IsDelete == null).ToListAsync(ct);
                var activeSceneIds = allControllers
                    .Select(c => c.ID == controller.ID ? matchedScene.ID : c.ActiveSceneId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToHashSet();

                var now = DateTime.UtcNow;
                var db = _vwControllerRep.Context;
                db.Ado.BeginTran();
                try
                {
                    await _vwControllerRep.AsUpdateable()
                        .SetColumns(u => new VwController { ActiveSceneId = matchedScene.ID, ActiveSceneAt = now })
                        .Where(u => u.ID == controller.ID)
                        .ExecuteCommandAsync();

                    await _vwSceneRep.AsUpdateable()
                        .SetColumns(u => new VwScene { ActiveScene = BaseEnums.ActiveScene.DeActivate })
                        .Where(u => u.IsDelete == null && u.ActiveScene == BaseEnums.ActiveScene.Activate && !activeSceneIds.Contains(u.ID))
                        .ExecuteCommandAsync();

                    await _vwSceneRep.AsUpdateable()
                        .SetColumns(u => new VwScene { ActiveScene = BaseEnums.ActiveScene.Activate })
                        .Where(u => u.ID == matchedScene.ID)
                        .ExecuteCommandAsync();

                    db.Ado.CommitTran();
                }
                catch
                {
                    db.Ado.RollbackTran();
                    throw;
                }

                _cache.RemoveByPrefixKey(CacheConst.Vw.VwScene);
                _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);

                output.Synced = true;
                output.Message = $"Đã đồng bộ Active Scene: {controller.ActiveSceneId} -> {matchedScene.Name ?? matchedScene.ID} (SID: {deviceSceneId})";

                output.Steps.Add(new VwSetupSceneStep
                {
                    Order = output.Steps.Count + 1,
                    Name = "AutoSyncActiveScene",
                    Method = "AUTO",
                    Success = true,
                    Message = output.Message,
                    DurationMs = 0
                });

                if (_publisher != null)
                {
                    await _publisher.PublishAsync(VwSubjects.Data, new
                    {
                        EventType = VwStatusEventType.SceneActivated,
                        SceneId = matchedScene.ID,
                        OutputId = matchedScene.OutputId,
                        TargetControllerIds = new[] { controller.ID }
                    }, ct);
                }
            }
            else
            {
                output.Synced = false;
                output.Message = $"Thiết bị đang chạy scene ID={deviceSceneId} không tồn tại trong CSDL. Chỉ ghi nhận lệch, không tạo scene mới.";
                output.Steps.Add(new VwSetupSceneStep
                {
                    Order = output.Steps.Count + 1,
                    Name = "SyncActiveSceneMismatch",
                    Method = "CHECK",
                    Success = false,
                    Message = output.Message,
                    DurationMs = 0
                });
            }

            return output;
        }

        private async Task<VwSetupSceneOutput> SetupSceneCore(VwSetupSceneInput input, CancellationToken ct)
        {
            var controller = await LoadController(input.ControllerId);
            var scene = await LoadScene(input);

            var output = new VwSetupSceneOutput
            {
                DryRun = input.DryRun,
                ControllerId = controller.ID,
                ControllerName = controller.Name,
                SceneId = scene.ID,
                SceneCode = scene.Code,
                SceneSid = scene.OutputId
            };

            var userCheck = await Track(output.Steps, "Xác thực Digest", "GET", "/ISAPI/Security/userCheck",
                () => _deviceClient.UserCheckAsync(controller, ct));
            if (!userCheck.Success)
                return Finish(output, "Không đăng nhập được thiết bị — dừng trước mọi thao tác ghi.");

            var capabilities = await Track(output.Steps, "Năng lực Video Wall", "GET", "/ISAPI/DisplayDev/VideoWall/capabilities",
                () => _deviceClient.GetCapabilitiesAsync(controller, ct));
            if (!capabilities.Success || capabilities.Data == null)
                return Finish(output, "Không đọc được năng lực Video Wall.");

            if (capabilities.Data.IsSupportScene == false)
                return Finish(output, "Thiết bị không hỗ trợ kịch bản (isSupportScene = false).");

            output.MaxSceneNums = capabilities.Data.MaxSceneNums;

            var wallNo = await ResolveWall(output.Steps, controller, input.WallNo, output.Walls, ct);
            if (wallNo == null)
                return Finish(output, "Không xác định được tường đang gắn màn hình.");

            output.WallNo = wallNo;

            await Track(output.Steps, "Cổng ra của tường", "GET", $"/ISAPI/DisplayDev/VideoWall/{wallNo}/outputs",
                () => _deviceClient.GetOutputsAsync(controller, wallNo, ct));

            if (input.SyncSources)
            {
                var sync = await SyncSources(new VwSyncSourcesInput { ID = controller.ID, Apply = !input.DryRun }, ct);

                // Gộp nguyên các bước con của lệnh đồng bộ vào timeline chính. Trước đây bước này
                // được dựng tay với Success = true nên GET inputs/channels lỗi vẫn báo xanh, và guard
                // "có bước nào thất bại không" bên dưới cũng không bắt được.
                foreach (var step in sync.Steps)
                {
                    step.Order = output.Steps.Count + 1;
                    output.Steps.Add(step);
                }

                var syncFailed = sync.Steps.Exists(s => !s.Success);
                output.Steps.Add(new VwSetupSceneStep
                {
                    Order = output.Steps.Count + 1,
                    Name = "Đồng bộ nguồn tín hiệu",
                    Method = "GET",
                    Endpoint = "/ISAPI/DisplayDev/Video/inputs/channels",
                    Success = !syncFailed,
                    Skipped = input.DryRun && !syncFailed,
                    Message = syncFailed
                        ? "Không đọc được kênh tín hiệu vào — SignalNo trong CSDL chưa được đối chiếu."
                        : input.DryRun
                            ? $"Chạy thử — {sync.Changes.Count} nguồn sẽ được cập nhật SignalNo."
                            : $"Đã cập nhật SignalNo cho {sync.UpdatedCount} nguồn."
                });

                if (syncFailed)
                    return Finish(output, "Đồng bộ nguồn tín hiệu thất bại — dừng trước mọi thao tác ghi.");
            }

            var snapshot = await Track(output.Steps, "Chụp cửa sổ hiện có", "GET", $"/ISAPI/DisplayDev/VideoWall/{wallNo}/windows",
                () => _deviceClient.GetWindowsAsync(controller, wallNo, ct));
            if (snapshot is { Success: true, Data: not null })
                output.WindowSnapshot = snapshot.Data.WallWindow;

            var windows = await LoadSceneWindows(scene.ID);
            var signalNoMap = await BuildSignalNoMap(windows);

            if (capabilities.Data?.MaxWindowNums == null)
            {
                output.Steps.Add(new VwSetupSceneStep
                {
                    Order = output.Steps.Count + 1,
                    Name = "Kiểm tra giới hạn cửa sổ",
                    Method = "CHECK",
                    Endpoint = "local:MaxWindowNums",
                    Success = true,
                    Message = "Thiết bị không khai maxWindowNums nên không kiểm được số lượng cửa sổ có nằm trong dải hợp lệ."
                });
            }
            else if (windows.Count > capabilities.Data.MaxWindowNums.Value)
            {
                return Finish(output, $"Số lượng cửa sổ ({windows.Count}) vượt quá giới hạn thiết bị hỗ trợ ({capabilities.Data.MaxWindowNums.Value}).");
            }

            await ApplyWindows(output, controller, scene, wallNo.Value, windows, signalNoMap, input, ct);
            if (!output.Steps.TrueForAll(s => s.Success))
                return Finish(output, "Có bước thất bại — xem chi tiết trong danh sách bước.");

            await ApplyScene(output, controller, scene, wallNo.Value, input, ct);

            return Finish(output, null);
        }

        private async Task<VwSetupSceneStep> SendPassthroughCore(VwISAPIPassthroughInput input, CancellationToken ct)
        {
            VwSetupSceneStep Reject(string message) => new()
            {
                Order = 1,
                Name = "Passthrough",
                Method = input.Method,
                Endpoint = input.Path,
                Success = false,
                Message = message
            };

            var allowedMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GET", "POST", "PUT", "DELETE" };
            if (!allowedMethods.Contains(input.Method))
                return Reject($"Method không hỗ trợ: {input.Method}. Chỉ chấp nhận GET, POST, PUT, DELETE.");

            var hasControllerId = !string.IsNullOrWhiteSpace(input.ControllerId);
            var hasDevice = input.Device != null && !string.IsNullOrWhiteSpace(input.Device.Ip);

            if (hasControllerId && hasDevice)
                return Reject("Chỉ được cung cấp ControllerId hoặc Device, không được cả hai.");

            if (!hasControllerId && !hasDevice)
                return Reject("Phải cung cấp ControllerId hoặc Device.");

            VwController controller;
            if (hasControllerId)
            {
                controller = await LoadController(input.ControllerId);
            }
            else
            {
                var device = input.Device!;
                var port = device.Port ?? VwDeviceProfile.DefaultPort;
                controller = new VwController
                {
                    ID = "adhoc-passthrough",
                    IP = device.Ip,
                    Port = port,
                    Account = device.Account,
                    PassWord = device.Password
                };
                // TODO: device.SkipPasswordCheck — nhận field cho khớp Postman contract nhưng chưa có
                // hành vi tương ứng trong code. Khi cần thật thì thêm logic bỏ qua xác thực ở đây.
            }

            var stopwatch = Stopwatch.StartNew();
            var result = await _deviceClient.SendRawAsync(
                controller,
                new HttpMethod(input.Method),
                input.Path,
                input.Body,
                input.ContentType,
                ct);
            stopwatch.Stop();

            return new VwSetupSceneStep
            {
                Order = 1,
                Name = "Passthrough",
                Method = input.Method,
                Endpoint = input.Path,
                Success = result.Success,
                HttpStatus = result.HttpStatusCode,
                Message = result.ErrorMessage,
                DurationMs = stopwatch.ElapsedMilliseconds,
                RequestXml = result.RawRequest,
                ResponseXml = result.RawResponse
            };
        }

        private async Task ApplyWindows(
            VwSetupSceneOutput output,
            VwController controller,
            VwScene scene,
            int wallNo,
            List<VwWindowScene> windows,
            Dictionary<string, int?> signalNoMap,
            VwSetupSceneInput input,
            CancellationToken ct)
        {
            var windowsEndpoint = $"/ISAPI/DisplayDev/VideoWall/{wallNo}/windows";

            if (!input.ResetWindows)
            {
                Skip(output.Steps, "Xoá cửa sổ cũ", "DELETE", windowsEndpoint,
                    "Bỏ qua theo cấu hình — lệnh này xoá trắng cả cửa sổ do người khác tạo.");
            }
            else if (input.DryRun)
            {
                Skip(output.Steps, "Xoá cửa sổ cũ", "DELETE", windowsEndpoint, "Chạy thử — không phát lệnh xoá.");
            }
            else
            {
                await Track(output.Steps, "Xoá cửa sổ cũ", "DELETE", windowsEndpoint,
                    () => _deviceClient.DeleteAllWindowsAsync(controller, wallNo, ct));
            }

            // Toạ độ trên master quy đổi theo cổng ra đã đồng bộ (VwControllerOutput) và bố cục đang áp dụng.
            var projection = await _regionService.GetMasterProjectionAsync();
            if (projection.Errors.Count > 0)
            {
                Fail(output.Steps, "Ánh xạ tường → master", "CHECK", "local:ControllerOutput", string.Join("; ", projection.Errors));
                return;
            }

            var pendingIdLookup = new List<(VwSetupWindowResult Result, VwWindowScene Window)>();

            foreach (var window in windows)
            {
                var rect = VwWallPlacement.MapWindow(projection, window.BlockNo,
                    window.X ?? 0, window.Y ?? 0, window.W ?? 0, window.H ?? 0,
                    PanelW, PanelH, out var mapError);
                if (rect == null)
                {
                    output.Windows.Add(BuildWindowResult(window, signalNoMap, 0, 0, 0, 0, false, mapError));
                    continue;
                }

                var (x, y, w, h) = rect.Value;
                var signalNo = ResolveSignalNo(window, signalNoMap);
                var request = BuildWindowRequest(x, y, w, h, signalNo);
                var payload = _deviceClient.SerializeToXml(request);
                var result = BuildWindowResult(window, signalNoMap, x, y, w, h, true, null);

                if (input.DryRun)
                {
                    output.Steps.Add(new VwSetupSceneStep
                    {
                        Order = output.Steps.Count + 1,
                        Name = $"Tạo cửa sổ '{window.Label ?? window.Name ?? window.ID}'",
                        Method = "POST",
                        Endpoint = windowsEndpoint,
                        Success = true,
                        Skipped = true,
                        Message = "Chạy thử — payload dưới đây là thứ sẽ được gửi.",
                        RequestXml = payload
                    });

                    result.Message = "Chạy thử — chưa tạo trên thiết bị.";
                    output.Windows.Add(result);
                    continue;
                }

                var addResult = await Track(output.Steps, $"Tạo cửa sổ '{window.Label ?? window.Name ?? window.ID}'",
                    "POST", windowsEndpoint, () => _deviceClient.AddWindowAsync(controller, request, wallNo, ct));

                result.Success = addResult.Success;
                result.Message = addResult.ErrorMessage;

                if (addResult is { Success: true, Data.Id: > 0 })
                {
                    result.DeviceWindowId = addResult.Data.Id.ToString();
                    // Ghi DeviceWindowId vào DB ngay — nhất quán với nhánh SyncSceneWindowsToDevice.
                    // Không gọi ở đây → VwWindowScene.DeviceWindowId bị mất khi thiết bị trả ID ngay lập tức.
                    await AssignDeviceWindowId(window, result.DeviceWindowId);
                }
                else if (addResult.Success)
                    pendingIdLookup.Add((result, window));

                output.Windows.Add(result);
            }

            await ResolveMissingDeviceWindowIdsForSetup(output, controller, wallNo, pendingIdLookup, input.DryRun, ct);
        }

        private async Task ApplyScene(
            VwSetupSceneOutput output,
            VwController controller,
            VwScene scene,
            int wallNo,
            VwSetupSceneInput input,
            CancellationToken ct)
        {
            // SID rỗng phải hiện thành bước ĐỎ, không phải bước bỏ qua. Trước đây nó được ghi là
            // Skip với Success = true nên toàn bộ kịch bản báo thành công trong khi dữ liệu nằm
            // nguyên trong CSDL và không có gì xuống tường — đúng kiểu hỏng im lặng khó lần ra nhất.
            if (string.IsNullOrWhiteSpace(scene.OutputId))
            {
                Fail(output.Steps, "Lưu kịch bản", "PUT", $"/ISAPI/DisplayDev/VideoWall/{wallNo}/scene/{{SID}}/saveData",
                    "Kịch bản chưa khai SID trên thiết bị (VwScene.OutputId) nên không lưu/kích hoạt được. Khai SID trong khoảng 1..maxSceneNums rồi chạy lại.");
                return;
            }

            // Ngoài khoảng thiết bị nhận thì saveData sẽ trả badParameters; chặn sớm để thông báo
            // nói rõ vì sao thay vì để người dùng đọc subStatusCode.
            if (output.MaxSceneNums == null)
            {
                output.Steps.Add(new VwSetupSceneStep
                {
                    Order = output.Steps.Count + 1,
                    Name = "Kiểm tra SID",
                    Method = "CHECK",
                    Endpoint = "local:SID",
                    Success = true,
                    Message = "Thiết bị không khai maxSceneNums nên không kiểm được SID có nằm trong dải hợp lệ."
                });
            }
            else if (!int.TryParse(scene.OutputId, out var sid) || sid < 1 || sid > output.MaxSceneNums.Value)
            {
                Fail(output.Steps, "Lưu kịch bản", "PUT", $"/ISAPI/DisplayDev/VideoWall/{wallNo}/scene/{scene.OutputId}/saveData",
                    $"SID '{scene.OutputId}' không hợp lệ — thiết bị chỉ nhận số nguyên trong khoảng 1..{output.MaxSceneNums.Value}.");
                return;
            }

            var saveEndpoint = $"/ISAPI/DisplayDev/VideoWall/{wallNo}/scene/{scene.OutputId}/saveData";
            var activateEndpoint = $"/ISAPI/DisplayDev/VideoWall/{wallNo}/scene/{scene.OutputId}/activate";

            if (input.DryRun)
            {
                Skip(output.Steps, "Lưu kịch bản", "PUT", saveEndpoint, "Chạy thử — không phát lệnh lưu.");
                Skip(output.Steps, "Kích hoạt kịch bản", "PUT", activateEndpoint, "Chạy thử — không phát lệnh kích hoạt.");
                return;
            }

            var save = await Track(output.Steps, "Lưu kịch bản", "PUT", saveEndpoint,
                () => _deviceClient.SaveSceneDataAsync(controller, scene.OutputId, wallNo, ct));
            if (!save.Success)
                return;

            if (!input.Activate)
            {
                Skip(output.Steps, "Kích hoạt kịch bản", "PUT", activateEndpoint,
                    "Bỏ qua theo cấu hình — đây là bước duy nhất làm đổi hình đang chiếu.");
                return;
            }

            var activate = await Track(output.Steps, "Kích hoạt kịch bản", "PUT", activateEndpoint,
                () => _deviceClient.ActivateSceneAsync(controller, scene.OutputId, wallNo, ct));
            if (!activate.Success)
                return;
        }

        private async Task ResolveMissingDeviceWindowIdsForSetup(
            VwSetupSceneOutput output,
            VwController controller,
            int wallNo,
            List<(VwSetupWindowResult Result, VwWindowScene Window)> pending,
            bool dryRun,
            CancellationToken ct)
        {
            if (dryRun || pending.Count == 0)
                return;

            var windowsResult = await Track(output.Steps, "Tra ID cửa sổ vừa tạo", "GET",
                $"/ISAPI/DisplayDev/VideoWall/{wallNo}/windows",
                () => _deviceClient.GetWindowsAsync(controller, wallNo, ct));

            if (windowsResult is not { Success: true, Data: not null })
                return;

            var taken = new HashSet<int>();

            foreach (var (result, window) in pending)
            {
                var match = MatchDeviceWindowByRect(
                    windowsResult.Data.WallWindow,
                    taken,
                    result.LocalX,
                    result.LocalY,
                    result.LocalW,
                    result.LocalH);

                if (match == null)
                {
                    result.Message = "Thiết bị không trả ID cửa sổ và cũng không tra ngược được theo toạ độ.";
                    continue;
                }

                taken.Add(match.Id);
                result.DeviceWindowId = match.Id.ToString();
                await AssignDeviceWindowId(window, match.Id.ToString());
            }

            _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);
        }


        /// <summary>
        /// Chọn tường và ghi lại danh sách tường đọc được. Luôn gọi GET danh sách tường kể cả khi
        /// đã có số hiệu từ cấu hình — đây là lệnh chỉ-đọc và nó cho biết ngay tường được chọn có
        /// thật sự đang gắn màn hình hay không.
        /// </summary>
        private async Task<int?> ResolveWall(
            List<VwSetupSceneStep> steps,
            VwController controller,
            int? requestedWallNo,
            List<VwWallSummary> walls,
            CancellationToken ct)
        {
            var wallStepIndex = steps.Count;
            var result = await Track(steps, "Danh sách tường", "GET", "/ISAPI/DisplayDev/VideoWall",
                () => _deviceClient.GetVideoWallsAsync(controller, ct));

            if (result is { Success: true, Data: not null })
            {
                walls.AddRange(result.Data.VideoWall.Select(w => new VwWallSummary
                {
                    Id = w.Id,
                    Name = w.Name,
                    BindOutputStatus = w.WallBindOutputStatus
                }));
            }

            try
            {
                var wallNo = await ResolveWall(controller, requestedWallNo, ct);
                foreach (var wall in walls.Where(w => w.Id == wallNo))
                    wall.Selected = true;

                return wallNo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "VwISAPIDeviceService: không xác định được tường cho {Controller}", controller.Name ?? controller.ID);
                steps[wallStepIndex].Success = false;
                steps[wallStepIndex].Message = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Cổng ra đã lưu (VwControllerOutput) của bộ điều khiển mà thiết bị không còn trả về, hoặc chưa đồng bộ.
        /// Chỉ đối chiếu — đồng bộ thật ở SyncOutputs.
        /// </summary>
        private async Task<List<VwDeviceMismatch>> CompareOutputs(string controllerId, List<VwISAPIOutputItem> outputs)
        {
            var saved = await _vwOutputRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.ControllerId == controllerId)
                .ToListAsync();

            var deviceOutputIds = outputs.Select(o => o.OutputId).ToHashSet();

            return saved
                .Where(o => o.DeviceOutputId == null || !deviceOutputIds.Contains(o.DeviceOutputId.Value))
                .Select(o => new VwDeviceMismatch
                {
                    EntityId = o.ID,
                    EntityName = o.DisplayName ?? o.Name,
                    Field = nameof(VwControllerOutput.DeviceOutputId),
                    DbValue = o.DeviceOutputId?.ToString(),
                    DeviceValue = string.Join(", ", deviceOutputIds),
                    Note = "Cổng ra đã lưu không khớp cổng nào của thiết bị — chạy Đồng bộ cổng ra."
                })
                .ToList();
        }

        private async Task<List<VwDeviceMismatch>> CompareSources(string controllerId, List<VwISAPIInputChannel> channels)
        {
            var sources = await _vwSourceRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .Where(u => u.ControllerId == controllerId)
                .ToListAsync();

            var deviceChannelIds = channels.Select(c => c.Id).ToHashSet();

            return sources
                .Where(s => !s.SignalNo.HasValue || !deviceChannelIds.Contains(s.SignalNo.Value))
                .Select(s => new VwDeviceMismatch
                {
                    EntityId = s.ID,
                    EntityName = s.Name,
                    Field = nameof(VwSource.SignalNo),
                    DbValue = s.SignalNo?.ToString(),
                    DeviceValue = string.Join(", ", deviceChannelIds),
                    Note = "SignalNo không khớp kênh vào nào — ID kênh của thiết bị là số đóng gói theo board/port, không phải 1..8."
                })
                .ToList();
        }

        private async Task<VwController> LoadController(string? controllerId)
        {
            if (string.IsNullOrWhiteSpace(controllerId))
                throw new InvalidOperationException(string.Format(BaseLocaleManager.BaseException.NotExist, BaseMsg.Vw.Entity.ControllerId));

            var controller = await _vwControllerRep.GetFirstAsync(u => u.IsDelete == null && u.ID == controllerId)
                ?? throw new InvalidOperationException(string.Format(BaseLocaleManager.BaseException.NotExist, BaseMsg.Vw.Entity.ControllerId));

            if (VwControllerTopology.IsSub(controller))
                throw new InvalidOperationException($"Bộ điều khiển [{controller.Name ?? controller.ID}] là bộ điều khiển CON — backend không gọi ISAPI trực tiếp tới bộ con (kiến trúc cascade DS-C66S).");

            if (string.IsNullOrWhiteSpace(controller.IP))
                throw new InvalidOperationException($"Bộ điều khiển [{controller.Name ?? controller.ID}] chưa đủ thông tin kết nối: chưa khai địa chỉ IP.");

            if (!_credentialResolver.HasCredential(controller))
                throw new InvalidOperationException($"Bộ điều khiển [{controller.Name ?? controller.ID}] chưa đủ thông tin kết nối: chưa có tài khoản Digest.");

            return controller;
        }

        private async Task<VwScene> LoadScene(VwSetupSceneInput input)
        {
            VwScene? scene;
            if (!string.IsNullOrWhiteSpace(input.SceneId))
                scene = await _vwSceneRep.GetFirstAsync(u => u.IsDelete == null && u.ID == input.SceneId);
            else if (!string.IsNullOrWhiteSpace(input.SceneCode))
                scene = await _vwSceneRep.GetFirstAsync(u => u.IsDelete == null && u.Code == input.SceneCode);
            else
                throw new InvalidOperationException(BaseMsg.Vw.Validation.SceneIdOrEventTypeIdRequired);

            return scene ?? throw new InvalidOperationException(string.Format(BaseLocaleManager.BaseException.NotExist, BaseMsg.Vw.Entity.SceneId));
        }

        private async Task<List<VwWindowScene>> LoadSceneWindows(string sceneId)
            => await _vwWindowSceneRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .Where(u => u.SceneId == sceneId)
                .OrderBy(u => u.ZIndex)
                .ToListAsync();

        private static VwSetupWindowResult BuildWindowResult(
            VwWindowScene window,
            Dictionary<string, int?> signalNoMap,
            int localX,
            int localY,
            int localW,
            int localH,
            bool success,
            string? message)
            => new()
            {
                WindowSceneId = window.ID,
                Label = window.Label ?? window.Name,
                SourceId = window.SourceId,
                SignalNo = ResolveSignalNo(window, signalNoMap),
                WallX = window.X ?? 0,
                WallY = window.Y ?? 0,
                WallW = window.W ?? 0,
                WallH = window.H ?? 0,
                LocalX = localX,
                LocalY = localY,
                LocalW = localW,
                LocalH = localH,
                Success = success,
                Message = message
            };

        private static VwSetupSceneOutput Finish(VwSetupSceneOutput output, string? message)
        {
            output.Success = output.Steps.TrueForAll(s => s.Success);

            // Không để thông báo mặc định "chạy xong" đè lên một kịch bản có bước đỏ: đó chính là
            // kiểu báo cáo làm người vận hành tin là đã đẩy được trong khi tường không đổi gì.
            output.Message = message ?? (output.Success
                ? output.DryRun
                    ? "Chạy thử hoàn tất — chưa có lệnh ghi nào được phát xuống thiết bị."
                    : "Kịch bản chạy xong."
                : "Có bước thất bại — xem chi tiết trong danh sách bước.");

            return output;
        }

        /// <summary>Bước bỏ qua theo cấu hình (DryRun, ResetWindows tắt...) — không tính là lỗi.</summary>
        private static void Skip(List<VwSetupSceneStep> steps, string name, string method, string endpoint, string reason)
            => AddStep(steps, name, method, endpoint, reason, success: true, skipped: true);

        /// <summary>
        /// Bước hỏng vì cấu hình phía hệ thống, chưa từng phát lệnh nào xuống thiết bị. Khác Skip ở
        /// chỗ Success = false nên kịch bản tổng báo thất bại thay vì lặng lẽ báo xong.
        /// </summary>
        private static void Fail(List<VwSetupSceneStep> steps, string name, string method, string endpoint, string reason)
            => AddStep(steps, name, method, endpoint, reason, success: false, skipped: false);

        private static void AddStep(List<VwSetupSceneStep> steps, string name, string method, string endpoint, string reason, bool success, bool skipped)
            => steps.Add(new VwSetupSceneStep
            {
                Order = steps.Count + 1,
                Name = name,
                Method = method,
                Endpoint = endpoint,
                Success = success,
                Skipped = skipped,
                Message = reason
            });

        private static async Task<VwISAPIResult<T>> Track<T>(
            List<VwSetupSceneStep> steps,
            string name,
            string method,
            string endpoint,
            Func<Task<VwISAPIResult<T>>> call) where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await call();
            stopwatch.Stop();

            steps.Add(BuildStep(steps.Count + 1, name, method, endpoint, result, stopwatch.ElapsedMilliseconds));
            return result;
        }

        private static async Task<VwISAPIResult> Track(
            List<VwSetupSceneStep> steps,
            string name,
            string method,
            string endpoint,
            Func<Task<VwISAPIResult>> call)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await call();
            stopwatch.Stop();

            steps.Add(BuildStep(steps.Count + 1, name, method, endpoint, result, stopwatch.ElapsedMilliseconds));
            return result;
        }

        private static VwSetupSceneStep BuildStep(
            int order,
            string name,
            string method,
            string endpoint,
            VwISAPIResult result,
            long durationMs)
            => new()
            {
                Order = order,
                Name = name,
                Method = method,
                Endpoint = endpoint,
                Success = result.Success,
                HttpStatus = result.HttpStatusCode,
                Message = result.ErrorMessage,
                DurationMs = durationMs,
                RequestXml = result.RawRequest,
                ResponseXml = result.RawResponse
            };

        private static VwISAPIWindowRequest BuildWindowRequest(int x, int y, int w, int h, int? signalNo)
        {
            var request = new VwISAPIWindowRequest
            {
                Rect = new VwISAPIRect
                {
                    Coordinate = new VwISAPICoordinate
                    {
                        X = x,
                        Y = y
                    },
                    Width = w,
                    Height = h
                }
            };

            if (!signalNo.HasValue)
                return request;

            // DS-C66S bỏ qua SubWindowList (tự gán "stream id" = 0 ⇒ cửa sổ trống) nếu thiếu windowMode + wndShowMode.
            // Khớp request WPF đã chạy được và cửa sổ đọc lại từ thiết bị (windowMode=1, wndShowMode=subWndMode) — 10/11/2026.
            request.WindowMode = 1;
            request.WindowModeSpecified = true;
            request.WndShowMode = "subWndMode";
            request.SubWindowList = new VwISAPISubWindowList
            {
                SubWindow =
                [
                    new VwISAPISubWindow
                    {
                        Id = 1,
                        SubWindowParam = new VwISAPISubWindowParam
                        {
                            VideoInputChannelId = signalNo.Value.ToString()
                        }
                    }
                ]
            };

            return request;
        }
    }
}
