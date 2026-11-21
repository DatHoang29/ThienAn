using System.Linq.Expressions;
using Services.Shared.Configuration;
using Services.Shared.Persistence;
using Shared.Core.Domain;

namespace ITS.VideoWall.Infrastructure
{
    /// <summary>
    /// Description: Repository dung chung cua phan he Video Wall Service, ke thua VwRepositoryBase
    /// Created date: 10/09/2026
    /// </summary>
    public class BaseRepository<T> : VwRepositoryBase where T : class, new()
    {
        public BaseRepository(DatabaseManager databaseManager, IOptions<DbConnectionOptions> dbOptions)
            : base(databaseManager, dbOptions)
        {
        }

        public ISugarQueryable<T> AsQueryable() => Client.Queryable<T>();

        public async Task<T?> GetFirstAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => await Client.Queryable<T>().FirstAsync(predicate, ct);

        public async Task<T?> GetFirst(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => await Client.Queryable<T>().FirstAsync(predicate, ct);

        public ISqlSugarClient Context => Client;

        public IUpdateable<T> AsUpdateable() => Client.Updateable<T>();

        public IUpdateable<T> AsUpdateable(T entity) => Client.Updateable(entity);

        public IUpdateable<T> AsUpdateable(List<T> entities) => Client.Updateable(entities);

        public Task<int> UpdateAsync(T entity, CancellationToken ct = default)
            => Client.Updateable(entity).ExecuteCommandAsync(ct);

        public Task<int> Update(T entity, CancellationToken ct = default)
            => UpdateAsync(entity, ct);

        public Task<int> InsertAsync(T entity, CancellationToken ct = default)
        {
            if (entity is EntityTenant tenantEntity)
            {
                Stamp(tenantEntity);
            }

            return Client.Insertable(entity).ExecuteCommandAsync(ct);
        }

        public Task<int> Insert(T entity, CancellationToken ct = default)
            => InsertAsync(entity, ct);

        public Task<int> InsertRangeAsync(List<T> entities, CancellationToken ct = default)
        {
            foreach (var entity in entities)
            {
                if (entity is EntityTenant tenantEntity)
                {
                    Stamp(tenantEntity);
                }
            }

            return Client.Insertable(entities).ExecuteCommandAsync(ct);
        }

        public Task<int> InsertRange(List<T> entities, CancellationToken ct = default)
            => InsertRangeAsync(entities, ct);
    }
}
