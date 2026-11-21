using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.VDS.Infrastructure;

/// <summary>SQL Server business tables retain the existing tenant/audit repository behavior.</summary>
public class BaseRepository<T> : SqlSugarRepository<T>, IScoped where T : class, new()
{
    public BaseRepository() : base() { }
}