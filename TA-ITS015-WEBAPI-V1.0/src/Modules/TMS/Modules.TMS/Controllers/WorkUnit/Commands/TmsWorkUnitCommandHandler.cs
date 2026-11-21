using Modules.TMS.Controllers.WorkContact.Dto;
using Modules.TMS.Controllers.WorkUnit.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.WorkUnit.Commands
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 25/11/2024
    /// </summary>
    public class TmsWorkUnitCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsWorkUnit> _workUnitRepo;
        private BaseCacheService _cache;
        private IMessageBus _messBus;

        public TmsWorkUnitCommandHandler(BaseRepository<TmsWorkUnit> workUnitRepo, BaseCacheService cache, IMessageBus messageBus)
        {
            this._cache = cache;
            this._workUnitRepo = workUnitRepo;
            this._messBus = messageBus;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm mới một record
        /// Created date: 25/11/2024
        /// </summary>
        public async Task HandleAsync(AddTmsWorkUnitInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 
            if (await this._workUnitRepo.IsAnyAsync(wu => wu.IsDelete == null && wu.Type == command.Type && wu.Name == command.Name))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Name);

            if (await this._workUnitRepo.IsAnyAsync(wu => wu.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);

            TmsWorkUnit workUnit = command.Adapt<TmsWorkUnit>();
            if (await this._workUnitRepo.InsertWithDiffLogAsync(workUnit) > 0)
            {
                // Insert the same data into TmsWorkContact
                await this._messBus.InvokeAsync(new AddTmsWorkContactInput()
                {
                    Code = command.Code,
                    Name = command.Name,
                    Phone = command.Phone,
                    Address = command.Address,
                    Info = command.Info,
                    Type = "2", // sysConfig => work_contact_type => 2 = Đơn vị
                    RefId = workUnit.ID,
                    Status = command.Status,
                    Remark = command.Remark
                });
            }

            this._cache.RemoveByPrefixKey(CacheConst.Tms.TmsWorkUnit);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 25/11/2024
        /// </summary>
        public async Task HandleAsync(UpdateTmsWorkUnitInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 
            if (await this._workUnitRepo.IsAnyAsync(wu => wu.IsDelete == null && wu.Type == command.Type && wu.Name == command.Name && wu.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Name);


            var wu = await this._workUnitRepo.GetByIdAsync(command.ID);
            if (wu == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (await this._workUnitRepo.UpdateWithDiffLogAsync(command.Adapt(wu)) > 0)
            {
                var workContact = await this._messBus.InvokeAsync<TmsWorkContact>(new TmsWorkContactPara() { RefId = command.ID });
                if (workContact != null)
                {
                    // Update the same data into TmsWorkContact
                    await this._messBus.InvokeAsync(new UpdateTmsWorkContactInput()
                    {
                        ID = workContact.ID,
                        Code = command.Code,
                        Name = command.Name,
                        Phone = command.Phone,
                        Address = command.Address,
                        Info = command.Info,
                        Type = "2", // sysConfig => work_contact_type => 2 = Đơn vị
                        //RefId = command.ID,
                        Status = command.Status,
                        Remark = command.Remark
                    });
                }
            }

            this._cache.RemoveByPrefixKey(CacheConst.Tms.TmsWorkUnit);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 25/11/2024
        /// </summary>
        public async Task HandleAsync(DeleteTmsWorkUnitInput command)
        {
            var wu = await this._workUnitRepo.GetByIdAsync(command.ID);
            if (wu == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (await this._workUnitRepo.SoftDeleteAsync<TmsWorkUnit>(wu) > 0)
            {
                var workContact = await this._messBus.InvokeAsync<TmsWorkContact>(new TmsWorkContactPara() { RefId = wu.ID });
                if (workContact != null)
                {
                    // Delete the same data from TmsWorkContact
                    await this._messBus.InvokeAsync(new DeleteTmsWorkContactInput() { ID = workContact.ID });
                }
            }

            this._cache.RemoveByPrefixKey(CacheConst.Tms.TmsWorkUnit);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 25/11/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteTmsWorkUnitInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();
            var wuList = await this._workUnitRepo.GetListAsync(wu => ids.Contains(wu.ID));

            if (wuList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await this._workUnitRepo.SoftDeleteAsync<TmsWorkUnit>(wu => ids.Contains(wu.ID));

            this._cache.RemoveByPrefixKey(CacheConst.Tms.TmsWorkUnit);
        }
    }
}
