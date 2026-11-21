using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.WallPermission;
using Module.VideoWall.Core.Dto.WallTopology;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Exceptions;
using Module.VideoWall.Core.Helper;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Layout;
using Module.VideoWall.Infrastructure.Services.Schedule;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.VideoWall.Controllers.WallTopology.Commands
{
    /// <summary>
    /// Description: Xử lý ghi bố cục tường (VwWallTopology + VwWallBlock); cổng ra tự đặt theo bố cục đang áp dụng
    /// Created date: 09/09/2026 — cập nhật 22/10/2026 (bố cục nhiều khối, tạo màn từ ô trống)
    /// </summary>
    public class VwWallTopologyCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwWallTopology> _vwWallTopologyRep;
        private readonly BaseRepository<VwWallBlock> _vwWallBlockRep;
        private readonly BaseRepository<VwScene> _vwSceneRep;
        private readonly BaseRepository<VwWallPermission> _vwWallPermissionRep;
        private readonly BaseRepository<VwWindowScene> _vwWindowSceneRep;
        private readonly BaseRepository<VwWallPermissionMember> _vwWallPermissionMemberRep;
        private readonly BaseRepository<VwSchedule> _vwScheduleRep;
        private readonly BaseRepository<VwEventRule> _vwEventRuleRep;
        private readonly BaseRepository<VwController> _vwControllerRep;
        private readonly VwScheduleJobService _jobService;
        private readonly VwWallLayoutService _layout;
        private readonly BaseCacheService _cache;

        public VwWallTopologyCommandHandler(
            BaseRepository<VwWallTopology> vwWallTopologyRep,
            BaseRepository<VwWallBlock> vwWallBlockRep,
            BaseRepository<VwScene> vwSceneRep,
            BaseRepository<VwWallPermission> vwWallPermissionRep,
            BaseRepository<VwWindowScene> vwWindowSceneRep,
            BaseRepository<VwWallPermissionMember> vwWallPermissionMemberRep,
            BaseRepository<VwSchedule> vwScheduleRep,
            BaseRepository<VwEventRule> vwEventRuleRep,
            BaseRepository<VwController> vwControllerRep,
            VwScheduleJobService jobService,
            VwWallLayoutService layout,
            BaseCacheService cache)
        {
            _vwWallTopologyRep = vwWallTopologyRep;
            _vwWallBlockRep = vwWallBlockRep;
            _vwSceneRep = vwSceneRep;
            _vwWallPermissionRep = vwWallPermissionRep;
            _vwWindowSceneRep = vwWindowSceneRep;
            _vwWallPermissionMemberRep = vwWallPermissionMemberRep;
            _vwScheduleRep = vwScheduleRep;
            _vwEventRuleRep = vwEventRuleRep;
            _vwControllerRep = vwControllerRep;
            _jobService = jobService;
            _layout = layout;
            _cache = cache;
        }

        #region Danh sách bố cục: thêm / sửa / xoá / đặt đang dùng

        /// <summary>
        /// Description: Thêm bố cục tường (mã duy nhất). Không phải bố cục đang dùng, trừ khi hệ thống chưa có bố cục nào.
        /// Created date: 26/10/2026
        /// </summary>
        public async Task HandleAsync(VwAddWallTopologyInput command)
        {
            command.Code = command.Code?.Trim();
            await EnsureCodeUniqueAsync(command.Code, null);
            var hasCurrent = await _layout.GetCurrentEntityAsync() != null;

            try
            {
                _vwWallTopologyRep.Context.Ado.BeginTran();

                var entity = new VwWallTopology();
                AssignInfo(entity, command);
                // Chưa có bố cục nào ⇒ bố cục đầu tiên được áp dụng luôn (và phải ở trạng thái Dùng)
                entity.IsApply = hasCurrent ? BaseEnums.YesNoEnum.N : BaseEnums.YesNoEnum.Y;
                entity.Status = hasCurrent ? command.Status ?? BaseEnums.StatusEnum.Enable : BaseEnums.StatusEnum.Enable;
                await _vwWallTopologyRep.InsertWithDiffLogAsync(entity);
                // Bố cục mới / nhân bản: không chép gán bộ điều khiển vào ô
                command.Blocks.ForEach(b => b.ControllerConfig = null);
                await SaveBlocksAsync(entity.ID, command.Blocks);

                _vwWallTopologyRep.Context.Ado.CommitTran();
            }
            catch
            {
                try { _vwWallTopologyRep.Context.Ado.RollbackTran(); } catch { /* không có tran đang mở */ }
                throw;
            }

            _layout.RemoveCache();
        }

        /// <summary>
        /// Description: Sửa bố cục tường.
        ///              - Bố cục đang dùng: đổi cấu trúc theo quy tắc EnsureStructureChangeAllowedAsync (mở rộng được, thu nhỏ khi phần cắt trống).
        ///              - Bố cục khác: sửa tự do — kịch bản / lập lịch của nó đã ở trạng thái không dùng; cửa sổ nằm ngoài lưới mới
        ///                sẽ bị chặn khi bật lại kịch bản.
        /// Created date: 26/10/2026
        /// </summary>
        public async Task HandleAsync(VwUpdateWallTopologyInput command)
        {
            var entity = await _vwWallTopologyRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            command.Code = command.Code?.Trim();
            await EnsureCodeUniqueAsync(command.Code, entity.ID);
            await EnsureCellRatioChangeAllowedAsync(entity, command.CellRatio);
            await EnsureCellCountMatchAsync(entity, command);
            await _layout.EnsureControllerConfigValidAsync(command.Blocks);

            // Đổi chỗ / xoá khối: số khối cũ → số khối mới (null = khối bị xoá)
            var current = await _layout.GetByIdAsync(entity.ID);
            var blockMap = BuildBlockMap(current, command.Blocks);
            var reordered = blockMap.Any(p => p.Value != p.Key);

            var isApplied = entity.IsApply == BaseEnums.YesNoEnum.Y;
            if (isApplied)
            {
                var next = VwWallLayoutService.BuildOutput(command, command.Blocks);
                if (reordered || IsStructureChanged(current, next))
                    await EnsureStructureChangeAllowedAsync(current, next, blockMap);
            }

            try
            {
                _vwWallTopologyRep.Context.Ado.BeginTran();

                AssignInfo(entity, command);
                // Bật / tắt trạng thái (switch ở danh sách); bố cục đang áp dụng luôn giữ Dùng
                entity.Status = isApplied ? BaseEnums.StatusEnum.Enable : command.Status ?? entity.Status;
                await _vwWallTopologyRep.UpdateWithDiffLogAsync(entity, false);
                if (reordered)
                    await RemapBlockDataAsync(entity, blockMap);
                await SaveBlocksAsync(entity.ID, command.Blocks);
                // Cổng ra nằm ở ô không còn hợp lệ ⇒ ra ngoài lưới
                if (isApplied)
                    await _layout.RefreshOutputPlacementAsync();

                _vwWallTopologyRep.Context.Ado.CommitTran();
            }
            catch
            {
                try { _vwWallTopologyRep.Context.Ado.RollbackTran(); } catch { /* không có tran đang mở */ }
                throw;
            }

            _layout.RemoveCache();
            if (reordered)
            {
                _cache.RemoveByPrefixKey(CacheConst.Vw.VwWallPermission);
                _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);
                RemoveSceneCaches();
            }
        }

        /// <summary>
        /// Description: Số khối cũ → số khối mới khi đổi chỗ / xoá khối (khối cũ không còn ⇒ null).
        ///              Không khối nào gửi SourceBlockNo ⇒ khớp theo BlockNo (giữ nguyên số khối như trước).
        /// Created date: 28/10/2026
        /// </summary>
        private static Dictionary<int, int?> BuildBlockMap(VwWallTopologyOutput current, List<VwWallBlockInput> blocks)
        {
            var bySource = blocks.Any(u => u.SourceBlockNo != null);
            var map = new Dictionary<int, int?>();
            foreach (var block in current.Blocks)
            {
                if (block.BlockNo is not int oldNo)
                    continue;
                var input = blocks.FirstOrDefault(u => (bySource ? u.SourceBlockNo : u.BlockNo) == oldNo);
                map[oldNo] = input?.BlockNo;
            }
            return map;
        }

        /// <summary>
        /// Description: Đánh số lại khối cho dữ liệu đang gắn với lưới theo vị trí mới của khối (chạy trong transaction của Update):
        ///              - Cửa sổ kịch bản của bố cục: đổi BlockNo (WallBlockId giữ nguyên vì khối giữ ID khi đổi chỗ).
        ///              - Vùng phân quyền của bố cục: đổi số khối trong Config; ô của khối bị xoá bị bỏ.
        ///              Cổng ra không đánh số lại ở đây: RefreshOutputPlacementAsync cập nhật BlockNo theo ID khối.
        ///              Bố cục đang áp dụng đã kiểm ở EnsureStructureChangeAllowedAsync: khối bị xoá phải không còn dữ liệu.
        /// Created date: 28/10/2026 — cập nhật 06/11/2026
        /// </summary>
        private async Task RemapBlockDataAsync(VwWallTopology topology, Dictionary<int, int?> blockMap)
        {
            int? Map(int? blockNo) => blockNo is int no && blockMap.TryGetValue(no, out var next) ? next : blockNo;

            var sceneIds = await _vwSceneRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.WallTopologyId == topology.ID)
                .Select(u => u.ID)
                .ToListAsync();
            if (sceneIds.Count > 0)
            {
                var windows = await _vwWindowSceneRep.AsQueryable()
                    .Where(u => u.IsDelete == null && sceneIds.Contains(u.SceneId) && u.BlockNo != null)
                    .ToListAsync();
                foreach (var window in windows)
                {
                    var next = Map(window.BlockNo);
                    if (next == window.BlockNo)
                        continue;
                    window.BlockNo = next;
                    await _vwWindowSceneRep.UpdateWithDiffLogAsync(window, false);
                }
            }

            var permissions = await _vwWallPermissionRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.WallTopologyId == topology.ID && u.IsFullAccess != true)
                .ToListAsync();
            foreach (var permission in permissions)
            {
                List<VwGridCell> cells;
                try
                {
                    cells = string.IsNullOrWhiteSpace(permission.Config) ? [] : JsonConvert.DeserializeObject<List<VwGridCell>>(permission.Config) ?? [];
                }
                catch (JsonException)
                {
                    continue;
                }
                var remapped = cells
                    .Select(c => (Cell: c, Next: Map(c.Block)))
                    .Where(x => x.Next != null)
                    .Select(x => new VwGridCell { Block = x.Next!.Value, Row = x.Cell.Row, Col = x.Cell.Col })
                    .ToList();
                permission.Config = JsonConvert.SerializeObject(remapped);
                await _vwWallPermissionRep.UpdateWithDiffLogAsync(permission);
            }
        }

        /// <summary>
        /// Description: Xoá mềm bố cục (không phải bố cục đang dùng) cùng khối, kịch bản (+ cửa sổ), lập lịch / liên kết sự kiện
        ///              trỏ tới các kịch bản đó, và phân quyền vùng (+ thành viên) của bố cục.
        /// Created date: 26/10/2026
        /// </summary>
        public async Task HandleAsync(VwDeleteWallTopologyInput command)
        {
            var entity = await _vwWallTopologyRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);
            if (entity.IsApply == BaseEnums.YesNoEnum.Y)
                throw Oops.Oh(BaseMsg.Vw.Exception.WallTopologyCurrentCannotDelete, entity.Code ?? entity.Name ?? entity.ID);

            var sceneIds = await _vwSceneRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.WallTopologyId == entity.ID)
                .Select(u => u.ID)
                .ToListAsync();
            var scheduleIds = sceneIds.Count == 0 ? new List<string>() : await _vwScheduleRep.AsQueryable()
                .Where(u => u.IsDelete == null && sceneIds.Contains(u.TargetSceneId))
                .Select(u => u.ID)
                .ToListAsync();
            var permissionIds = await _vwWallPermissionRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.WallTopologyId == entity.ID)
                .Select(u => u.ID)
                .ToListAsync();

            try
            {
                _vwWallTopologyRep.Context.Ado.BeginTran();

                if (sceneIds.Count > 0)
                {
                    await _vwWindowSceneRep.SoftDeleteAsync<VwWindowScene>(u => u.IsDelete == null && sceneIds.Contains(u.SceneId));
                    await _vwScheduleRep.SoftDeleteAsync<VwSchedule>(u => u.IsDelete == null && sceneIds.Contains(u.TargetSceneId));
                    await _vwEventRuleRep.SoftDeleteAsync<VwEventRule>(u => u.IsDelete == null && sceneIds.Contains(u.TargetSceneId));
                    await _vwSceneRep.SoftDeleteAsync<VwScene>(u => sceneIds.Contains(u.ID));
                }
                if (permissionIds.Count > 0)
                {
                    await _vwWallPermissionMemberRep.SoftDeleteAsync<VwWallPermissionMember>(u => u.IsDelete == null && permissionIds.Contains(u.PermissionId));
                    await _vwWallPermissionRep.SoftDeleteAsync<VwWallPermission>(u => permissionIds.Contains(u.ID));
                }
                await _vwWallBlockRep.SoftDeleteAsync<VwWallBlock>(u => u.IsDelete == null && u.WallTopologyId == entity.ID);
                await _vwWallTopologyRep.SoftDeleteAsync<VwWallTopology>(entity);

                _vwWallTopologyRep.Context.Ado.CommitTran();
            }
            catch
            {
                try { _vwWallTopologyRep.Context.Ado.RollbackTran(); } catch { /* không có tran đang mở */ }
                throw;
            }

            foreach (var id in scheduleIds)
                _jobService.RemoveJob(id);

            _layout.RemoveCache();
            RemoveSceneCaches();
        }

        /// <summary>
        /// Description: Áp dụng 1 bố cục lên tường (nút "Áp dụng"), trong 1 transaction:
        ///              - Bố cục được chọn: IsApply = 1 và trạng thái Dùng; các bố cục khác IsApply = 0 (trạng thái giữ nguyên).
        ///              - Cổng ra gán tay vào ô: ô không thuộc bố cục mới ⇒ cổng ra ngoài lưới (gán lại ở Sơ đồ cổng ra).
        ///              - Đổi sang bố cục khác coi như bắt đầu mới:
        ///                + Không còn kịch bản nào đang phát: xoá ActiveSceneId của bộ điều khiển, cờ ActiveScene = 0 toàn bộ.
        ///              - Kịch bản thuộc bố cục khác ⇒ Không dùng; lập lịch / liên kết sự kiện trỏ tới các kịch bản đó ⇒ Không dùng
        ///                (gỡ job Hangfire). Phân quyền vùng của bố cục khác ⇒ Không dùng.
        ///                Đổi lại bố cục cũ KHÔNG tự bật lại — người dùng tự bật.
        ///              Cờ kịch bản mặc định giữ nguyên: mỗi bố cục có kịch bản mặc định riêng.
        /// Created date: 22/10/2026 — cập nhật 27/10/2026 (đổi bố cục: màn về ngoài lưới, xoá kịch bản đang phát)
        /// </summary>
        public async Task HandleAsync(VwSetCurrentWallTopologyInput command)
        {
            var target = await _vwWallTopologyRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);
            var isSwitch = target.IsApply != BaseEnums.YesNoEnum.Y;

            // Kịch bản ngoài bố cục được chọn + lập lịch / liên kết sự kiện đang bật trỏ tới chúng
            var otherSceneIds = await _vwSceneRep.AsQueryable()
                .Where(u => u.IsDelete == null && (u.WallTopologyId == null || u.WallTopologyId != target.ID))
                .Select(u => u.ID)
                .ToListAsync();
            var schedules = otherSceneIds.Count == 0 ? new List<VwSchedule>() : await _vwScheduleRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.Status == BaseEnums.StatusEnum.Enable && otherSceneIds.Contains(u.TargetSceneId))
                .ToListAsync();

            // Phân quyền vùng thuộc bố cục khác đang bật ⇒ Không dùng (toạ độ ô chỉ đúng trong bố cục của nó).
            // Quyền chưa gắn bố cục (dữ liệu trước 22/10/2026) coi như thuộc bố cục đang dùng ⇒ giữ nguyên.
            var otherPermissionIds = await _vwWallPermissionRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.Status == BaseEnums.StatusEnum.Enable && u.WallTopologyId != null && u.WallTopologyId != target.ID)
                .Select(u => u.ID)
                .ToListAsync();

            try
            {
                _vwWallTopologyRep.Context.Ado.BeginTran();

                var others = await _vwWallTopologyRep.AsQueryable()
                    .Where(u => u.IsDelete == null && u.IsApply == BaseEnums.YesNoEnum.Y && u.ID != target.ID)
                    .ToListAsync();
                foreach (var other in others)
                {
                    other.IsApply = BaseEnums.YesNoEnum.N;
                    await _vwWallTopologyRep.UpdateWithDiffLogAsync(other);
                }
                target.IsApply = BaseEnums.YesNoEnum.Y;
                target.Status = BaseEnums.StatusEnum.Enable;
                await _vwWallTopologyRep.UpdateWithDiffLogAsync(target);

                await _layout.RefreshOutputPlacementAsync();

                if (isSwitch)
                {
                    await _vwControllerRep.AsUpdateable()
                        .SetColumns(u => new VwController { ActiveSceneId = null, ActiveSceneAt = null })
                        .Where(u => u.ActiveSceneId != null)
                        .ExecuteCommandAsync();
                    await _vwSceneRep.AsUpdateable()
                        .SetColumns(u => new VwScene { ActiveScene = BaseEnums.ActiveScene.DeActivate })
                        .Where(u => u.ActiveScene == BaseEnums.ActiveScene.Activate)
                        .ExecuteCommandAsync();
                }

                if (otherSceneIds.Count > 0)
                {
                    await _vwSceneRep.AsUpdateable()
                        .SetColumns(u => new VwScene { Status = BaseEnums.StatusEnum.Disable })
                        .Where(u => otherSceneIds.Contains(u.ID) && u.Status == BaseEnums.StatusEnum.Enable)
                        .ExecuteCommandAsync();
                    await _vwEventRuleRep.AsUpdateable()
                        .SetColumns(u => new VwEventRule { Status = BaseEnums.StatusEnum.Disable })
                        .Where(u => u.IsDelete == null && otherSceneIds.Contains(u.TargetSceneId) && u.Status == BaseEnums.StatusEnum.Enable)
                        .ExecuteCommandAsync();
                    foreach (var schedule in schedules)
                    {
                        schedule.Status = BaseEnums.StatusEnum.Disable;
                        schedule.NextRun = null;
                        await _vwScheduleRep.UpdateWithDiffLogAsync(schedule, false);
                    }
                }

                if (otherPermissionIds.Count > 0)
                    await _vwWallPermissionRep.AsUpdateable()
                        .SetColumns(u => new VwWallPermission { Status = BaseEnums.StatusEnum.Disable })
                        .Where(u => otherPermissionIds.Contains(u.ID))
                        .ExecuteCommandAsync();

                _vwWallTopologyRep.Context.Ado.CommitTran();
            }
            catch
            {
                try { _vwWallTopologyRep.Context.Ado.RollbackTran(); } catch { /* không có tran đang mở */ }
                throw;
            }

            foreach (var schedule in schedules)
                _jobService.RemoveJob(schedule.ID);

            _layout.RemoveCache();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwWallPermission);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwController);
            RemoveSceneCaches();
        }

        /// <summary>
        /// Description: Mã bố cục bắt buộc (validator) và không trùng bố cục khác.
        /// Created date: 26/10/2026
        /// </summary>
        private async Task EnsureCodeUniqueAsync(string? code, string? exceptId)
        {
            if (string.IsNullOrWhiteSpace(code))
                return;
            if (await _vwWallTopologyRep.IsAnyAsync(u => u.IsDelete == null && u.Code == code && u.ID != exceptId))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);
        }

        /// <summary>
        /// Description: Gán thông tin chung của bố cục (kể cả null — lưu với ignoreAllNullColumns = false). Không đụng Status.
        /// Created date: 26/10/2026
        /// </summary>
        private static void AssignInfo(VwWallTopology target, VwWallTopology source)
        {
            target.Code = source.Code;
            target.Name = source.Name;
            target.CellRatio = NormalizeCellRatio(source.CellRatio);
            target.CellCount = source.CellCount;
            target.DefaultPanelModel = source.DefaultPanelModel;
            target.DefaultPanelType = source.DefaultPanelType;
            target.DefaultPanelSize = source.DefaultPanelSize;
            target.DefaultResolution = source.DefaultResolution;
            target.Remark = source.Remark;
        }

        /// <summary>
        /// Description: Sửa lưới (khối) của bố cục có số ô ⇒ tổng cột × hàng của các khối phải đúng bằng số ô.
        ///              Chỉ đổi thông tin (mã, tên, số ô…) mà giữ nguyên lưới thì không kiểm — sửa lưới cho khớp ở tab Bố cục tường.
        /// Created date: 28/10/2026
        /// </summary>
        private async Task EnsureCellCountMatchAsync(VwWallTopology entity, VwAddWallTopologyInput command)
        {
            if (command.CellCount is not int cellCount)
                return;

            var current = await _layout.GetByIdAsync(entity.ID);
            var next = VwWallLayoutService.BuildOutput(command, command.Blocks);
            var gridChanged = !current.Blocks.Select(u => (u.BlockNo, u.Cols, u.Rows))
                .SequenceEqual(next.Blocks.Select(u => (u.BlockNo, u.Cols, u.Rows)));
            if (gridChanged && next.TotalScreens != cellCount)
                throw Oops.Oh(BaseMsg.Vw.Exception.WallCellCountMismatch, next.TotalScreens, cellCount);
        }

        /// <summary>
        /// Description: Tỉ lệ ô để trống ⇒ tỉ lệ mặc định của profile.
        /// Created date: 27/10/2026
        /// </summary>
        private static string NormalizeCellRatio(string? ratio)
            => string.IsNullOrWhiteSpace(ratio) ? VwWallProfile.DefaultCellRatio : ratio.Trim();

        /// <summary>
        /// Description: Chặn đổi tỉ lệ ô khi bố cục (đang dùng hay không) đã có kịch bản: cửa sổ đã dựng theo hình dạng ô cũ
        ///              sẽ bị méo. Muốn đổi thì nhân bản thành bố cục mới rồi đổi tỉ lệ.
        /// Created date: 27/10/2026
        /// </summary>
        private async Task EnsureCellRatioChangeAllowedAsync(VwWallTopology entity, string? nextRatio)
        {
            if (NormalizeCellRatio(entity.CellRatio) == NormalizeCellRatio(nextRatio))
                return;

            var sceneCount = await _vwSceneRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.WallTopologyId == entity.ID)
                .CountAsync();
            if (sceneCount > 0)
                throw Oops.Oh(BaseMsg.Vw.Exception.WallCellRatioLocked, sceneCount);
        }

        private void RemoveSceneCaches()
        {
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScene);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwSchedule);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwEventRule);
        }

        #endregion

        /// <summary>
        /// Description: Lưu bố cục tường (thông tin chung + toàn bộ khối) trong 1 transaction.
        ///
        ///              Ghi vào BỐ CỤC ĐANG DÙNG (chưa có bố cục nào thì khởi tạo, mã BC01), sửa tại chỗ.
        ///              Các bố cục khác quản lý qua Add / Update / Delete / SetCurrent.
        ///              Khối mang ControllerConfig (ô nào do bộ nào phụ trách); cổng ra gán tay vào ô ở Sơ đồ cổng ra.
        ///              Đổi cấu trúc khi đã có dữ liệu trên lưới:
        ///                - Thu nhỏ / bỏ khối chỉ được khi phần bị cắt không có cửa sổ kịch bản, vùng phân quyền.
        ///                - Kích thước ô khoá khi đã có kịch bản (toạ độ cửa sổ tính theo pixel của ô).
        /// Created date: 22/10/2026 — cập nhật 06/11/2026 (bỏ thêm / sửa / xoá màn)
        /// </summary>
        public async Task HandleAsync(VwSaveWallLayoutInput command)
        {
            // 1. Bố cục mới (chưa ghi). Không gửi tỉ lệ ô ⇒ giữ tỉ lệ đang lưu.
            var currentLayout = await _layout.GetCurrentAsync();
            if (string.IsNullOrWhiteSpace(command.CellRatio))
                command.CellRatio = currentLayout.CellRatio;
            var layout = VwWallLayoutService.BuildOutput(command, command.Blocks);
            var structureChanged = !currentLayout.IsDefault && IsStructureChanged(currentLayout, layout);
            if (structureChanged)
                await EnsureStructureChangeAllowedAsync(currentLayout, layout);
            // Đổi lưới thì tổng ô phải khớp số ô đang lưu
            if (structureChanged && currentLayout.CellCount is int cellCount && layout.TotalScreens != cellCount)
                throw Oops.Oh(BaseMsg.Vw.Exception.WallCellCountMismatch, layout.TotalScreens, cellCount);
            await _layout.EnsureControllerConfigValidAsync(command.Blocks);

            // 2. Ghi bố cục + khối; cổng ra ở ô không còn hợp lệ ⇒ ra ngoài lưới
            try
            {
                _vwWallTopologyRep.Context.Ado.BeginTran();

                await SaveTopologyAsync(command);
                await _layout.RefreshOutputPlacementAsync();

                _vwWallTopologyRep.Context.Ado.CommitTran();
            }
            catch
            {
                // Không rollback thì transaction treo lại trên connection và làm hỏng các lệnh sau
                try { _vwWallTopologyRep.Context.Ado.RollbackTran(); } catch { /* không có tran đang mở */ }
                throw;
            }

            _layout.RemoveCache();
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScene);
        }

        /// <summary>
        /// Description: Chặn đổi cấu trúc BỐ CỤC ĐANG DÙNG làm hỏng dữ liệu đang gắn với lưới (sửa tại chỗ):
        ///              - Đổi tỉ lệ ô khi bố cục đã có kịch bản ⇒ cửa sổ dựng theo hình dạng ô cũ bị méo.
        ///              - Thu nhỏ / bỏ khối mà còn cửa sổ kịch bản, vùng phân quyền đang trong lưới cũ nhưng ra ngoài lưới mới.
        ///              Liệt kê đối tượng vướng để người dùng gỡ trước; không tự dời / xoá gì.
        /// Created date: 26/10/2026 — cập nhật 27/10/2026 (tỉ lệ ô thay kích thước ô)
        /// </summary>
        private async Task EnsureStructureChangeAllowedAsync(VwWallTopologyOutput current, VwWallTopologyOutput next, Dictionary<int, int?>? blockMap = null)
        {
            var scenes = await _vwSceneRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.WallTopologyId == current.ID)
                .ToListAsync();

            if (current.CellRatio != next.CellRatio && scenes.Count > 0)
                throw Oops.Oh(BaseMsg.Vw.Exception.WallCellRatioLocked, scenes.Count);

            // Đổi chỗ / xoá khối: khối cũ tương ứng khối nào ở bố cục mới (khối bị xoá ⇒ null ⇒ coi như rơi ra ngoài)
            int? Map(int? block) => blockMap != null && block is int no && blockMap.TryGetValue(no, out var mapped) ? mapped : block;

            // Chỉ tính đối tượng đang hợp lệ ở bố cục cũ mà rơi ra ngoài bố cục mới — dữ liệu vốn đã lệch không chặn việc sửa
            bool CellLost(int? block, int? row, int? col)
                => VwWallLayoutService.IsCellInside(current, block, row, col) && !VwWallLayoutService.IsCellInside(next, Map(block), row, col);

            // Cổng ra (màn) ở ô bị cắt chỉ ra ngoài lưới (RefreshOutputPlacementAsync) ⇒ không chặn theo màn
            var lostScreens = new List<string>();

            var lostScenes = new List<string>();
            if (scenes.Count > 0)
            {
                var sceneIds = scenes.Select(u => u.ID).ToList();
                var windows = await _vwWindowSceneRep.AsQueryable()
                    .Where(u => u.IsDelete == null && sceneIds.Contains(u.SceneId))
                    .ToListAsync();
                var outside = windows
                    .Where(u => VwWallLayoutService.IsRectInside(current, u.BlockNo, u.X ?? 0, u.Y ?? 0, u.W ?? 0, u.H ?? 0)
                             && !VwWallLayoutService.IsRectInside(next, Map(u.BlockNo), u.X ?? 0, u.Y ?? 0, u.W ?? 0, u.H ?? 0))
                    .Select(u => u.SceneId)
                    .ToHashSet();
                lostScenes.AddRange(scenes.Where(u => outside.Contains(u.ID)).Select(u => u.Name ?? u.ID));
            }

            var lostPermissions = new List<string>();
            var permissions = await _vwWallPermissionRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.WallTopologyId == current.ID && u.IsFullAccess != true)
                .ToListAsync();
            foreach (var permission in permissions)
            {
                List<VwGridCell> cells;
                try
                {
                    cells = string.IsNullOrWhiteSpace(permission.Config) ? [] : JsonConvert.DeserializeObject<List<VwGridCell>>(permission.Config) ?? [];
                }
                catch (JsonException)
                {
                    continue;
                }
                if (cells.Any(c => CellLost(c.Block, c.Row, c.Col)))
                    lostPermissions.Add(permission.Name ?? permission.Code ?? permission.ID);
            }

            if (lostScreens.Count + lostScenes.Count + lostPermissions.Count > 0)
                throw Oops.Oh(BaseMsg.Vw.Exception.WallShrinkBlocked, JoinNames(lostScreens), JoinNames(lostScenes), JoinNames(lostPermissions));
        }

        /// <summary>
        /// Description: Ghép tên cho thông báo lỗi: tối đa 10 tên, còn lại ghi "…(+n)"; rỗng ⇒ "—".
        /// Created date: 26/10/2026
        /// </summary>
        private static string JoinNames(List<string> names)
        {
            const int max = 10;
            if (names.Count == 0)
                return "—";
            var text = string.Join(", ", names.Take(max));
            return names.Count > max ? $"{text} …(+{names.Count - max})" : text;
        }

        /// <summary>
        /// Description: Bố cục mới khác bố cục đang dùng về cấu trúc: tỉ lệ ô, số khối, hoặc lưới của một khối.
        ///              Tên / ghi chú / màn hình mẫu không tính là cấu trúc.
        /// Created date: 22/10/2026 — cập nhật 27/10/2026
        /// </summary>
        private static bool IsStructureChanged(VwWallTopologyOutput current, VwWallTopologyOutput next)
        {
            if (current.CellRatio != next.CellRatio)
                return true;

            var a = current.Blocks.Select(u => (u.BlockNo, u.Cols, u.Rows)).OrderBy(u => u.BlockNo);
            var b = next.Blocks.Select(u => (u.BlockNo, u.Cols, u.Rows)).OrderBy(u => u.BlockNo);
            return !a.SequenceEqual(b);
        }

        /// <summary>
        /// Description: Ghi bản ghi tường + khối.
        ///              - Chưa có bố cục ⇒ khởi tạo bố cục đầu tiên (mã BC01) làm bố cục đang dùng.
        ///              - Ngược lại ⇒ sửa tại chỗ bố cục đang dùng, đồng bộ khối theo BlockNo (giữ ID khối để màn / cửa sổ vẫn trỏ đúng).
        ///              Các trường gán tay để xoá được giá trị cũ (lưu với ignoreAllNullColumns = false). Mã bố cục chỉ sửa qua Update.
        /// Created date: 22/10/2026 — cập nhật 26/10/2026
        /// </summary>
        private async Task<(VwWallTopology Topology, List<VwWallBlock> Blocks)> SaveTopologyAsync(VwSaveWallLayoutInput command)
        {
            var entity = await _layout.GetCurrentEntityAsync();

            if (entity == null)
            {
                entity = new VwWallTopology();
                Assign(entity, command);
                entity.Code = string.IsNullOrWhiteSpace(command.Code) ? "BC01" : command.Code.Trim();
                entity.Status = BaseEnums.StatusEnum.Enable;
                entity.IsApply = BaseEnums.YesNoEnum.Y;
                await _vwWallTopologyRep.InsertWithDiffLogAsync(entity);
                return (entity, await SaveBlocksAsync(entity.ID, command.Blocks));
            }

            Assign(entity, command);
            entity.Status = BaseEnums.StatusEnum.Enable;
            entity.IsApply = BaseEnums.YesNoEnum.Y;
            await _vwWallTopologyRep.UpdateWithDiffLogAsync(entity, false);
            return (entity, await SaveBlocksAsync(entity.ID, command.Blocks));

            static void Assign(VwWallTopology target, VwWallTopology source)
            {
                target.Name = source.Name;
                target.CellRatio = NormalizeCellRatio(source.CellRatio);
                target.DefaultPanelModel = source.DefaultPanelModel;
                target.DefaultPanelType = source.DefaultPanelType;
                target.DefaultPanelSize = source.DefaultPanelSize;
                target.DefaultResolution = source.DefaultResolution;
                target.Remark = source.Remark;
            }
        }

        /// <summary>
        /// Description: Đồng bộ khối: khối đã có thì cập nhật (giữ ID), khối mới thì thêm,
        ///              khối không còn trong danh sách thì xoá mềm. Trả về các khối sau khi ghi.
        ///              Có SourceBlockNo (đổi chỗ / xoá khối) ⇒ khớp khối cũ theo SourceBlockNo rồi ghi số khối mới,
        ///              giữ ID khối để màn / cửa sổ (WallBlockId) vẫn trỏ đúng khối; không có ⇒ khớp theo BlockNo.
        /// Created date: 22/10/2026 — cập nhật 28/10/2026 (đổi chỗ / xoá khối)
        /// </summary>
        private async Task<List<VwWallBlock>> SaveBlocksAsync(string topologyId, List<VwWallBlockInput> blocks)
        {
            var current = await _vwWallBlockRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.WallTopologyId == topologyId)
                .ToListAsync();
            var result = new List<VwWallBlock>();
            var bySource = blocks.Any(u => u.SourceBlockNo != null);
            int? SourceOf(VwWallBlockInput input) => bySource ? input.SourceBlockNo : input.BlockNo;

            foreach (var input in blocks)
            {
                var source = SourceOf(input);
                var entity = source == null ? null : current.FirstOrDefault(u => u.BlockNo == source);
                if (entity == null)
                {
                    entity = new VwWallBlock
                    {
                        WallTopologyId = topologyId,
                        BlockNo = input.BlockNo,
                        Name = input.Name,
                        Cols = input.Cols,
                        Rows = input.Rows,
                        Remark = input.Remark,
                        ControllerConfig = VwWallPlacement.NormalizeCells(input.ControllerConfig, input.Cols ?? 0, input.Rows ?? 0),
                    };
                    await _vwWallBlockRep.InsertWithDiffLogAsync(entity);
                    result.Add(entity);
                    continue;
                }

                entity.BlockNo = input.BlockNo;
                entity.Name = input.Name;
                entity.Cols = input.Cols;
                entity.Rows = input.Rows;
                entity.Remark = input.Remark;
                // Không gửi ControllerConfig (null) ⇒ giữ cấu hình đang lưu; ô ngoài kích thước mới bị bỏ
                entity.ControllerConfig = VwWallPlacement.NormalizeCells(input.ControllerConfig ?? entity.ControllerConfig, input.Cols ?? 0, input.Rows ?? 0);
                await _vwWallBlockRep.UpdateWithDiffLogAsync(entity, false);
                result.Add(entity);
            }

            // Khối cũ không được khớp với khối nào trong danh sách mới ⇒ xoá
            foreach (var stale in current.Where(u => !result.Contains(u)))
                await _vwWallBlockRep.SoftDeleteAsync<VwWallBlock>(stale);

            return result;
        }
    }
}
