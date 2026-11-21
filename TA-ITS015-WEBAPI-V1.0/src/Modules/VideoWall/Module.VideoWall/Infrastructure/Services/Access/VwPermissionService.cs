using Shared.DTO.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.WallPermission;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Options;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Services;
using Module.VideoWall.Core.Exceptions;
using Module.VideoWall.Infrastructure.Services.Layout;

namespace Module.VideoWall.Infrastructure.Services.Access
{
    /// <summary>
    /// Description: Dịch vụ phân quyền Video Wall theo vùng màn hình (User / Org Area Grid Access).
    /// Created date: 11/09/2026 — cập nhật 27/09/2026
    /// </summary>
    public class VwPermissionService : IScoped
    {
        private readonly BaseRepository<VwWallPermission> _wallPermissionRep;
        private readonly BaseRepository<VwWallPermissionMember> _wallPermissionMemberRep;
        private readonly BaseRepository<VwWindowScene> _windowSceneRep;
        private readonly UserManager _userManager;
        private readonly BaseCacheService _cache;
        private readonly IOptions<VwDeviceOptions> _deviceOptions;
        private readonly ILogger<VwPermissionService> _logger;
        private readonly VwWallLayoutService _layout;

        public VwPermissionService(
            BaseRepository<VwWallPermission> wallPermissionRep,
            BaseRepository<VwWallPermissionMember> wallPermissionMemberRep,
            BaseRepository<VwWindowScene> windowSceneRep,
            UserManager userManager,
            BaseCacheService cache,
            IOptions<VwDeviceOptions> deviceOptions,
            ILogger<VwPermissionService> logger,
            VwWallLayoutService layout)
        {
            _wallPermissionRep = wallPermissionRep;
            _wallPermissionMemberRep = wallPermissionMemberRep;
            _windowSceneRep = windowSceneRep;
            _userManager = userManager;
            _cache = cache;
            _deviceOptions = deviceOptions;
            _logger = logger;
            _layout = layout;
        }

        /// <summary>
        /// Toàn quyền khi chạy ngoài ngữ cảnh HTTP (tiến trình nền Hangfire/NATS/Internal bus), tài khoản là SuperAdmin,
        /// hoặc đang bật cờ tạm bỏ phân quyền cho giai đoạn đấu nối thiết bị.
        /// Khi có HttpContext (HTTP request) mà không có User (request ẩn danh), tuyệt đối KHÔNG cấp quyền.
        /// </summary>
        public bool IsFullAccess =>
            App.HttpContext == null
            || (App.User != null && _userManager.SuperAdmin)
            || IsPermissionBypassed;

        /// <summary>
        /// Tài khoản đang đăng nhập là SuperAdmin (dùng cho thao tác cấp cả tường, VD Đặt kịch bản mặc định).
        /// Khác IsFullAccess: KHÔNG tính tiến trình nền, cờ Bypass hay bản ghi IsFullAccess.
        /// </summary>
        public bool IsSuperAdmin => App.User != null && _userManager.SuperAdmin;

        /// <summary>
        /// Cờ VideoWall:BypassPermission chỉ có hiệu lực ngoài Production. Bật nhầm ở Production thì
        /// bỏ qua cờ và ghi log lỗi, thay vì ném ngoại lệ làm chết mọi request của phân hệ.
        /// </summary>
        private bool IsPermissionBypassed
        {
            get
            {
                if (_deviceOptions?.Value?.BypassPermission != true)
                    return false;

                if (App.HostEnvironment?.IsProduction() == true)
                {
                    _logger.LogError("VwPermissionService: VideoWall:BypassPermission đang bật ở Production — bỏ qua cờ này.");
                    return false;
                }

                _logger.LogWarning("VwPermissionService: đang bỏ qua phân quyền Video Wall theo cấu hình VideoWall:BypassPermission.");
                return true;
            }
        }

        #region Tính toán ô lưới

        /// <summary>
        /// Description: Tính toán tập các ô lưới mà cửa sổ (x, y, w, h — toạ độ trong khối) phủ lên trong khối.
        /// </summary>
        public static IEnumerable<VwGridCell> GetCoveredCells(
            int block, int x, int y, int w, int h,
            int panelWidthPx = VwWallProfile.PanelWidthPx,
            int panelHeightPx = VwWallProfile.PanelHeightPx)
        {
            if (w <= 0 || h <= 0)
                yield break;

            var colStart = x / panelWidthPx;
            var colEnd = (x + w - 1) / panelWidthPx;
            var rowStart = y / panelHeightPx;
            var rowEnd = (y + h - 1) / panelHeightPx;

            for (var c = colStart; c <= colEnd; c++)
                for (var r = rowStart; r <= rowEnd; r++)
                    yield return new VwGridCell
                    {
                        Block = block,
                        Col = c,
                        Row = r
                    };
        }

        /// <summary>
        /// Description: Kiểm tra tập các ô lưới cửa sổ phủ lên có nằm hoàn toàn trong tập các ô lưới được cấp phép không.
        /// </summary>
        public static bool IsWithinAllowedCells(IEnumerable<VwGridCell> covered, IEnumerable<VwGridCell> allowed)
        {
            var coveredList = covered as IList<VwGridCell> ?? covered.ToList();
            if (coveredList.Count == 0)
                return false;

            var allowedSet = allowed.Select(c => (c.Block, c.Col, c.Row)).ToHashSet();
            if (allowedSet.Count == 0)
                return false;

            return coveredList.All(c => allowedSet.Contains((c.Block, c.Col, c.Row)));
        }

        #endregion

        #region Quyền hiệu lực của người dùng đang đăng nhập

        /// <summary>
        /// Description: Lấy các quyền hiệu lực cho tài khoản hiện tại. Quyền gán trực tiếp cho người dùng
        ///              thay thế quyền gán theo đơn vị; người dùng / đơn vị có thể thuộc nhiều quyền.
        ///              Source = "User" / "Org" (null khi không có quyền nào).
        ///              Chỉ xét quyền thuộc bố cục đang dùng (WallTopologyId) — quyền của phiên bản cũ không có hiệu lực,
        ///              kể cả khi chọn nguồn: người dùng chỉ còn quyền trực tiếp ở bản cũ thì rơi về quyền theo đơn vị.
        /// Created date: 13/09/2026 — cập nhật 23/10/2026
        /// </summary>
        public async Task<(List<VwWallPermission> Permissions, string? Source)> GetEffectivePermissionsAsync()
        {
            (List<VwWallPermission>, string?) empty = ([], null);
            if (App.User == null)
                return empty;

            var userId = _userManager.UserId;
            var orgId = _userManager.OrgId;
            if (string.IsNullOrWhiteSpace(userId))
                return empty;

            // Bố cục chưa lưu ⇒ chưa có quyền nào (tạo quyền đòi bố cục đã lưu)
            var layout = await _layout.GetCurrentAsync();
            if (layout.IsDefault || string.IsNullOrWhiteSpace(layout.ID))
                return empty;

            var permissions = (_cache.GetOrAdd(CacheConst.Vw.VwWallPermission, _ =>
            {
                return _wallPermissionRep.AsQueryable()
                    .Where(u => u.IsDelete == null)
                    .ToListAsync().Result;
            }) ?? []).Where(p => p.WallTopologyId == layout.ID && p.Status != BaseEnums.StatusEnum.Disable).ToList(); // Không dùng ⇒ không có hiệu lực (null = Dùng)
            var currentIds = permissions.Select(p => p.ID).ToHashSet();

            var members = _cache.GetOrAdd($"{CacheConst.Vw.VwWallPermission}:member", _ =>
            {
                return _wallPermissionMemberRep.AsQueryable()
                    .Where(u => u.IsDelete == null)
                    .ToListAsync().Result;
            }) ?? [];

            var source = "User";
            var permissionIds = members.Where(m => m.UserId == userId && currentIds.Contains(m.PermissionId)).Select(m => m.PermissionId).ToHashSet();

            if (permissionIds.Count == 0 && !string.IsNullOrWhiteSpace(orgId))
            {
                source = "Org";
                permissionIds = members
                    .Where(m => string.IsNullOrWhiteSpace(m.UserId) && m.OrgId == orgId && currentIds.Contains(m.PermissionId))
                    .Select(m => m.PermissionId)
                    .ToHashSet();
            }

            var result = permissions.Where(p => permissionIds.Contains(p.ID)).ToList();
            return result.Count == 0 ? empty : (result, source);
        }

        /// <summary>
        /// Description: Tính quyền vùng màn hình hiệu lực của tài khoản hiện tại (dùng chung cho kiểm tra quyền và API GetMy).
        /// Created date: 27/09/2026
        /// </summary>
        public async Task<VwMyWallPermissionOutput> GetMyAccessAsync()
        {
            // 1. SuperAdmin / tiến trình nền / cờ Bypass
            if (IsFullAccess)
                return new VwMyWallPermissionOutput
                {
                    AccessType = VwWallAccessType.Full,
                    IsFullAccess = true,
                    IsSuperAdmin = IsSuperAdmin,
                    AllowedCells = null,
                    Source = "SuperAdmin"
                };

            // 2. Không có quyền nào ⇒ mặc định chặn
            var (permissions, source) = await GetEffectivePermissionsAsync();
            if (permissions.Count == 0)
                return new VwMyWallPermissionOutput
                {
                    AccessType = VwWallAccessType.None,
                    IsFullAccess = false,
                    AllowedCells = []
                };

            // 3. Có ít nhất 1 quyền được đánh dấu toàn quyền
            if (permissions.Any(p => p.IsFullAccess == true))
                return new VwMyWallPermissionOutput
                {
                    AccessType = VwWallAccessType.Full,
                    IsFullAccess = true,
                    AllowedCells = null,
                    Source = source
                };

            // 4. Hợp các ô lưới của mọi quyền; Config rỗng / lỗi định dạng ⇒ quyền đó coi như không cấp ô nào
            var cells = new List<VwGridCell>();
            var seen = new HashSet<(int, int, int)>();
            foreach (var permission in permissions.Where(p => !string.IsNullOrWhiteSpace(p.Config)))
            {
                try
                {
                    var permissionCells = JsonConvert.DeserializeObject<List<VwGridCell>>(permission.Config!) ?? [];
                    cells.AddRange(permissionCells.Where(c => seen.Add((c.Block, c.Col, c.Row))));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "VwPermissionService: Config của VwWallPermission [{Id}] lỗi định dạng.", permission.ID);
                }
            }

            if (cells.Count == 0)
                return new VwMyWallPermissionOutput
                {
                    AccessType = VwWallAccessType.None,
                    IsFullAccess = false,
                    AllowedCells = [],
                    Source = source
                };

            return new VwMyWallPermissionOutput
            {
                AccessType = VwWallAccessType.Area,
                IsFullAccess = false,
                AllowedCells = cells,
                Source = source
            };
        }

        #endregion

        #region Kiểm tra quyền

        /// <summary>
        /// Description: Ép phân quyền — cửa sổ (toạ độ trong khối) phải nằm trọn trong tập các ô lưới được cấp
        ///              cho người dùng hoặc đơn vị hiện tại. Khối rỗng ⇒ khối đầu tiên (dữ liệu trước khi có khối).
        /// </summary>
        public async Task EnsureWindowInsideUserAllowedAreaAsync(int? blockNo, int x, int y, int w, int h, string? label = null)
        {
            var access = await GetMyAccessAsync();
            if (access.AccessType == VwWallAccessType.Full)
                return;

            var layout = await _layout.GetCurrentAsync();
            EnsureInside(access, layout.ScreenWidth!.Value, layout.ScreenHeight!.Value, blockNo ?? 0, x, y, w, h, label);
        }

        /// <summary>
        /// Description: Ép phân quyền cho cả danh sách cửa sổ (kích hoạt / xoá kịch bản):
        ///              mọi cửa sổ phải nằm trong vùng được cấp. Chỉ tính quyền 1 lần.
        /// Created date: 27/09/2026
        /// </summary>
        public async Task EnsureWindowsInsideUserAllowedAreaAsync(IEnumerable<VwWindowScene> windows)
        {
            var access = await GetMyAccessAsync();
            if (access.AccessType == VwWallAccessType.Full)
                return;

            var layout = await _layout.GetCurrentAsync();
            foreach (var win in windows)
                EnsureInside(access, layout.ScreenWidth!.Value, layout.ScreenHeight!.Value,
                    win.BlockNo ?? 0, win.X ?? 0, win.Y ?? 0, win.W ?? 0, win.H ?? 0, win.Name ?? win.ID);

            // Kịch bản rỗng cũng phải có quyền vùng mới được thao tác
            if (access.AccessType == VwWallAccessType.None)
                throw Oops.Oh(BaseMsg.Vw.Exception.WallPermissionNotAssigned);
        }

        /// <summary>
        /// Description: Ép phân quyền cho CẢ KỊCH BẢN — mọi cửa sổ (chưa xoá) của kịch bản phải nằm trong vùng được cấp.
        ///              Dùng cho các thao tác lên cả kịch bản: sửa thông tin, xoá, kích hoạt, gắn lịch / quy tắc sự kiện.
        ///              sceneId rỗng ⇒ bỏ qua (không có kịch bản đích).
        /// Created date: 28/09/2026
        /// </summary>
        public async Task EnsureSceneInsideUserAllowedAreaAsync(string? sceneId)
        {
            if (string.IsNullOrWhiteSpace(sceneId))
                return;

            var access = await GetMyAccessAsync();
            if (access.AccessType == VwWallAccessType.Full)
                return;

            var windows = await _windowSceneRep.AsQueryable()
                .Where(w => w.IsDelete == null && w.SceneId == sceneId)
                .ToListAsync();

            await EnsureWindowsInsideUserAllowedAreaAsync(windows);
        }

        /// <summary>
        /// Description: Chặn khi tài khoản chưa được cấp vùng nào (VD tạo kịch bản mới).
        /// Created date: 28/09/2026
        /// </summary>
        public async Task EnsureHasAnyAreaAsync()
        {
            var access = await GetMyAccessAsync();
            if (access.AccessType == VwWallAccessType.None)
                throw Oops.Oh(BaseMsg.Vw.Exception.WallPermissionNotAssigned);
        }

        /// <summary>
        /// Description: Chỉ SuperAdmin (VD Đặt kịch bản mặc định — cài đặt của cả tường).
        /// Created date: 28/09/2026
        /// </summary>
        public void EnsureSuperAdmin(string messageKey)
        {
            if (!IsSuperAdmin)
                throw Oops.Oh(messageKey);
        }

        private static void EnsureInside(
            VwMyWallPermissionOutput access, int panelWidthPx, int panelHeightPx,
            int block, int x, int y, int w, int h, string? label)
        {
            if (access.AccessType == VwWallAccessType.Full)
                return;

            if (access.AccessType == VwWallAccessType.None || access.AllowedCells == null || access.AllowedCells.Count == 0)
                throw Oops.Oh(BaseMsg.Vw.Exception.WallPermissionNotAssigned);

            var covered = GetCoveredCells(block, x, y, w, h, panelWidthPx, panelHeightPx);
            if (!IsWithinAllowedCells(covered, access.AllowedCells))
                throw Oops.Oh(BaseMsg.Vw.Exception.WindowOutsideAllowedRegion, label ?? string.Empty);
        }

        #endregion
    }
}
