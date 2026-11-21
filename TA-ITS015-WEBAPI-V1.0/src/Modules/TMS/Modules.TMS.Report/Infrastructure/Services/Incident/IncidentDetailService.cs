using Shared.Utility.Utilities;
using Mapster;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Constants;
using Microsoft.Extensions.Localization;
using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.System.Controllers.Org.Dto;
using Modules.System.Core.Entities;
using Modules.TMS.Controllers.TrafficPlan.Dto;
using Modules.TMS.Controllers.Zone.Dto;
using Modules.TMS.Report.Controllers.Incident.Dto;
using Modules.TMS.Report.Core.Exceptions;
using Shared.DTO.Dtos.Tms;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.Tms;
using Wolverine;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Report.Infrastructure.Services.Incident
{
    public class IncidentDetailService
    {
        private readonly IMessageBus _messBus;
        private List<SysConfigDataOutput> _configDataList = new();
        private List<TrafficPlanOutput> _trafficPlanList = new();
        private List<ZoneOutput> _zoneList = new();
        private List<SysOrg> _orgList = new();

        public IncidentDetailService(IMessageBus messBus)
        {
            this._messBus = messBus;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 05/12/2024
        /// Description: List the incident and workexecute based on provided parameters
        /// </summary>
        public async Task<IncidentDetailReport> BuildIncidentDetailReport(IncidentDetailRenderInput command, IStringLocalizer localizer)
        {
            var reportData = new IncidentDetailReport() { };

            // Load danh sách cấu hình, tổ chức...
            await this.LoadDefaultData();

            var incidentQuery = new TmsIncidentSingleInput() { Id = command.IncidentId };
            var incident = await _messBus.InvokeAsync<Modules.TMS.Core.Dto.Incident.TmsIncidentOutput>(incidentQuery);




            if (incident == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            var workExecList = this.IsIncidentFinished(incident.State)
                ? (await _messBus.InvokeAsync<List<Modules.TMS.Controllers.WorkExecuteHistory.Dto.TmsWorkExecuteHistoryOutput>>(
                    new Modules.TMS.Controllers.WorkExecuteHistory.Dto.TmsWorkExecuteHistoryParaInput { IncidentId = command.IncidentId }))
                    .Adapt<List<TmsWorkExecuteResponse>>()
                : (await _messBus.InvokeAsync<List<Modules.TMS.Core.Dto.WorkExecute.TmsWorkExecuteOutput>>(
                    new Modules.TMS.Core.Dto.WorkExecute.TmsWorkExecuteParaInput { IncidentId = command.IncidentId }))
                    .Adapt<List<TmsWorkExecuteResponse>>();

            workExecList?.ForEach(workExec =>
            {
                workExec.StateName = this.GetConfigDataName(SysConfigTypeCodeEnum.TmsWorkexecuteStatus, workExec.State);
                workExec.ExecuteOrgName = this.GetOrgName(workExec.ExecuteOrg);
            });

            if (workExecList?.Count == 0)
            {
                workExecList = new List<TmsWorkExecuteResponse>(4)
                {
                    new TmsWorkExecuteResponse(), new TmsWorkExecuteResponse(), new TmsWorkExecuteResponse(),
                    new TmsWorkExecuteResponse()
                };

            }

            reportData.Incident = new IncidentOutput()
            {
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
                Position = $"Km{incident.KmNumber ?? 0} +{incident.MetNumber ?? 0}",
                TrafficPlan = this.GetTrafficPlanName(incident.TrafficPlanId),
                WorkExecuteList = workExecList ?? new List<TmsWorkExecuteResponse>(),
                Metadata = incident.Metadata,
                Description = incident.Description,
                Information = IncidentReportInformation.Build(incident.Metadata, incident.Description, localizer),
                InfluenceScope = this.GetConfigDataName(SysConfigTypeCodeEnum.TmsDirectionConfigCode, incident.InfluenceScope)
            };



            this.LocalizeReportColumns(reportData, localizer, command);

            this.SetReportParameters(reportData, localizer, command);

            return reportData;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 05/12/2024
        /// Description: Set dữ liệu cho tham số ra
        /// </summary>
        private void SetReportParameters(IncidentDetailReport reportData, IStringLocalizer localizer, IncidentDetailRenderInput command = null)
        {
            reportData.Parameters.DataDate = string.Empty;
            reportData.Parameters.ReportDate = BaseUtil.TimeServer.GetTimeServer().ToString(DateTimeFormatEnum.DateTimeDefault);
            reportData.Parameters.ReportLogo = this.GetReportLogo();
            reportData.Parameters.ReportSign = this.GetReportSign();

            // Dịch thuật tham số
            reportData.Parameters.ReportTitle = localizer[BaseMsg.RptIncidentDetail.Label.RptTitle].Value.ToUpper();
            reportData.Parameters.IncidentSectionTitle = localizer[BaseMsg.RptIncidentDetail.Label.RptIncidentTitle].Value.ToUpper();
            reportData.Parameters.WorkExecSectionTitle = localizer[BaseMsg.RptIncidentDetail.Label.RptWorkExecuteTitle].Value.ToUpper();
            reportData.Parameters.PageLabel = localizer[BaseMsg.RptBase.Label.Page].Value;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 05/12/2024
        /// Description: Dịch thuật tiêu đề cột
        /// </summary>
        private void LocalizeReportColumns(IncidentDetailReport reportData, IStringLocalizer localizer, IncidentDetailRenderInput command = null)
        {
            reportData.IncidentColumns.Idx = localizer[BaseMsg.RptBase.Label.Idx].Value.ToUpper();
            reportData.IncidentColumns.Name = localizer[BaseMsg.RptIncidentDetail.Entity.Name].Value.ToUpper();
            reportData.IncidentColumns.Code = localizer[BaseMsg.RptIncidentDetail.Entity.Code].Value.ToUpper();
            reportData.IncidentColumns.StartDate = localizer[BaseMsg.RptIncidentDetail.Entity.StartDate].Value.ToUpper();
            reportData.IncidentColumns.FinishedDate = localizer[BaseMsg.RptIncidentDetail.Entity.FinishedDate].Value.ToUpper();
            reportData.IncidentColumns.EventType = localizer[BaseMsg.RptIncidentDetail.Entity.EventType].Value.ToUpper();
            reportData.IncidentColumns.Position = localizer[BaseMsg.RptIncidentDetail.Entity.Position].Value.ToUpper();
            reportData.IncidentColumns.Zone = localizer[BaseMsg.RptIncidentDetail.Entity.Zone].Value.ToUpper();
            reportData.IncidentColumns.Source = localizer[BaseMsg.RptIncidentDetail.Entity.Source].Value.ToUpper();
            reportData.IncidentColumns.TrafficPlan = localizer[BaseMsg.RptIncidentDetail.Entity.TrafficPlan].Value.ToUpper();
            reportData.IncidentColumns.Information = localizer[BaseMsg.RptIncidentDetail.Label.Information].Value.ToUpper();
            reportData.IncidentColumns.Direction = localizer[Modules.TMS.Core.Exceptions.BaseMsg.TmsIncident.Entity.InfluenceScope].Value.ToUpper();
            reportData.IncidentColumns.State = localizer[BaseMsg.RptIncidentDetail.Entity.State].Value.ToUpper();

            reportData.WorkExecuteColumns.Step = localizer[BaseMsg.RptIncidentDetail.Entity.WorkStep].Value.ToUpper();
            reportData.WorkExecuteColumns.Name = localizer[BaseMsg.RptIncidentDetail.Entity.WorkName].Value.ToUpper();
            reportData.WorkExecuteColumns.State = localizer[BaseMsg.RptIncidentDetail.Entity.WorkState].Value.ToUpper();
            reportData.WorkExecuteColumns.ExecuteOrg = localizer[BaseMsg.RptIncidentDetail.Entity.WorkExecuteOrg].Value.ToUpper();
        }

        private string GetReportLogo()
        {
            return "wwwroot/upload/logo.png";
        }

        private string GetReportSign()
        {
            return "wwwroot/upload/sign.png";
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 05/12/2024
        /// Description: Xác định incident đã kết thúc hay chưa
        /// </summary>
        private bool IsIncidentFinished(string? incidentState)
        {
            if(string.IsNullOrWhiteSpace(incidentState))
                return false;

            return incidentState.Equals(((int)TmsIncidentStateEnum.Skipped).ToString(), StringComparison.OrdinalIgnoreCase)
                || incidentState.Equals(((int)TmsIncidentStateEnum.Canceled).ToString(), StringComparison.OrdinalIgnoreCase)
                || incidentState.Equals(((int)TmsIncidentStateEnum.Finished).ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 05/12/2024
        /// Description: Lấy tên zone theo id
        /// </summary>
        private string GetZoneName(string? zoneId)
        {
            if (zoneId == null)
                return string.Empty;

            var zoneList = this._zoneList?.Where(zone => zoneId.IndexOf(zone.ID) != -1).ToList();

            return zoneList?.Count() > 0 ? string.Join(", ", zoneList.Select(zone => zone.Name).ToArray()) : string.Empty;
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
        /// Description: Lấy tên cấu hình thông qua code
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
        /// Description: Lấy tên cấu hình thông qua code
        /// </summary>
        private string GetOrgName(string? orgId)
        {
            return this._orgList?.FirstOrDefault(org => org.ID == orgId)?.Name ?? orgId ?? string.Empty;
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 05/12/2024
        /// Description: Đệ quy biến đổi danh sách org dạng tree thành list
        /// </summary>
        private void RecursivelyOrgList(List<SysOrg> orgList, int depth = 0)
        {
            // Recursively up to a depth of 10
            if (depth > 10 || orgList == null)
                return;

            if(this._orgList == null)
                this._orgList = new List<SysOrg>();

            orgList?.ForEach(org => {
                if (!this._orgList.Any(existOrg => existOrg.ID.Equals(org.ID, StringComparison.OrdinalIgnoreCase)))
                    this._orgList.Add(org);

                if (org?.Children?.Count() > 0)
                    this.RecursivelyOrgList(org.Children, depth + 1);
            });
        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 05/12/2024
        /// Description: Load dữ liệu mặc định cần thiết
        /// </summary>
        /// <returns></returns>
        private async Task LoadDefaultData()
        {
            // Fetch the org list
            var orgList = await this._messBus.InvokeAsync<List<SysOrg>>(new OrgInput() { ID = "0" });
            if (orgList?.Count() > 0)
                this.RecursivelyOrgList(orgList);

            // Fetch the zone list
            this._zoneList = await this._messBus.InvokeAsync<List<ZoneOutput>>(new ZoneInput())
                ?? new List<ZoneOutput>();

            // Fetch the configData list
            this._configDataList = await this._messBus.InvokeAsync<List<SysConfigDataOutput>>(new SysConfigDataInput())
                ?? new List<SysConfigDataOutput>();

            // Fetch the trafficPlan list
            this._trafficPlanList = await this._messBus.InvokeAsync<List<TrafficPlanOutput>>(new TrafficPlanInput())
                ?? new List<TrafficPlanOutput>();
        }
    }
}
