using Mapster;
using Module.VideoWall.Core.Dto.Screen;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.VideoWall.Controllers.Screen.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn dữ liệu VwScreen (kèm cổng ra đang cắm, chỉ đọc)
    /// Created date: 2026-08-01 — cập nhật 06/11/2026
    /// </summary>
    public class VwScreenQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwScreen> _vwScreenRep;
        private readonly BaseRepository<VwControllerOutput> _vwOutputRep;

        public VwScreenQueryHandler(BaseRepository<VwScreen> vwScreenRep, BaseRepository<VwControllerOutput> vwOutputRep)
        {
            _vwScreenRep = vwScreenRep;
            _vwOutputRep = vwOutputRep;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách VwScreen theo phân trang
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<SqlSugarPagedList<VwPageScreenOutput>> HandleAsync(VwPageScreenInput command)
        {
            var pagedList = await _vwScreenRep.AsQueryable()
                // Ô tìm "Mã / Tên": khớp mã HOẶC tên
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => (u.Name != null && u.Name.Contains(command.Name!)) || (u.Code != null && u.Code.Contains(command.Name!)))
                .WhereIF(!string.IsNullOrWhiteSpace(command.ControllerOutputId), u => u.ControllerOutputId == command.ControllerOutputId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.SerialNumber), u => u.SerialNumber != null && u.SerialNumber.Contains(command.SerialNumber!))
                .WhereIF(command.Installed == true, u => u.ControllerOutputId != null)
                .WhereIF(command.Installed == false, u => u.ControllerOutputId == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PanelType), u => u.PanelType != null && u.PanelType.Contains(command.PanelType!))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select(u => new VwPageScreenOutput() { }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            await FillOutputAsync(pagedList.Records);
            return pagedList;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy toàn bộ danh sách VwScreen theo tiêu chí lọc (không phân trang)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<List<VwScreenOutput>> HandleAsync(VwScreenInput command)
        {
            // 1. Đọc thẳng DB (kèm cổng ra gán cho màn) — KHÔNG cache: service ITS.VideoWall ghi DB (đồng bộ màn, trạng thái màn)
            //    mà không xoá được cache trong bộ nhớ của BE ⇒ cache làm trang thấy dữ liệu cũ (07/11/2026)
            var entities = await _vwScreenRep.AsQueryable()
                .Select((u) => new VwScreenOutput() { }, true)
                .OrderBy(u => u.CreateTime)
                .ToListAsync();
            await FillOutputAsync(entities);

            // 2. Lọc theo tiêu chí tìm kiếm
            var filtered = entities.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                // Khớp mã HOẶC tên (giống API phân trang)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => (!string.IsNullOrWhiteSpace(u.Name) && u.Name!.Contains(command.Name!, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(u.Code) && u.Code!.Contains(command.Name!, StringComparison.OrdinalIgnoreCase)))
                .WhereIF(!string.IsNullOrWhiteSpace(command.ControllerOutputId), u => u.ControllerOutputId == command.ControllerOutputId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.SerialNumber), u => !string.IsNullOrWhiteSpace(u.SerialNumber) && u.SerialNumber!.Contains(command.SerialNumber!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(command.Installed == true, u => u.ControllerOutputId != null)
                .WhereIF(command.Installed == false, u => u.ControllerOutputId == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.PanelType), u => !string.IsNullOrWhiteSpace(u.PanelType) && u.PanelType!.Contains(command.PanelType!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .OrderBy(st => st.CreateTime)
                .ToList();

            return filtered;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 VwScreen theo ID
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<VwScreenOutput> HandleAsync(VwIdScreenInput command)
        {
            var entity = await _vwScreenRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var output = entity.Adapt<VwScreenOutput>();
            await FillOutputAsync([output]);
            return output;
        }

        /// <summary>Ghép thông tin cổng ra gán cho màn (bộ điều khiển, tên cổng). Vị trí là của chính màn.</summary>
        private async Task FillOutputAsync<T>(IEnumerable<T> screens) where T : VwScreenWithOutput
        {
            var list = screens.ToList();
            var ids = list.Where(u => u.ControllerOutputId != null).Select(u => u.ControllerOutputId!).Distinct().ToList();
            if (ids.Count == 0)
                return;

            var outputs = (await _vwOutputRep.AsQueryable().Where(u => ids.Contains(u.ID)).ToListAsync())
                .ToDictionary(u => u.ID);
            foreach (var s in list)
            {
                if (s.ControllerOutputId == null || !outputs.TryGetValue(s.ControllerOutputId, out var o))
                    continue;
                s.ControllerId = o.ControllerId;
                s.OutputName = o.DisplayName ?? o.Name;
            }
        }
    }
}
