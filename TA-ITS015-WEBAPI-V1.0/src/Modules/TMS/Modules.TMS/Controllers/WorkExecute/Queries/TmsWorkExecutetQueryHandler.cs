using Modules.TMS.Controllers.WorkContact.Dto;
using Modules.TMS.Controllers.WorkDetailContact.Dto;
using Modules.TMS.Core.Dto.WorkExecute;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Enums;

namespace Modules.TMS.Controllers.WorkExecute.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 20/11/2024
    /// </summary>
    public class TmsWorkExecuteQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsWorkExecute> _workExecuteRepo;
        private IMessageBus _messageBus;

        public TmsWorkExecuteQueryHandler(BaseRepository<TmsWorkExecute> TmsWorkExecute, IMessageBus messageBus)
        {
            this._workExecuteRepo = TmsWorkExecute;
            this._messageBus = messageBus;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tham số
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<List<TmsWorkExecuteOutput>> HandleAsync(TmsWorkExecuteParaInput command)
        {
            if (string.IsNullOrEmpty(command.IncidentId))
                return null;

            var workDetailContactList = await this._messageBus.InvokeAsync<List<TmsWorkDetailContactOutput>>(new TmsWorkDetailContactInput() { }) ?? new List<TmsWorkDetailContactOutput>();

            var workContactList = await this._messageBus.InvokeAsync<List<TmsWorkContactOutput>>(new TmsWorkContactInput() { }) ?? new List<TmsWorkContactOutput>();

            var detailContactList = from wdc in workDetailContactList
                                    join wc in workContactList on wdc.WorkContactId equals wc.ID
                                    where wdc.IsDelete == null && wc.IsDelete == null && wc.Status == (BaseEnums.StatusEnum)BaseEnums.StatusEnum.Enable
                                    orderby wdc.OrderNo
                                    select new TmsWorkDetailContactOutput()
                                    {
                                        ContactName = wc.Name,
                                        ContactPhone = wc.Phone,
                                        ContactType = wc.Type,
                                        ContactRefId = wc.RefId
                                    };

            var weList = await this._workExecuteRepo.AsQueryable()
                .Where(we => we.IncidentId == command.IncidentId)
                .OrderByIF(command.UseForTimeline == true, we => we.ExecuteDate, OrderByType.Desc)
                .OrderBy(we => we.Step, OrderByType.Asc)
                .Select(we => new TmsWorkExecuteOutput() { }, true)
                .ToListAsync();

            weList?.ForEach(we =>
            {
                we.WorkContactList = detailContactList?.Where(wdc => wdc.WorkDetailId == we.WorkDetailId).ToList<object>();
            });

            return weList;
        }
    }
}
