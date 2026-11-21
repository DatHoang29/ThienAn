using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.WP.Report.Infrastructure
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Truy cập dữ liệu Base
    /// Created date: 14/08/2026
    /// </summary>
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base()
        {
        }
    }
}
