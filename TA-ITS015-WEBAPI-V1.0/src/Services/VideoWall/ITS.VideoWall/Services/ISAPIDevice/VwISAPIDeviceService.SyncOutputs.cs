using ITS.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Helper;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace ITS.VideoWall.Services.ISAPIDevice
{
    /// <summary>
    /// Description: Đồng bộ từ thiết bị về CSDL (chỉ đọc thiết bị, không ghi xuống), 2 lệnh riêng:
    ///              - SyncOutputs, mỗi bộ điều khiển (trung tâm + con): GET .../VideoWall/{wall}/outputs (bảng gán cổng → ô)
    ///                và /Video/outputs/channels (tên cổng) ⇒ VwControllerOutput (không đổi vị trí ô:
    ///                người dùng gán tay).
    ///              - SyncScreens (bộ con): POST .../ScreenCtrl/GetWallScreenInfo?format=json (body = rect các cổng đã lưu, 11/11/2026) ⇒ chỉ ghi thông số vào màn mà người dùng
    ///                đã gán cho cổng (ghép cổng theo rect). Không tạo / gán / tháo màn (07/11/2026).
    ///              Đọc bộ con là được phép (06/11/2026); lệnh dựng kịch bản vẫn chỉ gửi bộ trung tâm.
    /// Created date: 06/11/2026
    /// </summary>
    public partial class VwISAPIDeviceService
    {
        public Task<VwSyncOutputsOutput> SyncOutputs(VwSyncOutputsInput input, CancellationToken ct = default) =>
            TrackAndLog(VwEventTriggerAction.SyncOutputs,
                () => SyncOutputsCore(input, ct),
                output => (input.ID, (string?)null, (string?)null, (IReadOnlyList<VwSetupSceneStep>)output.Steps),
                (input.ID, null, null),
                ct);

        private async Task<VwSyncOutputsOutput> SyncOutputsCore(VwSyncOutputsInput input, CancellationToken ct)
        {
            var output = new VwSyncOutputsOutput();

            var all = await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.IP != null && u.IP != string.Empty)
                .ToListAsync();
            var targets = string.IsNullOrWhiteSpace(input.ID)
                ? all.OrderBy(u => u.ParentControllerId != null).ThenBy(u => u.Code).ToList()
                : all.Where(u => u.ID == input.ID).ToList();
            if (targets.Count == 0)
                throw new InvalidOperationException($"Không có bộ điều khiển nào (có IP) để đồng bộ{(string.IsNullOrWhiteSpace(input.ID) ? "" : $" [{input.ID}]")}.");

            foreach (var controller in targets)
            {
                var result = new VwSyncOutputsControllerResult { ControllerId = controller.ID, ControllerName = controller.Name };
                output.Controllers.Add(result);
                try
                {
                    await SyncControllerOutputsAsync(controller, input.WallNo, output.Steps, result, ct);
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    // Một bộ lỗi không chặn các bộ còn lại
                    _logger.LogWarning(ex, "SyncOutputs: lỗi đồng bộ {Controller}", controller.Name ?? controller.ID);
                    result.Message = ex.Message;
                    Fail(output.Steps, $"Đồng bộ [{controller.Name ?? controller.ID}]", "SYNC", "local:ControllerOutput", ex.Message);
                }
            }

            // Vị trí cổng trên ô do người dùng gán tay ⇒ đồng bộ không đổi vị trí
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwControllerOutput);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);
            return output;
        }

        /// <summary>Đồng bộ cổng ra của một bộ điều khiển.</summary>
        private async Task SyncControllerOutputsAsync(
            VwController controller, int? requestedWallNo, List<VwSetupSceneStep> steps,
            VwSyncOutputsControllerResult result, CancellationToken ct)
        {
            var label = controller.Name ?? controller.ID;

            // 1. Kích thước ô uniform (baseOutputSize) — quy rect ra cột / hàng
            var caps = await Track(steps, $"[{label}] Năng lực Video Wall", "GET", "/ISAPI/DisplayDev/VideoWall/capabilities",
                () => _deviceClient.GetCapabilitiesAsync(controller, ct));
            var bos = caps.Data?.BaseOutputSize is int b and > 0 ? b : VwWallProfile.BaseOutputSize;

            // 2. Tường: theo yêu cầu ⇒ tự dò (GetWallScreenInfo cần rect của cổng ⇒ không dùng để chọn tường được)
            var wallNo = requestedWallNo is > 0
                ? requestedWallNo.Value
                : await ResolveWall(controller, null, ct);
            result.WallNo = wallNo;

            // 3. Bảng gán cổng → ô
            var wallOutputs = await Track(steps, $"[{label}] Cổng ra của tường {wallNo}", "GET", $"/ISAPI/DisplayDev/VideoWall/{wallNo}/outputs",
                () => _deviceClient.GetOutputsAsync(controller, wallNo, ct));
            if (wallOutputs is not { Success: true, Data: not null })
                throw new InvalidOperationException(wallOutputs.ErrorMessage ?? "Không đọc được cổng ra của tường.");

            // 4. Tên cổng / board (không bắt buộc)
            var channels = await Track(steps, $"[{label}] Kênh cổng ra", "GET", "/ISAPI/DisplayDev/Video/outputs/channels",
                () => _deviceClient.GetOutputChannelsAsync(controller, ct));
            var channelById = channels.Data?.VideoOutputChannel.GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First()) ?? [];

            // 5. Ghi VwControllerOutput: khớp theo outputID, không có thì theo ô (dữ liệu dựng tay / outputID đổi)
            var now = DateTime.Now;
            var existing = await _vwOutputRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.ControllerId == controller.ID)
                .ToListAsync();
            var seen = new HashSet<string>();

            foreach (var item in wallOutputs.Data.WallOutput)
            {
                var x = item.Rect?.Coordinate?.X ?? 0;
                var y = item.Rect?.Coordinate?.Y ?? 0;
                var col = x / bos;
                var row = y / bos;

                var entity = existing.FirstOrDefault(u => u.DeviceOutputId == item.OutputId && !seen.Contains(u.ID))
                             ?? existing.FirstOrDefault(u => !seen.Contains(u.ID) && u.LocalCol == col && u.LocalRow == row
                                                             && (u.DeviceOutputId == null || u.DeviceState == VwOutputDeviceState.Seed));
                var isNew = entity == null;
                entity ??= new VwControllerOutput { ControllerId = controller.ID, Status = BaseEnums.StatusEnum.Enable };

                channelById.TryGetValue(item.OutputId, out var channel);
                entity.DeviceOutputId = item.OutputId;
                entity.Name = channel?.Name ?? entity.Name ?? $"Output {item.OutputId}";
                entity.PortType = channel?.PortType ?? entity.PortType;
                entity.BoardNo = channel?.PortInBoard?.BoardId ?? entity.BoardNo;
                entity.PortNo = channel?.PortInBoard?.PortId ?? entity.PortNo;
                entity.WallNo = wallNo;
                entity.RectX = x;
                entity.RectY = y;
                entity.RectW = item.Rect?.Width;
                entity.RectH = item.Rect?.Height;
                entity.LocalCol = col;
                entity.LocalRow = row;
                entity.DeviceState = VwOutputDeviceState.Present;
                entity.LastSyncedAt = now;

                if (isNew)
                {
                    await _vwOutputRep.InsertAsync(entity);
                    result.AddedOutputs++;
                }
                else
                {
                    await _vwOutputRep.UpdateAsync(entity);
                }
                seen.Add(entity.ID);
            }

            // Cổng có trên thiết bị (kênh cổng ra) nhưng không nằm trong tường ⇒ vẫn là cổng của bộ, chưa có vị trí
            // (VD bị bỏ khỏi tường khi áp dụng Bố cục gán cổng ⇒ nằm trong danh sách chờ gán, KHÔNG phải cổng mất) — 07/11/2026
            var onWall = wallOutputs.Data.WallOutput.Select(o => o.OutputId).ToHashSet();
            foreach (var channel in channelById.Values.Where(c => !onWall.Contains(c.Id)))
            {
                var entity = existing.FirstOrDefault(u => u.DeviceOutputId == channel.Id && !seen.Contains(u.ID));
                var isNew = entity == null;
                entity ??= new VwControllerOutput { ControllerId = controller.ID, Status = BaseEnums.StatusEnum.Enable };

                entity.DeviceOutputId = channel.Id;
                entity.Name = channel.Name ?? entity.Name ?? $"Output {channel.Id}";
                entity.PortType = channel.PortType ?? entity.PortType;
                entity.BoardNo = channel.PortInBoard?.BoardId ?? entity.BoardNo;
                entity.PortNo = channel.PortInBoard?.PortId ?? entity.PortNo;
                entity.WallNo = wallNo;
                entity.RectX = null;
                entity.RectY = null;
                entity.RectW = null;
                entity.RectH = null;
                entity.LocalCol = null;
                entity.LocalRow = null;
                entity.DeviceState = VwOutputDeviceState.Present;
                entity.LastSyncedAt = now;

                if (isNew)
                {
                    await _vwOutputRep.InsertAsync(entity);
                    result.AddedOutputs++;
                }
                else
                {
                    await _vwOutputRep.UpdateAsync(entity);
                }
                seen.Add(entity.ID);
            }

            // Cổng đã lưu mà thiết bị không trả (không có trong tường lẫn kênh cổng ra) ⇒ Missing (giữ bản ghi)
            foreach (var stale in existing.Where(u => !seen.Contains(u.ID) && u.DeviceState != VwOutputDeviceState.Missing))
            {
                stale.DeviceState = VwOutputDeviceState.Missing;
                stale.LastSyncedAt = now;
                await _vwOutputRep.UpdateAsync(stale);
                result.MissingOutputs++;
            }
            result.OutputCount = wallOutputs.Data.WallOutput.Count;

            // Màn đang cắm: đồng bộ riêng (SyncScreens), dựa trên cổng vừa lưu
        }

        public Task<VwSyncOutputsOutput> SyncScreens(VwSyncScreensInput input, CancellationToken ct = default) =>
            TrackAndLog(VwEventTriggerAction.SyncScreens,
                () => SyncScreensCore(input, ct),
                output => (input.ID, (string?)null, (string?)null, (IReadOnlyList<VwSetupSceneStep>)output.Steps),
                (input.ID, null, null),
                ct);

        /// <summary>
        /// Đồng bộ thông số màn của các bộ con (GetWallScreenInfo) ⇒ màn đang gán cổng ra ĐÃ LƯU (ghép theo rect).
        /// Không đọc / ghi cổng ra: bộ con chưa có cổng ⇒ báo lỗi, cần "Cập nhật cổng ra" trước.
        /// </summary>
        private async Task<VwSyncOutputsOutput> SyncScreensCore(VwSyncScreensInput input, CancellationToken ct)
        {
            var output = new VwSyncOutputsOutput();

            var targets = await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.ParentControllerId != null && u.IP != null && u.IP != string.Empty)
                .ToListAsync();
            if (!string.IsNullOrWhiteSpace(input.ID))
                targets = targets.Where(u => u.ID == input.ID).ToList();
            if (targets.Count == 0)
                throw new InvalidOperationException($"Không có bộ con nào (có IP) để đồng bộ màn hình{(string.IsNullOrWhiteSpace(input.ID) ? "" : $" [{input.ID}]")}.");

            foreach (var controller in targets.OrderBy(u => u.Code))
            {
                var label = controller.Name ?? controller.ID;
                var result = new VwSyncOutputsControllerResult { ControllerId = controller.ID, ControllerName = controller.Name };
                output.Controllers.Add(result);
                try
                {
                    var ports = await _vwOutputRep.AsQueryable()
                        .Where(u => u.IsDelete == null && u.ControllerId == controller.ID && u.DeviceState != VwOutputDeviceState.Missing)
                        .ToListAsync();
                    if (ports.Count == 0)
                        throw new InvalidOperationException("Chưa có cổng ra của bộ này — bấm \"Cập nhật cổng ra\" trước.");

                    // Tường: theo yêu cầu ⇒ theo cổng đã lưu ⇒ tự dò
                    var wallNo = input.WallNo is > 0 ? input.WallNo!.Value
                        : ports.Where(p => p.WallNo != null).GroupBy(p => p.WallNo).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault()
                          ?? await ResolveWall(controller, null, ct);
                    result.WallNo = wallNo;

                    // Thiết bị chỉ trả thông tin các rect được hỏi ⇒ hỏi đúng rect của cổng ra đã lưu trên tường này
                    var rects = ports
                        .Where(p => p.WallNo == wallNo && p.RectX != null && p.RectY != null && p.RectW is > 0 && p.RectH is > 0)
                        .Select(p => new ITS.VideoWall.Core.Dto.ISAPI.VwISAPIWallScreenRect { X = p.RectX!.Value, Y = p.RectY!.Value, Width = p.RectW!.Value, Height = p.RectH!.Value })
                        .ToList();
                    if (rects.Count == 0)
                        throw new InvalidOperationException($"Cổng ra của tường {wallNo} chưa có vị trí — bấm \"Cập nhật cổng ra\" trước.");

                    var info = await Track(output.Steps, $"[{label}] Màn đang cắm", "POST", "/ISAPI/DisplayDev/ScreenCtrl/GetWallScreenInfo?format=json",
                        () => _deviceClient.GetWallScreenInfoAsync(controller, wallNo, rects, ct));
                    if (info is not { Success: true, Data: not null })
                        throw new InvalidOperationException(info.ErrorMessage ?? "Không đọc được thông tin màn.");

                    var wall = info.Data.ScreenPosList.FirstOrDefault(w => w.WallId == wallNo);
                    if (wall != null)
                        await SyncScreensAsync(wall.ScreenInfoList, ports, DateTime.Now, result);
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    // Một bộ lỗi không chặn các bộ còn lại
                    _logger.LogWarning(ex, "SyncScreens: lỗi đồng bộ {Controller}", label);
                    result.Message = ex.Message;
                    Fail(output.Steps, $"Đồng bộ màn [{label}]", "SYNC", "local:Screen", ex.Message);
                }
            }

            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwControllerOutput);
            return output;
        }

        /// <summary>
        /// Chỉ ghi THÔNG SỐ thiết bị vào màn mà người dùng đã gán cho cổng (07/11/2026 — vị trí / gán cổng do người dùng quản lý):
        ///  - Màn thiết bị báo ⇒ cổng có rect chứa góc trên-trái của màn ⇒ màn đang gán cổng đó (VwScreen.ControllerOutputId).
        ///  - Ghi serial thiết bị (DeviceSerialNumber), phiên bản, nhiệt độ, giờ chạy, lần kiểm. Serial khai báo bị ghi đè bằng serial thiết bị
        ///    (11/11/2026); màn đã tháo (serial rỗng) ⇒ giữ serial khai báo cũ.
        ///  - KHÔNG tạo màn, không gán / tháo màn khỏi cổng. Cổng chưa gán màn ⇒ bỏ qua, ghi vào Message.
        ///  - Độ sáng KHÔNG đọc ở đây: screenStatus ~3,5 s / màn (32 màn ≈ 2 phút) ⇒ đọc riêng khi chọn màn (ReadScreenStatus).
        /// </summary>
        private async Task SyncScreensAsync(
            List<ITS.VideoWall.Core.Dto.ISAPI.VwISAPIWallScreenInfo> infos,
            List<VwControllerOutput> ports,
            DateTime now, VwSyncOutputsControllerResult result)
        {
            var portIds = ports.Select(p => p.ID).ToList();
            var screens = await _vwScreenRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.ControllerOutputId != null && portIds.Contains(u.ControllerOutputId))
                .ToListAsync();
            var unassigned = new List<string>();

            foreach (var info in infos)
            {
                var port = info.Rect == null ? null : ports.FirstOrDefault(p =>
                    p.RectX <= info.Rect.X && info.Rect.X < p.RectX + p.RectW &&
                    p.RectY <= info.Rect.Y && info.Rect.Y < p.RectY + p.RectH);
                if (port == null)
                    continue;

                var screen = screens.FirstOrDefault(u => u.ControllerOutputId == port.ID);
                if (screen == null)
                {
                    unassigned.Add(port.DisplayName ?? port.Name ?? port.ID);
                    continue;
                }

                // Ghi đúng giá trị thiết bị trả (kể cả rỗng) — không giữ giá trị cũ của tấm khác
                screen.DeviceSerialNumber = string.IsNullOrWhiteSpace(info.SerialNumber) ? null : info.SerialNumber.Trim();
                // Serial khai báo = serial thiết bị (11/11/2026, theo yêu cầu). Màn đã tháo (rỗng) ⇒ giữ serial khai báo cũ.
                if (screen.DeviceSerialNumber != null)
                    screen.SerialNumber = screen.DeviceSerialNumber;
                screen.SoftwareVersion = string.IsNullOrWhiteSpace(info.SoftwareVersion) ? null : info.SoftwareVersion;
                screen.Temperature = info.Temperature?.ToString();
                screen.RunTimeHours = info.DeviceUpTime is long sec ? Math.Round(sec / 3600d, 1).ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
                screen.LastCheckedAt = now;
                await _vwScreenRep.AsUpdateable(screen)
                    .UpdateColumns(u => new { u.SerialNumber, u.DeviceSerialNumber, u.SoftwareVersion, u.Temperature, u.RunTimeHours, u.LastCheckedAt })
                    .ExecuteCommandAsync();
                result.ScreenCount++;
            }

            if (unassigned.Count > 0)
                result.Message = $"Cổng có màn thật nhưng chưa gán màn trên web: {string.Join(", ", unassigned)}";
        }
    }
}
