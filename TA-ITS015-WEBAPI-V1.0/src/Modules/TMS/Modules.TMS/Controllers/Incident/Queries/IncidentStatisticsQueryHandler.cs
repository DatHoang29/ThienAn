using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Infrastructure.Services.Incident;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Utility.Utilities;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Controllers.Incident.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Description: Handler thống kê sự cố: IncidentChart / CountIncident.
    /// Tách từ IncidentQueryHandler; tái sử dụng truy vấn Page qua message bus.
    /// </summary>
    public class IncidentStatisticsQueryHandler : IWolverineHandler
    {
        private IMessageBus _messageBus;
        private IncidentChartService _chartService;

        public IncidentStatisticsQueryHandler(IMessageBus messageBus, IncidentChartService chartService)
        {
            this._messageBus = messageBus;
            this._chartService = chartService;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Biểu đồ sự cố
        /// </summary>
        public async Task<IncidentChartOutput> HandleAsync(IncidentChartInput input)
        {
            var incidentChart = new IncidentChartOutput();

            if (input.ChartDataType == "day")
            {
                var serverNow = BaseUtil.TimeServer.GetTimeServer();
                var fromDate = serverNow.Date;
                var toDate = serverNow;

                input.FromDate = fromDate;
                input.ToDate = toDate;

                var incidentList = await GetIncidentListAsync(input);

                incidentChart = this._chartService.BuildIncidentChart(incidentList, fromDate, toDate, input.ChartDataType);
            }
            else
            {
                var incidentList = await GetIncidentListAsync(input);

                var serverNow = BaseUtil.TimeServer.GetTimeServer();
                var fromDate = input.FromDate != null ? input.FromDate.Value : (incidentList?.Min(item => item.StartDate) ?? serverNow);
                var toDate = input.ToDate != null ? input.ToDate.Value : (incidentList?.Max(item => item.StartDate) ?? serverNow);

                incidentChart = this._chartService.BuildIncidentChart(incidentList, fromDate, toDate, input.ChartDataType);
            }

            return incidentChart;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Đếm số lượng sự cố
        /// </summary>
        public async Task<IncidentCountOutput> HandleAsync(IncidentCountInput input)
        {
            var summary = new IncidentCountOutput();

            if (input.ChartDataType == "day")
            {
                var serverNow = BaseUtil.TimeServer.GetTimeServer();
                input.FromDate = serverNow.Date;
                input.ToDate = serverNow;
            }

            var incidentList = await GetIncidentListAsync(input);

            summary.Total = incidentList?.Count() ?? 0;
            summary.New = incidentList?.Where(item => item.State == ((int)TmsIncidentStateEnum.Registration).ToString()).Count() ?? 0;
            summary.InValid = incidentList?.Where(item => item.State == ((int)TmsIncidentStateEnum.Canceled).ToString() || item.State == ((int)TmsIncidentStateEnum.Skipped).ToString()).Count() ?? 0;
            summary.Valid = incidentList?.Where(item => item.State == ((int)TmsIncidentStateEnum.Verified).ToString() || item.State == ((int)TmsIncidentStateEnum.InProgress).ToString() || item.State == ((int)TmsIncidentStateEnum.Finished).ToString()).Count() ?? 0;

            return summary;
        }

        /// <summary>
        /// Lấy danh sách sự cố theo filter (tái sử dụng truy vấn Page của CRUD query handler qua message bus).
        /// </summary>
        private async Task<List<PageTmsIncidentOutput>> GetIncidentListAsync(PageTmsIncidentInput input)
        {
            // Adapt sang ChartTmsIncidentPageInput — kiểu message NỘI BỘ để message bus định tuyến tới
            // nhánh "được vượt trần PageSize và bỏ COUNT" của IncidentQueryHandler. Dùng kiểu riêng thay vì
            // đặt cờ trên DTO công khai vì cờ đó client cũng đặt được.
            var pageQuery = input.Adapt<ChartTmsIncidentPageInput>();

            var dataList = await this._messageBus.InvokeAsync<TmsIncidentPageOutput>(pageQuery);

            // Bản trùng bị chống trùng loại không phải sự cố thật: giữ ngoài biểu đồ và mọi ô đếm để
            // Total vẫn bằng New + Valid + InValid. Trang quản lý vẫn tra cứu được vì không dùng hàm này.
            return dataList?.Records
                .Where(item => !IncidentRecordingConst.IsDuplicate(item.Metadata))
                .ToList();
        }
    }
}
