using Modules.TMS.Controllers.WorkUnit.Dto;
using Modules.TMS.Core.Entities;

namespace Modules.TMS.Infrastructure.Services.WorkUnit
{
    /// <summary>
    /// Author: ThangNN
    /// Created Date: 19/08/2024
    /// </summary>
    public class WorkUnitService
    {
        private BaseRepository<TmsWorkUnit> _workUnitRepo;
        private BaseRepository<TmsMapDetail> _mapDetailRepo;

        public WorkUnitService(BaseRepository<TmsWorkUnit> workUnitRepo, BaseRepository<TmsMapDetail> mapDetailRepo)
        {
            this._mapDetailRepo = mapDetailRepo;
            this._workUnitRepo = workUnitRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Hàm lấy thông tin đơn vị theo vị trí trên bản đồ
        /// Created date: 19/08/2024
        /// </summary>
        public async Task<List<TmsWorkUnitOutput>> HandleAsync(TmsWorkUnitPositionInput command)
        {
            var tdList = new List<TmsWorkUnitOutput>();
            if (command.WorkUnitId is not null)
            {
                tdList = await this._workUnitRepo.AsQueryable()
                    .Where(wu => wu.ID == command.WorkUnitId)
                    .Select((td) => new TmsWorkUnitOutput() { }, true)
                    .ToListAsync();
            }
            else
            {
                var workUnitIds = await this._mapDetailRepo.AsQueryable()
                    .WhereIF(!string.IsNullOrEmpty(command.MapLocationId), md => md.MapLocationId == command.MapLocationId)
                    .WhereIF(!string.IsNullOrEmpty(command.RefType), md => md.RefType != null && md.RefType.ToString() == command.RefType) // command.RefType phải là Đơn Vị
                    .Select(md => md.RefId)
                    .ToArrayAsync();

                if (workUnitIds.Any()) // => Fix .ToListAsync() throw exception nếu kết quả rỗng
                {
                    tdList = await this._workUnitRepo.AsQueryable()
                        .Where(wu => workUnitIds.Contains(wu.ID))
                        .Select((td) => new TmsWorkUnitOutput() { }, true)
                        .ToListAsync();
                }
            }

            return tdList;
        }
    }
}
