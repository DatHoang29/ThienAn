using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.VMS.Infrastructure
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/12/2024
    /// </summary>
    public class BaseRepository<T>: SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository(): base() { }
    }
}
