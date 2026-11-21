using Modules.VDS.Core.Services.Media;
using Modules.VDS.Core.Constants;
using Modules.VDS.Core.Entities;

namespace Modules.VDS.Core.Options;

/// <summary>
/// Author: KienLT
/// Description: Dedicated VdsConfig storage credentials, held in memory only.
/// Created date: 02/10/2026
/// </summary>
public sealed class VdsImageShareOptions(string? basePath, string? userName, string? password, string? domain)
{
    public string? BasePath { get; } = basePath;
    public string? UserName { get; } = userName;
    public string? Password { get; } = password;
    public string? Domain { get; } = domain;
    public override string ToString() => "VDS image-share settings (credentials redacted)";

    public static readonly string[] Codes = [VdsConfigConst.Code.MediaStorageDirectory,
        VdsConfigConst.Code.MediaStorageUsername, VdsConfigConst.Code.MediaStoragePassword, VdsConfigConst.Code.MediaStorageDomain];

    public static VdsImageShareOptions ForRoot(IEnumerable<VdsConfig> rows, string configuredRoot)
    {
        var share = VdsImageShareAccess.UncShare(configuredRoot);
        var candidates = rows.Where(x => x.GroupCode == VdsConfigConst.Group.Config && x.IsDelete == null
                && x.Status == Shared.DTO.Enums.BaseEnums.StatusEnum.Enable)
            .GroupBy(x => x.TenantId)
            .Where(group => group.Any(x => x.Code == Codes[0] && share is not null
                && string.Equals(VdsImageShareAccess.UncShare(x.Value), share, StringComparison.OrdinalIgnoreCase)))
            .Select(group => FromValues(group.Select(x => new KeyValuePair<string, string?>(x.Code!, x.Value)))).ToList();
        if (candidates.Count == 0) return new(configuredRoot, null, null, null);
        var first = candidates[0];
        if (candidates.Any(x => x.UserName != first.UserName || x.Password != first.Password || x.Domain != first.Domain))
            throw new InvalidOperationException("Conflicting VdsConfig credentials for the configured VDS share.");
        return new(configuredRoot, first.UserName, first.Password, first.Domain);
    }

    public static VdsImageShareOptions FromValues(IEnumerable<KeyValuePair<string, string?>> values)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in values.Where(pair => Codes.Contains(pair.Key, StringComparer.OrdinalIgnoreCase)))
        {
            if (map.TryGetValue(pair.Key, out var previous) && previous != pair.Value)
                throw new InvalidOperationException("VdsConfig has conflicting active values for " + pair.Key + ".");
            map[pair.Key] = pair.Value;
        }
        return new(map.GetValueOrDefault(Codes[0]), map.GetValueOrDefault(Codes[1]),
            map.GetValueOrDefault(Codes[2]), map.GetValueOrDefault(Codes[3]));
    }
}
