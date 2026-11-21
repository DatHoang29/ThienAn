using Modules.TMS.Controllers.MaintenanceSchedule.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.Core.Utilities;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.MaintenanceSchedule.Queries;

/// <summary>
/// Author: System
/// Description: Handler xử lý các truy vấn của MaintenanceGuide
/// Created date: 10/12/2025
/// </summary>
public class MaintenanceGuideQueryHandler : IWolverineHandler
{
    private BaseRepository<TmsMaintenanceGuide> _guideRep;
    private BaseRepository<TmsEquipmentType> _equipmentTypeRep;
    private BaseRepository<TmsEquipment> _equipmentRep;
    private BaseCacheService _cache;

    private const string CachePrefix = "TmsMaintenanceGuide";

    public MaintenanceGuideQueryHandler(
        BaseRepository<TmsMaintenanceGuide> guideRep,
        BaseRepository<TmsEquipmentType> equipmentTypeRep,
        BaseRepository<TmsEquipment> equipmentRep,
        BaseCacheService cache)
    {
        _guideRep = guideRep;
        _equipmentTypeRep = equipmentTypeRep;
        _equipmentRep = equipmentRep;
        _cache = cache;
    }

    /// <summary>
    /// Lấy danh sách hướng dẫn bảo trì
    /// </summary>
    public async Task<List<MaintenanceGuideOutput>> HandleAsync(MaintenanceGuideInput query)
    {
        var result = await _guideRep.AsQueryable()
            .WhereIF(!string.IsNullOrEmpty(query.Name), u => u.Name!.Contains(query.Name!))
            .WhereIF(!string.IsNullOrEmpty(query.EquipmentTypeId), u => u.EquipmentTypeId == query.EquipmentTypeId)
            .WhereIF(!string.IsNullOrEmpty(query.Manufacturer), u => u.Manufacturer == query.Manufacturer)
            .WhereIF(!string.IsNullOrEmpty(query.Frequency), u => u.Frequency == query.Frequency)
            .WhereIF(!string.IsNullOrEmpty(query.Priority), u => u.Priority == query.Priority)
            .WhereIF(query.Status.HasValue, u => u.Status == query.Status)
            .OrderByDescending(u => u.CreateTime)
            .ToListAsync();

        return await MapToOutput(result);
    }

    /// <summary>
    /// Lấy danh sách có phân trang
    /// </summary>
    public async Task<SqlSugarPagedList<MaintenanceGuideOutput>> HandleAsync(PageMaintenanceGuideInput query)
    {
        var pagedList = await _guideRep.AsQueryable()
            .WhereIF(!string.IsNullOrEmpty(query.Name), u => u.Name!.Contains(query.Name!))
            .WhereIF(!string.IsNullOrEmpty(query.EquipmentTypeId), u => u.EquipmentTypeId == query.EquipmentTypeId)
            .WhereIF(!string.IsNullOrEmpty(query.Manufacturer), u => u.Manufacturer == query.Manufacturer)
            .WhereIF(!string.IsNullOrEmpty(query.Frequency), u => u.Frequency == query.Frequency)
            .WhereIF(!string.IsNullOrEmpty(query.Priority), u => u.Priority == query.Priority)
            .WhereIF(query.Status.HasValue, u => u.Status == query.Status)
            .OrderByDescending(u => u.CreateTime)
            .Select(u => new MaintenanceGuideOutput() { }, true)
            .ToPagedListAsync(query.Page, query.PageSize);

        return pagedList;
    }

    /// <summary>
    /// Lấy chi tiết hướng dẫn bảo trì theo ID
    /// </summary>
    public async Task<MaintenanceGuideOutput?> HandleAsync(BaseIdInput query)
    {
        var entity = await _guideRep.GetByIdAsync(query.ID);
        if (entity == null) return null;

        var output = entity.Adapt<MaintenanceGuideOutput>();
        await EnrichOutput(output, entity);
        return output;
    }

    /// <summary>
    /// Lấy hướng dẫn bảo trì phù hợp cho thiết bị
    /// </summary>
    public async Task<List<MaintenanceGuideOutput>> HandleAsync(GetGuideByEquipmentInput query)
    {
        string? equipmentTypeId = query.EquipmentTypeId;
        string? manufacturer = query.Manufacturer;

        // Nếu có EquipmentId, lấy thông tin từ equipment
        if (!string.IsNullOrEmpty(query.EquipmentId))
        {
            var equipment = await _equipmentRep.GetByIdAsync(query.EquipmentId);
            if (equipment != null)
            {
                equipmentTypeId = equipment.EquipmentTypeId;
                manufacturer = equipment.Manufacturer;
            }
        }

        if (string.IsNullOrEmpty(equipmentTypeId))
            return new List<MaintenanceGuideOutput>();

        // 1. Tìm guide khớp chính xác (type + manufacturer)
        var exactMatches = await _guideRep.AsQueryable()
            .Where(u => u.EquipmentTypeId == equipmentTypeId)
            .Where(u => u.Manufacturer == manufacturer)
            .Where(u => u.Status == 1)
            .OrderByDescending(u => u.Priority == "High" ? 3 : u.Priority == "Medium" ? 2 : 1)
            .ToListAsync();

        // 2. Tìm guide generic (type only, manufacturer = null)
        var genericMatches = await _guideRep.AsQueryable()
            .Where(u => u.EquipmentTypeId == equipmentTypeId)
            .Where(u => u.Manufacturer == null)
            .Where(u => u.Status == 1)
            .OrderByDescending(u => u.Priority == "High" ? 3 : u.Priority == "Medium" ? 2 : 1)
            .ToListAsync();

        var result = new List<MaintenanceGuideOutput>();

        // Exact matches first
        foreach (var item in exactMatches)
        {
            var output = item.Adapt<MaintenanceGuideOutput>();
            await EnrichOutput(output, item);
            output.MatchType = "exact";
            result.Add(output);
        }

        // Generic matches second
        foreach (var item in genericMatches)
        {
            var output = item.Adapt<MaintenanceGuideOutput>();
            await EnrichOutput(output, item);
            output.MatchType = "generic";
            result.Add(output);
        }

        return result;
    }

    #region Private Methods

    private async Task<List<MaintenanceGuideOutput>> MapToOutput(List<TmsMaintenanceGuide> guides)
    {
        var result = new List<MaintenanceGuideOutput>();

        foreach (var guide in guides)
        {
            var output = guide.Adapt<MaintenanceGuideOutput>();
            await EnrichOutput(output, guide);
            result.Add(output);
        }

        return result;
    }

    private async Task EnrichOutput(MaintenanceGuideOutput output, TmsMaintenanceGuide guide)
    {
        // Get equipment type name
        if (!string.IsNullOrEmpty(guide.EquipmentTypeId))
        {
            var equipmentType = await _equipmentTypeRep.GetByIdAsync(guide.EquipmentTypeId);
            output.EquipmentTypeName = equipmentType?.Name;
        }

        // Parse checklist JSON
        if (!string.IsNullOrEmpty(guide.Checklist))
        {
            try
            {
                output.ChecklistItems = JsonConvert.DeserializeObject<List<ChecklistItem>>(guide.Checklist);
            }
            catch
            {
                output.ChecklistItems = null;
            }
        }
    }

    #endregion
}
