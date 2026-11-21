using System;
using System.Collections.Generic;
using System.Linq;
using Modules.FMS.Controllers.EquipmentTelemetry.Dto;
using Modules.FMS.Core.Entities;
using Modules.FMS.Infrastructure;
using Shared.DTO.Dtos.System;
using Shared.DTO.Dtos.Tms;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.System;
using Shared.Utility.Apis.Tms;
using Wolverine;

namespace Modules.FMS.Controllers.EquipmentTelemetry.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Truy vấn telemetry thiết bị FMS: phân trang lịch sử đã lưu.
    /// Created date: 2026-08-27
    /// </summary>
    public class EquipmentTelemetryQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<FmsEquipmentTelemetry> _repository;

        public EquipmentTelemetryQueryHandler(BaseRepository<FmsEquipmentTelemetry> repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Phân trang lịch sử telemetry thiết bị đã lưu.
        /// </summary>
        public async Task<SqlSugarPagedList<PageEquipmentTelemetryOutput>> HandleAsync(PageEquipmentTelemetryInput command)
        {
            return await _repository.AsQueryable()
                .Where(x => x.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), x => x.EquipmentId == command.EquipmentId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentCode), x => x.EquipmentCode != null && x.EquipmentCode.Contains(command.EquipmentCode))
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentTypeId), x => x.EquipmentTypeId == command.EquipmentTypeId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Source), x => x.Source == command.Source)
                .WhereIF(command.Succeeded != null, x => x.Succeeded == command.Succeeded)
                .WhereIF(command.FromDate != null, x => x.CollectedTime >= command.FromDate)
                .WhereIF(command.ToDate != null, x => x.CollectedTime <= command.ToDate)
                .Select((x) => new PageEquipmentTelemetryOutput() { }, true)
                .OrderBuilder(command, "", "CollectedTime", true)
                .ToPagedListAsync(command.Page, command.PageSize);
        }

        /// <summary>
        /// Phân trang DANH SÁCH THIẾT BỊ kèm telemetry MỚI NHẤT của mỗi thiết bị.
        /// - Danh sách thiết bị lấy từ TMS
        /// - Telemetry mới nhất: theo max(CollectedTime) của từng EquipmentId trong FmsEquipmentTelemetry.
        /// - Thiết bị chưa có telemetry vẫn hiển thị (3 cột thông số để null).
        /// Author: ChuongLP
        /// Date: 2026-09-10
        /// </summary>
        public async Task<SqlSugarPagedList<EquipmentLatestTelemetryOutput>> HandleAsync(PageEquipmentLatestInput command)
        {
            // 1) Lấy toàn bộ thiết bị từ TMS, bỏ thiết bị đã xóa.
            var eqResp = await BaseApiClient<IEquipmentApi>.Client().List(new TmsEquipmentListRequest());
            var equipList = (eqResp.Content?.Result ?? new List<TmsEquipmentResponse>())
                .Where(e => e.IsDelete == null)
                .ToList();
            
            // 2) Lọc theo mã / loại thiết bị (nếu có).
            if (!string.IsNullOrWhiteSpace(command.EquipmentCode))
                equipList = equipList
                    .Where(e => !string.IsNullOrWhiteSpace(e.Code)
                                && e.Code.IndexOf(command.EquipmentCode, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            if (!string.IsNullOrWhiteSpace(command.EquipmentTypeId))
                equipList = equipList.Where(e => e.EquipmentTypeId == command.EquipmentTypeId).ToList();

            // 2b) Chỉ giữ thiết bị thuộc loại được chọn giám sát (SysConfigData "fms_telemetry_equipment_type").
            //     Chưa cấu hình loại nào / không đọc được cấu hình -> giữ toàn bộ như trước.
            var monitoredTypeIds = await GetMonitoredEquipmentTypeIdsAsync();
            if (monitoredTypeIds.Count > 0)
                equipList = equipList.Where(e => e.EquipmentTypeId != null && monitoredTypeIds.Contains(e.EquipmentTypeId)).ToList();

            // 3) Lấy telemetry MỚI NHẤT theo từng EquipmentId (max CollectedTime).
            var latestTelemetry = await _repository.AsQueryable()
                .Where(x => x.IsDelete == null && x.EquipmentId != null)
                .Where(x => SqlFunc.Subqueryable<FmsEquipmentTelemetry>()
                    .Where(s => s.EquipmentId == x.EquipmentId && s.IsDelete == null)
                    .Max(s => s.CollectedTime) == x.CollectedTime)
                .ToListAsync();

            var latestByEquipmentId = latestTelemetry
                .Where(t => !string.IsNullOrWhiteSpace(t.EquipmentId))
                .GroupBy(t => t.EquipmentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.CollectedTime).First());

            // 4) Xác định THỜI ĐIỂM BẮT ĐẦU MẤT KẾT NỐI cho thiết bị đang lỗi (bản ghi mới nhất Succeeded=false).
            //    = thời điểm sớm nhất của chuỗi thất bại liên tiếp hiện tại (không có lần thành công nào sau đó).
            var failedEquipmentIds = latestByEquipmentId.Values
                .Where(t => t.Succeeded == false && !string.IsNullOrWhiteSpace(t.EquipmentId))
                .Select(t => t.EquipmentId!)
                .Distinct()
                .ToList();

            var disconnectedSinceByEquipmentId = new Dictionary<string, DateTime?>();
            if (failedEquipmentIds.Count > 0)
            {
                // Lấy các bản ghi lỗi thuộc CHUỖI LỖI HIỆN TẠI (không có lần thành công nào sau đó),
                // chỉ 2 cột cần thiết để nhẹ; min(CollectedTime) mỗi thiết bị = thời điểm bắt đầu mất kết nối.
                var outageRecords = await _repository.AsQueryable()
                    .Where(x => x.IsDelete == null && x.Succeeded == false && x.EquipmentId != null
                                && failedEquipmentIds.Contains(x.EquipmentId))
                    .Where(x => !SqlFunc.Subqueryable<FmsEquipmentTelemetry>()
                        .Where(s => s.EquipmentId == x.EquipmentId && s.IsDelete == null
                                    && s.Succeeded == true && s.CollectedTime > x.CollectedTime)
                        .Any())
                    .Select(x => new { x.EquipmentId, x.CollectedTime })
                    .ToListAsync();

                disconnectedSinceByEquipmentId = outageRecords
                    .Where(o => !string.IsNullOrWhiteSpace(o.EquipmentId))
                    .GroupBy(o => o.EquipmentId!)
                    .ToDictionary(g => g.Key, g => g.Min(o => o.CollectedTime));
            }

            // 5) Ghép thiết bị với telemetry mới nhất (thiết bị chưa có telemetry -> thông số null).
            var merged = equipList
                .OrderBy(e => e.Code)
                .Select(e =>
                {
                    latestByEquipmentId.TryGetValue(e.ID, out var t);
                    disconnectedSinceByEquipmentId.TryGetValue(e.ID ?? string.Empty, out var disconnectedSince);
                    return new EquipmentLatestTelemetryOutput
                    {
                        EquipmentId = e.ID,
                        EquipmentCode = e.Code,
                        EquipmentName = e.Name,
                        EquipmentTypeId = e.EquipmentTypeId,
                        Vendor = e.Vendor,
                        EquipmentState = e.EquipmentState,
                        KmNumber = e.KmNumber,
                        MetNumber = e.MetNumber,
                        ResourceMetric = t?.ResourceMetric,
                        EnvironmentMetric = t?.EnvironmentMetric,
                        DiagnosticMetric = t?.DiagnosticMetric,
                        Succeeded = t?.Succeeded,
                        CollectedTime = t?.CollectedTime,
                        Message = t?.Message,
                        DisconnectedSince = disconnectedSince,
                    };
                })
                .ToList();

            // 6) Phân trang in-memory (trả đúng format SqlSugarPagedList: records + total).
            var page = command.Page <= 0 ? 1 : command.Page;
            var pageSize = command.PageSize <= 0 ? 20 : command.PageSize;
            var records = merged.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new SqlSugarPagedList<EquipmentLatestTelemetryOutput>
            {
                Records = records,
                Total = merged.Count,
            };
        }

        /// <summary>
        /// ConfigTypeCode (SysConfigType) chứa danh sách loại thiết bị được giám sát ở màn Telemetry.
        /// Mỗi dòng SysConfigData: Code = Value = ID loại thiết bị.
        /// </summary>
        private const string MonitoredEquipmentTypeConfigCode = "fms_telemetry_equipment_type";

        /// <summary>
        /// Lấy danh sách ID loại thiết bị được chọn giám sát Telemetry (cấu hình SysConfigData).
        /// Rỗng khi chưa cấu hình hoặc không đọc được cấu hình -> nơi gọi hiển thị toàn bộ thiết bị.
        /// Author: ChuongLP
        /// Date: 2026-10-27
        /// </summary>
        private static async Task<HashSet<string>> GetMonitoredEquipmentTypeIdsAsync()
        {
            try
            {
                var response = await BaseApiClient<IConfigDataApi>.Client()
                    .List(new SysConfigDataListRequest() { ConfigTypeCode = MonitoredEquipmentTypeConfigCode });

                return (response.Content?.Result ?? new List<SysConfigDataResponse>())
                    .Where(c => c.ConfigTypeCode != null
                                && c.ConfigTypeCode.Equals(MonitoredEquipmentTypeConfigCode, StringComparison.OrdinalIgnoreCase))
                    .Select(c => !string.IsNullOrWhiteSpace(c.Value) ? c.Value : c.Code)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => id!.Trim())
                    .ToHashSet();
            }
            catch
            {
                return new HashSet<string>();
            }
        }
    }
}
