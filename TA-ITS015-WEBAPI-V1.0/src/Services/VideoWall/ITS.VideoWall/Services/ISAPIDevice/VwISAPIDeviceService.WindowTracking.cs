using Microsoft.Extensions.Logging;
using ITS.VideoWall.Core.Dto.ISAPI;
using Module.VideoWall.Core.Entities;

namespace ITS.VideoWall.Services.ISAPIDevice
{
    public partial class VwISAPIDeviceService
    {
        /// <summary>
        /// Tra VWMWID cho những cửa sổ mà thiết bị không trả ID lúc tạo: đọc lại danh sách cửa sổ
        /// trên tường rồi ghép theo Rect. Mỗi ID chỉ nhận đúng một lần để hai cửa sổ trùng toạ độ
        /// không cùng trỏ vào một ID.
        /// </summary>
        private async Task ResolveMissingDeviceWindowIds(
            VwController controller,
            int wallNo,
            List<(VwWindowScene Window, int X, int Y, int W, int H)> pending,
            CancellationToken ct)
        {
            if (pending.Count == 0)
                return;

            var windowsResult = await _deviceClient.GetWindowsAsync(controller, wallNo, ct);
            if (!windowsResult.Success || windowsResult.Data == null)
            {
                _logger.LogWarning(
                    "VwISAPIDeviceService: không tra được ID cửa sổ trên {Controller}: {Error}",
                    controller.Name ?? controller.ID,
                    windowsResult.ErrorMessage);
                return;
            }

            var taken = new HashSet<int>();

            foreach (var (window, x, y, w, h) in pending)
            {
                var match = MatchDeviceWindowByRect(windowsResult.Data.WallWindow, taken, x, y, w, h);
                if (match == null)
                    continue;

                taken.Add(match.Id);
                await AssignDeviceWindowId(window, match.Id.ToString());
            }
        }

        private async Task AssignDeviceWindowId(VwWindowScene window, string deviceWindowId)
        {
            // TODO: VwWindowScene bỏ DeviceWindowId
            //window.DeviceWindowId = deviceWindowId;
            //if (string.IsNullOrWhiteSpace(window.ID))
            //    return;

            //await _vwWindowSceneRep.AsUpdateable()
            //    .SetColumns(w => w.DeviceWindowId == deviceWindowId)
            //    .Where(w => w.ID == window.ID)
            //    .ExecuteCommandAsync();
            await Task.CompletedTask;
        }

        private static VwISAPIWallWindowItem? MatchDeviceWindowByRect(
            IEnumerable<VwISAPIWallWindowItem>? wallWindows,
            HashSet<int> taken,
            int x,
            int y,
            int w,
            int h)
        {
            if (wallWindows == null)
                return null;

            var exact = wallWindows.FirstOrDefault(item =>
                !taken.Contains(item.Id)
                && item.Rect?.Coordinate != null
                && item.Rect.Coordinate.X == x
                && item.Rect.Coordinate.Y == y
                && item.Rect.Width == w
                && item.Rect.Height == h);

            if (exact != null)
                return exact;

            return wallWindows.FirstOrDefault(item =>
                !taken.Contains(item.Id)
                && item.Rect?.Coordinate != null
                && item.Rect.Coordinate.X == x
                && item.Rect.Coordinate.Y == y);
        }

        /// <summary>
        /// Xác định bộ điều khiển đích để đẩy lệnh ISAPI.
        ///
        /// Cascade mode (có bộ trung tâm — bộ có bộ con): chỉ trả bộ trung tâm — backend
        /// không bao giờ gửi lệnh scene/window cho bộ con.
        ///
        /// Standalone fallback: giữ nguyên logic cũ (kịch bản vùng → 1 bộ, toàn tường → tất cả).
        /// Created date: 08/09/2026
        /// </summary>
        private async Task<List<VwController>> ResolveTargetControllers(VwScene scene)
        {
            // Cascade: luôn trả bộ trung tâm duy nhất, bất kể scene vùng hay toàn tường.
            var center = await TryGetCenterController();
            if (center != null)
                return [center];

            // ── Standalone fallback ──
            // TODO: VwScene bỏ ControllerId
            //if (!string.IsNullOrWhiteSpace(scene.ControllerId))
            //{
            //    var singleCtrl = await _vwControllerRep.GetFirstAsync(u => u.IsDelete == null && u.ID == scene.ControllerId);
            //    if (singleCtrl == null || string.IsNullOrWhiteSpace(singleCtrl.IP))
            //        return [];
            //
            //    return [singleCtrl];
            //}

            // Bộ điều khiển có cổng ra đang nằm trên lưới tường
            var wallControllerIds = await _vwOutputRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.BlockNo != null)
                .Select(u => u.ControllerId)
                .Distinct()
                .ToListAsync();

            if (wallControllerIds.Count > 0)
            {

                return await _vwControllerRep.AsQueryable()
                    .Where(u => u.IsDelete == null)
                    .Where(u => u.IP != null && u.IP != string.Empty)
                    .Where(u => wallControllerIds.Contains(u.ID))
                    .ToListAsync();
            }

            return await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .Where(u => u.IP != null && u.IP != string.Empty)
                .ToListAsync();
        }

        /// <summary>
        /// Trả bộ điều khiển trung tâm (bộ được bộ khác trỏ tới làm cha) nếu tồn tại.
        /// Trả null nếu không bộ nào có bộ con (standalone mode).
        /// Created date: 08/09/2026
        /// </summary>
        private async Task<VwController?> TryGetCenterController()
        {
            return await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.IP != null && u.IP != string.Empty)
                .WhereIsCenter()
                .FirstAsync();
        }

        private async Task<Dictionary<string, int?>> BuildSignalNoMap(List<VwWindowScene> targetWindows)
        {
            var sourceIds = targetWindows
                .Where(w => !string.IsNullOrWhiteSpace(w.SourceId))
                .Select(w => w.SourceId!)
                .Distinct()
                .ToList();

            if (sourceIds.Count == 0)
                return [];

            var sources = await _vwSourceRep.AsQueryable().Where(u => sourceIds.Contains(u.ID)).ToListAsync();
            return sources.ToDictionary(s => s.ID, s => s.SignalNo);
        }

        private static int? ResolveSignalNo(VwWindowScene window, Dictionary<string, int?> signalNoMap)
        {
            if (string.IsNullOrWhiteSpace(window.SourceId))
                return null;

            return signalNoMap.TryGetValue(window.SourceId, out var signalNo) ? signalNo : null;
        }
    }
}

