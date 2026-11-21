using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.WindowScene;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Exceptions;
using Module.VideoWall.Core.Interfaces;
using Module.VideoWall.Core.Options;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Services;

namespace ITS.VideoWall.Services.ISAPIDevice
{
    /// <summary>
    /// Tầng nghiệp vụ điều khiển và đấu nối thiết bị Video Wall. Gánh hai trách nhiệm:
    ///   1. Điều khiển thiết bị cho luồng CRUD scene/window: kích hoạt kịch bản, đẩy lại cửa sổ,
    ///      đổi nguồn/lớp, và giữ bộ đếm lỗi xác thực thiết bị chống khoá IP.
    ///   2. Kịch bản đấu nối 8 bước cho nhóm API vwdevicesetup: khảo sát an toàn, đối chiếu DB
    ///      với thiết bị, đồng bộ SignalNo, và dựng kịch bản hiển thị có ghi nhận timeline.
    ///
    /// Vòng đời Scoped: dịch vụ dùng repository SqlSugar theo request, mà SqlSugarClient không an
    /// toàn đa luồng nên không được chia sẻ giữa các request. Trạng thái lỗi xác thực thiết bị vẫn sống xuyên
    /// suốt ứng dụng vì nó nằm trong BaseCacheService chứ không nằm ở field của instance này.
    /// </summary>
    public partial class VwISAPIDeviceService : IVwISAPIDeviceService
    {
        private readonly IVwISAPIDeviceClient _deviceClient;
        private readonly BaseRepository<VwController> _vwControllerRep;
        private readonly BaseRepository<VwScene> _vwSceneRep;
        private readonly BaseRepository<VwWindowScene> _vwWindowSceneRep;
        private readonly BaseRepository<VwSource> _vwSourceRep;
        private readonly BaseRepository<VwScreen> _vwScreenRep;
        private readonly BaseRepository<VwControllerOutput> _vwOutputRep;
        private readonly IVwSceneRegionService _regionService;
        private readonly VwISAPICredentialResolver _credentialResolver;
        private readonly BaseCacheService _cache;
        private readonly IVwEventTriggerLogService _eventTriggerLogService;
        private readonly IOptions<VwDeviceOptions> _deviceOptions;
        private readonly ILogger<VwISAPIDeviceService> _logger;
        private readonly IVwNatsPublisher? _publisher;

        public VwISAPIDeviceService(
            IVwISAPIDeviceClient deviceClient,
            BaseRepository<VwController> vwControllerRep,
            BaseRepository<VwScene> vwSceneRep,
            BaseRepository<VwWindowScene> vwWindowSceneRep,
            BaseRepository<VwSource> vwSourceRep,
            BaseRepository<VwScreen> vwScreenRep,
            BaseRepository<VwControllerOutput> vwOutputRep,
            IVwSceneRegionService regionService,
            VwISAPICredentialResolver credentialResolver,
            BaseCacheService cache,
            IVwEventTriggerLogService eventTriggerLogService,
            IOptions<VwDeviceOptions> deviceOptions,
            ILogger<VwISAPIDeviceService> logger,
            IVwNatsPublisher? publisher = null)
        {
            _deviceClient = deviceClient;
            _vwControllerRep = vwControllerRep;
            _vwSceneRep = vwSceneRep;
            _vwWindowSceneRep = vwWindowSceneRep;
            _vwSourceRep = vwSourceRep;
            _vwScreenRep = vwScreenRep;
            _vwOutputRep = vwOutputRep;
            _regionService = regionService;
            _credentialResolver = credentialResolver;
            _cache = cache;
            _eventTriggerLogService = eventTriggerLogService;
            _deviceOptions = deviceOptions;
            _logger = logger;
            _publisher = publisher;
        }


        public async Task<VwISAPIInputChannelsResponse> GetInputChannels(VwController controller, CancellationToken ct = default)
        {
            var result = await _deviceClient.GetInputChannelsAsync(controller, ct);
            if (!result.Success || result.Data == null)
                throw new InvalidOperationException(result.ErrorMessage ?? $"Failed to query input channels from controller '{controller.Name}'");

            return result.Data;
        }

        public async Task<VwController> RequireCenterController(CancellationToken ct = default)
        {
            var centers = await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .WhereIsCenter()
                .ToListAsync();

            if (centers.Count == 0)
                throw new InvalidOperationException("Chưa cấu hình bộ điều khiển trung tâm (bộ điều khiển có bộ con).");

            if (centers.Count > 1)
                throw new InvalidOperationException("Có >1 bộ điều khiển trung tâm (nhiều bộ đang có bộ con).");

            var c = centers[0];
            if (string.IsNullOrWhiteSpace(c.IP))
                throw new InvalidOperationException("Bộ điều khiển trung tâm chưa khai IP.");

            return c;
        }

        public Task<VwController> GetCenterController(CancellationToken ct = default)
            => RequireCenterController(ct);

        public Task<VwController> GetCenterControllerAsync(CancellationToken ct = default)
            => RequireCenterController(ct);

        public async Task SyncSceneWindowsToDevice(
            string? sceneId,
            VwWindowScene? extraWindow = null,
            string? excludeWindowId = null,
            HashSet<string>? excludeWindowIds = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sceneId))
                return;

            var scene = await _vwSceneRep.GetFirstAsync(u => u.IsDelete == null && u.ID == sceneId);
            if (scene == null)
                return;

            var targetControllers = await ResolveTargetControllers(scene);
            if (targetControllers.Count == 0)
                return;

            var dbWindows = await _vwWindowSceneRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.SceneId == sceneId)
                .OrderBy(u => u.ZIndex)
                .ToListAsync();

            var targetWindows = new List<VwWindowScene>();
            foreach (var w in dbWindows)
            {
                if (!string.IsNullOrWhiteSpace(excludeWindowId) && w.ID == excludeWindowId)
                    continue;
                if (excludeWindowIds != null && excludeWindowIds.Contains(w.ID))
                    continue;
                if (extraWindow != null && w.ID == extraWindow.ID)
                    continue;

                targetWindows.Add(w);
            }

            if (extraWindow != null)
                targetWindows.Add(extraWindow);

            var signalNoMap = await BuildSignalNoMap(targetWindows);

            // Toạ độ trên master quy đổi theo cổng ra đã đồng bộ (VwControllerOutput) và bố cục đang áp dụng.
            var projection = await _regionService.GetMasterProjectionAsync();
            if (projection.Errors.Count > 0)
                throw new InvalidOperationException(string.Join("; ", projection.Errors));

            foreach (var ctrl in targetControllers)
            {
                var capResult = await _deviceClient.GetCapabilitiesAsync(ctrl, ct);
                if (!capResult.Success || capResult.Data == null)
                    throw new InvalidOperationException(capResult.ErrorMessage ?? $"Failed to get capabilities from controller '{ctrl.Name}'");

                if (capResult.Data.IsSupportScene == false)
                    throw new InvalidOperationException($"Controller '{ctrl.Name}' does not support scene capabilities.");

                var wallNos = await ResolveWalls(ctrl, ct);

                foreach (var wallNo in wallNos)
                {
                    var delResult = await _deviceClient.DeleteAllWindowsAsync(ctrl, wallNo, ct);
                    if (!delResult.Success)
                        throw new InvalidOperationException(delResult.ErrorMessage ?? $"Failed to delete windows on controller '{ctrl.Name}'");

                    var pendingIdLookup = new List<(VwWindowScene Window, int X, int Y, int W, int H)>();

                    foreach (var window in targetWindows)
                    {
                        var rect = VwWallPlacement.MapWindow(projection, window.BlockNo,
                            window.X ?? 0, window.Y ?? 0, window.W ?? 0, window.H ?? 0,
                            PanelW, PanelH, out var mapError);
                        if (rect == null)
                            throw new InvalidOperationException($"Cửa sổ '{window.Label ?? window.Name ?? window.ID}': {mapError}");

                        var signalNo = ResolveSignalNo(window, signalNoMap);
                        var windowReq = BuildWindowRequest(rect.Value.X, rect.Value.Y, rect.Value.W, rect.Value.H, signalNo);
                        var addResult = await _deviceClient.AddWindowAsync(ctrl, windowReq, wallNo, ct);

                        if (!addResult.Success)
                            throw new InvalidOperationException(addResult.ErrorMessage ?? $"Failed to add window on controller '{ctrl.Name}'");

                        if (addResult.Data?.Id > 0)
                            await AssignDeviceWindowId(window, addResult.Data.Id.ToString());
                        else
                            pendingIdLookup.Add((window, rect.Value.X, rect.Value.Y, rect.Value.W, rect.Value.H));
                    }

                    await ResolveMissingDeviceWindowIds(ctrl, wallNo, pendingIdLookup, ct);

                    if (!string.IsNullOrWhiteSpace(scene.OutputId))
                    {
                        var saveResult = await _deviceClient.SaveSceneDataAsync(ctrl, scene.OutputId, wallNo, ct);
                        if (!saveResult.Success)
                        {
                            _logger.LogWarning("SaveSceneDataAsync failed for {Name}: {Error}. Attempting CreateSceneAsync fallback...",
                                ctrl.Name, saveResult.ErrorMessage);

                            var createResult = await _deviceClient.CreateSceneAsync(ctrl, scene.Name ?? scene.Code ?? scene.ID, wallNo, ct);
                            if (createResult.Success && createResult.Data?.Id > 0)
                            {
                                scene.OutputId = createResult.Data.Id.ToString();
                                await _vwSceneRep.AsUpdateable()
                                    .SetColumns(s => s.OutputId == scene.OutputId)
                                    .Where(s => s.ID == scene.ID)
                                    .ExecuteCommandAsync();

                                var retrySave = await _deviceClient.SaveSceneDataAsync(ctrl, scene.OutputId, wallNo, ct);
                                if (!retrySave.Success)
                                    throw new InvalidOperationException(retrySave.ErrorMessage ?? $"Failed to save scene data on controller '{ctrl.Name}' after create");
                            }
                            else
                            {
                                throw new InvalidOperationException(saveResult.ErrorMessage ?? $"Failed to save scene data on controller '{ctrl.Name}'");
                            }
                        }
                    }
                }
            }

            _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);
        }

        /// <summary>Kích thước một màn trong toạ độ cửa sổ DB (mặc định 3840×2160).</summary>
        private int PanelW =>
            _deviceOptions.Value?.Device?.Profile?.PanelWidthPx is int val and > 0 ? val : VwWallProfile.PanelWidthPx;

        private int PanelH =>
            _deviceOptions.Value?.Device?.Profile?.PanelHeightPx is int val and > 0 ? val : VwWallProfile.PanelHeightPx;

        public async Task SetWindowLayer(
            VwController controller,
            string deviceWindowId,
            VwWindowLayerAction action,
            CancellationToken ct = default)
        {
            var wallNo = await ResolveWall(controller, null, ct);
            var result = action == VwWindowLayerAction.Top
                ? await _deviceClient.BringWindowToTopAsync(controller, deviceWindowId, wallNo, ct)
                : await _deviceClient.SendWindowToBottomAsync(controller, deviceWindowId, wallNo, ct);

            if (!result.Success)
                throw new InvalidOperationException(result.ErrorMessage ?? $"Failed to set layer '{action}' on controller '{controller.Name}'");
        }

        public bool IsDeviceAuthFailureBlocked(string? ipOrKey, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(ipOrKey))
                return false;

            var state = _cache.Get<VwDeviceAuthFailureState>(GetDeviceAuthFailureKey(ipOrKey));
            if (state == null || state.FailureCount < EffectiveMaxConsecutiveFailures)
                return false;

            var blockedUntil = state.BlockedUntilTicks > 0
                ? new DateTime(state.BlockedUntilTicks, DateTimeKind.Utc)
                : DateTime.UtcNow.Add(EffectiveBlockDuration);

            remaining = blockedUntil - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                ResetDeviceAuthFailure(ipOrKey);
                remaining = TimeSpan.Zero;
                return false;
            }

            return true;
        }

        public void RecordDeviceAuthFailure(string? ipOrKey, int statusCode)
        {
            if (string.IsNullOrWhiteSpace(ipOrKey))
                return;

            if (statusCode != 401 && statusCode != 403)
                return;

            var key = GetDeviceAuthFailureKey(ipOrKey);
            var state = _cache.Get<VwDeviceAuthFailureState>(key) ?? new VwDeviceAuthFailureState();
            state.FailureCount++;
            var blockDuration = EffectiveBlockDuration;
            var maxFailures = EffectiveMaxConsecutiveFailures;

            if (state.FailureCount >= maxFailures)
                state.BlockedUntilTicks = DateTime.UtcNow.Add(blockDuration).Ticks;

            _cache.Set(key, state, blockDuration);

            _logger.LogWarning("VwISAPIDeviceService: Ghi nhận lỗi xác thực ({StatusCode}) cho {Target}. Lỗi tích luỹ: {Count}/{Max}",
                statusCode, ipOrKey, state.FailureCount, maxFailures);
        }

        public void RecordAuthSuccess(string? ipOrKey)
        {
            if (string.IsNullOrWhiteSpace(ipOrKey))
                return;

            _cache.Remove(GetDeviceAuthFailureKey(ipOrKey));
        }

        public void ResetDeviceAuthFailure(string? ipOrKey)
        {
            if (string.IsNullOrWhiteSpace(ipOrKey))
                return;

            _cache.Remove(GetDeviceAuthFailureKey(ipOrKey));
            _logger.LogInformation("VwISAPIDeviceService: Đã reset trạng thái lỗi xác thực cho {Target}", ipOrKey);
        }

        public void ResetAllDeviceAuthFailures()
        {
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwDeviceAuthFailure);
            _logger.LogInformation("VwISAPIDeviceService: Đã reset toàn bộ trạng thái lỗi xác thực");
        }

        private int EffectiveMaxConsecutiveFailures =>
            _deviceOptions.Value?.Device?.Profile?.MaxConsecutiveFailures is int val and > 0
                ? val
                : VwWallProfile.MaxConsecutiveFailures;

        private TimeSpan EffectiveBlockDuration =>
            TimeSpan.FromMinutes(_deviceOptions.Value?.Device?.Profile?.BlockMinutes is int val and > 0
                ? val
                : VwWallProfile.BlockMinutes);

        private static string GetDeviceAuthFailureKey(string ipOrKey) => $"{CacheConst.Vw.VwDeviceAuthFailure}:{ipOrKey}";
    }
}

