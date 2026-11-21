using ITS.Toll.Core.Configuration;
using Microsoft.Extensions.Options;
using Services.Shared.Configuration;
using Services.Shared.Persistence;
using Shared.Core.Domain;

namespace ITS.Toll.Core.Persistence;

/// <summary>
/// Base dùng chung cho các repository Toll — dùng REPOSITORY DÙNG CHUNG của service
/// Author: ChuongLP
/// Created date: 26/08/2026
/// </summary>
public abstract class TollRepositoryBase : SqlSugarAccess
{
    /// <summary>TenantId gán cho bản ghi mới.</summary>
    protected readonly string TenantId;

    protected TollRepositoryBase(DatabaseManager databaseManager, IOptions<DbConnectionOptions> dbOptions, TollServiceOptions service)
        : base(databaseManager, () => ResolveConnectionId(dbOptions))
    {
        TenantId = string.IsNullOrWhiteSpace(service.TenantId) ? "Default" : service.TenantId;
    }

    private static string ResolveConnectionId(IOptions<DbConnectionOptions> dbOptions)
    {
        var id = dbOptions.Value.ConfigConnectionId;
        return string.IsNullOrWhiteSpace(id) ? "Default" : id;
    }

    /// <summary>Gán ID (Guid) + TenantId + CreateTime cho bản ghi mới.</summary>
    protected void Stamp(EntityTenant e, DateTime now)
    {
        if (string.IsNullOrEmpty(e.ID)) e.ID = Guid.NewGuid().ToString();
        e.TenantId = TenantId;
        e.CreateTime = now;
    }
}
