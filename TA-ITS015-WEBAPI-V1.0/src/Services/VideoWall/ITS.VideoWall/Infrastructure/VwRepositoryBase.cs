using Microsoft.Extensions.Options;
using Services.Shared.Configuration;
using Services.Shared.Persistence;
using Shared.Core.Domain;
using SqlSugar;

namespace ITS.VideoWall.Infrastructure
{
    /// <summary>
    /// Description: Base dung chung cho repository VideoWall - ke thua SqlSugarAccess (Services.Shared.Runtime)
    /// Created date: 13/09/2026
    /// </summary>
    public abstract class VwRepositoryBase : SqlSugarAccess
    {
        protected readonly string TenantId;

        protected VwRepositoryBase(DatabaseManager databaseManager, IOptions<DbConnectionOptions> dbOptions)
            : base(databaseManager, () => ResolveConnectionId(dbOptions))
        {
            TenantId = "Default";
        }

        private static string ResolveConnectionId(IOptions<DbConnectionOptions> dbOptions)
        {
            var id = dbOptions?.Value?.ConfigConnectionId;
            return string.IsNullOrWhiteSpace(id) ? "Default" : id;
        }

        public new ISqlSugarClient Client => base.Client;

        protected void Stamp(EntityTenant e, DateTime? now = null)
        {
            if (string.IsNullOrEmpty(e.ID)) e.ID = Guid.NewGuid().ToString();
            if (string.IsNullOrWhiteSpace(e.TenantId)) e.TenantId = TenantId;
            if (e.CreateTime == default) e.CreateTime = now ?? DateTime.Now;
        }
    }
}
