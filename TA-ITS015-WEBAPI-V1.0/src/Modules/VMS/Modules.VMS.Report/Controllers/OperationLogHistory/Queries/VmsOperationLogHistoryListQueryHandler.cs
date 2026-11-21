using System.Collections.Frozen;
using Microsoft.Extensions.Localization;
using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.Report.Base.Controllers.Management.Dto;
using Modules.TMS.Controllers.Equipment.Dto;
using Modules.TMS.Controllers.Module.Dto;
using Modules.TMS.Controllers.SignalLogHistory.Dto;
using Modules.VMS.Report.Controllers.OperationLogHistory.Dto;
using Modules.VMS.Report.Core.Exceptions;
using Modules.VMS.Report.Core.Helpers;
using Furion;
using Shared.DTO.Enums;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Utility.Utilities;
using Wolverine;

namespace Modules.VMS.Report.Controllers.OperationLogHistory.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Handler xử lý truy vấn và tạo báo cáo lịch sử điều khiển thiết bị VMS/LCS
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsOperationLogHistoryListQueryHandler : IWolverineHandler
    {
        private readonly IMessageBus _messBus;
        private readonly IStringLocalizer _localizer;

        // Template FastReport mới được tạo cho chức năng này
        private const string reportCode = "VmsOperationLogHistoryListReport";

        public VmsOperationLogHistoryListQueryHandler(IMessageBus messBus, IStringLocalizer localizer)
        {
            _messBus = messBus;
            _localizer = localizer;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Render báo cáo danh sách lịch sử điều khiển thiết bị
        /// Created date: 07/10/2026
        /// </summary>
        public async Task<FileStreamResult> HandleAsync(VmsOperationLogHistoryListRenderInput command)
        {
            var data = await BuildOperationLogHistoryListReport(command);

            // Lưu dữ liệu cache report
            await _messBus.InvokeAsync<bool>(new SaveDataReportInput()
            {
                ReportDataId = command.ReportDataId,
                Data = data
            });

            // Render report qua FastReport
            var report = await _messBus.InvokeAsync<FileStreamResult>(new RenderReportInput()
            {
                Code = reportCode,
                Data = data
            });

            return report;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Xuất file báo cáo danh sách lịch sử điều khiển thiết bị (PDF/Excel)
        /// Created date: 07/10/2026
        /// </summary>
        public async Task<FileStreamResult> HandleAsync(VmsOperationLogHistoryListExportInput command)
        {
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });

            VmsOperationLogHistoryListReport? data = null;
            if (!string.IsNullOrEmpty(dataStr))
            {
                data = JsonConvert.DeserializeObject<VmsOperationLogHistoryListReport>(dataStr);
            }

            if (data == null)
            {
                data = await BuildOperationLogHistoryListReport(command);
            }

            var report = await _messBus.InvokeAsync<FileStreamResult>(new ExportReportInput()
            {
                Code = reportCode,
                Data = data,
                Format = command.Format ?? "pdf",
                FileName = $"OperationLogHistory_{DateTime.Now:yyyyMMddHHmmss}"
            });

            return report;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Xử lý và ánh xạ dữ liệu báo cáo lịch sử điều khiển
        /// Created date: 07/10/2026
        /// </summary>
        private async Task<VmsOperationLogHistoryListReport> BuildOperationLogHistoryListReport(VmsOperationLogHistoryListRenderInput command)
        {
            var reportData = new VmsOperationLogHistoryListReport();
            try
            {
                command.FromDate = BaseUtil.TimeServer.GetTimeServer(command.FromDate);
                command.ToDate = BaseUtil.TimeServer.GetTimeServer(command.ToDate);

                // Lấy danh sách config dữ liệu trạng thái (TmsActionType)
                List<SysConfigDataOutput> configDataList = new();
                try
                {
                    var configs = await _messBus.InvokeAsync<List<SysConfigDataOutput>>(new SysConfigDataInput());
                    if (configs != null) configDataList = configs;
                }
                catch { }

                // Lấy danh mục thiết bị để map tên và vị trí (Km + Met)
                Dictionary<string, EquipmentOutput> equipDict = new();
                try
                {
                    var equipList = await _messBus.InvokeAsync<List<EquipmentOutput>>(new EquipmentInput());
                    if (equipList != null)
                    {
                        equipDict = equipList.Where(e => !string.IsNullOrEmpty(e.ID)).ToDictionary(e => e.ID);
                    }
                }
                catch { }

                // Truy vấn dữ liệu lịch sử điều khiển qua PageSignalLogHistoryInput (lấy tối đa 10000 dòng phục vụ báo cáo)
                var pagedLogs = await _messBus.InvokeAsync<SqlSugarPagedList<PageSignalLogHistoryOutput>>(new PageSignalLogHistoryInput
                {
                    Page = 1,
                    PageSize = 10000,
                    ModuleKey = command.ModuleKey,
                    EquipmentTypeId = command.EquipmentTypeId,
                    EquipmentId = command.EquipmentId,
                    State = command.State,
                    ExecuteBy = command.ExecuteBy,
                    FromDate = command.FromDate,
                    ToDate = command.ToDate,
                    Field = "ExecuteTime",
                    Order = "desc"
                });

                if (pagedLogs?.Records != null)
                {
                    int i = 1;
                    foreach (var log in pagedLogs.Records)
                    {
                        EquipmentOutput? equip = null;
                        if (!string.IsNullOrEmpty(log.EquipmentId))
                        {
                            equipDict.TryGetValue(log.EquipmentId, out equip);
                        }

                        var equipName = log.EquipmentName ?? equip?.Name ?? string.Empty;
                        var equipPosition = FormatEquipmentPosition(equip?.KmNumber, equip?.MetNumber);
                        var equipDisplayName = !string.IsNullOrEmpty(equipPosition)
                            ? $"{equipName}<br/><font size=\"8pt\"><i>{equipPosition}</i></font>"
                            : equipName;

                        var stateName = ResolveStateName(log.State, configDataList);

                        var oldRaw = !string.IsNullOrEmpty(log.OldUrl) ? log.OldUrl : log.OldData;
                        var newRaw = !string.IsNullOrEmpty(log.NewUrl) ? log.NewUrl : log.NewData;

                        var isOldImg = IsImageFile(oldRaw);
                        var isNewImg = IsImageFile(newRaw);

                        var oldImgPath = isOldImg ? ResolvePhysicalImagePath(oldRaw) : null;
                        var newImgPath = isNewImg ? ResolvePhysicalImagePath(newRaw) : null;

                        reportData.LogHistoryList.Add(new VmsOperationLogHistoryData
                        {
                            Idx = (i++).ToString(),
                            ExecuteTime = log.ExecuteTime?.ToString(BaseEnums.DateTimeFormatEnum.DateTimeDefault),
                            EquipmentName = equipName,
                            EquipmentPosition = equipPosition,
                            EquipmentDisplayName = equipDisplayName,
                            OldData = isOldImg ? string.Empty : oldRaw,
                            NewData = isNewImg ? string.Empty : newRaw,
                            OldImage = oldImgPath,
                            NewImage = newImgPath,
                            StateName = stateName,
                            ExecuteBy = log.ExecuteBy
                        });
                    }
                }

                LocalizeReportColumns(reportData);
                SetReportParameters(reportData, command);

                return reportData;
            }
            catch
            {
                return reportData;
            }
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Format vị trí Km và Mét của thiết bị
        /// </summary>
        private static string FormatEquipmentPosition(int? km, int? met)
        {
            if (km == null && met == null) return string.Empty;
            if (km != null && met != null) return $"Km{km} +{met}";
            if (km != null) return $"Km{km}";
            return $"+{met}m";
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Kiểm tra xem chuỗi dữ liệu có phải là file ảnh hay không
        /// </summary>
        private static bool IsImageFile(string? pathOrData)
        {
            if (string.IsNullOrWhiteSpace(pathOrData)) return false;
            var trimmed = pathOrData.Trim().ToLowerInvariant();
            return trimmed.EndsWith(".bmp") || trimmed.EndsWith(".png") || trimmed.EndsWith(".jpg") || trimmed.EndsWith(".jpeg");
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Dịch trạng thái điều khiển thiết bị (Anh / Việt)
        /// </summary>
        private string ResolveStateName(string? state, List<SysConfigDataOutput> configDataList)
        {
            if (string.IsNullOrWhiteSpace(state)) return string.Empty;

            var trimmed = state.Trim();

            // 1. Kiểm tra các mã trạng thái phổ biến
            if (trimmed.Equals("Success", StringComparison.OrdinalIgnoreCase) || trimmed == "1" || trimmed.Equals("OK", StringComparison.OrdinalIgnoreCase))
            {
                return GetLocText(BaseMsg.RptOperationLogHistory.Label.StateSuccess, "Thành công");
            }

            if (trimmed.Equals("Fail", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("Failed", StringComparison.OrdinalIgnoreCase) || trimmed == "0")
            {
                return GetLocText(BaseMsg.RptOperationLogHistory.Label.StateFailed, "Thất bại");
            }

            if (trimmed.Equals("Error", StringComparison.OrdinalIgnoreCase))
            {
                return GetLocText(BaseMsg.RptOperationLogHistory.Label.StateError, "Lỗi");
            }

            if (trimmed.Equals("Pending", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("Waiting", StringComparison.OrdinalIgnoreCase))
            {
                return GetLocText(BaseMsg.RptOperationLogHistory.Label.StatePending, "Đang chờ");
            }

            if (trimmed.Equals("Processing", StringComparison.OrdinalIgnoreCase))
            {
                return GetLocText(BaseMsg.RptOperationLogHistory.Label.StateProcessing, "Đang xử lý");
            }

            // 2. Tra cứu trong SysConfigData (tms_action_type)
            var cfg = configDataList.FirstOrDefault(c =>
                c.ConfigTypeCode != null
                && c.ConfigTypeCode.Equals("tms_action_type", StringComparison.OrdinalIgnoreCase)
                && c.Code != null
                && c.Code.Equals(trimmed, StringComparison.OrdinalIgnoreCase));

            if (cfg != null)
            {
                if (cfg.Name != null && cfg.Name.Equals("Success", StringComparison.OrdinalIgnoreCase))
                    return GetLocText(BaseMsg.RptOperationLogHistory.Label.StateSuccess, "Thành công");
                if (cfg.Name != null && (cfg.Name.Equals("Fail", StringComparison.OrdinalIgnoreCase) || cfg.Name.Equals("Failed", StringComparison.OrdinalIgnoreCase)))
                    return GetLocText(BaseMsg.RptOperationLogHistory.Label.StateFailed, "Thất bại");

                if (!string.IsNullOrWhiteSpace(cfg.Name))
                    return cfg.Name;
            }

            return trimmed;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Kiểm tra và chuyển đổi đường dẫn file ảnh thành đường dẫn tuyệt đối hoặc chuỗi đường dẫn hợp lệ cho FastReport
        /// Created date: 08/10/2026
        /// </summary>
        private static string? ResolvePhysicalImagePath(string? pathOrData)
        {
            if (string.IsNullOrWhiteSpace(pathOrData)) return null;

            var trimmed = pathOrData.Trim();
            if (!IsImageFile(trimmed))
            {
                return null;
            }

            // Chuẩn hóa dấu phân cách
            var normalized = trimmed.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            normalized = normalized.TrimStart(Path.DirectorySeparatorChar);

            // Kiểm tra trong wwwroot
            var webRoot = App.WebHostEnvironment?.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var fullPath = Path.Combine(webRoot, normalized);

            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            // Nếu đường dẫn là tuyệt đối sẵn và file tồn tại
            if (Path.IsPathRooted(trimmed) && File.Exists(trimmed))
            {
                return trimmed;
            }

            // Nếu file bắt đầu bằng "Upload" hoặc "upload"
            if (normalized.StartsWith("Upload", StringComparison.OrdinalIgnoreCase))
            {
                var tryPath = Path.Combine(webRoot, normalized);
                if (File.Exists(tryPath)) return tryPath;
            }

            return null;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Cấu hình các tham số tiêu đề báo cáo
        /// Created date: 07/10/2026
        /// </summary>
        private void SetReportParameters(VmsOperationLogHistoryListReport reportData, VmsOperationLogHistoryListRenderInput command)
        {
            var dataDate = command.FromDate != null && command.ToDate != null
                ? command.FromDate.Value.ToString(BaseEnums.DateTimeFormatEnum.DateTimeDefault) + " - " + command.ToDate.Value.ToString(BaseEnums.DateTimeFormatEnum.DateTimeDefault)
                : string.Empty;

            reportData.Parameters.DataDate = dataDate;
            reportData.Parameters.ReportDate = DateTime.Now.ToString(BaseEnums.DateTimeFormatEnum.DateTimeDefault);
            reportData.Parameters.ReportLogo = RptBaseHelper.LogoHp.GetLogo();
            reportData.Parameters.ReportSign = "wwwroot/upload/sign.png";

            // Title mặc định tiếng Việt giống file FR trạng thái thiết bị
            var title = _localizer[BaseMsg.RptOperationLogHistory.Label.RptTitle].Value;
            if (string.IsNullOrWhiteSpace(title) || title.StartsWith("lz.", StringComparison.OrdinalIgnoreCase))
            {
                title = "BÁO CÁO LỊCH SỬ ĐIỀU KHIỂN THIẾT BỊ";
            }
            reportData.Parameters.ReportTitle = title.ToUpper();

            var page = _localizer[BaseMsg.RptBase.Label.Page].Value;
            reportData.Parameters.PageLabel = string.IsNullOrWhiteSpace(page) || page.StartsWith("lz.", StringComparison.OrdinalIgnoreCase) ? "Trang" : page;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Đa ngôn ngữ tiêu đề các cột của báo cáo (với fallback tiếng Việt cứng chuẩn)
        /// Created date: 07/10/2026
        /// </summary>
        private void LocalizeReportColumns(VmsOperationLogHistoryListReport reportData)
        {
            reportData.Columns.Idx = GetLocText(BaseMsg.RptBase.Label.Idx, "STT").ToUpper();
            reportData.Columns.ExecuteTime = GetLocText(BaseMsg.RptOperationLogHistory.Entity.ExecuteTime, "THỜI GIAN THỰC THI").ToUpper();
            reportData.Columns.EquipmentName = GetLocText(BaseMsg.RptOperationLogHistory.Entity.Equipment, "THIẾT BỊ").ToUpper();
            reportData.Columns.OldData = GetLocText(BaseMsg.RptOperationLogHistory.Entity.BeforeData, "TRƯỚC TÁC ĐỘNG").ToUpper();
            reportData.Columns.NewData = GetLocText(BaseMsg.RptOperationLogHistory.Entity.AfterData, "SAU TÁC ĐỘNG").ToUpper();
            reportData.Columns.State = GetLocText(BaseMsg.RptOperationLogHistory.Entity.Status, "TRẠNG THÁI").ToUpper();
            reportData.Columns.ExecuteBy = GetLocText(BaseMsg.RptOperationLogHistory.Entity.ExecuteBy, "NGƯỜI THỰC HIỆN").ToUpper();
        }

        private string GetLocText(string key, string defaultText)
        {
            var val = _localizer[key].Value;
            if (string.IsNullOrWhiteSpace(val) || val.StartsWith("lz.", StringComparison.OrdinalIgnoreCase))
            {
                return defaultText;
            }
            return val;
        }
    }
}
