using Modules.TMS.Core.Entities;
using Modules.VDS.Core.Exceptions;
using Modules.VDS.Core.Constants;
using Shared.Utility.Counting;

namespace Modules.VDS.Infrastructure.Services.TrafficSummary;

public sealed class VdsTrafficSummaryService(BaseRepository<TmsTrafficSummary> repo) : IScoped
{
    public Task<List<TmsTrafficSummary>> ReadAsync(DateTime from, DateTime to, string period, string scope,
        string? scopeId = null, string? direction = null)
    {
        if (!VdsSummaryPeriodConst.IsValid(period) || scope is not (TrafficCountScopes.Equipment or TrafficCountScopes.Zone))
            throw Oops.Oh(BaseMsg.Vds.Exception.SummaryPeriodScopeInvalid);
        return repo.AsQueryable().Where(x => x.IsDelete == null && x.PeriodType == period && x.ScopeType == scope
                && x.FromTime >= from && x.ToTime <= to)
            .WhereIF(!string.IsNullOrWhiteSpace(scopeId), x => x.ScopeId == scopeId)
            .WhereIF(!string.IsNullOrWhiteSpace(direction), x => x.Direction == direction)
            .OrderBy(x => x.FromTime).OrderBy(x => x.ScopeId).ToListAsync();
    }
}
