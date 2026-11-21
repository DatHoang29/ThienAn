using Modules.TMS.Controllers.EquipmentTypeChart.Dto;
using Modules.TMS.Infrastructure.Services.EquipmentTypeChart;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.EquipmentTypeChart.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 19/08/2024
    /// </summary>
    public class EquipmentTypeChartQueryHandler : IWolverineHandler
    {
        private ValidEquipmentRateListService _validEquipmentService;
        private EquipmentStateChartService _stateChartService;
        private BaseCacheService _cache; // ← Thêm dòng này

        public EquipmentTypeChartQueryHandler(ValidEquipmentRateListService validEquipmentService, EquipmentStateChartService stateChartService, BaseCacheService cache)
        {
            this._validEquipmentService = validEquipmentService;
            this._stateChartService = stateChartService;
            this._cache = cache;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 19/08/2024
        /// Description: Danh sách thiết bị cùng tỉ lệ hoạt động
        /// </summary>
        public async Task<SqlSugarPagedList<PageValidEquipmentRateOutput>> HandleAsync(PageValidEquipmentRateInput command)
        {
            return await this._validEquipmentService.ListValidEquipmentRate();
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 19/08/2024
        /// Description: Biểu đồ tỉ lệ trạng thái của thiết bị
        /// </summary>
        public async Task<EquipmentStateChartOutput> HandleAsync(EquipmentStateChartInput command)
        {
            return await this._stateChartService.BuildEquipmentStateChart(command.EquipmentTypeId);
        }
    }
}
