using Mapster;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.ControllerOutput;
using Module.VideoWall.Core.Dto.Screen;
using Module.VideoWall.Core.Dto.WallTopology;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Exceptions;
using Module.VideoWall.Core.Helper;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Services;

namespace Module.VideoWall.Infrastructure.Services.Layout
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Bố cục tường (VwWallTopology + VwWallBlock) và các phép kiểm vị trí dựa trên nó.
    ///
    ///              Tường gồm 1..n khối xếp ngang theo BlockNo; mỗi khối có lưới Cols × Rows riêng.
    ///              Cổng ra (VwControllerOutput) được TỰ đặt vào (BlockNo, GridRow, GridCol) theo vị trí vật lý,
    ///              cửa sổ kịch bản theo (BlockNo, X, Y, W, H) — toạ độ đều tính TRONG khối, đơn vị 1 ô = ScreenWidth × ScreenHeight.
    /// Created date: 22/10/2026 — cập nhật 06/11/2026 (vị trí thuộc cổng ra, không còn đặt màn tay)
    /// </summary>
    public class VwWallLayoutService : IScoped
    {
        private readonly BaseRepository<VwWallTopology> _topologyRep;
        private readonly BaseRepository<VwWallBlock> _blockRep;
        private readonly BaseRepository<VwControllerOutput> _outputRep;
        private readonly BaseRepository<VwController> _controllerRep;
        private readonly BaseRepository<VwScreen> _screenRep;
        private readonly BaseCacheService _cache;

        public VwWallLayoutService(
            BaseRepository<VwWallTopology> topologyRep,
            BaseRepository<VwWallBlock> blockRep,
            BaseRepository<VwControllerOutput> outputRep,
            BaseRepository<VwController> controllerRep,
            BaseRepository<VwScreen> screenRep,
            BaseCacheService cache)
        {
            _topologyRep = topologyRep;
            _blockRep = blockRep;
            _outputRep = outputRep;
            _controllerRep = controllerRep;
            _screenRep = screenRep;
            _cache = cache;
        }

        #region Đọc bố cục

        /// <summary>
        /// Description: Bố cục tường đang dùng (đọc qua cache). Chưa cấu hình thì trả 1 khối mặc định
        ///              DefaultCols × DefaultRows kèm IsDefault = true, KHÔNG ghi CSDL.
        /// Created date: 22/10/2026
        /// </summary>
        public Task<VwWallTopologyOutput> GetCurrentAsync()
        {
            var layout = _cache.GetOrAdd(CacheConst.Vw.VwWallTopology, _ =>
            {
                var topology = GetCurrentEntityAsync().Result;
                return BuildOutput(topology, LoadBlocks(topology?.ID));
            });

            return Task.FromResult(layout);
        }

        /// <summary>
        /// Description: Bản ghi bố cục đang áp dụng lên tường (IsApply = 1). Dữ liệu chưa có cờ (chưa chạy migration)
        ///              thì lấy bản ghi chưa xoá đầu tiên.
        /// Created date: 22/10/2026 — cập nhật 28/10/2026 (đọc IsApply thay cho Status)
        /// </summary>
        public async Task<VwWallTopology?> GetCurrentEntityAsync()
        {
            return await _topologyRep.GetFirstAsync(u => u.IsDelete == null && u.IsApply == BaseEnums.YesNoEnum.Y)
                ?? await _topologyRep.GetFirstAsync(u => u.IsDelete == null);
        }

        /// <summary>
        /// Description: Một phiên bản bố cục theo ID (đọc qua cache) — dùng cho kịch bản thuộc bố cục bất kỳ.
        /// Created date: 22/10/2026
        /// </summary>
        public Task<VwWallTopologyOutput> GetByIdAsync(string topologyId)
        {
            var layout = _cache.GetOrAdd($"{CacheConst.Vw.VwWallTopology}:{topologyId}", _ =>
            {
                var topology = _topologyRep.GetFirstAsync(u => u.IsDelete == null && u.ID == topologyId).Result
                    ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);
                return BuildOutput(topology, LoadBlocks(topology.ID));
            });

            return Task.FromResult(layout);
        }

        private List<VwWallBlock> LoadBlocks(string? topologyId)
        {
            return string.IsNullOrWhiteSpace(topologyId)
                ? []
                : _blockRep.AsQueryable()
                    .Where(u => u.IsDelete == null && u.WallTopologyId == topologyId)
                    .ToListAsync().Result;
        }

        /// <summary>
        /// Description: Xoá cache bố cục — gọi sau khi ghi VwWallTopology / VwWallBlock.
        /// Created date: 22/10/2026
        /// </summary>
        public void RemoveCache() => _cache.RemoveByPrefixKey(CacheConst.Vw.VwWallTopology);

        /// <summary>
        /// Description: Dựng DTO bố cục từ bản ghi tường + danh sách khối (hàm thuần, không đọc CSDL).
        ///              Dùng chung cho đọc bố cục đang lưu và kiểm tra bố cục sắp lưu.
        /// Created date: 22/10/2026
        /// </summary>
        public static VwWallTopologyOutput BuildOutput(VwWallTopology? topology, IEnumerable<VwWallBlock> blocks)
        {
            var output = topology?.Adapt<VwWallTopologyOutput>() ?? new VwWallTopologyOutput();

            // Kích thước panel (ScreenWidth / ScreenHeight) cố định theo profile; tỉ lệ ô để trống thì lấy tỉ lệ mặc định.
            output.CellRatio = string.IsNullOrWhiteSpace(topology?.CellRatio) ? VwWallProfile.DefaultCellRatio : topology.CellRatio.Trim();

            var list = blocks
                .Where(u => u.BlockNo >= 0 && u.Cols > 0 && u.Rows > 0)
                .OrderBy(u => u.BlockNo)
                .ToList();

            output.IsDefault = topology == null || list.Count == 0;
            if (list.Count == 0)
                list = [new VwWallBlock { BlockNo = 0, Cols = VwWallProfile.DefaultCols, Rows = VwWallProfile.DefaultRows }];

            var offsetCol = 0;
            output.Blocks = list.Select(u =>
            {
                var block = u.Adapt<VwWallBlockOutput>();
                block.ScreenCount = u.Cols!.Value * u.Rows!.Value;
                block.OffsetCol = offsetCol;
                offsetCol += u.Cols.Value;
                return block;
            }).ToList();

            output.TotalScreens = output.Blocks.Sum(u => u.ScreenCount);
            output.CanvasWidth = offsetCol * output.ScreenWidth!.Value;
            output.CanvasHeight = output.Blocks.Max(u => u.Rows!.Value) * output.ScreenHeight!.Value;

            return output;
        }

        #endregion

        #region Kiểm vị trí

        public static VwWallBlockOutput? FindBlock(VwWallTopologyOutput layout, int? blockNo)
            => blockNo == null ? null : layout.Blocks.FirstOrDefault(u => u.BlockNo == blockNo);

        /// <summary>
        /// Description: Ô (hàng, cột) có nằm trong lưới của khối không.
        /// Created date: 22/10/2026
        /// </summary>
        public static bool IsCellInside(VwWallTopologyOutput layout, int? blockNo, int? row, int? col)
        {
            var block = FindBlock(layout, blockNo);
            return block != null && row >= 0 && col >= 0 && row < block.Rows && col < block.Cols;
        }

        /// <summary>
        /// Description: Hình chữ nhật (toạ độ trong khối) có nằm trọn trong khối không.
        /// Created date: 22/10/2026
        /// </summary>
        public static bool IsRectInside(VwWallTopologyOutput layout, int? blockNo, int x, int y, int w, int h)
        {
            var block = FindBlock(layout, blockNo);
            if (block == null || x < 0 || y < 0 || w <= 0 || h <= 0)
                return false;

            return x + w <= block.Cols * layout.ScreenWidth
                && y + h <= block.Rows * layout.ScreenHeight;
        }


        public static string BlockLabel(int? blockNo)
            => blockNo is >= 0 and < 26 ? ((char)('A' + blockNo.Value)).ToString() : blockNo?.ToString() ?? string.Empty;

        /// <summary>
        /// Description: ID bố cục đang dùng (đã lưu). Chưa lưu bố cục ⇒ chặn (kịch bản phải gắn một bố cục thật).
        /// Created date: 22/10/2026
        /// </summary>
        public async Task<string> GetCurrentIdAsync()
        {
            var layout = await GetCurrentAsync();
            if (layout.IsDefault || string.IsNullOrWhiteSpace(layout.ID))
                throw Oops.Oh(BaseMsg.Vw.Exception.WallLayoutNotSaved);
            return layout.ID;
        }

        /// <summary>
        /// Description: Kịch bản phải thuộc bố cục đang dùng — chặn kích hoạt / gắn lịch, quy tắc / sửa cửa sổ
        ///              với kịch bản dựng trên phiên bản bố cục khác.
        /// Created date: 22/10/2026
        /// </summary>
        public async Task EnsureSceneOnCurrentLayoutAsync(VwScene scene)
        {
            var layout = await GetCurrentAsync();
            if (layout.IsDefault || scene.WallTopologyId != layout.ID)
                throw Oops.Oh(BaseMsg.Vw.Exception.SceneLayoutNotCurrent, scene.Name ?? scene.Code ?? scene.ID);
        }

        #endregion

        #region Thông số màn

        /// <summary>
        /// Description: Điền thông số panel theo màn hình mẫu của tường cho các trường còn trống.
        /// Created date: 22/10/2026
        /// </summary>
        public static void ApplyPanelDefaults(VwScreen screen, VwWallTopologyOutput layout)
        {
            if (string.IsNullOrWhiteSpace(screen.PanelModel)) screen.PanelModel = layout.DefaultPanelModel;
            if (string.IsNullOrWhiteSpace(screen.PanelType)) screen.PanelType = layout.DefaultPanelType;
            if (string.IsNullOrWhiteSpace(screen.Resolution)) screen.Resolution = layout.DefaultResolution;
            if (string.IsNullOrWhiteSpace(screen.CellRatio)) screen.CellRatio = layout.CellRatio;
            screen.PanelSize ??= layout.DefaultPanelSize;
        }

        #endregion

        #region Cổng ra trên lưới (VwControllerOutput)

        /// <summary>
        /// Description: Giữ vị trí màn (và cổng gán cho màn) khớp bố cục đang áp dụng và ghi CSDL:
        ///              màn nằm ở khối không thuộc bố cục / ô ngoài khối ⇒ về kho (bỏ luôn cổng);
        ///              ô không còn do bộ của cổng phụ trách ⇒ màn ở lại, bỏ cổng; khối đổi chỗ (giữ ID) ⇒ cập nhật BlockNo.
        ///              Sau đó chép vị trí màn sang cổng. Gọi sau khi sửa / áp dụng bố cục. Chạy trong transaction của nơi gọi (nếu có).
        /// Created date: 06/11/2026 — cập nhật 07/11/2026 (vị trí thuộc màn)
        /// </summary>
        public async Task RefreshOutputPlacementAsync()
        {
            var topology = await GetCurrentEntityAsync();
            var blocks = topology == null ? [] : await _blockRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.WallTopologyId == topology.ID)
                .ToListAsync();
            var outputs = await _outputRep.AsQueryable().Where(u => u.IsDelete == null).ToListAsync();
            var outputById = outputs.ToDictionary(u => u.ID);
            var screens = await _screenRep.AsQueryable().Where(u => u.IsDelete == null).ToListAsync();

            var changed = new List<VwScreen>();
            foreach (var screen in screens.Where(u => u.WallBlockId != null || u.ControllerOutputId != null))
            {
                var before = (screen.WallBlockId, screen.BlockNo, screen.ControllerOutputId);
                var block = blocks.FirstOrDefault(b => b.ID == screen.WallBlockId);
                var inside = block != null && screen.GridCol is int col && screen.GridRow is int row
                             && col >= 0 && row >= 0 && col < block.Cols && row < block.Rows;
                if (!inside)
                {
                    ClearPlacement(screen);
                    SetOutput(screen, null, DateTime.Now);
                }
                else
                {
                    screen.BlockNo = block!.BlockNo;
                    if (screen.ControllerOutputId != null
                        && (!outputById.TryGetValue(screen.ControllerOutputId, out var output)
                            || VwWallPlacement.CellOwner(block, screen.GridCol!.Value, screen.GridRow!.Value) != output.ControllerId))
                        SetOutput(screen, null, DateTime.Now);
                }

                if (before != (screen.WallBlockId, screen.BlockNo, screen.ControllerOutputId))
                    changed.Add(screen);
            }

            if (changed.Count > 0)
                await UpdateScreensAsync(changed);
            await CopyScreenPlacementToOutputsAsync(screens, outputs);

            _cache.RemoveByPrefixKey(CacheConst.Vw.VwControllerOutput);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);
        }

        /// <summary>
        /// Description: Lưu sơ đồ màn trong 1 transaction: mỗi phần tử là trạng thái cuối của 1 màn (vị trí + cổng gán).
        ///              Không kiểm ô thuộc bộ / trùng ô / trùng cổng (FE đã kiểm khi kéo thả); chỉ kiểm khối thuộc bố cục đang áp dụng.
        ///              Màn về kho ⇒ bỏ luôn cổng. Xong chép vị trí màn sang cổng (cổng không gán màn nào ⇒ ra ngoài lưới).
        /// Created date: 07/11/2026
        /// </summary>
        public async Task SaveWallMapAsync(List<VwWallMapScreenItem> items)
        {
            if (items == null || items.Count == 0)
                return;

            var screens = await _screenRep.AsQueryable().Where(u => u.IsDelete == null).ToListAsync();
            var screenById = screens.ToDictionary(u => u.ID);
            if (items.Any(i => !screenById.ContainsKey(i.ID)))
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var blocks = new Dictionary<string, VwWallBlock>();
            if (items.Any(i => !string.IsNullOrWhiteSpace(i.WallBlockId)))
            {
                var topology = await GetCurrentEntityAsync()
                    ?? throw Oops.Oh(BaseMsg.Vw.Exception.WallLayoutNotSaved);
                blocks = (await _blockRep.AsQueryable()
                        .Where(u => u.IsDelete == null && u.WallTopologyId == topology.ID)
                        .ToListAsync())
                    .ToDictionary(b => b.ID);
            }

            var now = DateTime.Now;
            var changed = new List<VwScreen>();
            foreach (var item in items)
            {
                var screen = screenById[item.ID];
                // Mỗi trang chỉ gửi phần của mình (SetPlacement / SetOutput) ⇒ không ghi đè phần trang kia vừa lưu
                if (item.SetPlacement && string.IsNullOrWhiteSpace(item.WallBlockId))
                {
                    ClearPlacement(screen);
                }
                else if (item.SetPlacement)
                {
                    if (!blocks.TryGetValue(item.WallBlockId!, out var block))
                        throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);
                    screen.WallBlockId = block.ID;
                    screen.BlockNo = block.BlockNo;
                    screen.GridCol = item.GridCol;
                    screen.GridRow = item.GridRow;
                }

                // Cổng chỉ gán cho màn đang trên lưới
                var requested = item.SetOutput ? (string.IsNullOrWhiteSpace(item.ControllerOutputId) ? null : item.ControllerOutputId) : screen.ControllerOutputId;
                SetOutput(screen, screen.WallBlockId == null ? null : requested, now);
                changed.Add(screen);
            }

            // Mỗi cổng chỉ gán cho 1 màn: cổng vừa gán ⇒ gỡ khỏi mọi màn khác (kể cả màn không gửi lên, VD dữ liệu cũ / người khác vừa sửa)
            var claimed = changed.Where(u => u.ControllerOutputId != null)
                .GroupBy(u => u.ControllerOutputId!)
                .ToDictionary(g => g.Key, g => g.Last().ID);
            foreach (var other in screens.Where(u => u.ControllerOutputId != null
                                                     && claimed.TryGetValue(u.ControllerOutputId, out var owner) && owner != u.ID))
            {
                SetOutput(other, null, now);
                if (!changed.Contains(other))
                    changed.Add(other);
            }

            var outputs = await _outputRep.AsQueryable().Where(u => u.IsDelete == null).ToListAsync();
            try
            {
                _screenRep.Context.Ado.BeginTran();
                await UpdateScreensAsync(changed);
                await CopyScreenPlacementToOutputsAsync(screens, outputs);
                _screenRep.Context.Ado.CommitTran();
            }
            catch
            {
                try { _screenRep.Context.Ado.RollbackTran(); } catch { /* không có tran đang mở */ }
                throw;
            }

            _cache.RemoveByPrefixKey(CacheConst.Vw.VwControllerOutput);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);
        }

        /// <summary>
        /// Description: Đổi cổng gán cho màn. Cổng đổi / bỏ ⇒ xoá thông số thiết bị (của tấm nối với cổng cũ, không còn đúng cho màn này)
        ///              — màn chờ lần đồng bộ sau.
        /// Created date: 07/11/2026
        /// </summary>
        private static void SetOutput(VwScreen screen, string? outputId, DateTime now)
        {
            if (outputId == screen.ControllerOutputId)
                return;
            screen.InstalledAt = outputId != null ? now : screen.InstalledAt;
            screen.RemovedAt = outputId == null ? now : null;
            screen.ControllerOutputId = outputId;
            screen.DeviceSerialNumber = null;
            screen.SoftwareVersion = null;
            screen.Temperature = null;
            screen.RunTimeHours = null;
            screen.ScreenState = null;
            screen.LastCheckedAt = null;
        }

        private Task<int> UpdateScreensAsync(List<VwScreen> screens)
            => _screenRep.AsUpdateable(screens)
                .UpdateColumns(u => new
                {
                    u.WallBlockId, u.BlockNo, u.GridCol, u.GridRow, u.ControllerOutputId, u.InstalledAt, u.RemovedAt,
                    u.DeviceSerialNumber, u.SoftwareVersion, u.Temperature, u.RunTimeHours, u.ScreenState, u.LastCheckedAt,
                })
                .ExecuteCommandAsync();

        /// <summary>
        /// Description: Vị trí cổng = vị trí màn đang giữ cổng đó (bản sao để các chỗ đọc vị trí cổng không phải đổi);
        ///              cổng không gán màn nào trên lưới ⇒ ra ngoài lưới. Chỉ ghi cổng thay đổi.
        /// Created date: 07/11/2026
        /// </summary>
        private async Task CopyScreenPlacementToOutputsAsync(List<VwScreen> screens, List<VwControllerOutput> outputs)
        {
            var holder = screens
                .Where(u => u.ControllerOutputId != null && u.WallBlockId != null)
                .GroupBy(u => u.ControllerOutputId!)
                .ToDictionary(g => g.Key, g => g.First());

            var changed = new List<VwControllerOutput>();
            foreach (var output in outputs)
            {
                holder.TryGetValue(output.ID, out var s);
                if (output.WallBlockId == s?.WallBlockId && output.BlockNo == s?.BlockNo && output.GridCol == s?.GridCol && output.GridRow == s?.GridRow)
                    continue;
                output.WallBlockId = s?.WallBlockId;
                output.BlockNo = s?.BlockNo;
                output.GridCol = s?.GridCol;
                output.GridRow = s?.GridRow;
                changed.Add(output);
            }

            if (changed.Count > 0)
                await _outputRep.AsUpdateable(changed)
                    .UpdateColumns(u => new { u.WallBlockId, u.BlockNo, u.GridCol, u.GridRow })
                    .ExecuteCommandAsync();
        }

        private static void ClearPlacement(VwScreen screen)
        {
            screen.WallBlockId = null;
            screen.BlockNo = null;
            screen.GridCol = null;
            screen.GridRow = null;
        }

        #endregion

        #region Bộ điều khiển trên ô (VwWallBlock.ControllerConfig)

        /// <summary>
        /// Description: Kiểm bộ được gán vào ô trong các khối sắp lưu: phải tồn tại, đang dùng, là Bộ hiển thị / Bộ độc lập.
        ///              Khối không gửi ControllerConfig (null) ⇒ giữ cấu hình đang lưu, không kiểm lại.
        /// Created date: 07/11/2026
        /// </summary>
        public async Task EnsureControllerConfigValidAsync(IEnumerable<VwWallBlock> blocks)
        {
            var ids = blocks
                .SelectMany(b => VwWallPlacement.ParseCells(b.ControllerConfig))
                .Select(c => c.ControllerId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();
            if (ids.Count == 0)
                return;

            var valid = await _controllerRep.AsQueryable()
                .Where(u => u.IsDelete == null && ids.Contains(u.ID) && u.Status == BaseEnums.StatusEnum.Enable
                            && (u.Role == VwControllerRole.Display || u.Role == VwControllerRole.Standalone))
                .Select(u => u.ID)
                .ToListAsync();
            var invalid = ids.Except(valid).ToList();
            if (invalid.Count > 0)
                throw Oops.Oh(BaseMsg.Vw.Exception.BlockControllerInvalid, string.Join(", ", invalid));
        }

        /// <summary>
        /// Description: Bộ có đang được gán vào ô của bố cục đang áp dụng không (chặn xoá / tắt / đổi sang bộ phân phối).
        /// Created date: 07/11/2026
        /// </summary>
        public async Task<bool> IsControllerAssignedAsync(string controllerId)
        {
            var layout = await GetCurrentAsync();
            return layout.Blocks.Any(b => VwWallPlacement.ParseCells(b.ControllerConfig).Any(c => c.ControllerId == controllerId));
        }

        #endregion
    }
}
