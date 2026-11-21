using System.Globalization;
using Modules.VDS.Core.Constants;
using BaseMsg = Modules.VDS.Core.Exceptions.BaseMsg;
using Modules.VDS.Core.Dto;

namespace Modules.VDS.Core.Helper;

/// <summary>
/// Author: KienLT
/// Description: Chiều lý trình theo tuyến và mã hướng địa lý; dùng chung TMS và VDS.
/// Created date: 02/10/2026
/// </summary>
public class VdsRouteOrientationHelper
{
    public static Func<string?, int, int> Create(IEnumerable<VdsSegmentTopologyHelper.TopoConfigRow> rows, List<VdsSegmentTopologyMessageOutput> errors)
    {
        var map = new Dictionary<(string Route, int Direction), int>();
        foreach (var row in rows.Where(r => r.GroupCode == VdsConfigConst.Group.Segment
                     && r.Code?.Trim().StartsWith(VdsConfigConst.Pattern.KmDirPrefix, StringComparison.Ordinal) == true))
        {
            var code = row.Code!.Trim()[VdsConfigConst.Pattern.KmDirPrefix.Length..];
            var cut = code.LastIndexOf('_');
            var sign = row.Value?.Trim().ToLowerInvariant() switch { "asc" => 1, "desc" => -1, _ => 0 };
            if (cut <= 0 || !int.TryParse(code[(cut + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dir)
                || dir is not (1 or 2) || sign == 0)
            {
                errors.Add(new(BaseMsg.Vds.Message.TopologyKmDirInvalid, row.Code ?? "", row.Value ?? ""));
                continue;
            }
            var key = (code[..cut].Trim().ToLowerInvariant(), dir);
            if (!map.TryAdd(key, sign)) errors.Add(new(BaseMsg.Vds.Message.TopologyKmDirDuplicate, row.Code ?? ""));
        }
        return (route, dir) => map.TryGetValue(((route ?? "").Trim().ToLowerInvariant(), dir), out var sign) ? sign
            : map.TryGetValue((VdsSegmentTopologyHelper.DefaultRoute, dir), out var fallback) ? fallback
            : dir == 1 ? -1 : 1;
    }
}
