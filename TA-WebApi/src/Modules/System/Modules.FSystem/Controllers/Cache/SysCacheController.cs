using Shared.Infrastructure.Services;

namespace Modules.FSystem.Controllers.Cache
{

    public class SysCacheController:Modules.System.Controllers.Cache.SysCacheController
    {
        public SysCacheController(BaseCacheService sysCacheService) : base(sysCacheService)
        {
        }
    }
}
