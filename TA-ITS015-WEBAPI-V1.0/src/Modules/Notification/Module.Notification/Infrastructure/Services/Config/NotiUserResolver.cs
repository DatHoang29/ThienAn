using Microsoft.Extensions.Logging;
using Modules.Notification.Core.Enums;
using Modules.Notification.Core.Messages;
using Modules.Notification.Infrastructure.Services;
using Modules.System.Controllers.Org.Dto;
using Modules.System.Controllers.User.Dto;
using Modules.System.Core.Entities;
using Wolverine;
using static Shared.DTO.Enums.BaseEnums;

namespace Module.Notification.Infrastructure.Services.Config
{
    /// <summary>
    /// Author: SonTH
    /// Description: Resolve người nhận theo 3 phạm vi của NotiEnums.TargetTypeEnum.
    ///
    ///              User/org thuộc phân hệ System — hỏi QUA MESSBUS (Wolverine) tới
    ///              UserQueryHandler/OrgQueryHandler của Module.System (DLL), theo đúng nếp
    ///              MailConfigService: "hỏi qua bus chứ không tự mở repository sang bảng của
    ///              phân hệ khác". Không đi qua HTTP/Refit — cùng process, gọi thẳng handler
    ///              nhanh hơn và không phụ thuộc token của caller.
    ///
    ///              Contract (xác minh từ source gốc ITS_New/Core-Template .../Modules.System):
    ///              - UserQueryHandler.HandleAsync(ListUserInput{Status=Enable}) -> List&lt;UserOutput&gt;
    ///                : phẳng toàn bộ user đang Enable, đã join sẵn OrgName (UserOutput : SysUser).
    ///              - OrgQueryHandler.HandleAsync(OrgInput{ID="0"}) -> List&lt;SysOrg&gt;
    ///                : CÂY org, nút con nằm trong SysOrg.Children.
    ///
    ///              Lưu ý quyền: handler phía System tự loại tài khoản SuperAdmin khỏi kết quả
    ///              khi caller không phải SuperAdmin — fan-out vì vậy cũng bỏ superadmin, chấp nhận.
    ///
    ///              Semantics của Org/All là "phát thanh tại thời điểm gửi": user tạo sau,
    ///              user chuyển org sau KHÔNG thấy lại thông báo cũ — chủ đích thiết kế (tài liệu §6.2).
    /// Created date: 17/09/2026
    /// </summary>
    public class NotiUserResolver : INotiUserResolver, IScoped
    {
        private readonly IMessageBus _bus;

        public NotiUserResolver(IMessageBus bus)
        {
            _bus = bus;
        }

        public async Task<List<NotiUserSeed>> ResolveAsync(SendNotificationCommand command)
        {
            switch (command.TargetType)
            {
                case NotiEnums.TargetTypeEnum.User:
                    return ResolveByUserIds(command.UserIds);

                case NotiEnums.TargetTypeEnum.Org:
                    return await ResolveByOrgIdsAsync(command.OrgIds, command.IncludeChildren);

                case NotiEnums.TargetTypeEnum.All:
                    return await ResolveAllAsync();

                default:
                    return new List<NotiUserSeed>();
            }
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Gửi riêng từng người — giữ thứ tự UserIds, bỏ trùng và id rỗng.
        /// Created date: 17/09/2026
        /// </summary>
        private static List<NotiUserSeed> ResolveByUserIds(IEnumerable<string>? userIds)
        {
            return (userIds ?? Enumerable.Empty<string>())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct()
                .Select(u => new NotiUserSeed { UserId = u })
                .ToList();
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Gửi cho org: tính subtree của các org được chọn từ cây org, rồi lọc
        ///              user theo OrgId thuộc subtree đó. Chỉ nhận đúng org được chọn + con cháu
        ///              của nó — org ngoài subtree bị loại cả nhánh.
        /// Created date: 17/09/2026
        /// </summary>
        private async Task<List<NotiUserSeed>> ResolveByOrgIdsAsync(IEnumerable<string>? orgIds, bool includeChildren)
        {
            var rootIds = (orgIds ?? Enumerable.Empty<string>())
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct()
                .ToList();

            if (rootIds.Count == 0) return new List<NotiUserSeed>();

            var users = await LoadAllUsersAsync();
            if (users.Count == 0) return new List<NotiUserSeed>();

            var seedIds = rootIds.ToHashSet();
            // Khởi tạo sẵn bằng chính các org được chọn: khi cây org rỗng hoặc bị giới hạn
            // quyền phía System (non-admin), target=Org vẫn còn nghĩa "đúng org được chọn".
            var wantedOrgIds = seedIds.ToHashSet();
            if (includeChildren)
            {
                CollectOrgSubtree(await LoadOrgTreeAsync(), seedIds, wantedOrgIds);
            }

            var seeds = users
                .Where(u => !string.IsNullOrWhiteSpace(u.ID)
                            && !string.IsNullOrWhiteSpace(u.OrgId)
                            && wantedOrgIds.Contains(u.OrgId))
                .Select(u => new NotiUserSeed
                {
                    UserId = u.ID,
                })
                .ToList();

            return DistinctByUser(seeds);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Gửi toàn hệ thống — mọi user đang Enable của phân hệ System.
        /// Created date: 17/09/2026
        /// </summary>
        private async Task<List<NotiUserSeed>> ResolveAllAsync()
        {
            var users = await LoadAllUsersAsync();

            var seeds = users
                .Where(u => !string.IsNullOrWhiteSpace(u.ID))
                .Select(u => new NotiUserSeed
                {
                    UserId = u.ID,
                })
                .ToList();

            return DistinctByUser(seeds);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Gọi handler của Module.System qua bus lấy TOÀN BỘ user đang Enable.
        ///              Kết quả đã join sẵn OrgName (UserOutput : SysUser).
        /// Created date: 17/09/2026
        /// </summary>
        private async Task<List<UserOutput>> LoadAllUsersAsync()
        {
            var users = await _bus.InvokeAsync<List<UserOutput>>(new ListUserInput
            {
                Status = StatusEnum.Enable,
            });

            return users ?? new List<UserOutput>();
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Gọi handler của Module.System qua bus lấy CÂY org từ gốc "0"
        ///              (nút con nằm trong SysOrg.Children).
        /// Created date: 17/09/2026
        /// </summary>
        private async Task<List<SysOrg>> LoadOrgTreeAsync()
        {
            var tree = await _bus.InvokeAsync<List<SysOrg>>(new OrgInput { ID = "0" });

            return tree ?? new List<SysOrg>();
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Duyệt cây org và gom ĐÚNG subtree của các org gốc (seedIds):
        ///              chỉ chấp nhận node khi nó là org được chọn HOẶC cha của nó đã được
        ///              chấp nhận; nhánh không thuộc subtree bị cắt từ trên xuống.
        /// Created date: 18/09/2026
        /// </summary>
        private static void CollectOrgSubtree(IEnumerable<SysOrg> nodes, HashSet<string> seedIds,
            HashSet<string> resultIds)
        {
            foreach (var node in nodes)
            {
                CollectOrgNode(node, isParentAccepted: false, seedIds, resultIds);
            }
        }

        private static void CollectOrgNode(SysOrg? node, bool isParentAccepted, HashSet<string> seedIds,
            HashSet<string> resultIds)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.ID)) return;

            // Node được chấp nhận khi nó là org được chọn, hoặc là con cháu của org được chọn.
            var accepted = isParentAccepted || seedIds.Contains(node.ID);

            if (accepted)
            {
                resultIds.Add(node.ID);

                foreach (var child in node.Children ?? new List<SysOrg>())
                {
                    CollectOrgNode(child, isParentAccepted: true, seedIds, resultIds);
                }

                return;
            }

            // Node ngoài subtree: bỏ qua node này nhưng VẪN duyệt xuống con —
            // org được chọn có thể nằm sâu dưới một nhánh khác.
            foreach (var child in node.Children ?? new List<SysOrg>())
            {
                CollectOrgNode(child, isParentAccepted: false, seedIds, resultIds);
            }
        }

        /// <summary>Bỏ user trùng (giữ bản đầu tiên) — resolve từ nhiều nguồn có thể giao nhau.</summary>
        private static List<NotiUserSeed> DistinctByUser(List<NotiUserSeed> seeds)
        {
            var seen = new HashSet<string>();
            var result = new List<NotiUserSeed>();

            foreach (var seed in seeds)
            {
                if (seen.Add(seed.UserId)) result.Add(seed);
            }

            return result;
        }
    }
}
