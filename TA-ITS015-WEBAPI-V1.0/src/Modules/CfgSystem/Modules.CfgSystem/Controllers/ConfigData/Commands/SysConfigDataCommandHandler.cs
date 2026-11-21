using Modules.CfgSystem.Infrastructure.Services.Refresh;
using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Core.Exceptions;
using Modules.CfgSystem.Infrastructure;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.CfgSystem.Core.Refresh;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.CfgSystem.Controllers.ConfigData.Commands
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 09/08/2024
    /// </summary>
    public class SysConfigDataCommandHandler : IWolverineHandler
    {
        private BaseRepository<SysConfigData> _configDataRepo;
        private BaseCacheService _cache;
        private BaseRepository<SysConfigType> _configTypeRepo;
        private RefreshModifyService _refreshModify;

        // Key cache dùng chung với SysConfigDataQueryHandler (danh sách config data).
        // Mọi thao tác thêm/sửa/xoá phải xoá cache này, nếu không API DataList sẽ trả dữ liệu cũ
        // (vd sửa styleSetting/value của config nhưng client vẫn nhận giá trị cũ).
        private const string _cacheName = "sysConfigData";

        public SysConfigDataCommandHandler(
            BaseRepository<SysConfigData> baseRepository,
            BaseRepository<SysConfigType> configTypeRepo,
            BaseCacheService cache,
            RefreshModifyService refreshModify)
        {
            this._configDataRepo = baseRepository;
            this._configTypeRepo = configTypeRepo;
            this._cache = cache;
            this._refreshModify = refreshModify;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm mới 1 record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(AddSysConfigDataInput command)
        {
            if (await this._configDataRepo.IsAnyAsync(scd => scd.IsDelete == null && scd.Code == command.Code && scd.ConfigTypeId == command.ConfigTypeId))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            await this._configDataRepo.InsertWithDiffLogAsync(command.Adapt<SysConfigData>());
            this._cache.RemoveByPrefixKey(_cacheName);
            await NotifyAsync(command.ConfigTypeId, command.Code, "add");
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật 1 record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateSysConfigDataInput command)
        {
            if (await this._configDataRepo.IsAnyAsync(scd => scd.IsDelete == null && scd.Code == command.Code && scd.ConfigTypeId == command.ConfigTypeId && scd.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            var scd = await this._configDataRepo.GetByIdAsync(command.ID);
            if (scd == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._configDataRepo.UpdateWithDiffLogAsync(command.Adapt(scd));
            this._cache.RemoveByPrefixKey(_cacheName);
            await NotifyAsync(scd.ConfigTypeId, scd.Code, "update");
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa 1 record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(DeleteSysConfigDataInput command)
        {
            var scd = await this._configDataRepo.GetFirstAsync(scd => scd.ID == command.ID);
            if (scd == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._configDataRepo.SoftDeleteAsync<SysConfigData>(scd);
            this._cache.RemoveByPrefixKey(_cacheName);
            await NotifyAsync(scd.ConfigTypeId, scd.Code, "delete");
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteSysConfigDataInput> command)
        {
            List<string> ids = command.Select(item => item.ID).ToList();
            List<SysConfigData> scdList = await this._configDataRepo.AsQueryable().Where(scd => ids.Contains(scd.ID)).ToListAsync();

            if (scdList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await this._configDataRepo.SoftDeleteAsync<SysConfigData>(scd => ids.Contains(scd.ID));
            this._cache.RemoveByPrefixKey(_cacheName);
            foreach (var group in scdList.GroupBy(x => x.ConfigTypeId))
                await NotifyAsync(group.Key, null, "batch-delete");
        }

        private async Task NotifyAsync(string? configTypeId, string? key, string action)
        {
            if (string.IsNullOrWhiteSpace(configTypeId)) return;

            var configType = await this._configTypeRepo.GetFirstAsync(x => x.IsDelete == null && x.ID == configTypeId);
            if (string.IsNullOrWhiteSpace(configType?.Code)) return;

            await this._refreshModify.NotifyAsync(new RefreshModifyRequest("sysconfig", configType.Code, key, action));
        }
    }
}
