using SqlSugar;

namespace ITS.Sync.Infrastructure.Persistence;

/// <summary>
/// Repository cơ sở kế thừa SimpleClient của SqlSugar (giống ITS nhưng KHÔNG dính Furion).
/// Nhận kết nối nguồn/đích qua constructor. Có sẵn AsQueryable/AsInsertable/AsUpdateable/AsDeleteable...
/// </summary>
public class BaseRepository<T>(ISqlSugarClient db) : SimpleClient<T>(db) where T : class, new()
{
}
