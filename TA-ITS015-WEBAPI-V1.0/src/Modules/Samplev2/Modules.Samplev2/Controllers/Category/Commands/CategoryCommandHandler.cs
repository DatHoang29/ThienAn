using Mapster;
using Modules.Samplev2.Controllers.Category.Dto;
using Modules.Samplev2.Core.Entities;
using Modules.Samplev2.Core.Exceptions;
using Modules.Samplev2.Infrastructure;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.Samplev2.Controllers.Category.Commands
{

    public  class CategoryCommandHandler:IWolverineHandler
    {
        private SampleBaseRepository<SampCategory> _sysPosRep;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public CategoryCommandHandler(SampleBaseRepository<SampCategory> sysPosRep,BaseCacheService cache, UserManager userManager)
        {
            _sysPosRep = sysPosRep;
            _cache = cache;
            _userManager = userManager;
        }

        public async Task HandleAsync(AddCategoryInput command)
        {
            if (!command.Name.Contains("Cat_"))
            {
                throw Oops.Oh(BaseMsg.SampCategory.Validation.MustContainPrefix);
            }

            if (await _sysPosRep.AsQueryable().ClearFilter().AnyAsync(u => u.Name == command.Name && u.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist,BaseMsg.BaseEntity.CodeOrName);
            


            command.CreateUId = null;
             await _sysPosRep.InsertWithDiffLogAsync(command.Adapt<SampCategory>());
        }

        public async Task HandleAsync(UpdateCategoryInput command)
        {
            if (command.Name.Contains("Cat_"))
            {
                throw Oops.Oh(BaseMsg.SampCategory.Message.TestMessage);
            }

            if (await _sysPosRep.IsAnyAsync(u => u.Name == command.Name && u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist,BaseMsg.BaseEntity.CodeOrName);

            var sysPos = await _sysPosRep.GetByIdAsync(command.ID) ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            if (!_userManager.SuperAdmin && sysPos.CreateUId != _userManager.UserId)
                throw Oops.Oh(BaseMsg.BaseException.NoPermission);

            await _sysPosRep.UpdateWithDiffLogAsync(command.Adapt(sysPos));
        }


        public async Task HandleAsync(DeleteCategoryInput command)
        {
            var sysPos = await _sysPosRep.GetFirstAsync(u => u.ID == command.ID) 
                 ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            //if (!_userManager.SuperAdmin && sysPos.CreateUId != _userManager.UserId)
            //    throw Oops.Oh(BaseMsg.BaseException.NoPermission);


            if(sysPos.Status == BaseEnums.StatusEnum.Enable) 
                throw Oops.Oh(BaseMsg.SampCategory.Exception.CannotDeleteHasEnable);

            await _sysPosRep.SoftDeleteAsync<SampCategory>(sysPos);
        }

        public async Task HandleAsync(List<DeleteCategoryInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _sysPosRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count) throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            
            await _sysPosRep.SoftDeleteAsync<SampCategory>(l => ids.Contains(l.ID));

        }
    }
}
