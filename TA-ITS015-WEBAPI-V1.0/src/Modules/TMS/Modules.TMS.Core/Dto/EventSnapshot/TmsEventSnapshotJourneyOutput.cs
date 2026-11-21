using Modules.TMS.Core.Entities;

namespace Modules.TMS.Core.Dto.EventSnapshot;

/// <summary>
/// Author: KienLT
/// Description: Các hành trình vượt tốc độ hợp lệ trả về cho snapshot sự kiện VDS.
/// Created date: 22/10/2026
/// </summary>
public sealed class TmsEventSnapshotJourneyOutput
{
    public List<TmsTrafficJourney> Journeys { get; set; } = [];
}
