using Microsoft.Extensions.Logging;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Helper;
using Modules.TMS.Core.Exceptions;
using Furion.Localization;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Modules.TMS.Infrastructure.Services.Incident;
using Shared.Core.Utilities.Constants;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Controllers.Incident.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Description: Handler đọc dữ liệu nghiệp vụ sự cố theo tài khoản/phân quyền:
    /// LatestIncident / CheckIncident / ListByAccount / ListAllowedAction.
    /// Tách từ IncidentQueryHandler để phân tách rõ CRUD và workflow.
    /// </summary>
    public class IncidentWorkflowQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsIncident> _incidentRepo;
        private BaseRepository<TmsWorkExecute> _workExecuteRepo;
        private BaseRepository<TmsIncidentAutomationRun> _automationRunRepo;
        private IncidentPermissionService _permissionService;
        private IncidentAccessService _accessService;
        private ILogger<IncidentWorkflowQueryHandler> _logger;

        public IncidentWorkflowQueryHandler(
            BaseRepository<TmsIncident> incidentRepo,
            BaseRepository<TmsWorkExecute> workExecuteRepo,
            BaseRepository<TmsIncidentAutomationRun> automationRunRepo,
            IncidentPermissionService permissionService,
            IncidentAccessService accessService,
            ILogger<IncidentWorkflowQueryHandler> logger)
        {
            this._incidentRepo = incidentRepo;
            this._workExecuteRepo = workExecuteRepo;
            this._automationRunRepo = automationRunRepo;
            this._permissionService = permissionService;
            this._accessService = accessService;
            this._logger = logger;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy top N sự cố đã xác thực và chưa kết thúc
        /// </summary>
        public async Task<List<TmsIncidentOutput>> HandleAsync(TmsIncidentLatestInput command)
        {
            if (!IsUserLoggedIn())
                return null;

            var incidentList = await this._incidentRepo.AsQueryable()
                    .LeftJoin<TmsEventType>((ti, et) => ti.EventTypeId == et.ID)
                    .Where((ti, et) => ti.State == ((int)TmsIncidentStateEnum.Verified).ToString() || ti.State == ((int)TmsIncidentStateEnum.InProgress).ToString())
                    .Where((ti, et) => ti.IsDelete == null)
                    .OrderBy((ti, et) => et.Priority, OrderByType.Asc)
                    .OrderBy((ti, et) => ti.StartDate, OrderByType.Desc)
                    .Select((ti, et) => new TmsIncidentOutput() { EventTypeCode = et.Code, EventTypeName = et.Name }, true)
                    .Take(command.Count).ToListAsync();

            foreach (var item in incidentList.Where(item => IncidentEventDisplayHelper.IsUnknown(item.EventTypeCode)))
                item.EventTypeName = IncidentEventDisplayHelper.Name(item.EventTypeCode, item.EventTypeName, item.Metadata,
                    L.Text[BaseMsg.TmsIncident.Label.UnknownEventType].Value);
            return incidentList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Kiểm tra có sự cố theo Account
        /// </summary>
        public async Task<bool> HandleAsync(TmsIncidentCheckInput command)
        {
            if (!IsUserLoggedIn())
                return false;

            // Sử dụng OrgId của User để lọc work-execute theo ExecuteOrgId
            string userOrgId = App.User.FindFirst(ClaimConst.OrgId)?.Value;

            // Sử dụng OrgCode của User để lọc incident tương ứng phân quyền
            var orgCode = App.User.FindFirst(ClaimConst.OrgCode)?.Value;

            if (string.IsNullOrWhiteSpace(userOrgId) || string.IsNullOrWhiteSpace(orgCode))
                return false;

            // Author: SonTH - 24/07/2026
            // Có sự cố đang đếm ngược chờ tự động thực thi thì mở luồng dữ liệu cho MỌI nhóm, kể cả nhóm
            // đang không có việc gì chờ xử lý. Không mở thì frontend dừng ngay tại đây, không gọi
            // ListByAccount, và banner cảnh báo sẽ không bao giờ hiện với các nhóm đó.
            // Đây chỉ là quyền XEM; quyền thao tác do IncidentPermissionService.CanOperateAutomationAsync quyết định.
            if (await this.HasViewablePendingAutomationAsync())
                return true;

            // Nếu OrgCode là TcVhh
            var isLeader = await this._permissionService.IsInGroupAsync(AccountOrgCodeEnum.GroupLeader);
            if (isLeader)
                return await this._incidentRepo.IsAnyAsync(ti =>
                    ti.State == ((int)TmsIncidentStateEnum.InProgress).ToString()
                    && ti.IsDelete == null);

            // Nếu OrgCode là NvGsgt
            var isSupervisor = await this._permissionService.IsInGroupAsync(AccountOrgCodeEnum.GroupSupervisor);
            if (isSupervisor)
            {
                // Kiểm tra nếu tồn tại sự cố mới đăng ký hoặc đã được xác thực
                if (await this._incidentRepo.IsAnyAsync(ti =>
                            (ti.State == ((int)TmsIncidentStateEnum.Registration).ToString()
                                || ti.State == ((int)TmsIncidentStateEnum.Verified).ToString())
                            && ti.IsDelete == null
                    ))
                    return true;
            }

            // Tiếp tục kiểm tra nếu tồn tại công việc đang chờ nhóm khác (Bao gồm NvGsgt) xử lý
            return await this._workExecuteRepo.AsQueryable()
                .InnerJoin<TmsIncident>((we, ti) => we.IncidentId == ti.ID)
                .Where((we, ti) => ti.State == ((int)TmsIncidentStateEnum.InProgress).ToString() && ti.IsDelete == null)
                .Where((we, ti) => we.State == ((int)TmsWorkExecuteStateEnum.Waiting).ToString() && we.IsDelete == null)
                .Where((we, ti) => we.ExecuteOrg == userOrgId)
                .AnyAsync();
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Có tồn tại sự cố đang chờ tự động thực thi mà tài khoản hiện tại được phép xem hay không.
        ///              Không throw: đây là lớp mở rộng, lỗi ở đây chỉ được làm mất banner chứ không được
        ///              làm sập luồng kiểm tra sự cố thủ công.
        /// Created date: 24/07/2026
        /// </summary>
        private async Task<bool> HasViewablePendingAutomationAsync()
        {
            try
            {
                if (!await this._permissionService.CanViewAutomationAsync())
                    return false;

                return await this._automationRunRepo.IsAnyAsync(r =>
                    r.Status == IncidentAutomationConst.RunStatus.WarningPending && r.IsDelete == null);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "[IncidentWorkflowQueryHandler] Kiểm tra sự cố chờ tự động thực thi thất bại.");
                return false;
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy danh sách sự cố theo account
        /// Note: Logic đã tách sang IncidentAccessService.
        /// </summary>
        public async Task<Dictionary<string, List<TmsIncidentView>>> HandleAsync(TmsIncidentByAccountInput command)
        {
            return await this._accessService.GetByAccountAsync();
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Danh sách các thao tác tương ứng với account
        /// Note: Logic phân quyền đã tách sang IncidentPermissionService.
        /// </summary>
        public async Task<List<int>> HandleAsync(TmsIncidentAllowedActionInput command)
        {
            return await this._permissionService.GetAllowedActionsAsync();
        }

        private static bool IsUserLoggedIn()
        {
            return App.User?.Identity?.IsAuthenticated == true;
        }
    }
}
