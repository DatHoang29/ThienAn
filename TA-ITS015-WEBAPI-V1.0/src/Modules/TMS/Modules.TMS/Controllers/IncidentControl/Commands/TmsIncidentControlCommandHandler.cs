using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.IncidentControl;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.Core.Extensions;
using Shared.Core.Utilities.Constants;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.IncidentControl.Commands
{
    public class TmsIncidentControlCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsIncidentControl> _repo;
        private BaseCacheService _cacheService;

        public TmsIncidentControlCommandHandler(BaseRepository<TmsIncidentControl> repo, BaseCacheService cacheService)
        {
            _repo = repo;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Ghi nhận sự kiện thiết bị được điều khiển theo sự cố
        /// Author: ChuongLP
        /// Date: 2026-07-02
        /// </summary>
        public async Task HandleAsync(AddTmsIncidentControlInput command)
        {
            command.State = null;
            command.CreateTime = DateTime.Now;
            command.CreateUId = App.User.FindFirst(ClaimConst.Account)?.Value;
            await _repo.InsertAsync(command.Adapt<TmsIncidentControl>());
            _cacheService.RemoveByPrefixKey(CacheConst.Tms.TmsIncidentControl);
        }

        /// <summary>
        /// Update: nếu thiết bị đã có bản ghi -> set tất cả State = override.
        /// Nếu chưa có -> insert mới với State = null.
        /// Author: ChuongLP
        /// Date: 2026-07-02
        /// </summary>
        public async Task HandleAsync(UpdateTmsIncidentControlInput command)
        {
            var isExist = await _repo.IsAnyAsync(u => u.EquipmentId == command.EquipmentId && u.IncidentId == command.IncidentId);

            if (isExist)
            {
                // Tính sẵn giá trị ngoài lambda: SqlSugar không parse được method C# (GetUsername) trong SetColumns.
                // Luồng timer chạy nền qua Hangfire không có HTTP context nên App.User có thể null -> trả "system".
                var updateUId = App.User == null ? "system" : App.User.GetUsername();
                var updateTime = DateTime.Now;
                // Nối tiếp truyền State = append; lệnh gửi thẳng không truyền State -> override như cũ.
                var newState = string.IsNullOrEmpty(command.State) ? IncidentControlConst.State.Override : command.State;

                await _repo.AsUpdateable()
                    .SetColumns(u => u.State == newState)
                    .SetColumns(u => u.Priority == command.Priority)   // THÊM
                    .SetColumns(u => u.UpdateTime == updateTime)
                    .SetColumns(u => u.UpdateUId == updateUId)
                    // Chỉ ghi Url khi có (lệnh không gửi nội dung - vd đổi State - không được xoá ảnh đã lưu)
                    .SetColumnsIF(!string.IsNullOrEmpty(command.Url), u => u.Url == command.Url)
                    .Where(u => u.EquipmentId == command.EquipmentId && u.IncidentId == command.IncidentId)
                    .ExecuteCommandAsync();
            }
            else
            {
                // Chỉ giữ State khi là append (playlist); các trường hợp khác giữ hành vi cũ: insert lần đầu State = null.
                if (command.State != IncidentControlConst.State.Appeend) command.State = null;
                // CreateTime dùng để chọn sự cố mới nhất khi cùng ưu tiên (IncidentControlOccupancyHelper.PickDisplayOwner)
                command.CreateTime = DateTime.Now;
                await _repo.InsertAsync(command.Adapt<TmsIncidentControl>());
            }

            _cacheService.RemoveByPrefixKey(CacheConst.Tms.TmsIncidentControl);
        }


        /// <summary>
        /// Xóa bản ghi điều khiển sự cố của thiết bị
        /// Author: ChuongLP
        /// Date: 2026-07-02
        /// </summary>
        public async Task HandleAsync(DeleteTmsIncidentControlInput command)
        {
            var entity = await _repo.GetFirstAsync(u => u.IncidentId == command.IncidentId && u.EquipmentId == command.EquipmentId);
            if (entity == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await _repo.DeleteAsync(entity);
            _cacheService.RemoveByPrefixKey(CacheConst.Tms.TmsIncidentControl);
        }


        /// <summary>
        /// Xóa tất cả bản ghi điều khiển sự cố theo IncidentId (idempotent - không có dòng nào cũng không lỗi).
        /// Author: ChuongLP
        /// Date: 2026-09-04
        /// </summary>
        public async Task HandleAsync(DeleteIncidentControlByIncidentInput command)
        {
            if (string.IsNullOrWhiteSpace(command.IncidentId)) return;

            await _repo.DeleteAsync(u => u.IncidentId == command.IncidentId);
            _cacheService.RemoveByPrefixKey(CacheConst.Tms.TmsIncidentControl);
        }

    }
}
