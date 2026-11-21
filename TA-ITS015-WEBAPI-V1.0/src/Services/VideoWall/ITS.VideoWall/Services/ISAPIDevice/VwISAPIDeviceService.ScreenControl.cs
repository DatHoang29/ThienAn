


using ITS.VideoWall.Core.Dto.DeviceSetup;
using ITS.VideoWall.Core.Dto.ISAPI;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Entities;
using Shared.DTO.Constants.Application;

namespace ITS.VideoWall.Services.ISAPIDevice
{
    /// <summary>
    /// Description: Điều khiển màn của bộ hiển thị (11/11/2026). Lệnh gửi tới bộ con (màn cắm ở bộ con), không gửi bộ trung tâm.
    ///              - Bật / tắt: PUT ScreenCtrl/openAll | closeAll, body &lt;VideoWallID&gt; (lấy từ web hãng). Bật/tắt từng màn
    ///                (ScreenCtrl/control open|close) thiết bị trả badXmlFormat ⇒ chỉ làm theo bộ.
    ///              - Độ sáng: PUT ScreenCtrl/control, effect backlight 0-100 cho từng màn được chọn (rect cổng ra đang gán cho màn).
    ///              Một bộ lỗi không chặn các bộ còn lại.
    /// Created date: 11/11/2026
    /// </summary>
    public partial class VwISAPIDeviceService
    {
        public Task<VwScreenControlOutput> ScreenPower(VwScreenPowerInput input, CancellationToken ct = default) =>
            TrackAndLog(VwEventTriggerAction.ScreenPower,
                () => ScreenControlCore(input.ControllerIds, input.On ? "Bật màn" : "Tắt màn", ct,
                    (controller, wallNo, _, steps) => Track(steps, $"[{controller.Name ?? controller.ID}] {(input.On ? "Bật" : "Tắt")} màn tường {wallNo}", "PUT",
                        $"/ISAPI/DisplayDev/ScreenCtrl/{(input.On ? "openAll" : "closeAll")}",
                        () => _deviceClient.SetScreenPowerAsync(controller, wallNo, input.On, ct))),
                output => ((string?)null, (string?)null, (string?)null, (IReadOnlyList<VwSetupSceneStep>)output.Steps),
                (null, null, null),
                ct);

        public Task<VwScreenControlOutput> ScreenBacklight(VwScreenBacklightInput input, CancellationToken ct = default) =>
            TrackAndLog(VwEventTriggerAction.ScreenBacklight,
                () => ScreenBacklightCore(input, ct),
                output => ((string?)null, (string?)null, (string?)null, (IReadOnlyList<VwSetupSceneStep>)output.Steps),
                (null, null, null),
                ct);

        /// <summary>Màn ⇒ cổng ra đang gán ⇒ bộ của cổng. Mỗi bộ 1 lệnh PUT cho rect các màn được chọn của bộ đó.</summary>
        private async Task<VwScreenControlOutput> ScreenBacklightCore(VwScreenBacklightInput input, CancellationToken ct)
        {
            var screens = await _vwScreenRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.ControllerOutputId != null && input.ScreenIds.Contains(u.ID))
                .ToListAsync();
            if (screens.Count == 0)
                throw new InvalidOperationException("Màn được chọn chưa gán cổng ra nên không chỉnh được độ sáng.");

            var portIds = screens.Select(s => s.ControllerOutputId!).ToList();
            var controllerIds = await _vwOutputRep.AsQueryable()
                .Where(o => o.IsDelete == null && portIds.Contains(o.ID))
                .Select(o => o.ControllerId)
                .Distinct()
                .ToListAsync();
            if (controllerIds.Count == 0)
                throw new InvalidOperationException("Không tìm thấy cổng ra của màn được chọn.");

            var brightness = input.Value.ToString();
            return await ScreenControlCore(controllerIds!, "Độ sáng màn", ct, async (controller, wallNo, ports, steps) =>
            {
                var mine = ports.Where(p => portIds.Contains(p.ID) && p.RectX != null && p.RectY != null && p.RectW is > 0 && p.RectH is > 0).ToList();
                if (mine.Count == 0)
                    throw new InvalidOperationException("Cổng ra của màn chưa có vị trí — bấm \"Cập nhật cổng ra\" trước.");
                var rects = mine
                    .Select(p => new VwISAPIWallScreenRect { X = p.RectX!.Value, Y = p.RectY!.Value, Width = p.RectW!.Value, Height = p.RectH!.Value })
                    .ToList();

                var res = await Track(steps, $"[{controller.Name ?? controller.ID}] Độ sáng {input.Value} cho {rects.Count} màn", "PUT",
                    "/ISAPI/DisplayDev/ScreenCtrl/control",
                    () => _deviceClient.SetScreenBacklightAsync(controller, wallNo, rects, input.Value, ct));
                if (res.Success)
                {
                    var minePortIds = mine.Select(p => p.ID).ToList();
                    await _vwScreenRep.AsUpdateable()
                        .SetColumns(u => u.Brightness == brightness)
                        .Where(u => u.IsDelete == null && input.ScreenIds.Contains(u.ID) && minePortIds.Contains(u.ControllerOutputId))
                        .ExecuteCommandAsync();
                    _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);
                }
                return res;
            });
        }

        /// <summary>
        /// Đọc trạng thái 1 màn: màn ⇒ cổng đang gán (rect, tường) ⇒ bộ của cổng ⇒ screenStatus. Có độ sáng ⇒ ghi VwScreen.Brightness.
        /// Màn đã tháo ⇒ thiết bị báo gettingScreenStatusFailed ⇒ Success = false.
        /// </summary>
        public async Task<VwScreenStatusOutput> ReadScreenStatus(VwScreenStatusInput input, CancellationToken ct = default)
        {
            var output = new VwScreenStatusOutput { ScreenId = input.ID };
            var screen = await _vwScreenRep.AsQueryable().FirstAsync(u => u.IsDelete == null && u.ID == input.ID);
            if (screen?.ControllerOutputId == null)
                return Fail("Màn chưa gán cổng ra.");
            var port = await _vwOutputRep.AsQueryable().FirstAsync(o => o.IsDelete == null && o.ID == screen.ControllerOutputId);
            if (port is not { WallNo: not null, RectX: not null, RectY: not null, RectW: > 0, RectH: > 0 })
                return Fail("Cổng ra của màn chưa có vị trí — bấm \"Cập nhật cổng ra\" trước.");
            var controller = await _vwControllerRep.AsQueryable().FirstAsync(c => c.IsDelete == null && c.ID == port.ControllerId);
            if (controller == null)
                return Fail("Không tìm thấy bộ điều khiển của cổng ra.");

            var rect = new VwISAPIWallScreenRect { X = port.RectX!.Value, Y = port.RectY!.Value, Width = port.RectW!.Value, Height = port.RectH!.Value };
            var res = await _deviceClient.GetScreenStatusAsync(controller, port.WallNo!.Value, rect, ct);
            if (!res.Success || res.Data?.ScreenStatus == null)
                return Fail(res.ErrorMessage ?? "Không đọc được trạng thái màn.");

            output.Success = true;
            output.SwitchStatus = res.Data.ScreenStatus.SwitchStatus;
            output.Backlight = res.Data.ScreenStatus.ColorInfo?.Backlight;
            if (output.Backlight != null)
            {
                var brightness = output.Backlight.Value.ToString();
                await _vwScreenRep.AsUpdateable()
                    .SetColumns(u => u.Brightness == brightness)
                    .Where(u => u.ID == screen.ID)
                    .ExecuteCommandAsync();
                _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);
            }
            return output;

            VwScreenStatusOutput Fail(string message)
            {
                output.Message = message;
                return output;
            }
        }

        /// <summary>
        /// Chạy một lệnh màn lần lượt trên các bộ hiển thị / độc lập có IP (trống ⇒ toàn tường).
        /// Tường của bộ = WallNo của cổng ra đã đồng bộ ⇒ tự dò. send nhận cổng ra (đã có vị trí) của đúng tường đó.
        /// </summary>
        private async Task<VwScreenControlOutput> ScreenControlCore(
            List<string>? controllerIds, string actionLabel, CancellationToken ct,
            Func<VwController, int, List<VwControllerOutput>, List<VwSetupSceneStep>, Task<VwISAPIResult>> send)
        {
            var output = new VwScreenControlOutput();

            var targets = await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.IP != null && u.IP != string.Empty
                            && (u.Role == VwControllerRole.Display || u.Role == VwControllerRole.Standalone))
                .ToListAsync();
            if (controllerIds is { Count: > 0 })
                targets = targets.Where(u => controllerIds.Contains(u.ID)).ToList();
            if (targets.Count == 0)
                throw new InvalidOperationException("Không có bộ hiển thị / bộ độc lập nào (có IP) để điều khiển màn.");

            var targetIds = targets.Select(u => u.ID).ToList();
            var ports = await _vwOutputRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.WallNo != null && u.DeviceState != VwOutputDeviceState.Missing && targetIds.Contains(u.ControllerId))
                .ToListAsync();

            foreach (var controller in targets.OrderBy(u => u.Code))
            {
                var label = controller.Name ?? controller.ID;
                var result = new VwScreenControlControllerResult { ControllerId = controller.ID, ControllerName = controller.Name };
                output.Controllers.Add(result);
                try
                {
                    var own = ports.Where(p => p.ControllerId == controller.ID).ToList();
                    var wallNo = own.GroupBy(p => p.WallNo).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault()
                                 ?? await ResolveWall(controller, null, ct);
                    result.WallNo = wallNo;

                    var res = await send(controller, wallNo, own.Where(p => p.WallNo == wallNo).ToList(), output.Steps);
                    result.Success = res.Success;
                    result.Message = res.Success ? null : res.ErrorMessage;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{Action}: lỗi {Controller}", actionLabel, label);
                    result.Success = false;
                    result.Message = ex.Message;
                    Fail(output.Steps, $"{actionLabel} [{label}]", "PUT", "local:ScreenCtrl", ex.Message);
                }
            }

            return output;
        }
    }
}
