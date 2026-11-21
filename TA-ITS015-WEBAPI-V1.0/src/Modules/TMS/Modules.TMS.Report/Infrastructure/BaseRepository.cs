using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Report.Infrastructure
{
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base()
        {
        }
    }
}
