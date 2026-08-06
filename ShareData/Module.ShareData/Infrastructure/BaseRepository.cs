using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.ShareData.Infrastructure
{
    /// <summary>
    /// Description: Repository dùng chung của phân hệ Chia sẻ dữ liệu (ShareData)
    /// Created date: 2026-08-04
    /// </summary>
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base() { }
    }
}
