// Author: Codex
// Description: Dedicated VDS storage credentials; never reads the post-inspection share account.
// Created date: 02/10/2026

using Modules.VDS.Core.Options;
using Modules.VDS.Core.Entities;
using Modules.VDS.Core.Constants;
using Services.Shared.Persistence;
using Services.Vds.Configuration;
using Services.Vds.Shared.Persistence;
using static Shared.DTO.Enums.BaseEnums;

namespace Services.Vds.Features.Media;

/// <summary>Dedicated VDS storage credentials; never reads the post-inspection share account.</summary>
public sealed class VdsImageShareSettingsRepository(DatabaseManager databases, IVdsConfigManager config)
    : VdsRepositoryBase(databases, config)
{
    public async Task<VdsImageShareOptions> ReadAsync(CancellationToken ct)
    {
        var codes = VdsImageShareOptions.Codes;
        var rows = await Client.Queryable<VdsConfig>()
            .Where(x => x.TenantId == TenantId && x.GroupCode == VdsConfigConst.Group.Config && x.IsDelete == null && x.Status == StatusEnum.Enable
                && codes.Contains(x.Code!)).Select(x => new { x.Code, x.Value }).ToListAsync(ct);
        return VdsImageShareOptions.FromValues(rows.Select(x => new KeyValuePair<string, string?>(x.Code!, x.Value)));
    }
}
