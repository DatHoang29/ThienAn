using Microsoft.Extensions.Logging;
using Modules.TMS.Controllers.WorkDetailContact.Dto;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Helper;
using Modules.TMS.Core.Exceptions;
using Furion.Localization;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Dto.WorkExecute;
using Modules.TMS.Core.Entities;
using Shared.Core.Utilities.Constants;
using Shared.DTO.Enums;
using Shared.Utility.Utilities;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Infrastructure.Services.Incident
{
    /// <summary>
    /// Author: ThangNN (refactor)
    /// Description: Lấy danh sách sự cố + công việc theo phân quyền nhóm của tài khoản hiện tại.
    /// (tách từ TmsIncidentQueryHandler để handler gọn hơn).
    /// </summary>
    public class IncidentAccessService : IScoped
    {
        /// <summary>
        /// Author: SonTH - 24/07/2026
        /// Key chứa các sự cố đang chờ tự động thực thi, dùng cho banner cảnh báo đếm ngược.
        /// KHÔNG phụ thuộc phân quyền nhóm như các key còn lại: mọi nhóm đều có thể nhận (chỉ để xem),
        /// tuỳ công tắc IncidentPermissionService.GetAutomationBannerScopeAsync.
        /// </summary>
        public const string AutomationPendingKey = "automationPendingList";

        private readonly BaseRepository<TmsIncident> _incidentRepo;
        private readonly BaseRepository<TmsWorkExecute> _workExecuteRepo;
        private readonly BaseRepository<TmsWorkDetailContacts> _workDetailContactRepo;
        private readonly BaseRepository<TmsIncidentAutomationRun> _automationRunRepo;
        private readonly IncidentPermissionService _permissionService;
        private readonly ILogger<IncidentAccessService> _logger;

        public IncidentAccessService(
            BaseRepository<TmsIncident> incidentRepo,
            BaseRepository<TmsWorkExecute> workExecuteRepo,
            BaseRepository<TmsWorkDetailContacts> workDetailContactRepo,
            BaseRepository<TmsIncidentAutomationRun> automationRunRepo,
            IncidentPermissionService permissionService,
            ILogger<IncidentAccessService> logger)
        {
            _incidentRepo = incidentRepo;
            _workExecuteRepo = workExecuteRepo;
            _workDetailContactRepo = workDetailContactRepo;
            _automationRunRepo = automationRunRepo;
            _permissionService = permissionService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy sự cố theo phân quyền nhóm của tài khoản hiện tại.
        /// </summary>
        public async Task<Dictionary<string, List<TmsIncidentView>>> GetByAccountAsync()
        {
            if (!IsUserLoggedIn())
                return null;

            // Sử dụng OrgId của User để lọc work-execute theo ExecuteOrgId
            string userOrgId = App.User.FindFirst(ClaimConst.OrgId)?.Value;

            // Sử dụng OrgCode của User để lọc incident tương ứng phân quyền
            var orgCode = App.User.FindFirst(ClaimConst.OrgCode)?.Value;

            if (string.IsNullOrWhiteSpace(userOrgId) || string.IsNullOrWhiteSpace(orgCode))
                return null;

            Dictionary<string, List<TmsIncidentView>> result;

            // Nếu OrgCode là TcVhh
            if (await _permissionService.IsInGroupAsync(AccountOrgCodeEnum.GroupLeader))
                result = await GetForLeaderAsync();

            // Nếu OrgCode là NvGsgt
            else if (await _permissionService.IsInGroupAsync(AccountOrgCodeEnum.GroupSupervisor))
                result = await GetForSupervisorAsync(userOrgId);

            // Nếu OrgCode là nhóm nhân viên thực hiện
            else
                result = await GetForEmployeeAsync(userOrgId);

            // Author: SonTH - 24/07/2026
            // Gắn nhánh tự động hóa lên KẾT QUẢ CHUNG. Luồng thủ công ở trên giữ nguyên hoàn toàn;
            // tự động hóa chỉ là lớp thông tin bám thêm, nhờ đó frontend chỉ cần gọi đúng API này.
            //
            // Bọc try/catch tại đây: đây là API mà popup xử lý sự cố thủ công phụ thuộc vào. Lỗi ở lớp
            // mở rộng (kể cả lỗi gọi cấu hình sang phân hệ System) tuyệt đối không được làm sập luồng chính.
            try
            {
                await AttachAutomationAsync(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IncidentAccessService] Gắn thông tin tự động hóa vào danh sách sự cố thất bại.");
            }

            var incidents = result.Values
                .Where(list => list != null)
                .SelectMany(list => list)
                .Select(view => view?.Incident)
                .OfType<TmsIncidentOutput>()
                .Distinct();
            foreach (var item in incidents.Where(item => IncidentEventDisplayHelper.IsUnknown(item.EventTypeCode)))
                item.EventTypeName = IncidentEventDisplayHelper.Name(item.EventTypeCode, item.EventTypeName, item.Metadata,
                    L.Text[BaseMsg.TmsIncident.Label.UnknownEventType].Value);
            return result;
        }

        /// <summary>
        /// Lấy sự cố và công việc cho nhóm TcVhh (trưởng ca).
        /// </summary>
        private async Task<Dictionary<string, List<TmsIncidentView>>> GetForLeaderAsync()
        {
            var inProgressIncidentList = await _incidentRepo.AsQueryable().Where(x => x.State != IncidentRecordingConst.LegacyState)
                .LeftJoin<TmsEventType>((ti, et) => ti.EventTypeId == et.ID)
                .Where((ti, et) => ti.State == ((int)TmsIncidentStateEnum.InProgress).ToString())
                .Where((ti, et) => ti.IsDelete == null)
                .OrderBy((ti, et) => ti.StartDate, OrderByType.Desc)
                .Select((ti, et) => new TmsIncidentOutput() { EventTypeCode = et.Code, EventTypeName = et.Name }, true)
                .ToListAsync();

            var incidentWorkDetailList = inProgressIncidentList?.Count() > 0 ? await BuildWorkDetailViewAsync(inProgressIncidentList, null) : null;
            return new Dictionary<string, List<TmsIncidentView>>() { { "allProcessingIncident", incidentWorkDetailList } };
        }

        /// <summary>
        /// Lấy sự cố và công việc cho nhóm NvGsgt (giám sát).
        /// </summary>
        private async Task<Dictionary<string, List<TmsIncidentView>>> GetForSupervisorAsync(string executeOrgId)
        {
            var registrationIncidentList = await _incidentRepo.AsQueryable().Where(x => x.State != IncidentRecordingConst.LegacyState)
                .LeftJoin<TmsEventType>((ti, et) => ti.EventTypeId == et.ID)
                .Where((ti, et) => ti.State == ((int)TmsIncidentStateEnum.Registration).ToString())
                .Where((ti, et) => ti.IsDelete == null)
                .OrderBy((ti, et) => ti.StartDate, OrderByType.Desc)
                .Select((ti, et) => new TmsIncidentOutput() { EventTypeCode = et.Code, EventTypeName = et.Name }, true)
                .ToListAsync();

            var verifiedIncidentList = await _incidentRepo.AsQueryable().Where(x => x.State != IncidentRecordingConst.LegacyState)
                .LeftJoin<TmsEventType>((ti, et) => ti.EventTypeId == et.ID)
                .Where((ti, et) => ti.State == ((int)TmsIncidentStateEnum.Verified).ToString())
                .Where((ti, et) => ti.IsDelete == null)
                .OrderBy((ti, et) => ti.StartDate, OrderByType.Desc)
                .Select((ti, et) => new TmsIncidentOutput() { EventTypeCode = et.Code, EventTypeName = et.Name }, true)
                .ToListAsync();

            var inProgressIncidentList = await _incidentRepo.AsQueryable().Where(x => x.State != IncidentRecordingConst.LegacyState)
                .LeftJoin<TmsEventType>((ti, et) => ti.EventTypeId == et.ID)
                .Where((ti, et) => ti.State == ((int)TmsIncidentStateEnum.InProgress).ToString())
                .Where((ti, et) => ti.IsDelete == null)
                .OrderBy((ti, et) => ti.StartDate, OrderByType.Desc)
                .Select((ti, et) => new TmsIncidentOutput() { EventTypeCode = et.Code, EventTypeName = et.Name }, true)
                .ToListAsync();

            // Chuyển đổi các danh sách incident sang danh sách đối tượng TmsIncidentView
            List<TmsIncidentView> registrationIncidentViewList = null;
            if (registrationIncidentList?.Count() > 0)
            {
                registrationIncidentViewList = new List<TmsIncidentView>();
                registrationIncidentList.ForEach(ri =>
                {
                    registrationIncidentViewList.Add(new TmsIncidentView() { Incident = ri, WorkExecuteList = null });
                });
            }

            List<TmsIncidentView> verifiedIncidentViewList = null;
            if (verifiedIncidentList?.Count() > 0)
            {
                verifiedIncidentViewList = new List<TmsIncidentView>();
                verifiedIncidentList.ForEach(ri =>
                {
                    verifiedIncidentViewList.Add(new TmsIncidentView() { Incident = ri });
                });
            }

            var incidentWorkDetailList = inProgressIncidentList?.Count() > 0 ? await BuildWorkDetailViewAsync(inProgressIncidentList, executeOrgId) : null;

            return new Dictionary<string, List<TmsIncidentView>>() {
                { "registrationIncidentList", registrationIncidentViewList },
                { "verifiedIncidentList", verifiedIncidentViewList },
                { "inProgressIncidentList", incidentWorkDetailList }
            };
        }

        /// <summary>
        /// Lấy sự cố và công việc cho nhóm nhân viên thực hiện.
        /// </summary>
        private async Task<Dictionary<string, List<TmsIncidentView>>> GetForEmployeeAsync(string orgCode)
        {
            var inProgressIncidentList = await _incidentRepo.AsQueryable().Where(x => x.State != IncidentRecordingConst.LegacyState)
                .LeftJoin<TmsEventType>((ti, et) => ti.EventTypeId == et.ID)
                .Where((ti, et) => ti.State == ((int)TmsIncidentStateEnum.InProgress).ToString())
                .Where((ti, et) => ti.IsDelete == null)
                .OrderBy((ti, et) => ti.StartDate, OrderByType.Desc)
                .Select((ti, et) => new TmsIncidentOutput() { EventTypeCode = et.Code, EventTypeName = et.Name }, true)
                .ToListAsync();

            var incidentWorkDetailList = inProgressIncidentList?.Count() > 0 ? await BuildWorkDetailViewAsync(inProgressIncidentList, orgCode) : null;

            return new Dictionary<string, List<TmsIncidentView>>() { { "inProgressIncidentList", incidentWorkDetailList } };
        }

        /// <summary>
        /// Gán danh sách công việc chi tiết (work-execute) + thông tin liên hệ cho danh sách sự cố.
        /// </summary>
        private async Task<List<TmsIncidentView>> BuildWorkDetailViewAsync(List<TmsIncidentOutput> incidentList, string? executeOrgId)
        {
            if (incidentList == null || incidentList.Count() == 0)
                return null;

            // Lấy toàn bộ danh sách work-execute trong bảng TmsWorkExecute
            var workExecuteList = await _workExecuteRepo.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(executeOrgId), we => we.ExecuteOrg == executeOrgId) // IMPORTANCE: Chỉ lọc ExecuteOrg nếu executeOrgId khác rỗng
                .WhereIF(!string.IsNullOrWhiteSpace(executeOrgId), we => we.State == ((int)TmsWorkExecuteStateEnum.Waiting).ToString()) // IMPORTANCE: Chỉ lọc State nếu executeOrgId khác rỗng
                .OrderBy(we => we.Step, OrderByType.Asc)
                .Select(we => new TmsWorkExecuteOutput() { }, true)
                .ToListAsync();

            // Gắn danh sách công việc work-execute tương ứng cho từng sự cố
            var incidentViewList = new List<TmsIncidentView>();
            incidentList.ForEach(ti =>
            {
                if (workExecuteList?.Any(we => we.IncidentId == ti.ID) == true)
                {
                    incidentViewList.Add(new TmsIncidentView()
                    {
                        Incident = ti,
                        WorkExecuteList = workExecuteList?.Where(we => we.TrafficPlanId == ti.TrafficPlanId && we.IncidentId == ti.ID).ToList()
                    });
                }
            });

            // Lấy danh sách thông tin liên hệ liên quan đến công việc
            var workDetailContactList = await _workDetailContactRepo.AsQueryable()
                .LeftJoin<TmsWorkContact>((wdc, wc) => wdc.WorkContactId == wc.ID)
                .Where((wdc, wc) => wdc.IsDelete == null && wc.IsDelete == null && wc.Status == (BaseEnums.StatusEnum)BaseEnums.StatusEnum.Enable)
                .OrderBy((wdc, wc) => wdc.OrderNo, OrderByType.Asc)
                .Select((wdc, wc) => new TmsWorkDetailContactOutput()
                {
                    ContactName = wc.Name,
                    ContactPhone = wc.Phone,
                    ContactType = wc.Type,
                    ContactRefId = wc.RefId
                }, true)
                .ToListAsync();

            // Gán danh sách thông tin liên hệ tương ứng với mỗi công việc
            incidentViewList?.ForEach(iv =>
            {
                iv.WorkExecuteList?.ForEach(we =>
                {
                    we.WorkContactList = workDetailContactList?.Where(wdc => wdc.WorkDetailId == we.WorkDetailId).ToList<object>();
                });
            });

            // Trả về kết quả
            return incidentViewList?.Any(iv => iv.WorkExecuteList?.Count() > 0) == true ? incidentViewList : null;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Gắn thông tin luồng tự động vào kết quả của ListByAccount. Làm hai việc:
        ///              (1) Điền Automation cho những sự cố đã có trong các danh sách theo nhóm, để popup
        ///                  thủ công biết mà khóa thao tác thay vì để người trực bấm đè lên automation.
        ///              (2) Thêm key AutomationPendingKey chứa các sự cố đang đếm ngược, phục vụ banner.
        ///                  Key này KHÔNG lọc theo nhóm, chỉ theo công tắc phạm vi xem.
        ///
        ///              Không throw: tự động hóa là lớp mở rộng, lỗi ở đây không được làm hỏng luồng
        ///              thủ công - đây là API mà popup xử lý sự cố phụ thuộc vào.
        /// Created date: 24/07/2026
        /// </summary>
        private async Task AttachAutomationAsync(Dictionary<string, List<TmsIncidentView>> result)
        {
            if (result == null)
                return;

            if (!await _permissionService.CanViewAutomationAsync())
                return;

            // Chỉ lấy các run còn sống. Dùng danh sách trạng thái DƯƠNG (IN) thay vì phủ định danh sách
            // terminal (NOT IN) để câu SQL sinh ra rõ ràng và không phụ thuộc cách SqlSugar dịch toán tử !.
            var activeStatuses = new[]
            {
                IncidentAutomationConst.RunStatus.WarningPending,
                IncidentAutomationConst.RunStatus.Starting,
                IncidentAutomationConst.RunStatus.Running,
                IncidentAutomationConst.RunStatus.Active,
                IncidentAutomationConst.RunStatus.FinishPending,
                IncidentAutomationConst.RunStatus.Canceling,
                IncidentAutomationConst.RunStatus.ResettingDefault
            };

            var runRows = await _automationRunRepo.AsQueryable()
                .LeftJoin<TmsIncident>((r, i) => r.IncidentId == i.ID)
                .LeftJoin<TmsEventType>((r, i, et) => r.EventTypeId == et.ID)
                .LeftJoin<TmsTrafficPlans>((r, i, et, p) => r.TrafficPlanId == p.ID)
                .LeftJoin<TmsIncidentAutomationConfig>((r, i, et, p, c) => r.AutomationConfigId == c.ID && c.IsDelete == null)
                .Where((r, i, et, p, c) => r.IsDelete == null && i.IsDelete == null)
                .Where((r, i, et, p, c) => activeStatuses.Contains(r.Status))
                .OrderBy((r, i, et, p, c) => r.ExecuteDueAt, OrderByType.Asc)
                .Select((r, i, et, p, c) => new AutomationRunRow
                {
                    IncidentId = r.IncidentId,
                    RunId = r.ID,
                    Status = r.Status,
                    WarningStartedAt = r.WarningStartedAt,
                    ExecuteDueAt = r.ExecuteDueAt,
                    AutoFinishAt = r.AutoFinishAt,
                    ConfigSnapshotJson = r.ConfigSnapshotJson,
                    IsAutoFinishEnabled = c.IsAutoFinishEnabled,
                    AutoFinishAfterSeconds = c.AutoFinishAfterSeconds,
                    TrafficPlanName = p.Name,
                    EventTypeUrl = et.Url
                })
                .ToListAsync();

            runRows ??= new List<AutomationRunRow>();

            var canOperate = await _permissionService.CanOperateAutomationAsync();
            var serverTime = BaseUtil.TimeServer.GetTimeServer();

            // Một sự cố có thể có nhiều run trong lịch sử; ở đây chỉ còn run đang sống nên lấy bản đầu tiên.
            var runByIncident = runRows
                .GroupBy(row => row.IncidentId)
                .ToDictionary(g => g.Key, g => g.First());

            // (1) Điền Automation vào các danh sách theo nhóm đã dựng ở trên.
            foreach (var viewList in result.Values)
            {
                viewList?.ForEach(view =>
                {
                    var incidentId = view?.Incident?.ID;
                    if (incidentId != null && runByIncident.TryGetValue(incidentId, out var row))
                        view.Automation = row.ToView(canOperate, serverTime);
                });
            }

            // (1b) KienLT - 16/10/2026: sự cố Đang xử lý không còn run sống mà run GẦN NHẤT đã Failed (tự động dừng
            // giữa chừng và đã bàn giao) - gắn run đó kèm lý do để FE hiện nhãn "Tự động lỗi". Trước đây chỉ gắn run
            // còn sống nên người trực không biết vì sao sự cố tự động lại nằm chờ xử lý tay.
            await AttachFailedRunsAsync(result, canOperate, serverTime);

            if (runRows.Count == 0)
                return;

            // (2) Dựng danh sách riêng cho banner: chỉ những run đang đếm ngược chờ thực thi.
            var pendingRows = runRows
                .Where(row => row.Status == IncidentAutomationConst.RunStatus.WarningPending)
                .ToList();

            if (pendingRows.Count == 0)
                return;

            var pendingIncidentIds = pendingRows.Select(row => row.IncidentId).Distinct().ToList();

            // Banner tự động theo quy trình chung: hồ sơ nào có run đang đếm ngược thì hiện, kể cả nhóm
            // sự kiện. Sự kiện KHÔNG vào hai danh sách xử lý theo vai trò ở trên, nhưng người trực vẫn
            // phải thấy đếm ngược để hủy tự động hoặc chuyển thủ công.
            var pendingIncidentList = await _incidentRepo.AsQueryable().Where(x => x.State != IncidentRecordingConst.LegacyState)
                .LeftJoin<TmsEventType>((ti, et) => ti.EventTypeId == et.ID)
                .Where((ti, et) => ti.IsDelete == null && pendingIncidentIds.Contains(ti.ID))
                .Select((ti, et) => new TmsIncidentOutput() { EventTypeCode = et.Code, EventTypeName = et.Name }, true)
                .ToListAsync();

            var pendingViewList = new List<TmsIncidentView>();
            foreach (var row in pendingRows)
            {
                var incident = pendingIncidentList?.FirstOrDefault(ti => ti.ID == row.IncidentId);
                if (incident == null)
                    continue;

                pendingViewList.Add(new TmsIncidentView()
                {
                    Incident = incident,
                    WorkExecuteList = null,
                    Automation = row.ToView(canOperate, serverTime)
                });
            }

            if (pendingViewList.Count > 0)
                result[AutomationPendingKey] = pendingViewList;
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Với các sự cố Đang xử lý trong kết quả chưa có Automation: nếu run gần nhất (RunNo lớn nhất)
        ///              của sự cố đang Failed thì gắn run đó. Chỉ lấy run gần nhất để run lỗi cũ không che run sau
        ///              đã hoàn thành/huỷ.
        /// Created date: 16/10/2026
        /// </summary>
        private async Task AttachFailedRunsAsync(Dictionary<string, List<TmsIncidentView>> result, bool canOperate, DateTime serverTime)
        {
            var inProgress = ((int)TmsIncidentStateEnum.InProgress).ToString();
            var views = result.Values
                .Where(list => list != null)
                .SelectMany(list => list)
                .Where(view => view?.Automation == null && view?.Incident?.ID != null && view.Incident.State == inProgress)
                .ToList();
            if (views.Count == 0)
                return;

            var incidentIds = views.Select(view => view.Incident.ID).Distinct().ToList();
            var failedRows = await _automationRunRepo.AsQueryable()
                .LeftJoin<TmsEventType>((r, et) => r.EventTypeId == et.ID)
                .LeftJoin<TmsTrafficPlans>((r, et, p) => r.TrafficPlanId == p.ID)
                .Where((r, et, p) => r.IsDelete == null && incidentIds.Contains(r.IncidentId))
                .OrderBy((r, et, p) => r.RunNo, OrderByType.Desc)
                .Select((r, et, p) => new AutomationRunRow
                {
                    IncidentId = r.IncidentId,
                    RunId = r.ID,
                    Status = r.Status,
                    WarningStartedAt = r.WarningStartedAt,
                    ExecuteDueAt = r.ExecuteDueAt,
                    AutoFinishAt = r.AutoFinishAt,
                    ConfigSnapshotJson = r.ConfigSnapshotJson,
                    TrafficPlanName = p.Name,
                    EventTypeUrl = et.Url,
                    ErrorCode = r.ErrorCode,
                    ErrorMessage = r.ErrorMessage
                })
                .ToListAsync();

            var latestFailed = (failedRows ?? new List<AutomationRunRow>())
                .GroupBy(row => row.IncidentId)
                .Select(g => g.First())
                .Where(row => row.Status == IncidentAutomationConst.RunStatus.Failed)
                .ToDictionary(row => row.IncidentId);

            foreach (var view in views)
            {
                if (latestFailed.TryGetValue(view.Incident.ID, out var row))
                    view.Automation = row.ToView(canOperate, serverTime);
            }
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Bản ghi phẳng của automation run dùng nội bộ khi ghép dữ liệu. Tách riêng vì
        ///              SqlSugar không project trực tiếp được vào object lồng nhau.
        /// Created date: 24/07/2026
        /// </summary>
        private class AutomationRunRow
        {
            public string IncidentId { get; set; }
            public string RunId { get; set; }
            public string Status { get; set; }
            public DateTime? WarningStartedAt { get; set; }
            public DateTime? ExecuteDueAt { get; set; }
            public DateTime? AutoFinishAt { get; set; }
            public string? ConfigSnapshotJson { get; set; }
            public bool? IsAutoFinishEnabled { get; set; }
            public int? AutoFinishAfterSeconds { get; set; }
            public string? TrafficPlanName { get; set; }
            public string? EventTypeUrl { get; set; }
            public string? ErrorCode { get; set; }
            public string? ErrorMessage { get; set; }

            public IncidentAutomationView ToView(bool canOperate, DateTime serverTime)
            {
                var autoFinishConfig = GetAutoFinishConfig();
                var isAutoFinishEnabled = IsAutoFinishEnabled ?? autoFinishConfig.IsEnabled;
                var autoFinishAfterSeconds = AutoFinishAfterSeconds ?? autoFinishConfig.AfterSeconds;

                return new IncidentAutomationView()
                {
                    RunId = RunId,
                    Status = Status,
                    WarningStartedAt = WarningStartedAt,
                    ExecuteDueAt = ExecuteDueAt,
                    AutoFinishAt = AutoFinishAt,
                    IsAutoFinishEnabled = isAutoFinishEnabled && autoFinishAfterSeconds > 0,
                    AutoFinishAfterSeconds = autoFinishAfterSeconds,
                    TrafficPlanName = TrafficPlanName,
                    EventTypeUrl = EventTypeUrl,
                    CanOperate = canOperate,
                    ServerTime = serverTime,
                    ErrorCode = ErrorCode,
                    ErrorMessage = ErrorMessage
                };
            }

            private (bool IsEnabled, int? AfterSeconds) GetAutoFinishConfig()
            {
                if (string.IsNullOrWhiteSpace(ConfigSnapshotJson))
                    return (false, null);

                try
                {
                    var config = JsonConvert.DeserializeObject<TmsIncidentAutomationConfig>(ConfigSnapshotJson);
                    var afterSeconds = config?.AutoFinishAfterSeconds;
                    return (config?.IsAutoFinishEnabled == true && afterSeconds > 0, afterSeconds);
                }
                catch (JsonException)
                {
                    return (false, null);
                }
            }
        }

        private static bool IsUserLoggedIn()
        {
            return App.User != null && App.User.Identity.IsAuthenticated == true;
        }
    }
}
