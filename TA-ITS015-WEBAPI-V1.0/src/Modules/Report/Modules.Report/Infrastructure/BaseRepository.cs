using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.Report.Infrastructure
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Repository dùng chung của phân hệ quản lý báo cáo
    /// Created date: 27/07/2026
    /// </summary>
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base() { }
    }
}
