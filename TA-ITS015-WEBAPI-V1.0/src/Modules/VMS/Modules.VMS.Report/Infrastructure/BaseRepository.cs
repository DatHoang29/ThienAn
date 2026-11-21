using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.VMS.Report.Infrastructure
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Base repository cho phân hệ báo cáo VMS
    /// Created date: 07/10/2026
    /// </summary>
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base()
        {
        }
    }
}
