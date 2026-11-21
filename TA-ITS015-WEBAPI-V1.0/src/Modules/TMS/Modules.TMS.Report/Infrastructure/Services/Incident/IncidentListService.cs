using Mapster;
using Wolverine;
using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.TMS.Controllers.Zone.Dto;
using Modules.TMS.Controllers.TrafficPlan.Dto;
using Modules.TMS.Core.Dto.Incident;
using Microsoft.Extensions.Localization;
using Modules.TMS.Report.Controllers.Incident.Dto;
using Modules.TMS.Report.Core.Exceptions;
using Modules.TMS.Report.Core.Helpers;
using Shared.DTO.Dtos.System;
using Shared.DTO.Dtos.Tms;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.System;
using Shared.Utility.Apis.Tms;
using Shared.Utility.Utilities;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Report.Infrastructure.Services.Incident
{
    public class IncidentListService
    {
        private List<SysConfigDataOutput> _configDataList;
        private List<TrafficPlanOutput> _trafficPlanList;
        private List<ZoneOutput> _zoneList;
        private readonly IMessageBus _messBus;
        public IncidentListService(IMessageBus messBus) => _messBus = messBus;

        /// <summary>
        /// Author: ThangNN
        /// Created date: 05/12/2024
        /// Description: List the incidents based on provided parameters
        /// </summary>
        public async Task<IncidentListReport> BuildIncidentListReport(IncidentListRenderInput command, IStringLocalizer localizer)
        {
            var reportData = new IncidentListReport() { };

            // Load danh sách cấu hình, tổ chức...
            await this.LoadDefaultData();
            command.FromDate = BaseUtil.TimeServer.GetTimeServer(command.FromDate);
            command.ToDate = BaseUtil.TimeServer.GetTimeServer(command.ToDate);


            var incidentListQuery = new TmsIncidentInput() {
                EventTypeId = command.EventTypeId,
                Source = command.Source,
                FromDate = command.FromDate,
                ToDate = command.ToDate
            };
            var incidentList = await _messBus.InvokeAsync<List<Modules.TMS.Core.Dto.Incident.TmsIncidentOutput>>(incidentListQuery);




            var idx = 0;
            incidentList?.ForEach(incident =>
            {
                reportData.IncidentList.Add(new IncidentOutput()
                {
                    //Idx = (++idx).ToString().PadLeft(2, '0'),
                    Code = incident.Code,
                    Name = incident.Name,
                    EventTypeName = incident.EventTypeName,
                    Zone = this.GetZoneName(incident.ZoneId),
                    BeginDate = incident.StartDate?.ToString(DateTimeFormatEnum.DateDefault),
                    BeginTime = incident.StartDate?.ToString(DateTimeFormatEnum.TimeDefault),
                    EndDate = incident.FinishedDate?.ToString(DateTimeFormatEnum.DateDefault),
                    EndTime = incident.FinishedDate?.ToString(DateTimeFormatEnum.TimeDefault),
                    StateName = this.GetConfigDataName(SysConfigTypeCodeEnum.TmsIncidentStatus, incident.State),
                    Source = this.GetConfigDataName(SysConfigTypeCodeEnum.TmsSourceDetectIncident, incident.Source),
                    Position = $"{incident.KmNumber ?? 0} Km +{incident.MetNumber ?? 0}",
                    TrafficPlan = this.GetTrafficPlanName(incident.TrafficPlanId)
                });
            });
            if (reportData.IncidentList.Count == 0)
            {
                reportData.IncidentList.Add(new IncidentOutput());
                reportData.IncidentList.Add(new IncidentOutput());
                reportData.IncidentList.Add(new IncidentOutput());
                reportData.IncidentList.Add(new IncidentOutput());
                reportData.IncidentList.Add(new IncidentOutput());
                reportData.IncidentList.Add(new IncidentOutput());
                reportData.IncidentList.Add(new IncidentOutput());
                reportData.IncidentList.Add(new IncidentOutput());

            }

            this.LocalizeReportColumns(reportData, localizer, command);

            this.SetReportParameters(reportData, localizer, command);

            return reportData;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 05/12/2024
        /// Description: Set dữ liệu cho tham số ra
        /// </summary>
        private void SetReportParameters(IncidentListReport reportData, IStringLocalizer localizer, IncidentListRenderInput command = null)
        {
            var dataDate = command?.FromDate != null && command?.ToDate != null
            ? command.FromDate.Value.ToString(DateTimeFormatEnum.DateTimeDefault) + " - " + command.ToDate.Value.ToString(DateTimeFormatEnum.DateTimeDefault)
            : string.Empty;

            reportData.Parameters.DataDate = dataDate;
            reportData.Parameters.ReportDate = BaseUtil.TimeServer.GetTimeServer().ToString(DateTimeFormatEnum.DateTimeDefault);
            reportData.Parameters.ReportLogo = RptBaseHelper.LogoHp.GetLogo();
            reportData.Parameters.ReportSign = this.GetReportSign();

            // Dịch thuật tham số
            reportData.Parameters.ReportTitle = localizer[BaseMsg.RptIncidentList.Label.RptTitle].Value.ToUpper();
            reportData.Parameters.PageLabel = localizer[BaseMsg.RptBase.Label.Page].Value;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 05/12/2024
        /// Description: Dịch thuật tiêu đề cột
        /// </summary>
        private void LocalizeReportColumns(IncidentListReport reportData, IStringLocalizer localizer, IncidentListRenderInput command = null)
        {
            reportData.Columns.Idx = localizer[BaseMsg.RptBase.Label.Idx].Value.ToUpper();
            reportData.Columns.Name = localizer[BaseMsg.RptIncidentDetail.Entity.Name].Value.ToUpper();
            reportData.Columns.Code = localizer[BaseMsg.RptIncidentDetail.Entity.Code].Value.ToUpper();
            reportData.Columns.StartDate = localizer[BaseMsg.RptIncidentDetail.Entity.StartDate].Value.ToUpper();
            reportData.Columns.FinishedDate = localizer[BaseMsg.RptIncidentDetail.Entity.FinishedDate].Value.ToUpper();
            reportData.Columns.EventType = localizer[BaseMsg.RptIncidentDetail.Entity.EventType].Value.ToUpper();
            reportData.Columns.Position = localizer[BaseMsg.RptIncidentDetail.Entity.Position].Value.ToUpper();
            reportData.Columns.Source = localizer[BaseMsg.RptIncidentDetail.Entity.Source].Value.ToUpper();
            reportData.Columns.TrafficPlan = localizer[BaseMsg.RptIncidentDetail.Entity.TrafficPlan].Value.ToUpper();
            reportData.Columns.State = localizer[BaseMsg.RptIncidentDetail.Entity.State].Value.ToUpper();
        }
        private string GetReportSign()
        {
            return "wwwroot/upload/sign.png";
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 05/12/2024
        /// Description: Lấy tên zone theo id
        /// </summary>
        private string GetZoneName(string? zoneId)
        {
            var ids = (zoneId ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return string.Join(", ", (_zoneList ?? []).Where(zone => ids.Contains(zone.ID)).Select(zone => zone.Name));
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 05/12/2024
        /// Description: Lấy tên trafficPlan theo id
        /// </summary>
        private string GetTrafficPlanName(string? trafficPlanId)
        {
            return this._trafficPlanList?.FirstOrDefault(plan => plan.ID == trafficPlanId)?.Name ?? trafficPlanId ?? string.Empty;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 05/12/2024
        /// Description: Lấy tên cấu hình ti62 anhông qua code
        /// </summary>
        private string GetConfigDataName(string configTypeCode, string? configDataCode)
        {
            return this._configDataList?.FirstOrDefault(config =>
                    config.ConfigTypeCode != null
                    && config.Code != null
                    && config.ConfigTypeCode.Equals(configTypeCode, StringComparison.OrdinalIgnoreCase)
                    && config.Code.Equals(configDataCode, StringComparison.OrdinalIgnoreCase)
                )?.Name ?? configDataCode ?? string.Empty;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 05/12/2024
        /// Description: Load dữ liệu mặc định cần thiết
        /// </summary>
        /// <returns></returns>
        private async Task LoadDefaultData()
        {
            _zoneList = await _messBus.InvokeAsync<List<ZoneOutput>>(new ZoneInput()) ?? [];
            _configDataList = await _messBus.InvokeAsync<List<SysConfigDataOutput>>(new SysConfigDataInput()) ?? [];
            _trafficPlanList = await _messBus.InvokeAsync<List<TrafficPlanOutput>>(new TrafficPlanInput()) ?? [];
        }
    }
}
