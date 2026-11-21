using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.IncidentControl;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.IncidentControl.Queries
{
    public class IncidentControlQueryHandler
    {
        private BaseRepository<TmsIncidentControl> _repo;
        private BaseCacheService _cache;


        public IncidentControlQueryHandler(BaseRepository<TmsIncidentControl> repo, BaseCacheService cacheService)
        {
            _repo = repo;
            _cache = cacheService;
        }


        /// <summary>
        /// lấy danh sách thiết bị điều khiển theo sự cố
        /// Author: ChuongLP
        /// Created date: 17/07/2026
        /// </summary>
        public async Task<List<TmsIncidentControlOutput>> HandleAsync(TmsIncidentControlInput command)
        {

            var list = _cache.GetOrAdd(CacheConst.Tms.TmsIncidentControl, _ =>
            {
                return _repo.AsQueryable()
                    .Select((u) => new TmsIncidentControlOutput()
                    {
                    }, true)
                    .OrderBy(u => u.CreateTime).ToListAsync().Result;
            });


            list = list
                .WhereIF(!string.IsNullOrWhiteSpace(command.IncidentId), u => u.IncidentId != null && u.IncidentId.Contains(command.IncidentId))
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID != null && u.ID.Contains(command.ID))
                .WhereIF(command.State == "NULL", u => u.State == command.State)
                .ToList();
            return list;
        }

        /// <summary>
        /// lấy danh sách thiết bị điều khiển theo danh sách thiết bị
        /// Author: ChuongLP
        /// Created date: 17/07/2026
        /// </summary>
        public async Task<List<TmsIncidentControl>> HandleAsync(GetIncidentControlByEquipmentInput command)
        {
            if (command.EquipmentIds == null || command.EquipmentIds.Count == 0)
                return new List<TmsIncidentControl>();

            // KienLT - 16/10/2026: bản ghi của sự cố đã XOÁ hoặc không còn tồn tại là rác chắc chắn (không luồng nào còn
            // reset/xoá nó) và trước đây chặn tự động mãi mãi - nay bỏ qua. Sự cố chưa xoá thì VẪN tính là giữ thiết bị,
            // kể cả đã kết thúc: Hoàn thành/Huỷ ghi State trước rồi mới reset tuần tự từng thiết bị (không có trần thời
            // gian) và chỉ xoá quyền giữ khi reset xong, nhả sớm theo State/thời gian sẽ để sự cố khác gửi nội dung rồi bị
            // lệnh reset cũ ghi đè. Bản ghi tồn của sự cố đã kết thúc (reset ném lỗi trước bước xoá) dọn bằng dữ liệu.
            // Dòng không gắn sự cố (IncidentId null) giữ nguyên hành vi cũ ở cả hai nơi gọi.
            var inactiveStates = IncidentControlConst.InactiveIncidentStates;
            return await _repo.AsQueryable()
                .Where(u => command.EquipmentIds.Contains(u.EquipmentId))
                .WhereIF(!string.IsNullOrEmpty(command.ExcludeIncidentId), u => u.IncidentId != command.ExcludeIncidentId)
                .Where(u => u.IncidentId == null || SqlFunc.Subqueryable<TmsIncident>()
                    .Where(i => i.ID == u.IncidentId && i.IsDelete == null)
                    .Any())
                .WhereIF(command.OnlyActiveIncident, u => u.IncidentId != null && SqlFunc.Subqueryable<TmsIncident>()
                    .Where(i => i.ID == u.IncidentId && !inactiveStates.Contains(i.State))
                    .Any())
                .ToListAsync();
        }

        /// <summary>
        /// Lấy thông tin sự cố đang giữ từng thiết bị (chỉ sự cố còn hoạt động) phục vụ quyết định Ghi đè / Nối tiếp / Chặn.
        /// Một truy vấn duy nhất: TmsIncidentControl JOIN TmsIncident (bắt buộc) LEFT JOIN TmsEquipment (loại thiết bị).
        /// Author: ChuongLP
        /// Created date: 19/10/2026
        /// </summary>
        public async Task<List<TmsIncidentControlDataOutput>> HandleAsync(GetIncidentControlOccupancyInput command)
        {
            if (command.EquipmentIds == null || command.EquipmentIds.Count == 0)
                return new List<TmsIncidentControlDataOutput>();

            var inactiveStates = IncidentControlConst.InactiveIncidentStates;
            return await _repo.AsQueryable()
                .InnerJoin<TmsIncident>((c, i) => c.IncidentId == i.ID)
                .LeftJoin<TmsEquipment>((c, i, e) => c.EquipmentId == e.ID)
                .Where((c, i, e) => command.EquipmentIds.Contains(c.EquipmentId)
                    && i.IsDelete == null && !inactiveStates.Contains(i.State))
                .WhereIF(!string.IsNullOrEmpty(command.ExcludeIncidentId), (c, i, e) => c.IncidentId != command.ExcludeIncidentId)
                .WhereIF(!string.IsNullOrEmpty(command.EquipmentTypeId), (c, i, e) => e.EquipmentTypeId == command.EquipmentTypeId)
                .Select((c, i, e) => new TmsIncidentControlDataOutput
                {
                    ID = c.ID,
                    IncidentId = c.IncidentId,
                    EquipmentId = c.EquipmentId,
                    State = c.State,
                    Priority = c.Priority,
                    Url = c.Url,
                    CreateTime = c.CreateTime,
                    UpdateTime = c.UpdateTime,
                    IncidentState = i.State,
                    EventTypeId = i.EventTypeId,
                    EquipmentTypeId = e.EquipmentTypeId
                })
                .ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách thiết bị điều khiển theo sự cố (distinct theo EquipmentId)
        /// Author: ChuongLP
        /// Date: 2026-07-02
        /// </summary>
        public async Task<List<TmsIncidentControl>> HandleAsync(GetIncidentControlInput command)
        {
            var list = await _repo.AsQueryable()
                .Where(u => u.IncidentId == command.IncidentId)
                .ToListAsync();

            return list
                .Where(u => !string.IsNullOrEmpty(u.EquipmentId))
                .GroupBy(u => u.EquipmentId)
                .Select(g => g.First())
                .ToList();
        }
    }
}
