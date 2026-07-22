using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.Report.Demo.Infrastructure
{
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base()
        {
        }

    }

}
