using Furion.DatabaseAccessor;
using Modules.TMS.Controllers.Module.Dto;
using Modules.TMS.Controllers.SignalLogHistory.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.SignalLogHistory.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 01/02/2025
    /// </summary>
    public class SignalLogHistoryQueryHandler : IWolverineHandler
    {
        private IMessageBus _messageBus;
        private BaseRepository<TmsSignalLog> _baseRepository;

        public SignalLogHistoryQueryHandler(BaseRepository<TmsSignalLog> baseRepository, IMessageBus messageBus)
        {
            this._baseRepository = baseRepository;
            this._messageBus = messageBus;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 01/02/2025
        /// </summary>
        public async Task<SqlSugarPagedList<PageSignalLogHistoryOutput>> HandleAsync(PageSignalLogHistoryInput command)
        {
            var moduleList = !string.IsNullOrWhiteSpace(command.ModuleKey)
                    ? await this._messageBus.InvokeAsync<List<ModuleOutput>>(new ModuleInput() { })
                    : null;

            var moduleIdList = moduleList?.Count() > 0
                ? moduleList
                    .Where(item => command.ModuleKey == item.KeyName)
                    .Select(item => item.ID)
                    .ToList()
                : new List<string>();

            var orderByClause = this.BuildOrderByClause(command.Field, command.Order);

            var signalLogList = await this._baseRepository.AsQueryable()
                .LeftJoin<TmsEquipment>((sl, eq) => sl.EquipmentId == eq.ID)
                .LeftJoin<TmsEquipmentType>((sl, eq, eqt) => eq.EquipmentTypeId == eqt.ID)
                .WhereIF(moduleIdList?.Count() > 0, (sl, eq, eqt) => moduleIdList.Contains(eqt.ModuleId))
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), sl => sl.EquipmentId == command.EquipmentId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentTypeId), (sl, eq) => eq.EquipmentTypeId == command.EquipmentTypeId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.State), sl => sl.State == command.State)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ExecuteBy), sl => sl.ExecuteBy != null && sl.ExecuteBy.Contains(command.ExecuteBy, StringComparison.OrdinalIgnoreCase))
                .WhereIF(command.FromDate != null, sl => sl.ExecuteTime >= command.FromDate)
                .WhereIF(command.ToDate != null, sl => sl.ExecuteTime <= command.ToDate)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select((sl, eq, eqt) => new PageSignalLogHistoryOutput()
                {
                    ID = sl.ID,
                    EquipmentName = eq.Name,
                    EquipmentTypeId = eqt.ID,
                    EquipmentTypeCode = eqt.Code,
                    EquipmentTypeName = eqt.Name,
                }, true)
                .OrderByIF(!string.IsNullOrWhiteSpace(orderByClause), orderByClause)
                .ToPagedListAsync(command.Page, command.PageSize);

            return signalLogList;
        }

        private string BuildOrderByClause(string? field, string? order)
        {
            if (field == null || order == null)
                return "ExecuteTime Desc";

            if (field.Equals("EquipmentTypeName", StringComparison.OrdinalIgnoreCase))
                return $"eqt.Name {order}";

            if (field.Equals("EquipmentName", StringComparison.OrdinalIgnoreCase))
                return $"eq.Name {order}";

            if (field.Equals("State", StringComparison.OrdinalIgnoreCase))
                return $"sl.State {order}";

            return $"{field} {order}";
        }
    }
}
