using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.TMS.Controllers.Module.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using System.Diagnostics;

namespace Modules.TMS.Controllers.Module.Queries
{
    public class ModuleQueryHandler
    {
        private BaseRepository<TmsModule> _tmsModuleRep;
        private BaseCacheService _cache;

        public ModuleQueryHandler(BaseRepository<TmsModule> tmsModuleRep, BaseCacheService cache)
        {
            _tmsModuleRep = tmsModuleRep;
            _cache = cache;
        }

        public async Task<SqlSugarPagedList<PageTmsModuleOutput>> HandleAsync(PageTmsModuleInput command)
        {
            return await _tmsModuleRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.PluginId), u => u.PluginId == command.PluginId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.KeyName), u => u.KeyName.Contains(command.KeyName))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select((u) => new PageTmsModuleOutput()
                {
                }, true).OrderBuilder(command, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(command.Page, command.PageSize);
        }
        public async Task<List<ModuleOutput>> HandleAsync(ModuleInput command)
        {

            //var list =  _cache.GetOrAdd($"sysTmsModule:{command.Name}",   _ =>
            //{
            //    return   _tmsModuleRep.AsQueryable()
            //        .Select((u) => new TmsModuleOutput()
            //        {
            //        }, true)
            //        .OrderBy(u => u.CreateTime).ToListAsync().Result;

            //});


            //list = list.WhereIF(!string.IsNullOrWhiteSpace(command.KeyName), u => u.KeyName.Contains(command.Name))
            //           .WhereIF(!string.IsNullOrEmpty(command.Name),u=>u.Name.Contains(command.Name)).ToList();
            //return list;
            var tvrList = await _tmsModuleRep.AsQueryable()
              .WhereIF(!string.IsNullOrWhiteSpace(command.KeyName), u => u.KeyName.Contains(command.KeyName))
              .WhereIF(!string.IsNullOrWhiteSpace(command.PluginId), u => u.PluginId == command.PluginId)
              .WhereIF(!string.IsNullOrEmpty(command.Name), u => u.Name.Contains(command.Name))
              .WhereIF(command.Status != null, u => u.Status == command.Status)
             .Select((st) => new ModuleOutput() { }, true)
             .OrderBy(st => st.CreateTime)
             .ToListAsync();

            var results = ItsDataTransporter.BroadcastDataWithResult(ItsDataTransporterChannel.GetStatus, tvrList);
            Debug.WriteLine("Kết quả broadcast đến các handler:");
            foreach (var kvp in results)
            {
                Debug.WriteLine($"Handler: {kvp.Key}, Thành công: {kvp.Value}");
            }
            return tvrList;
        }
    }
}
