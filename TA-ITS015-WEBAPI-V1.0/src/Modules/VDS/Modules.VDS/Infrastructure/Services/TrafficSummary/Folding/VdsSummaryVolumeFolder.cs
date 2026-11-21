using Shared.Utility.Utilities;
using Shared.DTO.Dtos.Vds;
using Modules.VDS.Core.Constants;
using Shared.Utility.Counting;
namespace Modules.VDS.Infrastructure.Services.TrafficSummary.Folding;
public static class VdsSummaryVolumeFolder
{
    public static List<VdsTrafficVolumeBreakdownRow> FoldSummary(IEnumerable<Modules.TMS.Core.Entities.TmsTrafficSummary> rows)
        => rows.Where(x => x.FromTime != null && x.PeriodType == VdsSummaryPeriodConst.Hour && x.ScopeType == TrafficCountScopes.Equipment)
            .SelectMany(x => new[] {
                new VdsTrafficVolumeBreakdownRow { BucketTime = BaseUtil.TimeServer.ToUtc(x.FromTime!.Value), Direction = x.Direction, VehicleSize = "short", VehicleCount = x.SVehicleNumber ?? 0 },
                new VdsTrafficVolumeBreakdownRow { BucketTime = BaseUtil.TimeServer.ToUtc(x.FromTime!.Value), Direction = x.Direction, VehicleSize = "medium", VehicleCount = x.MVehicleNumber ?? 0 },
                new VdsTrafficVolumeBreakdownRow { BucketTime = BaseUtil.TimeServer.ToUtc(x.FromTime!.Value), Direction = x.Direction, VehicleSize = "long", VehicleCount = x.LVehicleNumber ?? 0 },
                new VdsTrafficVolumeBreakdownRow { BucketTime = BaseUtil.TimeServer.ToUtc(x.FromTime!.Value), Direction = x.Direction, VehicleSize = "other", VehicleCount = x.OtherVehicleNumber ?? 0 }
            }).GroupBy(x => new { x.BucketTime, x.Direction, x.VehicleSize })
            .Select(g => new VdsTrafficVolumeBreakdownRow { BucketTime = g.Key.BucketTime, Direction = g.Key.Direction,
                VehicleSize = g.Key.VehicleSize, VehicleCount = g.Sum(x => x.VehicleCount) }).OrderBy(x => x.BucketTime).ToList();
}
