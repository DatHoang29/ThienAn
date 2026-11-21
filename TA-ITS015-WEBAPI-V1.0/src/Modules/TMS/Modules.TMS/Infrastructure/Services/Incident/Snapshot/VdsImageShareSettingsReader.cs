using Modules.VDS.Core.Options;
using Modules.VDS.Core.Entities;
using Modules.VDS.Core.Constants;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Infrastructure.Services.Incident.Snapshot;

/// <summary>
/// Author: KienLT
/// Description: Resolves dedicated VDS credentials for a configured storage share, including background jobs.
/// Created date: 02/10/2026
/// </summary>
public sealed class VdsImageShareSettingsReader(BaseRepository<VdsConfig> settings)
{
    public async Task<VdsImageShareOptions> ReadAsync(string configuredRoot, CancellationToken ct)
    {
        var codes = VdsImageShareOptions.Codes;
        var rows = await settings.Context.Queryable<VdsConfig>()
            .Where(x => x.GroupCode == VdsConfigConst.Group.Config && x.IsDelete == null && x.Status == StatusEnum.Enable
                && codes.Contains(x.Code!)).ToListAsync(ct);
        return VdsImageShareOptions.ForRoot(rows, configuredRoot);
    }
}
