using Modules.TMS.Controllers.WorkContact.Dto;
using Modules.TMS.Controllers.WorkDetailContact.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.WorkDetailContact.Queries
{
    public class WorkDetailContactQueryHandler
    {
        private BaseRepository<TmsWorkDetailContacts> _TmsWorkDetailContactRep;
        private BaseCacheService _cache;

        public WorkDetailContactQueryHandler(BaseRepository<TmsWorkDetailContacts> TmsWorkDetailContactRep, BaseCacheService cache)
        {
            _TmsWorkDetailContactRep = TmsWorkDetailContactRep;
            _cache = cache;

        }

        public async Task<SqlSugarPagedList<PageTmsWorkDetailContactOutput>> HandleAsync(PageTmsWorkDetailContactInput command)

        {
            //return await _TmsWorkDetailContactRep.AsQueryable()
            //    .WhereIF(!string.IsNullOrWhiteSpace(command.WorkDetailId), u => u.WorkDetailId.Contains(command.WorkDetailId))
            //    .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
            //    .WhereIF(!string.IsNullOrWhiteSpace(command.WorkContactId), u => u.WorkContactId.Contains(command.WorkContactId))
            //    .OrderBy(u => u.OrderNo)  // Adding OrderBy clause based on OrderNo
            //    .Select((u) => new PageTmsWorkDetailContactOutput()
            //    {
            //    }, true).OrderBuilder(command, "")
            //    .ToPagedListAsync(command.Page, command.PageSize);
            var wuList = await this._TmsWorkDetailContactRep.AsQueryable()
                  .WhereIF(!string.IsNullOrWhiteSpace(command.WorkDetailId), u => u.WorkDetailId.Contains(command.WorkDetailId))
                  .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
                  .WhereIF(!string.IsNullOrWhiteSpace(command.WorkContactId), u => u.WorkContactId.Contains(command.WorkContactId))
                  .OrderBy(u => u.OrderNo)  // Adding OrderBy clause based on OrderNo
               .Select((st) => new PageTmsWorkDetailContactOutput() { }, true)
               .OrderBuilder(command, "")
               .ToPagedListAsync(command.Page, command.PageSize);

            return wuList;
        }
        public async Task<List<TmsWorkDetailContactOutput>> HandleAsync(TmsWorkDetailContactInput command)
        {

            //var list =  _cache.GetOrAdd($"sysTmsWorkDetailContact:{command.WorkDetailId}",   _ =>
            //{
            //    return   _TmsWorkDetailContactRep.AsQueryable()
            //        .Select((u) => new TmsWorkDetailContactOutput()
            //        {
            //        }, true)
            //        .OrderBy(u => u.OrderNo).ToListAsync().Result;

            //});


            //list = list.WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
            //           .WhereIF(!string.IsNullOrWhiteSpace(command.WorkDetailId), u => u.WorkDetailId.Contains(command.WorkDetailId))
            //           .WhereIF(!string.IsNullOrEmpty(command.WorkContactId),u=>u.WorkContactId.Contains(command.WorkContactId)).ToList();
            //return list;
            var wuList = await this._TmsWorkDetailContactRep.AsQueryable()
               .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code != null && u.Code.Contains(command.Code))
               .WhereIF(!string.IsNullOrWhiteSpace(command.WorkDetailId), u => u.WorkDetailId != null && u.WorkDetailId.Contains(command.WorkDetailId))
               .WhereIF(!string.IsNullOrEmpty(command.WorkContactId), u => u.WorkContactId != null && u.WorkContactId.Contains(command.WorkContactId))
               .Select((st) => new TmsWorkDetailContactOutput() { }, true)
               .OrderBy(st => st.OrderNo)
               .ToListAsync();

            return wuList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created Date: 20/11/2024
        /// Description: Lấy danh sách thông tin liên hệ bởi danh sách workdetail-id
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<List<TmsWorkContactOutput>> HandleAsync(TmsWorkDetailIdsInput command)
        {
            if ((command.WorkDetailIdList?.Count() ?? 0) == 0)
                return new List<TmsWorkContactOutput>();

            var workDetailIds = command.WorkDetailIdList.Split(",");

            var wcList = await this._TmsWorkDetailContactRep.AsQueryable()
                .LeftJoin<TmsWorkContact>((tblWDC, tblWC) => tblWDC.WorkContactId == tblWC.ID)
                .Where((tblWDC, tblWC) => workDetailIds.Contains(tblWDC.WorkDetailId))
                .Select((tblWDC, tblWC) => new TmsWorkContactOutput() { ID = tblWDC.WorkContactId }, true)
                .ToListAsync();

            return wcList;
        }
    }
}
