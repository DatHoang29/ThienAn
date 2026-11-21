using System.Collections.Frozen;
using Microsoft.Extensions.Localization;
using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.FMS.Controllers.EquipmentHistory.Dto;
using Modules.TMS.Controllers.Equipment.Dto;
using Modules.TMS.Controllers.EquipmentType.Dto;
using Modules.VMS.Report.Controllers.EquipmentHistory.Dto;
using Modules.VMS.Report.Core.Exceptions;
using Modules.VMS.Report.Core.Helpers;
using Shared.Utility.Utilities;
using SqlSugar;
using Wolverine;
using static Shared.DTO.Enums.BaseEnums;
using EquipmentHistoryOutput = Modules.FMS.Controllers.EquipmentHistory.Dto.EquipmentHistoryOutput;

namespace Modules.VMS.Report.Infrastructure.Services.EquipmentHistory
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Service tổng hợp và chuẩn bị dữ liệu báo cáo lịch sử thiết bị VMS và LCS
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsEquipmentHistoryListService : ITransient
    {
        private List<SysConfigDataOutput> _configDataList = new();
        private FrozenDictionary<string, EquipmentTypeOutput> _equipTypeList = new Dictionary<string, EquipmentTypeOutput>().ToFrozenDictionary();
        private FrozenDictionary<string, EquipmentOutput> _equipList = new Dictionary<string, EquipmentOutput>().ToFrozenDictionary();

        private readonly IMessageBus _messBus;
        private readonly IStringLocalizer _localizer;

        public VmsEquipmentHistoryListService(IStringLocalizer localizer, IMessageBus messBus)
        {
            _localizer = localizer;
            _messBus = messBus;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Truy xuất và xử lý dữ liệu báo cáo EquipmentHistory cho VMS và LCS
        /// Created date: 07/10/2026
        /// </summary>
        public async Task<VmsEquipmentHistoryListReport> BuildEquipmentHistoryListReport(VmsEquipmentHistoryListRenderInput command)
        {
            try
            {
                var reportData = new VmsEquipmentHistoryListReport();

                await LoadDefaultData();
                command.FromDate = BaseUtil.TimeServer.GetTimeServer(command.FromDate);
                command.ToDate = BaseUtil.TimeServer.GetTimeServer(command.ToDate);

                var equipHistoryList = await _messBus.InvokeAsync<List<EquipmentHistoryOutput>>(new EquipmentHistoryInput()
                {
                    EquipmentId = command.EquipmentId,
                    NewState = command.State,
                    FromDate = command.FromDate,
                    ToDate = command.ToDate
                });

                int i = 1;
                var list = equipHistoryList.GroupJoin(
                        _equipList,
                        h => h.EquipmentId,
                        e => e.Key,
                        (h, es) => new { h, equip = es.Select(item => item.Value).FirstOrDefault() }
                    )
                    .WhereIF(
                        !string.IsNullOrEmpty(command.EquipmentTypeId),
                        x => x.equip != null && x.equip.EquipmentTypeId == command.EquipmentTypeId
                    )
                    .Select(x => new VmsEquipmentHistoryData
                    {
                        Idx = (i++).ToString(),
                        EquipmentName = x.equip?.Name,
                        EquipmentType = x.equip != null ? GetEquipmentTypeName(x.equip.EquipmentTypeId) : null,
                        OldStateName = GetConfigDataName(SysConfigTypeCodeEnum.TmsEquipmentState, x.h.OldState),
                        NewStateName = GetConfigDataName(SysConfigTypeCodeEnum.TmsEquipmentState, x.h.NewState),
                        StartDate = x.h.StartTime?.ToString(DateTimeFormatEnum.DateTimeDefault),
                        UpdateDate = x.h.UpdateTime?.ToString(DateTimeFormatEnum.DateTimeDefault),
                    });

                var filteredItems = list?.Where(x => !string.IsNullOrEmpty(x.EquipmentName) && !string.IsNullOrEmpty(x.EquipmentType));
                if (filteredItems != null)
                {
                    reportData.EquipHistoryList.AddRange(filteredItems);
                }

                LocalizeReportColumns(reportData, command);
                SetReportParameters(reportData, command);

                return reportData;
            }
            catch
            {
                return new VmsEquipmentHistoryListReport();
            }
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Cấu hình các tham số hiển thị tiêu đề và thông tin chung trên báo cáo
        /// Created date: 07/10/2026
        /// </summary>
        private void SetReportParameters(VmsEquipmentHistoryListReport reportData, VmsEquipmentHistoryListRenderInput? command = null)
        {
            var dataDate = command?.FromDate != null && command?.ToDate != null
                ? command.FromDate.Value.ToString(DateTimeFormatEnum.DateTimeDefault) + " - " + command.ToDate.Value.ToString(DateTimeFormatEnum.DateTimeDefault)
                : string.Empty;

            reportData.Parameters.DataDate = dataDate;
            reportData.Parameters.ReportDate = DateTime.Now.ToString(DateTimeFormatEnum.DateTimeDefault);
            reportData.Parameters.ReportLogo = RptBaseHelper.LogoHp.GetLogo();
            reportData.Parameters.ReportSign = GetReportSign();

            reportData.Parameters.ReportTitle = _localizer[BaseMsg.RptEquipmentHistory.Label.RptTitle].Value.ToUpper();
            reportData.Parameters.PageLabel = _localizer[BaseMsg.RptBase.Label.Page].Value;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Đa ngôn ngữ tiêu đề các cột của báo cáo
        /// Created date: 07/10/2026
        /// </summary>
        private void LocalizeReportColumns(VmsEquipmentHistoryListReport reportData, VmsEquipmentHistoryListRenderInput? command = null)
        {
            reportData.Columns.Idx = reportData.Parameters.ReportTitle = _localizer[BaseMsg.RptBase.Label.Idx].Value.ToUpper();
            reportData.Columns.Name = _localizer[BaseMsg.RptEquipmentHistory.Entity.Name].Value.ToUpper();
            reportData.Columns.EquipmentType = _localizer[BaseMsg.RptEquipmentHistory.Entity.EquipmentType].Value.ToUpper();
            reportData.Columns.OldState = _localizer[BaseMsg.RptEquipmentHistory.Entity.OldState].Value.ToUpper();
            reportData.Columns.NewState = _localizer[BaseMsg.RptEquipmentHistory.Entity.NewState].Value.ToUpper();
            reportData.Columns.DateTime = _localizer[BaseMsg.RptBase.Label.Time].Value.ToUpper();
            reportData.Columns.Remark = _localizer[BaseMsg.RptEquipmentHistory.Entity.Remark].Value.ToUpper();
        }

        private string GetReportSign()
        {
            return "wwwroot/upload/sign.png";
        }

        private string GetEquipmentTypeName(string? equipmentTypeId)
        {
            if (_equipTypeList.Count > 0 && equipmentTypeId != null && _equipTypeList.ContainsKey(equipmentTypeId))
                return _equipTypeList[equipmentTypeId]?.Name ?? string.Empty;
            return string.Empty;
        }

        private string GetConfigDataName(string configTypeCode, string? configDataCode)
        {
            return _configDataList?.FirstOrDefault(config =>
                    config.ConfigTypeCode != null
                    && config.Code != null
                    && config.ConfigTypeCode.Equals(configTypeCode, StringComparison.OrdinalIgnoreCase)
                    && config.Code.Equals(configDataCode, StringComparison.OrdinalIgnoreCase)
                )?.Name ?? string.Empty;
        }

        private async Task LoadDefaultData()
        {
            try
            {
                var configDataList = await _messBus.InvokeAsync<List<SysConfigDataOutput>>(new SysConfigDataInput());
                _configDataList = configDataList ?? new List<SysConfigDataOutput>();

                var equipmentTypeList = await _messBus.InvokeAsync<List<EquipmentTypeOutput>>(new EquipmentTypeInput());
                _equipTypeList = (equipmentTypeList ?? new List<EquipmentTypeOutput>())
                    .ToFrozenDictionary(x => x.ID);

                var equipmentList = await _messBus.InvokeAsync<List<EquipmentOutput>>(new EquipmentInput());
                _equipList = (equipmentList ?? new List<EquipmentOutput>())
                    .ToFrozenDictionary(x => x.ID);
            }
            catch { }
        }
    }
}
