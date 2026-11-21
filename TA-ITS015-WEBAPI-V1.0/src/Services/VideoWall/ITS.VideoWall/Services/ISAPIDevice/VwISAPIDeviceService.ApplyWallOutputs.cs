using System.Xml.Linq;
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
    /// Description: Ghi bảng cổng ra → vị trí (WallOutputList) xuống bộ con theo Bố cục gán cổng (07/11/2026).
    ///              Mỗi bộ con (bộ hiển thị / độc lập có IP):
    ///                1. Tính bảng mới từ DB: màn (ô) + cổng gán cho màn + vùng ô của bộ (VwWallPlacement.BuildWallOutputs).
    ///                2. GET .../VideoWall/{wall}/outputs (vị trí hiện tại) ⇒ bảng so sánh.
    ///                3. GET .../VideoWall/{wall} (toàn bộ cấu hình tường) ⇒ sao lưu (BackupXml).
    ///                4. DryRun ⇒ dừng. Ngược lại (11/11/2026, cách web hãng làm): gỡ cổng đổi / bỏ (DELETE .../outputs/{id}),
    ///                   gắn lại cổng đổi / mới (PUT .../outputs), lỗi giữa chừng ⇒ gắn lại vị trí cũ; rồi GET outputs đọc lại
    ///                   để xác nhận và cập nhật rect của cổng trong DB. (PUT .../VideoWall/{wall} bỏ qua WallOutputList.)
    ///              Cổng không gán màn ⇒ bỏ khỏi tường (vẫn là cổng của bộ, nằm trong danh sách chờ gán).
    ///              Một bộ lỗi không chặn các bộ còn lại.
    /// Created date: 07/11/2026
    /// </summary>
    public partial class VwISAPIDeviceService
    {
        public Task<VwApplyWallOutputsOutput> ApplyWallOutputs(VwApplyWallOutputsInput input, CancellationToken ct = default) =>
            TrackAndLog(VwEventTriggerAction.ApplyWallOutputs,
                () => ApplyWallOutputsCore(input, ct),
                output => ((string?)null, (string?)null, (string?)null, (IReadOnlyList<VwSetupSceneStep>)output.Steps),
                (null, null, null),
                ct);

        private async Task<VwApplyWallOutputsOutput> ApplyWallOutputsCore(VwApplyWallOutputsInput input, CancellationToken ct)
        {
            var output = new VwApplyWallOutputsOutput { DryRun = input.DryRun };

            var targets = await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.IP != null && u.IP != string.Empty
                            && (u.Role == VwControllerRole.Display || u.Role == VwControllerRole.Standalone))
                .ToListAsync();
            if (input.ControllerIds is { Count: > 0 })
                targets = targets.Where(u => input.ControllerIds.Contains(u.ID)).ToList();
            if (targets.Count == 0)
                throw new InvalidOperationException("Không có bộ hiển thị / bộ độc lập nào (có IP) để áp dụng.");

            // Bố cục đang áp dụng + màn + cổng: đọc 1 lần cho mọi bộ
            var db = _vwScreenRep.Context;
            var topology = await db.Queryable<VwWallTopology>().FirstAsync(u => u.IsDelete == null && u.IsApply == BaseEnums.YesNoEnum.Y)
                ?? throw new InvalidOperationException("Chưa có bố cục tường đang áp dụng.");
            var blocks = await db.Queryable<VwWallBlock>().Where(u => u.IsDelete == null && u.WallTopologyId == topology.ID).ToListAsync();
            var screens = await _vwScreenRep.AsQueryable().Where(u => u.IsDelete == null && u.ControllerOutputId != null).ToListAsync();
            var outputs = await _vwOutputRep.AsQueryable().Where(u => u.IsDelete == null).ToListAsync();

            foreach (var controller in targets.OrderBy(u => u.Code))
            {
                var label = controller.Name ?? controller.ID;
                var result = new VwApplyWallOutputsControllerResult { ControllerId = controller.ID, ControllerName = controller.Name };
                output.Controllers.Add(result);
                try
                {
                    await ApplyControllerWallOutputsAsync(controller, label, input.DryRun, blocks, screens, outputs, output.Steps, result, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ApplyWallOutputs: lỗi áp dụng {Controller}", label);
                    result.Success = false;
                    result.Message = ex.Message;
                    Fail(output.Steps, $"Áp dụng gán cổng [{label}]", "APPLY", "local:WallOutputList", ex.Message);
                }
            }

            _cache.RemoveByPrefixKey(CacheConst.Vw.VwControllerOutput);
            return output;
        }

        private async Task ApplyControllerWallOutputsAsync(
            VwController controller, string label, bool dryRun,
            List<VwWallBlock> blocks, List<VwScreen> screens, List<VwControllerOutput> outputs,
            List<VwSetupSceneStep> steps, VwApplyWallOutputsControllerResult result, CancellationToken ct)
        {
            // 1. Ô gốc = 1920 (baseOutputSize thiết bị) — bảng mới theo Bố cục gán cổng
            var caps = await Track(steps, $"[{label}] Năng lực Video Wall", "GET", "/ISAPI/DisplayDev/VideoWall/capabilities",
                () => _deviceClient.GetCapabilitiesAsync(controller, ct));
            var bos = caps.Data?.BaseOutputSize is int b and > 0 ? b : VwWallProfile.BaseOutputSize;

            var (wanted, error) = VwWallPlacement.BuildWallOutputs(controller.ID, blocks, screens, outputs, bos);
            if (error != null)
                throw new InvalidOperationException(error);

            // 2. Tường của bộ: theo cổng đã đồng bộ ⇒ tự dò
            var ports = outputs.Where(o => o.ControllerId == controller.ID).ToList();
            var wallNo = ports.Where(p => p.WallNo != null).GroupBy(p => p.WallNo).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault()
                         ?? await ResolveWall(controller, null, ct);
            result.WallNo = wallNo;

            // 3. Vị trí hiện tại trên thiết bị ⇒ bảng so sánh
            var current = await Track(steps, $"[{label}] Cổng ra của tường {wallNo}", "GET", $"/ISAPI/DisplayDev/VideoWall/{wallNo}/outputs",
                () => _deviceClient.GetOutputsAsync(controller, wallNo, ct));
            if (current is not { Success: true, Data: not null })
                throw new InvalidOperationException(current.ErrorMessage ?? "Không đọc được cổng ra của tường.");

            var oldByDevice = current.Data.WallOutput
                .GroupBy(o => o.OutputId)
                .ToDictionary(g => g.Key, g => g.First());
            var newByDevice = wanted.ToDictionary(t => t.DeviceOutputId);
            var portByDevice = ports.Where(p => p.DeviceOutputId != null).GroupBy(p => p.DeviceOutputId!.Value).ToDictionary(g => g.Key, g => g.First());

            foreach (var deviceId in oldByDevice.Keys.Union(newByDevice.Keys).OrderBy(id => id))
            {
                oldByDevice.TryGetValue(deviceId, out var old);
                newByDevice.TryGetValue(deviceId, out var target);
                portByDevice.TryGetValue(deviceId, out var port);
                int[]? oldRect = old?.Rect == null ? null : [old.Rect.Coordinate?.X ?? 0, old.Rect.Coordinate?.Y ?? 0, old.Rect.Width, old.Rect.Height];
                int[]? newRect = target == null ? null : [target.X, target.Y, target.W, target.H];
                result.Changes.Add(new VwWallOutputChange
                {
                    OutputId = port?.ID,
                    DeviceOutputId = deviceId,
                    Name = port?.DisplayName ?? port?.Name ?? $"#{deviceId}",
                    Change = oldRect == null ? "added" : newRect == null ? "removed" : oldRect.SequenceEqual(newRect) ? "unchanged" : "moved",
                    OldRect = oldRect,
                    NewRect = newRect,
                });
            }

            // 4. Sao lưu toàn bộ cấu hình tường (để khôi phục tay nếu cần)
            var wallUri = $"ISAPI/DisplayDev/VideoWall/{wallNo}";
            var wall = await Track(steps, $"[{label}] Cấu hình tường {wallNo}", "GET", "/" + wallUri,
                () => _deviceClient.SendRawAsync(controller, HttpMethod.Get, wallUri, null, null, ct));
            if (!wall.Success || string.IsNullOrWhiteSpace(wall.RawResponse))
                throw new InvalidOperationException(wall.ErrorMessage ?? "Không đọc được cấu hình tường.");
            result.BackupXml = wall.RawResponse;

            var hasChange = result.Changes.Any(c => c.Change != "unchanged");
            if (dryRun || !hasChange)
            {
                result.Success = true;
                result.Message = dryRun ? "Chạy thử — chưa ghi xuống thiết bị." : "Thiết bị đã khớp Bố cục gán cổng, không cần ghi.";
                return;
            }

            // 5. Ghi (thử trên .19 ngày 11/11/2026, theo cách web hãng làm):
            //    - PUT /VideoWall/{wall} trả OK nhưng BỎ QUA WallOutputList (kèm WallWindowList đọc về ⇒ badParameters);
            //    - PUT /outputs và PUT /outputs/{id} với cổng đang gắn ⇒ theOutputHasBeenLinkedToASmartWall.
            //    ⇒ Gỡ cổng đổi / bỏ (DELETE /outputs/{id}) rồi gắn lại cổng đổi / mới (PUT /outputs, không kèm id).
            var outputsUri = $"ISAPI/DisplayDev/VideoWall/{wallNo}/outputs";
            var toUnlink = result.Changes.Where(c => c.Change is "moved" or "removed").Select(c => oldByDevice[c.DeviceOutputId]).ToList();
            var linkIds = result.Changes.Where(c => c.Change is "moved" or "added").Select(c => c.DeviceOutputId).ToHashSet();
            var toLink = wanted.Where(t => linkIds.Contains(t.DeviceOutputId)).ToList();

            var unlinked = new List<ITS.VideoWall.Core.Dto.ISAPI.VwISAPIOutputItem>();
            foreach (var o in toUnlink)
            {
                var name = portByDevice.TryGetValue(o.OutputId, out var p) ? p.DisplayName ?? p.Name : $"#{o.OutputId}";
                var del = await Track(steps, $"[{label}] Gỡ cổng {name} khỏi tường {wallNo}", "DELETE", $"/{outputsUri}/{o.Id}",
                    () => _deviceClient.SendRawAsync(controller, HttpMethod.Delete, $"{outputsUri}/{o.Id}", null, null, ct));
                if (!del.Success || !IsIsapiOk(del.RawResponse))
                {
                    await RelinkAsync(unlinked);
                    throw new InvalidOperationException(del.ErrorMessage ?? $"Thiết bị từ chối gỡ cổng {name}: {del.RawResponse}");
                }
                unlinked.Add(o);
            }

            if (toLink.Count > 0)
            {
                var body = BuildLinkOutputsXml(toLink.Select(t => (t.DeviceOutputId, t.X, t.Y, t.W, t.H)));
                var put = await Track(steps, $"[{label}] Gắn {toLink.Count} cổng vào tường {wallNo}", "PUT", "/" + outputsUri,
                    () => _deviceClient.SendRawAsync(controller, HttpMethod.Put, outputsUri, body, "application/xml", ct));
                if (!put.Success || !IsIsapiOk(put.RawResponse))
                {
                    // Gắn lại vị trí cũ để màn không bị tối
                    await RelinkAsync(unlinked);
                    throw new InvalidOperationException(put.ErrorMessage ?? $"Thiết bị từ chối gắn cổng: {put.RawResponse}");
                }
            }
            result.Applied = true;

            async Task RelinkAsync(List<ITS.VideoWall.Core.Dto.ISAPI.VwISAPIOutputItem> items)
            {
                if (items.Count == 0)
                    return;
                var body = BuildLinkOutputsXml(items.Where(i => i.Rect != null)
                    .Select(i => (i.OutputId, i.Rect!.Coordinate?.X ?? 0, i.Rect.Coordinate?.Y ?? 0, i.Rect.Width, i.Rect.Height)));
                await Track(steps, $"[{label}] Khôi phục {items.Count} cổng về vị trí cũ", "PUT", "/" + outputsUri,
                    () => _deviceClient.SendRawAsync(controller, HttpMethod.Put, outputsUri, body, "application/xml", ct));
            }

            // 6. Đọc lại để xác nhận + cập nhật rect của cổng trong DB (đồng bộ màn ghép theo rect)
            var verify = await Track(steps, $"[{label}] Kiểm tra lại tường {wallNo}", "GET", $"/ISAPI/DisplayDev/VideoWall/{wallNo}/outputs",
                () => _deviceClient.GetOutputsAsync(controller, wallNo, ct));
            if (verify is not { Success: true, Data: not null })
                throw new InvalidOperationException("Đã ghi nhưng không đọc lại được cổng ra để kiểm tra.");

            var actual = verify.Data.WallOutput.GroupBy(o => o.OutputId).ToDictionary(g => g.Key, g => g.First());
            var mismatched = wanted
                .Where(t => !actual.TryGetValue(t.DeviceOutputId, out var a) || a.Rect == null
                            || (a.Rect.Coordinate?.X ?? 0) != t.X || (a.Rect.Coordinate?.Y ?? 0) != t.Y)
                .Select(t => portByDevice.TryGetValue(t.DeviceOutputId, out var p) ? p.DisplayName ?? p.Name ?? $"#{t.DeviceOutputId}" : $"#{t.DeviceOutputId}")
                .ToList();

            var changedPorts = new List<VwControllerOutput>();
            foreach (var port in ports.Where(p => p.DeviceOutputId != null))
            {
                actual.TryGetValue(port.DeviceOutputId!.Value, out var a);
                port.WallNo = wallNo;
                port.RectX = a?.Rect?.Coordinate?.X;
                port.RectY = a?.Rect?.Coordinate?.Y;
                port.RectW = a?.Rect?.Width;
                port.RectH = a?.Rect?.Height;
                port.LocalCol = a?.Rect == null ? null : (a.Rect.Coordinate?.X ?? 0) / bos;
                port.LocalRow = a?.Rect == null ? null : (a.Rect.Coordinate?.Y ?? 0) / bos;
                changedPorts.Add(port);
            }
            if (changedPorts.Count > 0)
                await _vwOutputRep.AsUpdateable(changedPorts)
                    .UpdateColumns(u => new { u.WallNo, u.RectX, u.RectY, u.RectW, u.RectH, u.LocalCol, u.LocalRow })
                    .ExecuteCommandAsync();

            result.Success = mismatched.Count == 0;
            result.Message = mismatched.Count == 0
                ? "Đã ghi và kiểm tra lại khớp."
                : $"Đã ghi nhưng thiết bị trả vị trí khác cho: {string.Join(", ", mismatched)}";
        }

        /// <summary>
        /// Body gắn cổng vào tường (PUT /VideoWall/{wall}/outputs) như web hãng: mỗi cổng = outputID + Rect, KHÔNG kèm id
        /// (thiết bị tự giữ id theo cổng). Toạ độ uniform như GET outputs trả về.
        /// </summary>
        private static string BuildLinkOutputsXml(IEnumerable<(int OutputId, int X, int Y, int W, int H)> items)
        {
            XNamespace ns = "http://www.isapi.org/ver20/XMLSchema";
            var list = new XElement(ns + "WallOutputList", new XAttribute("version", "2.0"),
                items.Select(i => new XElement(ns + "WallOutput",
                    new XElement(ns + "outputID", i.OutputId),
                    new XElement(ns + "Rect",
                        new XElement(ns + "Coordinate", new XElement(ns + "x", i.X), new XElement(ns + "y", i.Y)),
                        new XElement(ns + "width", i.W),
                        new XElement(ns + "height", i.H)),
                    new XElement(ns + "coordinateMode", "uniformCoordinate"))));
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + list.ToString(SaveOptions.DisableFormatting);
        }

        /// <summary>ResponseStatus của ISAPI: statusCode 1 = OK. Không có ResponseStatus (body rỗng) ⇒ coi như OK (HTTP đã 2xx).</summary>
        private static bool IsIsapiOk(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return true;
            try
            {
                var root = XDocument.Parse(raw).Root;
                var code = root?.Element(root.Name.Namespace + "statusCode")?.Value;
                return code == null || code == "1";
            }
            catch
            {
                return true;
            }
        }
    }
}
