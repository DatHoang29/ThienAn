using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.Media.Infrastructure
{
    /// <summary>
    /// Description: Repository cơ sở của phân hệ Media, kế thừa SqlSugarRepository.
    /// Created date: 08/10/2026
    /// </summary>
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base() { }
    }
}
