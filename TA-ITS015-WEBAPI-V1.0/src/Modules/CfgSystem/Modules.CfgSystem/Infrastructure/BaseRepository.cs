using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.CfgSystem.Infrastructure
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 09/08/2024
    /// </summary>
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base() { }
    }
}
