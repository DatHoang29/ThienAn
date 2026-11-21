using Furion.DatabaseAccessor;
using Modules.TMS.Controllers.Zone.Dto;
using Modules.TMS.Controllers.ZoneStatus.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Controllers.Zone.Queries
{
    public class ZoneQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsZone> _zoneRep;
        private BaseCacheService _cache;
        private IMessageBus _bus;
        public ZoneQueryHandler(BaseRepository<TmsZone> zoneRep, BaseCacheService cache, IMessageBus bus)
        {
            _zoneRep = zoneRep;
            _cache = cache;
            _bus = bus;
        }

        /// <summary>
        /// Description: Lấy danh sách tree zone. Nếu có tìm kiếm, trả về list
        /// Author: SonTH
        /// Created date: 03/2025
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>

        public async Task<List<ZoneOutput>> HandleAsync(TreeZoneInput command)
        {
            // Lấy dữ liệu dạng cây với điều kiện lọc
            var filteredList = _zoneRep.AsQueryable()
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name))
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code != null && u.Code.Contains(command.Code))
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Type), u => u.Type != null && u.Type == command.Type)
                    .WhereIF(command.Status != null, u => u.Status == command.Status)

                    .WhereIF(command.Status != null, u => u.Status == command.Status)
                    .OrderBy(u => u.Code)
                ;

            // Neu khong loc theo thoi gian >> return
            if (string.IsNullOrEmpty(command.FromKmNumber)
                && string.IsNullOrEmpty(command.FromMetNumber)
                && string.IsNullOrEmpty(command.ToKmNumber)
                && string.IsNullOrEmpty(command.ToMetNumber))
            {
                if (!await filteredList.AnyAsync(l => l.Pid == "0"))
                    return await filteredList.Select(l=> new ZoneOutput(){},true).ToListAsync();
                return  (await filteredList.ToTreeAsync(u => u.Children, u => u.Pid, "0")).Adapt<List<ZoneOutput>>();
            }

            //TODO: Bug issue FE trả về chữ null

            //Lọc theo t.gian
            // Chuyển đổi vị trí từ đơn vị km sang đơn vị met
            var fromValue = int.Parse(!string.IsNullOrEmpty(command.FromKmNumber) && command.FromKmNumber != "null" ? command.FromKmNumber : "0") * 1000
                            + int.Parse(!string.IsNullOrEmpty(command.FromMetNumber) && command.FromMetNumber != "null" ? command.FromMetNumber : "0");

            var toValue = int.Parse(!string.IsNullOrEmpty(command.ToKmNumber) && command.ToKmNumber != "null" ? command.ToKmNumber : "0") * 1000
                          + int.Parse(!string.IsNullOrEmpty(command.ToMetNumber) && command.ToMetNumber != "null" ? command.ToMetNumber : "0");

            //Lấy tăng dần
            bool isGreater = !(toValue > 0 && toValue < fromValue);

            //Filter theo tăng dần
            filteredList.WhereIF(fromValue > 0 && isGreater,
                z => fromValue <= SqlFunc.IsNull(z.FromKmNumber, 0) * 1000 + SqlFunc.IsNull(z.FromMetNumber, 0)
            ).WhereIF(toValue > 0 && isGreater,
                z => toValue >= SqlFunc.IsNull(z.ToKmNumber, 0) * 1000 + SqlFunc.IsNull(z.ToMetNumber, 0)
            ).WhereIF(isGreater && toValue > 0, z => (SqlFunc.IsNull(z.FromKmNumber, 0) * 1000 + SqlFunc.IsNull(z.FromMetNumber, 0) <= (SqlFunc.IsNull(z.ToKmNumber, 0) * 1000 + SqlFunc.IsNull(z.ToMetNumber, 0))));

            //var a = await filteredList.ToListAsync();

            //Filter theo giảm dần
            filteredList.WhereIF(toValue > 0 && !isGreater,
                z => toValue <= SqlFunc.IsNull(z.FromKmNumber, 0) * 1000 + SqlFunc.IsNull(z.FromMetNumber, 0)
            ).WhereIF(fromValue > 0 && !isGreater,
                z => fromValue >= SqlFunc.IsNull(z.ToKmNumber, 0) * 1000 + SqlFunc.IsNull(z.ToMetNumber, 0)
            ).WhereIF(!isGreater, z => (SqlFunc.IsNull(z.FromKmNumber, 0) * 1000 + SqlFunc.IsNull(z.FromMetNumber, 0) >= (SqlFunc.IsNull(z.ToKmNumber, 0) * 1000 + SqlFunc.IsNull(z.ToMetNumber, 0))));


            return (await filteredList.ToListAsync()).Adapt<List<ZoneOutput>>();



        }


        /// <summary>
        /// Lấy danh sách khu vực
        /// Author: SonTH
        /// Created date: 03/2025
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<List<ZoneOutput>> HandleAsync(ZoneInput command)
        {

            var list = _cache.GetOrAdd($"{CacheConst.Tms.TmsZone}:''", _ =>
            {
                return _zoneRep.AsQueryable()
                    .Select((u) => new ZoneOutput()
                    {
                    }, true)
                    .OrderBy(u => u.CreateTime).ToListAsync().Result;

            });


            list = list
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code != null && u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID != null && u.ID.Contains(command.ID))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Type), u => u.Type != null && u.Type == command.Type)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .ToList();
            return list;


        }

        /// <summary>
        /// Lấy danh sách khu vực và trạng thái khu vực
        /// Author: SonTH
        /// Created date: 03/2025
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<List<ZoneOutput>> HandleAsync(ZoneStatusInfoInput command)
        {

            var zones = await HandleAsync(command as ZoneInput);
            var zoneStatusList = await _bus.InvokeAsync<List<ZoneStatusOutput>>(new ZoneStatusInput());
            zones = zones.Join(zoneStatusList, z => z.ID, s => s.ZoneId, (z, s) => { z.ZoneStatus = s; return z; }).ToList();
            //zones.ForEach(l => l.ZoneStatus = zoneStatusList?.FirstOrDefault(u => l.ID == u.ZoneId) ?? new ZoneStatusOutput());

            return zones;

        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List zone theo vị trí sự cố (bao gồm cha + con)
        /// Note: Một vị trí có thể thuộc về nhiều zone khác nhau
        /// Created date: 19/08/2024
        /// </summary>
        public async Task<List<ZoneOutput>> HandleAsync(TmsZonePositionInput command)
        {
            //TODO: Cache

            // Nếu tham số truyền vào có zoneId
            if (!string.IsNullOrWhiteSpace(command.ZoneId))
            {
                return await this._zoneRep.AsQueryable()
                    .WhereIF(!string.IsNullOrEmpty(command.ZoneId), z => z.ID == command.ZoneId)
                    .WhereIF(!string.IsNullOrEmpty(command.Location), z => z.Location == command.Location)

                    .Select((z) => new ZoneOutput() { }, true)
                    .ToListAsync();
            }

            var zList = await this._zoneRep.AsQueryable()
                .Where(Modules.TMS.Core.Helper.IncidentPositionHelper.Zones(
                    command.KmNumber, command.MetNumber, command.Location, command.DirectionId))
                .OrderBy(z => z.FromKmNumber)
                .OrderBy(z => z.FromMetNumber)
                .Select((z) => new ZoneOutput() { }, true)
                .ToListAsync();

            // Đánh dấu zone có zone con hay không
            zList?.ForEach(z =>
            {
                z.HasChildInList = zList.Any(zone => zone.Pid == z.ID);
            });

            // Lọc chỉ lấy danh sách zone cấp bé nhất
            if (command.IsExcludeParent)
                zList = zList.Where(z => z.HasChildInList == false).ToList();

            return zList;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List zone theo danh sách zone-id
        /// Created date: 19/08/2024
        /// </summary>
        public async Task<List<ZoneOutput>> HandleAsync(TmsZoneIdListInput command)
        {
            //TODO: Cache

            var zList = new List<ZoneOutput>();

            var zoneIdList = command.ZoneIdList?.Trim().Split(",").Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList() ?? null;

            if (zoneIdList == null || zoneIdList.Count == 0)
                return null;

            zList = await this._zoneRep.AsQueryable()
                .WhereIF(zoneIdList != null, z => zoneIdList.Contains(z.ID))
                .Where(z => z.Status == BaseEnums.StatusEnum.Enable && z.IsDelete == null)
                .OrderBy(z => z.FromKmNumber)
                .OrderBy(z => z.FromMetNumber)
                .Select(z => new ZoneOutput() { }, true)
                .ToListAsync();

            return zList;
        }



        /// <summary>
        /// Lấy danh sách khu vực cha và các khu vực con tương ứng
        /// Author: SonTH
        /// Created date: 12/2025
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<List<ZoneOutput>> HandleAsync(ZoneWithChildInput command)
        {
            //var list = _zoneRep.Context.Queryable<TmsZone>()
            //    .ToChildList(it => it.ID,command.ID
            //    )
            //    .ToList();
            //string sqlQuery = @$"
            //        WITH cte AS (
            //            SELECT * FROM TmsZone WHERE Id = '{command.ID}'
            //            UNION ALL
            //            SELECT n.* 
            //            FROM TmsZone n
            //            INNER JOIN cte ON n.Pid = cte.Id
            //        )
            //        SELECT * FROM cte
            //        ";

            var filteredList = _zoneRep.Context.Ado.UseStoredProcedure().SqlQuery<TmsZone>("Proc_TmsZone_GetAllChildren", new { ID = command.ID }).ToList();
            // Lấy dữ liệu dạng cây với điều kiện lọc
            //var filteredList = _zoneRep.AsQueryable()

            //        .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => (u.Pid != null && u.Pid.Contains(command.ID) || (u.ID != null && u.ID.Contains(command.ID))))
            //        //.WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code != null && u.Code.Contains(command.Code))
            //        //.WhereIF(!string.IsNullOrWhiteSpace(command.Type), u =>  u.Type != null && u.Type == command.Type)
            //        .OrderBy(u => u.Code)
            //    ;

            //if (!await filteredList.AnyAsync(l => l.Pid == "0"))
            //    return await filteredList.Select(l=> new ZoneOutput(){},true).ToListAsync();
            //return (await filteredList.ToTreeAsync(u => u.Children, u => u.Pid, command.ID)).Adapt<List<ZoneOutput>>();

            //return list.WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID != null && u.ID.Contains(command.ID) || u.Children.)
            //    .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code != null && u.Code.Contains(command.Code))
            //    .ToList();
            var zList = filteredList.Adapt<List<ZoneOutput>>();
            // Đánh dấu zone có zone con hay không

            zList?.ForEach(z =>
            {
                z.HasChildInList = zList.Any(zone => zone.Pid == z.ID);
            });
            // Lọc chỉ lấy danh sách zone cấp bé nhất
            if (command.IsExcludeParent)
                zList = zList?.Where(z => z.HasChildInList == false).ToList();

            return zList;
        }
    }
}
