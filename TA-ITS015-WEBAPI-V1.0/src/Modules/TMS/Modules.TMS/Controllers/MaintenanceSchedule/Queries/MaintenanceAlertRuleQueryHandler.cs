using Modules.TMS.Controllers.MaintenanceSchedule.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.MaintenanceSchedule.Queries;

/// <summary>
/// Author: System
/// Description: Handler xử lý các truy vấn của MaintenanceAlertRule
/// Created date: 11/01/2025
/// </summary>
public class MaintenanceAlertRuleQueryHandler : IWolverineHandler
{
    private BaseRepository<TmsMaintenanceAlertRule> _ruleRep;
    private BaseRepository<TmsEquipmentType> _equipmentTypeRep;
    private BaseRepository<TmsMaintenanceGuide> _guideRep;
    private UserManager _user;
    private BaseCacheService _cache;

    public MaintenanceAlertRuleQueryHandler(
        BaseRepository<TmsMaintenanceAlertRule> ruleRep,
        BaseRepository<TmsEquipmentType> equipmentTypeRep,
        BaseRepository<TmsMaintenanceGuide> guideRep,
        UserManager user,
        BaseCacheService cache)
    {
        _ruleRep = ruleRep;
        _equipmentTypeRep = equipmentTypeRep;
        _guideRep = guideRep;
        _user = user;
        _cache = cache;
    }

    /// <summary>
    /// Lấy danh sách quy tắc cảnh báo với phân trang và filter
    /// </summary>
    public async Task<SqlSugarPagedList<MaintenanceAlertRuleListOutput>> HandleAsync(GetMaintenanceAlertRuleListInput command)
    {
        var pagedList = await _ruleRep.AsQueryable()
            .WhereIF(!string.IsNullOrEmpty(command.Name), u => u.Name.Contains(command.Name!))
            .WhereIF(!string.IsNullOrEmpty(command.EquipmentTypeId), u => u.EquipmentTypeId == command.EquipmentTypeId)
            .WhereIF(command.IsActive.HasValue, u => u.IsActive == command.IsActive)
            .OrderByDescending(u => u.CreateTime)
            .Select(u => new MaintenanceAlertRuleListOutput() { }, true)
            .ToPagedListAsync(command.Page, command.PageSize);

        return pagedList;
    }

    /// <summary>
    /// Lấy chi tiết quy tắc cảnh báo
    /// </summary>
    public async Task<MaintenanceAlertRuleDetailOutput> HandleAsync(GetMaintenanceAlertRuleByIdInput command)
    {
        var rule = await _ruleRep.GetByIdAsync(command.ID)
                   ?? throw Oops.Oh(BaseMsg.TmsMaintenanceAlertRule.Exception.NotFound);

        var output = rule.Adapt<MaintenanceAlertRuleDetailOutput>();

        // Get equipment type info
        if (!string.IsNullOrEmpty(rule.EquipmentTypeId))
        {
            var equipmentType = await _equipmentTypeRep.GetByIdAsync(rule.EquipmentTypeId);
            if (equipmentType != null)
            {
                output.EquipmentType = equipmentType.Adapt<EquipmentTypeBasicOutput>();
            }
        }

        // Get guide info if exists
        if (!string.IsNullOrEmpty(rule.SuggestedGuideId))
        {
            var guide = await _guideRep.GetByIdAsync(rule.SuggestedGuideId);
            if (guide != null)
            {
                output.SuggestedGuide = guide.Adapt<MaintenanceGuideBasicOutput>();
            }
        }

        return output;
    }

    /// <summary>
    /// Lấy tất cả quy tắc cảnh báo đang active
    /// </summary>
    public async Task<List<MaintenanceAlertRuleListOutput>> HandleAsync(GetActiveAlertRulesInput command)
    {
        var rules = await _ruleRep.GetListAsync(u => u.IsActive == true);

        var items = new List<MaintenanceAlertRuleListOutput>();
        foreach (var rule in rules)
        {
            var output = rule.Adapt<MaintenanceAlertRuleListOutput>();

            // Get equipment type info
            if (!string.IsNullOrEmpty(rule.EquipmentTypeId))
            {
                var equipmentType = await _equipmentTypeRep.GetByIdAsync(rule.EquipmentTypeId);
                if (equipmentType != null)
                {
                    output.EquipmentTypeName = equipmentType.Name;
                }
            }

            items.Add(output);
        }

        return items;
    }
}
