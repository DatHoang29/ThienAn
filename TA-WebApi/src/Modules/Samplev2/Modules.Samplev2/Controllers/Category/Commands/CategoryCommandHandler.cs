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

    public class CategoryCommandHandler : IWolverineHandler
    {
        private BaseRepository<SampCategory> _sampCatRep;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public CategoryCommandHandler(BaseRepository<SampCategory> sysPosRep, BaseCacheService cache, UserManager userManager)
        {
            _sampCatRep = sysPosRep;
            _cache = cache;
            _userManager = userManager;
        }

        public async Task HandleAsync(AddCategoryInput command)
        {
            if (command.Name.Contains("Cat_"))
            {
                throw Oops.Oh(BaseMsg.SampCategory.Validation.MustContainPrefix);
            }

            if (await _sampCatRep.IsAnyAsync(u => u.Name == command.Name && u.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            command.CreateUId = null;
            await _sampCatRep.InsertWithDiffLogAsync(command.Adapt<SampCategory>());
            _cache.Remove("sampCategory");
        }

        public async Task HandleAsync(UpdateCategoryInput command)
        {

            if (await _sampCatRep.IsAnyAsync(u => (u.Name == command.Name || u.Code == command.Code) && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            var sampCat = await _sampCatRep.GetByIdAsync(command.ID)
                          ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await _sampCatRep.UpdateWithDiffLogAsync(command.Adapt(sampCat));
            _cache.Remove("sampCategory");

        }


        public async Task HandleAsync(DeleteCategoryInput command)
        {
            var sampCat = await _sampCatRep.GetFirstAsync(u => u.ID == command.ID) ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);


            if (sampCat.Status == BaseEnums.StatusEnum.Enable)
                throw Oops.Oh(BaseMsg.SampCategory.Exception.CannotDeleteHasEnable);

            await _sampCatRep.SoftDeleteAsync<SampCategory>(sampCat);
            _cache.Remove("sampCategory");

        }

        public async Task HandleAsync(List<DeleteCategoryInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _sampCatRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);


            await _sampCatRep.SoftDeleteAsync<SampCategory>(l => ids.Contains(l.ID));
            _cache.Remove("sampCategory");


        }
    }
}
