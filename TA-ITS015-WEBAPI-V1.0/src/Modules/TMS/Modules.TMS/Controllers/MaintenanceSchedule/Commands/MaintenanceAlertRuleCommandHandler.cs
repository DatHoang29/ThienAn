using Modules.TMS.Controllers.MaintenanceSchedule.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.MaintenanceSchedule.Commands;

/// <summary>
/// Author: System
/// Description: Handler xử lý các lệnh của MaintenanceAlertRule
/// Created date: 11/01/2025
/// </summary>
public class MaintenanceAlertRuleCommandHandler : IWolverineHandler
{
    private BaseRepository<TmsMaintenanceAlertRule> _ruleRep;
    private BaseRepository<TmsEquipmentType> _equipmentTypeRep;
    private BaseRepository<TmsMaintenanceGuide> _guideRep;
    private UserManager _user;
    private BaseCacheService _cache;

    public MaintenanceAlertRuleCommandHandler(
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
    /// Thêm mới quy tắc cảnh báo
    /// </summary>
    public async Task HandleAsync(AddMaintenanceAlertRuleInput command)
    {
        // Validate EquipmentType exists if provided
        if (!string.IsNullOrEmpty(command.EquipmentTypeId))
        {
            var equipmentTypeExists = await _equipmentTypeRep.IsAnyAsync(u => u.ID == command.EquipmentTypeId);
            if (!equipmentTypeExists)
                throw Oops.Oh(BaseMsg.TmsMaintenanceAlertRule.Exception.EquipmentTypeNotFound);
        }

        // Validate SuggestedGuide if provided
        if (!string.IsNullOrEmpty(command.SuggestedGuideId))
        {
            var guideExists = await _guideRep.IsAnyAsync(g => g.ID == command.SuggestedGuideId);
            if (!guideExists)
                throw Oops.Oh(BaseMsg.TmsMaintenanceAlertRule.Exception.GuideNotFound);
        }

        // Check duplicate name for same EquipmentType
        var duplicateExists = await _ruleRep.IsAnyAsync(u =>
            u.EquipmentTypeId == command.EquipmentTypeId &&
            u.Name == command.Name);
        if (duplicateExists)
            throw Oops.Oh(BaseMsg.TmsMaintenanceAlertRule.Exception.AlreadyExists);

        var entity = command.Adapt<TmsMaintenanceAlertRule>();
        entity.IsActive = command.IsActive;

        await _ruleRep.InsertAsync(entity);
        _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMaintenanceAlertRule);
    }

    /// <summary>
    /// Cập nhật quy tắc cảnh báo
    /// </summary>
    public async Task HandleAsync(UpdateMaintenanceAlertRuleInput command)
    {
        var entity = await _ruleRep.GetByIdAsync(command.ID)
                     ?? throw Oops.Oh(BaseMsg.TmsMaintenanceAlertRule.Exception.NotFound);

        // Validate EquipmentType exists if provided
        if (!string.IsNullOrEmpty(command.EquipmentTypeId))
        {
            var equipmentTypeExists = await _equipmentTypeRep.IsAnyAsync(u => u.ID == command.EquipmentTypeId);
            if (!equipmentTypeExists)
                throw Oops.Oh(BaseMsg.TmsMaintenanceAlertRule.Exception.EquipmentTypeNotFound);
        }

        // Validate SuggestedGuide if provided
        if (!string.IsNullOrEmpty(command.SuggestedGuideId))
        {
            var guideExists = await _guideRep.IsAnyAsync(g => g.ID == command.SuggestedGuideId);
            if (!guideExists)
                throw Oops.Oh(BaseMsg.TmsMaintenanceAlertRule.Exception.GuideNotFound);
        }

        // Check duplicate name (exclude current)
        var duplicateExists = await _ruleRep.IsAnyAsync(u =>
            u.ID != command.ID &&
            u.EquipmentTypeId == command.EquipmentTypeId &&
            u.Name == command.Name);
        if (duplicateExists)
            throw Oops.Oh(BaseMsg.TmsMaintenanceAlertRule.Exception.AlreadyExists);

        var updatedEntity = command.Adapt(entity);
        await _ruleRep.UpdateAsync(updatedEntity);
        _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMaintenanceAlertRule);
    }

    /// <summary>
    /// Xóa quy tắc cảnh báo
    /// </summary>
    public async Task HandleAsync(DeleteMaintenanceAlertRuleInput command)
    {
        var entity = await _ruleRep.GetByIdAsync(command.ID)
                     ?? throw Oops.Oh(BaseMsg.TmsMaintenanceAlertRule.Exception.NotFound);

        await _ruleRep.DeleteAsync(entity);
        _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMaintenanceAlertRule);
    }

    /// <summary>
    /// Kích hoạt quy tắc cảnh báo
    /// </summary>
    public async Task HandleAsync(ActivateAlertRuleInput command)
    {
        var entity = await _ruleRep.GetByIdAsync(command.ID)
                     ?? throw Oops.Oh(BaseMsg.TmsMaintenanceAlertRule.Exception.NotFound);

        entity.IsActive = true;
        await _ruleRep.UpdateAsync(entity);
        _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMaintenanceAlertRule);
    }

    /// <summary>
    /// Hủy kích hoạt quy tắc cảnh báo
    /// </summary>
    public async Task HandleAsync(DeactivateAlertRuleInput command)
    {
        var entity = await _ruleRep.GetByIdAsync(command.ID)
                     ?? throw Oops.Oh(BaseMsg.TmsMaintenanceAlertRule.Exception.NotFound);

        entity.IsActive = false;
        await _ruleRep.UpdateAsync(entity);
        _cache.RemoveByPrefixKey(CacheConst.Tms.TmsMaintenanceAlertRule);
    }
}
