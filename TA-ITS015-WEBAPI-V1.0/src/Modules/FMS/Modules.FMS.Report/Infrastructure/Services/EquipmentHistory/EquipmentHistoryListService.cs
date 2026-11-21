using System.Collections.Frozen;
using Microsoft.Extensions.Localization;
using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.FMS.Controllers.EquipmentHistory.Dto;
using Modules.FMS.Report.Controllers.EquipmentHistory.Dto;
using Modules.FMS.Report.Core.Exceptions;
using Modules.FMS.Report.Core.Helpers;
using Modules.TMS.Controllers.Equipment.Dto;
using Modules.TMS.Controllers.EquipmentType.Dto;
using Shared.Utility.Utilities;
using SqlSugar;
using Wolverine;
using static Shared.DTO.Enums.BaseEnums;
using EquipmentHistoryOutput = Modules.FMS.Controllers.EquipmentHistory.Dto.EquipmentHistoryOutput;

namespace Modules.FMS.Report.Infrastructure.Services.EquipmentHistory
{
    public class EquipmentHistoryListService
    {
        private List<SysConfigDataOutput> _configDataList = new();
        private FrozenDictionary<string, EquipmentTypeOutput> _equipTypeList = new Dictionary<string, EquipmentTypeOutput>().ToFrozenDictionary();
        private FrozenDictionary<string, EquipmentOutput> _equipList = new Dictionary<string, EquipmentOutput>().ToFrozenDictionary();

        IMessageBus _messBus;
        private IStringLocalizer _localizer;

        public EquipmentHistoryListService(IStringLocalizer localizer,IMessageBus messBus)
        {
            this._localizer = localizer;
            _messBus = messBus;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Truy xuất và xử lý dữ liệu báo cáo EquipmentHistory
        /// Create Date: 04/11/2024
        /// </summary>
        public async Task<EquipmentHistoryListReport> BuildEquipmentHistoryListReport(EquipmentHistoryListRenderInput command)
        {
            try
            {
                var reportData = new EquipmentHistoryListReport() { };

                await this.LoadDefaultData();
                command.FromDate = BaseUtil.TimeServer.GetTimeServer(command.FromDate);
                command.ToDate = BaseUtil.TimeServer.GetTimeServer(command.ToDate);

                #region API Method

                //var query = new FmsEquipmentHistoryListRequest()
                //{
                //    EquipmentId = command.EquipmentId,
                //    EquipmentTypeId = command.EquipmentTypeId,
                //    NewState = command.State,
                //    FromDate = command.FromDate,
                //    ToDate = command.ToDate
                //};
                //var response = await BaseApiClient<IEquipmentHistoryApi>.Client().List(query);

                //// TODO: Cache: where to cache?
                //var equipHistoryList = response.Content?.Result;


                #endregion


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
                    .Select(x => new EquipmentHistoryData
                    {
                        Idx = (i++).ToString(),
                        EquipmentName = x.equip?.Name,
                        EquipmentType = x.equip != null ? this.GetEquipmentTypeName(x.equip.EquipmentTypeId) : null,
                        OldStateName = this.GetConfigDataName(SysConfigTypeCodeEnum.TmsEquipmentState, x.h.OldState),
                        NewStateName = this.GetConfigDataName(SysConfigTypeCodeEnum.TmsEquipmentState, x.h.NewState),
                        StartDate = x.h.StartTime?.ToString(DateTimeFormatEnum.DateTimeDefault),
                        UpdateDate = x.h.UpdateTime?.ToString(DateTimeFormatEnum.DateTimeDefault),
                        //Remark = x.h.Remark
                    });

                reportData.EquipHistoryList.AddRange(list?.Where(x => !string.IsNullOrEmpty(x.EquipmentName) && !string.IsNullOrEmpty(x.EquipmentType)));
                //var idx = 0;
                //equipHistoryList?.ForEach(equip =>
                //{
                //    var equipmentInfo = this.GetEquipmentInfo(equip.EquipmentId);
                //    reportData.EquipHistoryList.Add(new EquipmentHistoryData()
                //    {
                //        //Idx = (++idx).ToString().PadLeft(2, '0'),
                //        EquipmentName = equipmentInfo?.Name,
                //        EquipmentType = this.GetEquipmentTypeName(equipmentInfo?.EquipmentTypeId),
                //        OldStateName = this.GetConfigDataName(SysConfigTypeCodeEnum.TmsEquipmentState, equip.OldState),
                //        NewStateName = this.GetConfigDataName(SysConfigTypeCodeEnum.TmsEquipmentState, equip.NewState),
                //        StartDate = equip.StartTime?.ToString(DateTimeFormatEnum.DateTimeDefault),
                //        UpdateDate = equip.UpdateTime?.ToString(DateTimeFormatEnum.DateTimeDefault),
                //        Remark = equip.Remark
                //    });
                //});

                this.LocalizeReportColumns(reportData, command);

                this.SetReportParameters(reportData, command);

                return reportData;
            }
            catch
            {
                return new EquipmentHistoryListReport(); // Empty report
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Set dữ liệu cho tham số ra
        /// Create Date: 04/11/2024
        /// </summary>
        private void SetReportParameters(EquipmentHistoryListReport reportData, EquipmentHistoryListRenderInput command = null)
        {
            var dataDate = command?.FromDate != null && command?.ToDate != null
            ? command.FromDate.Value.ToString(DateTimeFormatEnum.DateTimeDefault) + " - " + command.ToDate.Value.ToString(DateTimeFormatEnum.DateTimeDefault)
            : string.Empty;

            reportData.Parameters.DataDate = dataDate;
            reportData.Parameters.ReportDate = DateTime.Now.ToString(DateTimeFormatEnum.DateTimeDefault);
            reportData.Parameters.ReportLogo = RptBaseHelper.LogoHp.GetLogo();
            reportData.Parameters.ReportSign = this.GetReportSign();

            // Dịch thuật tham số
            //"LỊCH SỬ BIẾN ĐỘNG TRẠNG THÁI CỦA THIẾT BỊ"
            reportData.Parameters.ReportTitle = this._localizer[BaseMsg.RptEquipmentHistory.Label.RptTitle].Value.ToUpper();
            reportData.Parameters.PageLabel = this._localizer[BaseMsg.RptBase.Label.Page].Value;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Dịch thuật tiêu đề cột
        /// Create Date: 04/11/2024
        /// </summary>
        private void LocalizeReportColumns(EquipmentHistoryListReport reportData, EquipmentHistoryListRenderInput command = null)
        {
            reportData.Columns.Idx = reportData.Parameters.ReportTitle = this._localizer[BaseMsg.RptBase.Label.Idx].Value.ToUpper(); //"STT";
            reportData.Columns.Name = this._localizer[BaseMsg.RptEquipmentHistory.Entity.Name].Value.ToUpper(); //"TÊN THIẾT BỊ";
            reportData.Columns.EquipmentType = this._localizer[BaseMsg.RptEquipmentHistory.Entity.EquipmentType].Value.ToUpper(); //"LOẠI THIẾT BỊ";
            reportData.Columns.OldState = this._localizer[BaseMsg.RptEquipmentHistory.Entity.OldState].Value.ToUpper(); //"TRƯỚC TÁC ĐỘNG";
            reportData.Columns.NewState = this._localizer[BaseMsg.RptEquipmentHistory.Entity.NewState].Value.ToUpper(); // "SAU TÁC ĐỘNG";
            reportData.Columns.DateTime = this._localizer[BaseMsg.RptBase.Label.Time].Value.ToUpper(); // "THỜI GIAN";
            reportData.Columns.Remark = this._localizer[BaseMsg.RptEquipmentHistory.Entity.Remark].Value.ToUpper(); // "GHI CHÚ";
        }

       

        private string GetReportSign()
        {
            return "wwwroot/upload/sign.png";
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy tên EquipmentType theo id
        /// </summary>
        private string GetEquipmentTypeName(string? equipmentTypeId)
        {
            //return this._equipmentTypeList?.FirstOrDefault(equipType => equipType.ID == equipmentTypeId)?.Name ?? string.Empty;

            if(_equipTypeList.Count >0 && _equipTypeList.ContainsKey(equipmentTypeId))
                return _equipTypeList[equipmentTypeId]?.Name ?? string.Empty;
            return "";
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy tên Equipment theo id
        /// </summary>
        private EquipmentOutput? GetEquipmentInfo(string? equipmentId)
        {
            if(_equipList.Count >0 && _equipList.ContainsKey(equipmentId))
                return _equipList[equipmentId];
            return new EquipmentOutput();
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy tên cấu hình thông qua code
        /// </summary>
        private string GetConfigDataName(string configTypeCode, string? configDataCode)
        {
            return this._configDataList?.FirstOrDefault(config =>
                    config.ConfigTypeCode != null
                    && config.Code != null
                    && config.ConfigTypeCode.Equals(configTypeCode, StringComparison.OrdinalIgnoreCase)
                    && config.Code.Equals(configDataCode, StringComparison.OrdinalIgnoreCase)
                )?.Name ?? string.Empty;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Load dữ liệu mặc định cần thiết
        /// </summary>
        /// <returns></returns>
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
            catch { } // Không ném lỗi => Tiếp tục thực hiện task khác
        }
    }
}