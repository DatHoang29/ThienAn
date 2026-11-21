using Modules.TMS.Core.Entities;
using Modules.VDS.Core.Helper;

namespace Modules.VDS.Infrastructure.Services.Segment.Rules;

/// <summary>
/// Author: KienLT
/// Description: Một dòng kết quả đồng bộ đoạn: hành động, đoạn tính ra, bản ghi đang có và bản ghi sẽ ghi.
/// Created date: 03/10/2026
/// </summary>
public sealed record SegmentSyncItem(
    string Action, VdsBaseHelper.SegmentTopologyHp.PlannedSegment? Plan,
    TmsTrafficSegment? Existing,
    TmsTrafficSegment Target);
