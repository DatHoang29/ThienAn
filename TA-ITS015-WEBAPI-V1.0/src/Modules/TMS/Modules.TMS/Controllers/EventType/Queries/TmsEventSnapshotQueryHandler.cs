using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.EventSnapshot;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Enums;

namespace Modules.TMS.Controllers.EventType.Queries;

/// <summary>
/// Author: KienLT
/// Description: Đọc danh mục, vùng và hành trình phục vụ snapshot VDS trong module TMS.
/// Created date: 22/10/2026
/// </summary>
public sealed class TmsEventSnapshotQueryHandler(
    BaseRepository<TmsEventType> eventTypes,
    BaseRepository<TmsEquipment> equipments,
    BaseRepository<TmsZoneEquipment> zoneEquipments,
    BaseRepository<TmsTrafficJourney> journeys) : IWolverineHandler
{
    /// <summary>
    /// Author: KienLT
    /// Description: Đọc loại sự kiện đang bật và camera chưa xóa.
    /// Created date: 22/10/2026
    /// </summary>
    public async Task<TmsEventSnapshotCatalogOutput> HandleAsync(TmsEventSnapshotCatalogInput input)
    {
        var types = await eventTypes.AsQueryable()
            .Where(x => x.IsDelete == null && x.Status == BaseEnums.StatusEnum.Enable)
            .Select(x => new TmsEventSnapshotTypeOutput
            {
                Code = x.Code, Name = x.Name, Type = x.Type, Url = x.Url,
                WarningType = x.WarningType, Priority = x.Priority
            }).ToListAsync();
        var cameras = await equipments.AsQueryable()
            .Where(x => x.IsDelete == null)
            .Select(x => new TmsEventSnapshotCameraOutput
            {
                ID = x.ID, Name = x.Name, Location = x.Location, KmNumber = x.KmNumber, MetNumber = x.MetNumber
            }).ToListAsync();
        return new TmsEventSnapshotCatalogOutput { Types = types, Cameras = cameras };
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Đọc thiết bị thuộc vùng với cùng bộ lọc xóa mềm.
    /// Created date: 22/10/2026
    /// </summary>
    public async Task<TmsEventSnapshotZoneOutput> HandleAsync(TmsEventSnapshotZoneInput input)
        => new()
        {
            EquipmentIds = await zoneEquipments.AsQueryable()
                .Where(x => x.IsDelete == null && x.ZoneId == input.ZoneId && x.EquipmentId != null)
                .Select(x => x.EquipmentId!).ToListAsync()
        };

    /// <summary>
    /// Author: KienLT
    /// Description: Đọc hành trình vượt tốc độ hợp lệ trong cửa sổ hiển thị.
    /// Created date: 22/10/2026
    /// </summary>
    public async Task<TmsEventSnapshotJourneyOutput> HandleAsync(TmsEventSnapshotJourneyInput input)
        => new()
        {
            Journeys = await journeys.AsQueryable()
                .Where(x => x.IsDelete == null && x.CreateTime >= input.CreatedFrom
                    && x.IsOverSpeed == true && x.IsValid == true && x.ToTime >= input.From)
                .WhereIF(input.ZoneCameras != null, x => input.ZoneCameras!.Contains(x.ToEquipmentId!))
                .WhereIF(!string.IsNullOrWhiteSpace(input.EquipmentId), x => x.ToEquipmentId == input.EquipmentId)
                .WhereIF(!string.IsNullOrWhiteSpace(input.Direction), x => x.Direction == input.Direction)
                .OrderBy(x => x.ToTime, OrderByType.Desc)
                .Take(input.MaxItems)
                .ToListAsync()
        };

    /// <summary>
    /// Author: KienLT
    /// Description: Kiểm tra loại sự kiện đang bật để cấu hình thời gian hiển thị.
    /// Created date: 22/10/2026
    /// </summary>
    public async Task<TmsEventDisplayTypeOutput> HandleAsync(TmsEventDisplayTypeInput input)
        => new()
        {
            Exists = await eventTypes.Context.Queryable<TmsEventType>()
                .Where(x => x.IsDelete == null && x.Status == BaseEnums.StatusEnum.Enable && x.Code == input.Code
                    && x.Type!.Trim().ToLower() == IncidentEventGroupConst.Event).AnyAsync()
        };

    /// <summary>
    /// Author: KienLT
    /// Description: Tra mã loại sự kiện chưa xoá cho luật ghi cấu hình VDS (ánh xạ, skip/dedup, dùng lại ảnh).
    /// Created date: 22/10/2026
    /// </summary>
    public async Task<TmsEventTypeCodeOutput> HandleAsync(TmsEventTypeCodeInput input)
    {
        if (string.IsNullOrEmpty(input.Code)) return new TmsEventTypeCodeOutput();
        var catalogCode = await eventTypes.Context.Queryable<TmsEventType>()
            .Where(x => x.IsDelete == null && x.Code == input.Code).Select(x => x.Code).FirstAsync();
        var tenantCount = string.IsNullOrWhiteSpace(input.TenantId) ? 0 : await eventTypes.Context.Queryable<TmsEventType>()
            .Where(x => x.Code == input.Code && x.TenantId == input.TenantId && x.IsDelete == null).CountAsync();
        return new TmsEventTypeCodeOutput { CatalogCode = catalogCode, TenantCount = tenantCount };
    }
}
