using Modules.VDS.Core.Dto;
using Modules.TMS.Core.Entities;
using Modules.VDS.Core.Entities;
using Modules.VDS.Infrastructure;
using Modules.VDS.Infrastructure.Services.Persistence.Quest;

// KienLT - 27/10/2026: dựng truy vấn trên client do GuardAsync cấp để hỗ trợ router HA.
namespace Modules.VDS.Controllers.Media.Queries;

/// <summary>
/// Author: KienLT
/// Description: Tra nguồn thô (camera, loại bản tin) của một bản tin đăng ký từ QuestDB, chịu sai số làm tròn
///              datetime của SQL (±4 ms) và kiểm thiết bị thuộc đúng tenant.
///
///              Message <see cref="VdsRegistrationSourceLookupInput"/> là hợp đồng liên module (khai ở
///              <c>Modules.VDS.Core/Media</c>), do TMS (<c>TmsVdsRegistrationImageService</c>) gửi qua message bus.
///              Chuyển từ <c>Infrastructure/Services/Media</c> về <c>Controllers/Media/Queries</c> ngày 13/10/2026.
/// Created date: 02/10/2026
/// </summary>
public sealed class VdsRegistrationSourceQueryHandler(
    VdsRepository<VdsTrafficData> records, BaseRepository<TmsEquipment> equipment) : IWolverineHandler
{
    public async Task<VdsRegistrationImageSourceOutput?> HandleAsync(VdsRegistrationSourceLookupInput input)
    {
        if (input.EventId < 0 || input.DetectTimeUtc == default || string.IsNullOrWhiteSpace(input.TenantId)) return null;
        var time = DateTime.SpecifyKind(input.DetectTimeUtc, DateTimeKind.Utc);
        var start = time.AddMilliseconds(-4);
        var end = time.AddMilliseconds(4);
        var rows = await records.GuardAsync(questDb => questDb.Queryable<VdsTrafficData>()
            .Where(x => x.VendorEventId == input.EventId && x.DetectTime >= start && x.DetectTime <= end && x.IsDelete == null)
            .Take(2).ToListAsync());
        if (rows.Count != 1) return null; // Ambiguous source must never select somebody else's camera.
        var row = rows[0];
        var owned = await equipment.Context.Queryable<TmsEquipment>()
            .Where(x => x.ID == row.EquipmentId && x.TenantId == input.TenantId && x.IsDelete == null).AnyAsync();
        return owned ? new(row.VendorEventId, DateTime.SpecifyKind(row.DetectTime, DateTimeKind.Utc),
            row.EquipmentId!, row.CameraIp, row.EventType) : null;
    }
}
