using Modules.FMS.Controllers.EquipmentHistory.Dto;
using Modules.FMS.Core.Entities;
using Modules.FMS.Infrastructure;
using Wolverine;

namespace Modules.FMS.Controllers.EquipmentHistory.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 22/01/2025
    /// </summary>
    public class EquipmentHistoryQueryHandler : IWolverineHandler
    {
        private BaseRepository<FmsEquipmentHistory> _baseRepository;

        public EquipmentHistoryQueryHandler(BaseRepository<FmsEquipmentHistory> baseRepository)
        {
            this._baseRepository = baseRepository;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tham số
        /// Created date: 22/01/2025
        /// </summary>
        public async Task<List<EquipmentHistoryOutput>> HandleAsync(EquipmentHistoryInput command)
        {
            var equipList = await this._baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), equip => equip.EquipmentId == command.EquipmentId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.NewState), equip => equip.NewState == command.NewState)
                .WhereIF(command.FromDate != null, equip => equip.StartTime >= command.FromDate)
                .WhereIF(command.ToDate != null, equip => equip.StartTime <= command.ToDate)
                .Where(equip => equip.IsDelete == null)
                .OrderByDescending(equip => equip.StartTime)
                .Select(equip => new EquipmentHistoryOutput() { }, true)
                .ToListAsync();

            return equipList;
        }
    }
}
