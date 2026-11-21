using Shared.Infrastructure.Persistence.SqlSugar;

namespace Module.VideoWall.Infrastructure
{
    /// <summary>
    /// Description: Repository dùng chung của phân hệ Video Wall, kế thừa SqlSugarRepository
    /// Created date: 15/08/2026
    /// </summary>
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base() { }
    }
}
