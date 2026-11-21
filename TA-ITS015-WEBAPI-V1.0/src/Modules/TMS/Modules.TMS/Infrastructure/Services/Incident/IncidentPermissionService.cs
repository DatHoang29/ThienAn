using Modules.CfgSystem.Core.Dto.OpConfig;
using Shared.Core.Utilities.Constants;
using Shared.DTO.Constants.Application;
using Shared.DTO.Dtos.System;
using Shared.Infrastructure.Services;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.System;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Infrastructure.Services.Incident
{
    /// <summary>
    /// Author: ThangNN (refactor)
    /// Description: Xử lý phân quyền nhóm tài khoản cho phân hệ sự cố
    /// (đọc cấu hình nhóm tms_incident_action_group và xác định hành động được phép).
    /// </summary>
    public class IncidentPermissionService : IScoped
    {
        private const string AccountGroupConfigCode = "tms_incident_action_group";

        /// <summary>
        /// Author: SonTH - 24/07/2026
        /// Code của bản ghi bật/tắt phạm vi hiển thị banner cảnh báo tự động, NẰM TRONG nhóm
        /// tms_incident_action_group (AccountGroupConfigCode) - không phải một GroupCode riêng.
        /// Value hợp lệ: xem AutomationBannerScope.
        /// </summary>
        private const string AutomationBannerConfigCode = "automation_banner";

        // Cache trong phạm vi request (IScoped) để không gọi cấu hình lặp lại trong cùng request.
        private List<SysOpConfigResponse> _accountGroupList;

        private BaseCacheService _cache;
        private readonly IMessageBus _messageBus;

        public IncidentPermissionService(BaseCacheService cache, IMessageBus messageBus)
        {
            _cache = cache;
            _messageBus = messageBus;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Phạm vi tài khoản được NHÌN THẤY banner cảnh báo tự động.
        ///              Lưu ý: đây chỉ là quyền xem. Quyền THAO TÁC luôn giới hạn ở nhóm giám sát,
        ///              xem CanOperateAutomationAsync.
        /// Created date: 24/07/2026
        /// </summary>
        public static class AutomationBannerScope
        {
            /// <summary>Mọi tài khoản đều thấy banner (chỉ xem + đếm ngược). Mặc định.</summary>
            public const string All = "all";

            /// <summary>Chỉ nhóm giám sát thấy banner.</summary>
            public const string Supervisor = "supervisor";
        }

        /// <summary>
        /// Kiểm tra OrgCode của user hiện tại có thuộc nhóm chỉ định không.
        /// </summary>
        public async Task<bool> IsInGroupAsync(string groupCode)
        {
            groupCode = groupCode.ToLower();

            if ((this._accountGroupList?.Count() ?? 0) == 0)
            {
                var query = new SysOpConfigListRequest() { GroupCode = AccountGroupConfigCode };
                var response = await BaseApiClient<IOpConfigApi>.Client(null, null, TimeSpan.FromSeconds(20)).List(query);
                this._accountGroupList = response.Content?.Result;
            }

            var accountGroup = App.User.FindFirst(ClaimConst.OrgCode)?.Value.ToLower();
            if (accountGroup == null)
                return false;

            var groupList = this._accountGroupList?
                .Where(item => item.Code != null && item.Code.ToLower() == groupCode)
                .Select(item => item.Value)
                .FirstOrDefault();

            if (groupList != null && groupList.Replace(" ", "").ToLower().Split(",").Contains(accountGroup))
                return true;

            return accountGroup == groupCode;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Đọc phạm vi hiển thị banner tự động từ SysOpConfig.
        ///              Cache lại vì ListByAccount được gọi lại trên MỌI biến động sự cố (đường đi nóng),
        ///              không thể gọi HTTP sang phân hệ System mỗi lần.
        ///              Thiếu cấu hình / giá trị lạ => mặc định All (hiện cho tất cả, chỉ xem).
        /// Created date: 24/07/2026
        /// </summary>
        public async Task<string> GetAutomationBannerScopeAsync()
        {
            // Khoá cache ghép đúng GroupCode:Code thật trong DB để không gây hiểu sai là có nhóm riêng.
            var cacheKey = $"{CacheConst.Tms.TmsIncidentActionGroup}:{AutomationBannerConfigCode}";

            var value = await _cache.GetOrAdd(cacheKey, async _ =>
            {
                var configs = await _messageBus.InvokeAsync<List<SysOpConfigOutput>>(
                    new SysOpConfigInput { GroupCode = AccountGroupConfigCode });

                // Trả chuỗi rỗng thay vì null để FusionCache vẫn cache được kết quả "chưa cấu hình".
                return configs?.FirstOrDefault(config =>
                    string.Equals(config.Code, AutomationBannerConfigCode, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
            });

            return string.Equals(value?.Trim(), AutomationBannerScope.Supervisor, StringComparison.OrdinalIgnoreCase)
                ? AutomationBannerScope.Supervisor
                : AutomationBannerScope.All;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Tài khoản hiện tại có được thấy banner cảnh báo tự động hay không.
        /// Created date: 24/07/2026
        /// </summary>
        public async Task<bool> CanViewAutomationAsync()
        {
            var scope = await GetAutomationBannerScopeAsync();
            if (scope == AutomationBannerScope.All)
                return true;

            return await IsInGroupAsync(AccountOrgCodeEnum.GroupSupervisor);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Tài khoản hiện tại có được THAO TÁC trên luồng tự động (thực thi ngay / chuyển thủ công)
        ///              hay không. Chỉ nhóm giám sát, độc lập hoàn toàn với công tắc phạm vi xem.
        /// Created date: 24/07/2026
        /// </summary>
        public async Task<bool> CanOperateAutomationAsync()
        {
            return await IsInGroupAsync(AccountOrgCodeEnum.GroupSupervisor);
        }

        /// <summary>
        /// Danh sách các thao tác được phép tương ứng với nhóm của tài khoản hiện tại.
        /// </summary>
        public async Task<List<int>> GetAllowedActionsAsync()
        {
            if (await this.IsInGroupAsync(AccountOrgCodeEnum.GroupSupervisor))
                return new List<int>()
                {
                    (int)TmsIncidentActionEnum.Registration,
                    (int)TmsIncidentActionEnum.Verify,
                    (int)TmsIncidentActionEnum.Execute,
                    (int)TmsIncidentActionEnum.Cancel,
                    (int)TmsIncidentActionEnum.Skip,
                    (int)TmsIncidentActionEnum.Detail,
                    (int)TmsIncidentActionEnum.LiveCam
                };

            if (await this.IsInGroupAsync(AccountOrgCodeEnum.GroupLeader))
                return new List<int>()
                {
                    (int)TmsIncidentActionEnum.Registration,
                    (int)TmsIncidentActionEnum.Finish,
                    (int)TmsIncidentActionEnum.Report,
                    (int)TmsIncidentActionEnum.Detail,
                    (int)TmsIncidentActionEnum.LiveCam
                };

            return new List<int>()
            {
                (int)TmsIncidentActionEnum.Registration,
                (int)TmsIncidentActionEnum.Execute,
                (int)TmsIncidentActionEnum.Detail,
                (int)TmsIncidentActionEnum.LiveCam
            };
        }
    }
}
