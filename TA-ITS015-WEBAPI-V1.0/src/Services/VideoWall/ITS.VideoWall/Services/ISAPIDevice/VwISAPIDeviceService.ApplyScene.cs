using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Entities;
using Shared.DTO.Constants.Application;

namespace ITS.VideoWall.Services.ISAPIDevice
{
    /// <summary>
    /// Description: Áp dụng kịch bản lên tường (lệnh ACTIVATE_SCENE) — 10/11/2026, bỏ hash 12/11/2026.
    ///              DB là bản gốc; mỗi kịch bản có 1 ô trên bộ trung tâm (VwScene.OutputId = SID) — bản sao để web hãng
    ///              thấy / bật được và để isRunning chỉ đúng kịch bản. Mỗi lần Áp dụng LUÔN dựng lại từ DB (chấp nhận tường chớp):
    ///                1. Dựng kế hoạch từ DB: toạ độ quy đổi sang master + kênh nguồn + thứ tự lớp.
    ///                2. Xoá hết cửa sổ, dựng lại từ DB (tường đổi sang kịch bản).
    ///                3. Lưu vào ô SID của kịch bản (saveData chụp tường hiện tại) — ô không còn / tên khác tên kịch bản
    ///                   ⇒ tạo ô mới, không ghi đè ô của web hãng.
    ///                4. activate ô ⇒ isRunning = SID (saveData không đổi isRunning; activate ô cùng nội dung không chớp — thử 12/11).
    ///              Lưu / activate lỗi không làm hỏng lần Áp dụng (tường đã đúng) — chỉ ghi cảnh báo.
    /// Created date: 10/11/2026
    /// </summary>
    public partial class VwISAPIDeviceService
    {
        /// <summary>
        /// Tên kịch bản trên thiết bị tối đa 32 BYTE UTF-8 (scene/capabilities: name max=32). Đo 11/11/2026: tên 32 ký tự
        /// tiếng Việt = 36 byte ⇒ POST scene trả statusCode 4 invalidOperation ⇒ phải cắt theo byte.
        /// </summary>
        private const int DeviceSceneNameMaxLength = 32;

        /// <summary>Cắt chuỗi để số byte UTF-8 ≤ maxBytes, không cắt giữa ký tự; bỏ khoảng trắng cuối.</summary>
        private static string TruncateUtf8(string text, int maxBytes)
        {
            if (Encoding.UTF8.GetByteCount(text) <= maxBytes)
                return text;
            var length = text.Length;
            while (length > 0 && Encoding.UTF8.GetByteCount(text.AsSpan(0, length)) > maxBytes)
                length--;
            if (length > 0 && char.IsHighSurrogate(text[length - 1]))
                length--;
            return text[..length].TrimEnd();
        }

        /// <summary>Một cửa sổ đã quy đổi sang toạ độ uniform của master.</summary>
        private sealed record ScenePlanWindow(VwWindowScene Window, int X, int Y, int W, int H, int? SignalNo);

        /// <summary>
        /// Ghi nhật ký từng bước (VwEventTriggerLog, Action = ActiveScene) cả khi lỗi giữa đường (11/11/2026):
        /// trước đây luồng Áp dụng không ghi bước nào ⇒ không truy được vì sao lưu ô / dựng cửa sổ thất bại.
        /// </summary>
        public async Task ActivateScene(
            VwScene scene,
            List<VwController> controllers,
            List<string> targetControllerIds,
            CancellationToken ct = default)
        {
            var steps = new List<VwSetupSceneStep>();
            string? centerId = null;
            try
            {
                await ActivateSceneCore(scene, controllers, targetControllerIds, steps, id => centerId = id, ct);
            }
            catch (Exception ex)
            {
                Fail(steps, VwEventTriggerAction.FailedStepName, "APPLY", "local:ActivateScene", ex.Message);
                throw;
            }
            finally
            {
                await _eventTriggerLogService.WriteSteps(VwEventTriggerAction.ActiveScene, centerId, scene.ID, scene.Code, steps, ct);
            }
        }

        private async Task ActivateSceneCore(
            VwScene scene,
            List<VwController> controllers,
            List<string> targetControllerIds,
            List<VwSetupSceneStep> steps,
            Action<string> setCenterId,
            CancellationToken ct)
        {
            // 1. Bộ trung tâm (master) — mọi lệnh dựng / lưu / gọi lại kịch bản chỉ gửi bộ này
            var centerIds = VwControllerTopology.GetCenterIds(controllers);
            var center = controllers.FirstOrDefault(u => centerIds.Contains(u.ID))
                         ?? controllers.FirstOrDefault(u => u.ID == targetControllerIds?.FirstOrDefault())
                         ?? throw new InvalidOperationException("Chưa cấu hình bộ điều khiển trung tâm.");
            setCenterId(center.ID);
            var label = center.Name ?? center.ID;

            var caps = await Track(steps, $"[{label}] Năng lực Video Wall", "GET", "/ISAPI/DisplayDev/VideoWall/capabilities",
                () => _deviceClient.GetCapabilitiesAsync(center, ct));
            if (!caps.Success || caps.Data == null)
                throw new InvalidOperationException(caps.ErrorMessage ?? $"Không đọc được năng lực Video Wall của bộ điều khiển [{label}].");
            var supportScene = caps.Data.IsSupportScene == true;

            var wallNo = await ResolveWall(center, null, ct);

            // 2. Kế hoạch từ DB
            var plan = await BuildScenePlanAsync(scene);

            // 3. Dựng lại từ DB lên tường
            await RebuildWallAsync(center, wallNo, plan, steps, ct);
            _logger.LogInformation("ActivateScene: dựng {Count} cửa sổ cho [{Scene}] trên [{Controller}]",
                plan.Count, scene.Code ?? scene.ID, label);

            // 4. Lưu vào ô kịch bản + activate để isRunning đúng
            if (!supportScene)
            {
                _logger.LogWarning("ActivateScene: bộ điều khiển [{Controller}] không hỗ trợ kịch bản — chỉ dựng trực tiếp, không lưu.", label);
                return;
            }

            await SaveToDeviceSceneAsync(center, wallNo, scene, steps, ct);
        }

        /// <summary>Cửa sổ của kịch bản theo thứ tự lớp (ZIndex, rồi ID để thứ tự cố định) ⇒ toạ độ master + kênh nguồn.</summary>
        private async Task<List<ScenePlanWindow>> BuildScenePlanAsync(VwScene scene)
        {
            var windows = await _vwWindowSceneRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.SceneId == scene.ID)
                .OrderBy(u => u.ZIndex)
                .OrderBy(u => u.ID)
                .ToListAsync();

            var projection = await _regionService.GetMasterProjectionAsync();
            if (projection.Errors.Count > 0)
                throw new InvalidOperationException(string.Join("; ", projection.Errors));

            var signalNoMap = await BuildSignalNoMap(windows);
            var plan = new List<ScenePlanWindow>(windows.Count);
            foreach (var w in windows)
            {
                var rect = VwWallPlacement.MapWindow(projection, w.BlockNo,
                    w.X ?? 0, w.Y ?? 0, w.W ?? 0, w.H ?? 0, PanelW, PanelH, out var mapError)
                    ?? throw new InvalidOperationException($"Cửa sổ '{w.Label ?? w.Name ?? w.ID}': {mapError}");

                plan.Add(new ScenePlanWindow(w, rect.X, rect.Y, rect.W, rect.H, ResolveSignalNo(w, signalNoMap)));
            }

            return plan;
        }

        /// <summary>Xoá hết cửa sổ trên tường rồi thêm lần lượt theo thứ tự lớp (cửa sổ thêm sau nằm trên).</summary>
        private async Task RebuildWallAsync(VwController center, int wallNo, List<ScenePlanWindow> plan, List<VwSetupSceneStep> steps, CancellationToken ct)
        {
            var label = center.Name ?? center.ID;

            var del = await Track(steps, $"[{label}] Xoá cửa sổ trên tường {wallNo}", "DELETE", $"/ISAPI/DisplayDev/VideoWall/{wallNo}/windows",
                () => _deviceClient.DeleteAllWindowsAsync(center, wallNo, ct));
            if (!del.Success)
                throw new InvalidOperationException(del.ErrorMessage ?? $"Không xoá được cửa sổ trên bộ điều khiển [{label}].");

            var pendingIdLookup = new List<(VwWindowScene Window, int X, int Y, int W, int H)>();
            foreach (var p in plan)
            {
                var add = await Track(steps, $"[{label}] Thêm cửa sổ '{p.Window.Label ?? p.Window.Name ?? p.Window.ID}' ({p.X},{p.Y} {p.W}×{p.H}, kênh {p.SignalNo?.ToString() ?? "—"})",
                    "POST", $"/ISAPI/DisplayDev/VideoWall/{wallNo}/windows",
                    () => _deviceClient.AddWindowAsync(center, BuildWindowRequest(p.X, p.Y, p.W, p.H, p.SignalNo), wallNo, ct));
                if (!add.Success)
                    throw new InvalidOperationException(add.ErrorMessage ?? $"Không tạo được cửa sổ '{p.Window.Label ?? p.Window.Name ?? p.Window.ID}' trên bộ điều khiển [{label}].");

                if (add.Data?.Id > 0)
                    await AssignDeviceWindowId(p.Window, add.Data.Id.ToString());
                else
                    pendingIdLookup.Add((p.Window, p.X, p.Y, p.W, p.H));
            }

            await ResolveMissingDeviceWindowIds(center, wallNo, pendingIdLookup, ct);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);
        }

        /// <summary>
        /// Lưu tường hiện tại (vừa dựng) vào ô SID của kịch bản rồi activate ô đó.
        /// Dùng lại ô khi còn trên thiết bị VÀ tên ô = tên kịch bản (cắt 32 byte); ngược lại (chưa có / bị xoá / SID đã thuộc
        /// kịch bản khác của web hãng) ⇒ tạo ô mới. Không đọc được danh sách ô ⇒ bỏ qua lưu (tránh tạo ô trùng khi lỗi mạng).
        /// </summary>
        private async Task SaveToDeviceSceneAsync(VwController center, int wallNo, VwScene scene, List<VwSetupSceneStep> steps, CancellationToken ct)
        {
            var sceneLabel = scene.Code ?? scene.ID;
            var label = center.Name ?? center.ID;
            var name = TruncateUtf8(scene.Name ?? scene.Code ?? scene.ID, DeviceSceneNameMaxLength);

            var list = await Track(steps, $"[{label}] Danh sách ô kịch bản", "GET", $"/ISAPI/DisplayDev/VideoWall/{wallNo}/scene",
                () => _deviceClient.GetScenesAsync(center, wallNo, ct));
            if (!list.Success || list.Data == null)
            {
                _logger.LogWarning("ActivateScene: không đọc được danh sách kịch bản trên thiết bị ({Error}) — bỏ qua lưu [{Scene}]",
                    list.ErrorMessage, sceneLabel);
                return;
            }

            var sid = scene.OutputId;
            var slot = string.IsNullOrWhiteSpace(sid) ? null : list.Data.WallScene.FirstOrDefault(s => s.Id.ToString() == sid);
            if (slot == null || !string.Equals(slot.Name?.Trim(), name.Trim(), StringComparison.Ordinal))
            {
                if (slot != null)
                    _logger.LogWarning("ActivateScene: ô {Sid} trên thiết bị tên '{SlotName}' ≠ '{Name}' — không ghi đè, tạo ô mới cho [{Scene}]",
                        sid, slot.Name, name, sceneLabel);

                var created = await Track(steps, $"[{label}] Tạo ô kịch bản '{name}'", "POST", $"/ISAPI/DisplayDev/VideoWall/{wallNo}/scene",
                    () => _deviceClient.CreateSceneAsync(center, name, wallNo, ct));
                if (!created.Success || created.Data == null || created.Data.Id <= 0)
                {
                    _logger.LogWarning("ActivateScene: không tạo được ô kịch bản trên thiết bị ({Error}) — bỏ qua lưu [{Scene}]",
                        created.ErrorMessage, sceneLabel);
                    return;
                }

                sid = created.Data.Id.ToString();
            }

            var save = await Track(steps, $"[{label}] Lưu tường vào ô kịch bản {sid}", "PUT", $"/ISAPI/DisplayDev/VideoWall/{wallNo}/scene/{sid}/saveData",
                () => _deviceClient.SaveSceneDataAsync(center, sid, wallNo, ct));
            if (!save.Success)
            {
                _logger.LogWarning("ActivateScene: lưu ô kịch bản {Sid} lỗi ({Error}) [{Scene}]", sid, save.ErrorMessage, sceneLabel);
                return;
            }

            if (scene.OutputId != sid)
            {
                await _vwSceneRep.AsUpdateable()
                    .SetColumns(s => new VwScene { OutputId = sid })
                    .Where(s => s.ID == scene.ID)
                    .ExecuteCommandAsync();
                scene.OutputId = sid;
                _cache.RemoveByPrefixKey(CacheConst.Vw.VwScene);
            }

            var activate = await Track(steps, $"[{label}] Bật ô kịch bản {sid}", "PUT", $"/ISAPI/DisplayDev/VideoWall/{wallNo}/scene/{sid}/activate",
                () => _deviceClient.ActivateSceneAsync(center, sid, wallNo, ct));
            if (!activate.Success)
                _logger.LogWarning("ActivateScene: bật ô {Sid} lỗi ({Error}) — tường đã đúng, isRunning có thể chưa trỏ ô này [{Scene}]",
                    sid, activate.ErrorMessage, sceneLabel);

            _logger.LogInformation("ActivateScene: đã lưu [{Scene}] vào ô kịch bản {Sid}", sceneLabel, sid);
        }
    }
}
