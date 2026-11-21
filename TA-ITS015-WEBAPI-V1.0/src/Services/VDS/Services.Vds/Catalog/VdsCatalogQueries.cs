using Modules.CfgSystem.Core.Entities;
using Shared.DTO.Enums;

namespace Services.Vds.Catalog;

/// <summary>Typed SQL Server catalog queries; kept separate for provider translation tests.</summary>
internal static class VdsCatalogQueries
{
    internal static ISugarQueryable<VdsCatalogRepository.OpConfigRow> OpConfig(
        ISqlSugarClient db, string groupCode, string tenantId)
        => db.Queryable<VdsConfig>()
            .Where(x => x.GroupCode == groupCode && x.TenantId == tenantId
                && x.IsDelete == null && x.Status == BaseEnums.StatusEnum.Enable)
            .Select(x => new VdsCatalogRepository.OpConfigRow { Code = x.Code, Value = x.Value });

    internal static ISugarQueryable<VdsCatalogRepository.ConfigDataRow> VdsConfigData(
        ISqlSugarClient db, string groupCode, string tenantId)
        => db.Queryable<VdsConfig>()
            .Where(x => x.GroupCode == groupCode && x.TenantId == tenantId
                && x.IsDelete == null && x.Status == BaseEnums.StatusEnum.Enable)
            .OrderBy(x => x.OrderNo).OrderBy(x => x.ID)
            .Select(x => new VdsCatalogRepository.ConfigDataRow { Code = x.Code, Name = x.Name, Value = x.Value });

    /// <summary>
    /// Cấu hình tự động TMS còn hiệu lực của tenant, kèm mã loại sự kiện — nguồn để VDS suy danh sách loại cần gửi
    /// sang TMS. Lọc nguồn (<c>AllowedSources</c>) làm trong RAM bằng luật dùng chung của TMS.
    /// </summary>
    internal static ISugarQueryable<VdsCatalogRepository.AutomationConfigRow> AutomationConfigs(
        ISqlSugarClient db, string tenantId)
        => db.Queryable<Modules.TMS.Core.Entities.TmsIncidentAutomationConfig>()
            .InnerJoin<Modules.TMS.Core.Entities.TmsEventType>((config, type) => type.ID == config.EventTypeId)
            .Where((config, type) => config.TenantId == tenantId && config.IsDelete == null
                && config.Status == BaseEnums.StatusEnum.Enable && type.IsDelete == null
                && (config.IsAutoExecuteEnabled || config.IsUnclaimedAutoFinishEnabled))
            .Select((config, type) => new VdsCatalogRepository.AutomationConfigRow
            {
                EventTypeCode = type.Code,
                AllowedSources = config.AllowedSources,
            });

    internal static ISugarQueryable<VdsCatalogRepository.ConfigDataRow> SharedConfigData(
        ISqlSugarClient db, string configTypeCode)
        => db.Queryable<SysConfigData>()
            .InnerJoin<SysConfigType>((data, type) => type.ID == data.ConfigTypeId)
            .Where((data, type) => type.Code == configTypeCode && type.IsDelete == null && data.IsDelete == null)
            .OrderBy((data, type) => data.OrderNo)
            .Select((data, type) => new VdsCatalogRepository.ConfigDataRow { Code = data.Code, Name = data.Name, Value = data.Value });
}
