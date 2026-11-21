using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.Mail.Infrastructure
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Repository dùng chung của phân hệ Mail
    /// Created date: 14/08/2026
    /// </summary>
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base() { }
    }
}
