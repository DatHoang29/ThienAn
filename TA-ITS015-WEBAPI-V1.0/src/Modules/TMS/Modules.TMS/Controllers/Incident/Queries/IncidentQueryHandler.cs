using Modules.TMS.Infrastructure.Services.Incident.Rules;
using Modules.TMS.Infrastructure.Services.Incident.Classification;
using Microsoft.Extensions.Localization;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Helper;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Modules.TMS.Infrastructure.Services.Incident;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Utility.Utilities;
using Shared.Utility.Sse;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Controllers.Incident.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Description: Handler đọc dữ liệu CRUD của sự cố (Page / List / Single).
    /// Note: Truy vấn theo account (workflow) ở IncidentWorkflowQueryHandler; biểu đồ/đếm ở IncidentStatisticsQueryHandler.
    /// Create date: 19/08/2024
    /// </summary>
    public class IncidentQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsIncident> _incidentRepo;
        private IStringLocalizer _localizer;

        private IncidentGroupResolver _groupResolver;

        public IncidentQueryHandler(BaseRepository<TmsIncident> incidentRepo, IStringLocalizer localizer, IncidentGroupResolver groupResolver)
        {
            this._incidentRepo = incidentRepo;
            this._localizer = localizer;
            this._groupResolver = groupResolver;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 09/08/2024
        /// </summary>
        public Task<TmsIncidentPageOutput> HandleAsync(PageTmsIncidentInput command)
            => QueryPageAsync(command, isInternalChartQuery: false);

        /// <summary>
        /// Author: KienLT
        /// Description: Cùng truy vấn phân trang, nhưng cho đường NỘI BỘ vẽ biểu đồ / đếm sự cố: được vượt
        /// trần PageSize và bỏ hai câu COUNT thống kê. Kiểu message riêng nên client không gọi được đường này.
        /// Created date: 21/09/2026
        /// </summary>
        public Task<TmsIncidentPageOutput> HandleAsync(ChartTmsIncidentPageInput command)
            => QueryPageAsync(command, isInternalChartQuery: true);

        private async Task<TmsIncidentPageOutput> QueryPageAsync(PageTmsIncidentInput command, bool isInternalChartQuery)
        {
            TmsIncidentPageRules.Normalize(command, allowUnboundedPageSize: isInternalChartQuery);
            var filtered = BuildFilteredQuery(_incidentRepo.AsQueryable(), command);
            // Số tổng hợp tính trên TOÀN BỘ tập lọc nên không đổi khi người dùng sang trang khác.
            //
            // Bỏ hai câu COUNT cho đường NỘI BỘ vẽ biểu đồ / đếm sự cố: CountIncident và IncidentChart đều
            // đi qua chính handler này (IncidentStatisticsQueryHandler.GetIncidentListAsync dispatch
            // ChartTmsIncidentPageInput rồi chỉ lấy Records), nên tính Statistics cho hai đường đó là trả
            // giá cho số không ai đọc.
            //
            // Count both groups in the selected result; group is classification, not workflow.
            var newCount = 0;
            var executingCount = 0;
            if (!isInternalChartQuery)
            {
                var stateNew = ((int)TmsIncidentStateEnum.Registration).ToString();
                var stateVerified = ((int)TmsIncidentStateEnum.Verified).ToString();
                var stateInProgress = ((int)TmsIncidentStateEnum.InProgress).ToString();

                newCount = await filtered.Clone().MergeTable()
                    .Where(x => x.State == stateNew)
                    .CountAsync();
                executingCount = await filtered.Clone().MergeTable()
                    .Where(x => x.State == stateVerified || x.State == stateInProgress)
                    .CountAsync();
            }
            var tiList = await ApplyOrdering(filtered.Clone(), command)
                .ToPagedListAsync(command.Page, command.PageSize);

            var result = tiList.Adapt<TmsIncidentPageOutput>();
            foreach (var item in result.Records.Where(item => IncidentEventDisplayHelper.IsUnknown(item.EventTypeCode)))
                item.EventTypeName = IncidentEventDisplayHelper.Name(item.EventTypeCode, item.EventTypeName, item.Metadata,
                    _localizer[BaseMsg.TmsIncident.Label.UnknownEventType].Value);
            result.Statistics = new TmsIncidentPageStatisticsOutput { Total = tiList.Total, New = newCount, Executing = executingCount };
            return result;
        }

        internal static ISugarQueryable<PageTmsIncidentOutput> BuildFilteredQuery(
            ISugarQueryable<TmsIncident> source, PageTmsIncidentInput command)
        {
            // TODO: Cache

            int fromPosition = (command.FromKm ?? 0) * 1000 + (command.FromMet ?? 0);
            int toPosition = (command.ToKm ?? 0) * 1000 + (command.ToMet ?? 0);

            DateTime? closedIncidentFrom = null;
            DateTime? openIncidentFrom = null;

            if (command.FromDate == null && command.ToDate != null)
                command.FromDate = command.ToDate.Value.AddDays(-90);

            if (command.FromDate != null && command.ToDate == null)
                command.ToDate = command.FromDate.Value.AddDays(90);

            var serverNow = BaseUtil.TimeServer.GetTimeServer();
            if (command.ToDate > serverNow)
                command.ToDate = serverNow;

            if (command.OpenIncidentDaysLimit > 0)
                openIncidentFrom = serverNow.AddDays(-1 * (double)command.OpenIncidentDaysLimit);
            if (command.ClosedIncidentDaysLimit > 0)
                closedIncidentFrom = serverNow.AddDays(-1 * (double)command.ClosedIncidentDaysLimit);

            if (command.Field != null)
            {
                if (command.Field.Equals("EventTypeId", StringComparison.OrdinalIgnoreCase))
                    command.Field = "et.Name";
                else if (command.Field.Equals("Location", StringComparison.OrdinalIgnoreCase))
                    command.Field = "ti.KmNumber";
                else if (!command.Field.StartsWith("ti.", StringComparison.OrdinalIgnoreCase) && !command.Field.StartsWith("et.", StringComparison.OrdinalIgnoreCase))
                    command.Field = $"ti.{command.Field}";
            }

            // if (command.FromDate != null && command.ToDate != null)
            // {
            //     TimeSpan timeRange = command.ToDate.Value - command.FromDate.Value;

            //     if ((int)timeRange.TotalDays < 0 || (int)timeRange.TotalDays > 90)
            //     {
            //         if (command.IsRequestForChart)
            //             return null;

            //         var idx = 0;
            //         var msg = Regex.Replace(this._localizer[BaseMsg.TmsIncident.Validation.LimitDateRange], @"\{.*?\}", match => { return $"{{{idx++}}}"; });
            //         throw Oops.Oh(msg, 90);
            //     }
            // }

            // TODO: Kiểm tra lại lỗi .OrderBy((ti, et) => ti.StartDate, OrderByType.Desc)
            var filtered = source.Where(x => x.IsDelete == null)
                .WhereIF(!command.IncludeEvents, x => !(SqlFunc.IsNull(x.EventGroup, "") == IncidentEventGroupConst.Event || (x.EventGroup == null && SqlFunc.Subqueryable<TmsEventType>().Where(t => t.ID == x.EventTypeId && t.IsDelete == null && t.Type.Trim().ToLower() == IncidentEventGroupConst.Event).Any())))
                .WhereIF(!command.IncludeIncidents, x => SqlFunc.IsNull(x.EventGroup, "") == IncidentEventGroupConst.Event || (x.EventGroup == null && SqlFunc.Subqueryable<TmsEventType>().Where(t => t.ID == x.EventTypeId && t.IsDelete == null && t.Type.Trim().ToLower() == IncidentEventGroupConst.Event).Any()))
                .LeftJoin<TmsEventType>((ti, et) => ti.EventTypeId == et.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), ti => ti.Name != null && ti.Name.Contains(command.Name, StringComparison.OrdinalIgnoreCase) || ti.Code.Contains(command.Name, StringComparison.OrdinalIgnoreCase))
                // Sự cố do VDS tạo lưu EventId = VendorEventId của bản tin: tra sự cố của một sự kiện.
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventId), ti => ti.EventId == command.EventId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventTypeId), ti => ti.EventTypeId == command.EventTypeId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ZoneId), ti => ti.ZoneId != null && ti.ZoneId.ToString().Contains(command.ZoneId))
                .WhereIF(!string.IsNullOrWhiteSpace(command.State), ti => ti.State == command.State)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Source), ti => ti.Source == command.Source)
                .WhereIF(command.FromDate != null, ti => ti.StartDate != null && ti.StartDate.Value >= command.FromDate.Value)
                .WhereIF(command.ToDate != null, ti => ti.StartDate != null && ti.StartDate.Value <= command.ToDate.Value)
                .WhereIF(openIncidentFrom != null && closedIncidentFrom != null,
                    IncidentDisplayRules.Window(openIncidentFrom.GetValueOrDefault(), closedIncidentFrom.GetValueOrDefault()))
                .WhereIF(
                    (command.FromKm != null || command.FromMet != null) && (command.ToKm != null || command.ToMet != null),
                    ti =>
                        (SqlFunc.IsNull(ti.KmNumber, 0) * 1000 + SqlFunc.IsNull(ti.MetNumber, 0) >= fromPosition)
                        &&
                        (SqlFunc.IsNull(ti.KmNumber, 0) * 1000 + SqlFunc.IsNull(ti.MetNumber, 0) <= toPosition)
                )
                .WhereIF(
                    (command.FromKm != null || command.FromMet != null) && (command.ToKm == null && command.ToMet == null),
                    ti => (SqlFunc.IsNull(ti.KmNumber, 0) * 1000 + SqlFunc.IsNull(ti.MetNumber, 0) >= fromPosition)
                )
                .WhereIF(
                    (command.FromKm == null && command.FromMet == null) && (command.ToKm != null || command.ToMet != null),
                    ti => (SqlFunc.IsNull(ti.KmNumber, 0) * 1000 + SqlFunc.IsNull(ti.MetNumber, 0) <= toPosition)
                )
                // Chỉ lấy sự cố chưa bị xóa mềm (IsDelete == null). LeftJoin có thể làm filter soft-delete
                // toàn cục không áp, nên khai báo tường minh cho chắc (đã có Where IsDelete==null ở đầu chuỗi).
                .Where((ti, et) => ti.IsDelete == null)
                .Select((ti, et) => new PageTmsIncidentOutput() { EventTypeCode = et.Code, EventTypeName = et.Name, EventGroup = ti.EventGroup ?? et.Type.Trim().ToLower() }, true)
;
            return filtered;
        }

        public async Task<TmsIncidentMonitorOutput> HandleAsync(TmsIncidentMonitorInput input, CancellationToken cancellationToken)
        {
            using var diagnostic = SseDiagnosticOperation.Begin("incident-monitor-sql", cancellationToken);
            diagnostic.Phase = "sql-read";
            var command = TmsIncidentMonitorRules.Query(input);
            try
            {
                var rows = await ApplyOrdering(BuildFilteredQuery(_incidentRepo.AsQueryable(), command), command)
                    .ToListAsync(cancellationToken);
                foreach (var item in rows.Where(item => IncidentEventDisplayHelper.IsUnknown(item.EventTypeCode)))
                    item.EventTypeName = IncidentEventDisplayHelper.Name(item.EventTypeCode, item.EventTypeName, item.Metadata,
                        _localizer[BaseMsg.TmsIncident.Label.UnknownEventType].Value);
                diagnostic.Complete(rows.Count);
                return TmsIncidentMonitorOutput.From(rows);
            }
            catch (OperationCanceledException ex) { diagnostic.Cancelled(ex); throw; }
            catch (Exception ex) { diagnostic.Failed(ex); throw; }
        }

        internal static ISugarQueryable<PageTmsIncidentOutput> ApplyOrdering(
            ISugarQueryable<PageTmsIncidentOutput> query, PageTmsIncidentInput command)
            // OrderBuilder already supplies StartDate when no field is selected.
            // Adding it again makes SQL Server reject both the list and page queries.
            => query.OrderBuilder(command, "", "ti.StartDate").OrderBy(ti => ti.ID, OrderByType.Desc);

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<List<TmsIncidentOutput>> HandleAsync(TmsIncidentInput command)
        {
            // TODO: Cache

            int fromPosition = (command.FromKm ?? 0) * 1000 + (command.FromMet ?? 0);
            int toPosition = (command.ToKm ?? 0) * 1000 + (command.ToMet ?? 0);

            DateTime? closedIncidentFrom = null;
            DateTime? openIncidentFrom = null;

            if (command.OpenIncidentDaysLimit > 0)
                openIncidentFrom = BaseUtil.TimeServer.GetTimeServer().AddDays(-1 * (double)command.OpenIncidentDaysLimit);
            if (command.ClosedIncidentDaysLimit > 0)
                closedIncidentFrom = BaseUtil.TimeServer.GetTimeServer().AddDays(-1 * (double)command.ClosedIncidentDaysLimit);

            var tiList = await this._incidentRepo.AsQueryable().Where(x => x.State != IncidentRecordingConst.LegacyState)
                .LeftJoin<TmsEventType>((ti, et) => ti.EventTypeId == et.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), ti => ti.Name != null && ti.Name.Contains(command.Name, StringComparison.OrdinalIgnoreCase) || ti.Code.Contains(command.Name, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventTypeId), ti => ti.EventTypeId == command.EventTypeId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ZoneId), ti => ti.ZoneId != null && ti.ZoneId.ToString().Contains(command.ZoneId))
                .WhereIF(!string.IsNullOrWhiteSpace(command.State), ti => ti.State == command.State)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Source), ti => ti.Source == command.Source)
                .WhereIF(command.FromDate != null, ti => ti.StartDate != null && ti.StartDate.Value >= command.FromDate.Value)
                .WhereIF(command.ToDate != null, ti => ti.StartDate != null && ti.StartDate.Value <= command.ToDate.Value)
                .WhereIF(
                    (command.FromKm != null || command.FromMet != null) && (command.ToKm != null || command.ToMet != null),
                    ti =>
                        (SqlFunc.IsNull(ti.KmNumber, 0) * 1000 + SqlFunc.IsNull(ti.MetNumber, 0) >= fromPosition)
                    &&
                        (SqlFunc.IsNull(ti.KmNumber, 0) * 1000 + SqlFunc.IsNull(ti.MetNumber, 0) <= toPosition)
                )
                .WhereIF(openIncidentFrom != null && closedIncidentFrom != null,
                        ti => ti.StartDate != null
                        &&
                        (
                            (
                                (
                                    ti.State == ((int)TmsIncidentStateEnum.Registration).ToString()
                                    || ti.State == ((int)TmsIncidentStateEnum.Verified).ToString()
                                    || ti.State == ((int)TmsIncidentStateEnum.InProgress).ToString()
                                )
                                &&
                                ti.StartDate.Value.Date >= openIncidentFrom.Value.Date
                            )
                            ||
                            (
                                (
                                    ti.State == ((int)TmsIncidentStateEnum.Skipped).ToString()
                                    || ti.State == ((int)TmsIncidentStateEnum.Canceled).ToString()
                                    || ti.State == ((int)TmsIncidentStateEnum.Finished).ToString()
                                )
                                &&
                                ti.StartDate.Value.Date >= closedIncidentFrom.Value.Date
                            )
                        )
                )
                .WhereIF(
                    (command.FromKm != null || command.FromMet != null) && (command.ToKm == null && command.ToMet == null),
                    ti => (SqlFunc.IsNull(ti.KmNumber, 0) * 1000 + SqlFunc.IsNull(ti.MetNumber, 0) >= fromPosition)
                )
                .WhereIF(
                    (command.FromKm == null && command.FromMet == null) && (command.ToKm != null || command.ToMet != null),
                    ti => (SqlFunc.IsNull(ti.KmNumber, 0) * 1000 + SqlFunc.IsNull(ti.MetNumber, 0) <= toPosition)
                )
                .Where((ti, et) => ti.IsDelete == null)
                .OrderBy((ti, et) => ti.StartDate, OrderByType.Desc)
                .Select((ti, et) => new TmsIncidentOutput() { EventTypeCode = et.Code, EventTypeName = et.Name, EventGroup = ti.EventGroup ?? et.Type.Trim().ToLower() }, true)
                .ToListAsync();

            foreach (var item in tiList.Where(item => IncidentEventDisplayHelper.IsUnknown(item.EventTypeCode)))
                item.EventTypeName = IncidentEventDisplayHelper.Name(item.EventTypeCode, item.EventTypeName, item.Metadata,
                    _localizer[BaseMsg.TmsIncident.Label.UnknownEventType].Value);
            return tiList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy một record
        /// </summary>
        public async Task<TmsIncidentOutput> HandleAsync(TmsIncidentSingleInput command)
        {
            if (string.IsNullOrWhiteSpace(command.Id) && string.IsNullOrWhiteSpace(command.Code))
                return null;

            var incident = await this._incidentRepo.AsQueryable()
                    .LeftJoin<TmsEventType>((ti, et) => ti.EventTypeId == et.ID)
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Id), (ti, et) => ti.ID == command.Id)
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Code), (ti, et) => ti.Code == command.Code)
                    .Where((ti, et) => ti.IsDelete == null)
                    .Select((ti, et) => new TmsIncidentOutput() { EventTypeCode = et.Code, EventTypeName = et.Name }, true)
                    .FirstAsync();

            if (incident != null)
            {
                await this._groupResolver.PopulateGroupsAsync(new[] { incident });
                if (IncidentEventDisplayHelper.IsUnknown(incident.EventTypeCode))
                    incident.EventTypeName = IncidentEventDisplayHelper.Name(incident.EventTypeCode, incident.EventTypeName, incident.Metadata,
                        _localizer[BaseMsg.TmsIncident.Label.UnknownEventType].Value);
            }
            return incident;
        }
    }
}
